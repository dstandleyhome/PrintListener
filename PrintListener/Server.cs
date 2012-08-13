using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PrintListener
{
    public class Server
    {
        private readonly TypeOfPrinter _printerType;
        private readonly Thread listenThread;
        private readonly TcpListener tcpListener;
        private string _printerSpec;

        public Server(string printerSpec, int port, TypeOfPrinter typeOfPrinter)
        {
            _printerSpec = printerSpec;
            _printerType = typeOfPrinter;
            tcpListener = new TcpListener(IPAddress.Any, port);
            listenThread = new Thread(ListenForClients);
            listenThread.Start();
        }

        private void ListenForClients()
        {
            tcpListener.Start();

            while (true)
            {
                Console.WriteLine("Ready...");
                //blocks until a client has connected to the server
                TcpClient client = tcpListener.AcceptTcpClient();

                //create a thread to handle communication 
                //with connected client

                switch (_printerType)
                {
                    case TypeOfPrinter.Raw:
                        var clientThread = new Thread(HandleClientBytesComm);
                        clientThread.Start(client);
                        break;

                    case TypeOfPrinter.File:
                        var fileClientThread = new Thread(HandleClientComm);
                        fileClientThread.Start(client);
                        break;
                }
            }
        }

        private void HandleClientComm(object client)
        {
            var tcpClient = (TcpClient) client;
            NetworkStream clientStream = tcpClient.GetStream();

            Console.WriteLine("Receiving data on: " + ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address);

            var message = new byte[4096];
            int bytesRead;
            string fileName = Guid.NewGuid().ToString();
            bool firstRead = true;

            if (!_printerSpec.EndsWith(@"\"))
            {
                _printerSpec += @"\";
            }

            while (true)
            {
                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);

                    //Inspect first message to try to determine file extension.
                    if (firstRead)
                    {
                        string firstMessage = Encoding.ASCII.GetString(message);
                        if (firstMessage.StartsWith("BM"))
                        {
                            fileName += ".bmp";
                        }

                        if (firstMessage.StartsWith("%PDF"))
                        {
                            fileName += ".pdf";
                        }

                        if (firstMessage.StartsWith("%-12345X@PJL JOB"))
                        {
                            fileName += ".ps";
                        }

                        if (firstMessage.StartsWith("%!PS"))
                        {
                            fileName += ".ps";
                        }

                        firstRead = false;
                    }

                    //If actual bytes read are less than the buffer size, then resize array.
                    if(bytesRead < 4096)
                    {
                        var smallMessage = message.AsEnumerable().Take(bytesRead);
                        message = smallMessage.ToArray();
                    }

                    using (var stream = new FileStream(_printerSpec + fileName, FileMode.Append))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            writer.Write(message);
                            writer.Close();
                        }
                    }
                    //Re-initialize message in case it has been resized for a smaller message.
                    message = new byte[4096];
                }
                catch (Exception ex)
                {
                    //a socket error has occured
                    Console.WriteLine("Socket Error!");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                Console.Write(".");
            }

            tcpClient.Close();
            Console.WriteLine("file complete");

            //RawPrinterHelper.SendFileToPrinter(_printerSpec, "C:\\" + fileName);
        }

        private void HandleClientBytesComm(object client)
        {
            var tcpClient = (TcpClient) client;
            NetworkStream clientStream = tcpClient.GetStream();

            var message = new byte[4096];
            int bytesRead;
            var document = new ArrayList();
            int documentBytes = 0;

            Console.Write("Printing...");

            while (true)
            {
                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                    documentBytes += bytesRead;
                    foreach (byte messageByte in message)
                    {
                        document.Add(messageByte);
                    }
                }
                catch (Exception ex)
                {
                    //a socket error has occured
                    Console.WriteLine("Socket Error!");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                Console.Write(".");
            }

            tcpClient.Close();
            Console.Write("file complete");
            Console.WriteLine("");

            var documentByteArray = new byte[document.Count];
            document.CopyTo(documentByteArray, 0);
            //Get the unmanaged handle for the byte array
            IntPtr pointer = Marshal.AllocHGlobal(documentByteArray.Length);
            Marshal.Copy(documentByteArray, 0, pointer, documentByteArray.Length);

            RawPrinterHelper.SendBytesToPrinter(_printerSpec, pointer, documentBytes);

            Marshal.FreeHGlobal(pointer);
        }
    }
}