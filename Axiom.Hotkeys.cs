using System;
using System.Windows.Forms;
using System.Windows.Media;
using Axiom.Settings;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.WoWInternals;

namespace Axiom
{
    partial class Axiom
    {
        public static bool PvPRotation { get; set; }
        public static bool PvERotation { get; set; }
        public static bool AFK { get; set; }
        public static bool Trace { get; set; }
        public static bool Burst { get; set; }
        public static bool AOE { get; set; }
        public static bool Weave { get; set; }
        public static bool Dispell { get; set; }
        public static bool HealAll { get; set; }
        public static bool Movements { get; set; }
        public static bool Targeting { get; set; }

        public static bool ShowOverlay { get; set; }

        private static void UnregisterHotkeys()
        {
            HotkeysManager.Unregister("PvP Toggle");
            HotkeysManager.Unregister("PvE Toggle");
            HotkeysManager.Unregister("AFK Toggle");
            HotkeysManager.Unregister("Trace Toggle");
            HotkeysManager.Unregister("Burst Toggle");
            HotkeysManager.Unregister("AOE Toggle");
            HotkeysManager.Unregister("Weave Toggle");
            HotkeysManager.Unregister("Dispell Toggle");
            HotkeysManager.Unregister("Heal All");
            HotkeysManager.Unregister("Movements");
            HotkeysManager.Unregister("Targeting");
            HotkeysManager.Unregister("Show Overlay");
        }

        private static void RegisterHotkeys()
        {
            HotkeysManager.Register("PvP Toggle",
            Keys.P,
            ModifierKeys.Alt,
            o =>
            {
                PvPRotation = !PvPRotation;
                Logging.Write("PvP enabled: " + PvPRotation);
                GeneralSettings.Instance.PvP = PvPRotation;
                StyxWoW.Overlay.AddToast(() => string.Format("PvP Enabled: " + PvPRotation + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('PvP Enabled: " + PvPRotation + "')");
            });
            PvPRotation = false;

            HotkeysManager.Register("PvE Toggle",
            Keys.O,
            ModifierKeys.Alt,
            o =>
            {
                PvERotation = !PvERotation;
                Logging.Write("PvE enabled: " + PvERotation);
                StyxWoW.Overlay.AddToast(() => string.Format("PvE Enabled: " + PvERotation + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('PvE Enabled: " + PvERotation + "')");
            });
            PvERotation = false;

            HotkeysManager.Register("AFK Toggle",
            Keys.NumPad7,
            ModifierKeys.Control,
            o =>
            {
                AFK = !AFK;
                Logging.Write("AFK enabled: " + AFK);
                StyxWoW.Overlay.AddToast(() => string.Format("AFK Enabled: " + AFK + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('AFK Enabled: " + AFK + "')");
            });
            AFK = false;

            HotkeysManager.Register("Trace Toggle",
            Keys.NumPad8,
            ModifierKeys.Control,
            o =>
            {
                Trace = !Trace;
                Logging.Write("Trace enabled: " + Trace);
                StyxWoW.Overlay.AddToast(() => string.Format("Trace Enabled: " + Trace + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('Trace Enabled: " + Trace + "')");
            });
            Trace = false;

            HotkeysManager.Register("Burst Toggle",
            Keys.NumPad1,
            ModifierKeys.Control,
            o =>
            {
                Burst = !Burst;
                Logging.Write("Burst enabled: " + Burst); 
                StyxWoW.Overlay.AddToast(() => string.Format("Burst Enabled: " + Burst + ""), 
                                               TimeSpan.FromSeconds(1.5), 
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black, 
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('Burst Enabled: " + Burst + "')");
            });
            Burst = true;

            HotkeysManager.Register("AOE Toggle",
            Keys.NumPad4,
            ModifierKeys.Control,
            o =>
            {
                AOE = !AOE;
                Logging.Write("AOE enabled: " + AOE);
                StyxWoW.Overlay.AddToast(() => string.Format("AOE Enabled: " + AOE + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('AOE Enabled: " + AOE + "')");
            });
            AOE = true;

            HotkeysManager.Register("Weave Toggle",
            Keys.NumPad3,
            ModifierKeys.Control,
            o =>
            {
                Weave = !Weave;
                Logging.Write("Weave enabled: " + Weave);
                StyxWoW.Overlay.AddToast(() => string.Format("Weave Enabled: " + Weave + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('Weave Enabled: " + Weave + "')");
            });
            Weave = true;

            HotkeysManager.Register("Dispell Toggle",
            Keys.NumPad5,
            ModifierKeys.Control,
            o =>
            {
                Dispell = !Dispell;
                Logging.Write("Dispelling enabled: " + Dispell);
                StyxWoW.Overlay.AddToast(() => string.Format("Dispelling Enabled: " + Dispell + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('Dispelling Enabled: " + Dispell + "')");
            });
            Dispell = true;

            HotkeysManager.Register("Heal All",
            Keys.H,
            ModifierKeys.Control,
            o =>
            {
                HealAll = !HealAll;
                Logging.Write("Heal All enabled: " + HealAll);
                StyxWoW.Overlay.AddToast(() => string.Format("Heal All Enabled: " + HealAll + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('Heal All Enabled: " + HealAll + "')");
            });
            HealAll = false;

            HotkeysManager.Register("Movements",
            Keys.M,
            ModifierKeys.Control,
            o =>
            {
                Movements = !Movements;
                Logging.Write("Movement enabled: " + Movements);
                GeneralSettings.Instance.Movement = Movements;
                StyxWoW.Overlay.AddToast(() => string.Format("Movement Enabled: " + Movements + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('Movement Enabled: " + Movements + "')");
            });
            Movements = false;

            HotkeysManager.Register("Targeting",
            Keys.T,
            ModifierKeys.Control,
            o =>
            {
                Targeting = !Targeting;
                Logging.Write("Targeting enabled: " + Targeting);
                GeneralSettings.Instance.Targeting = Targeting;
                StyxWoW.Overlay.AddToast(() => string.Format("Targeting Enabled: " + Targeting + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('Targeting Enabled: " + Targeting + "')");
            });
            Targeting = true;

            HotkeysManager.Register("Show Overlay",
            Keys.O,
            ModifierKeys.Control,
            o =>
            {
                ShowOverlay = !ShowOverlay;
                Logging.Write("Show Overlay enabled: " + ShowOverlay);
                StyxWoW.Overlay.AddToast(() => string.Format("Show Overlay Enabled: " + ShowOverlay + ""),
                                               TimeSpan.FromSeconds(1.5),
                                               // Foreground Color, Background Color
                                               Colors.DarkOrange, Colors.Black,
                                               new FontFamily("Segoe UI"));
                // Chat Output a big no no right now. Toast Overlays recomended
                //Lua.DoString("print('Show Overlay Enabled: " + ShowOverlay + "')");
            });
            ShowOverlay = false;
        }
    }
}
