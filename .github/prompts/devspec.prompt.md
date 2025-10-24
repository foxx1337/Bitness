---
mode: 'Beast Mode'
---

# Bitness Tool – Development Specification (Targeting .NET 9)

## Overview  
**Bitness** is a simple .NET 9 utility for identifying the platform **bitness** of a Windows executable or DLL file. Given a target file, it determines whether the file is compiled for **32-bit (x86)**, **64-bit (x64)**, **ARM64**, or **ARM64EC** architectures. The tool will be delivered in two forms, both sharing a common library:

- **Bitness.dll** – A class library containing all core logic for analyzing the PE (Portable Executable) file.  
- **Bitness-CLI.exe** – A command-line tool that uses Bitness.dll to output the architecture info to the console.  
- **Bitness.exe** – A GUI tool (built with WPF on .NET 9) that provides a minimal graphical interface, allowing users to drag-and-drop a file or use a file dialog to get the information.

**Key Features and Requirements**:  
- *Accuracy*: Correctly identify x86, x64, ARM64, and ARM64EC binaries. (If possible, also handle edge cases like ARM64X combined binaries as “ARM64X”, since those are related to ARM64EC[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format).)  
- *Simplicity*: Keep the code clear and minimal. Use high-level APIs or libraries to avoid writing low-level parsing code if possible.  
- *Usability*: 
  - CLI should accept a file path argument and print a clear result. 
  - GUI should allow specifying the file via command-line arg, drag-and-drop, or file dialog, and then display the architecture in a user-friendly way.  
- *No heavy dependencies*: Prefer using .NET’s built-in capabilities or lightweight libraries over large frameworks or dev tools like Visual Studio’s dumpbin.

## Architecture & Technology Choices

### Options Considered for Reading PE Headers  
To determine a PE file’s architecture, we need to read its **PE header**, specifically the *Machine* field of the COFF File Header. This field is a 2-byte value that indicates the target CPU type[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). Key values include: `0x014C` for x86, `0x8664` for x64, `0xAA64` for ARM64, and `0xA641` for ARM64EC[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format)[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). There were a few approaches considered to retrieve this:

