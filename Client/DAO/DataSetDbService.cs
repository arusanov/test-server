using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Client.DAO.Models;
using NLog;

namespace Client.DAO
{
    public class DataSetDbService
    {
        private readonly string _connectionString;
        private readonly DbProviderFactory _dbFactory;

        private readonly ConcurrentQueue<ICollection<DetailsRecord>> _detailsRecordsQueue =
            new ConcurrentQueue<ICollection<DetailsRecord>>();

        private readonly Logger _logger = LogManager.GetLogger("data");

        private readonly ConcurrentQueue<ICollection<MasterRecord>> _masterRecordsQueue =
            new ConcurrentQueue<ICollection<MasterRecord>>();

        private readonly Regex _parseRegex = new Regex(@"(?'id'\d+),""(?'name'[^""]+)",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private bool _isProcessing;

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
                IEnumerable<IEnumerable<MasterRecord>> masterRecords = (from match in
                    _parseRegex.Matches(dataString).Cast<Match>()
                    select new MasterRecord
                    {
                        Id = long.Parse(match.Groups["id"].Value),
                        Name = match.Groups["name"].Value
                    }).ToList().ByParts(500);
                foreach (var masterRecord in masterRecords)
                {
                    _masterRecordsQueue.Enqueue(new HashSet<MasterRecord>(masterRecord));
                }
            }
            else if (dataSetType == DataSetType.Details)
            {
                IEnumerable<IEnumerable<DetailsRecord>> detailsRecords = (from match in
                    _parseRegex.Matches(dataString).Cast<Match>()
                    select new DetailsRecord
                    {
                        MasterRecordId = long.Parse(match.Groups["id"].Value),
                        Name = match.Groups["name"].Value
                    }).Distinct().ToList().ByParts(500);
                foreach (var detailsRecord in detailsRecords)
                {
                    _detailsRecordsQueue.Enqueue(new HashSet<DetailsRecord>(detailsRecord));
                }
            }
            if (_masterRecordsQueue.Count > 10 || _detailsRecordsQueue.Count > 10 || !_isProcessing)
            {
                await ProcessSets();
            }
        }

        private async Task ProcessSets()
        {
            if (!_isProcessing && (_masterRecordsQueue.Any() || _detailsRecordsQueue.Any()))
            {
                _isProcessing = true;
                var tasks = new List<Task>();
                for (int i = 0; i < 8; i++) // 8 task to run
                {
                    tasks.Add(ProcessQueues());
                }
                await Task.WhenAll(tasks);
                _isProcessing = false;
                //Queue for next proceesing
                await ProcessSets();
            }
        }

        private async Task ProcessQueues()
        {
            ICollection<MasterRecord> masterRecords;
            while (_masterRecordsQueue.TryDequeue(out masterRecords))
            {
                await SaveMasterDataSet(masterRecords);
            }
            ICollection<DetailsRecord> detailsRecords;
            while (_detailsRecordsQueue.TryDequeue(out detailsRecords))
            {
                await SaveDetailsDataSet(detailsRecords);
            }
        }

        private async Task SaveDetailsDataSet(ICollection<DetailsRecord> detailsRecords)
        {
            DbCommand queryCommand = _dbFactory.CreateCommand();
            //Build sql
            var parametersBuilder = new StringBuilder();
            int paramIndex = 0;
            foreach (DetailsRecord detailsRecord in detailsRecords)
            {
                parametersBuilder.AppendFormat("(@MasterRecordId{0},@Name{0}),", paramIndex);
                var masterRecordIdParameter = new SqlParameter
                {
                    ParameterName = "@MasterRecordId" + paramIndex,
                    DbType = DbType.Int64,
                    Direction = ParameterDirection.Input,
                    Value = detailsRecord.MasterRecordId
                };
                queryCommand.Parameters.Add(masterRecordIdParameter);
                var nameParameter = new SqlParameter
                {
                    ParameterName = "@Name" + paramIndex,
                    DbType = DbType.String,
                    Direction = ParameterDirection.Input,
                    Value = detailsRecord.Name
                };
                queryCommand.Parameters.Add(nameParameter);
                paramIndex++;
            }

            parametersBuilder.Remove(parametersBuilder.Length - 1, 1);
            var sql = new StringBuilder();
            sql.AppendFormat("merge into DetailsRecords as T using (values {0}) as S (MasterRecordId, Name) ",
                parametersBuilder);
            sql.Append("on (T.MasterRecordId = S.MasterRecordId and T.Name = S.Name) ");
            sql.Append(
                "when matched and exists (SELECT * FROM MasterRecords WHERE S.MasterRecordId = Id) then update set T.Name = S.Name ");
            sql.Append(
                "when not matched and exists (SELECT * FROM MasterRecords WHERE S.MasterRecordId = Id) then insert (MasterRecordId,Name) values (S.MasterRecordId,S.Name) ");
            sql.Append("output S.MasterRecordId as Id,S.Name,$action as Action;");
            queryCommand.CommandText = sql.ToString();

            _logger.Debug("updating db");
            await DoPushDetails(detailsRecords, queryCommand);
        }

