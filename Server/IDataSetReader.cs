using System.IO;
using System.Threading.Tasks;

namespace Server
{
    public interface IDataSetReader
    {
        Stream GetMasterRecords();
        Stream GetDetailsRecords();
    }
}