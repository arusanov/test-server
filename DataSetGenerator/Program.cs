using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataSetGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            int level = 100;
            if (args.Length > 0)
            {
                int.TryParse(args[0], out level);
            }

            int detailslevel = 10;
            if (args.Length > 1)
            {
                int.TryParse(args[1], out detailslevel);
            }
            WriteData(level, detailslevel).Wait();
        }

        private static async Task WriteData(int level, int detailslevel)
        {
            var idsHash = new HashSet<int>();
            var rnd = new Random();
            using (var fileMaster = File.CreateText("master.txt"))
            {
                for (int i = 0; i < level; i++)
                {
                    //Create and write master record
                    int masterId;
                    do
                    {
                        masterId = rnd.Next(level);
                    } while (!idsHash.Add(masterId));
                    await fileMaster.WriteLineAsync(string.Format("{0},\"{1}\"", masterId, Guid.NewGuid()));
                }

            }
            using (var fileDetails = File.CreateText("details.txt"))
            {
                var materIds = idsHash.ToList();
                for (int i = 0; i < level; i++)
                {
                    //get any master id
                    var masterId = materIds[rnd.Next(materIds.Count)];
                    var detailsCurrentLevel = rnd.Next(detailslevel/3, detailslevel);
                    for (int j = 0; j < detailsCurrentLevel; j++)
                    {
                        var detailRecord = string.Format("{0},\"{1}-Details {2}\"", masterId, j, Guid.NewGuid());
                        await fileDetails.WriteLineAsync(detailRecord);
                    }
                }
            }
        }
    }
}
