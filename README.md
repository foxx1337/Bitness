# Bitness - Windows PE Architecture Analyzer

A simple .NET 9 utility for identifying the platform architecture of Windows executable and DLL files.

## Overview

Bitness determines whether a Windows PE file is compiled for:
- **32-bit (x86)**
- **64-bit (x64)** 
- **ARM64**
- **ARM64EC**
- **ARM64X (Hybrid)**

## Components

The project consists of three components sharing a common core library:

- **Bitness.Core.dll** - Class library containing PE header parsing logic
- **Bitness-CLI.exe** - Command-line interface
- **Bitness.exe** - WPF GUI application with drag-and-drop support

## Installation

### Prerequisites
- .NET 9 Runtime (for running the applications)
- .NET 9 SDK (for building from source)

### Building from Source
```powershell
git clone <repository-url>
cd Bitness
dotnet build
```

The built executables will be in:
- `Bitness.CLI\bin\Debug\net9.0\Bitness-CLI.exe`
- `Bitness.GUI\bin\Debug\net9.0-windows\Bitness.exe`

For release builds:
```powershell
dotnet build -c Release
```

## Usage

### Command Line Interface

```powershell
# Analyze a file
Bitness-CLI.exe "C:\Program Files\MyApp\app.exe"

# Show help
Bitness-CLI.exe --help
```

**Example Output:**
```
File: C:\Program Files\MyApp\app.exe
Architecture: 64-bit (x64)
```

### GUI Application

**Launch GUI:**
```powershell
Bitness.exe
```

**Analyze file immediately:**
```powershell
Bitness.exe "C:\Path\To\File.dll"
```

**GUI Features:**
- Drag and drop files onto the window
- Click to browse for files
- Command-line file loading on startup
- Clear error messages for invalid files

## API Usage

The core library can be used in your own applications:

```csharp
using Bitness.Core;

// Get architecture string
string arch = BitnessChecker.GetMachineTypeString(@"C:\Path\To\File.exe");
Console.WriteLine(arch); // Output: "64-bit (x64)"

// Get raw machine type enum
var machine = BitnessChecker.GetMachineType(@"C:\Path\To\File.exe");
```

## Error Handling

The application handles various error conditions gracefully:
- **File not found** - Clear error message with file path
- **Invalid PE file** - Detects non-executable files (text files, images, etc.)
- **Access denied** - Handles permission issues
- **Corrupted files** - Detects malformed PE headers

## Architecture Detection

Uses .NET's built-in `System.Reflection.PortableExecutable.PEReader` to parse PE headers. Specifically reads the Machine field from the COFF header to determine target architecture.

**Supported Machine Types:**
- `0x014C` - 32-bit (x86)
- `0x8664` - 64-bit (x64)
- `0xAA64` - ARM64
- `0xA641` - ARM64EC
- `0xA64E` - ARM64X (Hybrid)

## Technical Details

- **Framework**: .NET 9
- **GUI Framework**: WPF (Windows only)
- **Dependencies**: None (uses only .NET Base Class Library)
- **PE Parsing**: System.Reflection.PortableExecutable namespace

## Project Structure

```
Bitness/
├── Bitness.sln              # Solution file
├── Bitness.Core/            # Core library
│   └── BitnessChecker.cs    # Main PE analysis logic
├── Bitness.CLI/             # Console application
│   └── Program.cs           # CLI implementation
├── Bitness.GUI/             # WPF GUI application
│   ├── App.xaml             # Application definition
│   ├── App.xaml.cs          # Startup logic with command-line handling
│   ├── MainWindow.xaml      # Main window layout
│   └── MainWindow.xaml.cs   # Window logic with drag-drop support
└── README.md                # This file
```

## License

[Add your license information here]

## Contributing

[Add contribution guidelines here]
