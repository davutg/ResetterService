using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;

namespace ResetterService
{
    public partial class ConnectionTracerService : ResertterServiceBase
    {
        private string PingAddress { get; set; }
        private static System.Timers.Timer PingTimer;
        private StringBuilder sb = new StringBuilder(100);    

        public ConnectionTracerService(string pingAddress)
        {
            this.PingAddress = pingAddress;
            Init();
        }

        private void Init()
        {
            PingTimer = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds); //Check it every minute
            PingTimer.Elapsed += (sender, e) =>
                {                   
                    sb.Clear();
                    sb.AppendLine(string.Format("Pinging {0}", this.PingAddress));
                    sb.AppendLine(string.Format("Signal time: {0:d/M/yyyy HH:mm:ss}", e.SignalTime));
                    Ping();
                };
            PingTimer.Start();  
        }


        private void Ping()
        {
                    
            Ping ping = new Ping();
            PingReply reply = ping.Send(this.PingAddress);
            if (reply.Status == IPStatus.Success)
            {                
                sb.AppendLine(string.Format("Address: {0}", reply.Address.ToString()));
                sb.AppendLine(string.Format("RoundTrip time: {0}", reply.RoundtripTime));
                sb.AppendLine(string.Format("Time to live: {0}", reply.Options.Ttl));
                sb.AppendLine(string.Format("Don't fragment: {0}", reply.Options.DontFragment));
                sb.AppendLine(string.Format("Buffer size: {0}", reply.Buffer.Length));
                Console.WriteLine(sb.ToString());
            }
            else
            {
                
                sb.AppendLine(string.Format("Reply Status   :{0}",reply.Status));
                sb.AppendLine(string.Format("Reply Address  :{0} ",reply.Address));
                sb.AppendLine(string.Format("Round Trip Time:{0}",reply.RoundtripTime));
                var bufferString=sb.ToString();
                Console.WriteLine(bufferString);
                Helpers.logToFile(bufferString);

            }
        }

        public override string DisplayName
        {
            get
            {
                return "Connection Tracer Service for "+ this.PingAddress;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}
