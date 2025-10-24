using Bitness.Core;

namespace Bitness.CLI;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            Console.WriteLine("Usage: Bitness-CLI <path-to-exe-or-dll>");
            Console.WriteLine("Analyzes the platform architecture of Windows PE files (executables and DLLs).");
            return args.Length == 0 ? 1 : 0;
        }

        if (args.Length > 1)
        {
            Console.Error.WriteLine("Error: Too many arguments. Please specify only one file path.");
            Console.WriteLine("Usage: Bitness-CLI <path-to-exe-or-dll>");
            return 1;
        }

        var filePath = Path.GetFullPath(args[0]);

        try
        {
            var architecture = BitnessChecker.GetMachineTypeString(filePath);
            
            Console.WriteLine($"File: {filePath}");
            Console.WriteLine($"Architecture: {architecture}");
            
            return 0;
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"Error: File not found - {filePath}");
            return 1;
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return 1;
        }
    }
}
