﻿using System.Globalization;
using System.Text;

namespace ZWaveDotNet.Util
{
    public static class MemoryUtil
    {
        public static Memory<byte> Fill(byte val, int count)
        {
            Memory<byte> ret = new byte[count];
            if (val != 0x0)
                ret.Span.Fill(val);
            return ret;
        }

        public static Memory<byte> PadZeros(Memory<byte> val, int count)
        {
            if (count <= 0)
                return val;
            Memory<byte> ret = new byte[val.Length + count];
            if (val.Length > 0)
                val.CopyTo(ret);
            return ret;
        }

        public static Memory<byte> LeftShift1(Memory<byte> array)
        {
            Memory<byte> ret = new byte[array.Length];
            for (int i = 0; i < ret.Length - 1; i++)
                ret.Span[i] = (byte)(array.Span[i] << 1 | (array.Span[i + 1] >> 7));
            ret.Span[ret.Length - 1] = (byte)(array.Span[array.Length - 1] << 1);
            return ret;
        }

        public static Memory<byte> XOR(Memory<byte> a, Memory<byte> b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Invalid Byte Array Sizes");
            Memory<byte> ret = new byte[a.Length];
            for (int i = 0; i < a.Length; i++)
                ret.Span[i] = (byte)(a.Span[i] ^ b.Span[i]);
            return ret;
        }

        public static void Increment(Memory<byte> mem)
        {
            for (int i = mem.Length - 1; i >= 0; i--)
            {
                mem.Span[i] += 1;
                if (mem.Span[i] != 0x0)
                    return;
            }
        }

        public static string Print(Memory<byte> mem)
        {
            StringBuilder ret = new StringBuilder(mem.Length * 3);
            foreach (byte b in mem.Span)
            {
                if (ret.Length > 0)
                    ret.Append(' ');
                ret.Append(b.ToString("X2"));
            }
            return ret.ToString();
        }

        public static Memory<byte> From(string hexString)
        {
                if (hexString.Length % 2 != 0)
                    throw new ArgumentException("Not a hex string");

                Memory<byte> data = new byte[hexString.Length / 2];
                for (int index = 0; index < data.Length; index++)
                    data.Span[index] = byte.Parse(hexString.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                return data;
        }
    }
}
