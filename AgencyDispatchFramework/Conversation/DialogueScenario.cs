using System;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents scenario information uses a series of sensible <see cref="PedResponse"/>s 
    /// to be used in a <see cref="Dialogue"/> when talking with the player. 
    /// To be chosen at random using a <see cref="ProbabilityGenerator{T}"/>
    /// </summary>
    public class DialogueScenario : ISpawnable
    {
        /// <summary>
        /// Gets the probability of spawning this <see cref="DialogueScenario"/> in a
        /// <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public int Probability { get; private set; }

        /// <summary>
        /// Gets or sets the name of this <see cref="DialogueScenario"/>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the condition requirement to be evaluated. If the evaluation
        /// returns false, then this item is removed from the <see cref="ProbabilityGenerator{T}"/>
        /// as a possible scenario.
        /// </summary>
        public string ConditionStatement { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="DialogueScenario"/>
        /// </summary>
        /// <param name="probability"></param>
        public DialogueScenario(string id, int probability)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Probability = probability;
        }

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
