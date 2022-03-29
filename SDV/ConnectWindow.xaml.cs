using Monitel.Mal;
using Monitel.Mal.Providers;
using Monitel.Mal.Providers.Mal;
using SDV.API;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace SDV
{
	/// <summary>
	/// Логика взаимодействия для ConnectWindow.xaml
	/// </summary>
	public partial class ConnectWindow : Window
	{
		public string BaseUrl { get; set; }
		public int OdbModelVersionId { get; set; }

		public APIrequests.TokenResponse TokenRead { get; set; }

		//private MalProvider DataProvider;
		//public ModelImage mImage;
		public bool ConnectSuccess;
		public bool isClose;
		public Exception exeption;
		private readonly string path = @"C:\temp\SDV_con.txt";
		private string OdbServerName { get; set; }
		private string OdbInstanseName { get; set; }


		private void ServerName11TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			OdbServerName = textBox.Text;
		}
		private void InstanseName11TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			OdbInstanseName = textBox.Text;
		}
		private void ModelVersionTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			OdbModelVersionId = Convert.ToInt32(textBox.Text);
		}
		private void BaseUriTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			BaseUrl = textBox.Text;
		}
		public ConnectWindow()
		{
			InitializeComponent();
			try
			{
				ReadFileCon();
				ServerName11TextBox.Text = OdbServerName;
				InstanseName11TextBox.Text = OdbInstanseName;
				ModelVersionTextBox.Text = OdbModelVersionId.ToString();
				BaseUriTextBox.Text = BaseUrl;

			}
			catch (Exception ex)
			{
				Close();
				exeption = ex;
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				TokenRead = APIrequests.GetToken(BaseUrl).Result;
				SaveFileCon();
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Не удалось подключиться по web-ep. Проверьте адрес.");
			}
			finally
			{
				Close();
				Mouse.SetCursor(Cursors.Wait);
			}
			
		}
		private void SaveFileCon()
		{
			string text = $"Server11=;{OdbServerName};" +
				$"Instans11=;{OdbInstanseName};" +
				$"Model11=;{OdbModelVersionId};" +
				$"Web-ep=;{BaseUrl};";
			using (FileStream fstream = new FileStream(path, FileMode.Create))
			{
				byte[] array = System.Text.Encoding.Default.GetBytes(text);
				fstream.Write(array, 0, array.Length);
			}
		}

		private void ReadFileCon()
		{
			try
			{
				if (!Directory.Exists(@"C:\temp"))
				{
					Directory.CreateDirectory(@"C:\temp");
				}
				using (FileStream fstream = File.OpenRead(path))
				{
					byte[] array = new byte[fstream.Length];
					fstream.Read(array, 0, array.Length);
					string textFromFile = System.Text.Encoding.Default.GetString(array);
					var range = textFromFile.Split(';');
					try
					{
						OdbServerName = range[1];
						OdbInstanseName = range[3];
						OdbModelVersionId = Convert.ToInt32(range[5]);
						BaseUrl = range[7];
					}
					catch { };

				}
			}
			catch (System.IO.FileNotFoundException)
			{
				OdbServerName = @"ag-lis-aipim";
				OdbInstanseName = @"ODB_SCADA";
				OdbModelVersionId = 2678;
				BaseUrl = @"app-web-test.odusv.so";
			}
		}
	}
}
