using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Client
{
    public enum DataSetType
    {
        Master = 0,
        Details = 1,
        Both = 2
    }

    public delegate void OnDataSetLinesRecievedDelegate(DataSetType dataSetType, string lines);

    public class DataSetClient
    {
        private const int ReadBufferSize = 1024*30; //30kb line
        private readonly IPEndPoint _ipEndPoint;
        private readonly Logger _logger = LogManager.GetLogger("client");

        public DataSetClient(IPEndPoint ipEndPoint)
        {
            _ipEndPoint = ipEndPoint;
        }

        public event OnDataSetLinesRecievedDelegate OnDataSetLinesRecieved = null;

        protected virtual void OnOnDataSetLinesRecieved(DataSetType datasettype, string lines)
        {
            OnDataSetLinesRecievedDelegate handler = OnDataSetLinesRecieved;
            if (handler != null) handler(datasettype, lines);
        }

        public async Task QueryServer(DataSetType dataSetType)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(_ipEndPoint.Address, _ipEndPoint.Port);
                _logger.Debug("client connected. retrieving datasets {0}", dataSetType);
                using (NetworkStream stream = client.GetStream())
                {
                    await stream.WriteAsync(new[] {(byte) dataSetType}, 0, 1); //Write command
                    if (dataSetType != DataSetType.Both)
                    {
                        await ReadDataBlock(dataSetType, stream);
                    }
                    else
                    {
                        await ReadDataBlock(DataSetType.Master, stream);
                        await ReadDataBlock(DataSetType.Details, stream);
                    }
                }
                _logger.Debug("Data readed");
                client.Close();
            }
        }

        private async Task ReadDataBlock(DataSetType dataSetType, NetworkStream stream)
        {
            byte[] lengthBuffer = BitConverter.GetBytes((long) 0);
            int readed = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            if (readed == lengthBuffer.Length)
            {
                //Start reading data
                long readData = 0L;
                long totalDataLength = BitConverter.ToInt64(lengthBuffer, 0);
                var dataBuffer = new byte[ReadBufferSize];
                var readLines = new StringBuilder();
                do
                {
                    readData +=
                        await
                            stream.ReadAsync(dataBuffer, 0,
                                (int) Math.Min(dataBuffer.Length, totalDataLength - readData));
                    //Parse lines
                    readLines.Append(Encoding.UTF8.GetString(dataBuffer));
                    int indexEndLine = LastIndexOf(readLines, Environment.NewLine);
                    if (indexEndLine > 0)
                    {
                        OnOnDataSetLinesRecieved(dataSetType, readLines.ToString(0, indexEndLine));
                        indexEndLine += Environment.NewLine.Length;
                        readLines.Remove(0, indexEndLine);
                    }
                } while (readData < totalDataLength);
            }
        }

        private int LastIndexOf(StringBuilder stringBulder, string stringToFind)
        {
            if (stringBulder == null) throw new ArgumentNullException("stringBulder");
            if (stringToFind == null) throw new ArgumentNullException("stringToFind");

            for (int i = stringBulder.Length - stringToFind.Length; i >= 0; i--)
            {
                if (!stringToFind.Where((t, j) => stringBulder[i + j] != t).Any())
                {
                    return i;
                }
            }
            return -1;
        }
    }
}