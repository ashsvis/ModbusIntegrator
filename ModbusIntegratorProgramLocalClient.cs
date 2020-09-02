using ModbusIntegratorEventClient;
using System;

namespace ModbusIntegrator
{
    partial class ModbusIntegratorProgram
    {
        static EventClient locEvClient;

        static void UpdateLocalConnectionStatus(Guid clientId, ClientConnectionStatus status)
        {
            //Say($"Local client: {status}");
            if (status == ClientConnectionStatus.Opened)
            {
                foreach (var socketName in mif.ReadSectionValues("sockets"))
                {
                    var itemName = socketName;
                    locEvClient.UpdateProperty("config", "add", itemName, itemName);
                    foreach (var nodeName in mif.ReadSectionValues($"{socketName}_nodes"))
                    {
                        itemName = $"{socketName}\\{mif.ReadString($"{socketName}_nodes", nodeName, nodeName)}";
                        locEvClient.UpdateProperty("config", "add", itemName, nodeName);
                        foreach (var key in mif.ReadSectionKeys(nodeName))
                        {
                            locEvClient.UpdateProperty("fetching", $"{socketName}\\{nodeName}", key, mif.ReadString(nodeName, key, ""));
                        }
                    }
                }
            }
        }

        static void ShowError(string errormessage)
        {
            Say($"Local client error: {errormessage}");
        }

        static void PropertyUpdate(DateTime servertime, string category, string pointname, string propname, string value)
        {
            //Say($"Prop update at {servertime}: {category}.{pointname}.{propname}={value}");
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
