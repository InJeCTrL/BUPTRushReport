﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BUPTRushReport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<User> UserList = new ObservableCollection<User>();
        private Timer t_Daily, t_Morning, t_Moon, t_Night;
        public MainWindow()
        {
            InitializeComponent();
            ReadCfgFromFile();
            UserGrid.DataContext = UserList;
            t_Daily = new Timer(Timedup_Daily);
            SetTime_Daily();
            t_Morning = new Timer(Timedup_Morning);
            SetTime_Morning();
            t_Moon = new Timer(Timedup_Moon);
            SetTime_Moon();
            t_Night = new Timer(Timedup_Night);
            SetTime_Night();
        }
        /// <summary>
        /// 从导出文件读取配置
        /// </summary>
        private void ReadCfgFromFile()
        {
            try
            {
                using (FileStream fs = new FileStream("userlist.xml", FileMode.Open))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<User>));
                    UserList = (ObservableCollection<User>)xmlSerializer.Deserialize(fs);
                    //MessageBox.Show("已导入现有配置");
                }
            }
            catch
            {
                MessageBox.Show("导入现有配置失败");
            }
        }
        /// <summary>
        /// 达到每日填报预定时间
        /// </summary>
        /// <param name="obj"></param>
        private void Timedup_Daily(object obj)
        {
            RushList(DateTime.Now, 0);
            SetTime_Daily(true);
        }
        /// <summary>
        /// 达到晨检预定时间
        /// </summary>
        /// <param name="obj"></param>
        private void Timedup_Morning(object obj)
        {
            RushList(DateTime.Now, 3);
            SetTime_Morning(true);
        }
        /// <summary>
        /// 达到午检预定时间
        /// </summary>
        /// <param name="obj"></param>
        private void Timedup_Moon(object obj)
        {
            RushList(DateTime.Now, 1);
            SetTime_Moon(true);
        }
        /// <summary>
        /// 达到晚检预定时间
        /// </summary>
        /// <param name="obj"></param>
        private void Timedup_Night(object obj)
        {
            RushList(DateTime.Now, 2);
            SetTime_Night(true);
        }
        /// <summary>
        /// 过一遍整个列表
        /// </summary>
        /// <param name="time">执行时间</param>
        /// <param name="type">填报类型(每日: 0,早: 3 ,午: 1, 晚: 2 )</param>
        private void RushList(DateTime time, int type)
        {
            Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    UserGrid.IsReadOnly = true;
                });
                foreach (var item in UserList)
                {
                    RushOne(item, time, type);
                }
                this.Dispatcher.Invoke(() =>
                {
                    UserGrid.IsReadOnly = false;
                });
            });
        }
        /// <summary>
        /// 对列表中某一项操作
        /// </summary>
        /// <param name="userInfo">用户信息</param>
        /// <param name="time">执行时间</param>
        /// <param name="type">填报类型(每日: 0,早: 3 ,午: 1, 晚: 2 )</param>
        private void RushOne(User user, DateTime time, int type)
        {
            user.Submit(time, type);
        }
        /// <summary>
        /// 设定每日填报Rush时间
        /// </summary>
        /// <param name="TodayComplete">是否当日已Rush</param>
        private void SetTime_Daily(bool TodayComplete = false)
        {
            DateTime Today = DateTime.Now;
            // 当日12点后或当日Rush过, 设定次日0点02填报
            if (Today.Hour >= 12 || TodayComplete)
            {
                DateTime Tomorrow = Today.AddDays(1);
                t_Daily.Change((int)(new DateTime(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day,
                                    0, 2, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
            //除此之外，立刻填报
            else
            {
                t_Daily.Change(0, Timeout.Infinite);
            }
/*            // 在当天填报时间范围内, 立刻填报
            else if (Today.Hour >= 6 && Today.Hour < 12)
            {
                t_Daily.Change(0, Timeout.Infinite);
            }
            // 当日6点前, 设定当日6点10填报
            else
            {
                t_Daily.Change((int)(new DateTime(Today.Year, Today.Month, Today.Day,
                    6, 10, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }*/
        }
        /// <summary>
        /// 设定晨检Rush时间
        /// </summary>
        /// <param name="TodayComplete">是否当日已Rush</param>
        private void SetTime_Morning(bool TodayComplete = false)
        {
            DateTime Today = DateTime.Now;
            // 当日12点后或当日Rush过, 设定次日0点04填报
            if (Today.Hour >= 12 || TodayComplete)
            {
                DateTime Tomorrow = Today.AddDays(1);
                t_Morning.Change((int)(new DateTime(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day,
                                    0, 4, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
            // 除此之外, 立刻填报
            else
            {
                t_Morning.Change(0, Timeout.Infinite);
            }
            /*            // 当日12点前, 设定当日14点10填报
                        else
                        {
                            t_Morning.Change((int)(new DateTime(Today.Year, Today.Month, Today.Day,
                                14, 10, 0) - Today).TotalMilliseconds, Timeout.Infinite);
                        }*/
        }
        /// <summary>
        /// 设定午检Rush时间
        /// </summary>
        /// <param name="TodayComplete">是否当日已Rush</param>
        private void SetTime_Moon(bool TodayComplete = false)
        {
            DateTime Today = DateTime.Now;
            // 当日18点后或当日Rush过, 设定次日12点02填报
            if (Today.Hour >= 18 || TodayComplete)
            {
                DateTime Tomorrow = Today.AddDays(1);
                t_Moon.Change((int)(new DateTime(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day,
                                    12, 2, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
            // 在当天填报时间范围内, 立刻填报
            else if (Today.Hour >= 12 && Today.Hour < 18)
            {
                t_Moon.Change(0, Timeout.Infinite);
            }
            // 当日12点前, 设定当日12点02填报
            else
            {
                t_Moon.Change((int)(new DateTime(Today.Year, Today.Month, Today.Day,
                    12, 2, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
        }
        /// <summary>
        /// 设定晚检Rush时间
        /// </summary>
        /// <param name="TodayComplete">是否当日已Rush</param>
        private void SetTime_Night(bool TodayComplete = false)
        {
            DateTime Today = DateTime.Now;
            // 当日23点后或当日Rush过, 设定次日18点02填报
            if (Today.Hour >= 23 || TodayComplete)
            {
                DateTime Tomorrow = Today.AddDays(1);
                t_Night.Change((int)(new DateTime(Tomorrow.Year, Tomorrow.Month, Tomorrow.Day,
                                    18, 2, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
            // 在当天填报时间范围内, 立刻填报
            else if (Today.Hour >= 18 && Today.Hour < 23)
            {
                t_Night.Change(0, Timeout.Infinite);
            }
            // 当日18点前, 设定当日19点20填报
            else
            {
                t_Night.Change((int)(new DateTime(Today.Year, Today.Month, Today.Day,
                    18, 02, 0) - Today).TotalMilliseconds, Timeout.Infinite);
            }
        }
        /// <summary>
        /// 手动进行一次列表Rush
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RushOne_Click(object sender, RoutedEventArgs e)
        {
            RushList(DateTime.Now, 0);
            int hour_now = DateTime.Now.Hour;
            if (hour_now >= 0 && hour_now < 12)
            {
                RushList(DateTime.Now, 3);
            }
            if (hour_now >= 12 && hour_now < 18)
            {
                RushList(DateTime.Now, 1);
            }
            else if (hour_now >= 18 && hour_now < 24)
            {
                RushList(DateTime.Now, 2);
            }
        }
        /// <summary>
        /// 导出列表到文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (FileStream fs = new FileStream("userlist.xml", FileMode.Create))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ObservableCollection<User>));
                xmlSerializer.Serialize(fs, UserList);
                MessageBox.Show("导出完成");
            }
        }
    }
}
