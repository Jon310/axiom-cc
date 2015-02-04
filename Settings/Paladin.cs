using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Paladin : Styx.Helpers.Settings
    {
        public static Paladin Instance = new Paladin();
        public Paladin() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Paladin.xml"))) { }
    }
}
