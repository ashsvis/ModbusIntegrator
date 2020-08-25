using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace ModbusIntegrator
{
    /*
     * Класс реализации запуска WCF-сервиса. 
     * Реализован с использованием шаблона Singleton
     */
    public sealed class WcfEventService
    {
        private readonly TimeSpan _timeout = new TimeSpan(0, 1, 30);
        private static WcfEventService _wcfEventService;
        private readonly ServiceHost _svcHost;

        public static WcfEventService EventService
        {
            get
            {
                _wcfEventService = _wcfEventService ?? new WcfEventService();
                return _wcfEventService;
            }
        }

        // Конструктор по умолчанию определяется как private
        private WcfEventService()
        {
            // Регистрация сервиса и его метаданных
            _svcHost = new ServiceHost(typeof(ModbusIntegratorEventService),
                                       new[]
                                           {
                                               new Uri("net.pipe://localhost/ModbusIntegrationServer")
                                           });
            _svcHost.AddServiceEndpoint(typeof(IModbusIntegratorEventService),
                                        new NetNamedPipeBinding(), "");
            var behavior = new ServiceMetadataBehavior();
            _svcHost.Description.Behaviors.Add(behavior);
            _svcHost.AddServiceEndpoint(typeof(IMetadataExchange),
                                        MetadataExchangeBindings.CreateMexNamedPipeBinding(), "mex");
        }

        public void Start()
        {
            try
            {
                _svcHost.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                _svcHost.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}
