using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Dispatching.Assignments
{
    public class OutOfService : BaseAssignment
    {
        public OutOfService()
        {
            Description = "Out of Service";
            Priority = CallPriority.Immediate;
        }
    }
}
