using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class DataSetReader : IDataSetReader
    {
        protected string MasterRecordsFile { get; set; }
        protected string DetailsRecordsFile { get; set; }
        private byte[] MasterRecordsData { get; set; }
        private byte[] DetailsRecordsData { get; set; }

        public DataSetReader(string masterRecordsFile, string detailsRecordsFile)
        {
            MasterRecordsFile = masterRecordsFile;
            DetailsRecordsFile = detailsRecordsFile;
        }

        public async Task Init()
        {
            MasterRecordsData = await ReadDataFromFile(MasterRecordsFile);
            DetailsRecordsData = await ReadDataFromFile(DetailsRecordsFile);
        }

        private async Task<byte[]> ReadDataFromFile(string file)
        {
            using (var reader = new StreamReader(file))
            {
                return Encoding.UTF8.GetBytes(await reader.ReadToEndAsync());
            }
        }

        public byte[] GetMasterRecords()
        {
            return MasterRecordsData;
        }

        public byte[] GetDetailsRecords()
        {
            return DetailsRecordsData;
        }
    }
}