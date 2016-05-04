namespace GibiDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            Scan scan = new Scan("http://hqonline.com.br/");                                    
            scan.Start(20);
            //scan.Full();

        }
    }
}
