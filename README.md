# PS4-PS5download0TOOL

Windows tool for extracting, editing, and rebuilding `download0.dat` UFS2 images used in PS4 and PS5 workflows.

## What It Does

PS4-PS5download0TOOL provides a graphical workflow around `download0.dat` files:

<img width="1915" height="1027" alt="image" src="https://github.com/user-attachments/assets/19ea436c-3c57-43b2-ba9b-a3483eab50a8" />

1. Import an original `download0.dat`.
2. Extract its UFS2 file tree into a work folder.
3. Edit the extracted files freely.
4. Rebuild a new `.dat` image from the edited folder.

The default extraction output folder is:

```text
dat_output/extracted
```

The rebuilt file is created as:

```text
dat_output/rebuilt_download0.dat
```

## Main Features

- Graphical interface built with .NET MAUI.
- PS4 and PS5 oriented `download0.dat` workflow.
- File import button for the source `.dat`.
- Folder picker for the work/output directory.
- Progress bar and operation log.
- Multilingual UI:
  - Portuguese Brazil
  - English
  - Spanish
  - Arabic
- Custom in-app alerts.
- UFS2 extraction through bundled SleuthKit Windows tools.
- UFS2 rebuild implemented in C#.
- Rebuild supports edited files that are larger or smaller than the originals.
- When possible, rebuild preserves the original `.dat` size for better compatibility.

## How The Process Works

### Extraction

The app uses bundled SleuthKit binaries:

- `fls.exe` lists the internal UFS2 file tree.
- `icat.exe` extracts each file by inode.

During extraction, the tool creates:

```text
dat_output/extracted
```

It also writes:

```text
dat_output/extracted/extract_summary.txt
```

The summary stores source metadata such as original path and size. The rebuild step uses this information to preserve the original image size when possible.

### Editing

After extraction, edit files inside:

```text
dat_output/extracted
```

You can edit JavaScript files, HTML files, binaries, or any other extracted file. Files may become larger or smaller than the original versions.

### Rebuild

The rebuild step does not require importing the original `.dat` again. Select either:

```text
dat_output
```

or:

```text
dat_output/extracted
```

The app stages the edited files, excludes `extract_summary.txt`, and writes a fresh UFS2 image using the managed C# builder.

The builder creates:

- UFS2 superblock and backup superblock.
- Cylinder group metadata.
- Inode table.
- Directory records.
- Direct file blocks.
- Single-indirect file blocks for larger files.

If the original size is known and large enough, the rebuilt file keeps that size. If the edited contents require more space, or if the original size is not known, the tool uses a safe rebuilt image size based on the current folder contents.

## Requirements

- Windows 10 or newer.
- .NET 8 SDK with the .NET MAUI workload installed.

The project bundles SleuthKit Windows binaries under:

```text
sleuthkit/
```

No WSL or Linux toolchain is required.

## Build

From the project folder:

```powershell
dotnet restore PS4-PS5download0TOOL.sln
dotnet build PS4-PS5download0TOOL.sln -f net8.0-windows10.0.19041.0
```

The Windows debug output is generated under:

```text
bin/Debug/net8.0-windows10.0.19041.0/win10-x64/
```

The main executable is:

```text
PS4-PS5download0TOOL.exe
```

## Run From Source

```powershell
dotnet build PS4-PS5download0TOOL.sln -f net8.0-windows10.0.19041.0
.\bin\Debug\net8.0-windows10.0.19041.0\win10-x64\PS4-PS5download0TOOL.exe
```

## Typical Usage

1. Open the app.
2. Click **Import** and select `download0.dat`.
3. Choose an output folder or keep the automatic `dat_output` folder.
4. Click **Extract**.
5. Edit files inside `dat_output/extracted`.
6. Select `dat_output` or `dat_output/extracted`.
7. Click **Rebuild .dat**.
8. Use `dat_output/rebuilt_download0.dat` as the rebuilt image.

## Notes And Limitations

- Rebuild currently creates a UFS2 image with 64 KB blocks and 64 KB fragments.
- Single-indirect blocks are supported for files larger than the direct block range.
- Compatibility may depend on the exact environment consuming the `.dat`.
- Keeping the original image size is preferred for compatibility, and the app attempts to do that automatically.
- The physical source folder may still have an older local directory name, but the project, solution, namespace, and assembly are named PS4-PS5download0TOOL.

## Credits

Credits to the creator of the original PS5download0TOOL concept and workflow:

[Master-s](https://github.com/Master-s)
