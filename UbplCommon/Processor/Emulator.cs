using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UbplCommon.Processor
{
    public class Emulator
    {
        #region Fields

        /// <summary>
        /// メモリ
        /// </summary>
        readonly Memory _memory;

        /// <summary>
        /// ジャンプフラグ
        /// </summary>
        bool _compareFlag;

        /// <summary>
        /// 汎用レジスタ
        /// </summary>
        readonly RegisterTable _registers;

        /// <summary>
        /// デバッグ用出力バッファ
        /// </summary>
        readonly List<string> _debugBuffer;

        /// <summary>
        /// MTEのheadに設定された値を保持する内部レジスタ
        /// </summary>
        uint _headTemporary;

        /// <summary>
        /// MTEのtailに設定された値を保持する内部レジスタ
        /// </summary>
        uint _tailTemporary;

        /// <summary>
        /// F5レジスタのデフォルト値
        /// </summary>
        readonly uint _initialStackAddress;

        /// <summary>
        /// NXレジスタのデフォルト値
        /// </summary>
        readonly uint _initialProgramAddress;

        /// <summary>
        /// アプリケーションのリターンアドレス
        /// </summary>
        readonly uint _returnAddress;

        #endregion

        #region Properties

        /// <summary>
        /// メモリの内容を表すDictionaryを返す．読み込み専用
        /// </summary>
        public IReadOnlyDictionary<uint, uint> Memory
        {
            get => _memory.Binaries;
        }

        /// <summary>
        /// メモリの内容を表示するかどうか
        /// </summary>
        public bool ViewMemory { get; set; }

        /// <summary>
        /// レジスタの内容を表示するかどうか
        /// </summary>
        public bool ViewRegister { get; set; }

        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Emulator() : this(UbplConstants.DEFAULT_INITIAL_F5,
            UbplConstants.DEFAULT_INITIAL_NX, UbplConstants.DEFAULT_RETURN_ADDRESS) { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="initialStackAddress">初期スタックアドレス</param>
        /// <param name="initialProgramAddress">初期プログラムアドレス</param>
        /// <param name="returnAddress">プログラムのリターンアドレス</param>
        public Emulator(uint initialStackAddress, uint initialProgramAddress, uint returnAddress)
        {
            _memory = new Memory();
            _compareFlag = false;
            _debugBuffer = new List<string>();

            _initialStackAddress = initialStackAddress;
            _initialProgramAddress = initialProgramAddress;
            _returnAddress = returnAddress;

            Random random = new Random();

            _registers = new RegisterTable
            {
                F0 = (uint)random.Next(int.MinValue, int.MaxValue),
                F1 = (uint)random.Next(int.MinValue, int.MaxValue),
                F2 = (uint)random.Next(int.MinValue, int.MaxValue),
                F3 = (uint)random.Next(int.MinValue, int.MaxValue),
                F4 = (uint)random.Next(int.MinValue, int.MaxValue),
                F5 = _initialStackAddress,
                F6 = (uint)random.Next(int.MinValue, int.MaxValue),
                XX = _initialProgramAddress,
            };

            _memory[_initialStackAddress] = _returnAddress;
        }

        /// <summary>
        /// バイナリコードを読み込みます．
        /// </summary>
        /// <param name="binary">ubplバイナリデータ</param>
        public void Read(IEnumerable<byte> binary)
        {
            Read(binary.ToArray());
        }

        /// <summary>
        /// バイナリコードを読み込みます．
        /// </summary>
        /// <param name="binary">ubplバイナリデータ</param>
        public void Read(byte[] binary)
        {
            CheckBinary(binary.LongLength);

            uint nowAddress = _initialProgramAddress;
            for (int i = 0; i < binary.Length; i += 4)
            {
                _memory[nowAddress] = (uint)((binary[i] << 24) | (binary[i + 1] << 16) | (binary[i + 2] << 8) | binary[i + 3]);
                nowAddress += 4;
            }
        }

        /// <summary>
        /// バイナリコードを読み込みます．
        /// </summary>
        /// <param name="binary">ubplバイナリデータ</param>
        public void Read(ReadOnlySpan<byte> binary)
        {
            CheckBinary(binary.Length);

            uint nowAddress = _initialProgramAddress;
            WriteMemory(binary, ref nowAddress);
        }

        /// <summary>
        /// 指定された名称のファイルパスからバイナリコードを読み込みます．
        /// </summary>
        /// <param name="filepath">2003fバイナリデータを保持するファイルのパス</param>
        public void Read(string filepath)
        {
            const int BUFFER_SIZE = 128;
            Span<byte> buffer = stackalloc byte[BUFFER_SIZE];

            using var file = File.OpenRead(filepath);
            long length = file.Length;
            uint nowAddress = _initialProgramAddress;

            CheckBinary(length);

            while (length > 0)
            {
                int readSize = file.Read(buffer);
                WriteMemory(buffer, ref nowAddress);

                length -= readSize;
            }
        }

        private void CheckBinary(long length)
        {
            if ((_initialProgramAddress + length) >= uint.MaxValue)
            {
                throw new ApplicationException("Too Large Programme");
            }
            else if ((length & 0x3) != 0)
            {
                throw new ApplicationException("Illegal binary: ubpl binary must be allocated in units of 4 bytes.");
            }
        }

        private void WriteMemory(ReadOnlySpan<byte> buffer, ref uint address)
        {
            for (int i = 0; i < buffer.Length; i += 4)
            {
                _memory[address] = (uint)((buffer[i] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8) | (buffer[i + 3]));
                address += 4U;
            }
        }

        /// <summary>
        /// 読み込んだバイナリを実行します．
        /// </summary>
        public void Run()
        {
            try
            {
                uint address;

                while ((address = _registers[Register.XX]) != _returnAddress)
                {
                    Mnemonic code = (Mnemonic)_memory[address];
                    address += 4;

                    ModRm modrm = new ModRm(_memory[address]);
                    address += 4;

                    uint head = _memory[address];
                    address += 4;

                    uint tail = _memory[address];
                    address += 4;

                    _registers[Register.XX] = address;

                    switch (code)
                    {
                        case Mnemonic.KRZ:
                            Krz(modrm, head, tail);
                            break;
                        case Mnemonic.MALKRZ:
                            Malkrz(modrm, head, tail);
                            break;
                        case Mnemonic.KRZ8I:
                            Krz8i(modrm, head, tail);
                            break;
                        case Mnemonic.KRZ16I:
                            Krz16i(modrm, head, tail);
                            break;
                        case Mnemonic.KRZ8C:
                            Krz8c(modrm, head, tail);
                            break;
                        case Mnemonic.KRZ16C:
                            Krz16c(modrm, head, tail);
                            break;
                        case Mnemonic.ATA:
                            Ata(modrm, head, tail);
                            break;
                        case Mnemonic.NTA:
                            Nta(modrm, head, tail);
                            break;
                        case Mnemonic.ADA:
                            Ada(modrm, head, tail);
                            break;
                        case Mnemonic.EKC:
                            Ekc(modrm, head, tail);
                            break;
                        case Mnemonic.DTO:
                            Dto(modrm, head, tail);
                            break;
                        case Mnemonic.DRO:
                            Dro(modrm, head, tail);
                            break;
                        case Mnemonic.DTOSNA:
                            Dtosna(modrm, head, tail);
                            break;
                        case Mnemonic.DAL:
                            Dal(modrm, head, tail);
                            break;
                        case Mnemonic.MTE:
                            Mte(modrm, head, tail);
                            break;
                        case Mnemonic.ANF:
                            Anf(modrm, head, tail);
                            break;
                        case Mnemonic.CLO:
                            Clo(modrm, head, tail);
                            break;
                        case Mnemonic.NIV:
                            Niv(modrm, head, tail);
                            break;
                        case Mnemonic.XTLONYS:
                            Xtlonys(modrm, head, tail);
                            break;
                        case Mnemonic.XYLONYS:
                            Xylonys(modrm, head, tail);
                            break;
                        case Mnemonic.XTLO:
                            Xtlo(modrm, head, tail);
                            break;
                        case Mnemonic.XYLO:
                            Xylo(modrm, head, tail);
                            break;
                        case Mnemonic.LAT:
                            Lat(modrm, head, tail);
                            break;
                        case Mnemonic.LATSNA:
                            Latsna(modrm, head, tail);
                            break;
                        case Mnemonic.KAK:
                            Kak(modrm, head, tail);
                            break;
                        case Mnemonic.KAKSNA:
                            Kaksna(modrm, head, tail);
                            break;
                        case Mnemonic.FNX:
                            Fnx(modrm, head, tail);
                            break;
                        case Mnemonic.KLON:
                            Klon(modrm, head, tail);
                            break;
                        default:
                            throw new NotImplementedException($"Not Implemented: {code:X}, nx = {(_registers[Register.XX] - 16):X08}");
                    }

                    OutputEmulatorValues();
                }

                if (_debugBuffer.Any())
                {
                    Console.WriteLine("[{0}]", string.Join(",", _debugBuffer));
                }
            }
            catch (Exception ex)
            {
                OutputEmulatorValues();
                if (_debugBuffer.Any())
                {
                    Console.WriteLine("[{0}]", string.Join(",", _debugBuffer));
                }

                throw new Exception("Emulator error", ex);
            }
        }

        private void OutputEmulatorValues()
        {
            if (ViewRegister)
            {
                for (int i = 0; i < _registers.Count; i++)
                {
                    Register register = (Register)i;
                    Console.WriteLine("{0} = {1:X08}", register, _registers[register]);
                }
            }

            if (ViewMemory)
            {
                int itemCount = 0;
                uint prevKey = 0;
                foreach (var item in _memory.Binaries.OrderBy(x => x.Key))
                {
                    if (itemCount % 4 == 0)
                    {
                        Console.WriteLine();
                        Console.Write("{0:X08}: {1:X08}", item.Key, item.Value);
                        itemCount = 0;
                    }
                    else if (prevKey != (item.Key - 4))
                    {
                        for (int i = itemCount; i < 4; i++)
                        {
                            Console.Write(" 00000000");
                        }
                        Console.WriteLine();

                        var topAddress = item.Key & 0xFFFFFFF4;
                        Console.Write("{0:X08}:", topAddress);

                        itemCount = (int)(item.Key - topAddress) / 4;
                        for (int i = 0; i < itemCount; i++)
                        {
                            Console.Write(" 00000000");
                        }

                        Console.Write(" {0:X08}", item.Value);
                    }
                    else
                    {
                        Console.Write(" {1:X08}", item.Key, item.Value);
                    }
                    itemCount++;
                    prevKey = item.Key;
                }
                if (itemCount % 4 != 0)
                {
                    Console.WriteLine();
                }
            }

            if (ViewRegister || ViewMemory)
            {
                Console.WriteLine();
            }
        }

        #region ModRM

        uint GetValue8(OperandMode mode, Register fir1, Register reg2, uint imm)
        {
            return GetValue(mode, fir1, reg2, imm, ValueSize.BYTE);
        }

        uint GetValue16(OperandMode mode, Register fir1, Register reg2, uint imm)
        {
            return GetValue(mode, fir1, reg2, imm, ValueSize.WORD);
        }

        uint GetValue32(OperandMode mode, Register fir1, Register reg2, uint imm)
        {
            return GetValue(mode, fir1, reg2, imm, ValueSize.DWORD);
        }

        private uint GetValue(OperandMode mode, Register reg1, Register reg2, uint imm, ValueSize size)
        {
            OperandMode operandPattern = GetOperandPattern(mode);
            uint result;
            if (_registers.TryGetValue(reg1, out uint reg1Value) && _registers.TryGetValue(reg2, out uint reg2Value)) {
                result = operandPattern switch
                {
                    OperandMode.REG => reg1Value,
                    OperandMode.IMM => imm,
                    OperandMode.IMM_REG => imm + reg1Value,
                    OperandMode.IMM_NREG => imm - reg1Value,
                    OperandMode.IMM_REG_REG => imm + reg1Value + reg2Value,
                    OperandMode.IMM_REG_NREG => imm + reg1Value - reg2Value,
                    OperandMode.IMM_NREG_REG => imm - reg1Value + reg2Value,
                    OperandMode.IMM_NREG_NREG => imm - reg1Value + reg2Value,
                    _ => throw new Exception($"invalid operand mode : {operandPattern}"),
                };
            }
            else
            {
                throw new Exception($"invalid register : {operandPattern}, {reg1}, {reg2}");
            }

            if (mode.HasFlag(OperandMode.ADDRESS))
            {
                return _memory[result, size];
            }
            else
            {
                return size switch
                {
                    ValueSize.BYTE => (result >> 24) | ((~(result >> 31) + 1) << 8),
                    ValueSize.WORD => (result >> 16) | ((~(result >> 31) + 1) << 16),
                    ValueSize.DWORD => result,
                    _ => throw new Exception($"invalid value size : {size}"),
                };
            }
        }

        void SetValue8(OperandMode mode, Register reg1, Register reg2, uint imm, uint value)
        {
            SetValue(mode, reg1, reg2, imm, value, ValueSize.BYTE);
        }

        void SetValue16(OperandMode mode, Register reg1, Register reg2, uint imm, uint value)
        {
            SetValue(mode, reg1, reg2, imm, value, ValueSize.WORD);
        }

        void SetValue32(OperandMode mode, Register reg1, Register reg2, uint imm, uint value)
        {
            SetValue(mode, reg1, reg2, imm, value, ValueSize.DWORD);
        }

        private void SetValue(OperandMode mode, Register reg1, Register reg2, uint imm, uint value, ValueSize size)
        {
            if (mode.HasFlag(OperandMode.ADDRESS))
            {
                OperandMode operandPattern = GetOperandPattern(mode);
                uint address;
                if (_registers.TryGetValue(reg1, out uint reg1Value) && _registers.TryGetValue(reg2, out uint reg2Value))
                {
                    address = operandPattern switch
                    {
                        OperandMode.REG => reg1Value,
                        OperandMode.IMM => imm,
                        OperandMode.IMM_REG => imm + reg1Value,
                        OperandMode.IMM_NREG => imm - reg1Value,
                        OperandMode.IMM_REG_REG => imm + reg1Value + reg2Value,
                        OperandMode.IMM_REG_NREG => imm + reg1Value - reg2Value,
                        OperandMode.IMM_NREG_REG => imm - reg1Value + reg2Value,
                        OperandMode.IMM_NREG_NREG => imm - reg1Value + reg2Value,
                        _ => throw new Exception($"invalid operand mode : {operandPattern}"),
                    };
                }
                else
                {
                    throw new Exception($"invalid register : {operandPattern}, {reg1}, {reg2}");
                }

                _memory[address, size] = value;
            }
            else
            {
                if (mode != OperandMode.REG)
                {
                    throw new Exception($"invalid operand mode : {mode}");
                }

                _registers[reg1] = size switch
                {
                    ValueSize.BYTE => (_registers[reg1] & 0x00FFFFFFU) | ((value & 0xFFU) << 24),
                    ValueSize.WORD => (_registers[reg1] & 0x0000FFFFU) | ((value & 0xFFFFU) << 16),
                    ValueSize.DWORD => value,
                    _ => throw new Exception($"invalid value size : {size}"),
                };
            }
            _compareFlag = false;
        }

        private OperandMode GetOperandPattern(OperandMode mode)
        {
            return (OperandMode)((uint)mode & 0x07);
        }

        #endregion

        #region Operators

        /// <summary>
        /// ataの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Ata(ModRm modrm, uint head, uint tail)
        {
            OperandMode tailMode = modrm.TailMode;
            Register tailReg1 = modrm.TailReg1;
            Register tailReg2 = modrm.TailReg2;

            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(tailMode, tailReg1, tailReg2, tail);

            SetValue32(tailMode, tailReg1, tailReg2, tail, tailValue + headValue);
        }

        /// <summary>
        /// ntaの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Nta(ModRm modrm, uint head, uint tail)
        {
            OperandMode tailMode = modrm.TailMode;
            Register tailReg1 = modrm.TailReg1;
            Register tailReg2 = modrm.TailReg2;

            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(tailMode, tailReg1, tailReg2, tail);

            SetValue32(tailMode, tailReg1, tailReg2, tail, tailValue - headValue);
        }

        /// <summary>
        /// adaの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Ada(ModRm modrm, uint head, uint tail)
        {
            OperandMode tailMode = modrm.TailMode;
            Register tailReg1 = modrm.TailReg1;
            Register tailReg2 = modrm.TailReg2;

            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(tailMode, tailReg1, tailReg2, tail);

            SetValue32(tailMode, tailReg1, tailReg2, tail, tailValue & headValue);
        }

        /// <summary>
        /// ekcの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Ekc(ModRm modrm, uint head, uint tail)
        {
            OperandMode tailMode = modrm.TailMode;
            Register tailReg1 = modrm.TailReg1;
            Register tailReg2 = modrm.TailReg2;

            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(tailMode, tailReg1, tailReg2, tail);

            SetValue32(tailMode, tailReg1, tailReg2, tail, tailValue | headValue);
        }

        /// <summary>
        /// dtoの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Dto(ModRm modrm, uint head, uint tail)
        {
            OperandMode tailMode = modrm.TailMode;
            Register tailReg1 = modrm.TailReg1;
            Register tailReg2 = modrm.TailReg2;

            int headValue = (int)GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(tailMode, tailReg1, tailReg2, tail);

            SetValue32(tailMode, tailReg1, tailReg2, tail, headValue > 31 ? 0 : tailValue >> headValue);
        }

        /// <summary>
        /// droの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Dro(ModRm modrm, uint head, uint tail)
        {
            OperandMode tailMode = modrm.TailMode;
            Register tailReg1 = modrm.TailReg1;
            Register tailReg2 = modrm.TailReg2;

            int headValue = (int)GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(tailMode, tailReg1, tailReg2, tail);

            SetValue32(tailMode, tailReg1, tailReg2, tail, headValue > 31 ? 0 : tailValue << headValue);
        }

        /// <summary>
        /// dtosnaの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Dtosna(ModRm modrm, uint head, uint tail)
        {
            OperandMode tailMode = modrm.TailMode;
            Register tailReg1 = modrm.TailReg1;
            Register tailReg2 = modrm.TailReg2;

            int headValue = (int)GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            int tailValue = (int)GetValue32(tailMode, tailReg1, tailReg2, tail);

            SetValue32(tailMode, tailReg1, tailReg2, tail, (uint)(tailValue >> (headValue > 31 ? 31 : headValue)));
        }

        /// <summary>
        /// dalの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Dal(ModRm modrm, uint head, uint tail)
        {
            OperandMode tailMode = modrm.TailMode;
            Register tailReg1 = modrm.TailReg1;
            Register tailReg2 = modrm.TailReg2;

            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(tailMode, tailReg1, tailReg2, tail);

            SetValue32(tailMode, tailReg1, tailReg2, tail, ~(tailValue ^ headValue));
        }

        /// <summary>
        /// krzの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Krz(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);

            SetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, headValue);
        }

        /// <summary>
        /// malkrzの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Malkrz(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);

            if (_compareFlag)
            {
                SetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, headValue);
            }
        }

        /// <summary>
        /// krz8iの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Krz8i(ModRm modrm, uint head, uint tail)
        {
            int headValue = (int)GetValue8(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);

            SetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, (uint)((headValue << 24) >> 24));
        }

        /// <summary>
        /// krz16iの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Krz16i(ModRm modrm, uint head, uint tail)
        {
            int headValue = (int)GetValue16(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);

            SetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, (uint)((headValue << 16) >> 16));
        }

        /// <summary>
        /// krz8cの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Krz8c(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);

            SetValue8(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, headValue);
        }

        /// <summary>
        /// krz16cの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Krz16c(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);

            SetValue16(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, headValue);
        }

        /// <summary>
        /// mteの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Mte(ModRm modrm, uint head, uint tail)
        {
            _headTemporary = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            _tailTemporary = GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);
        }

        /// <summary>
        /// anfの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Anf(ModRm modrm, uint head, uint tail)
        {
            SetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head, _headTemporary);
            SetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, _tailTemporary);
        }

        /// <summary>
        /// fi cloの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Clo(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            _compareFlag = headValue == tailValue;
        }

        /// <summary>
        /// fi nivの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Niv(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            _compareFlag = headValue != tailValue;
        }

        /// <summary>
        /// fi xtlonysの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Xtlonys(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            _compareFlag = headValue <= tailValue;
        }

        /// <summary>
        /// fi xylonysの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Xylonys(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            uint tailValue = GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            _compareFlag = headValue < tailValue;
        }

        /// <summary>
        /// fi xtloの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Xtlo(ModRm modrm, uint head, uint tail)
        {
            int headValue = (int)GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            int tailValue = (int)GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            _compareFlag = headValue <= tailValue;
        }

        /// <summary>
        /// fi xylonysの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Xylo(ModRm modrm, uint head, uint tail)
        {
            int headValue = (int)GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            int tailValue = (int)GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            _compareFlag = headValue < tailValue;
        }

        /// <summary>
        /// latの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Lat(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            ulong tailValue = GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            ulong temp = headValue * tailValue;
            _headTemporary = (uint)temp;
            _tailTemporary = (uint)(temp >> 32);
        }

        /// <summary>
        /// latsnaの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Latsna(ModRm modrm, uint head, uint tail)
        {
            int headValue = (int)GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            long tailValue = (int)GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            long temp = headValue * tailValue;
            _headTemporary = (uint)temp;
            _tailTemporary = (uint)(temp >> 32); ;
        }

        /// <summary>
        /// kakの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Kak(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            ulong tailValue = GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            ulong temp = headValue / tailValue;
            _headTemporary = (uint)temp;
            _tailTemporary = (uint)(temp >> 32);
        }

        /// <summary>
        /// kaksnaの処理を行います．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Kaksna(ModRm modrm, uint head, uint tail)
        {
            int headValue = (int)GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);
            long tailValue = (int)GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);

            long temp = headValue / tailValue;
            _headTemporary = (uint)temp;
            _tailTemporary = (uint)(temp >> 32); ;
        }

        /// <summary>
        /// fnxの処理を行います．
        /// これは"inj op1 xx op2"の動作と等しくなります．
        /// </summary>
        /// <param name="modrm">ModRM</param>
        /// <param name="head">第一即値</param>
        /// <param name="tail">第二即値</param>
        void Fnx(ModRm modrm, uint head, uint tail)
        {
            uint value = _registers[Register.XX];
            _registers[Register.XX] = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);

            SetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, value);
        }

        void Klon(ModRm modrm, uint head, uint tail)
        {
            uint headValue = GetValue32(modrm.HeadMode, modrm.HeadReg1, modrm.HeadReg2, head);

            switch (headValue)
            {
                case 0x76:
                    {

                        int value = Console.Read();

                        if (value == '\r' || value == '\n')
                        {
                            value = Console.In.Peek();
                            if (value == '\n')
                            {
                                Console.Read();
                            }

                            value = Console.Read();
                        }

                        SetValue8(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail, CharacterCode.ToByte((char)value));
                    }
                    break;
                case 0x81:
                    {
                        uint tailValue = GetValue8(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);
                        char c = CharacterCode.ToChar(tailValue);
                        Console.Write(c == '\n' ? Environment.NewLine : c.ToString());
                    }
                    break;
                case 0xFF:
                    {
                        uint tailValue = GetValue32(modrm.TailMode, modrm.TailReg1, modrm.TailReg2, tail);
                        _debugBuffer.Add(tailValue.ToString());
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
