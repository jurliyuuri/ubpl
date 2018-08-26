using UbplCommon;
using UbplCommon.Translator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubplla.Core
{
    /// <summary>
    /// 中間表現を保持するためのクラスです．
    /// </summary>
    class LkCode
    {
        /// <summary>
        /// ニーモニック
        /// </summary>
        public Mnemonic Mnemonic { get; set; }
        
        /// <summary>
        /// 最初の値
        /// </summary>
        public Operand Head { get; set; }

        /// <summary>
        /// 中間の値
        /// </summary>
        public Operand Middle { get; set; }

        /// <summary>
        /// 最後の値
        /// </summary>
        public Operand Tail { get; set; }

        /// <summary>
        /// ラベルの値のタイプ．
        /// 使用可能な値は"nll"か"l'"のみです．
        /// </summary>
        public string LabelType { get; set; }

        /// <summary>
        /// ラベル名
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// この中間表現がラベルを表しているかどうかを返します．
        /// </summary>
        public bool IsLabel
        {
            get => !string.IsNullOrEmpty(Label);
        }

        public override string ToString()
        {
            var buf = new StringBuilder("{ ");

            if(IsLabel)
            {
                buf.Append("LabelType: ").Append(LabelType)
                .Append(", Label:").Append(Label);
            }
            else
            {
                buf.Append("Mnemonic: ").Append(Mnemonic)
                .Append(", Head: ").Append(Head)
                .Append(", Middle: ").Append(Middle)
                .Append(", Tail: ").Append(Tail);
            }

            return buf.Append(" }").ToString();
        }
    }
}
