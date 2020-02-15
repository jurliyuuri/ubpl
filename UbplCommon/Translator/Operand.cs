using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UbplCommon.Translator
{
    public class Operand
    {
        private readonly IReadOnlyList<RegisterValue> _registers;
        private readonly uint _immidiate;
        private readonly bool _isAddressing;

        Operand(IReadOnlyList<RegisterValue> registers, uint immidiate, bool isAddressing)
        {
            _registers = registers;
            _immidiate = immidiate;
            _isAddressing = isAddressing;
        }

        internal Operand(uint immidiate, bool isAddressing = false)
            : this(new List<RegisterValue>(), immidiate, isAddressing) { }

        internal Operand(RegisterValue first, uint immidiate, bool isAddressing = false)
            : this(new List<RegisterValue> {
                new RegisterValue(first.Register, first.RelativeAddress, first.IsMinus),
            }, immidiate, isAddressing) { }

        internal Operand(RegisterValue first, RegisterValue second, uint immidiate, bool isAddressing = false)
            : this(new List<RegisterValue> {
                new RegisterValue(first.Register, first.RelativeAddress, first.IsMinus),
                new RegisterValue(second.Register, second.RelativeAddress, second.IsMinus),
            }, immidiate, isAddressing) { }

        internal RegisterValue? First
        {
            get
            {
                if (_registers.Count > 0) { return _registers[0]; }
                else { return null; }
            }
        }

        internal RegisterValue? Second
        {
            get
            {
                if (_registers.Count > 1) { return _registers[1]; }
                else { return null; }
            }
        }

        public Register? FirstRegister
        {
            get
            {
                if (_registers.Count > 0) { return _registers[0].Register; }
                else { return null; }
            }
        }

        public Register? SecondRegister
        {
            get
            {
                if (_registers.Count > 1) { return _registers[1].Register; }
                else { return null; }
            }
        }

        public uint Immidiate
        {
            get => _immidiate + _registers.Select(x => x.IsMinus ? (~x.RelativeAddress.Value + 1) : x.RelativeAddress.Value)
                .Aggregate(0U, (acc, x) => acc + x);
        }

        public bool IsAddressing
        {
            get => _isAddressing;
        }

        public bool HasLabel
        {
            get => _registers.Any(x => x.RelativeAddress != 0);
        }

        public bool IsLabelFirst
        {
            get => _registers.Count > 0 && _registers[0].RelativeAddress != 0;
        }

        public bool IsLabelSecond
        {
            get => _registers.Count > 1 && _registers[1].RelativeAddress != 0;
        }

        public OperandMode ValueType
        {
            get
            {
                OperandMode mode;

                switch (_registers.Count)
                {
                    case 0:
                        mode = OperandMode.IMM;
                        break;
                    case 1:
                        if (_immidiate == 0)
                        {
                            mode = OperandMode.REG;
                        }
                        else if (_registers[0].IsMinus)
                        {
                            mode = OperandMode.IMM_NREG;
                        }
                        else
                        {
                            mode = OperandMode.IMM_REG;
                        }
                        break;
                    case 2:
                        if (_registers[0].IsMinus)
                        {
                            if (_registers[1].IsMinus)
                            {
                                mode = OperandMode.IMM_NREG_NREG;
                            }
                            else
                            {
                                mode = OperandMode.IMM_NREG_REG;
                            }
                        }
                        else
                        {
                            if (_registers[1].IsMinus)
                            {
                                mode = OperandMode.IMM_REG_NREG;
                            }
                            else
                            {
                                mode = OperandMode.IMM_REG_REG;
                            }
                        }
                        break;
                    default:
                        mode = OperandMode.REG;
                        break;
                }

                if (_isAddressing)
                {
                    mode |= OperandMode.ADDRESS;
                }

                return mode;
            }
        }

        public Operand ToAddressing()
        {
            return new Operand(_registers.Select(x => new RegisterValue
            {
                Register = x.Register,
                RelativeAddress = x.RelativeAddress,
                IsMinus = x.IsMinus,
            }).ToList(), _immidiate, true);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            if (Immidiate != 0 || !_registers.Any())
            {
                builder.Append(Immidiate);
            }

            foreach (var register in _registers)
            {
                if (register.IsMinus)
                {
                    builder.Append("|").Append(register.Register);
                }
                else {
                    if (builder.Length != 0)
                    {
                        builder.Append("+");
                    }
                    builder.Append(register.Register);
                }
            }

            if (_isAddressing)
            {
                builder.Append("@");
            }

            return builder.ToString();
        }

        public static Operand operator +(Operand operand)
        {
            if (operand.IsAddressing)
            {
                throw new ArgumentException($"Not supported : '+(operand@)' ({operand})");
            }

            return new Operand(operand._registers.Select(x => new RegisterValue(x.Register, x.RelativeAddress, x.IsMinus)).ToList(),
                operand._immidiate, false);
        }

        public static Operand operator -(Operand operand)
        {
            if (operand.IsAddressing)
            {
                throw new ArgumentException($"Not supported : '-(operand@)' ({operand})");
            }

            return new Operand(operand._registers.Select(x => new RegisterValue(x.Register, x.RelativeAddress, !x.IsMinus)).ToList(), 
                ~operand._immidiate + 1, false);
        }

        public static Operand operator +(Operand left, Operand right)
        {
            if (left.IsAddressing || right.IsAddressing)
            {
                throw new ArgumentException($"Not supported : 'left@ + right@' ({left}+{right})");
            }

            int registerCount = left._registers.Count + right._registers.Count;
            if (registerCount > 2)
            {
                throw new ArgumentException($"Not supported : reg/label's count is more than 2 ({left} + {right})");
            }

            List<RegisterValue> registers = new List<RegisterValue>();
            registers.AddRange(left._registers);
            registers.AddRange(right._registers);

            uint immidiate = left._immidiate + right._immidiate;

            return new Operand(registers, immidiate, false);
        }

        public static Operand operator +(Operand left, uint right)
        {
            return left + new Operand(right);
        }

        public static Operand operator +(uint left, Operand right)
        {
            return new Operand(left) + right;
        }

        public static Operand operator -(Operand left, Operand right)
        {
            return left + (-right);
        }

        public static Operand operator -(Operand left, uint right)
        {
            return left + new Operand(~right + 1);
        }

        public static Operand operator -(uint left, Operand right)
        {
            return new Operand(left) + (-right);
        }

        public static readonly Operand F0;

        public static readonly Operand F1;
        
        public static readonly Operand F2;
        
        public static readonly Operand F3;
        
        public static readonly Operand F4;
        
        public static readonly Operand F5;
        
        public static readonly Operand F6;
        
        public static readonly Operand XX;

        public static readonly Operand ZERO;

        static Operand()
        {
            F0 = new Operand(new RegisterValue { Register = Register.F0 }, 0);
            F1 = new Operand(new RegisterValue { Register = Register.F1 }, 0);
            F2 = new Operand(new RegisterValue { Register = Register.F2 }, 0);
            F3 = new Operand(new RegisterValue { Register = Register.F3 }, 0);
            F4 = new Operand(new RegisterValue { Register = Register.F4 }, 0);
            F5 = new Operand(new RegisterValue { Register = Register.F5 }, 0);
            F6 = new Operand(new RegisterValue { Register = Register.F6 }, 0);
            XX = new Operand(new RegisterValue { Register = Register.XX }, 0);
            ZERO = new Operand(0, false);
        }
    }
}
