using System;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Security.Cryptography;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace InGameDanmaku
{
    class Translate
    {
        private readonly Dictionary<string, string> _cachedTranslations = new Dictionary<string, string>();

        public string Main(string text)
        {
            string translatedText = "";
            if (_cachedTranslations.TryGetValue(text, out translatedText))
            {
                return translatedText;
            }

            string privateKey = "";
            string from = "auto";
            string to = "en";
            string appid = "";
            string salt = "";
            string sign = Md5(appid + text + salt + privateKey);
            string uri = "http://api.fanyi.baidu.com/api/trans/vip/translate?q=" + HttpUtility.UrlEncode(text) + "&from=" + @from + "&to=" + to + "&appid=" + appid + "&salt=" + salt + "&sign=" + sign;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            WebResponse response = null;
            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    string responseFromServer = reader.ReadToEnd();
                    reader.Close();
                    stream.Close();

                    JObject json = JObject.Parse(responseFromServer);
                    try
                    {
                        JArray results = json.GetValue("trans_result").ToObject<JArray>();
                        translatedText = results[0].ToObject<JObject>().GetValue("dst").ToString();
                        _cachedTranslations.Add(text, translatedText);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(json.GetValue("error_msg").ToString());
                    }
                }
            }

            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }

            return translatedText;
        }

        private static string Md5(string input)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            md5.Clear();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(Convert.ToString(hash[i], 16).PadLeft(2, '0'));
            }

            return sb.ToString();
        }
    }
}
