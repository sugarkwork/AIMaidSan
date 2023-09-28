using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AIMaidSan
{
    public partial class MainWindow : Window
    {
        private void MaidWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            voicevox.ExitVoiceVox();
        }

        private void MaidWindow_Closed(object sender, EventArgs e)
        {
            voicevox.ExitVoiceVox();
        }

        private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            voicevox.ExitVoiceVox();
        }
    }
}
