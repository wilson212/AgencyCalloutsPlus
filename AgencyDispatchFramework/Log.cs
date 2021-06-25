using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// Provides an object wrapper for a file that is used to
    /// store messages into. This class is thread safe.
    /// </summary>
    internal class Log
    {
        /// <summary>
        /// Full path to the log file
        /// </summary>
        private static FileInfo LogFile;

        /// <summary>
        /// The <see cref="StreamWriter"/> instance for our <see cref="Log.LogFile"/>
        /// </summary>
        private static StreamWriter LogStream;

        /// <summary>
        /// Our lock object, preventing race conditions
        /// </summary>
        private static Object _threadSync = new Object();

        /// <summary>
        /// Gets or sets the <see cref="LogLevel"/>
        /// </summary>
        private static LogLevel LoggingLevel;

        /// <summary>
        /// Initilizes a new log file by clearing old data, or creating the file
        /// if it does not exist.
        /// </summary>
        /// <param name="fileLocation">The location of the logfile. If the file doesnt exist, it will be created.</param>
        public static void Initialize(string fileLocation, LogLevel level)
        {
            if (LogFile == null)
            {
                // Test that we are able to open and write to the file
                LogFile = new FileInfo(fileLocation);
                FileStream fileStream = LogFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                LogStream = new StreamWriter(fileStream, Encoding.UTF8);
                LogStream.BaseStream.SetLength(0);
                LogStream.BaseStream.Seek(0, SeekOrigin.Begin);
                LogStream.Flush();
            }
        }

        /// <summary>
        /// Appends the log file with a message at the <see cref="LogLevel.INFO"/> level
        /// </summary>
        /// <param name="message">The message to add to the log file</param>
        public static void Info(params string[] message)
        {
            if (LogLevel.INFO >= LoggingLevel)
                Write(message, LogLevel.INFO);
        }

        /// <summary>
        /// Appends the log file with a message at the <see cref="LogLevel.WARN"/> level
        /// </summary>
        /// <param name="message">The message to add to the log file</param>
        public static void Warning(params string[] message)
        {
            if (LogLevel.WARN >= LoggingLevel)
                Write(message, LogLevel.WARN);
        }

        /// <summary>
        /// Appends the log file with a message at the <see cref="LogLevel.ERROR"/> level
        /// </summary>
        /// <param name="message">The message to add to the log file</param>
        public static void Error(params string[] message)
        {
            if (LogLevel.ERROR >= LoggingLevel)
                Write(message, LogLevel.ERROR);
        }

        /// <summary>
        /// Appends the log file with a message at the <see cref="LogLevel.DEBUG"/> level
        /// </summary>
        /// <param name="message">The message to add to the log file</param>
        public static void Debug(params string[] message)
        {
            if (LogLevel.DEBUG >= LoggingLevel)
                Write(message, LogLevel.DEBUG);
        }

        /// <summary>
        /// Appends the log file with exception tracing information
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="optionMessage"></param>
        public static void Exception(Exception exception, string optionMessage = null)
        {
            var data = new Dictionary<string, string>(1);

            if (!String.IsNullOrEmpty(optionMessage))
                data.Add("Message", optionMessage);

            Exception(exception, data);
        }

        /// <summary>
        /// Appends the log file with exception tracing information
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="data"></param>
        public static void Exception(Exception exception, Dictionary<string, string> data)
        {
            // Only allow 1 thread at a time do these operations
            lock (_threadSync)
            {
                // Write the header data
                LogStream.WriteLine("-------- AgencyDispatchFramework Exception Trace Entry --------");

                // Log details
                LogStream.WriteLine("Exception Date: " + DateTime.Now.ToString());
                LogStream.WriteLine("Os Version: " + Environment.OSVersion.VersionString);
                LogStream.WriteLine("Architecture: " + ((Environment.Is64BitOperatingSystem) ? "x64" : "x86"));

                // Add additional data
                foreach (var item in data)
                {
                    LogStream.WriteLine($"{item.Key}: {item.Value}");
                }

                // Start logging all levels of the exception
                LogStream.WriteLine();
                LogStream.WriteLine("-------- Exception --------");

                // Log each inner exception
                int i = 0;
                while (true)
                {
                    // Create a stack trace
                    StackTrace trace = new StackTrace(exception, true);
                    StackFrame frame = trace.GetFrame(0);

                    // Log the current exception
                    LogStream.WriteLine("Type: " + exception.GetType().FullName);
                    LogStream.WriteLine("Message: " + exception.Message.Replace("\n", "\n\t"));
                    LogStream.WriteLine("Target Method: " + frame.GetMethod().Name);
                    LogStream.WriteLine("File: " + frame.GetFileName());
                    LogStream.WriteLine("Line: " + frame.GetFileLineNumber());
                    LogStream.WriteLine("StackTrace:");
                    LogStream.WriteLine(exception.StackTrace.TrimEnd());

                    // If we have no more inner exceptions, end the logging
                    if (exception.InnerException == null)
                        break;

                    // Prepare next inner exception data
                    LogStream.WriteLine();
                    LogStream.WriteLine("-------- Inner Exception ({0}) --------", i++);
                    exception = exception.InnerException;
                }

                LogStream.Flush();
            }
        }

        /// <summary>
        /// Sets the <see cref="LogLevel"/> to use
        /// </summary>
        /// <param name="logLevel"></param>
        internal static void SetLogLevel(LogLevel logLevel)
        {
            LoggingLevel = logLevel;
        }

        /// <summary>
        /// Adds a message to the queue, to be written to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        private static void Write(string[] messages, LogLevel level)
        {
            // Only allow 1 thread at a time do these operations
            lock (_threadSync)
            {
                foreach (var message in messages)
                    LogStream.WriteLine(String.Format("{0}: [{2}] {1}", DateTime.Now, message, level));

                LogStream.Flush();
            }
        }

        /// <summary>
        /// Destructor. Make sure we flush!
        /// </summary>
        public static void Close()
        {
            try
            {
                LogStream?.Dispose();
            }
            catch (ObjectDisposedException) { } // Ignore
        }
    }
}
