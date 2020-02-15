using System;
using System.Collections.Generic;
using System.Linq;

namespace UbplCommon
{
    public static class CharacterCode
    {
        private readonly static IDictionary<char, uint> _code;

        public static uint ToByte(char c)
        {
            if(_code.TryGetValue(c, out uint value))
            {
                return value;
            }
            else
            {
                throw new ArgumentException($"Not defined character: {c} : {(byte)c} ");
            }
        }
        
        public static char ToChar(uint b)
        {
            char? c = _code.Where(x => x.Value == b)
                .Select(x => (char?)x.Key).SingleOrDefault();

            if(c.HasValue)
            {
                return c.Value;
            }
            else
            {
                throw new ArgumentException($"Unknown code: {b}");
            }
        }

        static CharacterCode()
        {
            _code = new Dictionary<char, uint>();

            Number();
            Liparxe();
            Sign();
        }

        private static void Number()
        {
            _code['0'] = 0x10;
            _code['1'] = 0x11;
            _code['2'] = 0x12;
            _code['3'] = 0x13;
            _code['4'] = 0x14;
            _code['5'] = 0x15;
            _code['6'] = 0x16;
            _code['7'] = 0x17;
            _code['8'] = 0x18;
            _code['9'] = 0x19;
        }

        private static void Liparxe()
        {
            _code['p'] = 0x20;
            _code['F'] = 0x21;
            _code['f'] = 0x22;
            _code['t'] = 0x23;
            _code['c'] = 0x24;
            _code['x'] = 0x25;
            _code['k'] = 0x26;
            _code['q'] = 0x27;
            _code['h'] = 0x28;
            _code['R'] = 0x29;
            _code['z'] = 0x2A;
            _code['m'] = 0x2B;
            _code['n'] = 0x2C;
            _code['r'] = 0x2D;
            _code['l'] = 0x2E;
            _code['j'] = 0x2F;
            _code['w'] = 0x30;
            _code['b'] = 0x31;
            _code['V'] = 0x32;
            _code['v'] = 0x33;
            _code['d'] = 0x34;
            _code['s'] = 0x35;
            _code['g'] = 0x36;
            _code['X'] = 0x37;
            _code['i'] = 0x38;
            _code['y'] = 0x39;
            _code['u'] = 0x3A;
            _code['o'] = 0x3B;
            _code['e'] = 0x3C;
            _code['a'] = 0x3D;
            _code['Y'] = 0x3E;
            _code['U'] = 0x3F;
            _code['E'] = 0x40;
        }

        private static void Sign()
        {
            _code[' '] = 0x00;
            _code['\n'] = 0x01;
            _code['#'] = 0x02;
            _code['('] = 0x03;
            _code[')'] = 0x04;
            _code['《'] = 0x05;
            _code['》'] = 0x06;
            _code['<'] = 0x07;
            _code['>'] = 0x08;
            _code['@'] = 0x09;
            _code['&'] = 0x0A;
            _code['+'] = 0x0B;
            _code['|'] = 0x0C;
            _code['='] = 0x0D;
            _code['\"'] = 0x0E;
            _code['\''] = 0x0F;

            _code['_'] = 0x1A;
            _code['\\'] = 0x1B;
            _code['-'] = 0x1C;
            _code[':'] = 0x1D;
            _code['?'] = 0x1E;
            _code['!'] = 0x1F;

            _code[','] = 0x41;
            _code['.'] = 0x42;

            _code['\0'] = 0xFF;
        }
    }
}
