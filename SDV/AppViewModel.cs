using Monitel.Mal;
using Monitel.Mal.Context.CIM16;
using SDV.API;
using SDV.Foundation;
using SDV.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SDV
{
    class AppViewModel : AppViewModelBase
    {		
        APIrequests.TokenResponse TokenRead { get; set; }
        public List<OIck11> SelectedOi11List = new List<OIck11>();

        private OIck11 _selectedOi11;
        public OIck11 SelectedOi11
        {
            get { return _selectedOi11; }
            set { _selectedOi11 = value; RaisePropertyChanged(); }
        }
        private ObservableCollection<OIck11> _oi11List = new ObservableCollection<OIck11>();
        public ObservableCollection<OIck11> Oi11List
        {
            get { return _oi11List; }
            set { _oi11List = value; RaisePropertyChanged(); }
        }
        private ModelImage mImage;
        private string BaseUrl;
        

        public ICommand ConnectCommand { get { return new RelayCommand(ConnectExecute); } }
        public void ConnectExecute()
        {     
            ConnectWindow connectWindow = new ConnectWindow() { Owner = App.Current.MainWindow };
			try
			{
                connectWindow.ShowDialog();
                BaseUrl = connectWindow.BaseUrl;
                mImage = connectWindow.mImage;
                Log($"Подключение выполнено!");
            }
            catch(Exception ex)
			{
                Log($"Ошибка: {ex.Message}");
            }
            Oi11List.Clear();
            MetaClass avClass = mImage.MetaData.Classes["AnalogValue"];
            IEnumerable<AnalogValue> avCollect = mImage.GetObjects(avClass).Cast<AnalogValue>();
            ObservableCollection<AnalogValue> avList = new ObservableCollection<AnalogValue>(avCollect.Where(x => x.HISPartition.Uid == new Guid("1000007B-0000-0000-C000-0000006D746C")));

            foreach (AnalogValue av in avList)
            {
                OIck11 oi = new OIck11
                {
                    Name = av.name,
                    UidMeas = av.Analog.Uid,
                    UidVal = av.Uid,
                    HISpartition=av.HISPartition.name,
                    ValueSource=av.MeasurementValueSource.name,
                    //TODO: Class
                    MeasType=av.Analog.MeasurementType.name,
                    ValueType=av.MeasurementValueType.name  
                };                
                if (av is ReplicatedAnalogValue avRep)                
                    oi.Id = avRep.sourceId;
                else
                    oi.Id = av.externalId;
                Oi11List.Add(oi);
            }
            Log($"Чтение ИМ выполнено!");





            //OIck11 oi = new OIck11
            //{
            //    Name = "2",
            //    UidMeas = Guid.Empty,
            //    UidVal = Guid.Empty
            //};
            //Oi11List.Add(oi);
            // 
            /*ConnectWindow connectWindow = new ConnectWindow() { Owner = App.Current.MainWindow };
            connectWindow.ShowDialog();

            if (connectWindow.isClose)
            {
                if (connectWindow.ConnectSuccess)
                {
                    Log("Подключение к СК-11 выполнено!");
                    BaseUrl = connectWindow.BaseUrl;
                    mImage = connectWindow.mImage;
                    Oi11List.Clear();
                    MetaClass avClass = mImage.MetaData.Classes["AnalogValue"];
                    IEnumerable<AnalogValue> avCollect = mImage.GetObjects(avClass).Cast<AnalogValue>();
                    foreach (var av in avCollect)
                    {
                        string externalId;
                        if (av is ReplicatedAnalogValue rav)
                            externalId = rav.sourceId;
                        else 
                            externalId = av.externalId;
                        if (externalId!= String.Empty && externalId!= null)
                        {
                            OIck11 oi = new OIck11
                            {
                                Name = av.name,
                                UidMeas = av.Uid,
                                UidVal = av.Analog.Uid,
                                MeasValueType = av.MeasurementValueType.name,
                                Class = mImage.MetaData.Classes.First(x => x.Id == av.ClassId).DisplayName,
                                Type = externalId.Substring(0, 1),
                                Id = externalId.Substring(1),
                                ValueType = av.Analog.MeasurementType.name
                            };
                            Oi11List.Add(oi);
                        }
                            

                    }
                    Log("Чтение ИМ выполнено!");
                }
                else
                {
                    Log("Ошибка подключения к СК-11");
                    Log(connectWindow.exeption.Message);
                }
            }*/
        }
    }
}
