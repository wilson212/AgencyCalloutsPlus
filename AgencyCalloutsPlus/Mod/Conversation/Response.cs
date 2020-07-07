using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Mod.Conversation
{
    public sealed class Response
    {
        private SpawnGenerator<LineSet> Responses { get; set; }

        public string FromInputId { get; private set; }

        public string ReturnMenuId { get; private set; }

        /// <summary>
        /// Contains the selected response to the question. This does not
        /// change once selected!
        /// </summary>
        private LineSet SelectedResponse { get; set; }

        public Response(string fromInputId, string returnMenuId)
        {
            Responses = new SpawnGenerator<LineSet>();
            FromInputId = fromInputId;
            ReturnMenuId = returnMenuId;
        }

        internal void AddLineSet(LineSet set)
        {
            Responses.Add(set);
        }

        public LineSet GetResponseLineSet()
        {
            if (SelectedResponse == null)
            {
                SelectedResponse = Responses.Spawn();
            }

            return SelectedResponse;
        }
    }
}
