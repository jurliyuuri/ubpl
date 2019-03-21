using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UbplCommon;
using UbplCommon.Translator;

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

            using(var stream = File.Open(args[0], FileMode.Open))
            {
                var binary = new byte[16];

                int index = 0;
                while (stream.Read(binary, 0, binary.Length) > 0)
                {
                    uint[] values = ToUint(binary);
                    Mnemonic mnemonic = (Mnemonic)values[0];
                    ModRm modRm = new ModRm(values[1]);
                    string head = ToOperandString(modRm.ModeHead, modRm.RegHead, values[2]);
                    string tail = ToOperandString(modRm.ModeTail, modRm.RegTail, values[3]);

                    Console.Write("{0:X08}: ", index);
                    Console.Write("{0:X08} {1:X08} {2:X08} {3:X08} | ", values[0], values[1], values[2], values[3]);
                    Console.WriteLine("{0} {1} {2}", mnemonic, head, tail);

                    index += 16;
                }
            }
        }

        static uint[] ToUint(byte[] binary)
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

        static string ToOperandString(OperandMode operandMode, Register register, uint value)
        {
            string str = "";
            int signed = (int)value;

            switch (operandMode)
            {
                case OperandMode.REG32:
                case OperandMode.ADDR_REG32:
                case OperandMode.XX_REG32:
                case OperandMode.ADDR_XX_REG32:
                    str = register.ToString();
                    break;
                case OperandMode.XX_IMM32:
                case OperandMode.ADDR_XX_IMM32:
                    str = signed.ToString();
                    break;
                case OperandMode.IMM32:
                case OperandMode.ADDR_IMM32:
                    str = value.ToString();
                    break;
                case OperandMode.REG32_REG32:
                case OperandMode.ADDR_REG32_REG32:
                    str = register + "+" + (Register)value;
                    break;
                case OperandMode.REG32_IMM32:
                case OperandMode.ADDR_REG32_IMM32:
                case OperandMode.XX_REG32_IMM32:
                case OperandMode.ADDR_XX_REG32_IMM32:
                    if(signed < 0)
                    {
                        str = register + "" + signed;
                    }
                    else
                    {
                        str = register + "+" + signed;
                    }
                    break;
                default:
                    break;
            }

            if(operandMode.HasFlag(OperandMode.ADD_XX))
            {
                if(string.IsNullOrEmpty(str))
                {
                    str = Register.XX.ToString();
                }
                else if(str[0] == '-')
                {
                    str = Register.XX + str;
                }
                else
                {
                    str = Register.XX +"+"+ str;
                }
            }

            if(operandMode.HasFlag(OperandMode.ADDRESS))
            {
                str += "@";
            }

            return str;
        }
    }
}
