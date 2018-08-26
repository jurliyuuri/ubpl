using System;

namespace UbplCommon.Translator
{
    public class FiType
    {
        internal UbplMnemonic mne;

        internal FiType(UbplMnemonic mne)
        {
            this.mne = mne;
        }

        internal FiType(string mneName)
        {
            if(!Enum.TryParse(mneName, true, out this.mne))
            {
                throw new ArgumentException($"Not mnemonic '{mneName}'");
            }
        }
    }
}
