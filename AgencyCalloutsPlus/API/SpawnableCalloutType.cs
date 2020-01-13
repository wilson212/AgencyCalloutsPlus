namespace AgencyCalloutsPlus.API
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