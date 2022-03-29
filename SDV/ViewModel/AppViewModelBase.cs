using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace SDV.Model
{
    class AppViewModelBase
    {
        private int _currentProgress = 0;
        public int CurrentProgress
        {
            get => _currentProgress;
            set { _currentProgress = value; RaisePropertyChanged(); }
        }
        private int _progressMax = 1;
        public int ProgressMax
        {
            get => _progressMax;
            set { _progressMax = value; RaisePropertyChanged(); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<string> _infoCollect = new ObservableCollection<string>();
        public ObservableCollection<string> InfoCollect
        {
            get { return _infoCollect; }
            set { _infoCollect = value; RaisePropertyChanged(); }
        }
        public void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public class MyObservableCollection<AllOI> : ObservableCollection<AllOI>
        {
            public void UpdateCollection()
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                                    NotifyCollectionChangedAction.Reset));
            }
        }
        private readonly string path = @"C:\temp\SDV.log";

        public void Log(string message)
        {

            using (StreamWriter logFile = File.AppendText(path))
            {
                InfoCollect.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " " + message);
                logFile.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + message);
            }
        }

        /// <summary>
        /// Для отображения инструментов фильтрации
        /// </summary>
        public class OppositeBooleanToVisibility : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(bool)value)
                {
                    return System.Windows.Visibility.Visible;
                }
                else
                {
                    return System.Windows.Visibility.Collapsed;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                System.Windows.Visibility visibility = (System.Windows.Visibility)value;

                return visibility == System.Windows.Visibility.Visible ? false : true;
            }
        }
    }
}
