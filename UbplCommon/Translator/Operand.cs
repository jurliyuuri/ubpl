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
                && Disp.HasValue;
        }

        public bool IsReg
        {
            get => Reg.HasValue && !SecondReg.HasValue
                && !Disp.HasValue;
        }

        public bool IsLabel
        {
            get => !string.IsNullOrEmpty(Label);
        }

        public bool IsRegImm
        {
            get => Reg.HasValue && !SecondReg.HasValue
                && Disp.HasValue;
        }
        
        public bool HasSecondReg
        {
            get => Reg.HasValue && SecondReg.HasValue
                && !Disp.HasValue && string.IsNullOrEmpty(Label);
        }

        internal Operand(uint val, bool address = false) : this(null, null, val, null, address) { }
        internal Operand(Register reg, bool address = false) : this(reg, null, null, null, address) { }
        internal Operand(Register reg, uint val, bool address = false) : this(reg, null, val, null, address) { }
        internal Operand(Register reg, Register second, bool address = false) : this(reg, second, null, null, address) { }
        internal Operand(string label, bool address) : this(null, null, null, label, address) { }

        internal Operand ToAddressing()
        {
            if (this.IsAddress)
            {
                throw new InvalidOperationException();
            }
            
            return new Operand(this.Reg, this.SecondReg, this.Disp, this.Label, true);
        }

        public static Operand operator +(Operand left, Operand right)
        {
            if (left.IsAddress || right.IsAddress)
            {
                throw new ArgumentException($"Not supported : 'a@ + b@' ({left} + {right})");
            }

            if (((left.IsReg || left.IsLabel) && right.HasSecondReg)
                || (left.HasSecondReg && (right.IsReg || right.IsLabel)))
            {
                throw new ArgumentException($"Not supported : 'reg1 + reg2 + reg3/label' ({left} + {right})");
            }

            if ((left.IsReg && left.IsLabel && right.IsReg)
                || (left.IsReg && right.IsReg && right.IsLabel))
            {
                throw new ArgumentException($"Not supported : 'reg1 + reg2 + reg3/label' ({left} + {right})");
            }
            
            if (left.IsLabel && right.IsLabel)
            {
                throw new ArgumentException($"Not supported : 'label + label' ({left} + {right})");
            }

            uint value = (left.Disp ?? 0) + (right.Disp ?? 0);
            string label = left.Label ?? right.Label;
            Register? reg = left.Reg ?? right.Reg;
            Register? second = left.Reg.HasValue ? right.Reg : null;

            return new Operand(reg, second, value == 0 ? (uint?)null : value, label, false);
        }

        public static Operand operator+(Operand left, uint disp) {
            return left + new Operand(disp);
        }

        public static Operand operator+(uint disp, Operand right)
        {
            return new Operand(disp) + right;
        }

        public static Operand operator+(Operand left, string label)
        {
            return left + new Operand(label, false);
        }

        public static Operand operator+(string label, Operand right)
        {
            return new Operand(label, false) + right;
        }

        public override string ToString()
        {
            List<string> list = new List<string>();
            
            if (this.Reg.HasValue)
            {
                list.Add(this.Reg.Value.ToString());
            }

            if (this.HasSecondReg)
            {
                list.Add(this.SecondReg.Value.ToString());
            }

            if (this.IsLabel)
            {
                list.Add(this.Label);
            }

            if (this.IsImm)
            {
                list.Add(this.Disp.Value.ToString());
            }


            if (IsAddress)
            {
                return string.Join("+", list) + "@";
            }
            else
            {
                return string.Join("+", list);
            }
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
