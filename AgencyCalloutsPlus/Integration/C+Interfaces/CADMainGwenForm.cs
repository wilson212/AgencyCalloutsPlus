using AgencyCalloutsPlus.API;
using Gwen.Control;
using LSPD_First_Response.Mod.API;
using Rage;
using Rage.Forms;
using System;
using System.Drawing;

namespace AgencyCalloutsPlus.Integration
{
    internal class CADMainGwenForm : GwenForm
    {
        private ListBox list_prev_calls, list_active_calls;
        private Label lbl_c_unit, lbl_c_time, lbl_c_status, lbl_c_call;
        private Label lbl_ac_unit, lbl_ac_time, lbl_ac_status, lbl_ac_location, lbl_ac_priority, lbl_ac_type, lbl_ac_callnum;
        private Label lbl_a_id, lbl_a_time, lbl_a_call, lbl_a_loc, lbl_a_stat, lbl_a_unit, lbl_a_resp,
            lbl_a_desc, lbl_a_peds, lbl_a_vehs;
        private TextBox out_id, out_date, out_time, out_call, out_loc, out_stat, out_unit, out_resp;
        private MultilineTextBox out_desc, out_peds, out_vehs;
        private Base base_calls, base_active, base_active_calls;
        private TabControl tc_main;

        private Label label1;
        private Label lbl_status;
        private Label lbl_available;
        private Label label4;
        private Label lbl_officers;
        private Label label6;
        private Button btn_10_8;
        private Button btn_10_5;
        private Button btn_10_6;
        private Button btn_10_7;
        private Button btn_10_11;
        private Button btn_10_15;
        private Button btn_10_19;
        private Button btn_10_23;
        private Button btn_10_42;
        private Button btn_10_99;

        public ListBoxRow SelectedRow { get; private set; }

        internal GameFiber diag_callDetails;

        public CADMainGwenForm() : base(typeof(CADMainGwenFormTempplate))
        {
            
        }

        public override void InitializeLayout()
        {
            base.InitializeLayout();
            CreateComponents();
            this.Position = new Point(Game.Resolution.Width / 2 - this.Window.Width / 2, Game.Resolution.Height / 2 - this.Window.Height / 2);
            this.Window.DisableResizing();

            // Register for events
            btn_10_8.Clicked += Btn_10_8_Clicked;
            btn_10_8.SetToolTipText("Available For Calls");
            btn_10_7.Clicked += Btn_10_7_Clicked;
            btn_10_7.SetToolTipText("Out Of Service");
            btn_10_6.Clicked += Btn_10_6_Clicked;
            btn_10_6.SetToolTipText("Busy");
            btn_10_5.Clicked += Btn_10_5_Clicked;
            btn_10_5.SetToolTipText("Meal Break");
            btn_10_11.Clicked += Btn_10_11_Clicked;
            btn_10_11.SetToolTipText("On Traffic Stop");
            btn_10_15.Clicked += Btn_10_15_Clicked;
            btn_10_15.SetToolTipText("Returning To Station With Suspects");
            btn_10_19.Clicked += Btn_10_19_Clicked;
            btn_10_19.SetToolTipText("Returning To Station");
            btn_10_23.Clicked += Btn_10_23_Clicked;
            btn_10_23.SetToolTipText("On Scene");
            btn_10_42.Clicked += Btn_10_42_Clicked;
            btn_10_42.SetToolTipText("End Of Duty");
            btn_10_99.Clicked += Btn_10_99_Clicked;
            btn_10_99.SetToolTipText("Panic");
        }    

