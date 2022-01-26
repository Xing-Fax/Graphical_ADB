using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Navigation;

namespace Graphical_ADB
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string File_Path = Path.Combine(Directory.GetCurrentDirectory(), @"Platform-Tools\adb.exe");

        public static string RunCmd(string Command)
        {
            Process p = new Process();
            p.StartInfo.FileName = File_Path;
            p.StartInfo.Arguments = Command;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.StandardOutputEncoding = UTF8Encoding.UTF8;
            p.Start();
            return p.StandardOutput.ReadToEnd();
        }

        public static string Substring(string sourse, string startstr, string endstr)
        {
            string result = string.Empty;
            int startindex, endindex;
            try
            {
                startindex = sourse.IndexOf(startstr);
                if (startindex == -1)
                {
                    return result;
                }
                string tmpstr = sourse.Substring(startindex + startstr.Length);
                endindex = tmpstr.IndexOf(endstr);
                if (endindex == -1)
                {
                    return result;
                }
                result = tmpstr.Remove(endindex);
            }
            catch { }
            return result;
        }

        private void Assignment(string str)
        {
            Dispatcher.Invoke(new Action(delegate//同步线程
            {
                日志.Text += "\n" + str.Replace("\n","");
                框.ScrollToVerticalOffset(框.ExtentHeight);
            }));
        }

        private BitmapImage ToBitmapImage(MemoryStream stream)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        void Monitoring(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                RunCmd("shell rm /mnt/sdcard/Temp.png");
                RunCmd("shell screencap -p /mnt/sdcard/Temp.png");
                RunCmd("pull /mnt/sdcard/Temp.png " + Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png");
                FileStream fs = new FileStream(Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png", FileMode.Open);
                byte[] byData = new byte[fs.Length];
                fs.Read(byData, 0, byData.Length);
                fs.Close();
                Dispatcher.Invoke(new Action(delegate
                {
                    监控.Source = ToBitmapImage(new MemoryStream(byData));
                }));
                if (!RunCmd("devices -l").Contains("offline"))
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        按键.IsEnabled = false;
                        Inf.Text = "设备离线...";
                        设备.IsEnabled = true;
                        监控.Source = null;
                    }));
                    break;
                }
            }

            //while (true)
            //{
            //    RunCmd("shell rm /sdcard/01.png");
            //    RunCmd("shell screencap -p /sdcard/01.png");
            //    RunCmd("pull /sdcard/01.png " + Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png");
            //    FileStream fs = new FileStream(Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png", FileMode.Open);
            //    byte[] byData = new byte[fs.Length];
            //    fs.Read(byData, 0, byData.Length);
            //    fs.Close();
            //    Dispatcher.Invoke(new Action(delegate
            //    {
            //        监控.Source = ToBitmapImage(new MemoryStream(byData));
            //    }));

            //    if (RunCmd("devices -l").Contains("offline"))
            //    {
            //        Dispatcher.Invoke(new Action(delegate {
            //            按键.IsEnabled = false;
            //            Inf.Text = "设备离线...";
            //            设备.IsEnabled = true;
            //            监控.Source = null;
            //            信息.Text = string.Empty;
            //            Assignment("设备离线");
            //        }));
            //        break;
            //    }
            //}
        }

        void Replace_file(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(new Action(delegate { 进度.IsIndeterminate = true; }));

            string str = RunCmd("connect " + e.Argument.ToString());
            Assignment(str);
            if (str.Contains("connected to") && !RunCmd("devices -l").Contains("offline"))
            {
                Dispatcher.Invoke(new Action(delegate//同步线程
                {
                    Inf.Text = "连接到设备：" + RunCmd("shell getprop ro.product.model");
                    信息.Text = Substring(RunCmd("shell wm size"), "size:", "\n");
                    按键.IsEnabled = true;
                    设备.IsEnabled = false;
                    using (BackgroundWorker bw = new BackgroundWorker())
                    {
                        bw.DoWork += new DoWorkEventHandler(Monitoring);
                        bw.RunWorkerAsync();
                    }
                }));
            }
            else
            {
                Dispatcher.Invoke(new Action(delegate//同步线程
                {
                    Inf.Text = "设备连接失败...";
                }));
            }

            Dispatcher.Invoke(new Action(delegate { 进度.IsIndeterminate = false; }));
        }

        private static string Target_Device;

        public MainWindow()
        {
            InitializeComponent();
            按键.IsEnabled = false;
        }

        private void Button_Click_15(object sender, RoutedEventArgs e)
        {
            按键.IsEnabled = false;
            Target_Device = IP.Text;
            日志.Text = "开始连接设备：" + Target_Device;
            using (BackgroundWorker bw = new BackgroundWorker())
            {
                bw.DoWork += new DoWorkEventHandler(Replace_file);
                bw.RunWorkerAsync(Target_Device);
            }
        }

        private void 标题_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }

        private void Button_Click_14(object sender, RoutedEventArgs e)
        {
            RunCmd("Reboot");
        }

        void Implement(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(new Action(delegate { 进度.IsIndeterminate = true; }));
            RunCmd("shell input keyevent  keycode " + e.Argument.ToString());

            Assignment("执行完毕! -> " + e.Argument.ToString());
            Dispatcher.Invoke(new Action(delegate { 进度.IsIndeterminate = false; }));
        }

        private void Excuting_Order(int nID)
        {
            using (BackgroundWorker bw = new BackgroundWorker())
            {
                bw.DoWork += new DoWorkEventHandler(Implement);
                bw.RunWorkerAsync(nID);
            }
        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            RunCmd("kill-server");
            if (File.Exists(Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png"))
                File.Delete(Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png");
            Environment.Exit(0);
        }

        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Excuting_Order(26);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Excuting_Order(24);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Excuting_Order(25);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Excuting_Order(82);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            Excuting_Order(3);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            Excuting_Order(4);
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            Excuting_Order(19);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            Excuting_Order(20);
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            Excuting_Order(21);
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            Excuting_Order(22);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            Excuting_Order(23);
        }
    }
}
