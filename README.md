# DQ8 chr to glTF Converter
![](preview.jpg)

Convert Dragon Quest VIII (PS2) character models to modern 3D format.
A command-line tool for converting .CHR character model files from Dragon Quest VIII: Journey of the Cursed King (PlayStation 2) into glTF/GLB 3D formats for use in modern 3D applications, game engines, and viewers.


## Features

* Convert DQ8 **`.chr`** files to binary **`.glb`** (default) or text-based **`.glTF`** format.
* Convert **`.tm2`** textures to **`.png`** format.
* Read **`.mot`** motion files.
* Read **`info.cfg`** files to capture parameters of animation clips
* Batch processing of entire directories of character models.
* Raw asset extraction mode for research/modding purposes.
* Preserves original geometry, textures, and skeletal structure.

## Options

| Option | Description |
|--------|-------------|
| `-e`   | Extract only - unpack `.chr` without conversion |
| `-t`   | Output as `.glTF` (text) instead of `.glb` (binary) |
| `-b`   | Batch mode - process all `.chr` files in directory |

## Usage

### Basic Syntax

```
dq8chr2glb.exe <input_file> <output_dir> [options]
dq8chr2glb.exe <input_dir> -b  # Batch mode
dq8chr2glb.exe # No args for get help
```

### Examples

```
dq8chr2glb.exe "C:\Games\DQ8\@DATA.DAT\chara\ap002.chr" "C:\Exports"
dq8chr2glb.exe "C:\Games\DQ8\@DATA.DAT\chara" -b
dq8chr2glb.exe "C:\Games\DQ8\@DATA.DAT\chara\ap002.chr" "C:\Exports" -e
dq8chr2glb.exe "C:\Games\DQ8\@DATA.DAT\chara" -b -e
```
