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
    public enum LkMnemonic : uint
    {
        /// <summary>
        /// 加算
        /// </summary>
        ATA,

        /// <summary>
        /// 減算
        /// </summary>
        NTA,

        /// <summary>
        /// ビット積
        /// </summary>
        ADA,

        /// <summary>
        /// ビット和
        /// </summary>
        EKC,

        /// <summary>
        /// 論理右シフト
        /// </summary>
        DTO,

        /// <summary>
        /// 左シフト
        /// </summary>
        DRO,

        /// <summary>
        /// 算術右シフト
        /// </summary>
        DTOSNA,

        /// <summary>
        /// ビットxnor
        /// </summary>
        DAL,

        /// <summary>
        /// コピー
        /// </summary>
        KRZ,

        /// <summary>
        /// フラグが立っているときのみkrzを行う
        /// </summary>
        MALKRZ,

        /// <summary>
        /// 第一オペランドの上位8bitを32bit符号拡張してkrzを行う
        /// </summary>
        KRZ8I,

        /// <summary>
        /// 第一オペランドの上位16bitを32bit符号拡張してkrzを行う
        /// </summary>
        KRZ16I,

        /// <summary>
        /// 第一オペランドの下位8bit取得し，第二オペランドの上位8bitに設定する
        /// </summary>
        KRZ8C,

        /// <summary>
        /// 第一オペランドの下位16bit取得し，第二オペランドの上位16bitに設定する
        /// </summary>
        KRZ16C,

        /// <summary>
        /// 超過ならフラグを立てる(符号無し比較)
        /// </summary>
        LLONYS,

        /// <summary>
        /// 以下ならフラグを立てる(符号無し比較)
        /// </summary>
        XTLONYS,

        /// <summary>
        /// 以上ならフラグを立てる(符号無し比較)
        /// </summary>
        XOLONYS,

        /// <summary>
        /// 未満ならフラグを立てる(符号無し比較)
        /// </summary>
        XYLONYS,

        /// <summary>
        /// 同等ならフラグを立てる
        /// </summary>
        CLO,

        /// <summary>
        /// 等しくないならフラグを立てる(符号付き比較)
        /// </summary>
        NIV,

        /// <summary>
        /// 超過ならフラグを立てる(符号付き比較)
        /// </summary>
        LLO,

        /// <summary>
        /// 以下ならフラグを立てる(符号付き比較)
        /// </summary>
        XTLO,

        /// <summary>
        /// 以上ならフラグを立てる(符号付き比較)
        /// </summary>
        XOLO,

        /// <summary>
        /// 未満ならフラグを立てる(符号付き比較)
        /// </summary>
        XYLO,

        /// <summary>
        /// 二重移動
        /// </summary>
        INJ,

        /// <summary>
        /// 符号無し乗算
        /// </summary>
        LAT,

        /// <summary>
        /// 符号付き乗算
        /// </summary>
        LATSNA,

        /// <summary>
        /// 前置ラベル
        /// </summary>
        NLL,

        /// <summary>
        /// 後置ラベル
        /// </summary>
        L,

        /// <summary>
        /// 定数定義 32bit
        /// </summary>
        LIFEM,

        /// <summary>
        /// 定数定義 8bit
        /// </summary>
        LIFEM8,

        /// <summary>
        /// 定数定義 16bit
        /// </summary>
        LIFEM16,
    }
}