        public void CreateComponents()
        {
            /***** Main Tab Control *****/
            tc_main = new TabControl(this);
            tc_main.SetPosition(15, 120);
            tc_main.SetSize(760, 389);

            /***** Active Calls List Tab *****/
            // base container
            base_active_calls = new Base(this);
            base_active_calls.SetPosition(0, 0);
            base_active_calls.SetSize(753, 358);

            // calls listbox
            list_active_calls = new ListBox(base_active_calls);
            list_active_calls.SetPosition(0, 18);
            list_active_calls.SetSize(749, 333);

            // "Call ID" label
            lbl_ac_callnum = new Label(base_active_calls);
            lbl_ac_callnum.Text = "Call ID";
            lbl_ac_callnum.SetPosition(3, 1);
            lbl_ac_callnum.SetSize(30, 13);

            // "Call Type" label
            lbl_ac_type = new Label(base_active_calls);
            lbl_ac_type.Text = "Type";
            lbl_ac_type.SetPosition(73, 1);
            lbl_ac_type.SetSize(40, 13);

            // "Time" label
            lbl_ac_time = new Label(base_active_calls);
            lbl_ac_time.Text = "Time";
            lbl_ac_time.SetPosition(230, 1);
            lbl_ac_time.SetSize(40, 13);

            // "Priority" label
            lbl_ac_priority = new Label(base_active_calls);
            lbl_ac_priority.Text = "Priority";
            lbl_ac_priority.SetPosition(345, 1);
            lbl_ac_priority.SetSize(40, 13);

            // "Status" label
            lbl_ac_status = new Label(base_active_calls);
            lbl_ac_status.Text = "Status";
            lbl_ac_status.SetPosition(420, 1);
            lbl_ac_status.SetSize(40, 13);

            // "Unit" label
            lbl_ac_unit = new Label(base_active_calls);
            lbl_ac_unit.Text = "Assigned";
            lbl_ac_unit.SetPosition(545, 1);
            lbl_ac_unit.SetSize(40, 13);

            // "Location" label
            lbl_ac_location = new Label(base_active_calls);
            lbl_ac_location.Text = "Location";
            lbl_ac_location.SetPosition(648, 1);
            lbl_ac_location.SetSize(40, 13);

            /***** Previous Call List Tab *****/
            // base container
            base_calls = new Base(this);
            base_calls.SetPosition(0, 0);
            base_calls.SetSize(617, 358);

            // calls listbox
            list_prev_calls = new ListBox(base_calls);
            list_prev_calls.SetPosition(0, 18);
            list_prev_calls.SetSize(613, 333);

            // "Unit" label
            lbl_c_unit = new Label(base_calls);
            lbl_c_unit.Text = "Unit";
            lbl_c_unit.SetPosition(3, 1);
            lbl_c_unit.SetSize(26, 13);

            // "Time" label
            lbl_c_time = new Label(base_calls);
            lbl_c_time.Text = "Time";
            lbl_c_time.SetPosition(50, 1);
            lbl_c_time.SetSize(30, 13);

            // "Status" label
            lbl_c_status = new Label(base_calls);
            lbl_c_status.Text = "Status";
            lbl_c_status.SetPosition(120, 1);
            lbl_c_status.SetSize(37, 13);

            // "Call" label
            lbl_c_call = new Label(base_calls);
            lbl_c_call.Text = "Call";
            lbl_c_call.SetPosition(230, 1);
            lbl_c_call.SetSize(24, 13);

            /***** Active Call Tab *****/
            // base container
            base_active = new Base(this);
            base_active.SetPosition(0, 0);
            base_active.SetSize(617, 358);

            // "ID No." label
            lbl_a_id = new Label(base_active);
            lbl_a_id.Text = "ID No.";
            lbl_a_id.SetPosition(26, 6);
            lbl_a_id.SetSize(38, 13);
            // "ID No." textbox
            out_id = new TextBox(base_active);
            out_id.SetPosition(70, 3);
            out_id.SetSize(306, 20);
            out_id.KeyboardInputEnabled = false;

            // "Time" label
            lbl_a_time = new Label(base_active);
            lbl_a_time.Text = "Time";
            lbl_a_time.SetPosition(422, 7);
            lbl_a_time.SetSize(30, 13);
            // "Time" date textbox
            out_date = new TextBox(base_active);
            out_date.SetPosition(455, 3);
            out_date.SetSize(66, 20);
            out_date.KeyboardInputEnabled = false;
            // "Time" time textbox
            out_time = new TextBox(base_active);
            out_time.SetPosition(527, 3);
            out_time.SetSize(66, 20);
            out_time.KeyboardInputEnabled = false;

            // "Situation" label
            lbl_a_call = new Label(base_active);
            lbl_a_call.Text = "Situation";
            lbl_a_call.SetPosition(17, 33);
            lbl_a_call.SetSize(48, 13);
            // "Situation" textbox
            out_call = new TextBox(base_active);
            out_call.SetPosition(70, 29);
            out_call.SetSize(523, 20);
            out_call.KeyboardInputEnabled = false;

            // "Location" label
            lbl_a_loc = new Label(base_active);
            lbl_a_loc.Text = "Location";
            lbl_a_loc.SetPosition(18, 58);
            lbl_a_loc.SetSize(48, 13);
            // "Location" textbox
            out_loc = new TextBox(base_active);
            out_loc.SetPosition(70, 55);
            out_loc.SetSize(523, 20);
            out_loc.KeyboardInputEnabled = false;

            // "Status" label
            lbl_a_stat = new Label(base_active);
            lbl_a_stat.Text = "Status";
            lbl_a_stat.SetPosition(27, 84);
            lbl_a_stat.SetSize(37, 13);
            // "Status" textbox
            out_stat = new TextBox(base_active);
            out_stat.SetPosition(70, 81);
            out_stat.SetSize(106, 20);
            out_stat.KeyboardInputEnabled = false;

            // "Unit" label
            lbl_a_unit = new Label(base_active);
            lbl_a_unit.Text = "Unit";
            lbl_a_unit.SetPosition(258, 84);
            lbl_a_unit.SetSize(26, 13);
            // "Unit" textbox
            out_unit = new TextBox(base_active);
            out_unit.SetPosition(290, 81);
            out_unit.SetSize(86, 20);
            out_unit.KeyboardInputEnabled = false;

            // "Response" label
            lbl_a_resp = new Label(base_active);
            lbl_a_resp.Text = "Response";
            lbl_a_resp.SetPosition(454, 84);
            lbl_a_resp.SetSize(55, 13);
            // "Response" textbox
            out_resp = new TextBox(base_active);
            out_resp.SetPosition(514, 81);
            out_resp.SetSize(79, 20);
            out_resp.KeyboardInputEnabled = false;

            // "Comments" label
            lbl_a_desc = new Label(base_active);
            lbl_a_desc.Text = "Comments";
            lbl_a_desc.SetPosition(8, 113);
            lbl_a_desc.SetSize(56, 13);
            // "Comments" textbox
            out_desc = new MultilineTextBox(base_active);
            out_desc.SetPosition(70, 110);
            out_desc.SetSize(523, 103);
            out_desc.KeyboardInputEnabled = false;

            // "Persons" label
            lbl_a_peds = new Label(base_active);
            lbl_a_peds.Text = "Persons";
            lbl_a_peds.SetPosition(19, 226);
            lbl_a_peds.SetSize(45, 13);
            // "Persons" textbox
            out_peds = new MultilineTextBox(base_active);
            out_peds.SetPosition(70, 226);
            out_peds.SetSize(523, 57);
            out_peds.KeyboardInputEnabled = false;

            // "Vehicles" label
            lbl_a_vehs = new Label(base_active);
            lbl_a_vehs.Text = "Vehicles";
            lbl_a_vehs.SetPosition(19, 298);
            lbl_a_vehs.SetSize(47, 13);
            // "Vehicles" textbox
            out_vehs = new MultilineTextBox(base_active);
            out_vehs.SetPosition(70, 295);
            out_vehs.SetSize(523, 57);
            out_vehs.KeyboardInputEnabled = false;

            // Active Call tab is hidden when no callout is active
            if (Dispatch.DispatchedToPlayer != null)
            {
                tc_main.AddPage("Current Assignment", base_active);
            }
            else
            {
                base_active.Hide();
            }

            // Add tabs and their corresponding containers
            tc_main.AddPage("Active Call List", base_active_calls);
            for (int i = 1; i < 5; i++)
            {
                foreach (var call in Dispatch.GetCallList(i))
                {
                    var timeSpan = World.DateTime - call.CallCreated;
                    var row = list_active_calls.AddRow(
                        String.Format("{0}{1}{2}{3}{4}{5}{6}",
                            call.CallId.ToString().PadRight(12),
                            call.IncidentAbbreviation.PadRight(32),
                            timeSpan.ToString().PadRight(20),
                            call.Priority.ToString().PadRight(16),
                            call.CallStatus.ToString().PadRight(20),
                            call.PrimaryOfficer?.UnitString.PadRight(20) ?? " ".PadRight(25),
                            call.Zone.ScriptName
                        )
                    );

                    // Store call in the row
                    row.UserData = call;
                    row.DoubleClicked += Row_DoubleClicked;
                }
            }

            // Add call history page last
            tc_main.AddPage("Call History", base_calls);

            // Fill details
            int status = (int)Dispatch.GetPlayerStatus();
            lbl_officers.Text = $"{Dispatch.GetAvailableUnits()} / {Dispatch.PlayerAgency.ActualPatrols}";
            lbl_available.Text = Functions.IsPlayerAvailableForCalls() ? "Yes" : "No";
            lbl_status.Text = $"10-{status}";
        }

