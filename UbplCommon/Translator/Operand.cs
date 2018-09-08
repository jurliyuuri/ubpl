using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon.Translator
{
    public class Operand
    {
        public Register? Reg { get; }
        public Register? SecondReg { get; }
        public uint? Disp { get; }
        public string Label { get; }
        public bool IsAddress { get; }

        Operand(Register? reg, Register? second, uint? val, string label, bool address = false)
        {
            this.Reg = reg;
            this.SecondReg = second;
            this.Disp = val;
            this.IsAddress = address;
            this.Label = label;
        }

        public bool IsImm
        {
            get => !Reg.HasValue && !SecondReg.HasValue
                && Disp.HasValue && string.IsNullOrEmpty(Label);
        }

        public bool IsReg
        {
            get => Reg.HasValue && !SecondReg.HasValue
                && !Disp.HasValue && string.IsNullOrEmpty(Label);
        }

        public bool IsLabel
        {
            get => !string.IsNullOrEmpty(Label);
        }

        public bool IsRegAndImm
        {
            get => Reg.HasValue && !SecondReg.HasValue
                && Disp.HasValue && string.IsNullOrEmpty(Label);
        }

        public bool HasSecondReg
        {
            get => Reg.HasValue && SecondReg.HasValue
                && !Disp.HasValue && string.IsNullOrEmpty(Label);
        }

        internal Operand(uint val) : this(null, null, val, null, false) { }
        internal Operand(Register reg, bool address = false) : this(reg, null, null, null, address) { }
        internal Operand(Register reg, uint val, bool address = false) : this(reg, null, val, null, address) { }
        internal Operand(Register reg, Register second, bool address = false) : this(reg, second, null, null, address) { }
        internal Operand(string label, bool address, uint val = 0) : this(null, null, val, label, address) { }

        internal Operand ToAddressing()
        {
            if (this.IsAddress)
            {
                throw new InvalidOperationException();
            }
            
            return new Operand(this.Reg, this.SecondReg, this.Disp, null, true);
        }

        public static Operand operator+(Operand left, Operand right)
        {
            if (left.IsImm && right.IsImm)
            {
                throw new ArgumentException();
            }
            if (left.IsAddress || right.IsAddress)
            {
                throw new ArgumentException();
            }
            if (left.IsLabel || right.IsLabel)
            {
                throw new ArgumentException();
            }

            if ((left.Reg.HasValue && (left.Disp.HasValue || left.SecondReg.HasValue))
                || (right.Reg.HasValue && (right.Disp.HasValue || right.SecondReg.HasValue)))
            {
                throw new ArgumentException();
            }
            else
            {
                return new Operand(left.Reg, right.Reg, null, null);
            }
        }

        public static Operand operator+(Operand left, uint disp) {
            return new Operand(left.Reg.Value, disp);
        }

        public static Operand operator+(uint disp, Operand right)
        {
            return new Operand(right.Reg.Value, disp);
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            if (IsLabel)
            {
                buffer.Append(Label);
            }
            else if (Reg.HasValue)
            {
                buffer.Append(Reg.Value);

                if (SecondReg.HasValue)
                {
                    buffer.Append("+").Append(SecondReg);
                }
                else if (Disp.HasValue)
                {
                    buffer.Append("+").Append(Disp);
                }
            }
            else if (Disp.HasValue)
            {
                buffer.Append(Disp.Value);
            }

            if (IsAddress && !IsLabel)
            {
                buffer.Append("@");
            }

            return buffer.ToString();
        }

        public override bool Equals(object obj)
        {
            if(obj is Operand)
            {
                Operand opd = obj as Operand;

                return this.Reg == opd.Reg && this.SecondReg == opd.SecondReg
                    && this.Disp == opd.Disp && this.Label == opd.Label;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = 1546192730;
            hashCode = hashCode * -1521134295 + EqualityComparer<Register?>.Default.GetHashCode(Reg);
            hashCode = hashCode * -1521134295 + EqualityComparer<Register?>.Default.GetHashCode(SecondReg);
            hashCode = hashCode * -1521134295 + EqualityComparer<uint?>.Default.GetHashCode(Disp);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Label);
            return hashCode;
        }
    }
}
