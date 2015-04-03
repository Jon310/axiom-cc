using System;
using System.Windows.Forms;
using System.Windows.Media;
using Axiom.Helpers;
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


                if (PvPRotation)
                {
                    Log.ToastEnabled("PvP Enabled");
                    GeneralSettings.Instance.PvP = true;
                }
                else
                {
                    Log.ToastDisabled("PvP Disabled");
                    GeneralSettings.Instance.PvP = false;
                }

            });
            PvPRotation = false;

            HotkeysManager.Register("PvE Toggle",
            Keys.O,
            ModifierKeys.Alt,
            o =>
            {
                PvERotation = !PvERotation;
                Logging.Write("PvE enabled: " + PvERotation);

                if (PvERotation)
                    Log.ToastEnabled("PvE Enabled");
                else
                    Log.ToastDisabled("PvE Disabled");

            });
            PvERotation = false;

            HotkeysManager.Register("AFK Toggle",
            Keys.NumPad7,
            ModifierKeys.Control,
            o =>
            {
                AFK = !AFK;
                Logging.Write("AFK enabled: " + AFK);

                if (AFK)
                    Log.ToastEnabled("AFK Enabled");
                else
                    Log.ToastDisabled("AFK Disabled");

            });
            AFK = false;

            HotkeysManager.Register("Trace Toggle",
            Keys.NumPad8,
            ModifierKeys.Control,
            o =>
            {
                Trace = !Trace;
                Logging.Write("Trace enabled: " + Trace);

                if (Trace)
                    Log.ToastEnabled("Trace Enabled");
                else
                    Log.ToastDisabled("Trace Disabled");

            });
            Trace = false;

            HotkeysManager.Register("Burst Toggle",
            Keys.NumPad1,
            ModifierKeys.Control,
            o =>
            {
                Burst = !Burst;
                Logging.Write("Burst enabled: " + Burst);

                if (Burst)
                    Log.ToastEnabled("Burst Enabled");
                else
                    Log.ToastDisabled("Burst Disabled");

            });
            Burst = true;

            HotkeysManager.Register("AOE Toggle",
            Keys.NumPad4,
            ModifierKeys.Control,
            o =>
            {
                AOE = !AOE;
                Logging.Write("AOE enabled: " + AOE);

                if (AOE)
                    Log.ToastEnabled("AOE Enabled");
                else
                    Log.ToastDisabled("AOE Disabled");

            });
            AOE = true;

            HotkeysManager.Register("Weave Toggle",
            Keys.NumPad3,
            ModifierKeys.Control,
            o =>
            {
                Weave = !Weave;
                Logging.Write("Weave enabled: " + Weave);

                if (Weave)
                    Log.ToastEnabled("Weave Enabled");
                else
                    Log.ToastDisabled("Weave Disabled");

            });
            Weave = true;

            HotkeysManager.Register("Dispell Toggle",
            Keys.NumPad5,
            ModifierKeys.Control,
            o =>
            {
                Dispell = !Dispell;
                Logging.Write("Dispelling enabled: " + Dispell);

                if (Dispell)
                    Log.ToastEnabled("Dispelling Enabled");
                else
                    Log.ToastDisabled("Dispelling Disabled");

            });
            Dispell = true;

            HotkeysManager.Register("Heal All",
            Keys.H,
            ModifierKeys.Control,
            o =>
            {
                HealAll = !HealAll;
                Logging.Write("Heal All enabled: " + HealAll);

                if (HealAll)
                    Log.ToastEnabled("Heal All Enabled");
                else
                    Log.ToastDisabled("Heal All Disabled");

            });
            HealAll = false;

            HotkeysManager.Register("Movements",
            Keys.M,
            ModifierKeys.Control,
            o =>
            {
                Movements = !Movements;
                Logging.Write("Movement enabled: " + Movements);

                if (Movements)
                {
                    Log.ToastEnabled("Movement Enabled");
                    GeneralSettings.Instance.Movement = true;
                }
                else
                {
                    Log.ToastDisabled("Movement Disabled");
                    GeneralSettings.Instance.Movement = false;
                }

            });
            Movements = false;

            HotkeysManager.Register("Targeting",
            Keys.T,
            ModifierKeys.Control,
            o =>
            {
                Targeting = !Targeting;
                Logging.Write("Targeting enabled: " + Targeting);
                
                if (Targeting)
                {
                    Log.ToastEnabled("Targeting Enabled");
                    GeneralSettings.Instance.Targeting = true;
                }
                else
                {
                    Log.ToastDisabled("Targeting Disabled");
                    GeneralSettings.Instance.Targeting = false;
                }

            });
            Targeting = true;

            HotkeysManager.Register("Show Overlay",
            Keys.O,
            ModifierKeys.Control,
            o =>
            {
                ShowOverlay = !ShowOverlay;
                Logging.Write("Show Overlay enabled: " + ShowOverlay);

                if (ShowOverlay)
                    Log.ToastEnabled("Show Overlay Enabled");
                else
                    Log.ToastDisabled("Show Overlay Disabled");

            });
            ShowOverlay = false;
        }
    }
}
