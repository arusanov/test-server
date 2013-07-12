using System.Threading.Tasks;

namespace Server
{
    public interface IDataSetReader
    {
        Task Init();
        byte[] GetMasterRecords();
        byte[] GetDetailsRecords();
    }
}