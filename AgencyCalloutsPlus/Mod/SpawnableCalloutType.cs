using AgencyCalloutsPlus.API;

namespace AgencyCalloutsPlus.Mod
{
    internal class SpawnableCalloutType : ISpawnable
    {
        public int Probability { get; set; }

        public CalloutType CalloutType { get; set; }

        public SpawnableCalloutType(int probability, CalloutType calloutType)
        {
            Probability = probability;
            CalloutType = calloutType;
        }
    }
}