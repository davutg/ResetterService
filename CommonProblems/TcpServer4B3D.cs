using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V25.Message;


namespace AHBSBus.Web
{
    public class TcpServer4B3D
    {
        private byte[] _buffer = new byte[1024];
        private List<Socket> _clientSockets = new List<Socket>();
        private Socket _hl7NetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public ManualResetEvent _resetEvent = new ManualResetEvent(false);
        private bool connectionAborting = false;
        //static bool createdNew;
        //public static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "CF2D4313-33DE-489D-9721-6AFF69841DEA", out createdNew);

        public bool IsBound
        {
            get
            {
                if (_hl7NetSocket != null )
                {
                    return _hl7NetSocket.IsBound;
                }
                return false;
            }
        }

        public bool HasConnectedSocket
        {
            get
            {
                foreach (var socket in _clientSockets)
                {
                    if (socket.Connected)
                        return true;
                }
                return false;
            }
        }
        
        static void Main(string[] args)
        {
            TcpServer4B3D server = new TcpServer4B3D();
            server.SetupServer(14530);

            //Console.WriteLine("After Server Set Up" + DateTime.Now);
            //Console.WriteLine("Running !");
            //Thread.Sleep(1500);
            //client.StopClient();


            //Thread.Sleep(5000);

            //client = new WelchAllynClient();
            //client.StartClient();

            //Thread.Sleep(5000);
            //client.SendPatient();


            Console.ReadLine();
        }


        public bool StopServer()
        { 
            connectionAborting = true;

            if (_clientSockets != null && _clientSockets.Any())
            {

                foreach (var socket in _clientSockets)
                {
                    //socket.Shutdown(SocketShutdown.Both);
                    socket.Disconnect(false);
                    socket.Close();
                    if (socket != null)
                    {
                        socket.Dispose();
                    }
                }

                //_hl7NetSocket.Disconnect(false);                
            }


            if (_hl7NetSocket.IsBound)
            {
                //LingerOption lo = new LingerOption(false, 0);
                //_hl7NetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lo);

                if (_hl7NetSocket.Connected)
                _hl7NetSocket.Shutdown(SocketShutdown.Both);
                _hl7NetSocket.Close();
                _hl7NetSocket.Dispose();
                if (_hl7NetSocket.IsBound)
                    return false;
                return true;
            }

            return false;             
        }

        public void SetupServer(int serverPort=1013)
        {

            _hl7NetSocket.Bind(new IPEndPoint(IPAddress.Any, serverPort));
            _hl7NetSocket.Listen(5);
            var res = _hl7NetSocket.BeginAccept(AcceptCallback, null);

            Debug.WriteLine("Listener Socket instance is " + _hl7NetSocket.GetHashCode());
        }

        private void AcceptCallback(IAsyncResult result)
        {
            if (connectionAborting)
                return;

            Socket socket = _hl7NetSocket.EndAccept(result);
            _clientSockets.Add(socket);
            Debug.WriteLine("New Socket instance is " + socket.GetHashCode());
            _hl7NetSocket.BeginAccept(AcceptCallback, null);      // Bu satır ile yeni gelecek bağlantılara izin verilmiş olur.        

            while (true)
            {
                if (connectionAborting)
                    break;

                if (_buffer.Length < socket.Available)
                    _buffer = new byte[socket.Available];

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), socket);
                _resetEvent.WaitOne();
                _resetEvent.Reset();
            }
        }



        private void ReceiveCallBack(IAsyncResult result)
        {
            if (connectionAborting)
                return;

            try
            {
                Socket socket = (Socket)result.AsyncState;

                if (_buffer.Length < socket.Available)
                    _buffer = new byte[socket.Available];

                int received = socket.EndReceive(result);
                byte[] dataBuf = new byte[received];
                Array.Copy(_buffer, dataBuf, received);
                string text = Encoding.ASCII.GetString(dataBuf);
                Console.WriteLine("Text received: " + text);
                EvaluateText(socket, text);

                ClearBuffer();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private void ClearBuffer()
        {
            _buffer = new byte[_buffer.Length];
            Debug.WriteLine("Buffer Size:" + _buffer.Length);
        }

        public event Action<string> OnEvaluationTextReceived;
        private void EvaluateText(Socket socket, string text)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains("MSH"))
                return;

            text = text.Substring(text.IndexOf("MSH"));
            Debug.WriteLine("<< " + text);
            PipeParser parser = new PipeParser();
            try
            {
                ORU_R01 result = (ORU_R01)parser.Parse(text, "2.5");
                //result.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR
                SendText(socket, result);
                if (OnEvaluationTextReceived != null)
                {
                    OnEvaluationTextReceived(text);
                }                
            }
            catch (Exception exe)
            {
                Debug.WriteLine(exe);
            }
        }


        private void SendText(Socket socket, ORU_R01 receivedMessage)
        {
            string hapiTestResult =
            @"MSH|^~\&|RIS|B3D|B3D|B3D|20140307104326.991+0200||ACK^R01|101|P|2.5
                MSA|AA|2014030712163216";

            PipeParser p = new PipeParser();
            NHapi.Model.V25.Message.ACK ack = (NHapi.Model.V25.Message.ACK)p.Parse(hapiTestResult);
            ack.MSH.DateTimeOfMessage.Time.Value = DateTime.Now.ToString("yyyyMMddHHmmss.fffzzz").Replace(":", "");
            ack.MSA.MessageControlID.Value = receivedMessage.MSH.MessageControlID.Value;
            ack.MSH.MessageControlID.Value = "701";

            PipeParser parser = new PipeParser();
            var message = parser.Encode(ack);
            message = message.Substring(message.IndexOf("MSH"));
            message=message.Replace("|2.5", "|2.2");

                        //message.Replace((char)13 + "", "");
                        //message.Replace((char)10 + "", "");
            //message = message.Substring(0,message.LastIndexOf((char)13));

            message = (char)11 + "" + message + "" + (char)28 + "" + (char)13;

            byte[] data = Encoding.UTF8.GetBytes(message);

            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallBack), socket);
            Debug.WriteLine("Sent message>" + Environment.NewLine + message);
            Console.WriteLine("Sent message>" + Environment.NewLine + message + Environment.NewLine);

        }

        private void SendCallBack(IAsyncResult result)
        {
            Socket socket = (Socket)result.AsyncState;
            var byteCount = socket.EndSend(result);
            Debug.WriteLine(string.Format("{0} bytes sent successfully! By socket instance {1}", byteCount, socket.GetHashCode()));
        }

        /*
        private string createHL7()
        {

            ORU_R01 oruR01 = new ORU_R01();           

            oruR01.MSH.FieldSeparator.Value = "|";
            oruR01.MSH.EncodingCharacters.Value = @"^~\&";
            oruR01.MSH.SendingApplication.NamespaceID.Value = "SP";
            oruR01.MSH.SendingFacility.NamespaceID.Value = "SPZH";
            oruR01.MSH.ReceivingApplication.NamespaceID.Value = "MF";
            oruR01.MSH.ReceivingFacility.NamespaceID.Value = "INTRA";
            oruR01.MSH.DateTimeOfMessage.Time.SetLongDate(DateTime.Now);
            oruR01.MSH.ProcessingID.ProcessingID.Value = "P";
            

            PID pid = oruR01.GetPATIENT_RESULT().PATIENT.PID;
            pid.SetIDPID.Value = "12345";
            

            PipeParser parser = new PipeParser();
            string encodedMessage = parser.Encode(oruR01);

            return encodedMessage;
        }
        */
    }
}
