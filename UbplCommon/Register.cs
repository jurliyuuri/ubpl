using System;

namespace UbplCommon
{
    /// <summary>
    /// レジスタを表す列挙体です．
    /// </summary>
    public enum Register : uint
    {
        /// <summary>
        /// F0レジスタを表します．
        /// </summary>
        F0 = 0x0U,
        /// <summary>
        /// F1レジスタを表します．
        /// </summary>
        F1 = 0x1U,
        /// <summary>
        /// F2レジスタを表します．
        /// </summary>
        F2 = 0x2U,
        /// <summary>
        /// F3レジスタを表します．
        /// </summary>
        F3 = 0x3U,
        /// <summary>
        /// F4レジスタを表します．
        /// </summary>
        F4 = 0x4U,
        /// <summary>
        /// F5レジスタを表します．
        /// </summary>
        F5 = 0x5U,
        /// <summary>
        /// F6レジスタを表します．
        /// </summary>
        F6 = 0x6U,
        /// <summary>
        /// XXレジスタを表します．
        /// </summary>
        XX = 0x7U,
    }
}
