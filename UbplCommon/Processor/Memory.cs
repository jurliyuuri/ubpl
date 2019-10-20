using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace UbplCommon.Processor
{
    public class Memory
    {
        /// <summary>
        /// メモリ内容
        /// </summary>
        private readonly IDictionary<uint, uint> memory;

        /// <summary>
        /// メモリが未初期化だった場合に設定されている値を作成するRandom
        /// </summary>
        readonly Random random;

        /// <summary>
        /// メモリの内容を表す読み込み専用のDictionary
        /// </summary>
        public IReadOnlyDictionary<uint, uint> Binaries
        {
            get => new ReadOnlyDictionary<uint, uint>(this.memory);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Memory()
        {
            this.memory = new Dictionary<uint, uint>();
            this.random = new Random();
        }

        /// <summary>
        /// 指定されたアドレスの値を返します．
        /// 未使用のアドレスが指定された場合にはランダムな値を返します．
        /// また，アドレスの下位2bitの値は無視されます．
        /// </summary>
        /// <param name="address">アドレス</param>
        /// <returns></returns>
        public uint this[uint address]
        {
            get
            {
                return GetValue32(address);
            }
            set
            {
                SetValue32(address, value);
            }
        }

        public byte GetValue8(uint address)
        {
            uint readAddress = address & 0xFFFFFFFCU;
            uint pos = address & 0x03U;

            if (!this.memory.ContainsKey(readAddress))
            {
                this.memory[readAddress] = (uint)this.random.Next(int.MinValue, int.MaxValue);
            }

            switch (pos)
            {
                case 0:
                    return (byte)(this.memory[readAddress] >> 24);
                case 1:
                    return (byte)(this.memory[readAddress] >> 16);
                case 2:
                    return (byte)(this.memory[readAddress] >> 8);
                case 3:
                    return (byte)(this.memory[readAddress]);
                default:
                    return (byte)(this.memory[readAddress]);
            }
        }

        public ushort GetValue16(uint address)
        {
            uint readAddress = address & 0xFFFFFFFCU;
            uint pos = address & 0x03U;

            if (!this.memory.ContainsKey(readAddress))
            {
                this.memory[readAddress] = (uint)this.random.Next(int.MinValue, int.MaxValue);
            }

            switch (pos)
            {
                case 0:
                case 1:
                    return (ushort)(this.memory[readAddress] >> 16);
                case 2:
                case 3:
                    return (ushort)(this.memory[readAddress]);
                default:
                    return (ushort)(this.memory[readAddress]);
            }
        }

        public uint GetValue32(uint address)
        {
            uint readAddress = address & 0xFFFFFFFCU;

            if (!this.memory.ContainsKey(readAddress))
            {
                this.memory[readAddress] = (uint)this.random.Next(int.MinValue, int.MaxValue);
            }
            
            return this.memory[readAddress];
        }

        public void SetValue8(uint address, uint value)
        {
            uint readAddress = address & 0xFFFFFFFCU;
            uint pos = address & 0x03U;

            if (!this.memory.ContainsKey(readAddress))
            {
                this.memory[readAddress] = (uint)this.random.Next(int.MinValue, int.MaxValue);
            }

            switch (pos)
            {
                case 0:
                    this.memory[readAddress] &= 0x00FFFFFFU;
                    this.memory[readAddress] |= value << 24;
                    break;
                case 1:
                    this.memory[readAddress] &= 0xFF00FFFFU;
                    this.memory[readAddress] |= (value & 0xFFU) << 16;
                    break;
                case 2:
                    this.memory[readAddress] &= 0xFFFF00FFU;
                    this.memory[readAddress] |= (value & 0xFFU) << 8;
                    break;
                case 3:
                    this.memory[readAddress] &= 0xFFFFFF00U;
                    this.memory[readAddress] |= value & 0xFFU;
                    break;
                default:
                    break;
            }
        }

        public void SetValue16(uint address, uint value)
        {
            uint readAddress = address & 0xFFFFFFFCU;
            uint pos = address & 0x03U;

            if(!this.memory.ContainsKey(readAddress))
            {
                this.memory[readAddress] = (uint)this.random.Next(int.MinValue, int.MaxValue);
            }

            switch (pos)
            {
                case 0:
                case 1:
                    this.memory[readAddress] &= 0x0000FFFFU;
                    this.memory[readAddress] |= (value & 0xFFFFU) << 16;
                    break;
                case 2:
                case 3:
                    this.memory[readAddress] &= 0xFFFF0000U;
                    this.memory[readAddress] |= value & 0xFFFFU;
                    break;
                default:
                    break;
            }
        }

        public void SetValue32(uint address, uint value)
        {
            uint readAddress = address & 0xFFFFFFFCU;

            this.memory[readAddress] = value;
        }
    }
}
