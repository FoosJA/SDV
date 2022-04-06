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
using System.Windows;
using System.Windows.Input;

namespace SDV
{
    class AppViewModel : AppViewModelBase
    {
        APIrequests.TokenResponse TokenRead { get; set; }

        public List<TwoMeas> SelectedOi11List = new List<TwoMeas>();

        private TwoMeas _selectedOi11;
        public TwoMeas SelectedOi11
        {
            get { return _selectedOi11; }
            set { _selectedOi11 = value; RaisePropertyChanged(); }
        }
        private ObservableCollection<TwoMeas> _oi11List = new ObservableCollection<TwoMeas>();
        public ObservableCollection<TwoMeas> Oi11List
        {
            get { return _oi11List; }
            set { _oi11List = value; RaisePropertyChanged(); }
        }

        private ModelImage mImage;
        private string BaseUrl;
        private StoreDB dB = new StoreDB();

        #region Команды
        public ICommand ConnectCommand { get { return new RelayCommand(ConnectExecute); } }
        public void ConnectExecute()
        {
            ConnectWindow connectWindow = new ConnectWindow() { Owner = App.Current.MainWindow };
            try
            {
                connectWindow.ShowDialog();
                BaseUrl = connectWindow.BaseUrl;
                mImage = connectWindow.mImage;
                dB = connectWindow.DataBase;
                Log($"Подключение выполнено!");
            }
            catch (Exception ex)
            {
                Log($"Ошибка: {ex.Message}");
            }
            Oi11List.Clear();
            MetaClass avClass = mImage.MetaData.Classes["AnalogValue"];
            IEnumerable<AnalogValue> avCollect = mImage.GetObjects(avClass).Cast<AnalogValue>();

            List<OIck11> ck11List = new List<OIck11>();

            ObservableCollection<AnalogValue> hAvList = new ObservableCollection<AnalogValue>(avCollect.Where(x => x.HISPartition.Uid == new Guid("1000007B-0000-0000-C000-0000006D746C")));
            foreach (AnalogValue av in hAvList)
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
                else
                {
                    oi.Id = av.externalId.Replace("Calc", "").Replace("Agr", "").Replace("RB", "");
                }
                ck11List.Add(oi);
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
                ck11List.Add(oi);
            }
            Log($"Чтение ИМ выполнено!");
            ObservableCollection<OIck07> Oi07List = new ObservableCollection<OIck07>();
            if (dB != null)
            {
                try
                {
                    Oi07List = dB.GetAllOI();
                    Log($"Чтение БД СК-07 выполнено!");
                }
                catch (Exception ex)
                {
                    Log("Ошибка подключения к СК-07: " + ex.Message);
                }
            }

            var test = from oi11 in ck11List
                       join oi7 in Oi07List on oi11.Id equals oi7.Id into ps
                       from oi7 in ps.DefaultIfEmpty()
                       select new TwoMeas { OIck07 = oi7, OIck11 = oi11 };

            Oi11List = new ObservableCollection<TwoMeas>(test);
            Log($"Готово!");




        }
        #endregion
        #region Стандартные команды
        public ICommand ClearInfoCollect { get { return new RelayCommand(ClearInfoExecute); } }
        private void ClearInfoExecute() { InfoCollect.Clear(); }

        public ICommand CopyCommand { get { return new RelayCommand(CopyUidTempExecute); } }

        private void CopyUidTempExecute() { Clipboard.SetText(SelectedOi11.OIck11.UidMeas.ToString()); }
        #endregion

    }

    public class TwoMeas
    {
        public OIck07 OIck07 { get; set; }
        public OIck11 OIck11 { get; set; }
    }
}

  
