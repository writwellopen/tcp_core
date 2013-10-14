using System;
using System.Net.Sockets;
using System.Text;
using self_server;

namespace ServerTemplates.PrefixedMultiplex
{
    internal class MultiMessageConnection
    {
        private enum ReadType
        {
            Header = 1,
            Body = 2,
        }

        private enum SpecialOps
        {
            Ping = -1,
            Pong = -2
        }

        public void Start(TcpClient client) {
            var streamspindle = new StreamSpindle(client.GetStream(), client.ReceiveBufferSize);
            this.Start(streamspindle);
        }

        public void Start(StreamSpindle spindle)
        {
            spindle.OnBytesRead += new self_server.OnBytesRead(streamspindle_OnBytesRead);
            spindle.OnException += this.OnException;
            spindle.BeginReadLoop(sizeof(int), ReadType.Header);
            this.stream = spindle;
        }

        public event OnException OnException;
        public event OnStringRead OnStringRead;
        public event OnBytesRead OnBytesRead;
        public event OnSpecialRead OnSpecialRead;

        private StreamSpindle stream;
        private BitUtil util = new BitUtil();
        private readonly object writelock = new object();
        private DateTime last_ping_sent = DateTime.MinValue;
        private DateTime last_ping_received = DateTime.MinValue;
        private DateTime last_send = DateTime.MinValue;
        private DateTime last_receive = DateTime.MinValue;

        public TimeSpan LastPing
        {
            get
            {
                if (last_ping_sent == DateTime.MinValue) { return TimeSpan.MaxValue; }
                return DateTime.UtcNow.Subtract(last_ping_received);
            }
        }

        private void SendPong()
        {
            var p = BitConverter.GetBytes((int)SpecialOps.Pong);
            this.Send(p);
        }

        /// <summary>
        /// You can send a ping safely at any time from any thread since all message are sent in complete blocks, not fragments
        /// </summary>
        public void SendPing()
        {
            this.last_ping_sent = DateTime.UtcNow;
            var png = BitConverter.GetBytes((int)SpecialOps.Ping);
            this.Send(png);
        }

        /// <summary>
        /// Does a basic conversion to bytes and calls the main Send method
        /// </summary>
        /// <param name="message"></param>
        public void Send(string message)
        {
            var bytes = Encoding.Default.GetBytes(message);
            this.Send(bytes);
        }


        /// <summary>
        /// Sends the byte data of an ENTIRE message all at once, do not send messages pieces at
        /// </summary>
        /// <param name="message"></param>
        public void Send(byte[] message)
        {
            message = this.util.PrefixWithLength(message);
            lock (writelock)
            {
                this.stream.Write(message);
            }
        }


        /// <summary>
        /// this is what is called when some bytes are received by a server or a client
        /// </summary>
        /// <param name="data"></param>
        /// <param name="source"></param>
        /// <param name="state"></param>
        void streamspindle_OnBytesRead(byte[] data, object source, object state)
        {
            var stream = source as StreamSpindle;

            
            if ((ReadType)state == ReadType.Header)
            {
                var length = BitConverter.ToInt32(data, 0);
             
                //negitive "lengths" are acutally integer operation that are send alone without a body
                if (length < 1)
                {
                    var op = (SpecialOps)length;
                    if (op == SpecialOps.Ping)
                    {
                        this.last_ping_received = DateTime.Now;
                        if (this.OnSpecialRead != null) { this.OnSpecialRead("Received Ping", this, state); }
                        this.SendPong();
                    }
                    else if (op == SpecialOps.Pong)
                    {
                       //this is the other end getting the response to the ping we delt with above thiss
                        if (this.OnSpecialRead != null) { this.OnSpecialRead("Received Ping Response", this, state); }
                    }
                    //since the special operation is now done already, look for the next message header
                    stream.BeginReadLoop(sizeof(int), ReadType.Header);
                }
                else
                {
                    stream.BeginReadLoop(length, ReadType.Body);
                }                
            }
            else if ((ReadType)state == ReadType.Body)
            {
                //you should only ever get a body of date when you have already read in a "header" 
                if (this.OnBytesRead != null)
                {
                    this.OnBytesRead(data, this, state);
                }
                if (this.OnStringRead != null)
                {
                    var s = Encoding.Default.GetString(data);
                    this.OnStringRead(s, this, state);
                }
                stream.BeginReadLoop(sizeof(int), ReadType.Header);
            }
    

        }
    }
}
