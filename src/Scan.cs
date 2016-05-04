using GibiDownloader.DataAccess;
using GibiDownloader.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace GibiDownloader
{

    public class Scan
    {
        Gibis gibis = new Gibis();
        List<Gibi> listaDownload = new List<Gibi>();

        private string homePage;
        private string urlHome;
        CookieContainer ckC = new CookieContainer();

        public Scan(string url)
        {
            urlHome = url;
            homePage = Util.RequestWeb(urlHome, "UTF-8", ckC) + "?paged=";
            gibis = new Gibis();
        }

        public void Start(int ultimaPagina)
        {

            Util.VerificarDiretorios();

            Console.Title = "Download de Gibis";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("#Buscando títulos...");
            Console.ForegroundColor = ConsoleColor.White;

            // Tirar gibi da lista se arquivo não for encontrado
            gibis.FindAll(g => !ArquivoExiste(g)).ForEach(g => gibis.Remove(g));
            
            for (int i = 1; i <= ultimaPagina; i++)
            {
                string paginada = Util.RequestWeb(urlHome + "?paged=" + i, "UTF-8", ckC);
                int gibisNovos = 0;

                if (i == Math.Round((decimal)(ultimaPagina / 2), 0))
                    Console.Write("\nJá está acabando.");

                if (i == Math.Round((decimal)ultimaPagina / 2 + (decimal)(ultimaPagina / 4), 0))
                    Console.WriteLine(".. sério.");


                foreach (Match match in Regex.Matches(paginada, "(?:(?:href)|(?:rel))=\"(?:http://hqonline.com.br/leitor_online.php\\?hq=)?https://docs.google.com/(?:(?:file/\\w*/)|(?:open\\?id=))((?:[\\w|\\d|-])*)(?:/\\w*)?\"(?:\\s*target=\"_blank\")?><img (?:class=\"alignnone\"\\s*)?title=\"(.*?)\".*?></a></p>\\s*(?:<p.*(?:</p>)?\\s*)*?</div>\\s*<div class=\"postinfo\">(?:.*?\\s*)*?rel=\"category\">(.*?)</a>"))
                {

                    string id = match.Groups[1].ToString();


                    if (!gibis.Exists(g => g.Url.Contains(id)))
                    {

                        gibisNovos++;

                        using (Gibi gibiNovo = new Gibi())
                        {
                            gibiNovo.IdDownload = id;
                            gibiNovo.Titulo = Regex.Replace(match.Groups[3].ToString(), "\\s*[Vv]\\d+\\s*", "");
                            gibiNovo.Edicao = match.Groups[2].ToString();

                            listaDownload.Add(gibiNovo);
                        }

                    }

                }

                if (i == ultimaPagina)
                {

                    Console.Clear();

                    if (gibisNovos > 0)
                    {

                        listaDownload = null;
                        AllPages();
                        break;
                    }
                    else
                    {
                        SomePages();
                        break;

                    }
                }
            }

        }

        private void SomePages()
        {

            for (int i = 0; i < listaDownload.Count; i++)
            {

                Gibi gibi = new Gibi();
                gibi = listaDownload[i];

                MatchCollection urlComicMatches = Regex.Matches(homePage, "<li class=\"page_item page-item-[0-9]+ page_item_has_children\"><a href=\"http://hqonline.com.br/\\?page_id=[0-9]+\">(.*?)</a>\\s*<ul class='children'>\\s*(?:<li class=\"page_item.*?</li>\\s*)*?<li class=\".*?\"><a href=\"(.*?)\">.*?" + gibi.Titulo + ".*?</a></li>\\s*(?:<li class=\"page_item.*?</li>\\s*)*?</ul>", RegexOptions.IgnoreCase);

                foreach (Match urlComicMatch in urlComicMatches)
                {
                    try
                    {
                        string publisher = urlComicMatch.Groups[1].ToString().Replace("&amp;", "e").Trim();
                        string comicPage = Util.RequestWeb(urlComicMatch.Groups[2].ToString(), "UTF-8", ckC);
                        if (comicPage.Contains(gibi.IdDownload))
                        {

                            Match comicMatch = Regex.Match(comicPage, "<meta property=\"og:title\" content=\"(.*?)\"\\s*/>");
                            string comic = Regex.Replace(comicMatch.Groups[1].ToString(), "\\s*[/:]\\s*", " ").Replace("&amp;", "e").Trim();

                            gibi.Editora = publisher;
                            gibi.Titulo = comic;

                            gibi.VerificarSaga(comicPage);

                            if (gibi.Download())
                            {
                                gibis.Add(gibi);

                                gibis.Salvar();
                            }


                        }
                    }
                    catch
                    {

                    }

                }


                gibi.Dispose();

            }

        }
        public void AllPages()
        {

            MatchCollection urlParentMatches = Regex.Matches(homePage, "<li class=\"page_item page-item-[0-9]+ page_item_has_children\"><a href=\"(.*?)\">(.*?)</a></li>");

            HtmlDocument html = new HtmlWeb().Load(urlHome);

            //Selecionando nós de Editoras
            var nodeEditoras = html.DocumentNode.SelectNodes("//ul[@class='children']");

            try
            {
                foreach (var nodeEditora in nodeEditoras)
                {
                    //Valor do list item pai
                    string editora = nodeEditora.ParentNode.FirstChild.InnerText;

                    if (editora != "Animações")
                    {
                        if (Directory.Exists(Gibis.dirGibis + editora) == false)
                            Directory.CreateDirectory(Gibis.dirGibis + editora);

                        List<string> urlsInuteis = Util.UrlInutil();

                        //Selecionando nós de Títulos
                        var nodeComics = nodeEditora.SelectNodes(".//li");

                        Console.WriteLine("\n\n-------------||" + editora + "||-------------");

                        foreach (var nodeComic in nodeComics)
                        {
                            //Valor do texto do link
                            string comic = nodeComic.FirstChild.InnerText;

                            string urlComic = nodeComic.FirstChild.GetAttributeValue("href", string.Empty);

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\n\n-------------||" + comic + "||-------------");
                            Console.ForegroundColor = ConsoleColor.White;

                            html = new HtmlWeb().Load(urlComic);

                            var nodeEditions = html.DocumentNode.SelectNodes("//a[contains(@href,'docs.google.com/file')]");

                            #region Comic
                            if (!urlsInuteis.Contains(urlComic))
                            {
                                try
                                {

                                    //Fazendo download das edições
                                    if (nodeEditions.Count > 0)
                                    {


                                        #region Editions
                                        foreach (var nodeEdition in nodeEditions)
                                        {

                                            Gibi gibi = new Gibi();

                                            gibi.Editora = editora;
                                            gibi.Titulo = comic;
                                            gibi.IdDownload = Regex.Match(nodeEdition.GetAttributeValue("href", string.Empty), "https://docs.google.com/(?:(?:file/\\w*/)|(?:open\\?id=))((?:[\\w|\\d|-])*)(?:/\\w*)?").Value;

                                            if (!gibis.Exists(g => g.Url == gibi.Url && g.Titulo == gibi.Titulo))
                                            {
                                                string fileName = nodeEdition.FirstChild.GetAttributeValue("title", string.Empty);

                                                gibi.VerificarSaga(nodeComic.InnerHtml);

                                                if (gibi.Download())
                                                {
                                                    gibis.Add(gibi);

                                                    gibis.Salvar();
                                                }

                                            }

                                            gibi.Dispose();

                                        }

                                        #endregion

                                    }
                                }
                                catch { }

                            }
                            #endregion
                        }

                    }

                }
            }
            catch
            {
                Console.Write(html.DocumentNode.SelectSingleNode("//body").InnerText);
                Console.Read();
            }
        }

        public static bool ArquivoExiste(Gibi gibi)
        {

            return File.Exists(Gibis.dirGibis + "/" + gibi.Editora + "/" + gibi.Titulo + "/" + gibi.Edicao);

        }
    }
}