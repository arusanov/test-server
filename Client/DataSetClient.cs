using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Client
{
    public enum DataSetType
    {
        Master=0,
        Details = 1,
        Both = 2
    }

    public delegate void OnDataSetLinesRecievedDelegate(DataSetType dataSetType, string lines);

    public class DataSetClient
    {
        private readonly IPEndPoint _ipEndPoint;
        private readonly Logger _logger = LogManager.GetLogger("client");
        public event OnDataSetLinesRecievedDelegate OnDataSetLinesRecieved = null;

        protected virtual void OnOnDataSetLinesRecieved(DataSetType datasettype, string lines)
        {
            OnDataSetLinesRecievedDelegate handler = OnDataSetLinesRecieved;
            if (handler != null) handler(datasettype, lines);
        }

        public DataSetClient(IPEndPoint ipEndPoint)
        {
            _ipEndPoint = ipEndPoint;

        }

        public async Task<IDictionary<DataSetType, byte[]>> QueryServer(DataSetType dataSetType)
        {
            var dataSets = new Dictionary<DataSetType, byte[]>();
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(_ipEndPoint.Address, _ipEndPoint.Port);
                _logger.Debug("cleint connected. retrieving datasets {0}",dataSetType);
                using (var stream = client.GetStream())
                {
                    await stream.WriteAsync(new[] {(byte) dataSetType}, 0, 1);//Write command
                    if (dataSetType != DataSetType.Both)
                    {
                        await ReadDataBlock(dataSetType,stream);
                    }
                    else
                    {
                        await ReadDataBlock(DataSetType.Master,stream);
                        await ReadDataBlock(DataSetType.Details,stream);
                    }
                }
                client.Close();
            }
            foreach (var dataSet in dataSets)
            {
                _logger.Debug("retrieved dataset {0}. length: {1}", dataSet.Key, dataSet.Value!=null?dataSet.Value.Length:0);
            }
            return dataSets;
        }

        private async Task ReadDataBlock(DataSetType dataSetType, NetworkStream stream)
        {
            var lengthBuffer = BitConverter.GetBytes((long)0);
            var readed = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            if (readed == lengthBuffer.Length)
            {
                //Start reading data
                var readedData = 0L;
                var totalDataLength = BitConverter.ToInt64(lengthBuffer, 0);
                var dataBuffer = new byte[1024*10];//10kb line
                var readedLines = new StringBuilder();
                do
                {
                    readedData += await stream.ReadAsync(dataBuffer, 0,(int)Math.Min(dataBuffer.Length, totalDataLength - readedData));
                    //Parse lines
                    readedLines.Append(Encoding.UTF8.GetString(dataBuffer));
                    //Look inside builder and find full strings
                    var lines = readedLines.ToString();
                    var indexEndLine = lines.LastIndexOf(Environment.NewLine, System.StringComparison.Ordinal);
                    if (indexEndLine>0)
                    {
                        OnOnDataSetLinesRecieved(dataSetType, lines.Substring(0,indexEndLine));
                        readedLines.Clear();
                        indexEndLine += Environment.NewLine.Length;
                        readedLines.Append(lines.Substring(indexEndLine, lines.Length - indexEndLine));//Add last uncompleted string
                        
                    }
                } while (readedData < totalDataLength);
            }

            
        }
    }
}