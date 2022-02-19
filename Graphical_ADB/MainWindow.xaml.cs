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
        /// <summary>
        /// adb主程序路径
        /// </summary>
        private static string File_Path = Path.Combine(Directory.GetCurrentDirectory(), @"Platform-Tools\adb.exe");
        /// <summary>
        /// 当前连接状态
        /// </summary>
        private static bool State = false;
        /// <summary>
        /// 执行adb命令
        /// </summary>
        /// <param name="Command"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="str">日志内容</param>
        private void Assignment(string str)
        {
            Dispatcher.Invoke(new Action(delegate//同步线程
            {
                日志.Text += "\n" + str.Replace("\n","");
                框.ScrollToVerticalOffset(框.ExtentHeight);
            }));
        }

        /// <summary>
        /// 图片转换
        /// </summary>
        /// <param name="stream">数据流</param>
        /// <returns>BitmapImage对象</returns>
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
                //判断设备是否离线
                if (RunCmd("devices -l").Contains("offline") || State)
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        按键.IsEnabled = false;
                        Inf.Text = "未连接到设备...";
                        设备.IsEnabled = true;
                        监控.Source = null;
                        取消.IsEnabled = false;
                        信息.Text = string.Empty;
                    }));
                    break;
                }
                //删除截屏文件
                RunCmd("shell rm /mnt/sdcard/Temp.png");
                //得到新的截屏文件
                RunCmd("shell screencap -p /mnt/sdcard/Temp.png");
                //将截屏文件拷贝到程序目录
                RunCmd("pull /mnt/sdcard/Temp.png " + Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png");
                //读取截屏文件到内存
                FileStream fs = new FileStream(Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png", FileMode.Open);
                byte[] byData = new byte[fs.Length];
                fs.Read(byData, 0, byData.Length);
                fs.Close();
                //同步线程输出截图
                Dispatcher.Invoke(new Action(delegate
                {
                    监控.Source = ToBitmapImage(new MemoryStream(byData));
                }));
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
            //连接目标设备
            string str = RunCmd("connect " + e.Argument.ToString())
                .Replace("cannot connect to ","未成功连接：")
                .Replace("already connected to ", "已连接到设备：")
                .Replace("connected to ", "成功连接设备：");
            //打印日志
            Assignment(str);
            //判断是否成功连接设备
            if (RunCmd("shell getprop ro.product.model") != string.Empty)
            {
                //得到设备名称
                string InfName = RunCmd("shell getprop ro.product.model");
                //得到设备当屏幕尺寸
                string InfSize = Substring(RunCmd("shell wm size"), "size:", "\n");
                //同步线程打印信息
                Dispatcher.Invoke(new Action(delegate
                {
                    Inf.Text = "连接到设备：" + InfName;
                    信息.Text = "实时画面 " + InfSize;
                    按键.IsEnabled = true;
                    设备.IsEnabled = false;
                    取消.IsEnabled = true;
                    State = false;
                    //开始读取屏幕实时监控
                    using (BackgroundWorker bw = new BackgroundWorker())
                    {
                        bw.DoWork += new DoWorkEventHandler(Monitoring);
                        bw.RunWorkerAsync();
                    }
                }));
            }
            else
            {
                Dispatcher.Invoke(new Action(delegate
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
            取消.IsEnabled = false;
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

        private void 取消_Click(object sender, RoutedEventArgs e)
        {
            按键.IsEnabled = false;
            Inf.Text = "未连接到设备...";
            设备.IsEnabled = true;
            监控.Source = null;
            State = true;
            RunCmd("kill-server");
            Assignment("连接已经终止");
            取消.IsEnabled = false;
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

        void Exit(object sender, DoWorkEventArgs e)
        {
            RunCmd("kill-server");
            if (File.Exists(Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png"))
                File.Delete(Directory.GetCurrentDirectory() + "/Platform-Tools/Temp.png");
            Environment.Exit(0);
        }

        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            using (BackgroundWorker bw = new BackgroundWorker())
            {
                bw.DoWork += new DoWorkEventHandler(Exit);
                bw.RunWorkerAsync();
            }
        }

        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Excuting_Order(26);
            取消_Click(null, null);
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
