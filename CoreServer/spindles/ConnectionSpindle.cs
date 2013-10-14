using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace self_server
{
    public class ConnectionSpindle : IDisposable
    {
        public event OnException OnException;
        public event OnConnection OnConnection;

        private TcpListener listener;
        private object state;

        public void BeginListenLoop(IPEndPoint myIp, object state, int backlog = 10000)
        {
            this.state = state;
            listener = new TcpListener(myIp);
            listener.ExclusiveAddressUse = false;
            listener.Start();
            BeginAccept();
        }

        private void BeginAccept()
        {
            listener.BeginAcceptTcpClient(this.asyncCallback, listener);
        }


        private void asyncCallback(IAsyncResult ar)
        {

            try
            {
                var listener = ar.AsyncState as TcpListener;

                //signal that we want to end the connection loop, or got fed some bullshit
                if (listener == null) { return; }

                var con = listener.EndAcceptTcpClient(ar);
                if (this.OnConnection != null)
                {
                    this.OnConnection(con, this, this.state);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (this.OnException != null)
                    {
                        this.OnException(ex, this, this.state);
                    }
                }
                catch { }
            }
            finally
            {
                BeginAccept();
            }

        }



        public void Dispose()
        {
            try
            {
                if (this.listener != null)
                {
                    this.listener.Stop();
                    this.listener.EndAcceptSocket(null);
                    this.listener = null;
                }
            }
            catch { }
        }

        ~ConnectionSpindle()
        {
            this.Dispose();
        }

     
    }
}
