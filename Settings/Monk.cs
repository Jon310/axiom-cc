using System.ComponentModel;
using System.IO;
using Styx.Helpers;
using Styx.Common;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Axiom.Settings
{
    public class Monk : Styx.Helpers.Settings
    {
        public static readonly Monk Instance = new Monk();
        private Monk() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Monk.xml"))) { }

        #region General

        [Setting, DefaultValue(10), Category("General")]
        public static int ManaTea { get; set; }

        [Setting, DefaultValue(DetoxBehaviour.OnCoolDown), Category("General")]
        public static DetoxBehaviour Detox { get; set; }

        [Setting, DefaultValue(""), Category("General")]
        public static string DetoxBuff { get; set; }

        [Setting, DefaultValue(true), Category("General")]
        public static bool PrioritizeTanks { get; set; }

        [Setting, DefaultValue(true), Category("General")]
        public static bool PrioritizeSelf { get; set; }

        

        #endregion

        #region Defensive

        [Setting, DefaultValue(70), Category("Defensive")]
        public static int HealthStone { get; set; }

        [Setting, DefaultValue(30), Category("General")]
        public static int FortifyingBrew { get; set; }

        #endregion


        #region Healing Percentages

        [Setting, DefaultValue(100), Category("Healing Percentages")]
        public static int RenewingMist { get; set; }

        [Setting, DefaultValue(70), Category("Healing Percentages")]
        public static int SpinningCraneKick { get; set; }

        [Setting, DefaultValue(90), Category("Healing Percentages")]
        public static int ChiWave { get; set; }

        [Setting, DefaultValue(80), Category("Healing Percentages")]
        public static int Uplift { get; set; }

        [Setting, DefaultValue(30), Category("Healing Percentages")]
        public static int LifeCocoon { get; set; }

        [Setting, DefaultValue(100), Category("Healing Percentages")]
        public static int ZenSphere { get; set; }

        [Setting, DefaultValue(70), Category("Healing Percentages")]
        public static int Revival { get; set; }

        [Setting, DefaultValue(90), Category("Healing Percentages")]
        public static int EnvelopingMist{ get; set; }

        [Setting, DefaultValue(90), Category("Healing Percentages")]
        public static int SoothingMist { get; set; }

        [Setting, DefaultValue(0), Category("Healing Percentages")]
        public static int SurgingMist { get; set; }

        #endregion


        #region AOE Counts

        [Setting, DefaultValue(3), Category("AOE Counts")]
        public static int SpinningCraneKickCount { get; set; }

        [Setting, DefaultValue(3), Category("AOE Counts")]
        public static int ChiWaveCount { get; set; }

        #endregion

        #region Enums
        public enum DetoxBehaviour
        {
            OnCoolDown,
            OnDebuff,
            Manually
        }
        #endregion
    }
}
