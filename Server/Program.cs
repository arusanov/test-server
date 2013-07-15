using System.Net;
using System.Threading.Tasks;
using NLog;

namespace Server
{
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetLogger("programm");

        private static void Main(string[] args)
        {
            Task.WaitAll(Init());
        }

        private static async Task Init()
        {
            var dataSetReader = new DataSetReader(@".\DataSets\master.txt", @".\DataSets\details.txt");
            var socketServer = new SocketServer(new IPEndPoint(IPAddress.Any, 3333), dataSetReader);
            await socketServer.Listen();
        }
    }
}