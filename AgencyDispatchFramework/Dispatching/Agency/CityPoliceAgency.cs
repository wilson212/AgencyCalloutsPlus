namespace AgencyDispatchFramework.Dispatching
{
    public class CityPoliceAgency : Agency
    {
        internal CityPoliceAgency(string scriptName, string friendlyName, StaffLevel staffLevel) 
            : base(scriptName, friendlyName, staffLevel)
        {
            
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
