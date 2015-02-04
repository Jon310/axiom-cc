using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Druid : Styx.Helpers.Settings
    {
        public static Druid Instance = new Druid();
        public Druid() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Druid.xml"))) { }
    }
}