        private async Task DoPushDetails(IEnumerable<DetailsRecord> detailsRecords, DbCommand queryCommand)
        {
            var hashSet = new HashSet<long>();
            using (DbConnection connection = await GetConnection())
            {
                queryCommand.Connection = connection;
                try
                {
                    using (DbDataReader reader = await queryCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            long id = Convert.ToInt64(reader["Id"]);
                            string name = Convert.ToString(reader["Name"]);
                            string action = Convert.ToString(reader["Action"]);
                            if (action == "UPDATE")
                            {
                                _logger.Warn("details entry '{1}' with MasterId={0} already exists present",
                                    id, name);
                            }
                            hashSet.Add(id);
                        }
                        foreach (DetailsRecord detailsRecord in
                            detailsRecords.Where(detailsRecord => !hashSet.Contains(detailsRecord.MasterRecordId)))
                        {
                            _logger.Error("no master entry for details entry '{1}' with id={0}",
                                detailsRecord.MasterRecordId, detailsRecord.Name);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error reading data. Repeating", e);
                    DoPushDetails(detailsRecords, queryCommand);
                }
            }
        }

        private async Task SaveMasterDataSet(IEnumerable<MasterRecord> masterRecords)
        {
            DbCommand queryCommand = _dbFactory.CreateCommand();
            //Build sql
            var parametersBuilder = new StringBuilder();
            int paramIndex = 0;
            foreach (MasterRecord masterRecord in masterRecords)
            {
                parametersBuilder.AppendFormat("(@Id{0},@Name{0}),", paramIndex);
                var idParameter = new SqlParameter
                {
                    ParameterName = "@Id" + paramIndex,
                    DbType = DbType.Int64,
                    Direction = ParameterDirection.Input,
                    Value = masterRecord.Id
                };
                queryCommand.Parameters.Add(idParameter);
                var nameParameter = new SqlParameter
                {
                    ParameterName = "@Name" + paramIndex,
                    DbType = DbType.String,
                    Direction = ParameterDirection.Input,
                    Value = masterRecord.Name
                };
                queryCommand.Parameters.Add(nameParameter);
                paramIndex++;
            }

            parametersBuilder.Remove(parametersBuilder.Length - 1, 1);
            var sql = new StringBuilder();
            sql.AppendLine("SET IDENTITY_INSERT MasterRecords ON");
            sql.AppendFormat("merge into MasterRecords as T using (values {0}) as S (Id, Name) ", parametersBuilder);
            sql.Append("on (T.Id = S.Id) ");
            sql.Append("when matched then update set T.Name = S.Name ");
            sql.Append("when not matched then insert (Id,Name) values (S.Id,S.Name) ");
            sql.Append("output S.Id,S.Name,$action as Action;");
            sql.AppendLine("SET IDENTITY_INSERT MasterRecords OFF");

            queryCommand.CommandText = sql.ToString();
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
                            long id = Convert.ToInt64(reader["Id"]);
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
                    _logger.ErrorException("Error reading data. Repeating", e);
                    DoPushMaster(queryCommand);
                }
            }
        }

    }
}