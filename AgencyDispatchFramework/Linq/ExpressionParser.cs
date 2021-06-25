using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A class used to parse and compile expression strings into C# expressions that can be evaluated
    /// </summary>
    /// <seealso cref="https://github.com/zzzprojects/System.Linq.Dynamic/wiki/Dynamic-Expressions"/>
    public class ExpressionParser
    {
        /// <summary>
        /// Containts a hash table of ParameterName => ExpressionParameter
        /// </summary>
        private Dictionary<string, ParameterExpression> Parameters { get; set; }

        /// <summary>
        /// Contains a hash table of ParameterName => ParameterValue
        /// </summary>
        private Dictionary<string, object> Symbols { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ExpressionParser"/>
        /// </summary>
        public ExpressionParser()
        {
            Parameters = new Dictionary<string, ParameterExpression>();
            Symbols = new Dictionary<string, object>();
        }

        /// <summary>
        /// Sets a parameter name, value and value type that can be used 
        /// when parsing and evaluating the expression
        /// </summary>
        /// <typeparam name="T">Indicates the type of object the item is</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="item">The value of the parameter</param>
        public void SetParamater<T>(string name, T item)
        {
            var param = Expression.Parameter(typeof(T), name);
            if (Symbols.ContainsKey(name))
            {
                Symbols[name] = item;
                Parameters[name] = param;
            }
            else
            {
                Symbols.Add(name, item);
                Parameters.Add(name, param);
            }
        }

        /// <summary>
        /// Evaluates the expression and returns a value of <typeparamref name="T"/>
        /// </summary>
        /// <param name="expressionString">The expression string to be evaluated</param>
        /// <typeparam name="T">The expected <see cref="Type"/> returned by the expression string</typeparam>
        /// <returns>Returns a value of <typeparamref name="T"/>. 
        /// If expression failed, return the default value of <typeparamref name="T"/></returns>
        public ExpressionResult<T> Execute<T>(string expressionString)
        {
            try
            {
                // Ensure expression is not empty
                if (String.IsNullOrWhiteSpace(expressionString))
                {
                    throw new ArgumentException("expression string is null or empty", nameof(expressionString));
                }

                // Compile the expression
                Expression body = System.Linq.Dynamic.DynamicExpression.Parse(null, expressionString, Symbols);
                LambdaExpression e = Expression.Lambda(body, Parameters.Values.ToArray());
                Delegate d = e.Compile();

                // Invoke the expression to recieve the output value
                var result = d.DynamicInvoke(Symbols.Values.ToArray());

                // Cast on a seperate line, for better exception logging
                T expressionResult = (T)result;

                // If we are here, then we are good
                return new ExpressionResult<T>()
                {
                    Success = true,
                    ExpressionString = expressionString,
                    Value = expressionResult
                };
            }
            catch (Exception e)
            {
                // If we are here, then we are good
                return new ExpressionResult<T>()
                {
                    Success = false,
                    InnerException = e,
                    ExpressionString = expressionString,
                    Value = default(T)
                };
            }
        }
    }
}
