namespace WebPlatformServer.ConsoleRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Initialize and start the server
            Server server = new Server(port: 8080,webRoot:"/Users/metzliceleste/Desktop/PORTFOLIO/MyBioWebpage");
            server.Start();
        }
    }
}
