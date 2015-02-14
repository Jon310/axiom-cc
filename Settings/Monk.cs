using System.ComponentModel;
using System.IO;
using Styx.Helpers;
using Styx.Common;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Axiom.Settings
{
    public class Monk : Styx.Helpers.Settings
    {
        public static Monk Instance = new Monk();
        public Monk() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Monk.xml"))) { }

        [Setting]
        [DefaultValue(false)]
        [Category("Common")]
        [DisplayName("Dark Command Always")]
        public bool UseDarkCommand { get; set; }




        #region General

        [Setting]
        [DefaultValue(10)]
        [Category("General")]
        [DisplayName("Mana Tea Percent")]
        public int ManaTea { get; set; }

        [Setting]
        [DefaultValue(DetoxBehaviour.OnCoolDown)]
        [Category("General")]
        [DisplayName("Detox Behavior")]
        public DetoxBehaviour Detox { get; set; }

        [Setting]
        [DefaultValue("")]
        [Category("General")]
        [DisplayName("Detox Buff Names")]
        public string DetoxBuff { get; set; }

        [Setting, DefaultValue(true), Category("General")]
        public bool PrioritizeTanks { get; set; }

        [Setting, DefaultValue(true), Category("General")]
        public bool PrioritizeSelf { get; set; }

        

        #endregion

        #region Defensive

        [Setting, DefaultValue(70), Category("Defensive")]
        public int HealthStone { get; set; }

        [Setting, DefaultValue(30), Category("General")]
        public int FortifyingBrew { get; set; }

        #endregion

        #region Healing Percentages

        [Setting, DefaultValue(100), Category("Healing Percentages")]
        public int RenewingMist { get; set; }

        [Setting, DefaultValue(70), Category("Healing Percentages")]
        public int SpinningCraneKick { get; set; }

        [Setting, DefaultValue(90), Category("Healing Percentages")]
        public int ChiWave { get; set; }

        [Setting, DefaultValue(80), Category("Healing Percentages")]
        public int Uplift { get; set; }

        [Setting, DefaultValue(30), Category("Healing Percentages")]
        public int LifeCocoon { get; set; }

        [Setting, DefaultValue(100), Category("Healing Percentages")]
        public int ZenSphere { get; set; }

        [Setting, DefaultValue(70), Category("Healing Percentages")]
        public int Revival { get; set; }

        [Setting, DefaultValue(90), Category("Healing Percentages")]
        public int EnvelopingMist{ get; set; }

        [Setting, DefaultValue(90), Category("Healing Percentages")]
        public int SoothingMist { get; set; }

        [Setting, DefaultValue(0), Category("Healing Percentages")]
        public int SurgingMist { get; set; }

        #endregion

        #region AOE Counts

        [Setting, DefaultValue(3), Category("AOE Counts")]
        public int SpinningCraneKickCount { get; set; }

        [Setting, DefaultValue(3), Category("AOE Counts")]
        public int ChiWaveCount { get; set; }

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
