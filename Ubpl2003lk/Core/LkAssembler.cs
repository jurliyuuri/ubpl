using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UbplCommon;
using UbplCommon.Translator;
using Ubpl2003lk;
using System.IO;

namespace Ubpl2003lk.Core
{
    class LkAssembler : CodeGenerator
    {
        #region Constant

        private const string FASAL_LABEL = "@fasal";

        #endregion

        #region Properties

        public bool IsDebug { get; set; }

        #endregion

        readonly IList<string> inFiles;
        IDictionary<string, bool> kuexok;

        public LkAssembler(List<string> inFiles) : base()
        {
            this.inFiles = inFiles;
            this.kuexok = new Dictionary<string, bool>();
        }

        public void Execute(string outFile)
        {
            List<LkCode> codeList = new List<LkCode>();
            int count = 0;
            
            foreach (var inFile in inFiles)
            {
                IList<string> wordList = Read(inFile);

                if(IsDebug)
                {
                    Console.WriteLine("{0}", string.Join(",", wordList));
                }

                AnalyzeLabel(wordList);
                codeList.AddRange(Analyze(wordList, count++));
            }

            int startCount = codeList.Count(x => x.IsLabel && x.Label == FASAL_LABEL);

            switch(startCount)
            {
                case 0:
                    break;
                case 1:
                    codeList.Insert(0, new LkCode
                    {
                        Mnemonic = LkMnemonic.KRZ,
                        Head = ToOperand(FASAL_LABEL, false),
                        Tail = XX,
                    });
                    break;
                default:
                    throw new ApplicationException("Found multiple main files");
            }

            if(IsDebug)
            {
                Console.WriteLine("{0}", string.Join(",\n", codeList));
            }

            Create(codeList);
            Write(outFile);
        }

        #region 共通
        private Operand ToRegisterOperand(Register register)
        {
            Operand operand;
            switch (register)
            {
                case Register.F0:
                    operand = F0;
                    break;
                case Register.F1:
                    operand = F1;
                    break;
                case Register.F2:
                    operand = F2;
                    break;
                case Register.F3:
                    operand = F3;
                    break;
                case Register.F4:
                    operand = F4;
                    break;
                case Register.F5:
                    operand = F5;
                    break;
                case Register.F6:
                    operand = F6;
                    break;
                case Register.XX:
                    operand = XX;
                    break;
                case Register.UL:
                    operand = UL;
                    break;
                default:
                    throw new NotSupportedException($"Not Supported register: {register}");
            }

            return operand;
        }

        #endregion

        #region 字句解析

        private IList<string> Read(string inFile)
        {
            List<string> wordList = new List<string>();

            using (var reader = new StreamReader(inFile, new UTF8Encoding(false)))
            {
                StringBuilder buffer = new StringBuilder();

                while (!reader.EndOfStream)
                {
                    char c = System.Convert.ToChar(reader.Read());
                    if (char.IsWhiteSpace(c))
                    {
                        if (buffer.Length > 0)
                        {
                            wordList.Add(buffer.ToString());
                            buffer.Clear();
                        }
                    }
                    else if (c == ';')
                    {
                        while (!reader.EndOfStream && c != '\r' && c != '\n')
                        {
                            c = System.Convert.ToChar(reader.Read());
                        }
                    }
                    else if (c == '@')
                    {
                        if (buffer.Length > 0)
                        {
                            buffer.Append(c);
                            wordList.Add(buffer.ToString());
                            buffer.Clear();
                        }
                        else if (wordList.Count > 0)
                        {
                            wordList[wordList.Count - 1] += c;
                        }
                        else
                        {
                            throw new ApplicationException("Invalid Pattern '@'");
                        }
                    }
                    else if (c == '+')
                    {
                        if (buffer.Length == 0)
                        {
                            string str = wordList[wordList.Count - 1];
                            buffer.Append(str);
                            wordList.RemoveAt(wordList.Count - 1);
                        }

                        buffer.Append(c);

                        c = ' ';
                        while (!reader.EndOfStream && char.IsWhiteSpace(c))
                        {
                            c = System.Convert.ToChar(reader.Read());
                        }
                        
                        buffer.Append(c);
                    }
                    else
                    {
                        buffer.Append(c);
                    }
                }

                if(buffer.Length > 0)
                {
                    wordList.Add(buffer.ToString());
                }
            }

            return wordList;
        }

        #endregion

        #region ラベル処理

