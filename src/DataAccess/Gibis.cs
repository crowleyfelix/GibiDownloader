using GibiDownloader.Models;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace GibiDownloader.DataAccess
{

    public class Gibis : List<Gibi>
    {

        public static string dirGibis = "Gibis/";
        public static string dirConfig = dirGibis + "_config/";
        public static string xmlGibis = dirConfig + "gibis.xml";

        public Gibis()
        {
            Clear();
            Ler();
        }

        private void Ler()
        {
            try
            {
                var xml = XElement.Load(xmlGibis);

                foreach (XElement xeG in xml.Elements())
                {
                    Gibi g = new Gibi()
                    {

                        Titulo = xeG.Attribute("titulo").Value.ToString(),
                        Edicao = xeG.Attribute("edicao").Value.ToString(),
                        Editora = xeG.Attribute("editora").Value.ToString(),
                        IdDownload = xeG.Attribute("idDownload").Value.ToString()
                    };

                    Add(g);
                }
            }
            catch { }

        }

        public void Salvar()
        {

            var xml = new XElement("gibis");

            foreach (Gibi g in this)
            {
                var xeG = new XElement("gibi");

                xeG.Add(new XAttribute("titulo", g.Titulo));
                xeG.Add(new XAttribute("edicao", g.Edicao));
                xeG.Add(new XAttribute("editora", g.Editora));
                xeG.Add(new XAttribute("idDownload", g.IdDownload));

                xml.Add(xeG);
            }

            using (StreamWriter sw = new StreamWriter(xmlGibis))
            {

                try
                {
                    sw.Write(xml.ToString());
                    sw.Flush();
                }
                catch { }

            }

        }
    }
}
