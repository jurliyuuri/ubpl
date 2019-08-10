using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    public static class CharacterCode
    {
        private readonly static IDictionary<char, uint> code;

        public static uint ToByte(char c)
        {
            if(code.TryGetValue(c, out uint value))
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
            char? c = code.Where(x => x.Value == b)
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
            code = new Dictionary<char, uint>();

            Number();
            Liparxe();
            Sign();
        }

        private static void Number()
        {
            code['0'] = 0x10;
            code['1'] = 0x11;
            code['2'] = 0x12;
            code['3'] = 0x13;
            code['4'] = 0x14;
            code['5'] = 0x15;
            code['6'] = 0x16;
            code['7'] = 0x17;
            code['8'] = 0x18;
            code['9'] = 0x19;
        }

        private static void Liparxe()
        {
            code['p'] = 0x20;
            code['F'] = 0x21;
            code['f'] = 0x22;
            code['t'] = 0x23;
            code['c'] = 0x24;
            code['x'] = 0x25;
            code['k'] = 0x26;
            code['q'] = 0x27;
            code['h'] = 0x28;
            code['R'] = 0x29;
            code['z'] = 0x2A;
            code['m'] = 0x2B;
            code['n'] = 0x2C;
            code['r'] = 0x2D;
            code['l'] = 0x2E;
            code['j'] = 0x2F;
            code['w'] = 0x30;
            code['b'] = 0x31;
            code['V'] = 0x32;
            code['v'] = 0x33;
            code['d'] = 0x34;
            code['s'] = 0x35;
            code['g'] = 0x36;
            code['X'] = 0x37;
            code['i'] = 0x38;
            code['y'] = 0x39;
            code['u'] = 0x3A;
            code['o'] = 0x3B;
            code['e'] = 0x3C;
            code['a'] = 0x3D;
            code['Y'] = 0x3E;
            code['U'] = 0x3F;
            code['E'] = 0x40;
        }

        private static void Sign()
        {
            code[' '] = 0x00;
            code['\n'] = 0x01;
            code['#'] = 0x02;
            code['('] = 0x03;
            code[')'] = 0x04;
            code['《'] = 0x05;
            code['》'] = 0x06;
            code['<'] = 0x07;
            code['>'] = 0x08;
            code['@'] = 0x09;
            code['&'] = 0x0A;
            code['+'] = 0x0B;
            code['|'] = 0x0C;
            code['='] = 0x0D;
            code['\"'] = 0x0E;
            code['\''] = 0x0F;

            code['_'] = 0x1A;
            code['\\'] = 0x1B;
            code['-'] = 0x1C;
            code[':'] = 0x1D;
            code['?'] = 0x1E;
            code['!'] = 0x1F;

            code[','] = 0x41;
            code['.'] = 0x42;

            code['\0'] = 0xFF;
        }
    }
}
