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

        /// <summary>
        /// デバッグ用出力アドレス
        /// </summary>
        const uint TVARLON_KNLOAN_ADDRESS = 3126834864;

        private const string FASAL_LABEL_NAME = "@fasal";

        private static readonly JumpLabel FASAL_LABEL;
        
        static LkAssembler()
        {
            FASAL_LABEL = new JumpLabel();
        }

        #endregion

        #region Properties

        public bool IsDebug { get; set; }

        #endregion

        readonly IList<string> inFiles;
        readonly IDictionary<string, bool> kuexok;
        readonly IDictionary<string, JumpLabel> labels;

        public LkAssembler(List<string> inFiles) : base()
        {
            this.inFiles = inFiles;
            this.kuexok = new Dictionary<string, bool>();
            this.labels = new Dictionary<string, JumpLabel>();
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

            switch (startCount)
            {
                case 0:
                    break;
                case 1:
                    this.labels.Add(FASAL_LABEL_NAME, FASAL_LABEL);
                    codeList.Insert(0, new LkCode
                    {
                        Mnemonic = LkMnemonic.KRZ,
                        Head = FASAL_LABEL,
                        Tail = XX,
                    });
                    break;
                default:
                    throw new ApplicationException("Found multiple main files");
            }

            if (IsDebug)
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

        #region 外部公開ラベル処理

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
                    JumpLabel jumpLabel;

                    switch (str)
                    {
                        case "nll":
                            label = wordList[++i];

                            if (!this.kuexok.ContainsKey(label))
                            {
                                label = $"{label}@{fileCount}";
                            }

                            if (this.labels.ContainsKey(label))
                            {
                                jumpLabel = this.labels[label];
                            }
                            else
                            {
                                jumpLabel = new JumpLabel();
                                this.labels.Add(label, jumpLabel);
                            }
                            
                            codeList.Add(new LkCode
                            {
                                Mnemonic = LkMnemonic.NLL,
                                Label = jumpLabel,
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

                            if (!this.kuexok.ContainsKey(label))
                            {
                                label = $"{label}@{fileCount}";
                            }

                            if (this.labels.ContainsKey(label))
                            {
                                jumpLabel = this.labels[label];
                            }
                            else
                            {
                                jumpLabel = new JumpLabel();
                                this.labels.Add(label, jumpLabel);
                            }

                            codeList.Insert(codeList.Count - 1, new LkCode
                            {
                                Mnemonic = LkMnemonic.NLL,
                                Label = jumpLabel,
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

                            if (opd.HasLabel || (!opd.First.HasValue && !opd.Second.HasValue))
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

                            var code = new LkCode
                            {
                                Mnemonic = LkMnemonic.INJ,
                                Head = Convert(head, fileCount),
                                Middle = Convert(middle, fileCount),
                                Tail = Convert(tail, fileCount),
                            };

                            codeList.Add(code);
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
                result = ToOperand(paramArray[0], fileCount);

                for (int i = 1; i < paramArray.Length; i++)
                {
                    result += ToOperand(paramArray[i], fileCount);
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
                string label = str;

                if(!kuexok.ContainsKey(label))
                {
                    label = $"{label}@{fileCount}";
                }

                if(this.labels.ContainsKey(label))
                {
                    return this.labels[label];
                }
                else
                {
                    var jumpLabel = new JumpLabel();
                    this.labels.Add(label, jumpLabel);
                    return jumpLabel;
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
                        if (!code.Middle.HasLabel && !code.Middle.IsAddress
                            && code.Middle.First == Register.XX && code.Middle.Second == null && code.Middle.Immidiate == 0)
                        {
                            if(!code.Head.IsAddress && !code.Head.HasLabel && code.Head.Immidiate == TVARLON_KNLOAN_ADDRESS)
                            {
                                Klon(0xFF, Seti(F5 + 4));
                                Krz(code.Middle, code.Tail);
                            }
                            else
                            {
                                Fnx(code.Head, code.Tail);
                            }
                        }
                        else
                        {
                            if (code.Head.First == Register.XX || code.Head.Second == Register.XX)
                            {
                                if(code.Head.IsAddress)
                                {
                                    Operand newHead = ToRegisterOperand(code.Head.First.Value) + code.Head.Immidiate + 16;
                                    if(code.Head.Second.HasValue)
                                    {
                                        newHead += ToRegisterOperand(code.Head.Second.Value);
                                    }

                                    code.Head = Seti(newHead);
                                }
                                else
                                {
                                    code.Head += 16;
                                }
                            }

                            if (code.Middle.First == Register.XX || code.Middle.Second == Register.XX)
                            {
                                if (code.Middle.IsAddress)
                                {
                                    Operand newMiddle = ToRegisterOperand(code.Middle.First.Value) + code.Middle.Immidiate + 16;
                                    if (code.Middle.Second.HasValue)
                                    {
                                        newMiddle += ToRegisterOperand(code.Middle.Second.Value);
                                    }

                                    code.Middle = Seti(newMiddle);
                                }
                                else
                                {
                                    code.Middle += 16;
                                }
                            }

                            Mte(code.Head, code.Middle);
                            Anf(code.Middle, code.Tail);
                        }
                        break;
                    case LkMnemonic.LAT:
                        Lat(code.Head, code.Middle);
                        Anf(code.Tail, code.Middle);
                        break;
                    case LkMnemonic.LATSNA:
                        Latsna(code.Head, code.Middle);
                        Anf(code.Tail, code.Middle);
                        break;
                    case LkMnemonic.NLL:
                        Nll(code.Label);
                        break;
                    case LkMnemonic.LIFEM:
                        if(code.Head is JumpLabel)
                        {
                            Lifem(code.Head as JumpLabel);
                        }
                        else
                        {
                            Lifem(code.Head.Immidiate);
                        }
                        break;
                    case LkMnemonic.LIFEM8:
                        if (code.Head is JumpLabel)
                        {
                            Lifem8(code.Head as JumpLabel);
                        }
                        else
                        {
                            Lifem8(code.Head.Immidiate);
                        }
                        break;
                    case LkMnemonic.LIFEM16:
                        if (code.Head is JumpLabel)
                        {
                            Lifem16(code.Head as JumpLabel);
                        }
                        else
                        {
                            Lifem16(code.Head.Immidiate);
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
        
        #endregion
    }
}
