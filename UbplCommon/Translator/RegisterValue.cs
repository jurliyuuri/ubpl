
using System;

namespace UbplCommon.Translator
{
    internal class RegisterValue
    {
        public Register Register { get; set; }
        public RelativeAddressValue RelativeAddress { get; set; }
        public bool IsMinus { get; set; }

        public RegisterValue() : this(Register.F0, new RelativeAddressValue(), false) { }

        public RegisterValue(Register register, uint address = 0, bool isMinus = false)
            : this(register, new RelativeAddressValue { Value = address }, isMinus) { }

        public RegisterValue(Register register, RelativeAddressValue address, bool isMinus = false)
        {
            Register = register;
            RelativeAddress = address;
            IsMinus = isMinus;
        }

        public override string ToString()
        {
            return $"RegisterValue(Register: {Register}, RelativeAddress: {RelativeAddress}, IsMinus: {IsMinus})";
        }

        public class RelativeAddressValue
        {
            public uint Value { get; set; }

            public override bool Equals(object? obj)
            {
                return obj is RelativeAddressValue value &&
                       Value == value.Value;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Value);
            }

            public static bool operator ==(RelativeAddressValue addressValue, uint value)
            {
                return addressValue.Value == value;
            }

            public static bool operator !=(RelativeAddressValue addressValue, uint value)
            {
                return addressValue.Value != value;
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }
    }
}
