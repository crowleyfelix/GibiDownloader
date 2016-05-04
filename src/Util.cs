using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using GibiDownloader.DataAccess;

namespace GibiDownloader
{
    public static class Util
    {

        public static string xmlInuteis = Gibis.dirConfig + "urlsinuteis.xml";
        
        public static List<string> UrlInutil(string url = null)
        {

            var lista = new List<string>();


            XElement xml = null;
            try { xml = XElement.Load(xmlInuteis); }
            catch { }

            if (xml != null)
            {

                foreach (XElement xe in xml.Elements())
                {
                    lista.Add(xe.Attribute("url").Value.ToString());
                }

            }

            if (url != null)
            {
                if (xml == null)
                { xml = new XElement("urlsInuteis"); }

                var xe = new XElement("urlInutil");
                xe.Add(new XAttribute("url", url));

                xml.Add(xe);
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(xmlInuteis))
                {

                    try
                    {
                        sw.Write(xml.ToString());
                        sw.Flush();
                    }
                    catch { }

                }
            }
            catch { }

            return lista;

        }
        
        public static void VerificarDiretorios()
        {
            if (Directory.Exists(Gibis.dirGibis) == false)
            {
                Directory.CreateDirectory(Gibis.dirGibis);
            }
            if (Directory.Exists(Gibis.dirConfig) == false)
            {
                Directory.CreateDirectory(Gibis.dirConfig);
                new DirectoryInfo(Gibis.dirConfig).Attributes = FileAttributes.Hidden;

            }


            string[] arquivos = { Gibis.xmlGibis, xmlInuteis };
            CriarArquivos(arquivos);
        }

        private static void CriarArquivos(string[] arquivos)
        {

            foreach (string arquivo in arquivos)
            {
                if (File.Exists(arquivo) != true)
                {
                    File.Create(arquivo).Close();
                }
            }
        }
        
        public static string RequestWeb(string url, string charset = "ISO-8859-1", CookieContainer cookieContainer = null, int maxAttempts = 3, string method = "GET", string postData = "")
        {
            string content = "";
            int attempts = 0;

            do
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                    request.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.10; rv:34.0) Gecko/20100101 Firefox/34.0";
                    request.Accept = "text/html,application/xhtml+xml,application/xml,application/json,text/javascript;q=0.9,*/*;q=0.01";
                    request.KeepAlive = true;

                    request.Headers.Add("Accept-Language", "pt-br;pt;q=0.8,en-us;q=0.5,en;q=0.3");
                    request.Headers.Add("Accept-Encoding", "gzip,deflate");
                    request.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
                    request.Headers.Add("Cache-Control", "no-cache");
                    request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    request.Headers.Add("DNT", "1");
                    request.Headers.Add("Pragma", "no-cache");

                    request.Method = method;
                    request.Referer = url;
                    request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

                    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    request.AllowAutoRedirect = true;
                    //request.IfModifiedSince = DateTime.Today;

                    if (method == "POST")
                    {
                        byte[] postBytes = Encoding.UTF8.GetBytes(postData);

                        request.ContentType = "application/x-www-form-urlencoded";
                        request.ContentLength = postBytes.Length;

                        Stream postStream = request.GetRequestStream();
                        postStream.Write(postBytes, 0, postBytes.Length);
                        postStream.Flush();
                        postStream.Close();
                    }

                    request.CookieContainer = cookieContainer ?? new CookieContainer();

                    while (attempts <= maxAttempts)
                    {
                        attempts++;

                        try
                        {
                            using (var response = (HttpWebResponse)request.GetResponse())
                            {
                                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(charset)))
                                {
                                    content = reader.ReadToEnd();
                                }
                            }

                            content = WebUtility.HtmlDecode(content);
                            return WebUtility.HtmlDecode(content);
                        }
                        catch (Exception expt)
                        {
                            if (attempts >= maxAttempts)
                            {
                                throw expt;
                            }
                        }
                    }
                }
                catch (Exception expt)
                {
                    Trace.WriteLine("Não foi possível carregar página " + url + ". Erro: " + expt.Message);

                    System.Threading.Thread.Sleep(10000);
                }



            } while (!IsConnected());

            content = WebUtility.HtmlDecode(content);
            return content;
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        // Um método que verifica se esta conectado
        public static bool IsConnected()
        {
            int Description;
            return InternetGetConnectedState(out Description, 0);
        }
        
    }
}
