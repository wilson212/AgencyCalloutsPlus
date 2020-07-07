using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Mod.Conversation
{
    public class LineSet : ISpawnable
    {
        public int Probability { get; set; }

        public LineItem[] Lines { get; set; }

        public LineSet(int probability)
        {
            Probability = probability;
        }

        public void Play(PedWrapper speaker)
        {
            foreach (var line in Lines)
            {
                Game.DisplaySubtitle($"~y~{speaker.Persona.Forename}~w~: {line.Text}", line.Time);
            }
        }
    }
}
