using Monitel.Mal;
using Monitel.Mal.Providers;
using Monitel.Mal.Providers.Mal;
using SDV.API;
using SDV.Foundation;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
		private MalProvider DataProvider;
		public ModelImage mImage;
		public StoreDB DataBase;
		public bool ConnectSuccess;
		public bool ConnectSuccess07;
		public bool isClose;
		private readonly string path = @"C:\temp\SDV_con.txt";
		private string OdbServerName { get; set; }
		private string OdbInstanseName { get; set; }
		private int OdbModelVersionId { get; set; }
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
				ServerName07TextBox.Text = OdbServerName07;
				InstanseName07TextBox.Text = OdbInstanseName07;
			}
			catch (Exception ex)
			{
				Close();
				exeption = ex;
			}

		}

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

		private string OdbServerName07 { get; set; }
		private string OdbInstanseName07 { get; set; }

		private void BaseUriTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			BaseUrl = textBox.Text;
		}
		private void ServerName07TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			OdbServerName07 = textBox.Text;
		}
		private void InstanseName07TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = (TextBox)sender;
			OdbInstanseName07 = textBox.Text;
		}
		public Exception exeption;
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			string exep = "";
			try
			{
				MalContextParams context = new MalContextParams()
				{
					OdbServerName = OdbServerName,
					OdbInstanseName = OdbInstanseName,
					OdbModelVersionId = OdbModelVersionId,
				};
				DataProvider = new MalProvider(context, MalContextMode.Open, "test", -1);
				mImage = new ModelImage(DataProvider, true);
				SaveFileCon();
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Не удалось подключиться к СК-11. Проверьте строки подключения.", ex);
			}
			finally
			{
				Close();
				Mouse.SetCursor(Cursors.Wait);
			}
			try
			{				
				DataBase = new StoreDB { serverName = OdbServerName07, dbName = OdbInstanseName07 };
				var strBuilder = new SqlConnectionStringBuilder()
				{
					DataSource = DataBase.serverName,
					IntegratedSecurity = true,
					InitialCatalog = DataBase.dbName
				};
				var connString = strBuilder.ConnectionString;
				using (var connection = new SqlConnection(connString))
				{
					connection.Open();
					ConnectSuccess07 = true;
					connection.Close();
				}
				SaveFileCon();
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Не удалось подключиться к СК-07. Проверьте строки подключения.", ex);
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
				$"Web-ep=;{BaseUrl};" +
				$"Server07=;{OdbServerName07};" +
				$"Instans07=;{OdbInstanseName07}";
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
						OdbServerName07 = range[9];
						OdbInstanseName07 = range[11];
					}
					catch { };

				}
			}
			catch (System.IO.FileNotFoundException)
			{
				OdbServerName = @"ag-lis-aipim";
				OdbInstanseName = @"ODB_SCADA";
				OdbModelVersionId = 2159;
				BaseUrl = @"sv-app-web-wsfc.odusv.so";
				OdbServerName07 = @"ck07-test3";
				OdbInstanseName07 = @"OIK";
			}
		}
	}
}
