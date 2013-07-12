using System.Collections.Generic;
using System.Data;
using System.Data.EntityClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using Client.DAO.Extensions;
using Client.DAO.Models;
using NLog;

namespace Client.DAO
{
    public class DataSetDbService
    {
        internal class UpsertMaserData
        {
            public int Id { get; set; }

            public string Action { get; set; }
        }

        internal class UpsertDetailsData
        {
            public int MasterRecordId { get; set; }
            public string Name { get; set; }

            public string Action { get; set; }
        }

        private readonly Logger _logger = LogManager.GetLogger("data");

        private readonly Regex _parseRegex = new Regex(@"(?'id'\d+),""(?'name'[^""]+)",
            RegexOptions.Singleline | RegexOptions.Compiled);

        public void SaveDataSet(DataSetType dataSetType, byte[] data)
        {
            //Parse data
            var dataSetData = new Dictionary<int, string>();
            var dataString = Encoding.UTF8.GetString(data);
            using (var dataSetContext = new DataSets())
            {
                if (dataSetType == DataSetType.Master)
                {
                    var masterRecords = (from match in _parseRegex.Matches(dataString).Cast<Match>()
                        select new MasterRecord
                        {
                            Id = int.Parse(match.Groups["id"].Value),
                            Name = match.Groups["name"].Value
                        }).Distinct();
                    SaveMasterDataSet(dataSetContext, masterRecords);
                }
                else if (dataSetType == DataSetType.Details)
                {
                    var detailsRecords = (from match in _parseRegex.Matches(dataString).Cast<Match>()
                                         select new DetailsRecord
                                         {
                                             MasterRecordId = int.Parse(match.Groups["id"].Value),
                                             Name = match.Groups["name"].Value
                                         }).Distinct();
                    SaveDetailsDataSet(dataSetContext, detailsRecords);
                }
            }

        }

        private void SaveDetailsDataSet(DataSets dataSetContext, IEnumerable<DetailsRecord> detailsRecords)
        {
            var upsertResults = dataSetContext.UpsertMany<DetailsRecord, UpsertDetailsData>(detailsRecords)
                .Key(x => x.MasterRecordId)
                .Key(x => x.Name)
                .ExcludeField(x => x.MasterRecord)
                .ConstrainForeignKey<MasterRecord, int>(x => x.MasterRecordId, y => y.Id)
                .Query().ToList();

            var resultsLookup = upsertResults.ToLookup(x => x.MasterRecordId, y => y);
            foreach (var detailsRecord in detailsRecords)
            {
                if (!resultsLookup.Contains(detailsRecord.MasterRecordId))
                {
                    _logger.Error("no master entry for details entry '{1}' with id={0}",
                        detailsRecord.MasterRecordId, detailsRecord.Name);
                }
            }

            foreach (var upsertResult in upsertResults)
            {
                if (upsertResult.Action == "INSERT")
                {
                    _logger.Warn("master entry for details entry '{1}' with id={0} wasn't present in db",
                        upsertResult.MasterRecordId, upsertResult.Name);
                }
            }
        }

        private void SaveMasterDataSet(DataSets dataSetContext, IEnumerable<MasterRecord> masterRecords)
        {
            var upsertResults = dataSetContext.UpsertMany<MasterRecord,UpsertMaserData>(masterRecords).Identity(x=>x.Id).Query();
            foreach (var upsertResult in upsertResults.Where(x=>x.Action=="INSERT"))
            {
                _logger.Warn("master entry with id={0} wasn't present in db", upsertResult.Id);
            }
        }
    }


}