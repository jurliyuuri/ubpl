using System;
using System.IO;
using UbplCommon;

namespace Ubpldir
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("ubpldir.exe filename");
                Environment.Exit(1);
            }

            using var stream = File.OpenRead(args[0]);
            Span<byte> binary = stackalloc byte[16];

            int index = 0;
            while (stream.Read(binary) > 0)
            {
                uint[] values = ToUint(binary);
                Mnemonic mnemonic = (Mnemonic)values[0];
                ModRm modRm = new ModRm(values[1]);
                string head = ToOperandString(modRm.HeadMode, modRm.HeadReg1, modRm.HeadReg2, values[2]);
                string tail = ToOperandString(modRm.TailMode, modRm.TailReg1, modRm.TailReg2, values[3]);

                Console.Write("{0:X08}: ", index);
                Console.Write("{0:X08} {1:X08} {2:X08} {3:X08} | ", values[0], values[1], values[2], values[3]);
                Console.WriteLine("{0,-6} {1} {2}", mnemonic, head, tail);

                index += 16;
            }
        }

        static uint[] ToUint(ReadOnlySpan<byte> binary)
        {
            uint[] values = new uint[4];

            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    values[i] |= (uint)binary[i * 4 + j] << ((3 - j) * 8);
                }
            }

            return values;
        }

        static string ToOperandString(OperandMode operandMode, Register reg1, Register reg2, uint value)
        {
            string str = "";

            switch ((operandMode & ~OperandMode.ADDRESS))
            {
                case OperandMode.REG:
                    str = reg1.ToString();
                    break;
                case OperandMode.IMM:
                    str = value.ToString();
                    break;
                case OperandMode.IMM_REG:
                    str = value + "+" + reg1;
                    break;
                case OperandMode.IMM_NREG:
                    str = value + "|" + reg1;
                    break;
                case OperandMode.IMM_REG_REG:
                    str = value + "+" + reg1 + "+" + reg2;
                    break;
                case OperandMode.IMM_REG_NREG:
                    str = value + "+" + reg1 + "|" + reg2;
                    break;
                case OperandMode.IMM_NREG_REG:
                    str = value + "|" + reg1 + "+" + reg2;
                    break;
                case OperandMode.IMM_NREG_NREG:
                    str = value + "|" + reg1 + "|" + reg2;
                    break;
                default:
                    break;
            }

            if (operandMode.HasFlag(OperandMode.ADDRESS))
            {
                str += "@";
            }

            return str;
        }
    }
}
