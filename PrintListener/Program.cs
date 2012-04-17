using System;
using System.Collections.Generic;

namespace PrintListener
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var argsList = new List<string>(args);

            //switch = printer name
            int printerSwitchIndex = argsList.FindIndex(p => p.Contains("-printer"));
            int printFileOutputPathIndex = argsList.FindIndex(p => p.Contains("-printtofile"));
            int portSwitchIndex = argsList.FindIndex(p => p.Contains("-port"));

            //Either a -printer parameter or a -printfileto parameter is required.
            if ((printerSwitchIndex & printFileOutputPathIndex) < 0 || portSwitchIndex < 0)
            {
                ShowUsage();
            }

            int portNumber = 0;
            if (!int.TryParse(argsList[portSwitchIndex + 1], out portNumber))
            {
                ShowUsage();
                Console.WriteLine("-port must be an integer.");
            }

            if (printerSwitchIndex != -1)
            {
                string printerName = argsList[printerSwitchIndex + 1];
                Console.WriteLine("Redirecting port {0} to printer {1}", portNumber, printerName);

                var server = new Server(printerName, portNumber, TypeOfPrinter.Raw);
            }

            if (printFileOutputPathIndex != -1)
            {
                string printerName = argsList[printFileOutputPathIndex + 1];
                Console.WriteLine("Redirecting port {0} to file path {1}", portNumber, printerName);

                var server = new Server(printerName, portNumber, TypeOfPrinter.File);
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage: PrintListener -printer \"<local printer name>\" -port <port number>");
            Console.WriteLine("Usage: PrintListener -printtofile \"<filespec>\" -port <port number>");
            Environment.Exit(-1);
        }
    }
}