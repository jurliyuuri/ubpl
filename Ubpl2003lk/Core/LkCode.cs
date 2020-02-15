using UbplCommon;
using UbplCommon.Translator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubpl2003lk.Core
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
        /// 中間の値
        /// </summary>
        public Operand? Middle { get; set; }

        /// <summary>
        /// 最後の値
        /// </summary>
        public Operand? Tail { get; set; }
        
        public override string ToString()
        {
            var buf = new StringBuilder("{ ");

            buf.Append("Mnemonic: ").Append(Mnemonic)
            .Append(", Head: ").Append(Head)
            .Append(", Middle: ").Append(Middle)
            .Append(", Tail: ").Append(Tail);

            return buf.Append(" }").ToString();
        }
    }
}
