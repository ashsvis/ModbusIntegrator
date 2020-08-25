using System;
using System.ServiceModel;

namespace ModbusIntegrator
{
    public interface IClientCallback
    {
        [OperationContract(IsOneWay = true)]
        void PropertyUpdated(DateTime servertime, string category, string pointname, string propname, string value);

    }
}
