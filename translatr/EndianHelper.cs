using System;
using System.IO;

namespace translatr
{
    public static class EndianHelper
    {
        public static uint readuint(this Stream s, bool be)
        {
            byte[] b = new byte[4];
            s.Read(b, 0, 4);

            uint u = BitConverter.ToUInt32(b, 0);

            return be ? u.swap() : u;
        }

        public static void writeuint(this Stream s, uint u, bool be)
        {
            if (be)
                u = u.swap();
            
            var b = BitConverter.GetBytes(u);

            s.Write(b, 0, 4);
        }

        public static uint swap(this uint u)
        {
            return (((u & 0x00FF) << 24) |
                    ((u & 0xFF00) << 8) |
                    ((u >> 8) & 0xFF00) |
                    ((u >> 24) & 0xFF));
        }
    }
}
