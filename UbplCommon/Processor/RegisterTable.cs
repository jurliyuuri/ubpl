using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace UbplCommon.Processor
{
    class RegisterTable
    {
        uint _f0;
        uint _f1;
        uint _f2;
        uint _f3;
        uint _f4;
        uint _f5;
        uint _f6;
        uint _xx;

        public RegisterTable()
        {
            _f0 = 0;
            _f1 = 0;
            _f2 = 0;
            _f3 = 0;
            _f4 = 0;
            _f5 = 0;
            _f6 = 0;
            _xx = 0;
        }

        public int Count => 8;

        public uint F0 { get => _f0; set => _f0 = value; }
        public uint F1 { get => _f1; set => _f1 = value; }
        public uint F2 { get => _f2; set => _f2 = value; }
        public uint F3 { get => _f3; set => _f3 = value; }
        public uint F4 { get => _f4; set => _f4 = value; }
        public uint F5 { get => _f5; set => _f5 = value; }
        public uint F6 { get => _f6; set => _f6 = value; }
        public uint XX { get => _xx; set => _xx = value; }

        public uint this[Register register]
        {
            get
            {
                return register switch
                {
                    Register.F0 => _f0,
                    Register.F1 => _f1,
                    Register.F2 => _f2,
                    Register.F3 => _f3,
                    Register.F4 => _f4,
                    Register.F5 => _f5,
                    Register.F6 => _f6,
                    Register.XX => _xx,
                    _ => throw new ArgumentOutOfRangeException($"Invalid arguments: {register}"),
                };
            }
            set
            {
                switch (register)
                {
                    case Register.F0:
                        _f0 = value;
                        break;
                    case Register.F1:
                        _f1 = value;
                        break;
                    case Register.F2:
                        _f2 = value;
                        break;
                    case Register.F3:
                        _f3 = value;
                        break;
                    case Register.F4:
                        _f4 = value;
                        break;
                    case Register.F5:
                        _f5 = value;
                        break;
                    case Register.F6:
                        _f6 = value;
                        break;
                    case Register.XX:
                        _xx = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Invalid arguments: {register}");
                }
            }
        }

        public bool TryGetValue(Register register, out uint value)
        {
            bool has = true;

            switch (register)
            {
                case Register.F0:
                    value = _f0;
                    break;
                case Register.F1:
                    value = _f1;
                    break;
                case Register.F2:
                    value = _f2;
                    break;
                case Register.F3:
                    value = _f3;
                    break;
                case Register.F4:
                    value = _f4;
                    break;
                case Register.F5:
                    value = _f5;
                    break;
                case Register.F6:
                    value = _f6;
                    break;
                case Register.XX:
                    value = _xx;
                    break;
                default:
                    value = default;
                    has = false;
                    break;
            }

            return has;
        }
    }
}
