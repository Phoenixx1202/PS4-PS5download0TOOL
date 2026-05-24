using System.Buffers.Binary;
using System.Text;

namespace PS4_PS5download0TOOL.Services
{
    public sealed class Ufs2ImageBuilder
    {
        private const int BlockSize = 65536;
        private const int SectorSize = 512;
        private const int SuperBlockSize = 8192;
        private const int SuperBlockOffset = 65536;
        private const int SuperBlockCopyBlock = 2;
        private const int CylinderGroupBlock = 3;
        private const int InodeStartBlock = 4;
        private const int DirectoryRecordBlockSize = 512;
        private const int InodeSize = 256;
        private const int InodesPerBlock = BlockSize / InodeSize;
        private const int RootInode = 2;
        private const int DirectBlockCount = 12;
        private const int IndirectBlockCapacity = BlockSize / sizeof(long);
        private const int MinimumImageSize = 64 * 1024 * 1024;
        private const int FsFlagsUpdated = 0x80;
        private const int FsIsClean = 0x01;
        private const uint Ufs2Magic = 0x19540119;
        private const uint CylinderGroupMagic = 0x00090255;
        private const ushort DirectoryMode = 0x4000 | 0x01ff;
        private const ushort RegularMode = 0x8000 | 0x01ff;
        private const byte DirectoryType = 4;
        private const byte RegularType = 8;

        public static async Task BuildAsync(
            string sourceDirectory,
            string outputPath,
            long imageSize,
            CancellationToken ct)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException(sourceDirectory);

            imageSize = AlignTo(Math.Max(imageSize, MinimumImageSize), BlockSize);
            var imageBlocksLong = imageSize / BlockSize;
            if (imageBlocksLong > int.MaxValue)
                throw new InvalidOperationException("The image is too large for this UFS2 builder.");

            var root = BuildTree(new DirectoryInfo(sourceDirectory), null);
            var nextInode = RootInode + 1;
            AssignInodes(root, ref nextInode);

            var nodes = Flatten(root).ToList();
            var directories = nodes.Where(node => node.IsDirectory).ToList();
            foreach (var directory in directories)
                directory.DirectoryContent = BuildDirectoryContent(directory);

            var imageBlocks = (int)imageBlocksLong;
            var inodeCount = AlignTo(Math.Max(1024, nextInode + 16), InodesPerBlock);
            var inodeBlocks = inodeCount / InodesPerBlock;
            var dataStartBlock = InodeStartBlock + inodeBlocks;
            var firstPayloadBlock = dataStartBlock + 1;

            if (firstPayloadBlock >= imageBlocks)
                throw new InvalidOperationException("The image is too small for the UFS2 metadata.");

            var nextBlock = firstPayloadBlock;
            foreach (var node in nodes.OrderBy(node => node.Inode))
            {
                var dataBlockCount = node.GetDataBlockCount();
                if (dataBlockCount > DirectBlockCount + IndirectBlockCapacity)
                    throw new InvalidOperationException($"File is too large for single-indirect UFS2 storage: {node.RelativePath}");

                for (var i = 0; i < dataBlockCount; i++)
                    node.DataBlocks.Add(nextBlock++);

                if (dataBlockCount > DirectBlockCount)
                    node.IndirectBlock = nextBlock++;
            }

            if (nextBlock > imageBlocks)
                throw new InvalidOperationException("The selected image size is too small for the edited files.");

            var allocatedPayloadBlocks = nextBlock - firstPayloadBlock;
            var freeBlocks = Math.Max(0, imageBlocks - firstPayloadBlock - allocatedPayloadBlocks);
            var freeInodes = Math.Max(0, inodeCount - RootInode - nodes.Count);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await using var image = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, BlockSize, true);
            image.SetLength(imageSize);

            var superBlock = CreateSuperBlock(
                imageBlocks,
                inodeCount,
                dataStartBlock,
                directories.Count,
                freeBlocks,
                freeInodes,
                now);
            await WriteAtAsync(image, SuperBlockOffset, superBlock, SuperBlockSize, ct);
            await WriteAtAsync(image, (long)SuperBlockCopyBlock * BlockSize, superBlock, SuperBlockSize, ct);

            var summary = CreateSummaryBlock(directories.Count, freeBlocks, freeInodes);
            await WriteAtAsync(image, (long)dataStartBlock * BlockSize, summary, summary.Length, ct);

