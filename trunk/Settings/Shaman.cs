using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Shaman : Styx.Helpers.Settings
    {
        public static Shaman Instance = new Shaman();
        public Shaman() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Shaman.xml"))) { }
    }
}
