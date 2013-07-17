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

        private static int GetUniqueId(int range, HashSet<int> existingIds, Random rnd)
        {
            int id;
            do
            {
                id = rnd.Next(range);
            }
            while (!existingIds.Add(id));
            return id;
        }

        private static async Task WriteData(string dir, int level, int detailsLevel)
        {
            int masterId;
            var idsHash = new HashSet<int>();
            var rnd = new Random();
            using (StreamWriter fileMaster = File.CreateText(Path.Combine(dir, "master.txt")))
            {
                for (int i = 0; i < level; i++)
                {
                    masterId = GetUniqueId(level, idsHash, rnd);
                    await fileMaster.WriteLineAsync(string.Format("{0},\"{1}\"", masterId, Guid.NewGuid()));
                }
                Console.WriteLine("Master file created");
            }
            using (StreamWriter fileDetails = File.CreateText(Path.Combine(dir, "details.txt")))
            {
                idsHash.Clear();
                for (int i = 0; i < level; i++)
                {
                    masterId = GetUniqueId(level, idsHash, rnd);

                    int detailsCurrentLevel = rnd.Next(detailsLevel/3, detailsLevel);
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