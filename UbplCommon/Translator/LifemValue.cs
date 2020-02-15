using System.Collections.Generic;

namespace UbplCommon.Translator
{
    class LifemValue
    {
        public LifemValue()
        {
            Labels = new List<JumpLabel>();
        }

        public IList<JumpLabel> Labels { get; internal set; }
        public ValueSize Size { get; set; }
        public uint Value { get; set; }
    }
}
