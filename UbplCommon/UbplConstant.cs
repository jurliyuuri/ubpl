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
        /// 2003fのF5レジスタのデフォルト値
        /// </summary>
        public static readonly uint DEFAULT_INITIAL_F5 = 0x6D7AA0F8U;

        /// <summary>
        /// 2003fのNXレジスタのデフォルト値
        /// </summary>
        public static readonly uint DEFAULT_INITIAL_NX = 0x14830000U;

        /// <summary>
        /// アプリケーションのリターンアドレス
        /// </summary>
        public static readonly uint DEFAULT_RETURN_ADDRESS = 0xBDA574B8U;

        /// <summary>
        /// デバッグ用出力アドレス
        /// </summary>
        public static readonly uint TVARLON_KNLOAN_ADDRESS = 3126834864;

    }
}
