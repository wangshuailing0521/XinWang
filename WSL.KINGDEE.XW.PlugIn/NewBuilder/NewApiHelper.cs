using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn.NewBuilder
{
    public class NewApiHelper
    {
        private string _appId = "";
        private string _appSecret = "";
        public string _token = "";

        public NewApiHelper(string appId, string appSecret)
        {
            _appId = appId;
            _appSecret = appSecret;

            GetAccessToken();
        }

        public string GetAccessToken()
        {
            string url = $@"https://spzs.scjgj.sh.gov.cn/p4/api/v1/users/pub/token?appId={_appId}&appSecret={_appSecret}";

            string response = CreateGetHttpResponse(url);

            AccessTokenModel accessToken = JsonConvert.DeserializeObject<AccessTokenModel>(response);

            if (!accessToken.status.Contains("success"))
            {
                throw new Exception("获取token失败："+response);
            }

            _token = accessToken.token;

            return accessToken.token;
        }


        /// <summary>  
        /// 创建GET方式的HTTP请求  
        /// </summary>  
        public static string CreateGetHttpResponse(string url,CookieCollection cookies = null)
        {
            HttpWebRequest request = null;
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                //对服务端证书进行有效性校验（非第三方权威机构颁发的证书，如自己生成的，不进行验证，这里返回true）
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | (SecurityProtocolType)0x300 | (SecurityProtocolType)0xC00;
                //ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            request.Method = "GET";

            //设置代理UserAgent和超时
            //request.UserAgent = userAgent;
            //request.Timeout = timeout;
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())//响应对象
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string str = reader.ReadToEnd();//获取返回的页面信息
                return str;
            }
        }

        /// <summary>
        /// 验证证书
        /// </summary>
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

        public string Post(string url, string para)
        {
            HttpWebRequest request = null;
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                //对服务端证书进行有效性校验（非第三方权威机构颁发的证书，如自己生成的，不进行验证，这里返回true）
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | (SecurityProtocolType)0x300 | (SecurityProtocolType)0xC00;
                //ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = HttpWebRequest.Create(url) as HttpWebRequest;
            }
            else
            {
                request = HttpWebRequest.Create(url) as HttpWebRequest;
            }

            request.Headers["Authorization"] = "Bearer " + _token;
            request.Method = "Post";//请求方式
            //request.Headers.Add("Authorization","Bearer"+ _token);
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
