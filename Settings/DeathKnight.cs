using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class DeathKnight : Styx.Helpers.Settings
    {
        public static DeathKnight Instance = new DeathKnight();
        public DeathKnight() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/DeathKnight.xml"))) { }

    }
}
