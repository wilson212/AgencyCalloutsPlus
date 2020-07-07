using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AgencyCalloutsPlus.Mod
{
    /// <summary>
    /// A call used to parse string expressions into bool values
    /// </summary>
    public class ExpressionParser
    {
        private Dictionary<string, ParameterExpression> Parameters { get; set; }

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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="item"></param>
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
        /// Evaluates the expression and returns the boolean value
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool Evaluate(string input)
        {
            try
            {
                Expression body = System.Linq.Dynamic.DynamicExpression.Parse(null, input, Symbols);
                LambdaExpression e = Expression.Lambda(body, Parameters.Values.ToArray());
                Delegate d = e.Compile();

                var result = d.DynamicInvoke(Symbols.Values.ToArray());
                if (!(result is bool))
                {
                    Log.Error($"Expression does not return a bool '{input}'");
                    return false;
                }

                // Debugging!
                //Log.Debug($"ExpressionParser.Evaluate(): Result for \"{input}\" was {result}");

                return (bool)result;
            }
            catch (Exception e)
            {
                Log.Error($"Exception thrown while trying to parse expression '{input}'");
                Log.Exception(e);
                return false;
            }
        }
    }
}
