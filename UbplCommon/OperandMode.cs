using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    public enum OperandMode : uint
    {
        REG32 = 0,
        IMM32 = 1,
        REG32_REG32 = 2,
        REG32_IMM32 = 3,

        ADDRESS = 4,
        ADDR_REG32 = ADDRESS | REG32,
        ADDR_IMM32 = ADDRESS | IMM32,
        ADDR_REG32_REG32 = ADDRESS | REG32_REG32,
        ADDR_REG32_IMM32 = ADDRESS | REG32_IMM32,
    }
}
