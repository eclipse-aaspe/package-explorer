using AasCore.Aas3_1;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace AdminShellNS.DiaryData
{
    public class TaintedDataDef
    {
        [XmlIgnore]
        [JsonIgnore]
        private DateTime? _tainted = null;

        [XmlIgnore]
        [JsonIgnore]
        public DateTime? Tainted
        {
            get { return _tainted; }
            set { _tainted = value; }
        }

        public static void TaintIdentifiable(IReferable element)
        {
            // trivial
            if (element == null)
                return;

            // find identifiable
            var el = element;
            while (el != null)
            {
                // found?
                if (el is IIdentifiable && el is ITaintedData itd)
                {
                    itd.TaintedData.Tainted = DateTime.UtcNow;
                    return;
                }

                // up
                el = (el as IReferable)?.Parent as IReferable;
            }
        }
    }
}
