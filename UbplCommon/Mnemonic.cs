using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    /// <summary>
    /// 命令の種類を表す列挙体です．
    /// </summary>
    public enum Mnemonic : uint
    {
        /// <summary>
        /// 加算
        /// </summary>
        ATA = 0x00000000,

        /// <summary>
        /// 減算
        /// </summary>
        NTA = 0x00000001,

        /// <summary>
        /// ビット積
        /// </summary>
        ADA = 0x00000002,

        /// <summary>
        /// ビット和
        /// </summary>
        EKC = 0x00000003,

        /// <summary>
        /// 論理右シフト
        /// </summary>
        DTO = 0x00000004,

        /// <summary>
        /// 左シフト
        /// </summary>
        DRO = 0x00000005,

        /// <summary>
        /// 算術右シフト
        /// </summary>
        DTOSNA = 0x00000006,

        /// <summary>
        /// ビットxnor
        /// </summary>
        DAL = 0x00000007,

        /// <summary>
        /// コピー
        /// </summary>
        KRZ = 0x00000008,

        /// <summary>
        /// フラグが立っているときのみkrzを行う
        /// </summary>
        MALKRZ = 0x00000009,

        /// <summary>
        /// 超過ならフラグを立てる(符号無し比較)
        /// </summary>
        LLONYS = 0x00000010,

        /// <summary>
        /// 以下ならフラグを立てる(符号無し比較)
        /// </summary>
        XTLONYS = 0x00000011,

        /// <summary>
        /// 以上ならフラグを立てる(符号無し比較)
        /// </summary>
        XOLONYS = 0x00000012,

        /// <summary>
        /// 未満ならフラグを立てる(符号無し比較)
        /// </summary>
        XYLONYS = 0x00000013,

        /// <summary>
        /// 同等ならフラグを立てる
        /// </summary>
        CLO = 0x00000016,

        /// <summary>
        /// 等しくないならフラグを立てる(符号付き比較)
        /// </summary>
        NIV = 0x00000017,

        /// <summary>
        /// 超過ならフラグを立てる(符号付き比較)
        /// </summary>
        LLO = 0x00000018,

        /// <summary>
        /// 以下ならフラグを立てる(符号付き比較)
        /// </summary>
        XTLO = 0x00000019,

        /// <summary>
        /// 以上ならフラグを立てる(符号付き比較)
        /// </summary>
        XOLO = 0x0000001A,

        /// <summary>
        /// 未満ならフラグを立てる(符号付き比較)
        /// </summary>
        XYLO = 0x0000001B,
        
        /// <summary>
        /// 関数等呼び出し．
        /// inj A xx f5@と同等
        /// </summary>
        FNX = 0x00000020,

        /// <summary>
        /// 入れ替え．
        /// inj A B Aと同等
        /// </summary>
        ACH = 0x00000021,

        /// <summary>
        /// inj A B xxと同等
        /// </summary>
        INJXX = 0x00000022,

        /// <summary>
        /// 符号無し乗算
        /// </summary>
        LAT = 0x00000028,

        /// <summary>
        /// 符号付き乗算
        /// </summary>
        LATSNA = 0x00000029,

        /// <summary>
        /// 乗算結果設定
        /// krz ((tmp >> 32) & 0x0000FFFF) A, krz (tmp & 0x0000FFFF) Bと同等
        /// </summary>
        LATKRZ = 0x0000002A,

        /// <summary>
        /// 符号無し除算
        /// </summary>
        KAK = 0x0000002B,

        /// <summary>
        /// 符号付き除算
        /// </summary>
        KAKSNA = 0x0000002C,

        /// <summary>
        /// 除算結果設定
        /// </summary>
        KAKKRZ = 0x0000002D,
    }
}