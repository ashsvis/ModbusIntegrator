using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ModbusIntegrator
{
    partial class ModbusIntegratorProgram
    {
        static List<AskParamData> FetchParams = new List<AskParamData>();
        static List<AskParamData> FetchArchives = new List<AskParamData>();

        private static void ModbusWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            Console.WriteLine($"{e.UserState}");
        }

        private static void ModbusWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            Console.WriteLine($"{e.Result}");
        }

        private static void ModbusWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            var lastsecond = DateTime.Now.Second;
            var lastminute = -1;
            var lasthour = -1;

            var parameters = e.Argument as TcpTuning;

            var remoteEp = new IPEndPoint(parameters.Address, parameters.Port);

            while (!worker.CancellationPending)
            {
                var dt = DateTime.Now;
                if (lastsecond == dt.Second) continue;
                lastsecond = dt.Second;
                // прошла секунда
                if (dt.Second % 3 == 0)
                {
                    try
                    {
                        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            socket.SendTimeout = parameters.SendTimeout;
                            socket.ReceiveTimeout = parameters.ReceiveTimeout;
                            socket.Connect(remoteEp);
                            Thread.Sleep(500);
                            if (socket.Connected)
                            {
                                byte[] buff;
                                int numBytes;
                                for (var k = 0; k < FetchParams.Count; k++)
                                {
                                    var item = FetchParams[k];
                                    socket.Send(PrepareFetchParam(item.Node, item.Func, item.RegAddr, item.TypeValue));
                                    Thread.Sleep(500);
                                    buff = new byte[8192];
                                    numBytes = socket.Receive(buff);
                                    if (numBytes > 0)
                                    {
                                        var answer = CleanAnswer(buff);
                                        if (CheckAnswer(answer, item.Node, item.Func, item.TypeValue))
                                        {
                                            var result = EncodeFetchAnswer(answer, item.Node, item.Func, item.RegAddr, item.TypeValue, item.TypeSwap, item.UnitValue);

                                            if (item.LastValue != result.Value)
                                            {
                                                item.LastValue = result.Value;

                                                worker.ReportProgress(answer.Length, $"{item.ParamName}\t{result}");

                                                var columns = new Dictionary<string, object> {
                                                            { "TagName", item.ParamName },
                                                            { "Value", result.Value ?? "" },
                                                            { "Unit", result.Unit ?? "" },
                                                        };
                                                //if (!server.ReplaceInto("fetching", columns))
                                                //    worker.ReportProgress(answer.Length, server.LastError);
                                            }
                                        }
                                    }
                                }
                                socket.Disconnect(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        worker.ReportProgress(-1, ex.Message);
                    }
                }

                if (lastminute == dt.Minute) continue;
                lastminute = dt.Minute;
                // прошла минута
                worker.ReportProgress(lastminute, $"{dt.Minute} минута");
                try
                {
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket.SendTimeout = parameters.SendTimeout;
                        socket.ReceiveTimeout = parameters.ReceiveTimeout;
                        socket.Connect(remoteEp);
                        Thread.Sleep(500);
                        if (socket.Connected)
                        {
                            byte[] buff;
                            int numBytes, i = 0;
                            int recCount = 0;
                            DateTime d = new DateTime(1979, 1, 1);
                            bool dateExists = false;
                            for (var k = 0; k < FetchArchives.Count; k++)
                            {
                                var item = FetchArchives[k];
                                socket.Send(PrepareFetchParam(item.Node, item.Func, item.RegAddr, item.TypeValue));
                                Thread.Sleep(500);
                                buff = new byte[8192];
                                numBytes = socket.Receive(buff);
                                if (numBytes > 0)
                                {
                                    var answer = CleanAnswer(buff);
                                    if (CheckAnswer(answer, item.Node, item.Func, item.TypeValue))
                                    {
                                        var result = EncodeFetchAnswer(answer, item.Node, item.Func, item.RegAddr, item.TypeValue, item.TypeSwap, item.UnitValue);
                                        if (item.ParamName.StartsWith("$Records") && int.TryParse(result.Value, out recCount))
                                        {
                                            i = 0;
                                            dateExists = false;

                                            recCount = 5;
                                        }
                                        if (item.ParamName.StartsWith("$TimeRec") && DateTime.TryParse(result.Value, out d))
                                        {
                                            dateExists = true;
                                            i++;
                                        }
                                        if (item.LastValue != result.Value)
                                        {
                                            item.LastValue = result.Value;

                                            worker.ReportProgress(answer.Length, $"{item.ParamName}\t{result}");

                                            if (dateExists && i <= recCount && !item.ParamName.StartsWith("$"))
                                            {
                                                //if (!server.RecordExists("archives", item.ParamName, d))
                                                //{
                                                //    var columns = new Dictionary<string, object>
                                                //    {
                                                //        { "TagName", item.ParamName },
                                                //        { "Value", result.Value ?? "" },
                                                //        { "Unit", result.Unit ?? "" },
                                                //        { "Snaptime", d.ToString("yyyy-MM-dd HH:mm:ss.000") },
                                                //    };
                                                //    if (!server.InsertInto("archives", columns))
                                                //        worker.ReportProgress(answer.Length, server.LastError);
                                                //}
                                            }
                                        }
                                    }
                                }
                            }
                            socket.Disconnect(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    worker.ReportProgress(-1, ex.Message);
                }

                if (lasthour != dt.Hour && dt.Minute == 0)
                {
                    lasthour = dt.Hour;
                    // здесь закрываем предыдущий час
                }
            }
        }

        private static AnswerData EncodeFetchAnswer(byte[] answer, byte node, byte func, int regAddr, string typeValue, string typeSwap, string unitValue)
        {
            var dataset = new List<byte>(); // содержит данные ответа
            string value = string.Empty;
            switch (typeValue)
            {
                case "uint16":
                    if (answer.Length == 5)
                    {
                        var data = BitConverter.ToUInt16(Swap(answer, 3, typeSwap), 0);
                        if (unitValue == "bits")
                        {
                            var sb = new StringBuilder();
                            for (var i = 0; i < 16; i++)
                            {
                                var bc = data & 0x01;
                                if (bc > 0)
                                    sb.Insert(0, "1");
                                else
                                    sb.Insert(0, "0");
                                data = (UInt16)(data >> 1);
                                if (i % 4 == 3)
                                    sb.Insert(0, " ");
                            }
                            value = sb.ToString().Trim();
                        }
                        else
                            value = data.ToString(CultureInfo.GetCultureInfo("en-US"));
                    }
                    break;
                case "uint32":
                    if (answer.Length == 7)
                    {
                        var data = BitConverter.ToUInt32(Swap(answer, 3, typeSwap), 0);
                        if (unitValue == "UTC")
                        {
                            var dateTime = ConvertFromUnixTimestamp(data).ToLocalTime();
                            value = dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.GetCultureInfo("en-US"));
                        }
                        else
                            value = data.ToString(CultureInfo.GetCultureInfo("en-US"));
                    }
                    break;
                case "float":
                    if (answer.Length == 7)
                    {
                        var data = BitConverter.ToSingle(Swap(answer, 3, typeSwap), 0);
                        value = data.ToString("0.####", CultureInfo.GetCultureInfo("en-US"));
                    }
                    break;
                case "double":
                    if (answer.Length == 11)
                    {
                        var data = BitConverter.ToDouble(Swap(answer, 3, typeSwap), 0);
                        value = data.ToString("0.####", CultureInfo.GetCultureInfo("en-US"));
                    }
                    break;
            }
            return new AnswerData()
            {
                Node = node,
                Func = func,
                RegAddr = regAddr,
                Value = value,
                Unit = unitValue
            };
        }

        private static DateTime ConvertFromUnixTimestamp(uint timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        private static byte[] Swap(byte[] buff, int startIndex, string typeSwap)
        {
            var list = buff.Skip(startIndex).ToArray();
            if (list.Length == 2)
            {
                switch (typeSwap)
                {
                    case "AB":
                        return new byte[] { list[0], list[1] };
                    case "BA":
                        return new byte[] { list[1], list[0] };
                    default:
                        return list;
                }
            }
            else if (list.Length == 4)
            {
                switch (typeSwap)
                {
                    case "ABCD":
                        return new byte[] { list[0], list[1], list[2], list[3] };
                    case "CDAB":
                        return new byte[] { list[2], list[3], list[0], list[1] };
                    case "BADC":
                        return new byte[] { list[1], list[0], list[3], list[2] };
                    case "DCBA":
                        return new byte[] { list[3], list[2], list[1], list[0] };
                    default:
                        return list;
                }
            }
            else if (list.Length == 8)
            {
                switch (typeSwap)
                {
                    case "ABCDEFGH":
                        return new byte[] { list[0], list[1], list[2], list[3], list[4], list[5], list[6], list[7] };
                    case "GHEFCDAB":
                        return new byte[] { list[6], list[7], list[4], list[5], list[2], list[3], list[0], list[1] };
                    case "BADCFEHG":
                        return new byte[] { list[1], list[0], list[3], list[2], list[5], list[4], list[7], list[6] };
                    case "HGFEDCBA":
                        return new byte[] { list[7], list[6], list[5], list[4], list[3], list[2], list[1], list[0] };
                    default:
                        return list;
                }
            }
            else
                return list;
        }

        private static bool CheckAnswer(byte[] answer, byte node, byte func, string typeValue)
        {
            var datacount = DataLength(typeValue);
            if (datacount * 2 + 3 == answer.Length)
            {
                if (answer[0] == node && answer[1] == func && datacount * 2 == answer[2])
                    return true;
            }
            return false;
        }

        private static byte[] CleanAnswer(IEnumerable<byte> receivedBytes)
        {
            var source = new List<byte>();
            var length = 0;
            var n = 0;
            foreach (var b in receivedBytes)
            {
                if (n == 5)
                    length = b;
                else if (n > 5 && length > 0)
                {
                    source.Add(b);
                    if (source.Count == length)
                        break;
                }
                n++;
            }
            return source.ToArray();
        }

        private static byte[] PrepareFetchParam(byte node, byte func, int regAddr, string typeValue)
        {
            var datacount = DataLength(typeValue);
            var addr = regAddr - 1;
            return EncodeData(0, 0, 0, 0, 0, 6, (byte)node, (byte)(func),
                                       (byte)(addr >> 8), (byte)(addr & 0xff),
                                       (byte)(datacount >> 8), (byte)(datacount & 0xff));
        }

        private static byte[] EncodeData(params byte[] list)
        {
            var result = new byte[list.Length];
            for (var i = 0; i < list.Length; i++) result[i] = list[i];
            return result;
        }

        private static int DataLength(string typeValue)
        {
            int datacount = 1; // запрашиваем количество регистров
            switch (typeValue)
            {
                case "uint16":
                    datacount = 1;
                    break;
                case "uint32":
                case "float":
                    datacount = 2;
                    break;
                case "double":
                    datacount = 4;
                    break;
            }
            return datacount;
        }


    }

    public class AskParamData
    {
        public byte Node { get; set; }         // байт адреса прибора
        public byte Func { get; set; }         // номер функции Modbus
        public int RegAddr { get; set; }       // номер регистра
        public string TypeValue { get; set; }  // тип переменной
        public string TypeSwap { get; set; }   // тип перестановки
        public string UnitValue { get; set; }  // единица измерения
        public string ParamName { get; set; }
        public string LastValue { get; set; }
        public bool ExistsInSqlTable { get; set; }
    }

    public class AnswerData
    {
        public byte Node { get; set; }         // байт адреса прибора
        public byte Func { get; set; }         // номер функции Modbus
        public int RegAddr { get; set; }       // номер регистра
        public string Value { get; set; }
        public string Unit { get; set; }

        public override string ToString()
        {
            return $"{Node} {Func} {RegAddr} {Value} {Unit}";
        }
    }

}
