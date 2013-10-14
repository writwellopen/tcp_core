using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace self_server
{
    class BitUtil
    {
        public byte[] PrefixWithLength(byte[] message)
        {
            var length = BitConverter.GetBytes(message.Length);
            var n = new byte[length.Length + message.Length];
            length.CopyTo(n, 0);
            message.CopyTo(n, length.Length);
            return n;
        }
    }
}
