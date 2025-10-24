using Bitness.Core;

// Test the core library directly
Console.WriteLine("Testing Bitness.Core library:");
Console.WriteLine("=============================");

try
{
    // Test with a known 64-bit executable
    string result1 = BitnessChecker.GetMachineTypeString(@"C:\Windows\System32\notepad.exe");
    Console.WriteLine($"notepad.exe (System32): {result1}");
    
    // Test with a known 32-bit executable if it exists
    if (File.Exists(@"C:\Windows\SysWOW64\calc.exe"))
    {
        string result2 = BitnessChecker.GetMachineTypeString(@"C:\Windows\SysWOW64\calc.exe");
        Console.WriteLine($"calc.exe (SysWOW64): {result2}");
    }
    
    // Test with our own CLI executable
    string cliPath = Path.GetFullPath(@"..\Bitness.CLI\bin\Release\net9.0\Bitness-CLI.exe");
    if (File.Exists(cliPath))
    {
        string result3 = BitnessChecker.GetMachineTypeString(cliPath);
        Console.WriteLine($"Our CLI executable: {result3}");
    }
    else
    {
        Console.WriteLine($"CLI executable not found at: {cliPath}");
    }
    
    Console.WriteLine("\nAll tests completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
