// ServerInterface.cs
// Adapted from code by Justin Stenning
// Licensed under the terms of the MIT License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinaHook
{
    /// <summary>
    /// Provides an interface for communicating from the client (target) to the server (injector)
    /// </summary>
    public class ServerInterface : MarshalByRefObject
    {
        static public string WatchFile { get; set; }

        public string GetWatchFile()
        {
            return WatchFile;
        }

        public void IsInstalled(int clientPID)
        {
            Console.WriteLine("Tina has injected TinaHook into process {0}.", clientPID);
            Console.WriteLine("Watch and output the {0} file.\r\n", WatchFile);
        }

        /// <summary>
        /// Output the message to the console.
        /// </summary>
        /// <param name="fileNames"></param>
        public void ReportMessages(string[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                Console.Write(messages[i]);
            }
        }

        public void ReportMessage(string message)
        {
            Console.Write(message);
        }

        /// <summary>
        /// Report exception
        /// </summary>
        /// <param name="e"></param>
        public void ReportException(Exception e)
        {
            Console.WriteLine("The target process has reported an error:\r\n" + e.ToString());
        }

        int count = 0;

        /// <summary>
        /// Called to confirm that the IPC channel is still open / host application has not closed
        /// </summary>
        public bool Heartbeat()
        {
            return true;
        }

        /// <summary>
        /// Called to confirm that the IPC channel is still open / host application has not closed
        /// </summary>
        public void Ping()
        {
            // Mintty and bash raise error
            return;

            // Output token animation to visualise Ping
            var oldTop = Console.CursorTop;
            var oldLeft = Console.CursorLeft;
            Console.CursorVisible = false;

            var chars = "\\|/-";
            Console.SetCursorPosition(Console.WindowWidth - 1, oldTop - 1);
            Console.Write(chars[count++ % chars.Length]);

            Console.SetCursorPosition(oldLeft, oldTop);
            Console.CursorVisible = true;
        }
    }
}
