using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace PrintListener
{
    class Program
    {
        static void Main(string[] args)
        {
            //TODO: Add usage

            var argsList = new List<string>(args);

            //switch = printer name
            var printerSwitchIndex = argsList.FindIndex(p => p.Contains("-printer"));
            var portSwitchIndex = argsList.FindIndex(p => p.Contains("-port"));

            if (printerSwitchIndex < 0 || portSwitchIndex < 0)
            {
                ShowUsage();
            }

            var printerName = argsList[printerSwitchIndex + 1];

            var portNumber=0;
            if(!int.TryParse(argsList[portSwitchIndex + 1], out portNumber))
            {
                ShowUsage();
                Console.WriteLine("-port must be an integer.");
            }

            Console.WriteLine("Redirecting port {0} to printer {1}", portNumber, printerName);

            Server server = new Server(printerName, portNumber);
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: PrintListener -printer \"<local printer name>\" -port <port number>");
            System.Environment.Exit(-1);
        }
    }
}
