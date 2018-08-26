using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UbplCommon;
using UbplCommon.Translator;
using System.IO;

namespace Ubplla.Core
{
    class LkAssembler : CodeGenerator
    {
        #region Constant

        private const string FASAL_LABEL = "@fasal";

        #endregion

        IList<string> inFiles;
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
                        Mnemonic = Mnemonic.KRZ,
                        Head = ToOperand(FASAL_LABEL, false),
                        Tail = XX,
                    });
                    break;
                default:
                    throw new ApplicationException("Found multiple main files");
            }

            //Console.WriteLine("{0}", string.Join(",\n", codeList));

            Create(codeList);
            Write(outFile);
        }

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
                            string str = wordList[wordList.Count - 1];
                            if (char.IsDigit(str.Last()))
                            {
                                wordList[wordList.Count - 1] = str + c;
                            }
                            else
                            {
                                throw new ApplicationException("Invalid Pattern '@'");
                            }
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
                            if (char.IsDigit(str.Last()))
                            {
                                buffer.Append(str);
                                wordList.RemoveAt(wordList.Count - 1);
                            }
                            else
                            {
                                throw new ApplicationException("Invalid Pattern '+'");
                            }
                        }

                        buffer.Append(c);

                        c = ' ';
                        while (!reader.EndOfStream && char.IsWhiteSpace(c))
                        {
                            c = System.Convert.ToChar(reader.Read());
                        }

                        if (char.IsDigit(c) || c == 'f')
                        {
                            buffer.Append(c);
                        }
                        else
                        {
                            throw new ApplicationException("Invalid Pattern '+'");
                        }

                    }
                    else
                    {
                        buffer.Append(c);
                    }
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
                                LabelType = str,
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
                                LabelType = str,
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
                            (head, tail, i) = GetParam(wordList, isCI, i);

                            codeList.Add(new LkCode
                            {
                                Mnemonic = (Mnemonic)Enum.Parse(typeof(Mnemonic), str, true),
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
                                Mnemonic = (Mnemonic)Enum.Parse(typeof(Mnemonic), str, true),
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
                                Mnemonic = Mnemonic.DAL,
                                Head = ToOperand(0),
                                Tail = Convert(wordList[++i], fileCount),
                            });
                            break;
                        case "fi":
                            head = wordList[++i];
                            tail = wordList[++i];
                            bool isCompare = Enum.TryParse(wordList[++i].ToUpper(), out Mnemonic mne);

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
                                Mnemonic = Mnemonic.INJ,
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
                codeList.Insert(0, new LkCode
                {
                    LabelType = "nll",
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
            Operand ToRegisterOperand(Register reg)
            {
                switch (reg)
                {
                    case Register.F0:
                        return F0;
                    case Register.F1:
                        return F1;
                    case Register.F2:
                        return F2;
                    case Register.F3:
                        return F3;
                    case Register.F4:
                        return F4;
                    case Register.F5:
                        return F5;
                    case Register.F6:
                        return F6;
                    case Register.XX:
                        return XX;
                    default:
                        throw new ApplicationException($"InvalidParameter '{reg}'");
                }
            }
            
            bool add = str.IndexOf('+') != -1;
            bool seti = str.Last() == '@';
            Operand result;

            if (seti)
            {
                str = str.Remove(str.Length - 1);
            }

            if (add)
            {
                string[] param = str.Split('+');
                uint val1, val2;

                bool succReg1 = Enum.IsDefined(typeof(Register), param[0].ToUpper());
                bool succReg2 = Enum.IsDefined(typeof(Register), param[1].ToUpper());
                bool succVal1 = uint.TryParse(param[0], out val1);
                bool succVal2 = uint.TryParse(param[1], out val2);
                
                if (!succReg1 && !succVal1)
                {
                    throw new ApplicationException($"Invalid Parameter '{str}'");
                }

                if (!succReg2 && !succVal2)
                {
                    throw new ApplicationException($"Invalid Parameter '{str}'");
                }

                if(succVal1 && succVal2)
                {
                    throw new ApplicationException($"Invalid Parameter '{str}'");
                }

                Operand op1 = null, op2 = null;

                if(succReg1)
                {
                    op1 = ToRegisterOperand((Register)Enum.Parse(typeof(Register), param[0], true));
                }

                if(succReg2)
                {
                    op2 = ToRegisterOperand((Register)Enum.Parse(typeof(Register), param[1], true));
                }

                if (succReg1 && succVal2)
                {
                    result = op1 + val2;
                }
                else if (succReg2 && succVal1)
                {
                    result = val1 + op2;
                }
                else
                {
                    result = op1 + op2;
                }
            }
            else
            {
                if (uint.TryParse(str, out uint val))
                {
                    result = ToOperand(val);
                }
                else if (Enum.TryParse(str.ToUpper(), out Register reg))
                {
                    result = ToRegisterOperand(reg);
                }
                else
                {
                    if(kuexok.ContainsKey(str))
                    {
                        result = ToOperand(str, false);
                    }
                    else
                    {
                        result = ToOperand($"{str}@{fileCount}", false);
                    }
                }
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

        #endregion

        #region バイナリ作成

        private void Create(IList<LkCode> codeList)
        {
            foreach (var code in codeList)
            {
                if(code.IsLabel)
                {
                    switch (code.LabelType.ToLower())
                    {
                        case "nll":
                            Nll(code.Label);
                            break;
                        case "l'":
                            L(code.Label);
                            break;
                        default:
                            throw new ApplicationException($"Unknown Value: {code.LabelType}");
                    }
                }
                else
                {
                    switch (code.Mnemonic)
                    {
                        case Mnemonic.ATA:
                            Ata(code.Head, code.Tail);
                            break;
                        case Mnemonic.NTA:
                            Nta(code.Head, code.Tail);
                            break;
                        case Mnemonic.ADA:
                            Ada(code.Head, code.Tail);
                            break;
                        case Mnemonic.EKC:
                            Ekc(code.Head, code.Tail);
                            break;
                        case Mnemonic.DTO:
                            Dto(code.Head, code.Tail);
                            break;
                        case Mnemonic.DRO:
                            Dro(code.Head, code.Tail);
                            break;
                        case Mnemonic.DTOSNA:
                            Dtosna(code.Head, code.Tail);
                            break;
                        case Mnemonic.DAL:
                            Dal(code.Head, code.Tail);
                            break;
                        case Mnemonic.KRZ:
                            Krz(code.Head, code.Tail);
                            break;
                        case Mnemonic.MALKRZ:
                            Malkrz(code.Head, code.Tail);
                            break;
                        case Mnemonic.LLONYS:
                            Fi(code.Head, code.Tail, LLONYS);
                            break;
                        case Mnemonic.XTLONYS:
                            Fi(code.Head, code.Tail, XTLONYS);
                            break;
                        case Mnemonic.XOLONYS:
                            Fi(code.Head, code.Tail, XOLONYS);
                            break;
                        case Mnemonic.XYLONYS:
                            Fi(code.Head, code.Tail, XYLONYS);
                            break;
                        case Mnemonic.CLO:
                            Fi(code.Head, code.Tail, CLO);
                            break;
                        case Mnemonic.NIV:
                            Fi(code.Head, code.Tail, NIV);
                            break;
                        case Mnemonic.LLO:
                            Fi(code.Head, code.Tail, LLO);
                            break;
                        case Mnemonic.XTLO:
                            Fi(code.Head, code.Tail, XTLO);
                            break;
                        case Mnemonic.XOLO:
                            Fi(code.Head, code.Tail, XOLO);
                            break;
                        case Mnemonic.XYLO:
                            Fi(code.Head, code.Tail, XYLO);
                            break;
                        case Mnemonic.INJ:
                            Inj(code.Head, code.Middle, code.Tail);
                            break;
                        case Mnemonic.LAT:
                            Lat(code.Head, code.Middle, code.Tail);
                            break;
                        case Mnemonic.LATSNA:
                            Latsna(code.Head, code.Middle, code.Tail);
                            break;
                        default:
                            throw new ApplicationException($"Unknown value: {code}");
                    }
                }
            }
        }

        #endregion
    }
}
