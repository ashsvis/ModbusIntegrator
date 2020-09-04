using ModbusIntegratorEvent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.ServiceProcess;

namespace ModbusIntegrator
{
    partial class ModbusIntegratorProgram
    {
        private static readonly ConcurrentDictionary<string, ModbusItem> DictModbusItems =
            new ConcurrentDictionary<string, ModbusItem>();

        private static MemIniFile mif;

        static List<BackgroundWorker> workers = new List<BackgroundWorker>();

        static void Main(string[] args)
        {
            // загрузка текущей конфигурации сервера
            var configName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModbusIntegrator.ini");
            mif = new MemIniFile(configName);

            // запуск фонового процесса для прослушивания сокета Modbus Tcp 502
            var worker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            workers.Add(worker);
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;
            int.TryParse(mif.ReadString("default", "IpPort", "502"), out int port);
            var tcptuning = new TcpTuning { Port = port };
            worker.RunWorkerAsync(tcptuning);

            locEvClient = new EventClient();
            locEvClient.Connect(new[] { "config", "fetching", "archives" }, PropertyUpdate, ShowError, UpdateLocalConnectionStatus);

            LoadAndRunConfiguration();

            // если запускает пользователь сам
            if (Environment.UserInteractive)
            {
                var s = WcfEventService.EventService;
                s.Start();
                try
                {
                    Console.WriteLine($"{mif.ReadString("integrator", "descriptor", "Unknown program")}, ver {mif.ReadString("integrator", "version", "unknown")}");
                    Console.WriteLine();
                    Console.WriteLine("Type any key to exit");
                    Console.ReadKey();
                }
                finally
                {
                    s.Stop();
                }
            }
            else
            {
                // запуск в виде службы Windows
                var servicesToRun = new ServiceBase[] { new WinService() };
                ServiceBase.Run(servicesToRun);
            }

            // выгрузка фонового процесса при окончании работы сервиса
            worker.CancelAsync();
        }
    }
}
