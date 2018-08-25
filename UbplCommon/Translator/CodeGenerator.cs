using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon.Translator
{
    public abstract class CodeGenerator
    {
        private IList<Code> codeList;
        private IDictionary<string, int> labels;

        protected static readonly Operand F0 = new Operand(Register.F0);
        protected static readonly Operand F1 = new Operand(Register.F1);
        protected static readonly Operand F2 = new Operand(Register.F2);
        protected static readonly Operand F3 = new Operand(Register.F3);
        protected static readonly Operand F5 = new Operand(Register.F5);
        protected static readonly Operand XX = new Operand(Register.XX);

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
            labels = new Dictionary<string, int>();
        }

        protected Operand Seti(Operand opd)
        {
            return opd.ToAddressing();
        }

        protected Operand ToOperand(uint val)
        {
            return new Operand(val);
        }

        protected Operand ToOperand(string label)
        {
            return new Operand(label);
        }
        
        /// <summary>
        /// 指定されたファイル名のファイルにバイナリを書き込みます
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        public void Write(string fileName)
        {
            using (var writer = new BinaryWriter(File.OpenWrite(fileName)))
            {
                int count = 0;

                foreach (var code in this.codeList)
                {
                    writer.Write(ToBinary((uint)code.Mnemonic));
                    writer.Write(ToBinary(code.Modrm.Value));

                    if (code.Head.IsLabel)
                    {
                        writer.Write(ToBinary((uint)(this.labels[code.Head.Label] - (count + 16))));
                    }
                    else if (code.Head.IsReg)
                    {
                        writer.Write(0U);
                    }
                    else if (code.Head.IsImm)
                    {
                        writer.Write(ToBinary(code.Head.Disp.Value));
                    }
                    else if (code.Head.HasSecondReg)
                    {
                        writer.Write(ToBinary((uint)code.Head.SecondReg.Value));
                    }

                    if (code.Tail.IsLabel)
                    {
                        throw new ArgumentException($"Invalid Operand: {code.Tail}");
                    }
                    else if (code.Tail.IsReg)
                    {
                        writer.Write(0U);
                    }
                    else if (code.Head.IsImm)
                    {
                        if(code.Tail.IsAddress)
                        {
                            writer.Write(ToBinary(code.Tail.Disp.Value));
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid Operand: {code.Tail}");
                        }
                    }
                    else if (code.Tail.HasSecondReg)
                    {
                        writer.Write(ToBinary((uint)code.Tail.SecondReg.Value));
                    }

                    count += 4;
                }
            }

            byte[] ToBinary(uint value)
            {
                var buffer = new byte[4];

                buffer[0] = (byte)(value >> 24);
                buffer[1] = (byte)(value >> 24);
                buffer[2] = (byte)(value >> 24);
                buffer[3] = (byte)(value >> 24);

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

        private ModRm CreateModRm(Operand head, Operand tail)
        {
            ModRm modrm = new ModRm();
            (modrm.ModeHead, modrm.TypeHead, modrm.RegHead) = GetValue(head);
            (modrm.ModeTail, modrm.TypeTail, modrm.RegTail) = GetValue(tail);

            return modrm;

            (OperandMode, OperandType, Register) GetValue(Operand value) {
                OperandMode mode = OperandMode.REG32;
                OperandType type = OperandType.REG;
                Register register = Register.F0;

                if(value.IsLabel)
                {
                    mode = OperandMode.REG32_IMM32;
                    register = Register.XX;
                }
                else if(value.IsImm)
                {
                    mode = OperandMode.IMM32;
                    register = Register.F0;
                }
                else if (value.IsReg)
                {
                    mode = OperandMode.REG32;
                    register = value.Reg.Value;
                }
                else if (value.HasSecondReg)
                {
                    mode = OperandMode.REG32_REG32;
                    register = value.Reg.Value;
                }

                if(value.IsAddress)
                {
                    mode |= OperandMode.ADDRESS;
                }

                return (mode, type, register);
            }
        }

        #endregion

        #region Operation
        
        /// <summary>
        /// 後置ラベルを定義します．
        /// </summary>
        /// <param name="name">ラベル名</param>
        protected void L(string name)
        {
            if(this.codeList.Any())
            {
                this.labels.Add(name, (this.codeList.Count - 4) * 4);
            }
            else
            {
                throw new ArgumentException();
            }
        }
        
        /// <summary>
        /// 前置ラベルを定義します．
        /// </summary>
        /// <param name="name">ラベル名</param>
        protected void Nll(string name)
        {
            this.labels.Add(name, this.codeList.Count * 4);
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
            Append(Mnemonic.DAL, opd1, opd2);
        }

        /// <summary>
        /// nacを表すメソッドです．
        /// </summary>
        /// <param name="opd">オペランド</param>
        protected void Nac(Operand opd)
        {
            Append(Mnemonic.DAL, new Operand(0), opd);
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
        /// <param name="name">ラベルや関数名</param>
        /// <param name="opd">オペランド</param>
        protected void Krz(string name, Operand opd)
        {
            Krz(new Operand(name, true), opd);
        }

        /// <summary>
        /// krzを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Krz(Operand opd1, Operand opd2)
        {
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
        /// <param name="name">ラベルや関数名</param>
        /// <param name="opd">オペランド</param>
        protected void Malkrz(string name, Operand opd)
        {
            Malkrz(new Operand(name, true), opd);
        }

        /// <summary>
        /// malkrzを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        protected void Malkrz(Operand opd1, Operand opd2)
        {
            Append(Mnemonic.MALKRZ, opd1, opd2);
        }

        /// <summary>
        /// fi系を表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd">オペランド</param>
        protected void Fi(uint val, Operand opd, FiType f)
        {
            Append(f.mne, new Operand(val), opd);
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
        /// injを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd2">オペランド</param>
        /// <param name="opd3">オペランド</param>
        protected void Inj(uint val, Operand opd2, Operand opd3)
        {
            Inj(new Operand(val), opd2, opd3);
        }

        /// <summary>
        /// injを表すメソッドです．
        /// </summary>
        /// <param name="name">ラベルや関数名</param>
        /// <param name="opd2">オペランド</param>
        /// <param name="opd3">オペランド</param>
        protected void Inj(string name, Operand opd2, Operand opd3)
        {
            Inj(new Operand(name, true), opd2, opd3);
        }

        /// <summary>
        /// injを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        /// <param name="opd3">オペランド</param>
        protected void Inj(Operand opd1, Operand opd2, Operand opd3)
        {
            if(opd1.Equals(opd3))
            {
                Append(Mnemonic.ACH, opd1, opd2);
            }
            else if(opd3.IsReg && opd3.Reg == Register.XX)
            {
                Append(Mnemonic.INJXX, opd1, opd2);
            }
            else if(opd2.IsReg && opd2.Reg == Register.XX)
            {
                Append(Mnemonic.FNX, opd1, opd3);
            }
            else
            {
                Append(Mnemonic.KRZ, opd2, opd3);
                Append(Mnemonic.KRZ, opd1, opd2);
            }
        }

        /// <summary>
        /// latを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd2">オペランド</param>
        /// <param name="opd3">オペランド</param>
        protected void Lat(uint val, Operand opd2, Operand opd3)
        {
            Lat(new Operand(val), opd2, opd3);
        }

        /// <summary>
        /// latを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        /// <param name="opd3">オペランド</param>
        protected void Lat(Operand opd1, Operand opd2, Operand opd3)
        {
            Append(Mnemonic.LAT, opd1, opd2);
            Append(Mnemonic.LATKRZ, opd2, opd3);
        }

        /// <summary>
        /// latsnaを表すメソッドです．
        /// </summary>
        /// <param name="val">即値</param>
        /// <param name="opd2">オペランド</param>
        /// <param name="opd3">オペランド</param>
        protected void Latsna(uint val, Operand opd2, Operand opd3)
        {
            Latsna(new Operand(val), opd2, opd3);
        }

        /// <summary>
        /// latsnaを表すメソッドです．
        /// </summary>
        /// <param name="opd1">オペランド</param>
        /// <param name="opd2">オペランド</param>
        /// <param name="opd3">オペランド</param>
        protected void Latsna(Operand opd1, Operand opd2, Operand opd3)
        {
            Append(Mnemonic.LATSNA, opd1, opd2);
            Append(Mnemonic.LATKRZ, opd2, opd3);
        }
    }

    #endregion
}
