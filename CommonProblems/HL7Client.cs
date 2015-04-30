using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHapi.Base.Parser;
using NHapi.Model.V25.Message;
using NHapi.Base.Model;
using NHapi.Base.Model.Primitive;

namespace HL7Comm
{

    public class HL7Client
    {

        public ManualResetEvent _resetEvent = new ManualResetEvent(false);
        public TcpClient _client = null;
        public Thread tClientThread = null;

        public HL7Client()
        {

        }

        public bool IsConnected
        {
            get
            {
                if (_client != null)
                {
                    return _client.Connected;
                }
                return false;
            }
        }

        NetworkStream stm = null;

        public bool SendMessage(string query)
        {
            bool result = false;

            PipeParser parser = new PipeParser();
            var o01 = parser.Parse(query);
            //ORM_O01 o01 = (ORM_O01)parser.Parse(query);
            //a01.PID.SetIDPID.Value = patientID;


            var a01String = parser.Encode(o01);
            var data = Encoding.UTF8.GetBytes((char)11 + a01String + (char)13 + (char)28 + (char)13);
            Debug.WriteLine(BitConverter.ToString(data));
            try
            {
                stm.Write(data, 0, data.Length);
                byte[] _buffer = new byte[1024];        

                stm.Flush();
                var receivedCount=stm.Read(_buffer, 0, _buffer.Length);
                byte[] dataBuf = new byte[receivedCount];
                Array.Copy(_buffer, dataBuf, receivedCount);
                if (receivedCount > 0)
                {
                    var response = Encoding.UTF8.GetString(dataBuf);
                    //response = response.Replace("|2.2","|2.5");
                    PipeParser pp = new PipeParser();
                    IMessage reply = null;
                    if(!string.IsNullOrEmpty(response) && response.Contains("MSH"))
                        reply=pp.Parse(response.Substring(response.IndexOf("MSH")));
                    if (reply != null)
                    {
                        //IStructure msa = reply.GetStructure("MSA");
                        //IType ackCode = ((ISegment)msa).GetField(1)[0];                        
                        //string ackCodeValue =((GenericPrimitive)ackCode).Value;
                        
                        var r = (ACK)reply;
                        if (r.MSA.AcknowledgmentCode.Value == "AA")
                            return true;
                    }
                    return false;
                }

            }
            catch (SocketException soc)
            {
                Console.WriteLine(soc.Message);
                if (!HL7Client.IsSocketConnected(_client.Client))
                {
                    StartClient(this.HostIP, this.Port);
                }
                throw soc;                                
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }

            return result;
        }
        bool connectionAborting = false;
        /// <summary>
        /// Closes the connection and dispose the client
        /// </summary>
        public bool StopClient()
        {
            if (_client != null)
            {
                connectionAborting = true;
                _client.Close();
                tClientThread.Abort();
                tClientThread = null;
                _client = null;
            }
            return false;
            //_resetEvent.WaitOne();   
        }

        public string HostIP { get; set; }
        public int Port { get; set; }

        void EndConnect(IAsyncResult ar)
        {
            var client = (TcpClient)ar.AsyncState;
            

            try
            {
                client.EndConnect(ar);
            }
            catch { }

            if (client.Connected)
                return;

            client.Close();
        }

        public bool StartClient(string hostIP = "127.0.0.1", int port = 1012)
        {
         
                this.Port = port;
                this.HostIP = hostIP;

                if (connectionAborting)
                {
                    return false;
                }

                tClientThread = new Thread(new ThreadStart(() =>
                {

                    try
                    {

                        if (_client == null)
                            _client = new TcpClient() { SendTimeout = 10000, ReceiveTimeout = 30000 };

                        if (_client.Client == null)
                            return;

                        var result = _client.BeginConnect(IPAddress.Parse(this.HostIP), this.Port, EndConnect, _client);
                        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                        if (!success)
                        {
                            throw new Exception("Failed to connect.");
                        }

                        _client.EndConnect(result);

                        if (_client.Connected)
                        {
                            stm = _client.GetStream();
                            connectionAborting = false;
                        }


                        byte[] _buffer = new byte[1024];
                        while (true)
                        {
                            if (connectionAborting)
                                break;

                            if (_buffer.Length < _client.Available)
                                _buffer = new byte[_client.Available];

                            if (_client == null || _client.Client == null)
                                break;

                            #region ASYN Response Reader
                            ///ASYN olarak sonucu okur.
                            //_client.Client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback((result) =>
                            //{
                            //    if (connectionAborting)
                            //    {
                            //        return;
                            //    }
                            //    try
                            //    {
                            //        Socket socket = (Socket)result.AsyncState;
                            //        int received = socket.EndReceive(result);
                            //        byte[] dataBuf = new byte[received];
                            //        Array.Copy(_buffer, dataBuf, received);
                            //        string text = Encoding.UTF8.GetString(dataBuf);
                            //        Console.WriteLine("\n Text received: " + text);
                            //        PipeParser pp = new PipeParser();
                            //        var reply = pp.Parse(text.Substring(text.IndexOf("MSH")));
                            //        var r = (ACK)reply;
                            //        //r.MSA.AcknowledgmentCode
                            //    }
                            //    finally
                            //    {
                            //        _resetEvent.Set();
                            //    }
                            //}), _client.Client);
                            #endregion

                            _resetEvent.WaitOne();
                            _resetEvent.Reset();
                        }
                    }
                    catch (Exception excpt)
                    {
                        Debug.WriteLine(string.Format("TCP client couldn't started successfully :{0}", excpt));                        
                    }

                }

                    ))
                ;
                tClientThread.Start();
                tClientThread.Join(500);                      

            if (tClientThread.IsAlive && _client.Connected)
                return true;
            return false;
        }

        public static bool IsSocketConnected(Socket client)
        {
            // This is how you can determine whether a socket is still connected.
            bool blockingState = client.Blocking;
            try
            {
                byte[] tmp = new byte[1];

                client.Blocking = false;
                client.Send(tmp, 0, 0);
                return true;
            }
            catch (SocketException e)
            {
                // 10035 == WSAEWOULDBLOCK
                if (e.NativeErrorCode.Equals(10035))
                    return true;
                else
                {
                    return false;
                }
            }
            finally
            {
                client.Blocking = blockingState;
            }
        }

    }

 
}
