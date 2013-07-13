using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Server
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetLogger("programm");

        static void Main(string[] args)
        {
            Task.WaitAll(Init());
        }

        private static async Task Init()
        {
            var dataSetReader = new DataSetReader(@".\DataSets\master.txt", @".\DataSets\details.txt");
            var socketServer = new SocketServer(new IPEndPoint(IPAddress.Any, 3333),dataSetReader);
            await socketServer.Listen();
        }
    }
}
