using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    public class ModRm
    {
        private uint value;

        public ModRm() : this(0) { }

        public ModRm(uint value)
        {
            this.value = value;
        }

        public uint Value
        {
            get => this.value;
        }

        public OperandMode ModeHead
        {
            get => (OperandMode)((value >> 28) & 0xF);
            set => this.value = ((uint)value << 28);
        }

        public OperandType TypeHead
        {
            get => (OperandType)((value >> 24) & 0xF);
            set => this.value = ((uint)value << 24);
        }

        public Register RegHead
        {
            get => (Register)((value >> 20) & 0xFF);
            set => this.value = ((uint)value << 20);
        }

        public bool IsAddressHead
        {
            get => (value & ((uint)OperandMode.ADDRESS << 28)) != 0;
        }

        public bool IsXmmHead
        {
            get => ((value >> 24) & 1) != 0;
        }

        public OperandMode ModeTail
        {
            get => (OperandMode)((value >> 12) & 0xF);
            set => this.value = ((uint)value << 12);
        }

        public OperandType TypeTail
        {
            get => (OperandType)((value >> 8) & 0xF);
            set => this.value = ((uint)value << 8);
        }

        public Register RegTail
        {
            get => (Register)((value >> 4) & 0xFF);
            set => this.value = ((uint)value << 4);
        }

        public bool IsAddressTail
        {
            get => (value & ((uint)OperandMode.ADDRESS << 12)) != 0;
        }

        public bool IsXmmTail
        {
            get => ((value >> 8) & 1) != 0;
        }
    }
}
