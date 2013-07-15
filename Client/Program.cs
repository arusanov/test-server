using System;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;
using Client.DAO;
using NLog;

namespace Client
{
    internal class Program
    {
        private static DataSetDbService _dbService;
        private static readonly Logger Logger = LogManager.GetLogger("client");


        private static void Main(string[] args)
        {
            var dataSetType = DataSetType.Both;
            if (args.Length > 0)
            {
                Enum.TryParse(args[0], true, out dataSetType);
            }
            try
            {
                Init(dataSetType).Wait();
            }
            catch (AggregateException e)
            {
                Logger.ErrorException("Fatal error aggregate", e);
                foreach (Exception innerException in e.InnerExceptions)
                {
                    Logger.ErrorException("Fatal error:", innerException);
                }
            }
            catch (Exception e)
            {
                Logger.ErrorException("Fatal error", e);
            }
            Logger.Debug("Client work completed");
            Console.ReadLine();
        }

        private static async Task Init(DataSetType dataSetType)
        {
            _dbService = new DataSetDbService(ConfigurationManager.ConnectionStrings["DataSets"]);
            var client = new DataSetClient(new IPEndPoint(IPAddress.Loopback, 3333));

            client.OnDataSetLinesRecieved += client_OnDataSetLinesRecieved;
            await client.QueryServer(dataSetType);
        }

        private static void client_OnDataSetLinesRecieved(DataSetType dataSetType, string lines)
        {
            _dbService.SaveDataSet(dataSetType, lines).Wait();
        }
    }
}