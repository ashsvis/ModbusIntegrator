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
            Say($"\nLocal client: {status}");
                foreach (var socketName in mif.ReadSectionValues("sockets"))
                {
                    ModbusIntegratorEventService.SetPropValue("fetching", socketName, "IpPort", mif.ReadString(socketName, "IpPort", "502"));
                    var itemName = socketName;
                    ModbusIntegratorEventService.SetPropValue("config", "add", itemName, itemName);
                    foreach (var nodeName in mif.ReadSectionValues($"{socketName}_nodes"))
                    {
                        itemName = $"{socketName}\\{mif.ReadString($"{socketName}_nodes", nodeName, nodeName)}";
                        ModbusIntegratorEventService.SetPropValue("config", "add", itemName, nodeName);
                        foreach (var key in mif.ReadSectionKeys(nodeName))
                            ModbusIntegratorEventService.SetPropValue("fetching", $"{socketName}\\{nodeName}", key, mif.ReadString(nodeName, key, ""));
                        var fetchParamsSection = $"{nodeName}_FetchParams";
                        if (mif.SectionExists(fetchParamsSection))
                        {
                            itemName = $"{socketName}\\{nodeName}\\FetchParams";
                            ModbusIntegratorEventService.SetPropValue("config", "add", itemName, nodeName);
                            foreach (var key in mif.ReadSectionKeys(fetchParamsSection))
                            {
                                var pointname = $"{socketName}\\{nodeName}\\FetchParams";
                                var propname = key;
                                var value = mif.ReadString(fetchParamsSection, key, "");
                                ModbusIntegratorEventService.SetPropValue("fetching", pointname, propname, value);
                            }
                        }
                        var archives = "HourArchive;DayArchive;MonthArchive".Split(';');
                        foreach (var archiveName in archives)
                        {
                            var archiveSection = $"{nodeName}_{archiveName}";
                            if (mif.SectionExists(archiveSection))
                            {
                                itemName = $"{socketName}\\{nodeName}\\archives\\{archiveName}";
                                ModbusIntegratorEventService.SetPropValue("config", "add", itemName, nodeName);
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
