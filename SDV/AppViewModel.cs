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

		#region Команды
		public ICommand ConnectCommand { get { return new RelayCommand(ConnectExecute); } }
        public void ConnectExecute()
        {
            StoreDB dB=new StoreDB();
            ConnectWindow connectWindow = new ConnectWindow() { Owner = App.Current.MainWindow };
			try
			{
                connectWindow.ShowDialog();
                BaseUrl = connectWindow.BaseUrl;
                mImage = connectWindow.mImage;
                dB = connectWindow.DataBase;
                Log($"Подключение выполнено!");
            }
            catch(Exception ex)
			{
                Log($"Ошибка: {ex.Message}");
            }
            Oi11List.Clear();
            MetaClass avClass = mImage.MetaData.Classes["AnalogValue"];
            IEnumerable<AnalogValue> avCollect = mImage.GetObjects(avClass).Cast<AnalogValue>();
           
            ObservableCollection<AnalogValue> hAvList = new ObservableCollection<AnalogValue>(avCollect.Where(x => x.HISPartition.Uid == new Guid("1000007B-0000-0000-C000-0000006D746C")));                       
            foreach (AnalogValue av in hAvList)
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
				{
                    oi.Id = av.externalId.Replace("Calc", "").Replace("Agr", "").Replace("RB", ""); 
                }                    
                Oi11List.Add(oi);
            }

            ObservableCollection<AnalogValue> wAvList = new ObservableCollection<AnalogValue>(avCollect.Where(x => x.HISPartition.Uid == new Guid("1000007D-0000-0000-C000-0000006D746C")));
            foreach (AnalogValue av in wAvList)
            {
                OIck11 oi = new OIck11
                {
                    Name = av.name,
                    UidMeas = av.Analog.Uid,
                    UidVal = av.Uid,
                    HISpartition = av.HISPartition.name,
                    ValueSource = av.MeasurementValueSource.name,
                    //TODO: Class
                    MeasType = av.Analog.MeasurementType.name,
                    ValueType = av.MeasurementValueType.name
                };
                if (av is ReplicatedAnalogValue avRep)
                    oi.Id = avRep.sourceId;
                else if (av.externalId != null)
				{
                    oi.Id = av.externalId.Replace("Calc", "").Replace("Agr", "").Replace("RB", "");
                }                    
                Oi11List.Add(oi);
            }
            Log($"Чтение ИМ выполнено!");
            if(dB != null)
			{
                var test = dB.GetAllOI();
            }
            



          
        }
		#endregion
		#region Стандартные команды
		public ICommand ClearInfoCollect { get { return new RelayCommand(ClearInfoExecute); } }
        private void ClearInfoExecute() { InfoCollect.Clear(); }
        #endregion
    }
}
