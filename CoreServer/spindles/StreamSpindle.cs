using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace self_server
{
    public class StreamSpindle : IDisposable
    {

        public event OnBytesRead OnBytesRead;
        public event OnException OnException;

        private Stream s;
        private byte[] buffer;
        private int bufferPos;
        private object state;
        private bool inasyncread = false;

        public StreamSpindle(Stream s, int bufferlength)
        {
            this.s = s;
            buffer = new byte[bufferlength];
        }

        /// <summary>
        /// This will wait for a single byte[] message to be received and then through the OnBytesRead event no matter how few bytes are read
        /// </summary>
        /// <param name="state"></param>
        public void BeginReadOnce(object state = null)
        {
            this.BeginReadLoop(0, state);
        }

        /// <summary>
        /// This will continue reading from the stream in an Async loop until minLength bytes have been read from the stream. Once minLength bytes have been 
        /// read it will throw the OnBytesRead. You can stop an endless loop by disposing of this classs.
        /// </summary>
        /// <param name="minLength"></param>
        /// <param name="state"></param>
        public virtual void BeginReadLoop(int minLength, object state= null)
        {
            if (state != null) { this.state = state; }
            //expand the buffer if someone wants to read more than it can fit at once
            if (minLength > this.buffer.Length)
            {
                var nb = new byte[minLength];
                buffer.CopyTo(nb, 0);
                //buffer position should be fine where it is
            }
            //if you have already read this amount of data or more, return it
            if (minLength < 1 && this.bufferPos > 0)
            {
                this.ReturnData(this.bufferPos);
            }
            else if (minLength > 0 && bufferPos >= minLength)
            {
                this.ReturnData(minLength);
            }
            else
            {
                //if we haven't found all the data we need yet, get as much more as you can
                this.inasyncread = true;
                this.s.BeginRead(this.buffer, this.bufferPos, this.buffer.Length - this.bufferPos, this.asynCallback, minLength);
            }
        }

        /// <summary>
        /// Kind of like recursion, hope this shit doesn't create some kind of stack overflow type condition
        /// since it's event based I'm assuming it shouldn't
        /// </summary>
        /// <param name="ar"></param>
        private void asynCallback(IAsyncResult ar)
        {
            this.inasyncread = false;
            Console.WriteLine("ReadAsyncCallback  thread is " + Thread.CurrentThread.ManagedThreadId);
            try
            {
                if (ar == null || ar.AsyncState == null) { return; } //usually because this was manually called
                var desiredlength = (int)ar.AsyncState;
                var read = s.EndRead(ar);
                if (read < 1) { return; } //nothing left in the buffer
                //happens when a stream is gracefully closed
                this.bufferPos += read;

                this.BeginReadLoop(desiredlength, null);
            }
            catch (Exception ex)
            {
                try { if (this.OnException != null) { this.OnException(ex, this, this.state); } }
                catch { }
            }
        }

        /// <summary>
        /// Called when you know there is enough in the buffer to return the length amount
        /// </summary>
        /// <param name="length"></param>
        private void ReturnData(int length)
        {
            if (length == 0) { throw new ArgumentException("you're doing it wrong"); }

            var data = new byte[length];
            //copy out the data you're going to return
            Array.Copy(this.buffer, data, data.Length);
            //shift the buffer
            Array.Copy(this.buffer, length, this.buffer, 0, buffer.Length - length);
            bufferPos -= length;

            if (this.OnBytesRead != null) { this.OnBytesRead(data, this, this.state); }
        }

        ~StreamSpindle()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            try
            {
                if (this.s != null)
                {
                    using (this.s)
                    {
                        this.EndLoop();
                        this.s.Close();
                    }
                    this.s = null;
                }
            }
            catch { }
        }

        internal void EndLoop()
        {
            try
            {

                if (this.s != null && this.inasyncread)
                {
                    this.s.EndRead(null);
                }
            }
            catch 
            {
                //shit happens even though it shouldn't
            }
        }


        /// <summary>
        /// A synchronous write doesn't seem to burn the CPU that bad, leaving it for now
        /// </summary>
        /// <param name="resp"></param>
        public virtual void Write(byte[] resp)
        {
          
                this.s.Write(resp, 0, resp.Length);
            
        }
    }
}
