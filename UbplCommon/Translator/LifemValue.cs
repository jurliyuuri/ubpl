using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon.Translator
{
    class LifemValue
    {
        public LifemValue()
        {
            Labels = new List<JumpLabel>();
        }

        public IList<JumpLabel> Labels { get; internal set; }
        public Mnemonic SetType { get; set; }
        public uint Value { get; set; }
        public JumpLabel SourceLabel { get; set; }
    }
}
