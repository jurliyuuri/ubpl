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

        private readonly IList<string> _inFiles;
        private readonly IDictionary<string, bool> _kuexok;
        private readonly IDictionary<string, JumpLabel> _labels;
        private readonly IList<LkCode> _labelLifemList;

        public LkAssembler(List<string> inFiles) : base()
        {
            _inFiles = inFiles;
            _kuexok = new Dictionary<string, bool>();
            _labels = new Dictionary<string, JumpLabel>();
            _labelLifemList = new List<LkCode>();
        }

        public void Execute(string outFile)
        {
            List<LkCode> codeList = new List<LkCode>();
            int count = 0;
            bool hasMain = false;

            foreach (var inFile in _inFiles)
            {
                IList<string> wordList = Read(inFile);

                if (IsDebug)
                {
                    Console.WriteLine("wordList: {0}", string.Join(",", wordList));
                }

                AnalyzeLabel(wordList);
                IList<LkCode> lkCodes = Analyze(wordList, count++, out bool isMain);

                if (isMain)
                {
                    if (hasMain)
                    {
                        throw new ApplicationException("already define main");
                    }
                    else
                    {
                        codeList.InsertRange(0, lkCodes);
                        hasMain = true;
                    }
                }
                else
                {
                    codeList.AddRange(lkCodes);
                }
            }

            if (IsDebug)
            {
                Console.WriteLine("codeList: {0}", string.Join(",", codeList));
            }

            codeList.InsertRange(0, _labelLifemList);
            Create(codeList);
            Write(outFile);
        }

        #region 共通
        private Operand ToRegisterOperand(Register register)
        {
            var operand = register switch
            {
                Register.F0 => F0,
                Register.F1 => F1,
                Register.F2 => F2,
                Register.F3 => F3,
                Register.F4 => F4,
                Register.F5 => F5,
                Register.F6 => F6,
                Register.XX => XX,
                _ => throw new NotSupportedException($"Not Supported register: {register}"),
            };
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

                        _kuexok[label] = true;
                        break;
                    case "xok":
                        label = wordList[++i];

                        if (!_kuexok.ContainsKey(label))
                        {
                            _kuexok[label] = false;
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

        private IList<LkCode> Analyze(IList<string> wordList, int fileCount, out bool isMain)
        {
            List<LkCode> codeList = new List<LkCode>();
            bool isCI = false;
            isMain = true;

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
                                Head = GetLabel(label, fileCount),
                                Tail = ZERO,
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

                            codeList.Insert(codeList.Count - 1, new LkCode
                            {
                                Mnemonic = LkMnemonic.NLL,
                                Head = GetLabel(label, fileCount),
                                Tail = ZERO,
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

                            if (opd is JumpLabel jumpLabel)
                            {
                                var lifemValueLabel = new JumpLabel();

                                codeList.Add(new LkCode
                                {
                                    Mnemonic = LkMnemonic.NLL,
                                    Head = lifemValueLabel,
                                    Tail = ZERO,
                                });
                                codeList.Add(new LkCode
                                {
                                    Mnemonic = Enum.Parse<LkMnemonic>(str, true),
                                    Head = ZERO,
                                    Tail = ZERO,
                                });
                                _labelLifemList.Add(new LkCode
                                {
                                    Mnemonic = str switch
                                    {
                                        "lifem8" => LkMnemonic.KRZ8C,
                                        "lifem16" => LkMnemonic.KRZ16C,
                                        _ => LkMnemonic.KRZ,
                                    },
                                    Head = jumpLabel,
                                    Tail = Seti(lifemValueLabel),
                                });
                            }
                            else if (!opd.FirstRegister.HasValue && !opd.SecondRegister.HasValue)
                            {
                                codeList.Add(new LkCode
                                {
                                    Mnemonic = Enum.Parse<LkMnemonic>(str, true),
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
                                Mnemonic = Enum.Parse<LkMnemonic>(str, true),
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
                                Mnemonic = Enum.Parse<LkMnemonic>(str, true),
                                Head = Convert(head, fileCount),
                                Middle = Convert(middle, fileCount),
                                Tail = Convert(tail, fileCount),
                            });
                            break;
                        case "nac":
                            codeList.Add(new LkCode
                            {
                                Mnemonic = LkMnemonic.DAL,
                                Head = ToOperand(0),
                                Tail = Convert(wordList[++i], fileCount),
                            });
                            break;
                        case "fi":
                            head = wordList[++i];
                            tail = wordList[++i];
                            if (Enum.TryParse(wordList[++i], true, out LkMnemonic mne))
                            {
                                codeList.Add(new LkCode
                                {
                                    Mnemonic = mne,
                                    Head = Convert(head, fileCount),
                                    Tail = Convert(tail, fileCount),
                                });
                            }
                            else
                            {
                                throw new ApplicationException($"Invalid constant value: {wordList[i]}");
                            }
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
            Operand result;
            bool seti = str.Last() == '@';
            ReadOnlySpan<char> span = str.AsSpan(0, seti ? str.Length - 1 : str.Length);
            int nextIndex = span.IndexOf('+');

            if (nextIndex != -1)
            {
                result = GetOperand(span.Slice(0, nextIndex), fileCount) + GetOperand(span.Slice(nextIndex + 1), fileCount);
            }
            else
            {
                result = GetOperand(span, fileCount);
            }

            if (seti)
            {
                return Seti(result);
            }
            else
            {
                return result;
            }
        }

        Operand GetOperand(ReadOnlySpan<char> span, int fileCount)
        {
            if (uint.TryParse(span, out uint val))
            {
                return ToOperand(val);
            }
            else if (TryGetRegisterOperand(span, out Operand operand))
            {
                return operand;
            }
            else
            {
                string str = span.ToString();
                return GetLabel(str, fileCount);
            }
        }

        private bool TryGetRegisterOperand(ReadOnlySpan<char> span, out Operand register)
        {
            register = null!;

            if (span.Length != 2)
            {
                return false;
            }

            if (span[0] == 'x' && span[1] == 'x')
            {
                register = Operand.XX;
                return true;
            }
            else if (span[0] == 'f')
            {
                register = span[1] switch
                {
                    '0' => Operand.F0,
                    '1' => Operand.F1,
                    '2' => Operand.F2,
                    '3' => Operand.F3,
                    '4' => Operand.F4,
                    '5' => Operand.F5,
                    '6' => Operand.F6,
                    _ => null!,
                };

                return true;
            }
            else
            {
                return false;
            }
        }

        JumpLabel GetLabel(string labelName, int fileCount)
        {
            if (!_kuexok.ContainsKey(labelName))
            {
                labelName = $"{labelName}@{fileCount}";
            }

            if (_labels.ContainsKey(labelName))
            {
                return _labels[labelName];
            }
            else
            {
                var jumpLabel = new JumpLabel();
                _labels.Add(labelName, jumpLabel);
                return jumpLabel;
            }
        }

        #endregion

        #region バイナリ作成

        private void Create(IList<LkCode> codeList)
        {
            foreach (var code in codeList)
            {
                if (code.Head == null)
                {
                    throw new ApplicationException($"Illegal operand: head is null. {code}");
                }

                if (code.Tail == null)
                {
                    throw new ApplicationException($"Illegal operand: tail is null. {code}");
                }

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

                        if (code.Middle == null)
                        {
                            throw new ApplicationException($"Illegal operand: middle is null. {code}");
                        }

                        if (!code.Middle.HasLabel && !code.Middle.IsAddressing
                            && code.Middle.FirstRegister == Register.XX && code.Middle.SecondRegister == null && code.Middle.Immidiate == 0)
                        {
                            if(!code.Head.IsAddressing && !code.Head.HasLabel && code.Head.Immidiate == TVARLON_KNLOAN_ADDRESS)
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
                            if (code.Head.FirstRegister == Register.XX || code.Head.SecondRegister == Register.XX)
                            {
                                if(code.Head.IsAddressing)
                                {
                                    Operand newHead = ToRegisterOperand(code.Head.FirstRegister!.Value)
                                        + code.Head.Immidiate + 16;
                                    if(code.Head.SecondRegister.HasValue)
                                    {
                                        newHead += ToRegisterOperand(code.Head.SecondRegister.Value);
                                    }

                                    code.Head = Seti(newHead);
                                }
                                else
                                {
                                    code.Head += 16;
                                }
                            }

                            if (code.Middle.FirstRegister == Register.XX || code.Middle.SecondRegister == Register.XX)
                            {
                                if (code.Middle.IsAddressing)
                                {
                                    Operand newMiddle = ToRegisterOperand(code.Middle.FirstRegister!.Value) + code.Middle.Immidiate + 16;
                                    if (code.Middle.SecondRegister.HasValue)
                                    {
                                        newMiddle += ToRegisterOperand(code.Middle.SecondRegister.Value);
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
                        if (code.Middle == null)
                        {
                            throw new ApplicationException("Illegal operand: middle is null");
                        }

                        Lat(code.Head, code.Middle);
                        Anf(code.Tail, code.Middle);
                        break;
                    case LkMnemonic.LATSNA:
                        if (code.Middle == null)
                        {
                            throw new ApplicationException($"Illegal operand: middle is null. {code}");
                        }

                        Latsna(code.Head, code.Middle);
                        Anf(code.Tail, code.Middle);
                        break;
                    case LkMnemonic.NLL:
                        if (code.Head is JumpLabel jumpLabel)
                        {
                            Nll(jumpLabel);
                        }
                        else
                        {
                            throw new ApplicationException("Illegal operand: nll's openrand is not label");
                        }
                        break;
                    case LkMnemonic.LIFEM:
                        Lifem(code.Head.Immidiate);
                        break;
                    case LkMnemonic.LIFEM8:
                        Lifem8(code.Head.Immidiate);
                        break;
                    case LkMnemonic.LIFEM16:
                        Lifem16(code.Head.Immidiate);
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
