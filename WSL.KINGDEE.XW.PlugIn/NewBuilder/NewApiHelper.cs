using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn.NewBuilder
{
    public class NewApiHelper
    {
        private string _appId = "";
        private string _appSecret = "";

        public NewApiHelper(string appId, string appSecret)
        {
            _appId = appId;
            _appSecret = appSecret;
        }

        public string GetAccessToken()
        {
            string url = $@"https://spzs.scjgj.sh.gov.cn/p4/api/v1/users/pub/token?appId={_appId}&appSecret={_appSecret}";

            string response = Get(url);

            AccessTokenModel accessToken = JsonConvert.DeserializeObject<AccessTokenModel>(response);

            if (!accessToken.status.Contains("success"))
            {
                throw new Exception(response);
            }

            return accessToken.token;
        }

        public static string Get(string url)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);//创建请求对象
            request.Method = "Get";//请求方式
            request.KeepAlive = true;
            request.ContentType = "application/json";//请求头参数
            try
            {
                Stream stream = request.GetRequestStream();
                stream.Close();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())//响应对象
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string str = reader.ReadToEnd();//获取返回的页面信息
                    return str;
                }
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    if (response == null)
                    {
                        return e.ToString();
                    }
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        return text;
                    }
                }
            }

        }

        public string Post(string url, string para, string token)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);//创建请求对象
            request.Method = "Post";//请求方式
            request.Headers.Add("Authorization","Bearer"+ token);
            request.KeepAlive = true;
            request.ContentType = "application/json";//请求头参数
            byte[] bytes = Encoding.UTF8.GetBytes(para);//设置请求参数
            request.ContentLength = bytes.Length;

            try
            {
                Stream stream = request.GetRequestStream();
                stream.Write(bytes, 0, bytes.Length);//写入参数
                stream.Close();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())//响应对象
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string str = reader.ReadToEnd();//获取返回的页面信息
                    return str;
                }
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    //Console.WriteLine(e);
                    if (response == null)
                    {
                        return e.ToString();
                    }
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        // Console.WriteLine(text);
                        return text;
                    }
                }
            }

        }
    }
}
