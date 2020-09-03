using System;
using System.ComponentModel;
using System.Net;

namespace ModbusIntegrator
{
    partial class ModbusIntegratorProgram
    {
        static void LoadAndRunConfiguration()
        {
            var roots = "sources;sockets";
            foreach (var root in roots.Split(';'))
            {
                // загрузка корневых разделов
                foreach (var socketName in mif.ReadSectionValues(root))
                {
                    if (mif.KeyExists(socketName, "IpPort"))
                        ModbusIntegratorEventService.SetPropValue("fetching", socketName, "IpPort", mif.ReadString(socketName, "IpPort", "502"));
                    var itemName = socketName;
                    ModbusIntegratorEventService.SetPropValue("config", "add", itemName, itemName);
                    var section = $"{socketName}_nodes";
                    // загрузка узлов текущего раздела
                    foreach (var nodeName in mif.ReadSectionValues(section))
                    {
                        itemName = $"{socketName}\\{mif.ReadString(section, nodeName, nodeName)}";
                        ModbusIntegratorEventService.SetPropValue("config", "add", itemName, nodeName);
                        // загрузка общих параметров настройки узла
                        foreach (var key in mif.ReadSectionKeys(nodeName))
                        {
                            var pointname = $"{socketName}\\{nodeName}";
                            var propname = key;
                            var value = mif.ReadString(nodeName, key, "");
                            ModbusIntegratorEventService.SetPropValue("fetching", pointname, propname, value);
                        }
                        // проверка настройки включения узла
                        var actived = mif.ReadString(nodeName, "Active", "false").ToLower() == "true";
                        var modbusTcp = actived && mif.ReadString(nodeName, "LinkProtokol", "false").ToLower() == "modbus tcp";
                        if (modbusTcp &&
                           IPAddress.TryParse(mif.ReadString(nodeName, "IpAddress", "127.0.0.1"), out IPAddress ipAddr) &&
                           int.TryParse(mif.ReadString(nodeName, "IpPort", "502"), out int ipPort) &&
                           int.TryParse(mif.ReadString(nodeName, "SendTimeout", "5000"), out int sendTimeout) &&
                           int.TryParse(mif.ReadString(nodeName, "ReceiveTimeout", "5000"), out int receiveTimeout) &&
                           int.TryParse(mif.ReadString(nodeName, "ModbusNode", "247"), out int modbusNode))
                        {
                            // запуск потока для обработки устройства по протоколу Modbus Tcp
                            var worker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
                            workers.Add(worker);
                            worker.DoWork += ModbusWorker_DoWork;
                            worker.RunWorkerCompleted += ModbusWorker_RunWorkerCompleted;
                            worker.ProgressChanged += ModbusWorker_ProgressChanged;
                            var tcptuning = new TcpTuning
                            {
                                Address = ipAddr,
                                Port = ipPort,
                                SendTimeout = sendTimeout,
                                ReceiveTimeout = receiveTimeout,
                                Node = modbusNode
                            };
                            worker.RunWorkerAsync(tcptuning);
                        }
                        var fetchParamsSection = $"{nodeName}_FetchParams";
                        if (mif.SectionExists(fetchParamsSection))
                        {
                            itemName = $"{socketName}\\{nodeName}\\FetchParams";
                            ModbusIntegratorEventService.SetPropValue("config", "add", itemName, nodeName);
                            // загрузка узловых параметров опроса
                            foreach (var key in mif.ReadSectionKeys(fetchParamsSection))
                            {
                                var pointname = $"{socketName}\\{nodeName}\\FetchParams";
                                var propname = key;
                                var value = mif.ReadString(fetchParamsSection, key, "");
                                ModbusIntegratorEventService.SetPropValue("fetching", pointname, propname, value);
                            }
                        }
                        var archives = "HourArchive;DayArchive;MonthArchive".Split(';');
                        // загрузка секций настройки архивирования для узла (часовых, суточных и месячных)
                        foreach (var archiveName in archives)
                        {
                            var archiveSection = $"{nodeName}_{archiveName}";
                            if (mif.SectionExists(archiveSection))
                            {
                                itemName = $"{socketName}\\{nodeName}\\archives\\{archiveName}";
                                ModbusIntegratorEventService.SetPropValue("config", "add", itemName, nodeName);
                                // загрузка параметров настройки для архивирования
                                foreach (var key in mif.ReadSectionKeys(archiveSection))
                                {
                                    var pointname = $"{socketName}\\{nodeName}\\archives\\{archiveName}";
                                    var propname = key;
                                    var value = mif.ReadString(archiveSection, key, "");
                                    ModbusIntegratorEventService.SetPropValue("fetching", pointname, propname, value);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
