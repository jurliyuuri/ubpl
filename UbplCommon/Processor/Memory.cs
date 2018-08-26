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
        /// また，アドレスが4の倍数ではない場合にはExceptionが発生します．
        /// </summary>
        /// <param name="address">アドレス</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">アドレスが4の倍数でない場合</exception>
        public uint this[uint address]
        {
            get
            {
                if ((address & 3) != 0)
                {
                    throw new ArgumentException($"Invalid address:{address:X08}");
                }

                if (!this.memory.ContainsKey(address))
                {
                    this.memory[address] = (uint)this.random.Next(int.MinValue, int.MaxValue);
                }

                return this.memory[address];
            }
            set
            {
                if((address & 3) == 0)
                {
                    this.memory[address] = value;
                }
                else
                {
                    throw new ArgumentException($"Invalid address:{address:X08}");
                }
            }
        }
    }
}
