using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CGMinerLauncher
{
    internal class ApiWorker
    {
        public string Host { get; private set; }
        public Int32 Port { get; private set; }

        public ApiWorker(string host, Int32 port)
        {
            if (IPAddress.TryParse(host, out IPAddress ipAddress) && port > 1023 && port < 65536)
            {
                Host = host;
                Port = port;
            }
            else
            {
                throw new Exception("Wrong Host:Port parameters!");
            }
        }

        public string Request(string cmd)
        {
            string res = null;
            try
            {
                using (var client = new TcpClient(Host, Port))
                {
                    using (Stream stream = client.GetStream())
                    {
                        using (var streamReader = new StreamReader(stream))
                        {
                            var cmdByte = Encoding.ASCII.GetBytes(cmd);
                            stream.Write(cmdByte, 0, cmdByte.Length);

                            res = streamReader.ReadLine();
                            streamReader.Close();
                        }
                        stream.Close();
                    }
                    client.Close();
                }
            }
            catch
            {
            }

            return res;
        }
    }
}