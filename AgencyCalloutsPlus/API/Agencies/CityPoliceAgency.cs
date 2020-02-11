using AgencyCalloutsPlus.CrimeGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
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
