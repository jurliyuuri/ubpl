using System;

namespace UbplCommon.Translator
{
    public class FiType
    {
        internal Mnemonic mne;

        internal FiType(Mnemonic mne)
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
