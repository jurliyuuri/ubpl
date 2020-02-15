using System;
using UbplCommon.Processor;

namespace Ubpl
{

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ubpl.exe (option) filename");
                Console.WriteLine("  --register : Show register of last register value");
                Console.WriteLine("  --memory   : Show memory of last register value");
                Environment.Exit(1);
            }

            Emulator emulator = new Emulator();
            string fileName = "";

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "--register":
                        emulator.ViewRegister = true;
                        break;
                    case "--memory":
                        emulator.ViewMemory = true;
                        break;
                    default:
                        fileName = arg;
                        break;
                }
            }

            emulator.Read(fileName);

            emulator.Run();
        }
    }
}
