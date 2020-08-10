using Rage;

namespace AgencyCalloutsPlus.Mod.Conversation
{
    /// <summary>
    /// Represents a series of lines to display in a Subtitle
    /// </summary>
    public class LineSet : ISpawnable
    {
        /// <summary>
        /// 
        /// </summary>
        public int Probability { get; set; }

        /// <summary>
        /// Gets or sets the lines to display in the Subtitles
        /// </summary>
        public LineItem[] Lines { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to hide
        /// if this <see cref="LineSet"/> is displayed
        /// </summary>
        public string[] HidesMenuItems { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to unhide
        /// if this <see cref="LineSet"/> is displayed
        /// </summary>
        public string[] ShowMenuItems { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="LineSet"/> with the specified probability
        /// </summary>
        /// <param name="probability"></param>
        public LineSet(int probability)
        {
            Probability = probability;
        }

        /// <summary>
        /// Displays the response as a subtitle in game
        /// </summary>
        /// <param name="speaker"></param>
        public void Play(PedWrapper speaker)
        {
            foreach (var line in Lines)
            {
                Game.DisplaySubtitle($"~y~{speaker.Persona.Forename}~w~: {line.Text}", line.Time);
            }
        }
    }
}
