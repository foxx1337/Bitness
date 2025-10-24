# Bitness Project - AI Coding Assistant Instructions

## Project Overview
Bitness is a .NET 9 utility for identifying Windows executable/DLL platform architecture (x86, x64, ARM64, ARM64EC, ARM64X). The project consists of three components sharing a common core library:

- `Bitness.Core` - Class library with PE header parsing logic
- `Bitness.CLI` - Console application (`Bitness-CLI.exe`)  
- `Bitness.GUI` - WPF desktop application (`Bitness.exe`)

## Architecture & Key Decisions

### Core Implementation Strategy
- **PE Parsing**: Use `System.Reflection.PortableExecutable.PEReader` (built into .NET 9)
- **No External Dependencies**: Avoid third-party libraries like PeNet; use only .NET BCL
- **Target Framework**: .NET 9 for all projects
- **Shared Logic**: All architecture detection logic resides in `Bitness.Core`

### Critical Machine Type Constants
The core logic maps PE header machine values to human-readable strings:
- `0x014C` → "32-bit (x86)"
- `0x8664` → "64-bit (x64)" 
- `0xAA64` → "ARM64"
- `0xA641` → "ARM64EC" (handle as raw value if not in enum)
- `0xA64E` → "ARM64X (Hybrid)" (handle as raw value if not in enum)

### Project Structure Requirements
```
Bitness.sln
├── Bitness.Core/        # Class library (.NET 9)
├── Bitness.CLI/         # Console app (.NET 9)  
└── Bitness.GUI/         # WPF app (.NET 9, WindowsDesktop SDK)
```

## Implementation Guidelines

### Bitness.Core API Design
Expose minimal public surface:
```csharp
public static string GetMachineTypeString(string filePath);
// Optional: public static MachineType GetMachineType(string filePath);
```

### Error Handling Strategy
- Throw exceptions for file I/O errors and invalid PE files
- Let consuming applications (CLI/GUI) handle exception display
- Use meaningful exception messages like "File not found" or "Not a valid PE file"

### CLI Application Patterns
- Single file argument handling with clear usage message
- Output format: `File: <path>\nArchitecture: <result>`
- Exit code 1 on errors, print errors to `Console.Error`

### GUI Application Requirements  
- **WPF with drag-and-drop**: Set `AllowDrop="True"`, handle `Drop` events
- **File dialog fallback**: Click-to-browse functionality
- **Command-line startup**: Handle file path as startup argument
- **Simple layout**: Two-line display (File: / Architecture:) with instruction text when empty
- **Assembly naming**: Use `<AssemblyName>Bitness-CLI</AssemblyName>` to get exact output names

## Build & Development Workflow

### Project Setup Commands
```powershell
dotnet new sln -n Bitness
dotnet new classlib -n Bitness.Core -f net9.0
dotnet new console -n Bitness.CLI -f net9.0  
dotnet new wpf -n Bitness.GUI -f net9.0
dotnet sln add **/*.csproj
```

### Essential Project References
- CLI and GUI projects must reference Bitness.Core
- GUI project requires `<UseWPF>true</UseWPF>` in csproj
- Use `Microsoft.NET.Sdk.WindowsDesktop` SDK for WPF project

## Testing Strategy
Test with known binaries:
- x86 executable (32-bit)
- x64 executable/DLL (64-bit)  
- ARM64 binary (if available)
- Non-PE files (should handle gracefully)

## Code Style & Conventions
- **Minimalism**: Keep implementations simple and focused
- **No Heavy Dependencies**: Prefer .NET BCL over external packages
- **Error Clarity**: Provide clear, actionable error messages
- **Consistent Naming**: Follow exact assembly name requirements from spec

## Key Files to Reference
- `.github/prompts/devspec.prompt.md` - Complete technical specification
- Focus implementation on `System.Reflection.PortableExecutable` namespace usage
- Prioritize the three-project structure with shared core logic
