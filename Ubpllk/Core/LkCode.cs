using UbplCommon;
using UbplCommon.Translator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubpllk.Core
{
    /// <summary>
    /// 中間表現を保持するためのクラスです．
    /// </summary>
    class LkCode
    {
        /// <summary>
        /// ニーモニック
        /// </summary>
        public LkMnemonic Mnemonic { get; set; }

        /// <summary>
        /// 最初の値
        /// </summary>
        public Operand? Head { get; set; }

        /// <summary>
        /// 最後の値
        /// </summary>
        public Operand? Tail { get; set; }

        public LkCode()
        {
            Mnemonic = LkMnemonic.KRZ;
        }
        
        public override string ToString()
        {
            return $"{{ Mnemonic: {Mnemonic}, Head: {Head}, Tail: {Tail} }}";
        }
    }
}
