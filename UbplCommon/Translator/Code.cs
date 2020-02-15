
namespace UbplCommon.Translator
{
    class Code
    {
        public Mnemonic Mnemonic { get; set; }
        public ModRm Modrm { get; set; }
        public Operand Head { get; set; }
        public Operand Tail { get; set; }

        public Code()
        {
            Mnemonic = Mnemonic.KRZ;
            Modrm = new ModRm(0U);
            Head = Operand.F0;
            Tail = Operand.F0;
        }

        public override string ToString()
        {
            return $"Code(Mnemonic: {Mnemonic}, Modrm: {Modrm}, Head: {Head}, Tail: {Tail})";
        }
    }
}
