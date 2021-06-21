using System;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a conversation flow outcome for a scenario
    /// </summary>
    public class FlowOutcome : ISpawnable
    {
        /// <summary>
        /// Gets the probability of spawning this <see cref="FlowOutcome"/>
        /// </summary>
        public int Probability { get; set; }

        /// <summary>
        /// Gets or sets the name of this <see cref="FlowOutcome"/>
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
            if (String.IsNullOrWhiteSpace(ConditionStatement))
                return true;

            return parser.Evaluate<bool>(ConditionStatement);
        }
    }
}
