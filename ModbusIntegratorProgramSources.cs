using System.Collections.Generic;
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
                        ModbusIntegratorEventService.SetPropValue("config", socketName, "IpPort", mif.ReadString(socketName, "IpPort", "502"));
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
                            ModbusIntegratorEventService.SetPropValue("config", pointname, propname, value);
                        }

                        // загрузка форматов перестановки байтов (для Modbus устройств)
                        var swapFormats = new Dictionary<string, string>();
                        foreach (var key in mif.ReadSectionKeys($"{nodeName}_SwapFormats"))
                            swapFormats.Add(key, mif.ReadString($"{nodeName}_SwapFormats", key, ""));
                        byte.TryParse(mif.ReadString(nodeName, "ModbusNode", "247"), out byte modbusNode);

                        // заполнение списка параметров опроса
                        var fetchParams = new List<AskParamData>();
                        var suffix = "FetchParams";
                        var paramsSection = $"{nodeName}_{suffix}";
                        FillFetchParameters(socketName, nodeName, swapFormats, modbusNode, fetchParams, suffix, paramsSection);
                        // заполнение списка параметров конфигурации 
                        var fetchParamsSection = $"{nodeName}_{suffix}";
                        FillConfigParameters(socketName, nodeName, suffix, fetchParamsSection);
                        var fetchArchives = new List<AskParamData>();
                        var archives = "HourArchive;DayArchive;MonthArchive".Split(';');

                        // загрузка секций настройки архивирования для узла (часовых, суточных и месячных)
                        foreach (var archiveName in archives)
                        {
                            // заполнение списка параметров опроса
                            var archivesSection = $"{nodeName}_{archiveName}";
                            suffix = $"archives\\{archiveName}";
                            FillFetchParameters(socketName, nodeName, swapFormats, modbusNode, fetchArchives, suffix, archivesSection);
                            // заполнение списка параметров конфигурации
                            var archiveSection = $"{nodeName}_{archiveName}";
                            FillConfigParameters(socketName, nodeName, suffix, archiveSection);
                        }

                        // проверка настройки включения узла
                        var actived = mif.ReadString(nodeName, "Active", "false").ToLower() == "true";
                        var modbusTcp = actived && mif.ReadString(nodeName, "LinkProtokol", "false").ToLower() == "modbus tcp";
                        if (modbusTcp &&
                           IPAddress.TryParse(mif.ReadString(nodeName, "IpAddress", "127.0.0.1"), out IPAddress ipAddr) &&
                           int.TryParse(mif.ReadString(nodeName, "IpPort", "502"), out int ipPort) &&
                           int.TryParse(mif.ReadString(nodeName, "SendTimeout", "5000"), out int sendTimeout) &&
                           int.TryParse(mif.ReadString(nodeName, "ReceiveTimeout", "5000"), out int receiveTimeout))
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
                                FetchParams = fetchParams,
                                FetchArchives = fetchArchives
                            };
                            worker.RunWorkerAsync(tcptuning);
                        }
                    }
                }
            }
        }

        private static void FillConfigParameters(string socketName, string nodeName, string suffix, string section)
        {
            if (mif.SectionExists(section))
            {
                var itemName = $"{socketName}\\{nodeName}\\{suffix}";
                ModbusIntegratorEventService.SetPropValue("config", "add", itemName, nodeName);
                // загрузка узловых параметров опроса
                foreach (var key in mif.ReadSectionKeys(section))
                {
                    var pointname = $"{socketName}\\{nodeName}\\{suffix}";
                    var propname = key;
                    var value = mif.ReadString(section, key, "");
                    ModbusIntegratorEventService.SetPropValue("config", pointname, propname, value);
                }
            }
        }

        private static void FillFetchParameters(string socketName, string nodeName, Dictionary<string, string> swapFormats, byte modbusNode, List<AskParamData> fetchParams, string suffix, string paramsSection)
        {
            foreach (var key in mif.ReadSectionKeys(paramsSection))
            {
                if (key.StartsWith("#")) continue;
                var vals = mif.ReadString(paramsSection, key, "").Split(';');
                if (!string.IsNullOrWhiteSpace(key) && vals.Length >= 4 &&
                    byte.TryParse(vals[0], out byte func) &&
                    int.TryParse(vals[1], out int regaddr))
                {
                    fetchParams.Add(new AskParamData
                    {
                        Prefix = $"{socketName}\\{nodeName}\\{suffix}",
                        ParamName = key,
                        Node = modbusNode,
                        Func = func,        // также как и Channel
                        RegAddr = regaddr,  // также как и Parameter
                        TypeValue = vals[2],
                        TypeSwap = swapFormats.ContainsKey(vals[2]) ? swapFormats[vals[2]] : string.Empty,
                        EU = vals[3]
                    });
                }
            }
        }
    }
}
