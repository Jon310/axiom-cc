using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;
using Styx.Helpers;
using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Axiom.Settings
{
    class Shaman : Styx.Helpers.Settings
    {
        public static Shaman Instance = new Shaman();
        public Shaman() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Shaman.xml"))) { }

        #region Healing Percentages

        [Setting]
        [DefaultValue(60)]
        [Category("Healing Percentages")]
        [DisplayName("% Healing Wave")]
        [Description("Health % to cast this ability at. Set to 0 to disable.")]
        public int HealingWave { get; set; }

        [Setting]
        [DefaultValue(16)]
        [Category("Healing Percentages")]
        [DisplayName("% Healing Surge")]
        [Description("Health % to cast this ability at. Set to 0 to disable.")]
        public int HealingSurge { get; set; }

        [Setting]
        [DefaultValue(48)]
        [Category("Healing Percentages")]
        [DisplayName("% Spirit Link Totem")]
        [Description("Health % to cast this ability at.  Only valid in a group. Set to 0 to disable.")]
        public int SpiritLinkTotem { get; set; }

        [Setting]
        [DefaultValue(15)]
        [Category("Healing Percentages")]
        [DisplayName("% Oh Shoot!")]
        [Description("Health % to cast Oh Shoot Heal (Ancestral Swiftness + Healing Wave).  Disabled if set to 0, on cooldown, or talent not selected.")]
        public int AncestralSwiftness { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Healing Percentages")]
        [DisplayName("Healing Tide Totem %")]
        [Description("Health % to cast this ability at. Set to 0 to disable.")]
        public int HealingTideTotem { get; set; }

        [Setting]
        [DefaultValue(95)]
        [Category("Healing Percentages")]
        [DisplayName("% Healing Stream Totem")]
        [Description("Health % to cast this ability at. Set to 0 to disable.")]
        public int HealingStreamTotem { get; set; }

        [Setting]
        [DefaultValue(91)]
        [Category("Healing Percentages")]
        [DisplayName("% Healing Rain")]
        [Description("Health % to cast this ability at. Must heal Min of 3 people in party, 4 in a raid. Set to 0 to disable.")]
        public int HealingRain { get; set; }

        [Setting]
        [DefaultValue(92)]
        [Category("Healing Percentages")]
        [DisplayName("% Chain Heal")]
        [Description("Health % to cast this ability at. Must heal Min 2 people in party, 3 in a raid. Set to 0 to disable.")]
        public int ChainHeal { get; set; }

        [Setting]
        [DefaultValue(45)]
        [Category("Restoration")]
        [DisplayName("% Ascendance")]
        [Description("Health % to cast this ability at. Set to 0 to disable.")]
        public int Ascendance { get; set; }

        #endregion

        #region HealingCounts

        [Setting]
        [DefaultValue(1)]
        [Category("Healing Counts")]
        [DisplayName("Spirit Link Min Count")]
        [Description("Min number of players healed")]
        public int MinSpiritLinkCount { get; set; }

        [Setting]
        [DefaultValue(4)]
        [Category("Healing Counts")]
        [DisplayName("Healing Tide Min Count")]
        [Description("Min number of players healed")]
        public int MinHealingTideCount { get; set; }

        [Setting]
        [DefaultValue(4)]
        [Category("Healing Counts")]
        [DisplayName("Healing Rain Min Count")]
        [Description("Min number of players below Healing Rain % in area")]
        public int MinHealingRainCount { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Restoration")]
        [DisplayName("Chain Heal Min Count")]
        [Description("Min number of players healed")]
        public int MinChainHealCount { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Restoration")]
        [DisplayName("Ascendance Min Count")]
        [Description("Min number of players healed")]
        public int MinAscendanceCount { get; set; }

        [Setting]
        [DefaultValue(4)]
        [Category("Restoration")]
        [DisplayName("Roll Riptide Max Count")]
        [Description("Max number of players to roll Riptide on (always Roll on tanks, and tanks are included in count)")]
        public int RollRiptideCount { get; set; }

        #endregion

        #region General Settings

        [Setting]
        [DefaultValue(true)]
        [Category("General Settings")]
        [DisplayName("Use Ascendance")]
        [Description("True: Automatically cast Ascendance as needed, False: never cast Ascendance (left for User Control)")]
        public bool UseAscendance { get; set; }

        #endregion
    }
}
