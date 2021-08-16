
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class ApiHelper
    {
        public static string Post(string url,string para)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);//创建请求对象
            request.Method = "Post";//请求方式
            request.KeepAlive = true;
            request.ContentType = "application/json";//请求头参数
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(para);//设置请求参数
            request.ContentLength = bytes.Length;
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

        public static string HttpPostFrom(string url, string data)
        {
            string message = "";
            try
            {
                string SendMessageAddress = url;//请求链接
                HttpWebRequest request = null;

                //如果是发送HTTPS请求 
                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    //关于ServicePointManager.SecurityProtocol的设置是解决问题的关键。
                    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Ssl3;
                    //.Net4.0
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)48 | (SecurityProtocolType)0 | (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072 | (SecurityProtocolType)12288;
                    //.Net4.5
                    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls1.2;
                    request = WebRequest.Create(SendMessageAddress) as HttpWebRequest;

                    request.KeepAlive = false;
                    request.ProtocolVersion = HttpVersion.Version10;

                }
                else
                {
                    request = WebRequest.Create(SendMessageAddress) as HttpWebRequest;
                }
                
               
                request.Method = "POST";
                request.AllowAutoRedirect = true;
                request.Timeout = 20 * 1000;
                request.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
                
                string PostData = data;//请求参数
                Encoding code = Encoding.GetEncoding("utf-8");
                byte[] byteArray = code.GetBytes(PostData);
                request.ContentLength = byteArray.Length;
                using (Stream newStream = request.GetRequestStream())
                {
                    newStream.Write(byteArray, 0, byteArray.Length);//写入参数
                    newStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream rspStream = response.GetResponseStream();
                using (StreamReader reader = new StreamReader(rspStream, Encoding.UTF8))
                {
                    message = reader.ReadToEnd();
                    rspStream.Close();
                }
                response.Close();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return message;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受 
        }

        /// <summary>
        /// 组装普通文本请求参数。
        /// </summary>
        /// <param name="parameters">Key-Value形式请求参数字典</param>
        /// <returns>URL编码后的请求数据</returns>
        public static string BuildQuery(IDictionary<string, string> parameters)
        {
            StringBuilder postData = new StringBuilder();
            bool hasParam = false;

            IEnumerator<KeyValuePair<string, string>> dem = parameters.GetEnumerator();
            while (dem.MoveNext())
            {
                string name = dem.Current.Key;
                string value = dem.Current.Value;
                // 忽略参数名或参数值为空的参数
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    if (hasParam)
                    {
                        postData.Append("&");
                    }

                    postData.Append(name);
                    postData.Append("=");
                    postData.Append(value);
                    hasParam = true;
                }
            }

            return postData.ToString();
        }
    }
}
