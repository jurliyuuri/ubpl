using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon.Translator
{
    public class Operand
    {
        protected Register?[] registers;
        protected LabelAddress[] labelAddresses;
        protected uint immidiate;
        
        private Operand(Register?[] registers, LabelAddress[] labelAddresses, uint immidiate, bool isAddress)
        {
            this.registers = registers ?? new Register?[2];
            this.labelAddresses = labelAddresses ?? new LabelAddress[2];
            this.immidiate = immidiate;
            this.IsAddress = isAddress;
        }

        internal Operand(uint immidiate, bool isAddress = false)
            : this(null, null, immidiate, isAddress) { }
        
        internal Operand(Register reg0, LabelAddress label0, uint immidiate, bool isAddress = false)
            : this(new Register?[] { reg0, null }, new LabelAddress[] { label0, null }, immidiate, isAddress) { }
        
        internal Operand(Register reg0, LabelAddress label0, Register reg1, LabelAddress label1 , bool isAddress = false)
            : this(new Register?[] { reg0, reg1 }, new LabelAddress[] { label0, label1 }, 0, isAddress) { }
        
        public Register? First
        {
            get
            {
                if(this.registers[0].HasValue)
                {
                    return this.registers[0];
                }
                else
                {
                    return null;
                }
            }
        }

        public Register? Second
        {
            get
            {
                if (this.registers[1].HasValue)
                {
                    return this.registers[1];
                }
                else
                {
                    return null;
                }
            }
        }

        public uint Immidiate
        {
            get
            {
                return this.immidiate + this.labelAddresses.Where(x => x != null).Aggregate(0U, (acc, x) => acc + x.Value);
            }
        }

        public bool IsAddress { get; }
        public bool HasLabel
        {
            get => this.labelAddresses.Any(x => x != null);
        }

        public bool IsFirstLabel
        {
            get => this.labelAddresses[0] != null;
        }

        public bool IsSecondLabel
        {
            get => this.labelAddresses[1] != null;
        }

        public Operand ToAddressing()
        {
            return new Operand(this.registers, this.labelAddresses, this.immidiate, true);
        }

        public OperandMode ValueType
        {
            get
            {
                var mode = OperandMode.REG32;
                
                bool register0 = this.registers[0] == Register.XX;
                bool register1 = this.registers[1] == Register.XX;

                bool has0 = this.registers[0].HasValue;
                bool has1 = this.registers[1].HasValue;

                if (register0)
                {
                    if (has1)
                    {
                        mode |= OperandMode.XX_REG32_IMM32;
                    }
                    else if (this.labelAddresses[0] != null)
                    {
                        mode |= OperandMode.XX_IMM32;
                    }
                }
                else if (register1)
                {
                    if (has0)
                    {
                        mode |= OperandMode.XX_REG32_IMM32;
                    }
                    else if (this.labelAddresses[1] != null)
                    {
                        mode |= OperandMode.XX_IMM32;
                    }
                }
                else if (has0 && has1)
                {
                    mode |= OperandMode.REG32_REG32;
                }
                else if (has0 || has1)
                {
                    if (this.immidiate != 0)
                    {
                        mode |= OperandMode.REG32_IMM32;
                    }
                }
                else
                {
                    mode = OperandMode.IMM32;
                }

                if(this.IsAddress)
                {
                    mode |= OperandMode.ADDRESS;
                }

                return mode;
            }
        }

        public static Operand operator +(Operand left, uint right)
        {
            return left + new Operand(right);
        }
        
        public static Operand operator +(uint left, Operand right)
        {
            return new Operand(left) + right;
        }
        
        public static Operand operator +(Operand left, Operand right)
        {
            if(left.IsAddress || right.IsAddress)
            {
                throw new ArgumentException($"Not supported : 'left@ + right@' ({left}+{right})");
            }

            int registerCount = left.registers.Count(x => x.HasValue) + right.registers.Count(x => x.HasValue);
            if (registerCount > 2)
            {
                throw new ArgumentException($"Not supported : reg/label's count is more than 2 ({left} + {right})");
            }

            if (registerCount == 2 && (left.labelAddresses.All(x => x == null) && right.labelAddresses.All(x => x == null))
                && (left.immidiate + right.immidiate != 0))
            {
                throw new ArgumentException($"Not supported : 'reg + reg + imm' ({left}+{right})");
            }

            (Register? register, LabelAddress labelAddress)[] ps = new (Register? register, LabelAddress labelAddress)[4];

            ps[0] = (left.registers[0], left.labelAddresses[0]);
            ps[1] = (left.registers[1], left.labelAddresses[1]);
            ps[2] = (right.registers[0], right.labelAddresses[0]);
            ps[3] = (right.registers[1], right.labelAddresses[1]);

            Register?[] registers = new Register?[2];
            LabelAddress[] labelAddresses = new LabelAddress[2];
            uint immidiate = left.immidiate + right.immidiate;

            for (int i = 0, count = 0; i < ps.Length; i++)
            {
                if(ps[i].register.HasValue)
                {
                    registers[count] = ps[i].register;
                    labelAddresses[count] = ps[i].labelAddress;
                    count++;

                    if(count >= 2)
                    {
                        break;
                    }
                }
            }

            return new Operand(registers, labelAddresses, immidiate, false);
        }

        public override string ToString()
        {
            List<string> list = new List<string>();

            list.AddRange(this.registers.Where(x => x.HasValue).Select(x => x.ToString()));

            uint immidiate = this.Immidiate;
            if (immidiate != 0 || !this.registers.Any(x => x.HasValue))
            {
                list.Add(immidiate.ToString());
            }

            if (this.IsAddress)
            {
                return string.Join("+", list) + "@";
            }
            else
            {
                return string.Join("+", list);
            }
        }
    }

    public class JumpLabel : Operand
    {
        public JumpLabel() : base(Register.XX, new LabelAddress(), 0U) { }

        public uint RelativeAddress
        {
            get => this.labelAddresses[0].Value;
            set
            {
                this.labelAddresses[0].Value = value;
            }
        }

        public override string ToString()
        {
            return $"JumpLabel@{this.GetHashCode()}";
        }
    }
}
