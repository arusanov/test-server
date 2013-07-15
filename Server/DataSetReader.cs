using System.IO;

namespace Server
{
    public class DataSetReader : IDataSetReader
    {
        public DataSetReader(string masterRecordsFile, string detailsRecordsFile)
        {
            MasterRecordsFile = masterRecordsFile;
            DetailsRecordsFile = detailsRecordsFile;
        }

        protected string MasterRecordsFile { get; set; }
        protected string DetailsRecordsFile { get; set; }


        public Stream GetMasterRecords()
        {
            return File.Open(MasterRecordsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream GetDetailsRecords()
        {
            return File.Open(DetailsRecordsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
}