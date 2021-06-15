using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using Gwen.Control;
using Rage;
using Rage.Forms;
using System;

namespace AgencyDispatchFramework.Integration
{
    public class CallDetailsGwenForm : GwenForm
    {
        private TextBox text_unit;
        private MultilineTextBox text_comments;
        private Label label11;
        private TextBox text_response;
        private TextBox text_status;
        private Label label9;
        private TextBox text_location;
        private Label label8;
        private TextBox text_incident;
        private TextBox text_datetime;
        private Label label7;
        private TextBox text_call_id;
        private Label label6;
        private Label label5;
        private Label label12;
        private Label label10;
        private TextBox text_event_id;
        private Label label1;
        private TextBox text_priority;
        private Label label2;
        private Label label3;
        private TextBox text_agency;
        private Label label4;
        private TextBox text_source;
        private Button btn_dispatch;

        public PriorityCall Call { get; set; }

        public CallDetailsGwenForm(PriorityCall call) : base(typeof(CallDetailsGwenFormTemplate))
        {
            Call = call;
        }

        public override void InitializeLayout()
        {
            // Create controls
            base.InitializeLayout();

            // Set window position and make it a modal
            this.Position = this.GetLaunchPosition();
            this.Window.DisableResizing();
            this.Window.MakeModal(true);

            // Disable inputs
            text_agency.KeyboardInputEnabled = false;
            text_call_id.KeyboardInputEnabled = false;
            text_comments.KeyboardInputEnabled = false;
            text_datetime.KeyboardInputEnabled = false;
            text_event_id.KeyboardInputEnabled = false;
            text_incident.KeyboardInputEnabled = false;
            text_location.KeyboardInputEnabled = false;
            text_priority.KeyboardInputEnabled = false;
            text_response.KeyboardInputEnabled = false;
            text_source.KeyboardInputEnabled = false;
            text_status.KeyboardInputEnabled = false;
            text_unit.KeyboardInputEnabled = false;

            // Set text fields
            var locationText = Call.Location.StreetName ?? World.GetStreetName(Call.Location.Position);
            text_call_id.Text = Guid.NewGuid().ToString();
            text_agency.Text = Dispatch.PlayerAgency.ScriptName.ToUpperInvariant();
            text_event_id.Text = Call.CallId.ToString();
            text_datetime.Text = Call.CallCreated.ToString();
            text_location.Text = locationText;
            text_incident.Text = Call.ScenarioInfo.IncidentText;
            text_priority.Text = GetPriorityText((int)Call.OriginalPriority);
            text_status.Text = Call.CallStatus.ToString();
            text_source.Text = "CITIZEN";
            text_response.Text = Call.ResponseCode == ResponseCode.Code3 ? "CODE 3" : "CODE 2";
            text_comments.Text = Call.Description.Text
                .Replace("{{location}}", locationText)
                .WordWrap(450, text_comments.Font.FaceName.ToString()
            );

            // Only if the call is assigned
            if (Call.PrimaryOfficer != null)
                text_unit.Text = Call.PrimaryOfficer.CallSign;

            // Register for events
            if (!Dispatch.CanInvokeAnyCalloutForPlayer())
                btn_dispatch.Disable();
            else
                btn_dispatch.Clicked += Btn_dispatch_Clicked;
        }

        /// <summary>
        /// Event fired when the player clicks the "Dispatch" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void Btn_dispatch_Clicked(Base sender, ClickedEventArgs arguments)
        {
            if (Dispatch.InvokeCallForPlayer(Call))
            {
                this.Window.Close();
            }
        }

        /// <summary>
        /// Converts a priority integer into a string
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        private string GetPriorityText(int priority)
        {
            switch (priority)
            {
                case 1: return "1 - IMMEDIATE";
                case 2: return "2 - EMERGENCY";
                case 3: return "3 - EXPEDITED";
                default: return "4 - ROUTINE";
            }
        }
    }
}
