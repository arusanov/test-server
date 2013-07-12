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
        static void Main(string[] args)
        {
            var dataSetType = DataSetType.Both;
            if (args.Length > 0)
            {
                Enum.TryParse(args[0], true, out dataSetType);
            }
            Task.WaitAll(Init(dataSetType));
        }

        private static async Task Init(DataSetType dataSetType)
        {
            var client = new DataSetClient(new IPEndPoint(IPAddress.Loopback, 3333));
            var sets = await client.QueryServer(dataSetType);
            var dbService = new DataSetDbService();
            foreach (var set in sets)
            {
                dbService.SaveDataSet(set.Key,set.Value);
            }
            //yohoho and a bottle of rum!
            //Push it to the db!

        }
    }
}
