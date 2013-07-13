using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Client.DAO;

namespace Client
{
    class Program
    {
        private static DataSetDbService _dbService;

        static void Main(string[] args)
        {
            var dataSetType = DataSetType.Both;
            if (args.Length > 0)
            {
                Enum.TryParse(args[0], true, out dataSetType);
            }
            _dbService = new DataSetDbService();
            Task.WaitAll(Init(dataSetType));
        }

        private static async Task Init(DataSetType dataSetType)
        {
            var client = new DataSetClient(new IPEndPoint(IPAddress.Loopback, 3333));

            client.OnDataSetLinesRecieved += client_OnDataSetLinesRecieved;
            var sets = await client.QueryServer(dataSetType);
        }

        static void client_OnDataSetLinesRecieved(DataSetType dataSetType, string lines)
        {
            _dbService.SaveDataSet(dataSetType, lines);
        }
    }
}