            var cylinderGroup = CreateCylinderGroup(
                imageBlocks,
                inodeCount,
                dataStartBlock,
                firstPayloadBlock,
                nextBlock,
                directories.Count,
                freeBlocks,
                freeInodes,
                nodes,
                now);
            await WriteAtAsync(image, (long)CylinderGroupBlock * BlockSize, cylinderGroup, cylinderGroup.Length, ct);

            foreach (var node in nodes)
                await WriteInodeAsync(image, node, now, ct);

            foreach (var node in nodes.OrderBy(node => node.Inode))
                await WriteNodeDataAsync(image, node, ct);
        }

        private static UfsNode BuildTree(DirectoryInfo directory, UfsNode? parent)
        {
            var node = new UfsNode(directory.Name, directory.FullName, true, parent);

            foreach (var childDirectory in directory.GetDirectories().OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
                node.Children.Add(BuildTree(childDirectory, node));

            foreach (var file in directory.GetFiles().OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (file.Name.Equals("extract_summary.txt", StringComparison.OrdinalIgnoreCase))
                    continue;

                node.Children.Add(new UfsNode(file.Name, file.FullName, false, node)
                {
                    FileLength = file.Length,
                    LastWriteTime = file.LastWriteTimeUtc,
                    LastAccessTime = file.LastAccessTimeUtc
                });
            }

            node.LastWriteTime = directory.LastWriteTimeUtc;
            node.LastAccessTime = directory.LastAccessTimeUtc;
            return node;
        }

        private static void AssignInodes(UfsNode node, ref int nextInode)
        {
            if (node.Parent is null)
                node.Inode = RootInode;
            else
                node.Inode = nextInode++;

            foreach (var child in node.Children)
                AssignInodes(child, ref nextInode);
        }

        private static IEnumerable<UfsNode> Flatten(UfsNode node)
        {
            yield return node;

            foreach (var child in node.Children)
            {
                foreach (var descendant in Flatten(child))
                    yield return descendant;
            }
        }

        private static byte[] BuildDirectoryContent(UfsNode directory)
        {
            var entries = new List<DirectoryEntry>
            {
                new(".", directory.Inode, DirectoryType),
                new("..", directory.Parent?.Inode ?? directory.Inode, DirectoryType)
            };

            entries.AddRange(directory.Children.Select(child =>
                new DirectoryEntry(child.Name, child.Inode, child.IsDirectory ? DirectoryType : RegularType)));

            var output = new List<byte>();
            var index = 0;

            while (index < entries.Count)
            {
                var chunkEntries = new List<(DirectoryEntry Entry, byte[] NameBytes, int Size)>();
                var used = 0;

                while (index < entries.Count)
                {
                    var nameBytes = Encoding.UTF8.GetBytes(entries[index].Name);
                    if (nameBytes.Length > byte.MaxValue)
                        throw new InvalidOperationException($"Directory entry name is too long: {entries[index].Name}");

                    var recordSize = GetDirectoryRecordSize(nameBytes.Length);
                    if (used > 0 && used + recordSize > DirectoryRecordBlockSize)
                        break;

                    if (recordSize > DirectoryRecordBlockSize)
                        throw new InvalidOperationException($"Directory entry is too large: {entries[index].Name}");

                    chunkEntries.Add((entries[index], nameBytes, recordSize));
                    used += recordSize;
                    index++;

                    if (used == DirectoryRecordBlockSize)
                        break;
                }

                var chunk = new byte[DirectoryRecordBlockSize];
                var offset = 0;

                for (var i = 0; i < chunkEntries.Count; i++)
                {
                    var item = chunkEntries[i];
                    var recordLength = i == chunkEntries.Count - 1
                        ? DirectoryRecordBlockSize - offset
                        : item.Size;

                    WriteUInt32(chunk, offset, (uint)item.Entry.Inode);
                    WriteUInt16(chunk, offset + 4, (ushort)recordLength);
                    chunk[offset + 6] = item.Entry.Type;
                    chunk[offset + 7] = (byte)item.NameBytes.Length;
                    item.NameBytes.CopyTo(chunk.AsSpan(offset + 8));
                    offset += recordLength;
                }

                output.AddRange(chunk);
            }

            return output.ToArray();
        }

        private static int GetDirectoryRecordSize(int nameLength)
        {
            return 8 + AlignTo(nameLength + 1, 4);
        }

        private static byte[] CreateSuperBlock(
            int imageBlocks,
            int inodeCount,
            int dataStartBlock,
            int directoryCount,
            int freeBlocks,
            int freeInodes,
            long now)
        {
            var block = new byte[SuperBlockSize];
            var inodeBlocks = inodeCount / InodesPerBlock;
            var dataBlocks = imageBlocks - dataStartBlock - 1;
            var maxFileSize = CalculateMaxFileSize();

            WriteInt32(block, 8, SuperBlockCopyBlock);
            WriteInt32(block, 12, CylinderGroupBlock);
            WriteInt32(block, 16, InodeStartBlock);
            WriteInt32(block, 20, dataStartBlock);
            WriteUInt32(block, 44, 1);
            WriteInt32(block, 48, BlockSize);
            WriteInt32(block, 52, BlockSize);
            WriteInt32(block, 56, 1);
            WriteInt32(block, 60, 0);
            WriteInt32(block, 72, -BlockSize);
            WriteInt32(block, 76, -BlockSize);
            WriteInt32(block, 80, 16);
            WriteInt32(block, 84, 16);
            WriteInt32(block, 88, 1);
            WriteInt32(block, 92, 8192);
            WriteInt32(block, 96, 0);
            WriteInt32(block, 100, 7);
            WriteInt32(block, 104, SuperBlockSize);
            WriteInt32(block, 116, IndirectBlockCapacity);
            WriteUInt32(block, 120, InodesPerBlock);
            WriteInt32(block, 128, 1);
            WriteInt32(block, 144, unchecked((int)now));
            WriteInt32(block, 148, 1);
            WriteInt32(block, 156, BlockSize);
            WriteInt32(block, 160, BlockSize);
            WriteUInt32(block, 184, (uint)inodeCount);
            WriteInt32(block, 188, imageBlocks);
            block[209] = FsIsClean;
            block[211] = FsFlagsUpdated;
            WriteInt32(block, 724, 0);
            WriteInt32(block, 856, 0);
            WriteInt32(block, 860, BlockSize);
            WriteInt64(block, 1000, SuperBlockOffset);
            WriteInt64(block, 1008, directoryCount);
            WriteInt64(block, 1016, freeBlocks);
            WriteInt64(block, 1024, freeInodes);
            WriteInt64(block, 1032, 0);
            WriteInt64(block, 1072, now);
            WriteInt64(block, 1080, imageBlocks);
            WriteInt64(block, 1088, dataBlocks);
            WriteInt64(block, 1096, dataStartBlock);
            WriteUInt32(block, 1112, 0);
            WriteUInt32(block, 1196, 16384);
            WriteUInt32(block, 1200, 64);
            WriteUInt32(block, 1312, 0);
            WriteInt32(block, 1316, 0);
            WriteInt32(block, 1320, 120);
            WriteInt32(block, 1324, 2);
            WriteUInt64(block, 1328, (ulong)maxFileSize);
            WriteInt64(block, 1336, BlockSize - 1);
            WriteInt64(block, 1344, BlockSize - 1);
            WriteUInt32(block, 1372, Ufs2Magic);

            return block;
        }

        private static byte[] CreateSummaryBlock(int directoryCount, int freeBlocks, int freeInodes)
        {
            var block = new byte[BlockSize];
            WriteInt32(block, 0, directoryCount);
            WriteInt32(block, 4, freeBlocks);
            WriteInt32(block, 8, freeInodes);
            WriteInt32(block, 12, 0);
            return block;
        }

        private static byte[] CreateCylinderGroup(
            int imageBlocks,
            int inodeCount,
            int dataStartBlock,
            int firstPayloadBlock,
            int nextBlock,
            int directoryCount,
            int freeBlocks,
            int freeInodes,
            IReadOnlyList<UfsNode> nodes,
            long now)
        {
            var block = new byte[BlockSize];
            var inodeMapOffset = 168;
            var freeMapOffset = inodeMapOffset + DivRoundUp(inodeCount, 8);
            var nextFreeOffset = freeMapOffset + DivRoundUp(imageBlocks, 8);
            if (nextFreeOffset > BlockSize)
                throw new InvalidOperationException("The image is too large for one UFS2 cylinder group.");

            WriteUInt32(block, 4, CylinderGroupMagic);
            WriteUInt32(block, 12, 0);
            WriteUInt32(block, 20, (uint)imageBlocks);
            WriteInt32(block, 24, directoryCount);
            WriteInt32(block, 28, freeBlocks);
            WriteInt32(block, 32, freeInodes);
            WriteInt32(block, 36, 0);
            WriteUInt32(block, 40, (uint)Math.Max(firstPayloadBlock, nextBlock - 1));
            WriteUInt32(block, 44, (uint)Math.Max(firstPayloadBlock, nextBlock - 1));
            WriteUInt32(block, 48, 0);
            WriteUInt32(block, 92, (uint)inodeMapOffset);
            WriteUInt32(block, 96, (uint)freeMapOffset);
            WriteUInt32(block, 100, (uint)nextFreeOffset);
            WriteUInt32(block, 116, (uint)inodeCount);
            WriteUInt32(block, 120, (uint)Math.Min(inodeCount, InodesPerBlock * 2));
            WriteInt64(block, 136, now);

            SetBit(block, inodeMapOffset, 0);
            SetBit(block, inodeMapOffset, 1);
            foreach (var node in nodes)
                SetBit(block, inodeMapOffset, node.Inode);

            var allocated = new HashSet<int>();
            for (var blockNumber = 0; blockNumber < firstPayloadBlock; blockNumber++)
                allocated.Add(blockNumber);

            for (var blockNumber = firstPayloadBlock; blockNumber < nextBlock; blockNumber++)
                allocated.Add(blockNumber);

            allocated.Add(dataStartBlock);

            for (var blockNumber = firstPayloadBlock; blockNumber < imageBlocks; blockNumber++)
            {
                if (!allocated.Contains(blockNumber))
                    SetBit(block, freeMapOffset, blockNumber);
            }

            return block;
        }

        private static async Task WriteInodeAsync(FileStream image, UfsNode node, long now, CancellationToken ct)
        {
            var inode = new byte[InodeSize];
            var mode = node.IsDirectory ? DirectoryMode : RegularMode;
            var linkCount = node.IsDirectory
                ? 2 + node.Children.Count(child => child.IsDirectory)
                : 1;
            var size = node.IsDirectory ? node.DirectoryContent.Length : node.FileLength;
            var allocatedBlocks = node.DataBlocks.Count + (node.IndirectBlock is null ? 0 : 1);
            var time = ToUnixTime(node.LastWriteTime, now);
            var accessTime = ToUnixTime(node.LastAccessTime, time);

            WriteUInt16(inode, 0, mode);
            WriteInt16(inode, 2, (short)linkCount);
            WriteUInt32(inode, 4, 0);
            WriteUInt32(inode, 8, 0);
            WriteUInt32(inode, 12, 0);
            WriteUInt64(inode, 16, (ulong)size);
            WriteUInt64(inode, 24, (ulong)allocatedBlocks * (BlockSize / SectorSize));
            WriteInt64(inode, 32, accessTime);
            WriteInt64(inode, 40, time);
            WriteInt64(inode, 48, now);
            WriteInt64(inode, 56, time);
            WriteInt32(inode, 80, node.Inode);

            for (var index = 0; index < Math.Min(DirectBlockCount, node.DataBlocks.Count); index++)
                WriteInt64(inode, 112 + index * sizeof(long), node.DataBlocks[index]);

            if (node.IndirectBlock is not null)
                WriteInt64(inode, 208, node.IndirectBlock.Value);

            var inodeOffset = (long)InodeStartBlock * BlockSize + (long)node.Inode * InodeSize;
            await WriteAtAsync(image, inodeOffset, inode, inode.Length, ct);
        }

        private static async Task WriteNodeDataAsync(FileStream image, UfsNode node, CancellationToken ct)
        {
            if (node.IsDirectory)
            {
                await WriteBytesAcrossBlocksAsync(image, node.DirectoryContent, node.DataBlocks, ct);
                return;
            }

            if (node.DataBlocks.Count == 0)
                return;

            await using var source = new FileStream(node.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, true);
            var buffer = new byte[BlockSize];

            foreach (var block in node.DataBlocks)
            {
                Array.Clear(buffer);
                var totalRead = 0;
                while (totalRead < buffer.Length)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), ct);
                    if (read == 0)
                        break;
                    totalRead += read;
                }

                await WriteAtAsync(image, (long)block * BlockSize, buffer, buffer.Length, ct);
            }

            if (node.IndirectBlock is not null)
            {
                var indirect = new byte[BlockSize];
                var indirectBlocks = node.DataBlocks.Skip(DirectBlockCount).ToList();
                for (var index = 0; index < indirectBlocks.Count; index++)
                    WriteInt64(indirect, index * sizeof(long), indirectBlocks[index]);

                await WriteAtAsync(image, (long)node.IndirectBlock.Value * BlockSize, indirect, indirect.Length, ct);
            }
        }

        private static async Task WriteBytesAcrossBlocksAsync(
            FileStream image,
            byte[] content,
            IReadOnlyList<int> blocks,
            CancellationToken ct)
        {
            var offset = 0;

            foreach (var block in blocks)
            {
                var buffer = new byte[BlockSize];
                var length = Math.Min(BlockSize, content.Length - offset);
                if (length > 0)
                    content.AsSpan(offset, length).CopyTo(buffer);

                await WriteAtAsync(image, (long)block * BlockSize, buffer, buffer.Length, ct);
                offset += length;
            }
        }

        private static async Task WriteAtAsync(
            FileStream stream,
            long offset,
            byte[] data,
            int length,
            CancellationToken ct)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            await stream.WriteAsync(data.AsMemory(0, length), ct);
        }

        private static long CalculateMaxFileSize()
        {
            var maxFileSize = (long)BlockSize * DirectBlockCount - 1;
            var sizePerBlock = (long)BlockSize;

            for (var index = 0; index < 3; index++)
            {
                sizePerBlock *= IndirectBlockCapacity;
                maxFileSize += sizePerBlock;
            }

            return maxFileSize;
        }

        private static long ToUnixTime(DateTime value, long fallback)
        {
            if (value <= DateTime.UnixEpoch)
                return fallback;

            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToUnixTimeSeconds();
        }

        private static int DivRoundUp(int value, int divisor)
        {
            return (value + divisor - 1) / divisor;
        }

        private static int AlignTo(int value, int boundary)
        {
            var remainder = value % boundary;
            return remainder == 0 ? value : value + boundary - remainder;
        }

        private static long AlignTo(long value, long boundary)
        {
            var remainder = value % boundary;
            return remainder == 0 ? value : value + boundary - remainder;
        }

        private static void SetBit(byte[] buffer, int mapOffset, int bit)
        {
            buffer[mapOffset + bit / 8] |= (byte)(1 << (bit % 8));
        }

        private static void WriteInt16(byte[] buffer, int offset, short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(offset, sizeof(short)), value);
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset, sizeof(ushort)), value);
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, sizeof(int)), value);
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset, sizeof(uint)), value);
        }

        private static void WriteInt64(byte[] buffer, int offset, long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(offset, sizeof(long)), value);
        }

        private static void WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset, sizeof(ulong)), value);
        }

        private sealed class UfsNode
        {
            public UfsNode(string name, string fullPath, bool isDirectory, UfsNode? parent)
            {
                Name = name;
                FullPath = fullPath;
                IsDirectory = isDirectory;
                Parent = parent;
            }

            public string Name { get; }
            public string FullPath { get; }
            public bool IsDirectory { get; }
            public UfsNode? Parent { get; }
            public List<UfsNode> Children { get; } = new();
            public int Inode { get; set; }
            public long FileLength { get; set; }
            public byte[] DirectoryContent { get; set; } = [];
            public List<int> DataBlocks { get; } = new();
            public int? IndirectBlock { get; set; }
            public DateTime LastWriteTime { get; set; } = DateTime.UtcNow;
            public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;

            public string RelativePath
            {
                get
                {
                    var parts = new Stack<string>();
                    var current = this;
                    while (current.Parent is not null)
                    {
                        parts.Push(current.Name);
                        current = current.Parent;
                    }

                    return parts.Count == 0 ? "." : string.Join("/", parts);
                }
            }

            public int GetDataBlockCount()
            {
                var length = IsDirectory ? DirectoryContent.LongLength : FileLength;
                if (length == 0)
                    return 0;

                var blockCount = AlignTo(length, BlockSize) / BlockSize;
                if (blockCount > int.MaxValue)
                    throw new InvalidOperationException($"File is too large: {RelativePath}");

                return (int)blockCount;
            }
        }

        private sealed record DirectoryEntry(string Name, int Inode, byte Type);
    }
}
