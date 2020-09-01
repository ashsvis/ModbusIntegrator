using ModbusIntegratorEventClient;
using System;

namespace ModbusIntegrator
{
    partial class ModbusIntegratorProgram
    {
        static EventClient locEvClient;

        static void UpdateLocalConnectionStatus(Guid clientId, ClientConnectionStatus status)
        {
            Say($"Local client: {status}");
        }

        static void ShowError(string errormessage)
        {
            Say($"Local client error: {errormessage}");
        }

        static void PropertyUpdate(DateTime servertime, string category, string pointname, string propname, string value)
        {
            Say($"Prop update at {servertime}: {category}.{pointname}.{propname}={value}");
            switch (category)
            {
                case "Fetching":
                    break;
                case "Archives":
                     break;
                case "Config":
                    break;
            }
        }


    }
}
