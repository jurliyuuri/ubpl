using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ubplla.Core;

namespace Ubplla
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
            List<string> input = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-o":
                        if (i == args.Length - 1)
                        {
                            Console.WriteLine("No output file name.");
                            Environment.Exit(1);
                        }

                        output = args[++i];
                        break;
                    default:
                        input.Add(args[i]);
                        break;
                }
            }

            var assembler = new LkAssembler(input);
            assembler.Execute(output);
        }
    }
}
