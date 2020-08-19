using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Mod.UI;
using Rage;
using System.Collections.Generic;
using System.Drawing;

namespace AgencyCalloutsPlus.Mod
{
    /// <summary>
    /// Contains methods to manipulate the Agency Dispatch HUD above the minimap
    /// </summary>
    internal static class StatusHUD
    {
        public static bool IsDisplaying { get; private set; } = false;

        private static ContainerElement HudElement { get; set; }

        private static Dictionary<TextElementId, TextElement> Elements { get; set; }

        static StatusHUD()
        {
            Elements = new Dictionary<TextElementId, TextElement>();
        }

        /// <summary>
        /// Brings up the display to be shown
        /// </summary>
        public static void Show()
        {
            if (IsDisplaying) return;

            // Calculate start point for text
            var point = new PointF(Settings.HudPositionX, Settings.HudPositionY);
            HudElement = new ContainerElement(point, new SizeF(500, 250));
            Elements.Clear();
            
            // Show status text
            var text = new TextElement("10-8: ", new PointF(1, 1))
            {
                Size = (Settings.HudTextScale * 0.3f),
                Font = GameFont.ChaletLondon,
                Outline = true,
                Shadow = true,
                Color = Color.WhiteSmoke
            };

            // Add text element
            HudElement.Items.Add(text);
            Elements.Add(TextElementId.StatusCode, text);

            // Show status available
            point = new PointF(65, 1);
            text = new TextElement("Available", point)
            {
                Size = (Settings.HudTextScale * 0.3f),
                Font = GameFont.ChaletLondon,
                Outline = true,
                Shadow = true,
                Color = Color.Green
            };

            // Add text element
            HudElement.Items.Add(text);
            Elements.Add(TextElementId.StatusText, text);

            // Show status available
            point = new PointF(175, 1);
            text = new TextElement("Priority Calls: ", point)
            {
                Size = (Settings.HudTextScale * 0.3f),
                Font = GameFont.ChaletLondon,
                Outline = true,
                Shadow = true,
                Color = Color.WhiteSmoke
            };

            // Add text element
            HudElement.Items.Add(text);

            // Show status available
            point = new PointF(275, 1);
            text = new TextElement("2", point)
            {
                Size = (Settings.HudTextScale * 0.3f),
                Font = GameFont.ChaletLondon,
                Outline = true,
                Shadow = true,
                Color = Color.WhiteSmoke
            };

            // Add text element
            HudElement.Items.Add(text);
            Elements.Add(TextElementId.PriorityCalls, text);

            // Tell Rage to draw hud ever tick
            Game.FrameRender += RenderDisplay;
            IsDisplaying = true;
        }

        /// <summary>
        /// Hides the display
        /// </summary>
        public static void Hide()
        {
            if (!IsDisplaying) return;

            Game.FrameRender -= RenderDisplay;
            IsDisplaying = false;
        }

        /// <summary>
        /// Main render event
        /// </summary>
        private static void RenderDisplay(object sender, GraphicsEventArgs e)
        {
            if (Game.IsPaused || Game.IsLoading || !Main.OnDuty) return;

            UpdateElements();

            HudElement.Draw();
        }

        private static void UpdateElements()
        {
            // Get updated Status
            GetStatus(out string code, out string desc, out Color color);

            var text = Elements[TextElementId.StatusCode];
            text.Caption = code;

            text = Elements[TextElementId.StatusText];
            text.Caption = desc;
            text.Color = color;
        }

        private static void GetStatus(out string code, out string text, out Color color)
        {
            var call = Dispatch.PlayerActiveCall;
            var status = Dispatch.GetPlayerStatus();
            code = string.Concat("10-", (int)status, ": ");

            switch (status)
            {
                case OfficerStatus.Busy:
                    text = "Busy";
                    color = Color.Aqua;
                    break;
                case OfficerStatus.Dispatched:
                    text = "Dispatched";
                    switch (call?.ResponseCode ?? 1)
                    {
                        default: // No hurry
                            color = Color.DodgerBlue;
                            break;
                        case 2:
                            color = Color.Orange;
                            break;
                        case 3:
                            color = Color.Red;
                            break;
                    }
                    break;
                case OfficerStatus.MealBreak:
                    text = "On Break";
                    color = Color.Aqua;
                    break;
                case OfficerStatus.OnScene:
                    text = "On Scene";
                    color = Color.Red;
                    break;
                case OfficerStatus.OnTrafficStop:
                    text = "Traffic Stop";
                    color = Color.DodgerBlue;
                    break;
                case OfficerStatus.OutOfService:
                    text = "Out of Service";
                    color = Color.DarkGray;
                    break;
                case OfficerStatus.Panic:
                    text = "PANIC";
                    color = Color.Red;
                    break;
                case OfficerStatus.ReturningToStation:
                    text = "Returning To Station";
                    color = Color.Aqua;
                    break;
                case OfficerStatus.ReturningToStationWithSuspect:
                    text = "Suspect Transport";
                    color = Color.Orange;
                    break;
                default:
                    text = "Availble";
                    color = Color.Green;
                    break;
            }
        }

        private enum TextElementId
        {
            StatusCode,
            StatusText,
            PriorityCalls
        }
    }
}
