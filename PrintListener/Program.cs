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

            var argsList = new List<string>(args);

            //switch = printer name
            var printerSwitchIndex = argsList.FindIndex(p => p.Contains("-printer"));
            var printFileOutputPathIndex = argsList.FindIndex(p => p.Contains("-printtofile"));
            var portSwitchIndex = argsList.FindIndex(p => p.Contains("-port"));

            //Either a -printer parameter or a -printfileto parameter is required.
            if ((printerSwitchIndex & printFileOutputPathIndex) < 0 || portSwitchIndex < 0)
            {
                ShowUsage();
            }

            var portNumber=0;
            if(!int.TryParse(argsList[portSwitchIndex + 1], out portNumber))
            {
                ShowUsage();
                Console.WriteLine("-port must be an integer.");
            }

            if (printerSwitchIndex != -1)
            {
                var printerName = argsList[printerSwitchIndex + 1];
                Console.WriteLine("Redirecting port {0} to printer {1}", portNumber, printerName);

                Server server = new Server(printerName, portNumber, TypeOfPrinter.Raw);
            }

            if (printFileOutputPathIndex != -1)
            {
                var printerName = argsList[printFileOutputPathIndex + 1];
                Console.WriteLine("Redirecting port {0} to file path {1}", portNumber, printerName);

                Server server = new Server(printerName, portNumber, TypeOfPrinter.File);
            }

        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: PrintListener -printer \"<local printer name>\" -port <port number>");
            Console.WriteLine("Usage: PrintListener -printtofile \"<filespec>\" -port <port number>");
            System.Environment.Exit(-1);
        }
    }
}
