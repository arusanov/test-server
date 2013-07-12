using System.Data.Entity;
using Client.DAO.Models;

namespace Client.DAO
{
    public class DataSets:DbContext
    {
         public DbSet<MasterRecord> MasterRecords { get; set; }
         public DbSet<DetailsRecord> DetailsRecords { get; set; }
    }
}