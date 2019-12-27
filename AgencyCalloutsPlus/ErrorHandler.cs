using Rage;
using System;
using System.Diagnostics;
using System.IO;

namespace AgencyCalloutsPlus
{
    internal static class ErrorHandler
    {
        public static void HandleError(string message)
        {
            Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: {message}");
        }

        public static void HandleException(Exception e)
        {
            // Show generic message
            Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: A handled exception has been logged in 'Plugins/LSPDFR/AgencyCalloutsPlus/Errors.log'");

            // log exception
            string file = Path.Combine(Main.PluginFolderPath, "Errors.log");
            LogException(file, e);
        }

        /// <summary>
        /// Generates a trace log for an exception. If an exception is thrown here, The error
        /// will automatically be logged in the programs error log
        /// </summary>
        /// <param name="fileName">The tracelog filepath (Must not exist yet)</param>
        /// <param name="exception">The exception to log</param>
        private static void LogException(string fileName, Exception exception)
        {
            // Try to write to the log
            try
            {
                // Generate the tracelog
                using (StreamWriter Log = new StreamWriter(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write)))
                {
                    // Write the header data
                    Log.WriteLine("-------- AgencyCalloutsPlus Exception Trace Entry --------");
                    Log.WriteLine("Exception Date: " + DateTime.Now.ToString());
                    Log.WriteLine("Os Version: " + Environment.OSVersion.VersionString);
                    Log.WriteLine("Architecture: " + ((Environment.Is64BitOperatingSystem) ? "x64" : "x86"));
                    Log.WriteLine();
                    Log.WriteLine("-------- Exception --------");

                    // Log each exception
                    int i = 0;
                    while (true)
                    {
                        // Create a stack trace
                        StackTrace trace = new StackTrace(exception, true);
                        StackFrame frame = trace.GetFrame(0);

                        // Log the current exception
                        Log.WriteLine("Type: " + exception.GetType().FullName);
                        Log.WriteLine("Message: " + exception.Message.Replace("\n", "\n\t"));
                        Log.WriteLine("Target Method: " + frame.GetMethod().Name);
                        Log.WriteLine("File: " + frame.GetFileName());
                        Log.WriteLine("Line: " + frame.GetFileLineNumber());
                        Log.WriteLine("StackTrace:");
                        Log.WriteLine(exception.StackTrace.TrimEnd());

                        // If we have no more inner exceptions, end the logging
                        if (exception.InnerException == null)
                            break;

                        // Prepare next inner exception data
                        Log.WriteLine();
                        Log.WriteLine("-------- Inner Exception ({0}) --------", i++);
                        exception = exception.InnerException;
                    }

                    Log.Flush();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
