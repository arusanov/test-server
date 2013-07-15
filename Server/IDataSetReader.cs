using System.IO;

namespace Server
{
    public interface IDataSetReader
    {
        Stream GetMasterRecords();
        Stream GetDetailsRecords();
    }
}