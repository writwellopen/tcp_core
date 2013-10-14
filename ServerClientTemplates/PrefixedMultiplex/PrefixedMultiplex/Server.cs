using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using self_server;
using System.Net.Sockets;

namespace ServerTemplates.PrefixedMultiplex
{
    class Server
    {

        private List<MultiMessageConnection> connections;
        private IPEndPoint ip;
        private ConnectionSpindle conListener;

        public Server()
        {

        }

        public void Start(IPEndPoint ip)
        {
            this.ip = ip;
            conListener = new ConnectionSpindle();
            conListener.OnConnection += new OnConnection(conListener_OnConnection);
            conListener.OnException += new OnException(conListener_OnException);
            conListener.BeginListenLoop(ip, this);

        }

        void conListener_OnConnection(TcpClient con, object source, object state)
        {
            var m = new MultiMessageConnection();
            m.OnException += new OnException(m_OnException);
            m.Start(con);
           
        }


        /// <summary>
        /// for errors on a particular connection
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="source"></param>
        /// <param name="state"></param>
        void m_OnException(Exception ex, object source, object state)
        {
            throw new NotImplementedException();
        }

     

        /// <summary>
        /// whene there is an error when receiving a new connection
        /// </summary>
        void conListener_OnException(Exception ex, object source, object state)
        {
            throw new NotImplementedException();
        }

        




    }
}
