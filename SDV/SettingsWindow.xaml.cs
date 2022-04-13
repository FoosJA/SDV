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
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            ReadFileCon();
        }
        public SettingsWindow(bool trigAgreg, bool trigCalc, bool trigRepValue)
        {
            InitializeComponent();
            ReadFileCon();
            TriggerAgreg = trigAgreg;
            TriggerCalc = trigCalc;
            TriggerСreateRep = trigRepValue;
            AnalogTextBox.Text = GuidAnalog.ToString();
            DiscreteTextBox.Text = GuidDiscrete.ToString();
        }

        private bool _triggerGreateOperand;
        public bool TriggerСreateRep
        {
            get { return _triggerGreateOperand; }
            set { _triggerGreateOperand = value; checkBoxCreateRep.IsChecked = value; }
        }
        private bool _triggerAgreg;
        public bool TriggerAgreg
        {
            get { return _triggerAgreg; }
            set { _triggerAgreg = value; checkBoxAgreg.IsChecked = value; }
        }
        private bool _triggerCalc;
        public bool TriggerCalc
        {
            get { return _triggerCalc; }
            set { _triggerCalc = value; checkBoxCalc.IsChecked = value; }
        }

        public Guid GuidAnalog { get; set; }

        public Guid GuidDiscrete { get; set; }
        public bool SaveChange = false;


        private void checkBoxCreateRep_Checked(object sender, RoutedEventArgs e)
        {
            TriggerСreateRep = true;
            AnalogTextBox.IsEnabled = true;
            DiscreteTextBox.IsEnabled = true;
        }

        private void checkBoxCreateRep_Unchecked(object sender, RoutedEventArgs e)
        {
            TriggerСreateRep = false;
            AnalogTextBox.IsEnabled = false;
            DiscreteTextBox.IsEnabled = false;
        }

        private void AnalogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                GuidAnalog = new Guid(AnalogTextBox.Text);
            }
            catch (FormatException) { };
        }

        private void DiscreteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                GuidDiscrete = new Guid(DiscreteTextBox.Text);
            }
            catch (FormatException) { };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TriggerСreateRep = true;
            GuidAnalog = new Guid("B5D67D78-F557-4BCB-B2DD-5AC78E618930");
            GuidDiscrete = new Guid("57E015D7-4A7C-4EC9-A0A5-B430F37D4A48");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SaveChange = true;
            SaveFileCon();
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SaveChange = false;
            Close();
        }

        private readonly string path = @"C:\temp\CreateCalcVal_Create.txt";
        private void SaveFileCon()
        {
            string text = $"Analog=;{GuidAnalog};" +
                $"Discrete=;{GuidDiscrete};";
            using (FileStream fstream = new FileStream(path, FileMode.OpenOrCreate))
            {
                byte[] array = System.Text.Encoding.Default.GetBytes(text);
                fstream.Write(array, 0, array.Length);
            }
        }
        private void ReadFileCon()
        {
            try
            {
                using (FileStream fstream = File.OpenRead(path))
                {
                    byte[] array = new byte[fstream.Length];
                    fstream.Read(array, 0, array.Length);
                    string textFromFile = System.Text.Encoding.Default.GetString(array);
                    var range = textFromFile.Split(';');
                    try
                    {
                        GuidAnalog = new Guid(range[1]);
                        GuidDiscrete = new Guid(range[3]);
                    }
                    catch (System.FormatException)
                    {
                        GuidAnalog = Guid.Empty;
                        GuidDiscrete = Guid.Empty;
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                GuidAnalog = new Guid("B5D67D78-F557-4BCB-B2DD-5AC78E618930");
                GuidDiscrete = new Guid("57E015D7-4A7C-4EC9-A0A5-B430F37D4A48");
            };
        }

        private void checkBoxAgreg_Checked(object sender, RoutedEventArgs e)
        {
            TriggerAgreg = true;
        }

        private void checkBoxAgreg_Unchecked(object sender, RoutedEventArgs e)
        {
            TriggerAgreg = false;
        }

        private void checkBoxCalc_Checked(object sender, RoutedEventArgs e)
        {
            TriggerCalc = true;
        }

        private void checkBoxCalc_Unchecked(object sender, RoutedEventArgs e)
        {
            TriggerCalc = false;
        }
    }
}
