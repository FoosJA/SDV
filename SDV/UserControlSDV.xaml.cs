using Monitel.Mal;
using SDV.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Логика взаимодействия для UserControlSDV.xaml
    /// </summary>
    public partial class UserControlSDV : UserControl
    {
        public static readonly DependencyProperty myModelImageProperty = DependencyProperty.Register("myImModel", typeof(ModelImage), typeof(UserControl), new FrameworkPropertyMetadata(null));
        private ModelImage myImModel
        {
            get { return (ModelImage)GetValue(myModelImageProperty); }
            set { SetValue(myModelImageProperty, value);}
        }


        public UserControlSDV()
        {
            InitializeComponent();
            DataContext = new AppViewModel();
        }

        private void otiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*if (ViewModel != null)
            {
                ViewModel.SelectedOi11List.Clear();
                if (sender is DataGrid dg)
                {
                    foreach (object item in dg.SelectedItems)
                    {
                        ViewModel.SelectedOi11List.Add((OIck11)item);
                    }
                }
            }*/
        }   
    }
}
