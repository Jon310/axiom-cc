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
    class Warrior : Styx.Helpers.Settings
    {
        public static Warrior Instance = new Warrior();
        public Warrior() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Warrior.xml"))) { }

        [Setting]
        [DefaultValue(false)]
        [Category("Common")]
        [DisplayName("Dark Command Always")]
        public bool UseDarkCommand { get; set; }
    }
}
