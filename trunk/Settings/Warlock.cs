using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Warlock : Styx.Helpers.Settings
    {
        public static Warlock Instance = new Warlock();
        public Warlock() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Warlock.xml"))) { }
    }
}
