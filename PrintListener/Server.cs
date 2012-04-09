using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace PrintListener
{
    public class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private string _printerSpec;
        private TypeOfPrinter _printerType;

        public Server(string printerSpec, int port, TypeOfPrinter typeOfPrinter)
        {

            this._printerSpec = printerSpec;
            this._printerType = typeOfPrinter;
            this.tcpListener = new TcpListener(IPAddress.Any, port);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                Console.WriteLine("Ready...");
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //create a thread to handle communication 
                //with connected client

                switch (_printerType)
                {
                    case TypeOfPrinter.Raw:
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientBytesComm));
                        clientThread.Start(client);
                        break;

                    case TypeOfPrinter.File:
                        Thread fileClientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                        fileClientThread.Start(client);
                        break;
                }

            }
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;
            string fileName = Guid.NewGuid().ToString();
            var firstRead = true;

            if(!this._printerSpec.EndsWith(@"\"))
            {
                this._printerSpec += @"\";
            }

            while (true)
            {
                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);

                    //Inspect first message to try to determine file extension.
                    if(firstRead)
                    {
                        var firstMessage = System.Text.Encoding.ASCII.GetString(message);
                        if(firstMessage.StartsWith("BM"))
                        {
                            fileName += ".bmp";
                        }

                        if(firstMessage.StartsWith("%PDF"))
                        {
                            fileName += ".pdf";
                        }

                        firstRead = false;
                    }

                    using (FileStream stream = new FileStream(this._printerSpec + fileName, FileMode.Append))
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            writer.Write(message);
                            writer.Close();
                        }
                    }
                }
                catch
                {
                    //a socket error has occured
                    Console.WriteLine("Socket Error!");
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
            TcpClient tcpClient = (TcpClient) client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[4096];
            int bytesRead;
            ArrayList document = new ArrayList();
            int documentBytes = 0;

            Console.Write("Printing...");

            while (true)
            {
                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                    documentBytes += bytesRead;
                    foreach (var messageByte in message)
                    {
                        document.Add(messageByte);
                    }
                }
                catch
                {
                    //a socket error has occured
                    Console.WriteLine("Socket Error!");
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

            byte[] documentByteArray = new byte[document.Count];
            document.CopyTo(documentByteArray,0);
            //Get the unmanaged handle for the byte array
            IntPtr pointer = Marshal.AllocHGlobal(documentByteArray.Length);
            Marshal.Copy(documentByteArray, 0, pointer, documentByteArray.Length);

            RawPrinterHelper.SendBytesToPrinter(_printerSpec, pointer, documentBytes);

            Marshal.FreeHGlobal(pointer);
        }
    }
}
