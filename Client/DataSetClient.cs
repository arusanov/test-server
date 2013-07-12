using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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

    public class DataSetClient
    {
        private readonly IPEndPoint _ipEndPoint;
        private readonly Logger _logger = LogManager.GetLogger("client");

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
                        dataSets[dataSetType] = await ReadDataBlock(stream);
                    }
                    else
                    {
                        dataSets[DataSetType.Master] = await ReadDataBlock(stream);
                        dataSets[DataSetType.Details] = await ReadDataBlock(stream);
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

        private static async Task<byte[]> ReadDataBlock(NetworkStream stream)
        {
            var lengthBuffer = BitConverter.GetBytes(0);
            var readed = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            if (readed == lengthBuffer.Length)
            {
                //Start reading data
                var readedData = 0;
                var dataBuffer = new byte[BitConverter.ToInt32(lengthBuffer, 0)];
                do
                {
                    readedData += await stream.ReadAsync(dataBuffer, readedData, dataBuffer.Length - readedData);
                } while (readedData < dataBuffer.Length);
                return dataBuffer;
            }
            return null;
        }
    }
}