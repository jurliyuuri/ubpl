using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon.Translator
{
    public abstract class CodeGenerator
    {
        private readonly IList<Code> codeList;
        private readonly IList<JumpLabel> labels;
        private readonly IList<LifemValue> lifemList;

        protected static readonly Operand F0 = new Operand(Register.F0, null, 0U);
        protected static readonly Operand F1 = new Operand(Register.F1, null, 0U);
        protected static readonly Operand F2 = new Operand(Register.F2, null, 0U);
        protected static readonly Operand F3 = new Operand(Register.F3, null, 0U);
        protected static readonly Operand F4 = new Operand(Register.F4, null, 0U);
        protected static readonly Operand F5 = new Operand(Register.F5, null, 0U);
        protected static readonly Operand F6 = new Operand(Register.F6, null, 0U);
        protected static readonly Operand XX = new Operand(Register.XX, null, 0U);
        protected static readonly Operand UL = new Operand(Register.UL, null, 0U);

        protected static readonly FiType XTLO = new FiType(Mnemonic.XTLO);
        protected static readonly FiType XYLO = new FiType(Mnemonic.XYLO);
        protected static readonly FiType CLO = new FiType(Mnemonic.CLO);
        protected static readonly FiType XOLO = new FiType(Mnemonic.XOLO);
        protected static readonly FiType LLO = new FiType(Mnemonic.LLO);
        protected static readonly FiType NIV = new FiType(Mnemonic.NIV);
        protected static readonly FiType XTLONYS = new FiType(Mnemonic.XTLONYS);
        protected static readonly FiType XYLONYS = new FiType(Mnemonic.XYLONYS);
        protected static readonly FiType XOLONYS = new FiType(Mnemonic.XOLONYS);
        protected static readonly FiType LLONYS = new FiType(Mnemonic.LLONYS);

        protected CodeGenerator()
        {
            codeList = new List<Code>();
            labels = new List<JumpLabel>();
            lifemList = new List<LifemValue>();
        }

        protected Operand Seti(Operand opd)
        {
            return opd.ToAddressing();
        }

        protected Operand Seti(JumpLabel label)
        {
            return label.ToAddressing();
        }

        protected Operand Seti(uint val)
        {
            return new Operand(val, true);
        }

        protected Operand ToOperand(uint val, bool address = false)
        {
            return new Operand(val, address);
        }

        /// <summary>
        /// 指定されたファイル名のファイルにバイナリを書き込みます
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        public void Write(string fileName)
        {
            using (var writer = new BinaryWriter(File.OpenWrite(fileName)))
            {
                writer.Write(ToBinaryCode().ToArray());
            }
        }

        /// <summary>
        /// バイナリを出力します
        /// </summary>
        /// <returns>変換結果</returns>
        public IReadOnlyList<byte> ToBinaryCode()
        {
            List<byte> binaryCode = new List<byte>();
            
            int labelLifemCount = this.lifemList.Where(x => x.SourceLabel != null).Count();
            int count = (this.codeList.Count + labelLifemCount + 1) * 16;
            
            foreach (var lifem in this.lifemList)
            {
                if (lifem.SetType == Mnemonic.KRZ && (count & 0x3) != 0)
                {
                    count += 4 - (count & 0x3);
                }
                else if (lifem.SetType == Mnemonic.KRZ16C && (count & 0x1) != 0)
                {
                    count += 1;
                }

                if (lifem.Labels != null)
                {
                    foreach(var label in lifem.Labels)
                    {
                        label.RelativeAddress = (uint)count - 16;
                        this.labels.Add(label);
                    }
                }
                
                switch (lifem.SetType)
                {
                    case Mnemonic.KRZ8C:
                        count += 1;
                        break;
                    case Mnemonic.KRZ16C:
                        count += 2;
                        break;
                    case Mnemonic.KRZ:
                        count += 4;
                        break;
                    default:
                        throw new ApplicationException($"Unknown Exception: {lifem.SetType}");
                }
            }

            binaryCode.AddRange(new byte[]
            {
                0x00, 0x00, 0x00, 0x08,
                0x03, 0x07, 0x00, 0x0F,
                0xFF, 0xFF, 0xFF, 0xF0,
                0x00, 0x00, 0x00, 0x00,
            });

            count = (this.codeList.Count + labelLifemCount + 1) * 16;
            foreach (var lifem in this.lifemList)
            {
                if (lifem.SetType == Mnemonic.KRZ && (count & 0x3) != 0)
                {
                    count += 4 - (count & 0x3);
                }
                else if (lifem.SetType == Mnemonic.KRZ16C && (count & 0x1) != 0)
                {
                    count += 1;
                }

                if (lifem.SourceLabel != null)
                {
                    uint position = this.labels.Where(x => x == lifem.SourceLabel).Single().RelativeAddress;

                    switch (lifem.SetType)
                    {
                        case Mnemonic.KRZ8C:
                            binaryCode.AddRange(new byte[] {
                                0x00, 0x00, 0x00, 0x0C,
                                0x03, 0x0F, 0x23, 0x0F,
                                (byte)(position >> 24),(byte)(position >> 16),
                                (byte)(position >> 8),(byte)(position),
                                (byte)(count >> 24),(byte)(count >> 16),
                                (byte)(count >> 8),(byte)(count),
                            });
                            break;
                        case Mnemonic.KRZ16C:
                            binaryCode.AddRange(new byte[] {
                                0x00, 0x00, 0x00, 0x0D,
                                0x03, 0x0F, 0x23, 0x0F,
                                (byte)(position >> 24),(byte)(position >> 16),
                                (byte)(position >> 8),(byte)(position),
                                (byte)(count >> 24),(byte)(count >> 16),
                                (byte)(count >> 8),(byte)(count),
                            });
                            break;
                        case Mnemonic.KRZ:
                            binaryCode.AddRange(new byte[] {
                                0x00, 0x00, 0x00, 0x08,
                                0x03, 0x0F, 0x23, 0x0F,
                                (byte)(position >> 24),(byte)(position >> 16),
                                (byte)(position >> 8),(byte)(position),
                                (byte)(count >> 24),(byte)(count >> 16),
                                (byte)(count >> 8),(byte)(count),
                            });
                            break;
                        default:
                            break;
                    }
                }

                switch (lifem.SetType)
                {
                    case Mnemonic.KRZ8C:
                        count += 1;
                        break;
                    case Mnemonic.KRZ16C:
                        count += 2;
                        break;
                    case Mnemonic.KRZ:
                        count += 4;
                        break;
                    default:
                        break;
                }
            }

            count = (labelLifemCount + 1) * 16;
            foreach (var code in this.codeList)
            {
                binaryCode.AddRange(ToBinary((uint)code.Mnemonic));
                binaryCode.AddRange(ToBinary(code.Modrm.Value));

                uint value = 0;
                switch (code.Modrm.ModeHead)
                {
                    case OperandMode.IMM32:
                    case OperandMode.ADDR_IMM32:
                    case OperandMode.REG32_IMM32:
                    case OperandMode.ADDR_REG32_IMM32:
                        value = code.Head.Immidiate;
                        break;
                    case OperandMode.XX_IMM32:
                    case OperandMode.XX_REG32_IMM32:
                    case OperandMode.ADDR_XX_IMM32:
                    case OperandMode.ADDR_XX_REG32_IMM32:
                        value = (uint)(code.Head.Immidiate - count);
                        break;
                    case OperandMode.REG32_REG32:
                    case OperandMode.ADDR_REG32_REG32:
                        value = (uint)code.Head.Second;
                        break;
                    default:
                        break;
                }
                binaryCode.AddRange(ToBinary(value));

                value = 0;
                switch (code.Modrm.ModeTail)
                {
                    case OperandMode.IMM32:
                    case OperandMode.ADDR_IMM32:
                    case OperandMode.REG32_IMM32:
                    case OperandMode.ADDR_REG32_IMM32:
                        value = code.Tail.Immidiate;
                        break;
                    case OperandMode.XX_IMM32:
                    case OperandMode.XX_REG32_IMM32:
                    case OperandMode.ADDR_XX_IMM32:
                    case OperandMode.ADDR_XX_REG32_IMM32:
                        value = (uint)(code.Tail.Immidiate - count);
                        break;
                    case OperandMode.REG32_REG32:
                    case OperandMode.ADDR_REG32_REG32:
                        value = (uint)code.Tail.Second;
                        break;
                    default:
                        break;
                }
                binaryCode.AddRange(ToBinary(value));

                count += 16;
            }

            count = 0;
            foreach (var lifem in this.lifemList)
            {
                var binary = ToBinary(lifem.Value);

                if (lifem.SetType == Mnemonic.KRZ && (count & 0x3) != 0)
                {
                    int offset = 4 - (count & 0x3);
                    for(int i = 0; i < offset; i++)
                    {
                        binaryCode.Add(0);
                    }
                    count += offset;
                }
                else if (lifem.SetType == Mnemonic.KRZ16C && (count & 0x1) != 0)
                {
                    binaryCode.Add(0);
                    count += 1;
                }

                switch (lifem.SetType)
                {
                    case Mnemonic.KRZ8C:
                        binaryCode.Add(binary[3]);
                        count += 1;
                        break;
                    case Mnemonic.KRZ16C:
                        binaryCode.Add(binary[2]);
                        binaryCode.Add(binary[3]);
                        count += 2;
                        break;
                    case Mnemonic.KRZ:
                        binaryCode.AddRange(binary);
                        count += 4;
                        break;
                    default:
                        throw new ApplicationException($"Unknown Exception: {lifem.SetType}");
                }
            }

            if((count & 0x3) != 0)
            {
                int offset = 4 - (count & 0x3);
                for (int i = 0; i < offset; i++)
                {
                    binaryCode.Add(0);
                }
                count += offset;
            }

            return new ReadOnlyCollection<byte>(binaryCode);

            byte[] ToBinary(uint value)
            {
                var buffer = new byte[4];

                buffer[0] = (byte)(value >> 24);
                buffer[1] = (byte)(value >> 16);
                buffer[2] = (byte)(value >> 8);
                buffer[3] = (byte)value;

                return buffer;
            }
        }

        #region GeneratingMethod

        private void Append(Mnemonic mne, Operand head, Operand tail)
        {
            ModRm modrm = CreateModRm(head, tail);

            this.codeList.Add(new Code
            {
                Mnemonic = mne,
                Modrm = modrm,
                Head = head,
                Tail = tail,
            });
        }

        private bool IsInvalidOperand(Operand opd)
        {
            return !opd.IsAddress && (opd.Second.HasValue || opd.Immidiate != 0|| opd.HasLabel);
        }

        private ModRm CreateModRm(Operand head, Operand tail)
        {
            ModRm modrm = new ModRm();
            (modrm.ModeHead, modrm.RegHead) = GetValue(head);
            (modrm.ModeTail, modrm.RegTail) = GetValue(tail);

            return modrm;

            (OperandMode, Register) GetValue(Operand value)
            {
                OperandMode mode = value.ValueType;
                Register register;

                if ((mode & OperandMode.XX_REG32) != 0)
                {
                    if(value.First == Register.XX)
                    {
                        register = value.Second ?? Register.F0;
                    }
                    else
                    {
                        register = value.First.Value;
                    }
                }
                else
                {
                    register = value.First ?? Register.F0;
                }
                
                return (mode, register);
            }
        }

        #endregion

        #region Operation
        
        /// <summary>
        /// 前置ラベルを定義します．
        /// </summary>
        /// <param name="label">ラベル</param>
        protected void Nll(JumpLabel label)
        {
            label.RelativeAddress = (uint)(this.codeList.Count * 16);
            this.labels.Add(label);
        }

        /// <summary>
        /// 定数を定義します．
        /// </summary>
        /// <param name="value"></param>
        protected void Lifem(uint value)
        {
            Lifem(new LifemValue
            {
                SetType = Mnemonic.KRZ,
                Value = value,
            });
        }

        /// <summary>
        /// ラベル先のアドレスを定数として定義します．
        /// </summary>
        /// <param name="value"></param>
        protected void Lifem(JumpLabel label)
        {
            Lifem(new LifemValue
            {
                SetType = Mnemonic.KRZ,
                SourceLabel = label,
            });
        }

        /// <summary>
        /// 定数を定義します．
        /// </summary>
        /// <param name="value"></param>
        protected void Lifem8(uint value)
        {
            Lifem(new LifemValue
            {
                SetType = Mnemonic.KRZ8C,
                Value = value,
            });
        }

        /// <summary>
        /// ラベル先のアドレスを定数として定義します．
        /// </summary>
        /// <param name="label"></param>
        protected void Lifem8(JumpLabel label)
        {
            Lifem(new LifemValue
            {
                SetType = Mnemonic.KRZ8C,
                SourceLabel = label,
            });
        }

        /// <summary>
        /// 定数を定義します．
        /// </summary>
        /// <param name="value"></param>
        protected void Lifem16(uint value)
        {
            Lifem(new LifemValue
            {
                SetType = Mnemonic.KRZ16C,
                Value = value,
            }); ;
        }

        /// <summary>
        /// ラベル先のアドレスを定数として定義します．
        /// </summary>
        /// <param name="label"></param>
        protected void Lifem16(JumpLabel label)
        {
            Lifem(new LifemValue
            {
                SetType = Mnemonic.KRZ16C,
                SourceLabel = label,
            });
        }

        /// <summary>
        /// 指定された値を定数として定義します．
        /// </summary>
        /// <param name="value"></param>
        private void Lifem(LifemValue lifem)
        {
            var labels = this.labels.Where(x => x.RelativeAddress == this.codeList.Count * 16).ToList();

            if (labels.Any())
            {
                foreach (var label in labels)
                {
                    lifem.Labels.Add(label);
                    this.labels.Remove(label);
                }
            }

            this.lifemList.Add(lifem);
        }

        /// <summary>
        /// ataを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Ata(uint val, Operand opd)
        {
            Ata(new Operand(val), opd);
        }

        /// <summary>
        /// ataを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Ata(Operand opd1, Operand opd2)
        {
            if(IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.ATA, opd1, opd2);
        }

        /// <summary>
        /// ntaを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Nta(uint val, Operand opd)
        {
            Nta(new Operand(val), opd);
        }

        /// <summary>
        /// ntaを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Nta(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.NTA, opd1, opd2);
        }

        /// <summary>
        /// adaを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Ada(uint val, Operand opd)
        {
            Ada(new Operand(val), opd);
        }

        /// <summary>
        /// adaを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Ada(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.ADA, opd1, opd2);
        }

        /// <summary>
        /// ekcを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Ekc(uint val, Operand opd)
        {
            Ekc(new Operand(val), opd);
        }

        /// <summary>
        /// ekcを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Ekc(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.EKC, opd1, opd2);
        }

        /// <summary>
        /// dtoを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Dto(uint val, Operand opd)
        {
            Dto(new Operand(val), opd);
        }

        /// <summary>
        /// dtoを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Dto(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.DTO, opd1, opd2);
        }

        /// <summary>
        /// droを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Dro(uint val, Operand opd)
        {
            Dro(new Operand(val), opd);
        }

        /// <summary>
        /// droを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Dro(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.DRO, opd1, opd2);
        }

        /// <summary>
        /// dtosnaを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Dtosna(uint val, Operand opd)
        {
            Dtosna(new Operand(val), opd);
        }

        /// <summary>
        /// dtosnaを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Dtosna(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.DTOSNA, opd1, opd2);
        }

        /// <summary>
        /// dalを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Dal(uint val, Operand opd)
        {
            Dal(new Operand(val), opd);
        }

        /// <summary>
        /// dalを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Dal(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.DAL, opd1, opd2);
        }
        
        /// <summary>
        /// krzを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Krz(uint val, Operand opd)
        {
            Krz(new Operand(val), opd);
        }
        
        /// <summary>
        /// krzを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Krz(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.KRZ, opd1, opd2);
        }

        /// <summary>
        /// malkrzを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Malkrz(uint val, Operand opd)
        {
            Malkrz(new Operand(val), opd);
        }
        
        /// <summary>
        /// malkrzを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Malkrz(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.MALKRZ, opd1, opd2);
        }

        /// <summary>
        /// krz8iを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Krz8i(uint val, Operand opd)
        {
            Krz8i(new Operand(val), opd);
        }
        
        /// <summary>
        /// krz8iを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Krz8i(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.KRZ8I, opd1, opd2);
        }

        /// <summary>
        /// krz16iを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Krz16i(uint val, Operand opd)
        {
            Krz16i(new Operand(val), opd);
        }
        
        /// <summary>
        /// krz16iを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Krz16i(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.KRZ16I, opd1, opd2);
        }
        /// <summary>
        /// krz8cを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Krz8c(uint val, Operand opd)
        {
            Krz8c(new Operand(val), opd);
        }
        
        /// <summary>
        /// krz8cを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Krz8c(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.KRZ8C, opd1, opd2);
        }

        /// <summary>
        /// krz16cを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Krz16c(uint val, Operand opd)
        {
            Krz16c(new Operand(val), opd);
        }
        /// <summary>
        /// krz16cを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Krz16c(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.KRZ16C, opd1, opd2);
        }
        /// <summary>
        /// fi系を表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Fi(uint val, Operand opd, FiType f)
        {
            Fi(new Operand(val), opd, f);
        }

        /// <summary>
        /// fi系を表すメソッドです．
        /// </summary>
        /// <param name="opd">オペランド</param>
        /// <param name="val">即値</param>
        protected void Fi(Operand opd, uint val, FiType f)
        {
            Fi(opd, new Operand(val), f);
        }

        /// <summary>
        /// fi系を表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Fi(Operand opd1, Operand opd2, FiType f)
        {
            Append(f.mne, opd1, opd2);
        }

        /// <summary>
        /// fnxを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Fnx(uint val, Operand opd)
        {
            Fnx(new Operand(val), opd);
        }
        
        /// <summary>
        /// fnxを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Fnx(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.FNX, opd1, opd2);
        }

        /// <summary>
        /// mteを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Mte(uint val, Operand opd)
        {
            Mte(new Operand(val), opd);
        }

        /// <summary>
        /// mteを表すメソッドです．
        /// </summary>
        /// <param name="val1">即値</param>
        /// <param name="val2">即値</param>
        protected void Mte(uint val1, uint val2)
        {
            Mte(new Operand(val1), new Operand(val2));
        }
        
        /// <summary>
        /// mteを表すメソッドです．
        /// </summary>
        /// <param name="opd">オペランド</param>
        /// <param name="val">即値</param>
        protected void Mte(Operand opd, uint val)
        {
            Mte(opd, new Operand(val));
        }
        
        /// <summary>
        /// mteを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Mte(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.MTE, opd1, opd2);
        }

        /// <summary>
        /// anfを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Anf(Operand opd1, Operand opd2)
        {
            if (IsInvalidOperand(opd1))
            {
                throw new ArgumentException($"Invalid Operand: {opd1} count:{this.codeList.Count}");
            }

            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{this.codeList.Count}");
            }
            Append(Mnemonic.ANF, opd1, opd2);
        }

        /// <summary>
        /// latを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Lat(uint val, Operand opd)
        {
            Lat(new Operand(val), opd);
        }

        /// <summary>
        /// latを表すメソッドです．
        /// </summary>
        /// <param name="opd">オペランド</param>
        /// <param name="val">即値</param>
        protected void Lat(Operand opd, uint val)
        {
            Lat(opd, new Operand(val));
        }

        /// <summary>
        /// latを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Lat(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.LAT, opd1, opd2);
        }

        /// <summary>
        /// latsnaを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Latsna(Operand opd, uint val)
        {
            Latsna(new Operand(val), opd);
        }

        /// <summary>
        /// latsnaを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Latsna(uint val, Operand opd)
        {
            Latsna(new Operand(val), opd);
        }

        /// <summary>
        /// latsnaを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Latsna(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.LATSNA, opd1, opd2);
        }

        protected void Klon(uint val, Operand opd)
        {
            Klon(new Operand(val), opd);
        }

        protected void Klon(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.KLON, opd1, opd2);
        }
    }

    #endregion
}
