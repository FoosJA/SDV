using Monitel.Mal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SDV
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private bool _isInitialized;
		public static DependencyProperty ModelImageProperty = DependencyProperty.Register("ImModel", typeof(ModelImage), typeof(MainWindow), new FrameworkPropertyMetadata(null));
		private ModelImage ImModel
		{
			get { return (ModelImage)GetValue(ModelImageProperty); }
			set { SetValue(ModelImageProperty, value); }
		}
		public MainWindow()
		{
			InitializeComponent();
			DataContext = new AppViewModel();
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_isInitialized = true;
			var viewModel = (this.DataContext as AppViewModel);
			if (viewModel != null)
			{
				viewModel.ConnectExecute();
			}

		}
		private void otiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isInitialized)
			{
				var viewModel = (this.DataContext as AppViewModel);
				if (viewModel != null)
				{
					viewModel.SelectedHList.Clear();
					if (sender is DataGrid dg)
					{
						foreach (object item in dg.SelectedItems)
						{
							viewModel.SelectedHList.Add((HalfHourMeas)item);
						}
					}
				}
			}
		}

		private void ListBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.C)
			{
				string s = listBox1.SelectedItem.ToString();
				Clipboard.SetText(s);
			}
		}

		private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isInitialized)
			{
				var viewModel = (this.DataContext as AppViewModel);
				if (viewModel != null)
				{
					viewModel.SelectedSdvList.Clear();
					if (sender is DataGrid dg)
					{
						foreach (object item in dg.SelectedItems)
						{
							viewModel.SelectedSdvList.Add((SdvMeas)item);
						}
					}
				}
			}
		}

		/*   private void MenuItem_Click(object sender, RoutedEventArgs e)
           {
               ConnectWindow connectWindow = new ConnectWindow() { Owner = App.Current.MainWindow };
               connectWindow.ShowDialog();
               ImModel = connectWindow.mImage;
           }*/
	}
}
