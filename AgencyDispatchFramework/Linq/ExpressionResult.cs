using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A class that is used to describe the result of an expression string
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExpressionResult<T>
    {
        /// <summary>
        /// Gets the expression string used to fetch this <see cref="ExpressionResult{T}"/>
        /// </summary>
        public string ExpressionString { get; internal set; }

        /// <summary>
        /// Indicates whether the expression string parsed, compiled and 
        /// executed without error
        /// </summary>
        public bool Success { get; internal set; }

        /// <summary>
        /// Gets the <see cref="Exception"/> instance if the expression string
        /// failed to compile or execute successfully.
        /// </summary>
        public Exception InnerException { get; internal set; }

        /// <summary>
        /// Gets the value returned by the expression string
        /// </summary>
        public T Value { get; internal set; }

        /// <summary>
        /// Private constructor
        /// </summary>
        internal ExpressionResult()
        {

        }

        /// <summary>
        /// Logs the result in the Game.log file as an exception on failure,
        /// or a Debug entry on success
        /// </summary>
        public void LogResult()
        {
            // If we failed, log the exception
            if (!Success && InnerException != null)
            {
                var data = new Dictionary<string, string>
                {
                    { "Expression String", ExpressionString }
                };

                Log.Exception(InnerException, data);
            }
            else
            {
                // creates a messages array
                var lines = new[] 
                {
                    "ExpressionParser.Execute(): ",
                    $"\t\tExpression String: '{ExpressionString}'",
                    $"\t\tSuccess: '{Success}'",
                    $"\t\tResult: '{Value}'"
                };
                
                // Log messages using one single lock on the stream
                Log.Debug(lines);
            }
        }
    }
}
