using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BUPTRushReport
{
    public class User : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public User()
        {
            userid = "学号";
            userpwd = "密码";
            Cookies = "";
            AutoMorning = false;
            AutoMoon = false;
            AutoNight = false;
            Skip = false;
            Payload1 = "";
            Payload2 = "";
            LatestResult_Daily = "";
            LatestResult_Morning = "";
            LatestResult_Moon = "";
            LatestResult_Night = "";
        }
        /// <summary>
        /// 学号
        /// </summary>
        public string userid { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string userpwd { get; set; }
        /// <summary>
        /// 成功登录Cookies
        /// </summary>
        public string Cookies { get; set; }
        /// <summary>
        /// 填报页面HTML
        /// </summary>
        private string page_Input = "";
        /// <summary>
        /// 验证网站、账号并尝试获取Cookies
        /// </summary>
        /// <param name="DayTime">执行时间</param>
        /// <param name="type">填报类型(每日: 0,早: 3 ,午: 1, 晚: 2 )</param>
        /// <returns>
        /// 登录成功: true, 并设置Cookies;
        /// 登录失败: false, 并设置LatestResult
        /// </returns>
        private bool Verify(string DayTime, int type)
        {
            bool LoginResult = false;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://app.bupt.edu.cn/uc/wap/login/check");
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0";
            // 禁止自动跳转
            webRequest.AllowAutoRedirect = false;
            try
            {
                using (Stream reqstream = webRequest.GetRequestStream())
                {
                    byte[] LoginPayload = Encoding.UTF8.GetBytes("username=" + userid + "&password=" + userpwd);
                    reqstream.Write(LoginPayload, 0, LoginPayload.Length);
                    reqstream.Flush();
                    using (WebResponse webResponse = webRequest.GetResponse())
                    {
                        string cookies = webResponse.Headers.Get("Set-Cookie");
                        using (Stream respstream = webResponse.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(respstream))
                            {
                                string ret = streamReader.ReadToEnd();
                                // Console.WriteLine(cookies);
                                var reply = JsonSerializer.Deserialize<Dictionary<string, object>>(ret);
                                if (reply["e"].ToString() != "0")
                                {
                                    Cookies = "";
                                    switch (type)
                                    {
                                        case 0:
                                            LatestResult_Daily = DayTime + reply["m"].ToString();
                                            break;
                                        case 1:
                                            LatestResult_Moon = DayTime + reply["m"].ToString();
                                            break;
                                        case 2:
                                            LatestResult_Night = DayTime + reply["m"].ToString();
                                            break;
                                        case 3:
                                            LatestResult_Morning = DayTime + reply["m"].ToString();
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                else
                                {
                                    Cookies = cookies;
                                    LoginResult = true;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                Cookies = "";
                switch (type)
                {
                    case 0:
                        LatestResult_Daily = DayTime + "网站异常";
                        break;
                    case 1:
                        LatestResult_Moon = DayTime + "网站异常";
                        break;
                    case 2:
                        LatestResult_Night = DayTime + "网站异常";
                        break;
                    case 3:
                        LatestResult_Morning = DayTime + "网站异常";
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                if (webRequest != null)
                {
                    webRequest.Abort();
                }
            }
            return LoginResult;
        }
        /// <summary>
        /// 检查当日某时段是否已填报
        /// </summary>
        /// <param name="type">填报类型(每日: 0,早: 3 ,午: 1, 晚: 2 )</param>
        /// <returns>
        /// 已填报: true;
        /// 未填报: false
        /// </returns>
        private bool CheckSubmitted(int type)
        {
            bool flag = false;
            string checkURL = "";
            // 每日填报
            if (type == 0)
            {
                checkURL = "https://app.bupt.edu.cn/ncov/wap/default/index";
            }
            // 晨午晚检
            else
            {
                checkURL = "https://app.bupt.edu.cn/xisuncov/wap/open-report/index";
            }
            WebRequest webRequest = WebRequest.Create(checkURL);
            webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
            webRequest.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0";
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream stream = webResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        page_Input = reader.ReadToEnd();
                        // 每日填报
                        if (type == 0)
                        {
                            if (page_Input.IndexOf("hasFlag: '1'") != -1)
                            {
                                flag = true;
                            }
                        }
                        // 晨午晚检
                        else
                        {
                            if (page_Input.IndexOf("\"realonly\":true") != -1)
                            {
                                flag = true;
                            }
                        }
                    }
                }
            }
            if (webRequest != null)
            {
                webRequest.Abort();
            }
            return flag;
        }
        /// <summary>
        /// 提交填报信息
        /// </summary>
        /// <param name="time">执行时间</param>
        /// <param name="type">填报类型(每日: 0,早: 3 ,午: 1, 晚: 2 )</param>
        public void Submit(DateTime time, int type)
        {
            string DayTime = time.Year.ToString() + "." +
                                time.Month.ToString() + "." +
                                time.Day.ToString() + " ";
            if (!Skip && 
                ((type == 1 && AutoMoon) || (type == 2 && AutoNight) || (type == 3 && AutoMorning) || type == 0) && 
                Verify(DayTime, type))
            {
                if (CheckSubmitted(type) == false)
                {
                    string submitURL = "";
                    byte[] Payload = { };
                    // 每日填报
                    if (type == 0)
                    {
                        submitURL = "https://app.bupt.edu.cn/ncov/wap/default/save";
                        Payload = Encoding.UTF8.GetBytes(Payload1);
                    }
                    // 晨午晚检
                    else
                    {
                        submitURL = "https://app.bupt.edu.cn/xisuncov/wap/open-report/save";
                        Payload = Encoding.UTF8.GetBytes(Payload2);
                    }
                    WebRequest webRequest = WebRequest.Create(submitURL);
                    webRequest.Method = "POST";
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Headers.Set(HttpRequestHeader.Cookie, Cookies);
                    webRequest.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:81.0) Gecko/20100101 Firefox/81.0";
                    using (Stream reqstream = webRequest.GetRequestStream())
                    {
                        reqstream.Write(Payload, 0, Payload.Length);
                        reqstream.Flush();
                        WebResponse webResponse = webRequest.GetResponse();
                        webResponse.Dispose();
                        webResponse.Close();
                    }
                    if (webRequest != null)
                    {
                        webRequest.Abort();
                    }
                    string twiceChk = "";
                    if (CheckSubmitted(type))
                    {
                        twiceChk = "自动填报成功";
                    }
                    else
                    {
                        twiceChk = "!!!自动填报失败!!!";
                    }
                    switch (type)
                    {
                        case 0:
                            LatestResult_Daily = DayTime + twiceChk;
                            break;
                        case 1:
                            LatestResult_Moon = DayTime + twiceChk;
                            break;
                        case 2:
                            LatestResult_Night = DayTime + twiceChk;
                            break;
                        case 3:
                            LatestResult_Morning = DayTime + twiceChk;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (type)
                    {
                        case 0:
                            LatestResult_Daily = DayTime + "已填报, 无需重复提交";
                            break;
                        case 1:
                            LatestResult_Moon = DayTime + "已填报, 无需重复提交";
                            break;
                        case 2:
                            LatestResult_Night = DayTime + "已填报, 无需重复提交";
                            break;
                        case 3:
                            LatestResult_Morning = DayTime + "已填报, 无需重复提交";
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// 是否自动晨检
        /// </summary>
        public bool AutoMorning { get; set; }
        /// <summary>
        /// 是否自动午检
        /// </summary>
        public bool AutoMoon { get; set; }
        /// <summary>
        /// 是否自动晚检
        /// </summary>
        public bool AutoNight { get; set; }
        /// <summary>
        /// 是否跳过本条
        /// </summary>
        public bool Skip { get; set; }
        /// <summary>
        /// 每日填报Payload
        /// </summary>
        public string Payload1 { get; set; }
        /// <summary>
        /// 晨午晚检Payload
        /// </summary>
        public string Payload2 { get; set; }
        /// <summary>
        /// 每日填报执行结果
        /// </summary>
        private string _LatestResult_Daily;
        public string LatestResult_Daily
        {
            get
            {
                return _LatestResult_Daily;
            }
            set
            {
                _LatestResult_Daily = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LatestResult_Daily"));
                }
            }
        }
        /// <summary>
        /// 晨检执行结果
        /// </summary>
        private string _LatestResult_Morning;
        public string LatestResult_Morning
        {
            get
            {
                return _LatestResult_Morning;
            }
            set
            {
                _LatestResult_Morning = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LatestResult_Morning"));
                }
            }
        }
        /// <summary>
        /// 午检执行结果
        /// </summary>
        private string _LatestResult_Moon;
        public string LatestResult_Moon
        {
            get
            {
                return _LatestResult_Moon;
            }
            set
            {
                _LatestResult_Moon = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LatestResult_Moon"));
                }
            }
        }
        /// <summary>
        /// 晚检执行结果
        /// </summary>
        private string _LatestResult_Night;
        public string LatestResult_Night
        {
            get
            {
                return _LatestResult_Night;
            }
            set
            {
                _LatestResult_Night = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LatestResult_Night"));
                }
            }
        }
    }
}