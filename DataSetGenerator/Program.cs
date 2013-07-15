using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataSetGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            int level = 100;
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (args.Length > 0)
            {
                dir = args[0];
            }
            if (args.Length > 1)
            {
                int.TryParse(args[1], out level);
            }

            int detailslevel = 10;
            if (args.Length > 2)
            {
                int.TryParse(args[2], out detailslevel);
            }
            Console.WriteLine("Creating set with {0} master records and arpoximately {1} details", level,
                level*detailslevel);
            WriteData(dir, level, detailslevel).Wait();
        }

        private static async Task WriteData(string dir, int level, int detailslevel)
        {
            var idsHash = new HashSet<int>();
            var rnd = new Random();
            using (StreamWriter fileMaster = File.CreateText(Path.Combine(dir, "master.txt")))
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
                Console.WriteLine("Master file created");
            }
            using (StreamWriter fileDetails = File.CreateText(Path.Combine(dir, "details.txt")))
            {
                List<int> materIds = idsHash.ToList();
                for (int i = 0; i < level; i++)
                {
                    //get any master id
                    int masterId = materIds[rnd.Next(materIds.Count)];
                    int detailsCurrentLevel = rnd.Next(detailslevel/3, detailslevel);
                    for (int j = 0; j < detailsCurrentLevel; j++)
                    {
                        string detailRecord = string.Format("{0},\"{1}-Details {2}\"", masterId, j, Guid.NewGuid());
                        await fileDetails.WriteLineAsync(detailRecord);
                    }
                }
                Console.WriteLine("Details file created");
            }
        }
    }
}