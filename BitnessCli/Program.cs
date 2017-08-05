using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PeNet;

namespace BitnessCli
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = new PeFile(args[0]);
            if (file.IsValidPeFile)
            {
                if (file.Is64Bit)
                {
                    Console.WriteLine("64 bit");
                }
                else if (file.Is32Bit)
                {
                    Console.WriteLine("32 bit");
                }
            }
            else if (file.IsDLL)
            {
                Console.Write("DLL");
                Console.WriteLine(file.Is64Bit ? " 64 bit" : " 32 bit");
            }
        }
    }
}
