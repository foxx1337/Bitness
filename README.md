# Bitness - simple project to inspect PE files

Uses the nice [PeNet library](https://github.com/secana/PeNet) for parsing the images.

In order to add this to the right-click menu, the following `.reg` are required
(do not forget to edit the path):

1. for `dll`:

   ```
   Windows Registry Editor Version 5.00

   [HKEY_CLASSES_ROOT\dllfile\shell\bitness]
   @="Bitness"

   [HKEY_CLASSES_ROOT\dllfile\shell\bitness\command]
   @="d:\\src\\Bitness\\Bitness\\bin\\Debug\\Bitness.exe \"%1\""
   ```

2. for `exe`:

   ```
   Windows Registry Editor Version 5.00

   [HKEY_CLASSES_ROOT\exefile\shell\bitness]
   @="Bitness"

   [HKEY_CLASSES_ROOT\exefile\shell\bitness\command]
   @="d:\\src\\Bitness\\Bitness\\bin\\Debug\\Bitness.exe \"%1"\"
   ```

## TODO

- Add pwsh script to automate registry setup.
- Add installer.
