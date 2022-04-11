using System;
using System.Collections.Generic;

namespace SDV.Model
{
    public class IOperInfo
    {
       
        public string Name { get; set; }
        public string Id { get; set; }
        ///// <summary>
        ///// какой-то тип
        ///// </summary>
        //public string Type { get; set; }
        /// <summary>
        /// Значения измерений
        /// </summary>
        public IEnumerable<MeasValue> MeasValueList { get; set; }
    }

    public class OIck11: IOperInfo
    {
        /// <summary>
        /// UID измерения
        /// </summary>
        public System.Guid UidMeas { get; set; }
        /// <summary>
        /// UID значения 
        /// </summary>
        public System.Guid UidVal { get; set; }
        /// <summary>
        /// Класс значения 
        /// </summary>
        public string Class { get; set; }
        /// <summary>
        /// Тип значения
        /// </summary>
        public string ValueType { get; set; }
        /// <summary>
        /// Тип измерения
        /// </summary>
        public string MeasType { get; set; }
        /// <summary>
        /// Источник значения
        /// </summary>
        public string ValueSource { get; set; }
        /// <summary>
        /// Стратегия хранения
        /// </summary>
        public string HISpartition { get; set; }
    }
    public class OIck07 : IOperInfo
    {
        public string CategoryH { get; set; }
        public string CategoryW { get; set; }

    }

    public class MeasValue
    {
        public int QualityCode;
        public double Value;
        public DateTime Date;
    }

}
