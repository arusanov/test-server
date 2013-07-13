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


        public DataSetReader(string masterRecordsFile, string detailsRecordsFile)
        {
            MasterRecordsFile = masterRecordsFile;
            DetailsRecordsFile = detailsRecordsFile;
        }


        public Stream GetMasterRecords()
        {
            return File.Open(MasterRecordsFile,FileMode.Open,FileAccess.Read,FileShare.Read);
        }

        public Stream GetDetailsRecords()
        {
            return File.Open(DetailsRecordsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
}