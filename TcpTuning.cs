using System.Net;

namespace ModbusIntegrator
{
    public class TcpTuning
    {
        public IPAddress Address { get; set; }
        public int Port { get; set; }
        public int SendTimeout { get; set; }
        public int ReceiveTimeout { get; set; }
        public int Node { get; set; }

        public TcpTuning()
        {
            Address = new IPAddress(new byte[] { 127, 0, 0, 1 });
            Port = 502;
            SendTimeout = 5000;
            ReceiveTimeout = 5000;
            Node = 247;
        }
    }
}