        /// <summary>
        /// Event fired when a call is double clicked in the Active Call List tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        private void Row_DoubleClicked(Base sender, ClickedEventArgs arguments)
        {
            SelectedRow = (ListBoxRow)sender;
            diag_callDetails = new GameFiber(OpenCallDetailsDialog);
            diag_callDetails.Start();
        }

        private void OpenCallDetailsDialog()
        {
            PriorityCall call = (PriorityCall)SelectedRow.UserData;
            GwenForm help = new CallDetailsGwenForm(call);
            help.Show();
            while (help.Window.IsVisible)
            {
                GameFiber.Yield();
            }
        }

        #region Button Click Events

        private void Btn_10_5_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-5";
            Dispatch.SetPlayerStatus(OfficerStatus.MealBreak);
        }

        private void Btn_10_6_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-6";
            Dispatch.SetPlayerStatus(OfficerStatus.Busy);
        }

        private void Btn_10_7_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-7";
            Dispatch.SetPlayerStatus(OfficerStatus.OutOfService);
        }

        private void Btn_10_8_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "Yes";
            lbl_status.Text = "10-8";
            Dispatch.SetPlayerStatus(OfficerStatus.Available);
        }

        private void Btn_10_11_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-11";
            Dispatch.SetPlayerStatus(OfficerStatus.OnTrafficStop);
        }

        private void Btn_10_15_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-15";
            Dispatch.SetPlayerStatus(OfficerStatus.ReturningToStationWithSuspect);
        }

        private void Btn_10_19_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-19";
            Dispatch.SetPlayerStatus(OfficerStatus.ReturningToStation);
        }

        private void Btn_10_23_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-23";
            Dispatch.SetPlayerStatus(OfficerStatus.OnScene);
        }

        private void Btn_10_42_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-42";
            Dispatch.SetPlayerStatus(OfficerStatus.EndingDuty);
        }

        private void Btn_10_99_Clicked(Base sender, ClickedEventArgs arguments)
        {
            lbl_available.Text = "No";
            lbl_status.Text = "10-99";

            // todo - add panic 
            Dispatch.SetPlayerStatus(OfficerStatus.Panic);
        }

        #endregion

        #region Properties



        #endregion
    }
}