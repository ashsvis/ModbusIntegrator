using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ModbusIntegrator
{
    partial class ModbusIntegratorProgram
    {
        static void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //var worker = (BackgroundWorker)sender;
            Console.WriteLine($"{e.UserState}");
        }

        static void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //var worker = (BackgroundWorker)sender;

        }

        static void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            //var lastsecond = DateTime.Now.Second;
            //var lastminute = -1;
            //var lasthour = -1;

            //while (!worker.CancellationPending)
            //{
            //    var dt = DateTime.Now;
            //    if (lastsecond == dt.Second) continue;
            //    lastsecond = dt.Second;
            //    // прошла секунда
            //    //worker.ReportProgress(lastsecond, $"{dt.Second} секунда");

            //    if (lastminute == dt.Minute) continue;
            //    lastminute = dt.Minute;
            //    // прошла минута
            //    worker.ReportProgress(lastminute, $"{dt.Minute} минута");

            //    if (lasthour != dt.Hour && dt.Minute == 0)
            //    {
            //        lasthour = dt.Hour;
            //        // здесь закрываем предыдущий час
            //    }
            //}

            #region работа с Tcp портом

            var tt = e.Argument as TcpTuning;
            if (tt != null)
            {
                const int socketTimeOut = 90000;
                var listener = new TcpListener(IPAddress.Any, tt.Port)
                {
                    Server = { SendTimeout = socketTimeOut, ReceiveTimeout = socketTimeOut }
                };
                Thread.Sleep(100);
                Say(worker, $"\nСокет localhost:{tt.Port} прослушивается...");
                do
                {
                    Thread.Sleep(1);
                    try
                    {
                        listener.Start(10);
                        // Buffer for reading data
                        var bytes = new byte[256];

                        while (!listener.Pending())
                        {
                            Thread.Sleep(1);
                            if (!worker.CancellationPending) continue;
                            listener.Stop();
                            e.Cancel = true;
                            Say(worker, $"\nСокет TCP({tt.Port}) - остановка прослушивания.");
                            return;
                        }
                        var clientData = listener.AcceptTcpClient();
                        // создаем отдельный поток для каждого подключения клиента
                        ThreadPool.QueueUserWorkItem(arg =>
                        {
                            try
                            {
                                // Get a stream object for reading and writing
                                var stream = clientData.GetStream();
                                int count;
                                // Loop to receive all the data sent by the client.
                                while ((count = stream.Read(bytes, 0, bytes.Length)) != 0)
                                {
                                    //Thread.Sleep(1);
                                    var list = new List<string>();
                                    for (var i = 0; i < count; i++) list.Add(string.Format("{0}", bytes[i]));
                                    //Say("Q:" + string.Join(",", list));

                                    if (count < 6) continue;
                                    var header1 = Convert.ToUInt16(bytes[0] * 256 + bytes[1]);
                                    var header2 = Convert.ToUInt16(bytes[2] * 256 + bytes[3]);
                                    var packetLen = Convert.ToUInt16(bytes[4] * 256 + bytes[5]);
                                    if (count != packetLen + 6) continue;
                                    var nodeAddr = bytes[6];
                                    var funcCode = bytes[7];
                                    var startAddr = Convert.ToUInt16(bytes[8] * 256 + bytes[9]);
                                    var regCount = Convert.ToUInt16(bytes[10] * 256 + bytes[11]);
                                    var singleValue = Convert.ToUInt16(bytes[10] * 256 + bytes[11]);
                                    EnsureNode(nodeAddr);
                                    List<byte> answer;
                                    byte bytesCount;
                                    string childName;
                                    ModbusItem modbusitem;
                                    ModbusHoldingRegister modbusHr;
                                    byte[] msg;
                                    switch (funcCode)
                                    {
                                        case 3: // - read holding registers
                                        case 4: // - read input registers
                                            answer = new List<byte>();
                                            answer.AddRange(BitConverter.GetBytes(Swap(header1)));
                                            answer.AddRange(BitConverter.GetBytes(Swap(header2)));
                                            bytesCount = Convert.ToByte(regCount * 2);
                                            packetLen = Convert.ToUInt16(bytesCount + 3); // 
                                            answer.AddRange(BitConverter.GetBytes(Swap(packetLen)));
                                            answer.Add(nodeAddr);
                                            answer.Add(funcCode);
                                            answer.Add(bytesCount);
                                            for (var addr = 0; addr < regCount; addr++)
                                            {
                                                if (funcCode == 3)
                                                    EnsureModbusHR(nodeAddr, startAddr + addr);
                                                else
                                                    EnsureModbusAI(nodeAddr, startAddr + addr);
                                                childName = funcCode == 3 ? $"Node{nodeAddr}.HR{startAddr + addr}" : $"Node{nodeAddr}.AI{startAddr + addr}";
                                                while (!DictModbusItems.TryGetValue(childName, out modbusitem)) Thread.Sleep(10);
                                                ushort value;
                                                if (funcCode == 3)
                                                {
                                                    modbusHr = (ModbusHoldingRegister)modbusitem;
                                                    value = BitConverter.ToUInt16(BitConverter.GetBytes(modbusHr.IntValue), 0);
                                                }
                                                else
                                                {
                                                    var modbusAi = (ModbusAnalogInput)modbusitem;
                                                    value = BitConverter.ToUInt16(BitConverter.GetBytes(modbusAi.IntValue), 0);

                                                }
                                                answer.AddRange(BitConverter.GetBytes(Swap(value)));
                                            }
                                            list.Clear();
                                            list.AddRange(answer.Select(t => string.Format("{0}", t)));
                                            //Say("A:" + string.Join(",", list));
                                            msg = answer.ToArray();
                                            stream.Write(msg, 0, msg.Length);
                                            break;
                                        case 6: // write one register
                                            answer = new List<byte>();
                                            answer.AddRange(BitConverter.GetBytes(Swap(header1)));
                                            answer.AddRange(BitConverter.GetBytes(Swap(header2)));
                                            bytesCount = Convert.ToByte(regCount * 2);
                                            packetLen = Convert.ToUInt16(bytesCount + 3); // 
                                            answer.AddRange(BitConverter.GetBytes(Swap(packetLen)));
                                            answer.Add(nodeAddr);
                                            answer.Add(funcCode);
                                            answer.AddRange(BitConverter.GetBytes(Swap(startAddr)));
                                            answer.AddRange(BitConverter.GetBytes(Swap(singleValue)));
                                            EnsureModbusHR(nodeAddr, startAddr);
                                            childName = $"Node{nodeAddr}.HR{startAddr}";
                                            while (!DictModbusItems.TryGetValue(childName, out modbusitem)) Thread.Sleep(10);
                                            modbusHr = (ModbusHoldingRegister)modbusitem;
                                            modbusHr.IntValue = BitConverter.ToInt16(BitConverter.GetBytes(singleValue), 0);
                                            while (!DictModbusItems.TryUpdate(childName, modbusHr, modbusHr)) Thread.Sleep(10);
                                            locEvClient.UpdateProperty("Fetching", $"Node:{nodeAddr}:{funcCode}", $"HR:{startAddr}", $"{modbusHr.IntValue}", false);
                                            list.Clear();
                                            list.AddRange(answer.Select(t => string.Format("{0}", t)));
                                            //Say("A:" + string.Join(",", list));
                                            msg = answer.ToArray();
                                            stream.Write(msg, 0, msg.Length);
                                            break;
                                        case 16: // write several registers
                                            answer = new List<byte>();
                                            answer.AddRange(BitConverter.GetBytes(Swap(header1)));
                                            answer.AddRange(BitConverter.GetBytes(Swap(header2)));
                                            answer.AddRange(BitConverter.GetBytes(Swap(6)));
                                            answer.Add(nodeAddr);
                                            answer.Add(funcCode);
                                            answer.AddRange(BitConverter.GetBytes(Swap(startAddr)));
                                            answer.AddRange(BitConverter.GetBytes(Swap(regCount)));
                                            var bytesToWrite = bytes[12];
                                            if (bytesToWrite != regCount * 2) break;
                                            var n = 13;
                                            for (var i = 0; i < regCount; i++)
                                            {
                                                var value = Convert.ToUInt16(bytes[n] * 256 + bytes[n + 1]);
                                                EnsureModbusHR(nodeAddr, startAddr + i);
                                                childName = $"Node{nodeAddr}.HR{startAddr + i}";
                                                while (!DictModbusItems.TryGetValue(childName, out modbusitem)) Thread.Sleep(10);
                                                modbusHr = (ModbusHoldingRegister)modbusitem;
                                                modbusHr.IntValue = BitConverter.ToInt16(BitConverter.GetBytes(value), 0);
                                                while (!DictModbusItems.TryUpdate(childName, modbusHr, modbusHr)) Thread.Sleep(10);
                                                locEvClient.UpdateProperty("Fetching", $"Node:{nodeAddr}:{funcCode}", $"HR:{startAddr + i}", $"{modbusHr.IntValue}", false);
                                                n = n + 2;
                                            }
                                            list.Clear();
                                            list.AddRange(answer.Select(t => string.Format("{0}", t)));
                                            //Say("A:" + string.Join(",", list));
                                            msg = answer.ToArray();
                                            stream.Write(msg, 0, msg.Length);
                                            break;
                                    }
                                }
                                // Shutdown and end connection
                                clientData.Close();
                            }
                            catch (Exception ex)
                            {
                                if (!worker.CancellationPending) Say(worker, ex.Message);
                            }
                        });
                    }
                    catch (SocketException exception)
                    {
                        if (!worker.CancellationPending)
                            Say(worker, $"Ошибка приёма: {exception.Message}");
                        break;
                    }
                } while (!worker.CancellationPending);
                listener.Stop();
                Say(worker, $"\nСокет TCP({tt.Port}) - остановка прослушивания.");
            }

            #endregion работа с Tcp портом
        }

        private static void Say(BackgroundWorker worker, string message)
        {
            worker.ReportProgress(-1, message);
        }

        private static void EnsureModbusAI(byte nodeAddr, int addr)
        {
            EnsureNode(nodeAddr);
            var childName = $"Node{nodeAddr}.AI{addr}";
            DictModbusItems.TryAdd(childName, new ModbusAnalogInput { Key = childName });
        }

        private static void EnsureModbusHR(byte nodeAddr, int addr)
        {
            EnsureNode(nodeAddr);
            var childName = $"Node{nodeAddr}.HR{addr}";
            DictModbusItems.TryAdd(childName, new ModbusHoldingRegister { Key = childName });
        }

        private static void EnsureNode(byte nodeAddr)
        {
            var childName = $"Node{nodeAddr}";
            DictModbusItems.TryAdd(childName, new ModbusNode { Key = childName });
        }

        private static ushort Swap(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            var buff = bytes[0];
            bytes[0] = bytes[1];
            bytes[1] = buff;
            return BitConverter.ToUInt16(bytes, 0);
        }


    }
}
