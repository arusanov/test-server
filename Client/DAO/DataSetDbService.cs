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
                await SaveMasterDataSet(dataString);
            }
            else if (dataSetType == DataSetType.Details)
            {
                await SaveDetailsDataSet(dataString);
            }
        }


        private async Task SaveDetailsDataSet(string detailsRecords)
        {
            DbCommand queryCommand = _dbFactory.CreateCommand();
            queryCommand.Parameters.Add(new SqlParameter
            {
                ParameterName = "@detailsString",
                DbType = DbType.String,
                Direction = ParameterDirection.Input,
                Value = detailsRecords,
            });
            queryCommand.CommandText = "[dbo].[spUpsertDetails] @detailsString";
            await DoPushDetails(queryCommand);
        }

        private async Task DoPushDetails(DbCommand queryCommand)
        {
            using (DbConnection connection = await GetConnection())
            {
                queryCommand.Connection = connection;
                try
                {
                    using (DbDataReader reader = await queryCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string id = Convert.ToString(reader["Id"]);
                            string name = Convert.ToString(reader["Name"]);
                            string action = Convert.ToString(reader["Action"]);
                            if (action == "UPDATE")
                            {
                                _logger.Warn("details entry '{1}' with MasterId={0} already exists",
                                    id, name);
                            }
                            else if (action == "NONE")
                            {
                                _logger.Warn("No master record found for details record '{1}' with MasterId={0}",
                                    id, name);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error reading data.", e);
                }
            }
        }

        private async Task SaveMasterDataSet(string masterRecords)
        {
            DbCommand queryCommand = _dbFactory.CreateCommand();
            queryCommand.Parameters.Add(new SqlParameter
            {
                ParameterName = "@masterString",
                DbType = DbType.String,
                Direction = ParameterDirection.Input,
                Value = masterRecords,
            });
            queryCommand.CommandText = "[dbo].[spUpsertMaster] @masterString";
            await DoPushMaster(queryCommand);
        }

        private async Task DoPushMaster(DbCommand queryCommand)
        {
            using (DbConnection connection = await GetConnection())
            {
                queryCommand.Connection = connection;
                try
                {
                    using (DbDataReader reader = await queryCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string id = Convert.ToString(reader["Id"]);
                            string action = Convert.ToString(reader["Action"]);
                            if (action == "UPDATE")
                            {
                                _logger.Warn("master entry with id={0} already exists", id);
                            }
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