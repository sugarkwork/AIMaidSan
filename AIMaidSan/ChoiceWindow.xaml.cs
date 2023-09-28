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
    /// ChoiceWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ChoiceWindow : Window
    {
        public ChoiceWindow()
        {
            InitializeComponent();
            Result = null;
        }

        public object? Result { get; set; }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void AddButton(string text, object? result = null)
        {
            Button newButton = new Button
            {
                Margin = new Thickness(10, 10, 10, 10),
                FontSize = 18,
            };
            TextBlock textContent = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };
            newButton.Content = textContent;
            newButton.Click += (object sender, RoutedEventArgs e) => { Result = result; DialogResult = true; this.Close(); };
            
            buttonStackPanel.Children.Add(newButton);
        }

    }
}
