using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UbplCommon.Processor
{
    public class Memory
    {
        /// <summary>
        /// メモリ内容
        /// </summary>
        private readonly IDictionary<uint, uint> _data;

        /// <summary>
        /// メモリが未初期化だった場合に設定されている値を作成するRandom
        /// </summary>
        readonly Random _random;

        /// <summary>
        /// メモリの内容を表す読み込み専用のDictionary
        /// </summary>
        public IReadOnlyDictionary<uint, uint> Binaries
        {
            get => new ReadOnlyDictionary<uint, uint>(_data);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Memory()
        {
            _data = new Dictionary<uint, uint>();
            _random = new Random();
        }

        /// <summary>
        /// 指定されたアドレスの値を取得，またはアドレスに値を設定します．
        /// 未使用のアドレスが指定された場合にはランダムな値を返します．
        /// また，sizeがDWORDの場合にはアドレスの下位2bitの値が，
        /// WORDの場合にはアドレスの下位1bitの値が無視されます．
        /// </summary>
        /// <param name="address">アドレス</param>
        /// <param name="size">設定する値のビット幅</param>
        /// <returns></returns>
        public uint this[uint address, ValueSize size = ValueSize.DWORD]
        {
            get
            {
                return size switch
                {
                    ValueSize.BYTE => GetValue8(address),
                    ValueSize.WORD => GetValue16(address),
                    _ => GetValue32(address),
                };
            }
            set
            {
                switch (size)
                {
                    case ValueSize.BYTE:
                        SetValue8(address, value);
                        break;
                    case ValueSize.WORD:
                        SetValue16(address, value);
                        break;
                    case ValueSize.DWORD:
                    default:
                        SetValue32(address, value);
                        break;
                }
            }
        }

        public uint GetValue8(uint address)
        {
            uint readAddress = address & 0xFFFFFFFCU;
            uint pos = address & 0x03U;

            if (!_data.ContainsKey(readAddress))
            {
                _data[readAddress] = (uint)_random.Next(int.MinValue, int.MaxValue);
            }

            return pos switch
            {
                0 => (_data[readAddress] >> 24) & 0xFFU,
                1 => (_data[readAddress] >> 16) & 0xFFU,
                2 => (_data[readAddress] >> 8) & 0xFFU,
                _ => _data[readAddress] & 0xFFU,
            };
        }

        public uint GetValue16(uint address)
        {
            uint readAddress = address & 0xFFFFFFFCU;
            uint pos = address & 0x03U;

            if (!_data.ContainsKey(readAddress))
            {
                _data[readAddress] = (uint)_random.Next(int.MinValue, int.MaxValue);
            }

            switch (pos)
            {
                case 0:
                case 1:
                    return (_data[readAddress] >> 16) & 0xFFFFU;
                case 2:
                case 3:
                default:
                    return _data[readAddress] & 0xFFFFU;
            }
        }

        public uint GetValue32(uint address)
        {
            uint readAddress = address & 0xFFFFFFFCU;

            if (!_data.ContainsKey(readAddress))
            {
                _data[readAddress] = (uint)_random.Next(int.MinValue, int.MaxValue);
            }

            return _data[readAddress];
        }

        public void SetValue8(uint address, uint value)
        {
            uint readAddress = address & 0xFFFFFFFCU;
            uint pos = address & 0x03U;

            if (!_data.ContainsKey(readAddress))
            {
                _data[readAddress] = (uint)_random.Next(int.MinValue, int.MaxValue);
            }

            uint readValue = _data[readAddress];

            switch (pos)
            {
                case 0:
                    readValue &= 0x00FFFFFFU;
                    readValue |= (value & 0xFFU) << 24;
                    break;
                case 1:
                    readValue &= 0xFF00FFFFU;
                    readValue |= (value & 0xFFU) << 16;
                    break;
                case 2:
                    readValue &= 0xFFFF00FFU;
                    readValue |= (value & 0xFFU) << 8;
                    break;
                case 3:
                default:
                    readValue &= 0xFFFFFF00U;
                    readValue |= value & 0xFFU;
                    break;
            }

            _data[readAddress] = readValue;
        }

        public void SetValue16(uint address, uint value)
        {
            uint readAddress = address & 0xFFFFFFFCU;
            uint pos = address & 0x03U;

            if (!_data.ContainsKey(readAddress))
            {
                _data[readAddress] = (uint)_random.Next(int.MinValue, int.MaxValue);
            }

            uint readValue = _data[readAddress];

            switch (pos)
            {
                case 0:
                case 1:
                    readValue &= 0x0000FFFFU;
                    readValue |= (value & 0xFFFFU) << 16;
                    break;
                case 2:
                case 3:
                default:
                    readValue &= 0xFFFF0000U;
                    readValue |= value & 0xFFFFU;
                    break;
            }

            _data[readAddress] = readValue;
        }

        public void SetValue32(uint address, uint value)
        {
            uint readAddress = address & 0xFFFFFFFCU;

            _data[readAddress] = value;
        }
    }
}