        private void AnalyzeLabel(IList<string> wordList)
        {
            for (int i = 0; i < wordList.Count; i++)
            {
                var str = wordList[i];
                
                string label;
                switch (str)
                {
                    case "nll":
                    case "l'":
                        ++i;
                        break;
                    case "kue":
                        label = wordList[++i];

                        kuexok[label] = true;
                        break;
                    case "xok":
                        label = wordList[++i];

                        if (!kuexok.ContainsKey(label))
                        {
                            kuexok[label] = false;
                        }
                        break;
                    case "krz":
                    case "kRz":
                    case "ata":
                    case "nta":
                    case "ada":
                    case "ekc":
                    case "dal":
                    case "dto":
                    case "dtosna":
                    case "dro":
                    case "dRo":
                    case "malkrz":
                    case "malkRz":
                    case "krz8i":
                    case "kRz8i":
                    case "krz16i":
                    case "kRz16i":
                    case "krz8c":
                    case "kRz8c":
                    case "krz16c":
                    case "kRz16c":
                    case "klon":
                        i += 2;
                        break;
                    case "nac":
                        i++;
                        break;
                    case "lat":
                    case "latsna":
                    case "fi":
                    case "inj":
                        i += 3;
                        break;
                    default:
                        break;
                }
            }

        }

        #endregion

        #region 中間コード化

        private IList<LkCode> Analyze(IList<string> wordList, int fileCount)
        {
            List<LkCode> codeList = new List<LkCode>();
            bool isMain = true;
            bool isCI = false;

            for (int i = 0; i < wordList.Count; i++)
            {
                var str = wordList[i];

                if (str == "'c'i")
                {
                    isCI = true;
                }
                else if (str == "'i'c")
                {
                    isCI = false;
                }
                else
                {
                    string label;
                    string head, middle, tail;

                    switch (str)
                    {
                        case "nll":
                            label = wordList[++i];
                            
                            codeList.Add(new LkCode
                            {
                                Mnemonic = LkMnemonic.NLL,
                                Label = kuexok.ContainsKey(label) ? label : $"{label}@{fileCount}",
                            });

                            if (wordList[i + 1] == "l'")
                            {
                                throw new ApplicationException($"Wrong label nll {wordList[i]} l'");
                            }
                            break;
                        case "l'":
                            if (i == 0)
                            {
                                throw new ApplicationException($"Wrong label l'");
                            }

                            label = wordList[++i];

                            codeList.Add(new LkCode
                            {
                                Mnemonic = LkMnemonic.L,
                                Label = kuexok.ContainsKey(label) ? label : $"{label}@{fileCount}",
                            });
                            break;
                        case "kue":
                            ++i;
                            isMain = false;
                            break;
                        case "xok":
                            ++i;
                            break;
                        case "lifem":
                        case "lifem8":
                        case "lifem16":
                            Operand opd = Convert(wordList[++i], fileCount);

                            if (opd.IsImm || opd.IsLabel)
                            {
                                codeList.Add(new LkCode
                                {
                                    Mnemonic = (LkMnemonic)Enum.Parse(typeof(LkMnemonic), str, true),
                                    Head = opd,
                                });
                            }
                            else
                            {
                                throw new ApplicationException($"Invalid constant value: {opd}");
                            }
                            break;
                        case "krz":
                        case "kRz":
                        case "ata":
                        case "nta":
                        case "ada":
                        case "ekc":
                        case "dal":
                        case "dto":
                        case "dtosna":
                        case "dro":
                        case "dRo":
                        case "malkrz":
                        case "malkRz":
                        case "krz8i":
                        case "kRz8i":
                        case "krz16i":
                        case "kRz16i":
                        case "krz8c":
                        case "kRz8c":
                        case "krz16c":
                        case "kRz16c":
                        case "klon":
                            (head, tail, i) = GetParam(wordList, isCI, i);

                            codeList.Add(new LkCode
                            {
                                Mnemonic = (LkMnemonic)Enum.Parse(typeof(LkMnemonic), str, true),
                                Head = Convert(head, fileCount),
                                Tail = Convert(tail, fileCount),
                            });
                            break;
                        case "lat":
                        case "latsna":
                            if (isCI)
                            {
                                middle = wordList[++i];
                                tail = wordList[++i];
                                head = wordList[++i];
                            }
                            else
                            {
                                head = wordList[++i];
                                middle = wordList[++i];
                                tail = wordList[++i];
                            }

                            codeList.Add(new LkCode
                            {
                                Mnemonic = (LkMnemonic)Enum.Parse(typeof(LkMnemonic), str, true),
                                Head = Convert(head, fileCount),
                                Middle = Convert(middle, fileCount),
                                Tail = Convert(tail, fileCount),
                            });
                            break;
                        case "kak":
                            throw new NotSupportedException("Not Supported 'kak'");
                        case "nac":
                            codeList.Add(new LkCode
                            {
                                Mnemonic = LkMnemonic.DAL,
                                Head = ToRegisterOperand(0),
                                Tail = Convert(wordList[++i], fileCount),
                            });
                            break;
                        case "fi":
                            head = wordList[++i];
                            tail = wordList[++i];
                            bool isCompare = Enum.TryParse(wordList[++i].ToUpper(), out LkMnemonic mne);

                            codeList.Add(new LkCode
                            {
                                Mnemonic = mne,
                                Head = Convert(head, fileCount),
                                Tail = Convert(tail, fileCount),
                            });

                            break;
                        case "inj":
                            if (isCI)
                            {
                                tail = wordList[++i];
                                middle = wordList[++i];
                                head = wordList[++i];
                            }
                            else
                            {
                                head = wordList[++i];
                                middle = wordList[++i];
                                tail = wordList[++i];
                            }

                            codeList.Add(new LkCode
                            {
                                Mnemonic = LkMnemonic.INJ,
                                Head = Convert(head, fileCount),
                                Middle = Convert(middle, fileCount),
                                Tail = Convert(tail, fileCount),
                            });

                            break;
                        default:
                            break;
                    }
                }
            }

            if (isMain)
            {
                LkMnemonic[] skip =
                {
                     LkMnemonic.LIFEM,
                     LkMnemonic.LIFEM8,
                     LkMnemonic.LIFEM16,
                     LkMnemonic.NLL,
                     LkMnemonic.L,
                };

                int index = codeList.FindIndex(x => !skip.Contains(x.Mnemonic));

                codeList.Insert(index, new LkCode
                {
                    Mnemonic = LkMnemonic.NLL,
                    Label = FASAL_LABEL,
                });
            }

            return codeList;
        }

