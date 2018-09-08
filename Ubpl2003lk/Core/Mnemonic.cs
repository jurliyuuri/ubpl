using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubpl2003lk
{
    /// <summary>
    /// 命令の種類を表す列挙体です．
    /// </summary>
    public enum Mnemonic : byte
    {
        /// <summary>
        /// 加算
        /// </summary>
        ATA = 0x00,

        /// <summary>
        /// 減算
        /// </summary>
        NTA = 0x01,

        /// <summary>
        /// ビット積
        /// </summary>
        ADA = 0x02,

        /// <summary>
        /// ビット和
        /// </summary>
        EKC = 0x03,

        /// <summary>
        /// 論理右シフト
        /// </summary>
        DTO = 0x04,

        /// <summary>
        /// 左シフト
        /// </summary>
        DRO = 0x05,

        /// <summary>
        /// 算術右シフト
        /// </summary>
        DTOSNA = 0x06,

        /// <summary>
        /// ビットxnor
        /// </summary>
        DAL = 0x07,

        /// <summary>
        /// コピー
        /// </summary>
        KRZ = 0x08,

        /// <summary>
        /// フラグが立っているときのみkrzを行う
        /// </summary>
        MALKRZ = 0x09,

        /// <summary>
        /// 超過ならフラグを立てる(符号無し比較)
        /// </summary>
        LLONYS = 0x10,

        /// <summary>
        /// 以下ならフラグを立てる(符号無し比較)
        /// </summary>
        XTLONYS = 0x11,

        /// <summary>
        /// 以上ならフラグを立てる(符号無し比較)
        /// </summary>
        XOLONYS = 0x12,

        /// <summary>
        /// 未満ならフラグを立てる(符号無し比較)
        /// </summary>
        XYLONYS = 0x13,

        /// <summary>
        /// 同等ならフラグを立てる
        /// </summary>
        CLO = 0x16,

        /// <summary>
        /// 等しくないならフラグを立てる(符号付き比較)
        /// </summary>
        NIV = 0x17,

        /// <summary>
        /// 超過ならフラグを立てる(符号付き比較)
        /// </summary>
        LLO = 0x18,

        /// <summary>
        /// 以下ならフラグを立てる(符号付き比較)
        /// </summary>
        XTLO = 0x19,

        /// <summary>
        /// 以上ならフラグを立てる(符号付き比較)
        /// </summary>
        XOLO = 0x1A,

        /// <summary>
        /// 未満ならフラグを立てる(符号付き比較)
        /// </summary>
        XYLO = 0x1B,

        /// <summary>
        /// 二重移動
        /// </summary>
        INJ = 0x20,

        /// <summary>
        /// 符号無し乗算
        /// </summary>
        LAT = 0x28,

        /// <summary>
        /// 符号付き乗算
        /// </summary>
        LATSNA = 0x29,
    }
}
