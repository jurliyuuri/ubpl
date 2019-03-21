using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UbplCommon
{
    public class ModRm
    {
        public ModRm() : this(0) { }

        public ModRm(uint value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Head(16bit)とTail(16bit)のmodrmを持つ
        /// それぞれ，Mode(8bit)，Type(4bit)，Reg(4bit)となっている
        /// </summary>
        public uint Value
        {
            get;
            set;
        }

        public OperandMode ModeHead
        {
            get => (OperandMode)((this.Value >> 24) & 0xFFU);
            set => this.Value = this.Value & 0x00FFFFFFU | (((uint)value & 0xFFU) << 24);
        }

        public OperandType TypeHead
        {
            get => (OperandType)((this.Value >> 20) & 0xFU);
            set => this.Value = this.Value & 0xFF0FFFFFU | (((uint)value & 0xFU) << 20);
        }

        public Register RegHead
        {
            get => (Register)((this.Value >> 16) & 0xFU);
            set => this.Value = this.Value & 0xFFF0FFFFU | (((uint)value & 0xFU) << 16);
        }

        public bool IsAddressHead
        {
            get => (this.Value & ((uint)OperandMode.ADDRESS << 24)) != 0;
        }

        public OperandMode ModeTail
        {
            get => (OperandMode)((this.Value >> 8) & 0xFF);
            set => this.Value = this.Value & 0xFFFF00FFU | (((uint)value & 0xFFU) << 8);
        }

        public OperandType TypeTail
        {
            get => (OperandType)((this.Value >> 4) & 0xF);
            set => this.Value = this.Value & 0xFFFFFF0FU | (((uint)value & 0xFU) << 4);
        }

        public Register RegTail
        {
            get => (Register)(this.Value & 0xFU);
            set => this.Value = this.Value & 0xFFFFFFF0U | ((uint)value & 0xFU);
        }

        public bool IsAddressTail
        {
            get => (this.Value & ((uint)OperandMode.ADDRESS << 8)) != 0;
        }
    }
}
