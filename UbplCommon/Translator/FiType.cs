namespace UbplCommon.Translator
{
    public enum FiType
    {
        /// <summary>
        /// 同等ならフラグを立てる
        /// </summary>
        CLO,
        
        /// <summary>
        /// 等しくないならフラグを立てる
        /// </summary>
        NIV,

        /// <summary>
        /// 以下ならフラグを立てる(符号無し比較)
        /// </summary>
        XTLONYS,

        /// <summary>
        /// 未満ならフラグを立てる(符号無し比較)
        /// </summary>
        XYLONYS,

        /// <summary>
        /// 以上ならフラグを立てる(符号無し比較)
        /// </summary>
        XOLONYS,

        /// <summary>
        /// 超過ならフラグを立てる(符号無し比較)
        /// </summary>
        LLONYS,

        /// <summary>
        /// 以下ならフラグを立てる(符号付き比較)
        /// </summary>
        XTLO,
        
        /// <summary>
        /// 未満ならフラグを立てる(符号付き比較)
        /// </summary>
        XYLO,
        
        /// <summary>
        /// 以上ならフラグを立てる(符号付き比較)
        /// </summary>
        XOLO,
        
        /// <summary>
        /// 超過ならフラグを立てる(符号付き比較)
        /// </summary>
        LLO,
    }
}
