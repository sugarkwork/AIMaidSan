using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AIMaidSan
{
    internal class WindowInfoControl
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "GetWindowText", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        public delegate void ChangeWindow(string windowTitle, string processName, string? productName, string? fileName, int lookTimeMin);
        public event ChangeWindow? ChangeWindowEvent;

        private Dispatcher dispatcher;
        public WindowInfoControl(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }
        public WindowInfoControl(Window window)
        {
            this.dispatcher = window.Dispatcher;
        }

        public async Task GetActiveWindowTitle()
        {
            string beforeProcess = string.Empty;
            string beforeTitle = string.Empty;
            bool printInfo = false;
            int lookCount = 0;
            int delayTimer = 6000; // ms
            int minCount = 0;

            string windowTitle = string.Empty;
            string processName = string.Empty;
            string? productName = null;
            string? fileName = null;

            while (true)
            {
                try
                {
                    var act = await Task.Run(() =>
                    {
                        Process? result = null;
                        dispatcher.Invoke(() =>
                        {
                            int processid = 0;
                            var windowHandle = GetForegroundWindow();
                            GetWindowThreadProcessId(windowHandle, out processid);
                            result = Process.GetProcessById(processid);
                        });

                        return result;
                    });

                    if (act != null)
                    {
                        if (beforeProcess != act.ProcessName)
                        {
                            beforeProcess = act.ProcessName;
                            printInfo = true;
                        }
                        if (beforeTitle != act.MainWindowTitle)
                        {
                            beforeTitle = act.MainWindowTitle;
                            printInfo = true;
                        }

                        if (printInfo)
                        {
                            windowTitle = act.MainWindowTitle;
                            processName = act.ProcessName;
                            try
                            {
                                if (act.MainModule != null)
                                {
                                    productName = act.MainModule.FileVersionInfo.ProductName;
                                    fileName = act.MainModule.FileName;
                                }
                            }
                            catch { }

                            printInfo = false;
                            lookCount = 0;
                            minCount = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.ToString());
                }

                if (lookCount == 0 || (lookCount * delayTimer) - (minCount * 60000) > 60000)
                {
                    // await Console.Out.WriteLineAsync($"over {minCount} min");

                    await Task.Run(() =>
                    {
                        if (ChangeWindowEvent != null) ChangeWindowEvent(windowTitle, processName, productName, fileName, minCount);
                    });

                    minCount++;
                }

                lookCount++;

                await Task.Delay(delayTimer);
            }
        }
    }
}
