using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A class used to parse and evaluate string expressions into C# condition statements
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
        /// Creates a new instance of ExpressionParser
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
        /// <param name="input">The expression to be evaluated as a string</param>
        /// <returns>Returns a value of <typeparamref name="T"/>. 
        /// If expression failed, return the default value of <typeparamref name="T"/></returns>
        public T Evaluate<T>(string input)
        {
            // Ensure expression is not empty
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("expression input is null", nameof(input));
            }

            // Compile the expression
            Expression body = System.Linq.Dynamic.DynamicExpression.Parse(null, input, Symbols);
            LambdaExpression e = Expression.Lambda(body, Parameters.Values.ToArray());
            Delegate d = e.Compile();

            // Invoke the expression to recieve the output value
            var result = d.DynamicInvoke(Symbols.Values.ToArray());
            var expectedType = typeof(T);
            var resultType = result.GetType();

            // Ensure our return type is expected
            if (resultType != expectedType)
            {
                Log.Error($"ExpressionParser.Evaluate(): Expression does not return the expected type:");
                Log.Error($"\t\tExpression input: '{input}'");
                Log.Error($"\t\tExpected return type: '{expectedType.Name}'");
                Log.Error($"\t\tActual return type: '{resultType.Name}'");
                throw new InvalidCastException($"Unable to cast {resultType.Name} to {expectedType.Name}");
            }

            return (T)result;
        }

        /// <summary>
        /// Converts the string representation of a number to its 32-bit signed integer equivalent. 
        /// </summary>
        /// <param name="input">The expression to be evaluated as a string</param>
        /// <returns>A return value indicates whether the operation succeeded.</returns>
        public bool TryEvaluate<T>(string input, out T expressionResult)
        {
            // Set default
            expressionResult = default(T);

            // Try and evaluate the expression
            try
            {
                expressionResult = Evaluate<T>(input);
                return true;
            }
            catch (InvalidCastException)
            {
                // Logging is already done in that method
                return false;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
        }
    }
}
