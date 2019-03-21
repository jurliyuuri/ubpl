using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon.Translator
{
    public class LabelAddress
    {
        internal uint Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
