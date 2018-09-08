﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon.Processor
{
    public class Emulator
    {
        #region Constants

        /// <summary>
        /// レジスタ数
        /// </summary>
        private static readonly int REGISTER_COUNT = UbplConstant.REGISTER_COUNT;

        /// <summary>
        /// 2003fのF5レジスタのデフォルト値
        /// </summary>
        private static readonly uint DEFAULT_INITIAL_F5 = UbplConstant.DEFAULT_INITIAL_F5;

        /// <summary>
        /// 2003fのNXレジスタのデフォルト値
        /// </summary>
        private static readonly uint DEFAULT_INITIAL_NX = UbplConstant.DEFAULT_INITIAL_NX;

        /// <summary>
        /// アプリケーションのリターンアドレス
        /// </summary>
        private static readonly uint DEFAULT_RETURN_ADDRESS = UbplConstant.DEFAULT_RETURN_ADDRESS;

        /// <summary>
        /// デバッグ用出力アドレス
        /// </summary>
        private static readonly uint TVARLON_KNLOAN_ADDRESS = UbplConstant.TVARLON_KNLOAN_ADDRESS;

        #endregion

        #region Fields

        /// <summary>
        /// メモリ
        /// </summary>
        readonly Memory memory;

        /// <summary>
        /// ジャンプフラグ
        /// </summary>
        bool flags;

        /// <summary>
        /// 汎用レジスタ
        /// </summary>
        readonly IDictionary<Register, uint> registers;

        /// <summary>
        /// デバッグ用出力バッファ
        /// </summary>
        List<string> debugBuffer;

        /// <summary>
        /// Lat系やKak系の値を一時保存するための変数
        /// </summary>
        ulong temporary;

        #endregion

        #region Properties

        /// <summary>
        /// メモリの内容を表すDictionaryを返す．読み込み専用
        /// </summary>
        public IReadOnlyDictionary<uint, uint> Memory
        {
            get => this.memory.Binaries;
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
        public Emulator()
        {
            this.memory = new Memory();
            this.flags = false;
            this.debugBuffer = new List<string>();

            this.registers = new Dictionary<Register, uint>
            {
                [Register.F0] = 0,
                [Register.F1] = 0,
                [Register.F2] = 0,
                [Register.F3] = 0,
                [Register.F4] = 0,
                [Register.F5] = DEFAULT_INITIAL_F5,
                [Register.F6] = 0,
                [Register.XX] = DEFAULT_INITIAL_NX,
                [Register.UL] = 0,
            };

            this.memory[DEFAULT_INITIAL_F5] = DEFAULT_RETURN_ADDRESS;
        }

        /// <summary>
        /// バイナリコードを読み込みます．
        /// </summary>
        /// <param name="binary">ubplバイナリデータ</param>
        public void Read(byte[] binary)
        {
            if (DEFAULT_INITIAL_NX + binary.LongLength >= (long)uint.MaxValue)
            {
                throw new ApplicationException("Too Large Programme");
            }

            uint u = DEFAULT_INITIAL_NX;
            for (int i = 0; i < binary.Length; i += 4)
            {
                this.memory[u] = (uint)((binary[i] << 24) | (binary[i + 1] << 16)
                    | (binary[i + 2] << 8) | binary[i + 3]);
                u += 4;
            }
        }

        /// <summary>
        /// 指定された名称のファイルパスからバイナリコードを読み込みます．
        /// </summary>
        /// <param name="filepath">2003fバイナリデータを保持するファイルのパス</param>
        public void Read(string filepath)
        {
            Read(File.ReadAllBytes(filepath));
        }


        /// <summary>
        /// 読み込んだバイナリコードを実行します．
        /// </summary>
        public void Run()
        {
            try
            {
                while (this.registers[Register.XX] != DEFAULT_RETURN_ADDRESS)
                {
                    if (this.registers[Register.XX] == TVARLON_KNLOAN_ADDRESS)
                    {
                        debugBuffer.Add(this.memory[this.registers[Register.F5] + 4].ToString());
                        this.registers[Register.XX] = this.memory[this.registers[Register.F5]];

                        continue;
                    }

                    Mnemonic code = (Mnemonic)this.memory[this.registers[Register.XX]];
                    //Console.WriteLine("nx = {0:X08}, code = {1:X08}", this.registers[Register.XX], code);

                    this.registers[Register.XX] += 4;

                    ModRm modrm = new ModRm(this.memory[this.registers[Register.XX]]);
                    this.registers[Register.XX] += 4;

                    uint first = this.memory[this.registers[Register.XX]];
                    this.registers[Register.XX] += 4;

                    uint second = this.memory[this.registers[Register.XX]];
                    this.registers[Register.XX] += 4;

                    #region コード分岐
                    switch (code)
                    {
                        case Mnemonic.ATA:
                            Ata(modrm, first, second);
                            break;
                        case Mnemonic.NTA:
                            Nta(modrm, first, second);
                            break;
                        case Mnemonic.ADA:
                            Ada(modrm, first, second);
                            break;
                        case Mnemonic.EKC:
                            Ekc(modrm, first, second);
                            break;
                        case Mnemonic.DTO:
                            Dto(modrm, first, second);
                            break;
                        case Mnemonic.DRO:
                            Dro(modrm, first, second);
                            break;
                        case Mnemonic.DTOSNA:
                            Dtosna(modrm, first, second);
                            break;
                        case Mnemonic.DAL:
                            Dal(modrm, first, second);
                            break;
                        case Mnemonic.KRZ:
                            Krz(modrm, first, second);
                            break;
                        case Mnemonic.MALKRZ:
                            Malkrz(modrm, first, second);
                            break;
                        case Mnemonic.LLONYS:
                            Llonys(modrm, first, second);
                            break;
                        case Mnemonic.XTLONYS:
                            Xtlonys(modrm, first, second);
                            break;
                        case Mnemonic.XOLONYS:
                            Xolonys(modrm, first, second);
                            break;
                        case Mnemonic.XYLONYS:
                            Xylonys(modrm, first, second);
                            break;
                        case Mnemonic.CLO:
                            Clo(modrm, first, second);
                            break;
                        case Mnemonic.NIV:
                            Niv(modrm, first, second);
                            break;
                        case Mnemonic.LLO:
                            Llo(modrm, first, second);
                            break;
                        case Mnemonic.XTLO:
                            Xtlo(modrm, first, second);
                            break;
                        case Mnemonic.XOLO:
                            Xolo(modrm, first, second);
                            break;
                        case Mnemonic.XYLO:
                            Xylo(modrm, first, second);
                            break;
                        case Mnemonic.FNX:
                            Fnx(modrm, first, second);
                            break;
                        case Mnemonic.MTE:
                            Mte(modrm, first, second);
                            break;
                        case Mnemonic.ANF:
                            Anf(modrm, first, second);
                            break;
                        case Mnemonic.LAT:
                            Lat(modrm, first, second);
                            break;
                        case Mnemonic.LATSNA:
                            Latsna(modrm, first, second);
                            break;
                        case Mnemonic.KAK:
                            Kak(modrm, first, second);
                            break;
                        case Mnemonic.KAKSNA:
                            Kaksna(modrm, first, second);
                            break;
                        default:
                            throw new NotImplementedException($"Not Implemented: {code:X}");
                    }

                    #endregion
                }

                if (ViewRegister)
                {
                    for (int i = 0; i < this.registers.Count; i++)
                    {
                        if (this.registers.ContainsKey((Register)i))
                        {
                            Console.WriteLine("{0} = {1:X08}", (Register)i, this.registers[(Register)i]);
                        }
                    }
                }

                if (ViewMemory)
                {
                    foreach (var item in this.memory.Binaries.OrderBy(x => x.Key))
                    {
                        Console.WriteLine("{0:X08}: {1:X08}", item.Key, item.Value);
                    }
                }

                Console.WriteLine("[{0}]", string.Join(",", this.debugBuffer));
            }
            catch (Exception ex)
            {
                if (ViewRegister)
                {
                    for (int i = 0; i < this.registers.Count; i++)
                    {
                        if (this.registers.ContainsKey((Register)i))
                        {
                            Console.WriteLine("{0} = {1:X08}", (Register)i, this.registers[(Register)i]);
                        }
                    }
                }

                if (ViewMemory)
                {
                    foreach (var item in this.memory.Binaries.OrderBy(x => x.Key))
                    {
                        Console.WriteLine("{0:X08}: {1:X08}", item.Key, item.Value);
                    }
                }

                Console.WriteLine("[{0}]", string.Join(",", this.debugBuffer));

                throw new Exception("Emulator error", ex);
            }
        }

        #region ModRm

        (uint, uint) GetValue(ModRm modrm, uint first, uint second)
        {
            return (Get(modrm.ModeHead, modrm.RegHead, first), Get(modrm.ModeTail, modrm.RegTail, second));

            uint Get(OperandMode mode, Register register, uint value)
            {
                uint result = 0;

                switch (mode)
                {
                    case OperandMode.REG32:
                        result = this.registers[register];
                        break;
                    case OperandMode.IMM32:
                        result = value;
                        break;
                    case OperandMode.REG32_REG32:
                        result = this.registers[register] + this.registers[(Register)value];
                        break;
                    case OperandMode.REG32_IMM32:
                        result = this.registers[register] + value;
                        break;
                    case OperandMode.ADDR_REG32:
                        result = this.memory[this.registers[register]];
                        break;
                    case OperandMode.ADDR_IMM32:
                        result = this.memory[value];
                        break;
                    case OperandMode.ADDR_REG32_REG32:
                        result = this.memory[this.registers[register] + this.registers[(Register)value]];
                        break;
                    case OperandMode.ADDR_REG32_IMM32:
                        result = this.memory[this.registers[register] + value];
                        break;
                    default:
                        break;
                }

                return result;
            }
        }

        void SetValue(OperandMode mode, Register register, uint tail, uint value)
        {
            switch (mode)
            {
                case OperandMode.REG32:
                    this.registers[register] = value;
                    break;
                case OperandMode.IMM32:
                    throw new ArgumentException("Operand mode is 'IMM32'");
                case OperandMode.REG32_REG32:
                    throw new ArgumentException("Operand mode is 'REG32_REG32'");
                case OperandMode.REG32_IMM32:
                    throw new ArgumentException("Operand mode is 'REG32_IMM32'");
                case OperandMode.ADDR_REG32:
                    this.memory[this.registers[register]] = value;
                    break;
                case OperandMode.ADDR_IMM32:
                    this.memory[tail] = value;
                    break;
                case OperandMode.ADDR_REG32_REG32:
                    this.memory[this.registers[register] + this.registers[(Register)tail]] = value;
                    break;
                case OperandMode.ADDR_REG32_IMM32:
                    this.memory[this.registers[register] + tail] = value;
                    break;
                default:
                    throw new ArgumentException($"Operand mode is Unknown ({mode})");
            }

            this.flags = false;
        }

        #endregion

        #region Operators

        /// <summary>
        /// ataの処理を行います．
        /// </summary>
        void Ata(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, tail + head);
        }

        /// <summary>
        /// ntaの処理を行います．
        /// </summary>
        void Nta(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, tail - head);
        }

        /// <summary>
        /// adaの処理を行います．
        /// </summary>
        void Ada(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, tail & head);
        }

        /// <summary>
        /// ekcの処理を行います．
        /// </summary>
        void Ekc(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, tail | head);
        }

        /// <summary>
        /// dtoの処理を行います．
        /// </summary>
        void Dto(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            uint value;
            int iHead = (int)head;
            if(iHead >= 32)
            {
                value = 0;
            }
            else
            {
                value = tail >> iHead;
            }

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, value);
        }

        /// <summary>
        /// droの処理を行います．
        /// </summary>
        void Dro(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            uint value;
            int iHead = (int)head;

            if (iHead >= 32)
            {
                value = 0;
            }
            else
            {
                value = tail << iHead;
            }

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, value);
        }

        /// <summary>
        /// drosnaの処理を行います．
        /// </summary>
        void Dtosna(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            uint value;
            int iHead = (int)head;

            if (iHead >= 32)
            {
                value = (uint)((int)tail >> 31);
            }
            else
            {
                value = (uint)((int)tail >> iHead);
            }

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, value);
        }

        /// <summary>
        /// dalの処理を行います．
        /// </summary>
        void Dal(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, ~(tail ^ head));
        }

        /// <summary>
        /// krzの処理を行います．
        /// </summary>
        void Krz(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            SetValue(modrm.ModeTail, modrm.RegTail, tmp, head);
        }

        /// <summary>
        /// malkrzの処理を行います．
        /// </summary>
        void Malkrz(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            if(this.flags)
            {
                SetValue(modrm.ModeTail, modrm.RegTail, tmp, head);
            }
        }

        /// <summary>
        /// fi llonysの処理を行います．
        /// </summary>
        void Llonys(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = head > tail;
        }

        /// <summary>
        /// fi xtlonysの処理を行います．
        /// </summary>
        void Xtlonys(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = head <= tail;
        }

        /// <summary>
        /// fi xolonysの処理を行います．
        /// </summary>
        void Xolonys(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = head >= tail;
        }

        /// <summary>
        /// fi xylonysの処理を行います．
        /// </summary>
        void Xylonys(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = head < tail;
        }

        /// <summary>
        /// fi cloの処理を行います．
        /// </summary>
        void Clo(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = head == tail;
        }

        /// <summary>
        /// fi nivの処理を行います．
        /// </summary>
        void Niv(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = head != tail;
        }

        /// <summary>
        /// lloの処理を行います．
        /// </summary>
        void Llo(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = (int)head > (int)tail;
        }

        /// <summary>
        /// xtloの処理を行います．
        /// </summary>
        void Xtlo(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = (int)head <= (int)tail;
        }

        /// <summary>
        /// xoloの処理を行います．
        /// </summary>
        void Xolo(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = (int)head >= (int)tail;
        }

        /// <summary>
        /// xyloの処理を行います．
        /// </summary>
        void Xylo(ModRm modrm, uint head, uint tail)
        {
            (head, tail) = GetValue(modrm, head, tail);

            this.flags = (int)head < (int)tail;
        }

        /// <summary>
        /// fnxの処理を行います．
        /// これは"inj op1 xx op2"の動作と等しくなります．
        /// </summary>
        void Fnx(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            uint xx = this.registers[Register.XX];
            (head, tail) = GetValue(modrm, head, tail);

            this.registers[Register.XX] = head;
            SetValue(modrm.ModeTail, modrm.RegTail, tmp, xx);
        }
        
        /// <summary>
        /// mteの処理を行います．
        /// krz64 tail &lt;&lt; 32 | head tmp と等しくなります．
        /// </summary>
        void Mte(ModRm modrm, uint head, uint tail)
        {
            uint originHead = head;
            uint originTail = tail;
            (head, tail) = GetValue(modrm, head, tail);

            this.temporary = ((ulong)tail << 32) | head;
        }

        /// <summary>
        /// latの処理を行います．
        /// </summary>
        void Lat(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            this.temporary = (ulong)tail * head;
        }

        /// <summary>
        /// latsnaの処理を行います．
        /// </summary>
        void Latsna(ModRm modrm, uint head, uint tail)
        {
            uint tmp = tail;
            (head, tail) = GetValue(modrm, head, tail);

            this.temporary = (ulong)((long)tail * head);
        }

        /// <summary>
        /// anfの処理を行います．
        /// krz ((tmp >> 32) & 0x0000FFFF) tail, krz (tmp & 0x0000FFFF) headと等しくなります．
        /// </summary>
        void Anf(ModRm modrm, uint head, uint tail)
        {
            SetValue(modrm.ModeTail, modrm.RegTail, tail, (uint)(this.temporary >> 32));
            SetValue(modrm.ModeHead, modrm.RegHead, head, (uint)(this.temporary));
        }

        /// <summary>
        /// kakの処理を行います．
        /// </summary>
        void Kak(ModRm modrm, uint head, uint tail)
        {

        }

        /// <summary>
        /// kaksnaの処理を行います．
        /// </summary>
        void Kaksna(ModRm modrm, uint head, uint tail)
        {

        }
        
        #endregion
    }
}