        private (string, string, int) GetParam(IList<string> wordList, bool isCI, int count)
        {
            string h, t;
            if (isCI)
            {
                t = wordList[++count];
                h = wordList[++count];
            }
            else
            {
                h = wordList[++count];
                t = wordList[++count];
            }
            return (h, t, count);
        }


        private Operand Convert(string str, int fileCount)
        {
            bool seti = str.Last() == '@';
            Operand result;

            if (seti)
            {
                str = str.Remove(str.Length - 1);
            }

            if (str.IndexOf('+') != -1)
            {
                string[] paramArray = str.Split('+');

                result = ToOperand(0);

                foreach (var param in paramArray)
                {
                    result += ToOperand(param, fileCount);
                }
            }
            else
            {
                result = ToOperand(str, fileCount);
            }

            if(seti)
            {
                return Seti(result);
            }
            else
            {
                return result;
            }
        }

        Operand ToOperand(string str, int fileCount)
        {
            if (uint.TryParse(str, out uint val))
            {
                return ToOperand(val);
            }
            else if (Enum.TryParse(str.ToUpper(), out Register reg))
            {
                return ToRegisterOperand(reg);
            }
            else
            {
                if (kuexok.ContainsKey(str))
                {
                    return ToOperand(str, false);
                }
                else
                {
                    return ToOperand($"{str}@{fileCount}", false);
                }
            }
        }

        #endregion

        #region バイナリ作成

