using ModbusIntegratorEventClient;
using System;

namespace ModbusIntegrator
{
    partial class ModbusIntegratorProgram
    {
        static EventClient locEvClient;

        static void UpdateLocalConnectionStatus(Guid clientId, ClientConnectionStatus status)
        {
            if (status == ClientConnectionStatus.Opened)
            {
            }
        }

        static void ShowError(string errormessage)
        {
        }

        static void PropertyUpdate(DateTime servertime, string category, string pointname, string propname, string value)
        {
            switch (category.ToLower())
            {
                case "fetching":
                    break;
                case "archives":
                     break;
                case "config":
                    //switch (pointname.ToLower())
                    //{
                    //    case "add":
                    //        var section = propname;
                    //        break;
                    //}
                    break;
            }
        }


    }
}
