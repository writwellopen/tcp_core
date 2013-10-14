using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace self_server
{
    class TestStreamServer : ServerTemplate
    {
        public override void AfterDataReceived(byte[] data, object source, object state)
        {
            try{
            Console.WriteLine("got message, attempting to respond");
            var worker = source as TcpConnectionWorker;
            var dir = Environment.CurrentDirectory;
            var mp3s = Directory.GetFiles(dir, "*.mp3");
            Console.WriteLine("found " + mp3s.Length + " mp3s in " + dir);
            var ms = new MemoryStream();
            foreach (var mp3 in mp3s)
            {
                Console.WriteLine("Packing " + mp3);
                var b = File.ReadAllBytes(mp3);
                ms.Write(b, 0, b.Length);
            }
            Console.WriteLine("sending combined data");
            worker.BeginSend(ms.ToArray());

            }catch(Exception ex){
                Console.WriteLine("FAIL: " + ex.ToString());
                Console.ReadLine();
            }
        }
    }
}
