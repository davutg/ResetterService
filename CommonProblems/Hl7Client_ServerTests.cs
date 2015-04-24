using System;
using AHBSBus.Web;
using HL7Comm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonProblems
{
    [TestClass]
    public class Hl7Client_ServerTests
    {
        string hl7OrmO01 = @"MSH|^~\&|Avicenna|192.168.25.45|Siemens|Pacs|20141120092049||ORM^O01|145495932|P|2.5|||AL|AL|TR
                            PID|||2014106215||AKTEKİN^DENİZ||20140911|M
                            PV1||0|PEGS-BHS|||||Doç.Dr. DOĞANCI ŞEFİKA TÜMAY^DOĞANCI^ŞEFİKA TÜMAY|||||||||||145495827|||||||||||||||||||||||||20141120092049|999999999999
                            ORC|NW|000000145495828|||IP||^^^20141120092049^^ROUTINE|||||PEGS-BHS^DOĞANCI|RAD-BHS
                            OBR|1|000000145495828||US.RDY00627^US.RDY00627|ROUTINE|20141120092049||||||||||||||||||BHS_RAD
                            ";
        
        [TestMethod]
        public void IS_1071_ALIVE()
        {

            HL7Client client = new HL7Client();
            client.StartClient("127.0.0.1", 10710);
            Assert.IsTrue(client.IsConnected);
            var result=client.SendMessage(hl7OrmO01);
            Assert.IsTrue(result);
            

        }

        
        public static void main(String[] args)
        {
            TcpServer4B3D server = new TcpServer4B3D();
            server.SetupServer(14530);
        }

    }
}
