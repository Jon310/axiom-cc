using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Priest : Styx.Helpers.Settings
    {
        public static Priest Instance = new Priest();
        public Priest() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Priest.xml"))) { }
    }
}
