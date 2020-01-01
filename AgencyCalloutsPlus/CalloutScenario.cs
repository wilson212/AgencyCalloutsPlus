using System.Xml;

namespace AgencyCalloutsPlus
{
    internal class CalloutScenario : ISpawnable
    {
        public int Probability { get; set; }

        public string Name { get; set; }

        public bool RespondCode3 { get; set; }
    }
}