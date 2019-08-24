using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    public enum OperandMode : uint
    {
        REG32 = 0U,
        IMM32 = 1U,
        REG32_REG32 = 2U,
        REG32_IMM32 = 3U,

        XX_REG32 = ADD_XX | REG32,
        XX_IMM32 = ADD_XX | IMM32,
        XX_REG32_IMM32 = ADD_XX | REG32_IMM32,

        ADDR_REG32 = ADDRESS | REG32,
        ADDR_IMM32 = ADDRESS | IMM32,
        ADDR_REG32_REG32 = ADDRESS | REG32_REG32,
        ADDR_REG32_IMM32 = ADDRESS | REG32_IMM32,

        ADDR_XX_REG32 = ADDRESS | ADD_XX | REG32,
        ADDR_XX_IMM32 = ADDRESS | ADD_XX | IMM32,
        ADDR_XX_REG32_IMM32 = ADDRESS | ADD_XX | REG32_IMM32,
        
        ADD_XX = 0x10U,
        ADDRESS = 0x20U,
    }
}
