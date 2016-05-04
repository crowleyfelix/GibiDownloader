using GibiDownloader.DataAccess;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace GibiDownloader.Models
{
    public class Gibi : IDisposable
    {
        private string titulo;
        public string Titulo
        {
            get
            {
                return titulo;
            }
            set
            {
                if (Regex.Match(value.ToLower(), "saga.+").Success)
                    titulo = value.Replace("Saga", "Saga -");
                else
                    titulo = value;

            }
        }

        private string edicao { get; set; }

        public string Edicao
        {
            get { return edicao; }
            set
            {
                edicao = Regex.Replace(value, "\\s*\\[...nline.com.br\\]", "");
                edicao = titulo.Replace(".PDF", "").Replace("&amp;", "e").Trim();

                if (!edicao.Contains(".pdf"))
                    edicao = titulo + ".pdf";
            }
        }


        public string Editora { get; set; }
                
        public string Url
        {

            get
            {
                return "https://docs.google.com/uc?id=" + idDownload + "&export=download";
            }
        }

        private string idDownload;
        public string IdDownload
        {
            get { return idDownload; }

            set
            {
                idDownload = value;
            }
        }

        public bool Download()
        {
            if (!string.IsNullOrEmpty(Edicao))
            {


                string downloadPath = Gibis.dirGibis + "/" + Editora + "/" + Titulo + "/";

                if (Directory.Exists(downloadPath) == false)
                    Directory.CreateDirectory(downloadPath);

                try
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Baixando: " + Edicao);

                        do
                        {
                            //Verificando se arquivo já existe.
                            if (!Scan.ArquivoExiste(this))
                                new WebClient().DownloadFile(Url, downloadPath + Edicao);


                        } while (!Util.IsConnected());


                }
                catch { return false; }
            }


            return true;
        }

        public void VerificarSaga(string comicPage)
        {
            if (Regex.Match(Titulo, "saga.+?").Success)
            {
                MatchCollection matches = Regex.Matches(comicPage, "(?:(?:href)|(?:rel))=\"(?:http://hqonline.com.br/leitor_online.php\\?hq=)?https://docs.google.com/(?:(?:file/\\w*/)|(?:open\\?id=))((?:[\\w|\\d|-])*)(?:/\\w*)?\"(?:\\s*target=\"_blank\")?><img title=");

                for (int i = 0; i < matches.Count; i++)
                {
                    if (matches[i].Groups[1].ToString().Contains(idDownload))
                    {

                        Edicao = (i + 1) + "- " + Edicao;

                    }

                }
            }


        }

        #region Dispose
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        #endregion

    }
}
