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
using System.Windows.Shapes;

namespace AIMaidSan
{
    /// <summary>
    /// InputWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class InputWindow : Window
    {
        public InputWindow()
        {
            InitializeComponent();
            Result = string.Empty;
        }

        public string Result { get; set; }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Result = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(Result))
            {
                DialogResult = false;
            }
            else
            {
                DialogResult = true;
            }

            this.Close();
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
