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
        UL = 0xFU,
    }
}
