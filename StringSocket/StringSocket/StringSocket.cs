// Written by Joe Zachary for CS 3500, November 2012
// Revised by Joe Zachary April 2016

using System;
using System.Text;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace CustomNetworking
{
    /// <summary> 
    /// A StringSocket is a wrapper around a Socket.  It provides methods that
    /// asynchronously read lines of text (strings terminated by newlines) and 
    /// write strings. (As opposed to Sockets, which read and write raw bytes.)  
    ///
    /// StringSockets are thread safe.  This means that two or more threads may
    /// invoke methods on a shared StringSocket without restriction.  The
    /// StringSocket takes care of the synchronization.
    /// 
    /// Each StringSocket contains a Socket object that is provided by the client.  
    /// A StringSocket will work properly only if the client refrains from calling
    /// the contained Socket's read and write methods.
    /// 
    /// If we have an open Socket s, we can create a StringSocket by doing
    /// 
    ///    StringSocket ss = new StringSocket(s, new UTF8Encoding());
    /// 
    /// We can write a string to the StringSocket by doing
    /// 
    ///    ss.BeginSend("Hello world", callback, payload);
    ///    
    /// where callback is a SendCallback (see below) and payload is an arbitrary object.
    /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
    /// successfully written the string to the underlying Socket, or failed in the 
    /// attempt, it invokes the callback.  The parameters to the callback are a
    /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
    /// the Exception that caused the send attempt to fail.
    /// 
    /// We can read a string from the StringSocket by doing
    /// 
    ///     ss.BeginReceive(callback, payload)
    ///     
    /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
    /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
    /// string of text terminated by a newline character from the underlying Socket, or
    /// failed in the attempt, it invokes the callback.  The parameters to the callback are
    /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
    /// string or the Exception will be non-null, but nor both.  If the string is non-null, 
    /// it is the requested string (with the newline removed).  If the Exception is non-null, 
    /// it is the Exception that caused the send attempt to fail.
    /// </summary>
    public class StringSocket
    {
        /// <summary>
        /// The type of delegate that is called when a send has completed.
        /// </summary>
        public delegate void SendCallback(Exception e, object payload);

        /// <summary>
        /// The type of delegate that is called when a receive has completed.
        /// </summary>
        public delegate void ReceiveCallback(String s, Exception e, object payload);

        // Underlying socket
        private Socket socket;

        // Instance variables for BeginSend()
        private Encoding encoding;
        private bool sendIsOngoing;
        private readonly object sendSync;
        private StringBuilder outgoing;
        private byte[] pendingBytes;
        private int pendingIndex;
        private ConcurrentQueue<SendInformation> sendQueue;

        // Instance variables for BeginReceive()
        private Decoder decoder;
        private StringBuilder incoming;
        private byte[] incomingBytes;
        private char[] incomingChars;
        private ConcurrentQueue<ReceiveInformation> receiveQueue;
        private ConcurrentQueue<String> lineQueue;

        /// <summary>
        /// Creates a StringSocket from a regular Socket, which should already be connected.  
        /// The read and write methods of the regular Socket must not be called after the
        /// StringSocket is created.  Otherwise, the StringSocket will not behave properly.  
        /// The encoding to use to convert between raw bytes and strings is also provided.
        /// </summary>
        public StringSocket(Socket s, Encoding e)
        {
            socket = s;
            encoding = e;

            // Instance variables for sending
            sendIsOngoing = false;
            sendSync = new object();
            outgoing = new StringBuilder();
            pendingBytes = new byte[0];
            pendingIndex = 0;
            sendQueue = new ConcurrentQueue<SendInformation>();

            // Instance variables for receiving
            decoder = encoding.GetDecoder();
            incoming = new StringBuilder();
            incomingBytes = new byte[1024];
            incomingChars = new char[1024];
            receiveQueue = new ConcurrentQueue<ReceiveInformation>();
            lineQueue = new ConcurrentQueue<string>();
        }

        /// <summary>
        /// Shuts down and closes the socket.  No need to change this.
        /// </summary>
        public void Shutdown  ()
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// We can write a string to a StringSocket ss by doing
        /// 
        ///    ss.BeginSend("Hello world", callback, payload);
        ///    
        /// where callback is a SendCallback (see above) and payload is an arbitrary object.
        /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
        /// successfully written the string to the underlying Socket, or failed in the 
        /// attempt, it invokes the callback.  The parameters to the callback are a
        /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
        /// the Exception that caused the send attempt to fail. 
        /// 
        /// This method is non-blocking.  This means that it does not wait until the string
        /// has been sent before returning.  Instead, it arranges for the string to be sent
        /// and then returns.  When the send is completed (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginSend
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginSend must take care of synchronization instead.  On a given StringSocket, each
        /// string arriving via a BeginSend method call must be sent (in its entirety) before
        /// a later arriving string can be sent.
        /// </summary>
        public void BeginSend(string s, SendCallback callback, object payload)
        {
            // Get exclusive access to send mechanism
            lock (sendSync)
            {
                outgoing.Append(s);
                sendQueue.Enqueue(new SendInformation(callback, payload));

                if (!sendIsOngoing)
                {
                    sendIsOngoing = true;
                    SendBytes();
                }
            }
        }

        /// <summary>
        /// Private helper method for sending the data on a separate thread.
        /// </summary>
        private void SendBytes()
        {
            if (pendingIndex < pendingBytes.Length)
            {
                socket.BeginSend(pendingBytes, pendingIndex, pendingBytes.Length - pendingIndex,
                    SocketFlags.None, MessageSent, null);
            }
            else if (outgoing.Length > 0)
            {
                pendingBytes = encoding.GetBytes(outgoing.ToString());
                pendingIndex = 0;
                outgoing.Clear();
                socket.BeginSend(pendingBytes, 0, pendingBytes.Length,
                    SocketFlags.None, MessageSent, null);
            }
            else
            {
                sendIsOngoing = false;
                SendInformation info;
                if (sendQueue.TryDequeue(out info))
                {
                    info.callback(null, info.payload);
                }
            }
        }
        
        /// <summary>
        /// Private callback method invoked by the socket after trying to send the bytes
        /// </summary>
        private void MessageSent(IAsyncResult result)
        {
            int bytesSent = socket.EndSend(result);
            if(!socket.Connected)
            {
                return;
            }
            lock (sendSync)
            {
                if (bytesSent == 0)
                {
                    Task task = new Task(InvokeSendCallback);
                    task.Start();
                }
                else
                {
                    pendingIndex += bytesSent;
                    SendBytes();
                }
            }
        }

        /// <summary>
        /// Private Action delegate for invoking BeginSend callback on new thread
        /// </summary>
        private void InvokeSendCallback()
        {
            SendInformation info;
            if (sendQueue.TryDequeue(out info))
            {
                info.callback(null, info.payload);
            }
        }

        /// <summary>
        /// We can read a string from the StringSocket by doing
        /// 
        ///     ss.BeginReceive(callback, payload)
        ///     
        /// where callback is a ReceiveCallback (see above) and payload is an arbitrary object.
        /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
        /// string of text terminated by a newline character from the underlying Socket, or
        /// failed in the attempt, it invokes the callback.  The parameters to the callback are
        /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
        /// string or the Exception will be null, or possibly both.  If the string is non-null, 
        /// it is the requested string (with the newline removed).  If the Exception is non-null, 
        /// it is the Exception that caused the send attempt to fail.  If both are null, this
        /// indicates that the sending end of the remote socket has been shut down.
        ///  
        /// Alternatively, we can read a string from the StringSocket by doing
        /// 
        ///     ss.BeginReceive(callback, payload, length)
        ///     
        /// If length is negative or zero, this behaves identically to the first case.  If length
        /// is length, then instead of sending the next complete line in the callback, it sends
        /// the next length characters.  In other respects, it behaves analogously to the first case.
        /// 
        /// This method is non-blocking.  This means that it does not wait until a line of text
        /// has been received before returning.  Instead, it arranges for a line to be received
        /// and then returns.  When the line is actually received (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginReceive
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginReceive must take care of synchronization instead.  On a given StringSocket, each
        /// arriving line of text must be passed to callbacks in the order in which the corresponding
        /// BeginReceive call arrived.
        /// 
        /// Note that it is possible for there to be incoming bytes arriving at the underlying Socket
        /// even when there are no pending callbacks.  StringSocket implementations should refrain
        /// from buffering an unbounded number of incoming bytes beyond what is required to service
        /// the pending callbacks.
        /// </summary>
        public void BeginReceive(ReceiveCallback callback, object payload, int length = 0)
        {
            receiveQueue.Enqueue(new ReceiveInformation(callback, payload, length));
            if(receiveQueue.Count == 1)
            {
                socket.BeginReceive(incomingBytes, 0, incomingBytes.Length,
                SocketFlags.None, MessageReceived, null);
            }
        }

        /// <summary>
        /// Private callback method invoked by socket after attempting to receive bytes
        /// </summary>
        private void MessageReceived(IAsyncResult result)
        {
            if (!socket.Connected)
            {
                return;
            }
            byte[] buffer = (byte[])result.AsyncState;
            char[] chars = new char[1024];
            int bytesRead = socket.EndReceive(result);
            if (bytesRead == 0)
            {
                socket.Close();
            }
            else
            {
                int charsRead = decoder.GetChars(incomingBytes, 0, bytesRead, incomingChars, 0, false);
                incoming.Append(incomingChars, 0, charsRead);

                if(incoming.ToString().Contains('\n'))
                {
                    string[] splits = incoming.ToString().Split('\n').Where((s) => (!s.Equals(""))).ToArray();
                    incoming.Clear();
                    foreach(string split in splits)
                    {
                        lineQueue.Enqueue(split);
                    }
                }

                TryInvokeReceive();
            }
        }

        /// <summary>
        /// Private Action delegate for invoking BeginReceive callback on new thread
        /// </summary>
        private void TryInvokeReceive()
        {
            while(lineQueue.Count > 0 && receiveQueue.Count > 0)
            {
                ReceiveInformation info;
                bool Invoked = false;
                while(!Invoked && receiveQueue.TryDequeue(out info))
                {
                    string line;
                    if (lineQueue.TryDequeue(out line))
                    {
                        Invoked = true;
                        Task.Run(() => info.callback(line, null, info.payload));
                    }
                }

            }
            if(receiveQueue.Count > 0)
            {
                byte[] buffer = new byte[1024];
                socket.BeginReceive(incomingBytes, 0, incomingBytes.Length, SocketFlags.None, MessageReceived, null);
            }
        }
    }

    /// <summary>
    /// Abstraction of necessary callback information for SendBytes
    /// </summary>
    public class SendInformation
    {
        /// <summary>
        /// Callback method
        /// </summary>
        public StringSocket.SendCallback callback;
        /// <summary>
        /// Payload for the callback method
        /// </summary>
        public object payload;

        /// <summary>
        /// Constructor
        /// </summary>
        public SendInformation(StringSocket.SendCallback c, object p)
        {
            callback = c;
            payload = p;
        }
    }

    /// <summary>
    /// Abstraction of necessary callback information for ReceiveBytes
    /// </summary>
    public class ReceiveInformation
    {
        /// <summary>
        /// Callback method
        /// </summary>
        public StringSocket.ReceiveCallback callback;
        /// <summary>
        /// Payload for the callback method
        /// </summary>
        public object payload;
        /// <summary>
        /// Length of expected incoming data
        /// </summary>
        public int length;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReceiveInformation(StringSocket.ReceiveCallback c, object p, int l)
        {
            callback = c;
            payload = p;
            length = l;
        }
    }
}