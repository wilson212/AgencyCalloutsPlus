using System;
using System.IO;
using System.Text;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// Provides an object wrapper for a file that is used to
    /// store LogMessage's into. Uses a Multi-Thread safe Queueing
    /// system, and provides full Asynchronous writing and flushing
    /// </summary>
    public class Log
    {
        /// <summary>
        /// Full path to the log file
        /// </summary>
        private static FileInfo LogFile;

        /// <summary>
        /// The <see cref="StreamWriter"/> for our <paramref name="LogFile"/>
        /// </summary>
        private static StreamWriter LogStream;

        /// <summary>
        /// Our lock object, preventing race conditions
        /// </summary>
        private static Object _sync = new Object();

        /// <summary>
        /// Provides a full sync lock between all isntances of this app
        /// </summary>
        private static Object _fullSync = new Object();

        /// <summary>
        /// Creates a new Log Writter instance
        /// </summary>
        /// <param name="FileLocation">The location of the logfile. If the file doesnt exist,
        /// It will be created.</param>
        public static void Initialize(string FileLocation)
        {
            if (LogFile == null)
            {
                // Test that we are able to open and write to the file
                LogFile = new FileInfo(FileLocation);
                FileStream fileStream = LogFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                LogStream = new StreamWriter(fileStream, Encoding.UTF8);
                LogStream.BaseStream.SetLength(0);
                LogStream.BaseStream.Seek(0, SeekOrigin.Begin);
                LogStream.Flush();
            }
        }

        public static void Info(string message)
        {
            Write(message, LogLevel.INFO);
        }

        public static void Warning(string message)
        {
            Write(message, LogLevel.WARN);
        }

        public static void Error(string message)
        {
            Write(message, LogLevel.ERROR);
        }
        public static void Debug(string message)
        {
            Write(message, LogLevel.DEBUG);
        }


        /// <summary>
        /// Adds a message to the queue, to be written to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        private static void Write(string message, LogLevel level)
        {
            // Only allow 1 thread at a time do these operations
            lock (_sync)
            {
                LogStream.WriteLine(String.Format("{0}: [{2}] {1}", DateTime.Now, message, level));
                LogStream.Flush();
            }
        }

        /// <summary>
        /// Adds a message to the queue, to be written to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        private static void Write(string message, LogLevel level, params object[] items)
        {
            // Only allow 1 thread at a time do these operations
            lock (_sync)
            {
                LogStream.WriteLine(String.Format("{0}: [{1}] {2}", DateTime.Now, level, String.Format(message, items)));
                LogStream.Flush();
            }
        }

        /// <summary>
        /// Destructor. Make sure we flush!
        /// </summary>
        public void Close()
        {
            try
            {
                LogStream?.Dispose();
            }
            catch (ObjectDisposedException) { } // Ignore
        }

        private enum LogLevel
        {
            DEBUG,

            INFO,

            WARN,

            ERROR,
        }
    }
}
