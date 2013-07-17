using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NLog;

namespace Client.DAO
{
    public class DataSetDbService
    {
        private readonly string _connectionString;
        private readonly DbProviderFactory _dbFactory;
        private readonly Logger _logger = LogManager.GetLogger("data");

        public DataSetDbService(ConnectionStringSettings connectionString)
        {
            _dbFactory = DbProviderFactories.GetFactory(connectionString.ProviderName);
            _connectionString = connectionString.ConnectionString;
        }

        private async Task<DbConnection> GetConnection()
        {
            DbConnection conn = _dbFactory.CreateConnection();
            if (conn != null)
            {
                conn.ConnectionString = _connectionString;
                await conn.OpenAsync();

                return conn;
            }
            return null;
        }

        public async Task SaveDataSet(DataSetType dataSetType, string dataString)
        {
            if (dataSetType == DataSetType.Master)
            {
                await UpsertData(dataString, "[dbo].[spUpsertMaster]", (reader) => {
                    string action = Convert.ToString(reader["Action"]);
                    if (action == "UPDATE")
                    {
                        string id = Convert.ToString(reader["Id"]);
                        _logger.Warn("Master record with Id={0} already exists", id);
                    }                
                });
            }
            else if (dataSetType == DataSetType.Details)
            {
                await UpsertData(dataString, "[dbo].[spUpsertDetails]", (reader) =>
                {
                    string id = Convert.ToString(reader["Id"]);
                    string name = Convert.ToString(reader["Name"]);
                    string action = Convert.ToString(reader["Action"]);
                    if (action == "UPDATE")
                    {
                        _logger.Warn("Details record with Id={0} already exists '{1}'", id, name);
                    }
                    else if (action == "NONE")
                    {
                        _logger.Warn("Master record with Id={0} is missing for Details '{1}'", id, name);
                    }
                });
            }
        }

        private async Task UpsertData(string dataString, string spName, Action<DbDataReader> logProcessor)
        {
            DbCommand queryCommand = _dbFactory.CreateCommand();
            queryCommand.CommandText = spName + " @dataString";
            queryCommand.Parameters.Add(new SqlParameter {
                ParameterName = "@dataString",
                DbType = DbType.String,
                Direction = ParameterDirection.Input,
                Value = dataString,
            });

            using (queryCommand.Connection = await GetConnection())
            {
                try
                {
                    using (DbDataReader reader = await queryCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logProcessor(reader);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error reading data.", e);
                }
            }
        }
    }
}
