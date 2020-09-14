using System.Collections.Generic;
using System.Net;

namespace ModbusIntegrator
{
    public class TcpTuning
    {
        public IPAddress Address { get; set; } = new IPAddress(new byte[] { 127, 0, 0, 1 });
        public int Port { get; set; } = 502;
        public int SendTimeout { get; set; } = 5000;
        public int ReceiveTimeout { get; set; } = 5000;
        public List<AskParamData> FetchParams { get; set; } = new List<AskParamData>();
        public List<AskParamData> FetchArchives { get; set; } = new List<AskParamData>();
    }
}
