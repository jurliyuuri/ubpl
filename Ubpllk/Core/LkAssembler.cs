using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UbplCommon;
using UbplCommon.Translator;

namespace Ubpllk.Core
{
    class LkAssembler : CodeGenerator
    {
        #region Constant

        /// <summary>
        /// デバッグ用出力アドレス
        /// </summary>
        const uint TVARLON_KNLOAN_ADDRESS = 3126834864;

        static LkAssembler() { }

        #endregion

        #region Properties

        public bool IsDebug { get; set; }

        #endregion

        readonly IList<string> _inFiles;
        readonly IDictionary<string, bool> _kuexok;
        readonly IDictionary<string, JumpLabel> _labels;

        public LkAssembler(List<string> inFiles) : base()
        {
            _inFiles = inFiles;
            _kuexok = new Dictionary<string, bool>();
            _labels = new Dictionary<string, JumpLabel>();
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

                if (buffer.Length > 0)
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
                    case "lat":
                    case "latsna":
                    case "fnx":
                    case "mte":
                    case "anf":
                        i += 2;
                        break;
                    case "fi":
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
            isMain = true;

            for (int i = 0; i < wordList.Count; i++)
            {
                string label;
                string head, tail;

                string str = wordList[i];
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

                        if (!opd.FirstRegister.HasValue && !opd.SecondRegister.HasValue && !opd.IsAddressing)
                        {
                            codeList.Add(new LkCode
                            {
                                Mnemonic = Enum.Parse<LkMnemonic>(str, true),
                                Head = opd,
                                Tail = ZERO,
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
                    case "mte":
                    case "anf":
                    case "fnx":
                    case "lat":
                    case "latsna":
                    case "kak":
                    case "kaksna":
                        head = wordList[++i];
                        tail = wordList[++i];

                        codeList.Add(new LkCode
                        {
                            Mnemonic = Enum.Parse<LkMnemonic>(str, true),
                            Head = Convert(head, fileCount),
                            Tail = Convert(tail, fileCount),
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
                    default:
                        break;
                }
            }

            return codeList;
        }

        private static readonly char[] OPERAND_OP = { '+', '-' };
        private Operand Convert(string str, int fileCount)
        {
            ReadOnlySpan<char> op = OPERAND_OP.AsSpan();
            bool seti = str.Last() == '@';
            Operand? result = null;
            Operand operand;
            ReadOnlySpan<char> span = str.AsSpan(0, seti ? str.Length - 1 : str.Length);
            int nextIndex = span.IndexOfAny(op);
            char key = '\0';

            while (nextIndex != -1)
            {
                operand = GetOperand(span.Slice(0, nextIndex), fileCount);

                if (key == '|')
                {
                    operand = -operand;
                }

                if (result == null)
                {
                    result = operand;
                }
                else
                {
                    result += operand;
                }

                key = span[nextIndex];
                span = span.Slice(nextIndex + 1);
                nextIndex = span.IndexOfAny(op);
            };

            operand = GetOperand(span, fileCount);

            if (key == '|')
            {
                operand = -operand;
            }

            if (result == null)
            {
                result = operand;
            }
            else
            {
                result += operand;
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
            if (uint.TryParse(span, out uint val)) {
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
                    throw new ApplicationException("Illegal operand: head is null");
                }

                if (code.Tail == null)
                {
                    throw new ApplicationException("Illegal operand: head is null");
                }

                switch (code.Mnemonic)
                {
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
                    case LkMnemonic.FNX:
                        Fnx(code.Head, code.Tail);
                        break;
                    case LkMnemonic.MTE:
                        Mte(code.Head, code.Tail);
                        break;
                    case LkMnemonic.ANF:
                        Anf(code.Head, code.Tail);
                        break;
                    case LkMnemonic.LAT:
                        Lat(code.Head, code.Tail);
                        break;
                    case LkMnemonic.LATSNA:
                        Latsna(code.Head, code.Tail);
                        break;
                    case LkMnemonic.KAK:
                        Kak(code.Head, code.Tail);
                        break;
                    case LkMnemonic.KAKSNA:
                        Kaksna(code.Head, code.Tail);
                        break;
                    case LkMnemonic.NLL:
                        if (code.Head is JumpLabel label)
                        {
                            Nll(label);
                        }
                        else
                        {
                            throw new ApplicationException($"cannot set \"{code.Head}\" in nll");
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
