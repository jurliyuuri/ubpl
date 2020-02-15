using System;

namespace UbplCommon.Translator
{
    public class JumpLabel : Operand
    {
        public JumpLabel() : base(new RegisterValue(Register.XX), 0, false) { }

        public uint RelativeAddress
        {
            get
            {
                if (First is null)
                {
                    throw new ApplicationException($"Illegal operand: {FirstRegister}");
                }

                uint address = First.RelativeAddress.Value;

                return First.IsMinus ? (~address + 1) : address;
            }
            set
            {
                if (First is null)
                {
                    throw new ApplicationException($"Illegal operand: {FirstRegister}");
                }

                First.RelativeAddress.Value = First.IsMinus ? (~value + 1) : value;
            }
        }

        public override string ToString()
        {
            return $"JumpLabel(RelativeAddress: {RelativeAddress})";
        }
    }
}
