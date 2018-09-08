using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon.Translator
{
    /// <summary>
    /// CodeGeneratorの中間表現を保持するためのクラスです．
    /// </summary>
    class Code
    {
        public Mnemonic Mnemonic { get; set; }
        public ModRm Modrm { get; set; }
        public Operand Head { get; set; }
        public Operand Tail { get; set; }

        public override string ToString()
        {
            return new StringBuilder("Code(")
                .Append("Mnemonic: ").Append(this.Mnemonic).Append(", ")
                .Append("Modrm: ").Append(this.Modrm.Value).Append(", ")
                .Append("Head: ").Append(this.Head).Append(", ")
                .Append("Tail: ").Append(this.Tail)
                .Append(")").ToString();
        }
    }
}
