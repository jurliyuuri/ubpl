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

        XX_REG32 = ADD_XX | REG32,
        XX_IMM32 = ADD_XX | IMM32,
        XX_REG32_IMM32 = ADD_XX | REG32_IMM32,

        ADDR_REG32 = ADDRESS | REG32,
        ADDR_IMM32 = ADDRESS | IMM32,
        ADDR_REG32_REG32 = ADDRESS | REG32_REG32,
        ADDR_REG32_IMM32 = ADDRESS | REG32_IMM32,

        ADDR_XX_REG32 = ADDR_ADD_XX | REG32,
        ADDR_XX_IMM32 = ADDR_ADD_XX | IMM32,
        ADDR_XX_REG32_IMM32 = ADDR_ADD_XX | REG32_IMM32,
        
        ADD_XX = 0x10U,
        ADDRESS = 0x20U,
        ADDR_ADD_XX = ADDRESS | ADD_XX,
    }
}
