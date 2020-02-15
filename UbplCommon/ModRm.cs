using System;
using System.Collections.Generic;
using System.Text;

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
        /// それぞれ，Mode(8bit)，Reg(4bit)，Reg(4bit)となっている
        /// </summary>
        public uint Value { get; set; }

        public OperandMode HeadMode
        {
            get => (OperandMode)((Value >> 24) & 0xFFU);
            set => Value = (Value & 0x00FFFFFFU) | ((uint)value << 24);
        }

        public Register HeadReg1
        {
            get => (Register)((Value >> 20) & 0xFU);
            set => Value = (Value & 0xFF0FFFFFU) | ((uint)value << 20);
        }

        public Register HeadReg2
        {
            get => (Register)((Value >> 16) & 0xFU);
            set => Value = (Value & 0xFFF0FFFFU) | ((uint)value << 16);
        }

        public bool IsAddressHead
        {
            get => (Value & ((uint)OperandMode.ADDRESS << 24)) != 0U;
            set => Value = (Value & 0xEFFFFFFFU) | (value ? (uint)OperandMode.ADDRESS << 24 : 0U);
        }

        public OperandMode TailMode
        {
            get => (OperandMode)((Value >> 8) & 0xFFU);
            set => Value = (Value & 0xFFFF00FFU) | ((uint)value << 8);
        }

        public Register TailReg1
        {
            get => (Register)((Value >> 4) & 0xFU);
            set => Value = (Value & 0xFFFFFF0FU) | ((uint)value << 4);
        }

        public Register TailReg2
        {
            get => (Register)(Value & 0xFU);
            set => Value = (Value & 0xFFFFFFF0U) | ((uint)value);
        }

        public bool IsAddressTail
        {
            get => (Value & ((uint)OperandMode.ADDRESS << 8)) != 0U;
            set => Value = (Value & 0xFFFFEFFFU) | (value ? (uint)OperandMode.ADDRESS << 8 : 0U);
        }

        public override string ToString()
        {
            return $"ModRm(HeadMode: {HeadMode}, HeadReg1: {HeadReg1}, HeadReg2: {HeadReg2}, IsAddressHead: {IsAddressHead}, "
                + $"TailMode: {TailMode}, TailReg1: {TailReg1}, TailReg2: {TailReg2}, IsAddressTail: {IsAddressTail})";
        }
    }
}
