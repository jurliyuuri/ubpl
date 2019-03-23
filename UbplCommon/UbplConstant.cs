using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    public static class UbplConstant
    {
        /// <summary>
        /// レジスタ数
        /// </summary>
        public static readonly int REGISTER_COUNT = 8;

        /// <summary>
        /// ubplのF5レジスタのデフォルト値
        /// </summary>
        public static readonly uint DEFAULT_INITIAL_F5 = 0x6D7AA0F8U;

        /// <summary>
        /// ubplのNXレジスタのデフォルト値
        /// </summary>
        public static readonly uint DEFAULT_INITIAL_NX = 0x14830000U;

        /// <summary>
        /// アプリケーションのリターンアドレス
        /// </summary>
        public static readonly uint DEFAULT_RETURN_ADDRESS = 0xBDA574B8U;
    }
}
