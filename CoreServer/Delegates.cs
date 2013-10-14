using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace self_server
{

    public delegate void OnConnection(TcpClient con, object source, object state);
    public delegate void OnBytesRead(byte[] data, object source, object state);
    public delegate void OnStringRead(string data, object source, object state);
    public delegate void OnSpecialRead(string data, object source, object state);
    public delegate void OnMessageRead(byte[] data,  object source, object state);
    public delegate void OnException(Exception ex, object source, object state);
}
