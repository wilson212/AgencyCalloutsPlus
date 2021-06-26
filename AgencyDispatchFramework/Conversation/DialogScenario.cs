using System;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a scenario for a series of <see cref="PedResponse"/>s
    /// </summary>
    public class DialogScenario : ISpawnable
    {
        /// <summary>
        /// Gets the probability of spawning this <see cref="DialogScenario"/>
        /// </summary>
        public int Probability { get; set; } = 1;

        /// <summary>
        /// Gets or sets the name of this <see cref="DialogScenario"/>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the condition requirement to be evaluated. If the evaluation
        /// returns false, then this item is removed from the <see cref="ProbabilityGenerator{T}"/>
        /// as a possible outcome.
        /// </summary>
        public string ConditionStatement { get; set; }

        /// <summary>
        /// Evaluates the <see cref="ConditionStatement"/> and returns true or false
        /// </summary>
        /// <param name="parser"></param>
        /// <returns></returns>
        public bool Evaluate(ExpressionParser parser)
        {
            // If the condition statement is empty, just return true then
            if (String.IsNullOrWhiteSpace(ConditionStatement))
            {
                return true;
            }

            // Execute the condition statement
            var result = parser.Execute<bool>(ConditionStatement);
            if (result.Success)
            {
                return result.Value;
            }
            else
            {
                result.LogResult();
                return false;
            }
        }
    }
}
