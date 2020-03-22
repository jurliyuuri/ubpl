using System;
using System.Buffers;
using System.IO;
using System.Text;
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
            Span<uint> values = stackalloc uint[4];

            int index = 0;
            while (stream.Read(binary) > 0)
            {
                ToUint(binary, values);
                Mnemonic mnemonic = (Mnemonic)values[0];
                ModRm modRm = new ModRm(values[1]);
                string head = ToOperandString(modRm.HeadMode, modRm.HeadReg1, modRm.HeadReg2, values[2]);
                string tail = ToOperandString(modRm.TailMode, modRm.TailReg1, modRm.TailReg2, values[3]);

                Console.Write("{0:X08}: ", index);
                Console.Write("{0:X08} {1:X08} {2:X08} {3:X08} | ", values[0], values[1], values[2], values[3]);
                Console.WriteLine("{0,-6} {1} {2}", mnemonic, head, tail);

                index += 16;
                binary.Fill(0);
            }
        }

        static void ToUint(ReadOnlySpan<byte> binary, Span<uint> values)
        {
            values.Fill(0);

            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    values[i] |= (uint)binary[(i << 2) + j] << ((3 - j) << 3);
                }
            }
        }

        static string ToOperandString(OperandMode operandMode, Register reg1, Register reg2, uint value)
        {
            StringBuilder builder = new StringBuilder(24);

            switch ((operandMode & ~OperandMode.ADDRESS))
            {
                case OperandMode.REG:
                    builder.Append(ToString(reg1));
                    break;
                case OperandMode.IMM:
                    builder.Append(value);
                    break;
                case OperandMode.IMM_REG:
                    builder.Append(value).Append("+").Append(ToString(reg1));
                    break;
                case OperandMode.IMM_NREG:
                    builder.Append(value).Append("|").Append(ToString(reg1));
                    break;
                case OperandMode.IMM_REG_REG:
                    builder.Append(value).Append("+").Append(ToString(reg1))
                        .Append("+").Append(ToString(reg2));
                    break;
                case OperandMode.IMM_REG_NREG:
                    builder.Append(value).Append("+").Append(ToString(reg1))
                        .Append("|").Append(ToString(reg2));
                    break;
                case OperandMode.IMM_NREG_REG:
                    builder.Append(value).Append("|").Append(ToString(reg1))
                        .Append("+").Append(ToString(reg2));
                    break;
                case OperandMode.IMM_NREG_NREG:
                    builder.Append(value).Append("|").Append(ToString(reg1))
                        .Append("|").Append(ToString(reg2));
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid value: {operandMode}");
            }

            if (operandMode.HasFlag(OperandMode.ADDRESS))
            {
                builder.Append("@");
            }

            return builder.ToString();

            static string ToString(Register register)
            {
                return register switch {
                    Register.F0 => "F0",
                    Register.F1 => "F1",
                    Register.F2 => "F2",
                    Register.F3 => "F3",
                    Register.F4 => "F4",
                    Register.F5 => "F5",
                    Register.F6 => "F6",
                    Register.XX => "XX",
                    _ => throw new ArgumentOutOfRangeException($"Invalid value: {register}"),
                };
            }
        }
    }
}
