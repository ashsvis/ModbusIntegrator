using System;
using System.Collections.Specialized;

namespace ModbusIntegrator
{
    public class ModbusItem
    {
        public string Key { get; set; }

        public ModbusFetchError FetchError { get; set; }

        public virtual void SaveProperties(NameValueCollection coll)
        {
            if (FetchError != ModbusFetchError.Err00)
                coll.Set("FetchError", FetchError.ToString());
        }

        public virtual void LoadProperties(NameValueCollection coll)
        {
            ModbusFetchError fe;
            if (Enum.TryParse(coll["FetchError"] ?? "", out fe))
                FetchError = fe;
        }
    }

    public enum ModbusFetchError
    {
        Err00 = 0,
    }
}