- **Using Dumpbin (External Tool)**: The Visual Studio `dumpbin /headers` command already shows the machine type (e.g., `8664 machine (x64)` in its output[2](https://www.codeproject.com/Tips/1071146/How-to-Identify-if-the-Executable-is-bit-or-bit)). While this confirms the concept, incorporating dumpbin into our tool would require Visual Studio to be installed and parsing its text output, which is not ideal for a self-contained utility.

- **Using an Open-Source PE Library (PeNet)**: The https://github.com/secana/PeNet library can parse PE files and provide the machine architecture easily. For example, `PeFile.ImageNtHeaders.FileHeader.MachineResolved` returns a human-readable string like "AMD64" for x64 executables[3](https://deepwiki.com/secana/PeNet/3-pe-structure-analysis)[3](https://deepwiki.com/secana/PeNet/3-pe-structure-analysis). This would simplify coding to essentially: open the file with PeNet and read the `MachineResolved` property. **Potential concern**: We need to ensure it recognizes ARM64EC. As of recent versions, PeNet likely handles newer constants (PeNet 5.1.0 targets .NET 8 and should include modern machine types). Using PeNet adds an external dependency (via NuGet) but is still quite straightforward.

- **Using .NET’s built-in Metadata APIs**: .NET (since .NET Core) provides the **System.Reflection.Metadata** library which includes `PEReader` and related classes in the `System.Reflection.PortableExecutable` namespace. This allows reading the PE headers in pure managed code. We can use `PEReader.PEHeaders.CoffHeader.Machine` to get the machine enum value. .NET’s `Machine` enum covers many architectures (x86, x64, ARM64, etc.) but in .NET 9 it might not have a named entry for ARM64EC if that enum wasn’t updated. However, we can still obtain the numeric value and compare to known constants (0xA641 for ARM64EC, 0xA64E for ARM64X[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format)). This approach requires no additional packages and minimal code.

- **Manual PE Header Parsing**: We could manually read the file bytes to find the machine field. This involves reading the DOS header to get the offset to the NT headers (`e_lfanew` at file offset 0x3C), then jumping to that offset + 4 to read the 2-byte machine value (after the "PE\0\0" signature). This is a small amount of code and gives full control. However, we’d be reimplementing what the above APIs already provide, and we’d need to map constants ourselves. It’s an option if we wanted zero dependencies and didn’t trust higher-level APIs, but it’s more error-prone if not done carefully.

- **Win32 API (P/Invoke)**: Windows offers APIs like `GetBinaryType` or functions in DbgHelp to retrieve binary information. `GetBinaryType` can tell if a file is 32- or 64-bit, but it doesn’t distinguish ARM64 vs ARM64EC. It’s also limited (it returns a simple enum for 32-bit, 64-bit, DOS, etc.). Other APIs to get the IMAGE_NT_HEADERS would effectively replicate manual parsing with added interop complexity. Given the simplicity of the need, pure C# approaches are preferable.

**Chosen Approach**: Use the **System.Reflection.Metadata** API (`PEReader`) to implement the core logic. This choice is made for the following reasons:  
- It’s part of the .NET 9 framework, so no third-party dependency is required (keeping the project simple and self-contained).  
- It handles the heavy lifting of parsing the file format, reducing chances of manual parsing errors.  
- Performance is not an issue for just reading one file’s header, and this API is efficient for such tasks anyway.  
- We can easily extend support to new machine types in the future by updating our mapping logic (or if .NET adds them to the enum).

**ARM64EC Handling**: We will explicitly handle ARM64EC and ARM64X values since those might not have named constants in the enum. According to Microsoft’s PE specification:  
- **ARM64EC** binaries have Machine value `0xA641`[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). We will label this as “ARM64EC”.  
- **ARM64X** (combined ARM64 + ARM64EC) binaries have Machine value `0xA64E`[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format). We can label this as “ARM64X (Hybrid)”.  
These are relatively new and mostly relevant for Windows 11 on ARM scenarios. Our tool will recognize and report them, even if the average user mainly encounters x86/x64.

*Note:* If we find during implementation that `PEReader` doesn’t expose the Machine value for some reason (which is unlikely), we will fallback to a quick manual read of those bytes. But given `CoffHeader.Machine` is readily available, that should suffice.

### .NET 9 and Project Templates  
We will use **.NET 9** as the target framework for all projects. .NET 9 ensures we have the latest API support and runtime optimizations. Both the CLI and GUI will be Windows-specific (especially the GUI, since WPF runs on Windows), but targeting .NET 9 allows using the latest C# features and library improvements. WPF is supported on .NET (Core) via the “Windows Desktop” SDK, so we’ll ensure the GUI project is configured accordingly.

The development environment can be Visual Studio 2025 (or later) or the .NET CLI with a code editor. The solution will contain three projects as outlined below.

## Project Structure  

Our solution will contain the following projects (assemblies):

1. **Bitness.Core** – *Class Library* (.NET 9)  
   - Output: `Bitness.dll`  
   - Contains the core functionality: opening a PE file and determining its machine type.  
   - No UI or I/O beyond reading the file. It will expose an API that the other two projects can call.  

2. **Bitness.CLI** – *Console Application* (.NET 9)  
   - Output: `Bitness-CLI.exe`  
   - References Bitness.Core.  
   - Provides a command-line interface: parse arguments, call the core library, and print results.  

3. **Bitness.GUI** – *WPF Desktop Application* (.NET 9, using Windows Desktop SDK)*  
   - Output: `Bitness.exe`  
   - References Bitness.Core.  
   - Provides a GUI for users who prefer a drag-and-drop or click interface.  

*\*If WPF is not desirable or if cross-platform GUI was needed, we could consider WinUI or MAUI. However, the requirement is specifically to run on Windows and simplicity is key, so WPF is an appropriate choice.* 

### Project Relationships and Organization  
All three projects will reside in a single solution (e.g., `Bitness.sln`). Bitness.Core will have no dependencies other than the base class library. The CLI and GUI will each have a Project Reference to Bitness.Core. This way, they can use the core functionality easily.

We will ensure the output names are exactly as specified (for example, in .csproj we might set `<AssemblyName>Bitness-CLI</AssemblyName>` to get that exact file name, since by default it might use the project name).

## Implementation Details

### Bitness.Core (Library)  
This is the heart of the tool. It will likely consist of a single class (e.g., `BitnessChecker` or `BitnessUtil`) with a couple of static methods to analyze a file. Key points:

- **Public API**: We can expose methods such as:  
  ```csharp
  public static MachineType GetMachineType(string filePath);
  public static string GetMachineTypeString(string filePath);
  ```  
  where `MachineType` is an enum we define (with values X86, X64, ARM64, ARM64EC, ARM64X, Unknown), and `GetMachineTypeString` returns a friendly description (e.g., "32-bit (x86)"). Alternatively, we may decide to skip our own enum and directly return the description string since that’s ultimately what we display.

- **Internal Logic**: 
  - Use `FileStream` or `File.OpenRead` to open the target file. (Ensure to handle exceptions if file is missing or access is denied, and throw a meaningful error or return an “Unknown” result.)
  - Create a `PEReader` from the stream (`using System.Reflection.PortableExecutable;`). This will parse the PE structures. 
  - Access the COFF header via `peReader.PEHeaders.CoffHeader`. From this, get the `Machine` value (type is `System.Reflection.PortableExecutable.Machine`). 
  - Map the `Machine` to our result:
    * If `Machine == Machine.Amd64` (0x8664), that’s x64[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format).
    * If `Machine == Machine.I386` (0x14C), that’s x86[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format).
    * If `Machine == Machine.Arm64` (0xAA64), that’s ARM64[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format).
    * For ARM64EC and ARM64X, since these might not be distinct enum members in .NET 9, we compare the numeric value: 
      - If `((ushort)MachineValue) == 0xA641`, that’s ARM64EC[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format).
      - If `((ushort)MachineValue) == 0xA64E`, that’s ARM64X[1](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format).
    * (We could also include a case for Itanium (0x200) or others as Unknown, but it’s outside our scope.)
    * Anything not recognized in our mapping we’ll label as “Unknown or unsupported”. This could include older architectures (Alpha, etc.) or if the file is not a PE file at all.
  - Close the file stream (the using statement will handle that). 

- **Return/Output**: We then return either the enum or string. Likely, we will implement `GetMachineTypeString` to directly return the final string for convenience in UI layers. For instance: 
  - `"32-bit (x86)"`, `"64-bit (x64)"`, `"ARM64"`, `"ARM64EC"`, `"ARM64X (Hybrid)"`, or `"Unknown"`. 
  - Using a consistent format helps; including both bitness and architecture name for x86/x64 is user-friendly (many users recognize “32-bit” vs “64-bit”). For ARM64 and ARM64EC, just the names are probably clear enough (since ARM64 implies 64-bit already). We might still prepend “64-bit” to ARM64 for consistency: e.g., "64-bit (ARM64)" and "64-bit (ARM64EC)" – but ARM64EC is a special case not just a standard ARM64, so probably best shown as just “ARM64EC”.

- **Example**: If `filePath` is an x64 binary, `GetMachineTypeString` might return `"64-bit (x64)"`. If it’s an ARM64EC binary, it would return `"ARM64EC"`.

- **Errors**: If the file isn’t found or isn’t a valid PE, we may throw an exception or return a special value. Perhaps simpler is to throw exceptions on error (with messages like "File not found" or "Not a valid PE file") and let the calling application handle it, since the CLI can catch and print the error, and the GUI can catch and display a dialog or message. Alternatively, return "Unknown" or null on error, but then we lose error details. We will likely throw a custom exception or use `IOException`/`InvalidDataException` for the GUI/CLI to catch.

- **Unit Testing**: (If we include a test project) we can test this method with known files. .NET itself comes with utilities, but since our target environment is just development, we can manually test on known binaries.

### Bitness.CLI (Console)  
This project is a straightforward console app, with essentially a `Main` method that funnels to Bitness.Core:

- **Argument Handling**: We expect the user to provide one argument: the path to the file to check. We’ll use `args` from `Main(string[] args)`. If `args.Length == 0`, or the path is something like `-h` or `--help`, we will display a usage message. For example:  
  ```text
  Usage: Bitness-CLI <path-to-exe-or-dll>
  ```  
  and exit with an error code (say 1). If more than one argument is provided, we might also treat that as an error (or optionally allow multiple files in future, but the spec focuses on one file at a time).

- **Processing**: 
  - Take the first argument as the file path. We might want to expand it to full path for clarity (using `Path.GetFullPath`).
  - Call the core library, e.g., 
    ```csharp
    try {
        string result = BitnessUtil.GetMachineTypeString(filePath);
        // print result
    } catch(Exception ex) {
        // print error message
        Environment.Exit(1);
    }
    ```
  - The output format should be simple and clear. Possibly:
    - If successful: either just print the architecture or include the file name in the output. For instance:  
      ```text
      File: C:\Apps\MyApp.exe  
      Architecture: 64-bit (x64)
      ```  
      This multi-line output is easy to read. Another option is a one-liner:  
      ```text
      C:\Apps\MyApp.exe : 64-bit (x64)
      ```  
      or just the architecture if the context is obvious. But including file name can be helpful especially if the user checks multiple files in a batch.
  - If error: print something to `Console.Error`, like "Error: [message]". For example "Error: File not found or inaccessible." or "Error: Not a valid PE file." 

- **Examples**: 
  - `Bitness-CLI.exe C:\Windows\System32\notepad.exe` might output `Architecture: 64-bit (x64)`. 
  - `Bitness-CLI.exe somefile.dll` (if somefile.dll is 32-bit) might output `Architecture: 32-bit (x86)`. 
  - If run with no arguments, we show usage. If run with a wrong path, we show an error.

- Since this app is just a thin wrapper, not much more needs to be specified. We will ensure the project references Bitness.Core and the executable is set to be named `Bitness-CLI.exe`.

### Bitness.GUI (WPF Application)  
This project provides a minimal graphical interface. The focus is on simplicity:

- **Startup Behavior**: The application’s `App` can capture command-line arguments. In WPF, we can override `OnStartup` or use `App.xaml` with `StartupUri` for a Window and then in that window’s code-behind check `Environment.GetCommandLineArgs()`. If an argument (other than the .exe name) is present, we treat it as a file to load on launch. 
  - We will attempt to load that file immediately and show the result. If it fails (invalid path or not a PE), we can show an error message box to the user and still show the main window (perhaps with the instruction text remaining or cleared).

- **Main Window UI**: Design the window to have one primary area (could be just the entire window):
  - Before a file is loaded: show a message like *“Drag an EXE/DLL here, or click to browse.”* This can be a `TextBlock` in the center, maybe italic or gray to indicate it's an instruction. The window background could be plain white or a light color to keep it simple.
  - After a file is loaded: show two lines of information:
    - **File:** `<full path or file name>`  
    - **Architecture:** `<architecture string>`  
    These can be in a StackPanel or Grid. We might use two TextBlocks or Labels, possibly with some basic styling (e.g., make "File:" and "Architecture:" labels in bold and the actual values normal weight, or use separate text runs).
  - We should consider text wrapping if the path is very long (maybe use a TextBlock with `TextWrapping` enabled, or truncate the middle of the path with an ellipsis if it’s too long to fit).

- **Drag & Drop Support**: 
  - Set the `AllowDrop="True"` on the main window or a specific panel. 
  - Handle `DragOver` and `Drop` events. In `DragOver`, we check if the data is a file; if so, set `e.Effects = Copy` and `e.Handled = true`. This gives the user a visual cue (cursor) that dropping is allowed. 
  - In `Drop`, retrieve the file path:  
    ```csharp
    if(e.Data.GetDataPresent(DataFormats.FileDrop)) {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if(files.Length > 0) {
            LoadFile(files[0]);
        }
    }
    ```  
    Where `LoadFile(string path)` is a helper that uses Bitness.Core to get the info and then updates the UI.
  - We should restrict to one file at a time (if multiple are dropped, we can either take the first or show an error/message asking for one at a time).

- **Click to Open Dialog**: 
  - We can handle a MouseLeftButtonUp event on the background or a specific visible element. For example, if we use the instruction TextBlock, we can handle its click event (or the window’s MouseUp if no file loaded yet).
  - When triggered, open a file dialog:  
    ```csharp
    var dialog = new Microsoft.Win32.OpenFileDialog();
    dialog.Filter = "Executable and DLL files|*.exe;*.dll|All files|*.*";
    if(dialog.ShowDialog() == true) {
        LoadFile(dialog.FileName);
    }
    ```
  - This allows users who may prefer clicking and browsing to select a file.

- **Displaying the Result**: The core of `LoadFile(path)` will be:
  - Try to get the architecture string via `BitnessUtil.GetMachineTypeString(path)`. 
    - On failure (exception), show a `MessageBox.Show("Error: ...")` and do not change the current display (or if nothing was displayed yet, keep the instruction text).
    - On success, update the UI text: e.g. set `fileText.Text = $"File: {path}"` and `archText.Text = $"Architecture: {result}"`.
  - We may also set the window title to include the file name, or just leave it as "Bitness".

- **Edge Cases**: If a user double-clicks Bitness.exe (no initial file) and then cancels the file dialog or doesn’t drop a file, the app will just continue showing the instruction message. That’s fine. We only exit when they close the window.  
  If a user drags a new file in after already having one displayed, it should simply overwrite the display with the new info (like checking multiple files sequentially in the same session).

- **Layout and Aesthetics**: Keep it simple:
  - A fixed minimum window size that can accommodate the text. Possibly allow it to auto-size to content or make it a reasonable default size (like 400x200).
  - Center the content. We can use a Grid with centered TextBlocks.
  - No menus, no buttons (except an implicit button via clicking the area). This minimalism aligns with the requirement for a “very simple” GUI.

- **Technology**: Use WPF on .NET 9. (Project will likely use `<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">` and `<UseWPF>true</UseWPF>` in the .csproj.) We could also consider WinForms for simplicity, but drag-and-drop and the overall modern feel is nicer in WPF. Since the user specifically mentioned WPF, we'll stick with that.

### Example Workflow

**CLI Usage Example**:  
```shell
> Bitness-CLI.exe "C:\Program Files\Example\app.dll"
```  
Output (to console):  
```
File: C:\Program Files\Example\app.dll  
Architecture: 64-bit (x64)
```  
If the user runs it without arguments:  
```
Usage: Bitness-CLI <path-to-exe-or-dll>
```  
If an error occurs (e.g., file not found or not a PE file):  
```
Error: Could not open the file or invalid PE format.
```  

**GUI Usage Example**:  
- Launching via command:  
  ```
  Bitness.exe "D:\Tools\myprog.exe"
  ```  
  The GUI window appears, and after a brief moment, it displays:  
  **File:** D:\Tools\myprog.exe  
  **Architecture:** 32-bit (x86)  

- Drag-and-drop:  
  Open Bitness.exe (no arguments). Window shows “Drag an EXE or DLL here...”. The user drags `C:\test\foo.dll` onto it. The text updates to:  
  **File:** C:\test\foo.dll  
  **Architecture:** ARM64EC  

  The user then drags another file, say `bar.exe`. Instantly the text updates to show that file’s info (replacing the previous). 

- Using file dialog:  
  Open Bitness.exe, click the window. A dialog appears; user selects `C:\Apps\app.exe`. The window then shows:  
  **File:** C:\Apps\app.exe  
  **Architecture:** 64-bit (x64)  

At all times, the determination is done by the shared logic, so the results in CLI and GUI are consistent.

## Tooling and Dependencies

- **.NET 9 SDK** – for building the projects. All projects target .NET 9; ensure you have the .NET 9 runtime for running the tools. We choose .NET 9 to leverage the latest API (and it implies we have System.Reflection.Metadata available and updated). .NET 9 is backwards compatible with prior .NET Core in terms of libraries we use, so no issues expected.  
- **IDE/Build**: Visual Studio 2025 (or later) will natively support .NET 9 and WPF. Alternatively, Visual Studio Code or another editor with `dotnet` CLI can be used.  
- **NuGet Packages**: None explicitly needed for core functionality. (If we had chosen PeNet, we would add the PeNet NuGet package, but we decided not to add extra dependencies). The System.Reflection.Metadata namespace is part of .NET’s Base Class Library in `System.Reflection.Metadata.dll`, which is included.  
- **Testing**: We should test with sample files:
  - A known 32-bit EXE (e.g., an old app or one compiled for x86) – expect output "32-bit (x86)".  
  - A known 64-bit EXE/DLL – expect "64-bit (x64)".  
  - If possible, an ARM64 binary (on an ARM machine or a file from Windows on ARM SDK) – expect "ARM64".  
  - If possible, an ARM64EC binary. (If none readily available, one could use a Windows 11 on ARM system file as a test – some system DLLs might be ARM64EC. This is optional, but our logic is prepared for it.)  
  - Also test dropping non-PE files (like a .txt) to see that we handle the error gracefully.

## Rationale for Simplicity and Clarity  
Throughout the design, we prioritize minimalism:
- We avoided complex frameworks (no heavy UI toolkit beyond WPF, no dependency on Visual Studio tools for parsing, no multi-file processing logic).
- The core parsing uses a high-level API in just a few lines, instead of dozens of lines of manual parsing code. This makes the code easier to maintain and understand.
- By sharing logic in Bitness.dll, we avoid duplicating code in CLI/GUI, reducing potential inconsistencies and bugs.
- The GUI is intentionally basic – no resizing complexities, no multi-file list, no saving results – just one file in, info out. This ensures the code for the GUI (likely just one window class) remains very straightforward.

## Conclusion  
**Bitness** will be a small, focused tool consisting of three artifacts (DLL, CLI exe, GUI exe) that work together to provide a convenient way to check the architecture of Windows binaries. Using .NET 9 and C#, we leverage modern APIs to keep implementation concise. Both advanced users (who prefer command-line automation) and casual users (who prefer drag-and-drop GUI) are served by the two interfaces. The project’s structure facilitates easy updates (for example, if a new architecture comes out, update the mapping in one place) and reusability of the core logic. The outcome is a simple yet effective utility aligning with the requirements of accuracy, simplicity, and clarity.
