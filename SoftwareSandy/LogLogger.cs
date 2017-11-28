using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace SoftwareSandy
{
    public class LogLogger
    {
        EventLog _appLog = null;
        public LogLogger(string eventLogname, string applicationName)
        {
            _appLog = new EventLog(eventLogname); // e.g "Application"
            _appLog.Source = applicationName; // "The name of your application"
        }

        public void LogError(string message)
        {
            _appLog.WriteEntry(message, EventLogEntryType.Error);
        }

        public void LogInformation(string message)
        {
            _appLog.WriteEntry(message, EventLogEntryType.Information);
        }
    }
}
