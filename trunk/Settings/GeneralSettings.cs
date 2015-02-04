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
    class GeneralSettings : Styx.Helpers.Settings
    {
        public static GeneralSettings Instance = new GeneralSettings();

        public GeneralSettings() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/General.xml"))) { }

        [Setting, DefaultValue(false), Category("Behaviour")]
        public bool DisableTargeting { get; set; }

        [Setting, DefaultValue(true), Category("Behaviour")]
        public bool DisableMovement { get; set; }

        [Setting, DefaultValue(2)]
        public int AOEMobCount { get; set; }

        [Setting, DefaultValue(true)]
        public bool UseTrinket1 { get; set; }

        [Setting, DefaultValue(true)]
        public bool UseTrinket2 { get; set; }
        
        [Setting, DefaultValue(false), Category("Debugging")]
        public bool LogCanCastResults { get; set; }

        [Setting, DefaultValue(true), Category("Behaviour")]
        public bool AutoDismount { get; set; }

    }
}
