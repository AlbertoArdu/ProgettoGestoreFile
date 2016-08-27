using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;

namespace WPFPageSwitch
{
    enum Level
    {
        DEBUG=0,
        INFO=1,
        WARN=2,
        ERR=3
    }

    /// <summary>
    /// Singleton class to record log. Settings in \Properties\LogSettings.settings
    /// </summary>
    class Log
    {
        static bool log_on_console;
        static string log_file;
        static string log_path;
        static int message_max_lenght;
        TextWriter logStream = null;
        static private Log l=null;

        static private Mutex constuct_mutex = new Mutex();
        private Mutex log_mutex;
        private const string separator = "\t";
        private Level level;

        private Log()
        {
            this.log_mutex = new Mutex();
            if(logStream == null)
            {
                log_on_console = Properties.LogSettings.Default.log_on_console;
                log_file = Properties.LogSettings.Default.log_file;
                log_path = Properties.LogSettings.Default.log_path;
                message_max_lenght = Properties.LogSettings.Default.message_max_lenght;
                level = (Level)Properties.LogSettings.Default.level;

                if (log_on_console)
                {
                    this.logStream = Console.Error;
                }
                else
                {
                    try
                    {
                        logStream = new StreamWriter(log_path + "\\" + log_file);
                    }
                    catch (IOException e)
                    {
                        this.logStream = Console.Error;
                        this.log("Error opening log file ("+e.Message+"). Switching to log on standard error");
                    }
                }
            }
        }

        static public Log getLog()
        {
            try {
                constuct_mutex.WaitOne();
                if (l == null)
                {
                    l = new Log();
                }
            }
            finally
            {
                constuct_mutex.ReleaseMutex();
            }
            return l;
        }
        /// <summary>
        /// Write a log with format:
        /// TIME+separator+FILENAME+separator+LINE_NUMBER+separator+MESSAGE+\r\n
        /// </summary>
        /// <param name="message">Messaggio che verrà stampato</param>
        /// <param name="level">Livello di log</param>
        /// <returns></returns>
        public bool log(string message,
                Level level = 0,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0
            )
        {
            try
            {
                if (level >= this.level)
                {
                    log_mutex.WaitOne();
                    sourceFilePath = Path.GetFileName(sourceFilePath);
                    //The following line delete all non-printable character from message string
                    message = new String(message.Where((c, b) => { return Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || Char.IsSeparator(c); }).ToArray());
                    if (message.Length > message_max_lenght)
                    {
                        message = message.Substring(0, message_max_lenght);
                    }

                    StringBuilder entry = new StringBuilder();
                    entry.Append(DateTime.Now.ToString("dd-MM HH:mm:ss.fff"));

                    if (sourceFilePath.Length > 0)
                        entry.Append(separator)
                        .Append(sourceFilePath);

                    if (sourceLineNumber > 0)
                        entry.Append(separator)
                        .Append("Line: " + sourceLineNumber);
                    if (memberName.Length > 0)
                        entry.Append(separator)
                        .Append("Function: " + memberName);

                    entry.Append(separator)
                        .AppendLine(message); //Line separator

                    this.logStream.Write(entry);
                    this.logStream.Flush();
                }
            }
            finally
            {
                log_mutex.ReleaseMutex();
            }
            return true;
        }
    }
}
