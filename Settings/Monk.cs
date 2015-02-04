using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Monk : Styx.Helpers.Settings
    {
        public static Monk Instance = new Monk();
        public Monk() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Monk.xml"))) { }
    }
}
