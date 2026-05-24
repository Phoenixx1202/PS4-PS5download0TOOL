using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace PS4_PS5download0TOOL.Services
{
    public enum ProcessingMode
    {
        Extract,
        Patch
    }

    public sealed class ProcessingOptions
    {
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public ProcessingMode Mode { get; set; }
        public string LanguageCode { get; set; } = "pt-BR";
    }

    public sealed class ProgressInfo
    {
        public double Percentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? LogMessage { get; set; }
    }

    public sealed class DatProcessingService
    {
        private const int BlockSize = 65536;
        private static readonly Regex FileEntryRegex = new(@"([rd])/([rd])\s+(\d+):\s+(.+)$", RegexOptions.Compiled);

        private static readonly Dictionary<string, Dictionary<string, string>> ServiceText = new()
        {
            ["pt-BR"] = new()
            {
                ["CheckingTools"] = "Verificando ferramentas SleuthKit...",
                ["ToolsReady"] = "Ferramentas encontradas em: {0}",
                ["ReadingImage"] = "Lendo imagem UFS...",
                ["FilesFound"] = "{0} arquivo(s) encontrado(s).",
                ["NoFilesFound"] = "Nenhum arquivo foi encontrado na imagem.",
                ["CleaningFolder"] = "Preparando pasta extracted...",
                ["Extracting"] = "Extraindo {0}/{1}: {2}",
                ["ExtractDone"] = "Extração concluída: {0}",
                ["WritingSummary"] = "Resumo salvo: {0}",
                ["CopyingImage"] = "Copiando imagem base...",
                ["CopyProgress"] = "Cópia: {0} / {1}",
                ["PreparingRebuild"] = "Preparando rebuild completo...",
                ["CalculatingImage"] = "Calculando tamanho da nova imagem...",
                ["Rebuilding"] = "Recriando UFS2 em C#...",
                ["RebuildSize"] = "Tamanho da nova imagem: {0}",
                ["PreserveOriginalSize"] = "Preservando tamanho original detectado: {0}",
                ["CompactFallbackSize"] = "Tamanho original não detectado; usando tamanho mínimo seguro: {0}",
                ["RebuildDone"] = "Novo .dat gerado: {0}",
                ["ReadingStructure"] = "Mapeando arquivos da imagem...",
                ["Patching"] = "Aplicando patch {0}/{1}: {2}",
                ["SkipMissing"] = "Ignorado, não encontrado na imagem: {0}",
                ["SkipBigger"] = "Ignorado, arquivo maior que o original: {0} ({1} > {2})",
                ["Patched"] = "OK: {0}",
                ["PatchDone"] = "Patch concluído: {0}",
                ["ToolMissing"] = "SleuthKit não encontrado. Esperado: fls.exe, icat.exe e istat.exe.",
                ["ToolFailed"] = "{0} falhou com código {1}: {2}",
                ["PatchSourceMissing"] = "Pasta extracted não encontrada: {0}",
                ["InvalidOutputPath"] = "Caminho inválido dentro da imagem: {0}",
                ["BlockInfoMissing"] = "Não foi possível ler tamanho/blocos de {0}.",
                ["BlockSpaceMissing"] = "Blocos insuficientes para {0}.",
                ["Done"] = "Concluído.",
            },
            ["en"] = new()
            {
                ["CheckingTools"] = "Checking SleuthKit tools...",
                ["ToolsReady"] = "Tools found at: {0}",
                ["ReadingImage"] = "Reading UFS image...",
                ["FilesFound"] = "{0} file(s) found.",
                ["NoFilesFound"] = "No files were found in the image.",
                ["CleaningFolder"] = "Preparing extracted folder...",
                ["Extracting"] = "Extracting {0}/{1}: {2}",
                ["ExtractDone"] = "Extraction complete: {0}",
                ["WritingSummary"] = "Summary saved: {0}",
                ["CopyingImage"] = "Copying base image...",
                ["CopyProgress"] = "Copy: {0} / {1}",
                ["PreparingRebuild"] = "Preparing full rebuild...",
                ["CalculatingImage"] = "Calculating new image size...",
                ["Rebuilding"] = "Rebuilding UFS2 in C#...",
                ["RebuildSize"] = "New image size: {0}",
                ["PreserveOriginalSize"] = "Preserving detected original size: {0}",
                ["CompactFallbackSize"] = "Original size not detected; using safe minimum size: {0}",
                ["RebuildDone"] = "New .dat created: {0}",
                ["ReadingStructure"] = "Mapping image files...",
                ["Patching"] = "Patching {0}/{1}: {2}",
                ["SkipMissing"] = "Skipped, not found in image: {0}",
                ["SkipBigger"] = "Skipped, file is bigger than original: {0} ({1} > {2})",
                ["Patched"] = "OK: {0}",
                ["PatchDone"] = "Patch complete: {0}",
                ["ToolMissing"] = "SleuthKit was not found. Expected: fls.exe, icat.exe and istat.exe.",
                ["ToolFailed"] = "{0} failed with code {1}: {2}",
                ["PatchSourceMissing"] = "Extracted folder not found: {0}",
                ["InvalidOutputPath"] = "Invalid image path: {0}",
                ["BlockInfoMissing"] = "Could not read size/blocks for {0}.",
                ["BlockSpaceMissing"] = "Not enough blocks for {0}.",
                ["Done"] = "Done.",
            },
            ["es"] = new()
            {
                ["CheckingTools"] = "Verificando herramientas SleuthKit...",
                ["ToolsReady"] = "Herramientas encontradas en: {0}",
                ["ReadingImage"] = "Leyendo imagen UFS...",
                ["FilesFound"] = "{0} archivo(s) encontrado(s).",
                ["NoFilesFound"] = "No se encontraron archivos en la imagen.",
                ["CleaningFolder"] = "Preparando carpeta extracted...",
                ["Extracting"] = "Extrayendo {0}/{1}: {2}",
                ["ExtractDone"] = "Extracción completada: {0}",
                ["WritingSummary"] = "Resumen guardado: {0}",
                ["CopyingImage"] = "Copiando imagen base...",
                ["CopyProgress"] = "Copia: {0} / {1}",
                ["PreparingRebuild"] = "Preparando reconstrucción completa...",
                ["CalculatingImage"] = "Calculando tamaño de la nueva imagen...",
                ["Rebuilding"] = "Reconstruyendo UFS2 en C#...",
                ["RebuildSize"] = "Tamaño de la nueva imagen: {0}",
                ["PreserveOriginalSize"] = "Preservando tamaño original detectado: {0}",
                ["CompactFallbackSize"] = "Tamaño original no detectado; usando tamaño mínimo seguro: {0}",
                ["RebuildDone"] = "Nuevo .dat creado: {0}",
                ["ReadingStructure"] = "Mapeando archivos de la imagen...",
                ["Patching"] = "Aplicando patch {0}/{1}: {2}",
                ["SkipMissing"] = "Ignorado, no encontrado en la imagen: {0}",
                ["SkipBigger"] = "Ignorado, archivo mayor que el original: {0} ({1} > {2})",
                ["Patched"] = "OK: {0}",
                ["PatchDone"] = "Patch completado: {0}",
                ["ToolMissing"] = "SleuthKit no fue encontrado. Se esperaba: fls.exe, icat.exe e istat.exe.",
                ["ToolFailed"] = "{0} falló con código {1}: {2}",
                ["PatchSourceMissing"] = "Carpeta extracted no encontrada: {0}",
                ["InvalidOutputPath"] = "Ruta inválida en la imagen: {0}",
                ["BlockInfoMissing"] = "No se pudo leer tamaño/bloques de {0}.",
                ["BlockSpaceMissing"] = "Bloques insuficientes para {0}.",
                ["Done"] = "Completado.",
            },
            ["ar"] = new()
            {
                ["CheckingTools"] = "فحص أدوات SleuthKit...",
                ["ToolsReady"] = "تم العثور على الأدوات في: {0}",
                ["ReadingImage"] = "قراءة صورة UFS...",
                ["FilesFound"] = "تم العثور على {0} ملف.",
                ["NoFilesFound"] = "لم يتم العثور على ملفات داخل الصورة.",
                ["CleaningFolder"] = "تحضير مجلد extracted...",
                ["Extracting"] = "استخراج {0}/{1}: {2}",
                ["ExtractDone"] = "اكتمل الاستخراج: {0}",
                ["WritingSummary"] = "تم حفظ الملخص: {0}",
                ["CopyingImage"] = "نسخ الصورة الأساسية...",
                ["CopyProgress"] = "نسخ: {0} / {1}",
                ["PreparingRebuild"] = "تحضير إعادة البناء الكاملة...",
                ["CalculatingImage"] = "حساب حجم الصورة الجديدة...",
                ["Rebuilding"] = "إعادة بناء UFS2 باستخدام C#...",
                ["RebuildSize"] = "حجم الصورة الجديدة: {0}",
                ["PreserveOriginalSize"] = "الحفاظ على الحجم الأصلي المكتشف: {0}",
                ["CompactFallbackSize"] = "لم يتم اكتشاف الحجم الأصلي؛ استخدام حجم آمن: {0}",
                ["RebuildDone"] = "تم إنشاء ملف .dat جديد: {0}",
                ["ReadingStructure"] = "تخطيط ملفات الصورة...",
                ["Patching"] = "تطبيق Patch {0}/{1}: {2}",
                ["SkipMissing"] = "تم التجاهل، غير موجود في الصورة: {0}",
                ["SkipBigger"] = "تم التجاهل، الملف أكبر من الأصلي: {0} ({1} > {2})",
                ["Patched"] = "تم: {0}",
                ["PatchDone"] = "اكتمل Patch: {0}",
                ["ToolMissing"] = "لم يتم العثور على SleuthKit. المطلوب: fls.exe و icat.exe و istat.exe.",
                ["ToolFailed"] = "فشل {0} برمز {1}: {2}",
                ["PatchSourceMissing"] = "مجلد extracted غير موجود: {0}",
                ["InvalidOutputPath"] = "مسار غير صالح داخل الصورة: {0}",
                ["BlockInfoMissing"] = "تعذرت قراءة الحجم/الكتل لـ {0}.",
                ["BlockSpaceMissing"] = "الكتل غير كافية لـ {0}.",
                ["Done"] = "تم.",
            },
        };

        public Task ProcessAsync(
            ProcessingOptions options,
            IProgress<ProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            return options.Mode switch
            {
                ProcessingMode.Extract => ExtractAsync(options, progress, cancellationToken),
                ProcessingMode.Patch => PatchAsync(options, progress, cancellationToken),
                _ => throw new InvalidOperationException("Unsupported processing mode.")
            };
        }

        private async Task ExtractAsync(
            ProcessingOptions options,
            IProgress<ProgressInfo> progress,
            CancellationToken ct)
        {
            progress.Report(new ProgressInfo { Percentage = 2, Status = S(options, "CheckingTools") });
            var tools = ResolveToolPaths(options);
            progress.Report(new ProgressInfo
            {
                Percentage = 4,
                Status = S(options, "CheckingTools"),
                LogMessage = string.Format(S(options, "ToolsReady"), tools.BinDirectory)
            });

            progress.Report(new ProgressInfo { Percentage = 8, Status = S(options, "ReadingImage") });
            var flsOutput = await RunTextToolAsync(tools.Fls, [ "-rp", options.InputPath ], options, ct);
            var entries = ParseFlsEntries(flsOutput);
            var fileEntries = entries.Where(entry => !entry.IsDirectory).ToList();
            var directoryEntries = entries.Where(entry => entry.IsDirectory).ToList();

            if (fileEntries.Count == 0)
                throw new InvalidOperationException(S(options, "NoFilesFound"));

            progress.Report(new ProgressInfo
            {
                Percentage = 12,
                Status = S(options, "ReadingImage"),
                LogMessage = string.Format(S(options, "FilesFound"), fileEntries.Count)
            });

            var extractDir = Path.Combine(options.OutputPath, "extracted");
            progress.Report(new ProgressInfo { Percentage = 15, Status = S(options, "CleaningFolder") });
            PrepareCleanDirectory(extractDir, options.OutputPath);

            foreach (var directoryEntry in directoryEntries.OrderBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                var outputPath = GetSafeOutputPath(extractDir, directoryEntry.RelativePath, options);
                Directory.CreateDirectory(outputPath);
            }

            for (var index = 0; index < fileEntries.Count; index++)
            {
                ct.ThrowIfCancellationRequested();

                var entry = fileEntries[index];
                var outputPath = GetSafeOutputPath(extractDir, entry.RelativePath, options);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                var status = string.Format(S(options, "Extracting"), index + 1, fileEntries.Count, entry.RelativePath);
                var percent = 15 + ((index + 1) * 80.0 / fileEntries.Count);
                progress.Report(new ProgressInfo
                {
                    Percentage = percent,
                    Status = status,
                    LogMessage = status
                });

                await RunIcatToFileAsync(tools.Icat, options.InputPath, entry.Inode, outputPath, options, ct);
            }

            var summaryPath = Path.Combine(extractDir, "extract_summary.txt");
            await WriteExtractSummaryAsync(summaryPath, options, entries, ct);
            progress.Report(new ProgressInfo
            {
                Percentage = 98,
                Status = S(options, "Done"),
                LogMessage = string.Format(S(options, "WritingSummary"), summaryPath)
            });

            progress.Report(new ProgressInfo
            {
                Percentage = 100,
                Status = S(options, "Done"),
                LogMessage = string.Format(S(options, "ExtractDone"), extractDir)
            });
        }

        private async Task PatchAsync(
            ProcessingOptions options,
            IProgress<ProgressInfo> progress,
            CancellationToken ct)
        {
            var extractDir = Path.Combine(options.OutputPath, "extracted");
            if (!Directory.Exists(extractDir))
                throw new DirectoryNotFoundException(string.Format(S(options, "PatchSourceMissing"), extractDir));

            progress.Report(new ProgressInfo { Percentage = 3, Status = S(options, "PreparingRebuild") });

            var outputPath = Path.Combine(options.OutputPath, "rebuilt_download0.dat");
            var stagingDir = Path.Combine(options.OutputPath, ".rebuild_staging");

            try
            {
                progress.Report(new ProgressInfo
                {
                    Percentage = 12,
                    Status = S(options, "PreparingRebuild"),
                    LogMessage = S(options, "PreparingRebuild")
                });

                PrepareCleanDirectory(stagingDir, options.OutputPath);
                CopyDirectoryForRebuild(extractDir, stagingDir, ct);

                progress.Report(new ProgressInfo
                {
                    Percentage = 32,
                    Status = S(options, "CalculatingImage"),
                    LogMessage = S(options, "CalculatingImage")
                });

                var minimumImageSize = CalculateMinimumRebuildImageSize(stagingDir);
                var originalImageSize = TryResolveOriginalImageSize(extractDir, options);
                var imageSize = originalImageSize is not null && originalImageSize.Value >= minimumImageSize
                    ? AlignTo(originalImageSize.Value, BlockSize)
                    : minimumImageSize;
                var sizeMessageKey = originalImageSize is not null && originalImageSize.Value >= minimumImageSize
                    ? "PreserveOriginalSize"
                    : "CompactFallbackSize";

                progress.Report(new ProgressInfo
                {
                    Percentage = 40,
                    Status = S(options, "Rebuilding"),
                    LogMessage = string.Format(S(options, sizeMessageKey), $"{FormatFileSize(imageSize)} ({imageSize:N0} bytes)")
                });

                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                await Ufs2ImageBuilder.BuildAsync(stagingDir, outputPath, imageSize, ct);

                progress.Report(new ProgressInfo
                {
                    Percentage = 100,
                    Status = S(options, "Done"),
                    LogMessage = string.Format(S(options, "RebuildDone"), outputPath)
                });
            }
            finally
            {
                DeleteDirectoryIfInside(stagingDir, options.OutputPath);
            }
        }

        private static void CopyDirectoryForRebuild(string sourceDirectory, string targetDirectory, CancellationToken ct)
        {
            foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(sourceDirectory, directory);
                Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
            }

            foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                if (Path.GetFileName(file).Equals("extract_summary.txt", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relativePath = Path.GetRelativePath(sourceDirectory, file);
                var destination = Path.Combine(targetDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, overwrite: true);
            }
        }

        private static long CalculateMinimumRebuildImageSize(string sourceDirectory)
        {
            const long minimumImageSize = 64L * 1024 * 1024;
            const long rebuildReserve = 64L * 1024 * 1024;

            var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            var directories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories);
            var contentBytes = files.Sum(file => new FileInfo(file).Length);
            var directoryReserve = Math.Max(1, directories.Length + 1) * (long)BlockSize;
            var fileReserve = Math.Max(1, files.Length) * (long)BlockSize;
            var requestedSize = contentBytes + directoryReserve + fileReserve + rebuildReserve;

            return AlignTo(Math.Max(minimumImageSize, requestedSize), BlockSize);
        }

        private static long? TryResolveOriginalImageSize(string extractDir, ProcessingOptions options)
        {
            if (File.Exists(options.InputPath))
                return new FileInfo(options.InputPath).Length;

            var summaryPath = Path.Combine(extractDir, "extract_summary.txt");
            if (File.Exists(summaryPath))
            {
                var sourcePath = ReadSummaryValue(summaryPath, "Source");
                if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
                    return new FileInfo(sourcePath).Length;

                var sourceSize = ReadSummaryValue(summaryPath, "SourceSize")
                    ?? ReadSummaryValue(summaryPath, "Source Size");
                if (!string.IsNullOrWhiteSpace(sourceSize))
                {
                    var digitsOnly = Regex.Replace(sourceSize, @"[^\d]", string.Empty);
                    if (long.TryParse(digitsOnly, out var parsedSize) && parsedSize > 0)
                        return parsedSize;
                }
            }

            foreach (var candidate in GetOriginalImageCandidates(options.OutputPath))
            {
                if (File.Exists(candidate))
                    return new FileInfo(candidate).Length;
            }

            return null;
        }

        private static string? ReadSummaryValue(string summaryPath, string key)
        {
            foreach (var line in File.ReadLines(summaryPath))
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex < 0)
                    continue;

                var lineKey = line[..separatorIndex].Trim();
                if (lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return line[(separatorIndex + 1)..].Trim();
            }

            return null;
        }

        private static IEnumerable<string> GetOriginalImageCandidates(string outputPath)
        {
            var outputDirectory = new DirectoryInfo(outputPath);
            yield return Path.Combine(outputDirectory.FullName, "download0.dat");

            if (outputDirectory.Parent is not null)
            {
                yield return Path.Combine(outputDirectory.Parent.FullName, "download0.dat");
                yield return Path.Combine(outputDirectory.Parent.FullName, "original_download0.dat");
            }
        }

        private static long AlignTo(long value, long boundary)
        {
            var remainder = value % boundary;
            return remainder == 0 ? value : value + boundary - remainder;
        }

        private static async Task CopyImageAsync(
            string sourcePath,
            string destinationPath,
            IProgress<ProgressInfo> progress,
            ProcessingOptions options,
            CancellationToken ct)
        {
            progress.Report(new ProgressInfo
            {
                Percentage = 7,
                Status = S(options, "CopyingImage"),
                LogMessage = S(options, "CopyingImage")
            });

            await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
            await using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, true);
            var buffer = new byte[1024 * 1024];
            long copied = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), ct);
                copied += read;
                progress.Report(new ProgressInfo
                {
                    Percentage = 7 + copied * 22.0 / source.Length,
                    Status = string.Format(S(options, "CopyProgress"), FormatFileSize(copied), FormatFileSize(source.Length))
                });
            }
        }

        private static async Task<BlockInfo> GetBlockInfoAsync(
            string istatPath,
            string imagePath,
            string inode,
            ProcessingOptions options,
            CancellationToken ct)
        {
            var output = await RunTextToolAsync(istatPath, [ imagePath, inode ], options, ct);
            long size = 0;
            var blocks = new List<long>();

            foreach (var rawLine in output.SplitLines())
            {
                var line = rawLine.Trim();

                if (line.Contains("Size:", StringComparison.OrdinalIgnoreCase))
                {
                    var numbers = Regex.Matches(line, @"\d+");
                    if (numbers.Count > 0 && long.TryParse(numbers[^1].Value, out var parsedSize))
                        size = parsedSize;
                }

                if (Regex.IsMatch(line, @"^\d+(\s+\d+)*$"))
                {
                    foreach (Match match in Regex.Matches(line, @"\d+"))
                    {
                        if (long.TryParse(match.Value, out var block))
                            blocks.Add(block);
                    }
                }
            }

            return new BlockInfo(size, blocks);
        }

        private static async Task PatchFileIntoImageAsync(
            string imagePath,
            string localPath,
            BlockInfo blockInfo,
            string relativePath,
            ProcessingOptions options,
            CancellationToken ct)
        {
            await using var image = new FileStream(imagePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, BlockSize, true);
            await using var localFile = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, true);

            var buffer = new byte[BlockSize];
            var zeroBuffer = new byte[BlockSize];
            long originalRemaining = blockInfo.Size;
            long dataRemaining = localFile.Length;

            foreach (var block in blockInfo.Blocks)
            {
                ct.ThrowIfCancellationRequested();

                if (originalRemaining <= 0)
                    break;

                var chunkSize = (int)Math.Min(BlockSize, originalRemaining);
                var dataToWrite = (int)Math.Min(chunkSize, dataRemaining);

                image.Seek(block * BlockSize, SeekOrigin.Begin);

                if (dataToWrite > 0)
                {
                    var read = await ReadExactlyAsync(localFile, buffer, dataToWrite, ct);
                    await image.WriteAsync(buffer.AsMemory(0, read), ct);
                    dataRemaining -= read;
                }

                var zerosToWrite = chunkSize - dataToWrite;
                if (zerosToWrite > 0)
                    await image.WriteAsync(zeroBuffer.AsMemory(0, zerosToWrite), ct);

                originalRemaining -= chunkSize;
            }

            if (originalRemaining > 0)
                throw new InvalidOperationException(string.Format(S(options, "BlockSpaceMissing"), relativePath));
        }

        private static async Task<int> ReadExactlyAsync(
            FileStream stream,
            byte[] buffer,
            int count,
            CancellationToken ct)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), ct);
                if (read == 0)
                    break;
                totalRead += read;
            }

            return totalRead;
        }

        private static async Task<string> RunTextToolAsync(
            string executablePath,
            IReadOnlyList<string> arguments,
            ProcessingOptions options,
            CancellationToken ct)
        {
            using var process = StartTool(executablePath, arguments, redirectOutput: true);
            using var registration = ct.Register(() => KillProcess(process));

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(ct);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException(string.Format(S(options, "ToolFailed"), Path.GetFileName(executablePath), process.ExitCode, stderr.Trim()));

            return stdout;
        }

        private static async Task RunIcatToFileAsync(
            string icatPath,
            string imagePath,
            string inode,
            string outputPath,
            ProcessingOptions options,
            CancellationToken ct)
        {
            using var process = StartTool(icatPath, [ imagePath, inode ], redirectOutput: true);
            using var registration = ct.Register(() => KillProcess(process));
            var stderrTask = process.StandardError.ReadToEndAsync();

            await using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, BlockSize, true))
                await process.StandardOutput.BaseStream.CopyToAsync(output, ct);

            await process.WaitForExitAsync(ct);
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException(string.Format(S(options, "ToolFailed"), Path.GetFileName(icatPath), process.ExitCode, stderr.Trim()));
        }

        private static Process StartTool(
            string executablePath,
            IReadOnlyList<string> arguments,
            bool redirectOutput)
        {
            var executableDirectory = Path.GetDirectoryName(executablePath);
            var startInfo = new ProcessStartInfo(executablePath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = string.IsNullOrWhiteSpace(executableDirectory) ? AppContext.BaseDirectory : executableDirectory
            };

            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            return Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Could not start {Path.GetFileName(executablePath)}.");
        }

        private static ToolPaths ResolveToolPaths(ProcessingOptions options)
        {
            foreach (var directory in GetToolSearchDirectories())
            {
                var fls = Path.Combine(directory, "fls.exe");
                var icat = Path.Combine(directory, "icat.exe");
                var istat = Path.Combine(directory, "istat.exe");

                if (File.Exists(fls) && File.Exists(icat) && File.Exists(istat))
                    return new ToolPaths(directory, fls, icat, istat);
            }

            throw new FileNotFoundException(S(options, "ToolMissing"));
        }

        private static IEnumerable<string> GetToolSearchDirectories()
        {
            foreach (var root in EnumerateSelfAndParents(AppContext.BaseDirectory))
                yield return Path.Combine(root, "sleuthkit", "bin");

            foreach (var root in EnumerateSelfAndParents(Environment.CurrentDirectory))
                yield return Path.Combine(root, "sleuthkit", "bin");

            yield return Path.Combine(@"C:\Projetos\PS4-PS5download0TOOL", "sleuthkit", "bin");
            yield return Path.Combine(@"C:\Projetos\PS5_download0_Windows_Tool", "sleuthkit", "bin");
        }

        private static IEnumerable<string> EnumerateSelfAndParents(string path)
        {
            var directory = Directory.Exists(path)
                ? new DirectoryInfo(path)
                : new DirectoryInfo(Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);

            while (directory is not null)
            {
                yield return directory.FullName;
                directory = directory.Parent;
            }
        }

        private static List<FileEntry> ParseFlsEntries(string flsOutput)
        {
            var entries = new List<FileEntry>();

            foreach (var line in flsOutput.SplitLines())
            {
                if (!line.Contains("r/r", StringComparison.Ordinal) && !line.Contains("d/d", StringComparison.Ordinal))
                    continue;

                var match = FileEntryRegex.Match(line);
                if (!match.Success)
                    continue;

                var relativePath = match.Groups[4].Value.Trim().TrimStart('/', '\\');
                if (relativePath.StartsWith("$OrphanFiles", StringComparison.OrdinalIgnoreCase))
                    continue;

                entries.Add(new FileEntry(match.Groups[3].Value, NormalizeImagePath(relativePath), match.Groups[1].Value == "d"));
            }

            return entries;
        }

        private static string GetSafeOutputPath(string rootDirectory, string relativePath, ProcessingOptions options)
        {
            var pathParts = relativePath
                .Split([ '/', '\\' ], StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            if (pathParts.Any(part => part is "." or ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                throw new InvalidOperationException(string.Format(S(options, "InvalidOutputPath"), relativePath));

            var fullRoot = Path.GetFullPath(rootDirectory);
            var combined = Path.GetFullPath(Path.Combine([ fullRoot, .. pathParts ]));
            var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar)
                ? fullRoot
                : fullRoot + Path.DirectorySeparatorChar;

            if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(string.Format(S(options, "InvalidOutputPath"), relativePath));

            return combined;
        }

        private static void PrepareCleanDirectory(string targetDirectory, string expectedParent)
        {
            var fullTarget = Path.GetFullPath(targetDirectory);
            var fullParent = Path.GetFullPath(expectedParent);
            var parentWithSeparator = fullParent.EndsWith(Path.DirectorySeparatorChar)
                ? fullParent
                : fullParent + Path.DirectorySeparatorChar;

            if (!fullTarget.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to prepare a folder outside the selected output directory.");

            if (Directory.Exists(fullTarget))
                Directory.Delete(fullTarget, recursive: true);

            Directory.CreateDirectory(fullTarget);
        }

        private static void DeleteDirectoryIfInside(string targetDirectory, string expectedParent)
        {
            var fullTarget = Path.GetFullPath(targetDirectory);
            var fullParent = Path.GetFullPath(expectedParent);
            var parentWithSeparator = fullParent.EndsWith(Path.DirectorySeparatorChar)
                ? fullParent
                : fullParent + Path.DirectorySeparatorChar;

            if (fullTarget.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullTarget))
                Directory.Delete(fullTarget, recursive: true);
        }

        private static async Task WriteExtractSummaryAsync(
            string summaryPath,
            ProcessingOptions options,
            IReadOnlyList<FileEntry> entries,
            CancellationToken ct)
        {
            var builder = new StringBuilder();
            builder.AppendLine("PS4/PS5 download0.dat Tool");
            builder.AppendLine($"Generated : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Source    : {options.InputPath}");
            if (File.Exists(options.InputPath))
                builder.AppendLine($"SourceSize: {new FileInfo(options.InputPath).Length}");
            builder.AppendLine($"Output    : {Path.GetDirectoryName(summaryPath)}");
            builder.AppendLine($"Files     : {entries.Count}");
            builder.AppendLine();
            builder.AppendLine("Files:");

            foreach (var entry in entries)
            {
                var kind = entry.IsDirectory ? "dir " : "file";
                builder.AppendLine($"- {kind}: {entry.RelativePath} [{entry.Inode}]");
            }

            await File.WriteAllTextAsync(summaryPath, builder.ToString(), Encoding.UTF8, ct);
        }

        private static string NormalizeImagePath(string path)
        {
            return path.Trim().TrimStart('/', '\\').Replace('\\', '/');
        }

        private static void KillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
            }
        }

        private static string S(ProcessingOptions options, string key)
        {
            if (ServiceText.TryGetValue(options.LanguageCode, out var language) && language.TryGetValue(key, out var value))
                return value;

            return ServiceText["en"].TryGetValue(key, out var fallback) ? fallback : key;
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        private sealed record ToolPaths(string BinDirectory, string Fls, string Icat, string Istat);
        private sealed record FileEntry(string Inode, string RelativePath, bool IsDirectory);
        private sealed record BlockInfo(long Size, List<long> Blocks);
    }

    internal static class StringSplitExtensions
    {
        public static IEnumerable<string> SplitLines(this string value)
        {
            using var reader = new StringReader(value);
            while (reader.ReadLine() is { } line)
                yield return line;
        }
    }
}
