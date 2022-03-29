using System;
using System.Collections.Generic;

namespace SDV.Model
{
    public class OperInfo
    {
       
        public string Name { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }

        public IEnumerable<MeasValue> MeasValueList { get; set; }
    }

    public class OIck11: OperInfo
    {
        public System.Guid UidMeas { get; set; }
        public System.Guid UidVal { get; set; }
        public string Class { get; set; }
        public string MeasValueType { get; set; }
        public string ValueType { get; set; }
        public string ValueSource { get; set; }
        public string HISpartition { get; set; }
    }
    public class OIck07 : OperInfo
    {
        public string Category { get; set; }

    }

    public class MeasValue
    {
        public int QualityCode;
        public double Value;
        public DateTime Date;
    }

}