        private void Create(IList<LkCode> codeList)
        {
            foreach (var code in codeList)
            {
                switch (code.Mnemonic)
                {
                    case LkMnemonic.ATA:
                        Ata(code.Head, code.Tail);
                        break;
                    case LkMnemonic.NTA:
                        Nta(code.Head, code.Tail);
                        break;
                    case LkMnemonic.ADA:
                        Ada(code.Head, code.Tail);
                        break;
                    case LkMnemonic.EKC:
                        Ekc(code.Head, code.Tail);
                        break;
                    case LkMnemonic.DTO:
                        Dto(code.Head, code.Tail);
                        break;
                    case LkMnemonic.DRO:
                        Dro(code.Head, code.Tail);
                        break;
                    case LkMnemonic.DTOSNA:
                        Dtosna(code.Head, code.Tail);
                        break;
                    case LkMnemonic.DAL:
                        Dal(code.Head, code.Tail);
                        break;
                    case LkMnemonic.KRZ:
                        Krz(code.Head, code.Tail);
                        break;
                    case LkMnemonic.MALKRZ:
                        Malkrz(code.Head, code.Tail);
                        break;
                    case LkMnemonic.KRZ8I:
                        Krz8i(code.Head, code.Tail);
                        break;
                    case LkMnemonic.KRZ16I:
                        Krz16i(code.Head, code.Tail);
                        break;
                    case LkMnemonic.KRZ8C:
                        Krz8c(code.Head, code.Tail);
                        break;
                    case LkMnemonic.KRZ16C:
                        Krz16c(code.Head, code.Tail);
                        break;
                    case LkMnemonic.LLONYS:
                        Fi(code.Head, code.Tail, LLONYS);
                        break;
                    case LkMnemonic.XTLONYS:
                        Fi(code.Head, code.Tail, XTLONYS);
                        break;
                    case LkMnemonic.XOLONYS:
                        Fi(code.Head, code.Tail, XOLONYS);
                        break;
                    case LkMnemonic.XYLONYS:
                        Fi(code.Head, code.Tail, XYLONYS);
                        break;
                    case LkMnemonic.CLO:
                        Fi(code.Head, code.Tail, CLO);
                        break;
                    case LkMnemonic.NIV:
                        Fi(code.Head, code.Tail, NIV);
                        break;
                    case LkMnemonic.LLO:
                        Fi(code.Head, code.Tail, LLO);
                        break;
                    case LkMnemonic.XTLO:
                        Fi(code.Head, code.Tail, XTLO);
                        break;
                    case LkMnemonic.XOLO:
                        Fi(code.Head, code.Tail, XOLO);
                        break;
                    case LkMnemonic.XYLO:
                        Fi(code.Head, code.Tail, XYLO);
                        break;
                    case LkMnemonic.INJ:
                        if (code.Middle.IsReg && !code.Middle.IsAddress && code.Middle.Reg == Register.XX)
                        {
                            Fnx(code.Head, code.Tail);
                        }
                        else if ((code.Head.IsAddress && (code.Head.Reg == Register.XX || code.Head.SecondReg == Register.XX))
                            || (code.Middle.IsAddress && (code.Middle.Reg == Register.XX || code.Middle.SecondReg == Register.XX)))
                        {
                            Operand first = ToSetiXX(code.Head);
                            Operand second = ToSetiXX(code.Middle);

                            Krz(XX + 32, UL);
                            Mte(first, second);
                            if (second.IsAddress && second.Reg == Register.UL)
                            {
                                Anf(code.Tail, second);
                            }
                            else
                            {
                                Anf(code.Tail, code.Middle);
                            }
                        }
                        else
                        {
                            Operand first = ToXX(code.Head);
                            Operand second = ToXX(code.Middle);

                            Mte(first, second);
                            Anf(code.Tail, code.Middle);
                        }
                        break;
                    case LkMnemonic.LAT:
                        Lat(code.Head, code.Middle);
                        Anf(code.Middle, code.Tail);
                        break;
                    case LkMnemonic.LATSNA:
                        Latsna(code.Head, code.Middle);
                        Anf(code.Middle, code.Tail);
                        break;
                    case LkMnemonic.NLL:
                        Nll(code.Label);
                        break;
                    case LkMnemonic.L:
                        L(code.Label);
                        break;
                    case LkMnemonic.LIFEM:
                        if(code.Head.IsImm)
                        {
                            Lifem(code.Head.Disp.Value);
                        }
                        else
                        {
                            Lifem(code.Head.Label);
                        }
                        break;
                    case LkMnemonic.LIFEM8:
                        if (code.Head.IsImm)
                        {
                            Lifem8(code.Head.Disp.Value);
                        }
                        else
                        {
                            Lifem8(code.Head.Label);
                        }
                        break;
                    case LkMnemonic.LIFEM16:
                        if (code.Head.IsImm)
                        {
                            Lifem16(code.Head.Disp.Value);
                        }
                        else
                        {
                            Lifem16(code.Head.Label);
                        }
                        break;
                    case LkMnemonic.KLON:
                        Klon(code.Head, code.Tail);
                        break;
                    default:
                        throw new ApplicationException($"Unknown value: {code}");
                }
            }
        }

        private Operand ToXX(Operand operand)
        {
            if (operand.Reg == Register.XX)
            {
                return XX + 16;
            }
            else
            {
                return operand;
            }
        }

        private Operand ToSetiXX(Operand operand)
        {
            if (operand.IsAddress)
            {
                if (operand.Reg == Register.XX)
                {
                    if (operand.SecondReg == Register.XX)
                    {
                        return Seti(UL + UL);
                    }
                    else if (operand.HasSecondReg)
                    {
                        return Seti(UL + ToRegisterOperand(operand.SecondReg.Value));
                    }
                    else if (operand.IsRegImm)
                    {
                        return Seti(UL + operand.Disp.Value);
                    }
                    else
                    {
                        return Seti(UL);
                    }
                }
                else if (operand.HasSecondReg && operand.SecondReg == Register.XX)
                {
                    return Seti(UL + ToRegisterOperand(operand.Reg.Value));
                }
            }
            else if (operand.Reg == Register.XX)
            {
                return UL;
            }

            return operand;
        }

        #endregion
    }
}
