using AgencyCalloutsPlus.API;

namespace AgencyCalloutsPlus
{
    internal class CalloutScenarioInfo : ISpawnable
    {
        public int Probability { get; set; }

        public string Name { get; set; }

        public string CalloutName { get; set; }

        public bool RespondCode3 { get; set; }

        public int Priority { get; set; }

        public LocationType LocationType { get; set; }

        public string ScannerText { get; set; }

        public string[] Descriptions { get; set; }
    }
}