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
    }
}
