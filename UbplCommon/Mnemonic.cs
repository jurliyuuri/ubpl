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
        /// 第一オペランドの上位8bitを32bit符号拡張してkrzを行う
        /// </summary>
        KRZ8I = 0x0000000A,

        /// <summary>
        /// 第一オペランドの上位16bitを32bit符号拡張してkrzを行う
        /// </summary>
        KRZ16I = 0x0000000B,

        /// <summary>
        /// 第一オペランドの下位8bit取得し，第二オペランドの上位8bitに設定する
        /// </summary>
        KRZ8C = 0x0000000C,

        /// <summary>
        /// 第一オペランドの下位16bit取得し，第二オペランドの上位16bitに設定する
        /// </summary>
        KRZ16C = 0x0000000D,

        /// <summary>
        /// krz64 head &lt;&lt; 32 | tail tmp と同等
        /// </summary>
        MTE = 0x0000000E,

        /// <summary>
        /// 乗算結果設定
        /// krz ((tmp >> 32) & 0x0000FFFF) head, krz (tmp & 0x0000FFFF) tailと同等
        /// </summary>
        ANF = 0x0000000F,

        /// <summary>
        /// 同等ならフラグを立てる
        /// </summary>
        CLO = 0x00000010,

        /// <summary>
        /// 等しくないならフラグを立てる
        /// </summary>
        NIV = 0x00000011,

        /// <summary>
        /// 以下ならフラグを立てる(符号無し比較)
        /// </summary>
        XTLONYS = 0x00000012,

        /// <summary>
        /// 未満ならフラグを立てる(符号無し比較)
        /// </summary>
        XYLONYS = 0x00000013,

        /// <summary>
        /// 以下ならフラグを立てる(符号付き比較)
        /// </summary>
        XTLO = 0x00000014,

        /// <summary>
        /// 未満ならフラグを立てる(符号付き比較)
        /// </summary>
        XYLO = 0x00000015,

        /// <summary>
        /// 符号無し乗算
        /// </summary>
        LAT = 0x00000016,

        /// <summary>
        /// 符号付き乗算
        /// </summary>
        LATSNA = 0x00000017,

        /// <summary>
        /// 符号無し除算
        /// </summary>
        KAK = 0x00000018,

        /// <summary>
        /// 符号付き除算
        /// </summary>
        KAKSNA = 0x00000019,

        /// <summary>
        /// 関数等呼び出し．
        /// inj A xx Bと同等
        /// </summary>
        FNX = 0x00000020,
        
        /// <summary>
        /// I/O命令
        /// </summary>
        KLON = 0x00000040,
    }
}