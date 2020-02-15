using System;
using System.Collections.Generic;
using System.Text;

namespace UbplCommon
{
    public enum OperandMode : uint
    {
        REG = 0U,
        IMM = 1U,
        IMM_REG = 2U,
        IMM_NREG = 3U,
        IMM_REG_REG = 4U,
        IMM_REG_NREG = 5U,
        IMM_NREG_REG = 6U,
        IMM_NREG_NREG = 7U,

        ADDR_REG = ADDRESS | REG,
        ADDR_IMM = ADDRESS | IMM,
        ADDR_IMM_REG = ADDRESS | IMM_REG,
        ADDR_IMM_NREG = ADDRESS | IMM_NREG,
        ADDR_IMM_REG_REG = ADDRESS | IMM_REG_REG,
        ADDR_IMM_REG_NREG = ADDRESS | IMM_REG_NREG,
        ADDR_IMM_NREG_REG = ADDRESS | IMM_NREG_REG,
        ADDR_IMM_NREG_NREG = ADDRESS | IMM_NREG_NREG,
        
        ADDRESS = 0x10U,
    }
}
