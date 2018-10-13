using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ubpl2003lk.Core;

namespace Ubpl2003lk
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length < 1)
            {
                Console.WriteLine("ubplla.exe fileName... [-o outFileName]");
                Environment.Exit(0);
            }

            string output = "a.out";
            bool isDebug = false;
            List<string> input = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--debug":
                        isDebug = true;
                        break;
                    case "-o":
                        output = args[++i];
                        break;
                    default:
                        input.Add(args[i]);
                        break;
                }
            }


            if (!input.Any())
            {
                Console.WriteLine("No output file name.");
                Environment.Exit(1);
            }

            var assembler = new LkAssembler(input)
            {
                IsDebug = isDebug,
            };

            assembler.Execute(output);
        }
    }
}
