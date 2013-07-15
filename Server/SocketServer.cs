using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;

namespace Server
{
    public class SocketServer
    {
        private readonly IDataSetReader _dataSetReader;
        private readonly TcpListener _listener;
        private readonly Logger _logger = LogManager.GetLogger("socketServer");
        private bool _isStarted;

        public SocketServer(IPEndPoint ipEndPoint, IDataSetReader dataSetReader)
        {
            if (dataSetReader == null) throw new ArgumentNullException("dataSetReader");
            _dataSetReader = dataSetReader;
            _listener = new TcpListener(ipEndPoint);
        }

        public async Task Listen()
        {
            _isStarted = true;
            _listener.Start();
            while (_isStarted)
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                if (tcpClient != null)
                {
                    _logger.Debug("client connected");
                    ProcessClient(tcpClient); //Run task as async 
                }
            }
        }

        private async Task ProcessClient(TcpClient client)
        {
            //Read command. Since we have a test app we will send only command codes: 
            // 0 - only master recs
            // 1 - only details
            // 2 - both
            var commandBuffer = new Byte[1];
            using (NetworkStream stream = client.GetStream())
            {
                int readed = await stream.ReadAsync(commandBuffer, 0, commandBuffer.Length);
                if (readed == commandBuffer.Length)
                {
                    //We got correct command
                    _logger.Debug("client asked for set:{0}", commandBuffer[0]);
                    switch (commandBuffer[0])
                    {
                        case 0:
                            //Write master
                            await WriteData(stream, _dataSetReader.GetMasterRecords());
                            break;
                        case 1:
                            //Write details
                            await WriteData(stream, _dataSetReader.GetDetailsRecords());
                            break;
                        case 2:
                            //Write all
                            await WriteData(stream, _dataSetReader.GetMasterRecords());
                            await WriteData(stream, _dataSetReader.GetDetailsRecords());
                            break;
                    }
                }
            }
            _logger.Debug("client closed");
            client.Close();
        }

        private async Task WriteData(Stream stream, Stream dataToWrite)
        {
            _logger.Debug("writing data to client");
            byte[] length = BitConverter.GetBytes(dataToWrite.Length);
            await stream.WriteAsync(length, 0, length.Length);
            await dataToWrite.CopyToAsync(stream);
            _logger.Debug("writing data to client completed");
        }

        public void Stop()
        {
            _isStarted = false;
            _listener.Stop();
        }
    }
}