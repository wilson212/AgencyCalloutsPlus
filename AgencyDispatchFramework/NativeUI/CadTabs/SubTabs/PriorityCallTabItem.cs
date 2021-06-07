using AgencyDispatchFramework.Dispatching;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using RAGENativeUI.PauseMenu;
using System;
using System.Drawing;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// A submenu <see cref="TabItem"/> that represents a <see cref="PriorityCall"/>
    /// </summary>
    internal class PriorityCallTabItem : TabItem, IEquatable<PriorityCallTabItem>
    {
        /// <summary>
        /// Defines the spacing between headers (each row)
        /// </summary>
        const int LineSpaceBetweenHeaders = 90;

        /// <summary>
        /// Defines the spacing below a header to write the value text
        /// </summary>
        const int LineSpaceValueAfterHeader = 35;

        /// <summary>
        /// Defines the header text weight
        /// </summary>
        const float HeaderTextWeight = 0.55f;

        /// <summary>
        /// Defines the value text weight
        /// </summary>
        const float ValueTextWeight = 0.40f;

        /// <summary>
        /// Gets the <see cref="PriorityCall"/>
        /// </summary>
        public PriorityCall Call { get; private set; }

        /// <summary>
        /// Gets or sets the address of this call
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the dimensions of this instance
        /// </summary>
        public Size Dimensions { get; set; }

        /// <summary>
        /// Gets the parsed variable description
        /// </summary>
        private string Description { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="PriorityCallTabItem"/>
        /// </summary>
        /// <param name="call"></param>
        public PriorityCallTabItem(PriorityCall call) : base(call.IncidentText)
        {
            Call = call;
            Address = Call.Location.GetAddress();

            if (String.IsNullOrWhiteSpace(Address))
                Address = World.GetStreetName(Call.Location.Position);

            // @todo replace with Tokenizer
            Description = Call.Description.Text.ToUpperInvariant().Replace("$LOCATION$", Address);
        }

        /// <summary>
        /// Draws this instance
        /// </summary>
        public override void Draw()
        {
            FadeInWhenFocused = false;

            // Draw's background
            base.Draw();

            // define text alpha
            var a = ResText.Alignment.Left;
            int alpha = 255; // (Focused || !CanBeFocused) ? 255 : 150;
            Dimensions = new Size(BottomRight.SubtractPoints(TopLeft));

            // Grab (relative to screen resolution) starting points for 4 columns
            int col1p = GetXPointByPercentageOfWidth(0.07f);
            int col2p = GetXPointByPercentageOfWidth(0.30f);
            int col3p = GetXPointByPercentageOfWidth(0.54f);
            int col4p = GetXPointByPercentageOfWidth(0.79f);

            // ===
            // First Row
            // ===
            var row = 1;
            var headerY = 40 + (LineSpaceBetweenHeaders * (row - 1));
            var valueY = headerY + LineSpaceValueAfterHeader;

            // Draw 911 icon
            ResRectangle.Draw(this.SafeSize.AddPoints(new Point(col1p, headerY)), new Size(148, 148), Color.FromArgb(150, Color.Black));
            Sprite.Draw(Call.ScenarioInfo.CADSpriteName, Call.ScenarioInfo.CADSpriteTextureDict, this.SafeSize.AddPoints(new Point(col1p + 10, headerY + 10)), new Size(128, 128), 0.0f, Color.White, true);

            // Add callout call ID
            var headerLoc = SafeSize.AddPoints(new Point(col2p, headerY));
            var valueLoc = SafeSize.AddPoints(new Point(col2p, valueY));
            ResText.Draw("~b~Event ID", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~SA-{Call.CallId}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // Add call date
            headerLoc = SafeSize.AddPoints(new Point(col3p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col3p, valueY));
            ResText.Draw("~b~Call DateTime", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{Call.CallCreated}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // Add call source
            headerLoc = SafeSize.AddPoints(new Point(col4p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col4p, valueY));
            ResText.Draw("~b~Call Source", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{Call.Description.Source}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // ===
            // 2nd Row
            // ===
            row = 2;
            headerY = 43 + (LineSpaceBetweenHeaders * (row - 1));
            valueY = headerY + LineSpaceValueAfterHeader;

            // Add callout name header/value
            headerLoc = SafeSize.AddPoints(new Point(col2p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col2p, valueY));
            ResText.Draw("~b~Incident", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{Call.IncidentText}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // Add Priority
            headerLoc = SafeSize.AddPoints(new Point(col4p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col4p, valueY));
            ResText.Draw("~b~Priority", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{GetPriorityText(Call.Priority)}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // ===
            // 3rd Row
            // ===
            row = 3;
            headerY = 43 + (LineSpaceBetweenHeaders * (row - 1));
            valueY = headerY + LineSpaceValueAfterHeader;

            // Add callout location
            headerLoc = SafeSize.AddPoints(new Point(col1p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col1p, valueY));
            ResText.Draw("~y~Incident Location", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{Address}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // Add callout zone
            headerLoc = SafeSize.AddPoints(new Point(col3p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col3p, valueY));
            ResText.Draw("~y~Zone", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{Call.Zone.ScriptName}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // Add callout postal
            headerLoc = SafeSize.AddPoints(new Point(col4p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col4p, valueY));
            ResText.Draw("~y~Postal", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{Call.Location.Postal.Code}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // ===
            // 4th Row
            // ===
            row = 4;
            headerY = 43 + (LineSpaceBetweenHeaders * (row - 1));
            valueY = headerY + LineSpaceValueAfterHeader;

            // Add callout status
            headerLoc = SafeSize.AddPoints(new Point(col1p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col1p, valueY));
            ResText.Draw("~y~Call Status", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{Call.CallStatus}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // add Officer
            headerLoc = SafeSize.AddPoints(new Point(col2p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col2p, valueY));
            ResText.Draw("~y~Primary Officer", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{Call.PrimaryOfficer?.CallSign ?? "None"}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // add Agency
            var agency = Call.PrimaryOfficer != null ? Call.PrimaryOfficer.Agency.ScriptName : Call.Zone.PoliceAgencies[0].ScriptName;
            headerLoc = SafeSize.AddPoints(new Point(col3p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col3p, valueY));
            ResText.Draw("~y~Primary Agency", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"~w~{agency.ToUpperInvariant()}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // Add callout status
            var rt = GetResponseText(Call.ResponseCode);
            headerLoc = SafeSize.AddPoints(new Point(col4p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col4p, valueY));
            ResText.Draw("~y~Response Type", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(250, 0));
            ResText.Draw($"{rt}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, false);

            // ===
            // 5th Row
            // ===
            row = 5;
            headerY = 43 + (LineSpaceBetweenHeaders * (row - 1));
            valueY = headerY + LineSpaceValueAfterHeader;

            // Add description
            headerLoc = SafeSize.AddPoints(new Point(col1p, headerY));
            valueLoc = SafeSize.AddPoints(new Point(col1p, valueY));
            ResText.Draw("~o~Call Details", headerLoc, HeaderTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(col3p, 0));
            ResText.Draw($"~w~{Description}", valueLoc, ValueTextWeight, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletComprimeCologne, a, true, true, new Size(col4p, 0));
        }

        /// <summary>
        /// Converts a priority integer into a string
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>-
        private static string GetPriorityText(CallPriority priority)
        {
            switch ((int)priority)
            {
                case 1: return "1 - IMMEDIATE";
                case 2: return "2 - EMERGENCY";
                case 3: return "3 - EXPEDITED";
                default: return "4 - ROUTINE";
            }
        }

        /// <summary>
        /// Converts a response code integer into a string
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>-
        private static string GetResponseText(ResponseCode code)
        {
            switch (code)
            {
                default:
                case ResponseCode.Code1: return "~w~Code 1";
                case ResponseCode.Code2: return "~y~Code 2";
                case ResponseCode.Code3: return "~o~Code 3";
            }
        }

        private int GetXPointByPercentageOfWidth(float percent)
        {
            return (int)Math.Round(Dimensions.Width * percent, 0);
        }

        public override int GetHashCode()
        {
            return Call.CallId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PriorityCallTabItem);
        }

        public bool Equals(PriorityCallTabItem other)
        {
            if (other == null) return false;
            return other.Call.CallId == Call.CallId;
        }
    }
}
