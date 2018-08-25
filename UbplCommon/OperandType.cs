using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    public enum OperandType : uint
    {
        REG = 0x0U,
        XMM = 0x1U,

        BIT64 = 0x8,
    }
}
