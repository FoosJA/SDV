using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SDV.Model
{
    public class IOperInfo
    {
       
        public string Name { get; set; }
        public string Id { get; set; }
        private ObservableCollection<MeasValue> _measValList = new ObservableCollection<MeasValue>();
        ///// <summary>
        ///// какой-то тип
        ///// </summary>
        //public string Type { get; set; }
        /// <summary>
        /// Значения измерений
        /// </summary>
        public ObservableCollection<MeasValue> MeasValueList
        {
            get { return _measValList; }
            set { _measValList = value; RaisePropertyChanged(); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
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
        public int QualityCode { get; set; }
        public double Value { get; set; }
        public DateTime Date { get; set; }
    }

}
