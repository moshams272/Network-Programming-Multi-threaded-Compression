using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Threading;

namespace ServerCmpress
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int port = 9050;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine($"Server started. Listening on port {port}...");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }


       
        static void HandleClient(TcpClient client)
        {
            Console.WriteLine($"\n[+] New connection from client: {client.Client.RemoteEndPoint}");

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                   
                    byte[] sizeBuffer = new byte[8];
                    ReadExactly(stream, sizeBuffer, 8);
                    long originalSize = BitConverter.ToInt64(sizeBuffer, 0);
                    Console.WriteLine($"[*] Receiving file of size {originalSize} bytes...");

                    
                    byte[] fileData = new byte[originalSize];
                    ReadExactly(stream, fileData, (int)originalSize);
                    Console.WriteLine("[*] File received completely. Compressing...");

             
                    byte[] compressedData = CompressData(fileData);
                    long compressedSize = compressedData.Length;
                    Console.WriteLine($"[*] Compression complete. New size: {compressedSize} bytes.");

                  
                    stream.Write(BitConverter.GetBytes(compressedSize), 0, 8);

                
                    stream.Write(compressedData, 0, compressedData.Length);
                    Console.WriteLine($"[+] Compressed version sent to client successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] Error with client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

   
        static byte[] CompressData(byte[] data)
        {
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (GZipStream zipStream = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    zipStream.Write(data, 0, data.Length);
                }
                return outputStream.ToArray();
            }
        }

      
        static void ReadExactly(NetworkStream stream, byte[] buffer, int length)
        {
            int totalRead = 0;
            while (totalRead < length)
            {
                int read = stream.Read(buffer, totalRead, length - totalRead);
                if (read == 0) throw new Exception("Connection dropped unexpectedly.");
                totalRead += read;
            }
        }
    }
}