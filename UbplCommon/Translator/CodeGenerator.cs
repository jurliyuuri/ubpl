using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace UbplCommon.Translator
{
    public abstract class CodeGenerator
    {
        private readonly IList<Code> _codes;
        private readonly IList<JumpLabel> _labels;
        private readonly IList<LifemValue> _lifemValues;

        protected static readonly Operand F0 = Operand.F0;
        protected static readonly Operand F1 = Operand.F1;
        protected static readonly Operand F2 = Operand.F2;
        protected static readonly Operand F3 = Operand.F3;
        protected static readonly Operand F4 = Operand.F4;
        protected static readonly Operand F5 = Operand.F5;
        protected static readonly Operand F6 = Operand.F6;
        protected static readonly Operand XX = Operand.XX;
        protected static readonly Operand ZERO = Operand.ZERO;

        protected static readonly FiType CLO = FiType.CLO;
        protected static readonly FiType NIV = FiType.NIV;
        protected static readonly FiType XTLONYS = FiType.XTLONYS;
        protected static readonly FiType XYLONYS = FiType.XYLONYS;
        protected static readonly FiType XOLONYS = FiType.XOLONYS;
        protected static readonly FiType LLONYS = FiType.LLONYS;
        protected static readonly FiType XTLO = FiType.XTLO;
        protected static readonly FiType XYLO = FiType.XYLO;
        protected static readonly FiType XOLO = FiType.XOLO;
        protected static readonly FiType LLO = FiType.LLO;

        protected CodeGenerator()
        {
            _codes = new List<Code>();
            _labels = new List<JumpLabel>();
            _lifemValues = new List<LifemValue>();
        }

        protected Operand Seti(Operand opd)
        {
            return opd.ToAddressing();
        }

        protected Operand Seti(uint value)
        {
            return new Operand(value, true);
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
            using var writer = new BinaryWriter(File.OpenWrite(fileName));
            writer.Write(ToBinaryCode().ToArray());
        }

        /// <summary>
        /// バイナリを出力します
        /// </summary>
        /// <returns>変換結果</returns>
        public IReadOnlyList<byte> ToBinaryCode()
        {
            List<byte> binaryCode = new List<byte>();
            List<byte> lifemBinary = new List<byte>();
            uint count = (uint)_codes.Count * 16U;

            // lifemのラベル処理，バイナリ化
            foreach (var lifemValue in _lifemValues)
            {
                byte[] binary = ToBinary(lifemValue.Value);

                if (lifemValue.Size == ValueSize.DWORD && (count & 0x3) != 0)
                {
                    uint offset = 4 - (count & 0x3);

                    for (uint i = 0; i < offset; i++)
                    {
                        lifemBinary.Add(0);
                    }

                    count += offset;
                }
                else if (lifemValue.Size == ValueSize.WORD && (count & 0x1) != 0)
                {
                    lifemBinary.Add(0);
                    count += 1;
                }

                if (lifemValue.Labels.Any())
                {
                    foreach (var label in lifemValue.Labels)
                    {
                        label.RelativeAddress = count;
                        _labels.Add(label);
                    }
                }

                switch (lifemValue.Size)
                {
                    case ValueSize.BYTE:
                        lifemBinary.Add(binary[3]);
                        count += 1;
                        break;
                    case ValueSize.WORD:
                        lifemBinary.Add(binary[2]);
                        lifemBinary.Add(binary[3]);
                        count += 2;
                        break;
                    case ValueSize.DWORD:
                        lifemBinary.AddRange(binary);
                        count += 4;
                        break;
                    default:
                        throw new ApplicationException($"Invalid value: {lifemValue.Size}");
                }
            }

            // コードのバイナリ化
            count = 16U;
            foreach (var code in _codes)
            {
                ModRm modrm = code.Modrm;
                uint value;
                
                binaryCode.AddRange(ToBinary((uint)code.Mnemonic));
                
                if (code.Head.HasLabel)
                {
                    switch (modrm.HeadMode)
                    {
                        case OperandMode.REG:
                            modrm.HeadMode = OperandMode.IMM_REG;
                            break;
                        case OperandMode.ADDR_REG:
                            modrm.HeadMode = OperandMode.ADDR_IMM_REG;
                            break;
                        default:
                            break;
                    }
                }

                if (code.Tail.HasLabel)
                {
                    switch (modrm.TailMode)
                    {
                        case OperandMode.REG:
                            modrm.TailMode = OperandMode.IMM_REG;
                            break;
                        case OperandMode.ADDR_REG:
                            modrm.TailMode = OperandMode.ADDR_IMM_REG;
                            break;
                        default:
                            break;
                    }
                }

                binaryCode.AddRange(ToBinary(code.Modrm.Value));
                
                if ((modrm.HeadMode & (~OperandMode.ADDRESS)) == OperandMode.REG)
                {
                    value = 0U;
                }
                else
                {
                    value = code.Head.Immidiate;

                    RegisterValue? register = code.Head.First;
                    if (!(register is null) && register.RelativeAddress != 0)
                    {
                        value -= count;
                    }

                    register = code.Head.Second;
                    if (!(register is null) && register.RelativeAddress != 0)
                    {
                        value -= count;
                    }
                }

                binaryCode.AddRange(ToBinary(value));

                if ((modrm.TailMode & (~OperandMode.ADDRESS)) == OperandMode.REG)
                {
                    value = 0;
                }
                else
                {
                    value = code.Tail.Immidiate;

                    RegisterValue? register = code.Tail.First;
                    if (!(register is null) && register.RelativeAddress != 0)
                    {
                        value -= count;
                    }

                    register = code.Tail.Second;
                    if (!(register is null) && register.RelativeAddress != 0)
                    {
                        value -= count;
                    }
                }
                binaryCode.AddRange(ToBinary(value));

                count += 16;
            }

            binaryCode.AddRange(lifemBinary);

            int length = binaryCode.Count & 0xF;
            for (int i = 16 - length; i > 0; i--)
            {
                binaryCode.Add(0);
            }

            return new ReadOnlyCollection<byte>(binaryCode);

            static byte[] ToBinary(uint value)
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

            _codes.Add(new Code
            {
                Mnemonic = mne,
                Modrm = modrm,
                Head = head,
                Tail = tail,
            });
        }

        private bool IsInvalidOperand(Operand opd)
        {
            return !opd.IsAddressing && (opd.Second != null || opd.Immidiate != 0 || opd.HasLabel);
        }

        private ModRm CreateModRm(Operand head, Operand tail)
        {
            ModRm modrm = new ModRm
            {
                Value = ((uint)head.ValueType << 24)
                | ((uint)(head.FirstRegister ?? Register.F0) << 20)
                | ((uint)(head.SecondRegister ?? Register.F0) << 16)
                | ((uint)tail.ValueType << 8)
                | ((uint)(tail.FirstRegister ?? Register.F0) << 4)
                | (uint)(tail.SecondRegister ?? Register.F0)
            };

            return modrm;
        }

        #endregion

        #region Operation

        /// <summary>
        /// 前置ラベルを定義します．
        /// </summary>
        /// <param name="label">ラベル</param>
        protected void Nll(JumpLabel label)
        {
            label.RelativeAddress = (uint)(_codes.Count * 16);
            _labels.Add(label);
        }

        /// <summary>
        /// 定数を定義します．
        /// </summary>
        /// <param name="value"></param>
        protected void Lifem(uint value)
        {
            Lifem(new LifemValue
            {
                Size = ValueSize.DWORD,
                Value = value,
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
                Size = ValueSize.BYTE,
                Value = value,
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
                Size = ValueSize.WORD,
                Value = value,
            });
        }

        private void Lifem(LifemValue lifemValue)
        {
            var relativeAddress = _codes.Count * 16;
            if (relativeAddress >= 0)
            {
                var labels = _labels.Where(x => x.RelativeAddress == relativeAddress).ToList();

                if (labels.Any())
                {
                    foreach (var label in labels)
                    {
                        lifemValue.Labels.Add(label);
                        _labels.Remove(label);
                    }
                }
            } 

            _lifemValues.Add(lifemValue);
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
            }
            Append(Mnemonic.KRZ16C, opd1, opd2);
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
            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2}, count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
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
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
            }
            Append(Mnemonic.DAL, opd1, opd2);
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
        /// <param name="type">比較タイプ</param>
        protected void Fi(Operand opd1, Operand opd2, FiType type)
        {
            Operand head, tail;
            Mnemonic mnemonic;

            switch (type)
            {
                case FiType.CLO:
                    mnemonic = Mnemonic.CLO;
                    head = opd1;
                    tail = opd2;
                    break;
                case FiType.NIV:
                    mnemonic = Mnemonic.NIV;
                    head = opd1;
                    tail = opd2;
                    break;
                case FiType.XTLO:
                    mnemonic = Mnemonic.XTLO;
                    head = opd1;
                    tail = opd2;
                    break;
                case FiType.XYLO:
                    mnemonic = Mnemonic.XYLO;
                    head = opd1;
                    tail = opd2;
                    break;
                case FiType.XOLO:
                    mnemonic = Mnemonic.XTLO;
                    head = opd2;
                    tail = opd1;
                    break;
                case FiType.LLO:
                    mnemonic = Mnemonic.XYLO;
                    head = opd2;
                    tail = opd1;
                    break;
                case FiType.XTLONYS:
                    mnemonic = Mnemonic.XTLONYS;
                    head = opd1;
                    tail = opd2;
                    break;
                case FiType.XYLONYS:
                    mnemonic = Mnemonic.XYLONYS;
                    head = opd1;
                    tail = opd2;
                    break;
                case FiType.XOLONYS:
                    mnemonic = Mnemonic.XTLONYS;
                    head = opd2;
                    tail = opd1;
                    break;
                case FiType.LLONYS:
                    mnemonic = Mnemonic.XYLONYS;
                    head = opd2;
                    tail = opd1;
                    break;
                default:
                    throw new ArgumentException($"Invalid Operation: {type} count:{_codes.Count}");
            }
            Append(mnemonic, head, tail);
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
                throw new ArgumentException($"Invalid Operand: {opd1} count:{_codes.Count}");
            }

            if (IsInvalidOperand(opd2))
            {
                throw new ArgumentException($"Invalid Operand: {opd2} count:{_codes.Count}");
            }
            Append(Mnemonic.ANF, opd1, opd2);
        }

        /// <summary>
        /// latを表すメソッドです．
        /// </summary>
        /// <param name="val1">即値</param>
        /// <param name="val2">即値</param>
        protected void Lat(uint val1, uint val2)
        {
            Lat(new Operand(val1), new Operand(val2));
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
        /// <param name="val1">即値</param>
        /// <param name="val2">即値</param>
        protected void Latsna(uint val1, uint val2)
        {
            Latsna(new Operand(val1), new Operand(val2));
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

        /// <summary>
        /// kakを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Kak(uint val, Operand opd)
        {
            Kak(new Operand(val), opd);
        }

        /// <summary>
        /// kakを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Kak(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.KAK, opd1, opd2);
        }

        /// <summary>
        /// kaksnaを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Kaksna(uint val, Operand opd)
        {
            Kaksna(new Operand(val), opd);
        }

        /// <summary>
        /// kaksnaを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Kaksna(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.KAKSNA, opd1, opd2);
        }

        /// <summary>
        /// klonを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Klon(uint val, Operand opd)
        {
            Klon(new Operand(val), opd);
        }

        /// <summary>
        /// klonを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Klon(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.KLON, opd1, opd2);
        }

        #endregion
    }
}