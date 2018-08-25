using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    public enum Register : uint
    {
        F0 = 0x0U,
        F1 = 0x1U,
        F2 = 0x2U,
        F3 = 0x3U,
        F4 = 0x4U,
        F5 = 0x5U,
        F6 = 0x6U,
        XX = 0x7U,

        L0 = XMM | F0,
        L1 = XMM | F1,
        L2 = XMM | F2,
        L3 = XMM | F3,
        L4 = XMM | F4,
        L5 = XMM | F5,
        L6 = XMM | F6,
        L7 = XMM | XX,

        XMM = OperandType.XMM << 4,
        BIT64 = OperandType.BIT64 << 4,
    }
}
