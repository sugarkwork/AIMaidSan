using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AIMaidSan
{
    public partial class MainWindow : Window
    {
        private async Task FadeInWindow()
        {
            for (int i = 0; i < 100; i += 15)
            {
                Dispatcher.Invoke(() =>
                {
                    main_image.Opacity = i / 100.0;
                });
                await Task.Delay(120);
            }
            Dispatcher.Invoke(() =>
            {
                main_image.Opacity = 1.0;
            });
        }
        private void main_image_Loaded(object sender, RoutedEventArgs e)
        {
            MaidWindow.Height = SystemParameters.WorkArea.Height / 2.5;
            MaidWindow.Width = MaidWindow.Height * (main_image.Source.Width / main_image.Source.Height);

            MaidWindow.Top = SystemParameters.WorkArea.Height - MaidWindow.Height;
            MaidWindow.Left = SystemParameters.WorkArea.Width - MaidWindow.Width - (SystemParameters.WorkArea.Width / 20);

            Task _;
            _ = FadeInWindow();
            _ = windowInfoControl.StartCheckActiveWindowTitle();
        }
    }

}
