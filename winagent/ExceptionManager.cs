﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace winagent
{
    static class ExceptionManager
    {
        internal static void HandleError(string errorMessage, int errorCode, string exception)
        {
            if (Environment.UserInteractive)
            {
                Console.Error.WriteLine(String.Format("Error: {0}", errorCode));
                Console.Error.WriteLine(errorMessage);
#if DEBUG
                Console.WriteLine(e.ToString());
#endif
            }
            else
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    StringBuilder message = new StringBuilder(errorMessage);
                    message.Append(Environment.NewLine);
                    message.Append(exception);

                    eventLog.Source = "Winagent";
                    eventLog.WriteEntry(message.ToString(), EventLogEntryType.Error, errorCode, 1);
                }
            }
        }

        internal static void HandleInformation(string errorMessage, int warningCode, string exception)
        {
            if (Environment.UserInteractive)
            {
                Console.Error.WriteLine(String.Format("Warning: {0}", warningCode));
                Console.Error.WriteLine(errorMessage);
#if DEBUG
                Console.WriteLine(e.ToString());
#endif
            }
            else
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    StringBuilder message = new StringBuilder(errorMessage);
                    message.Append(Environment.NewLine);
                    message.Append(exception);

                    eventLog.Source = "Winagent";
                    eventLog.WriteEntry(message.ToString(), EventLogEntryType.Warning, warningCode, 1);
                }
            }
        }
    }
}