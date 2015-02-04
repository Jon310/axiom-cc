using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Warrior : Styx.Helpers.Settings
    {
        public static Warrior Instance = new Warrior();
        public Warrior() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Warrior.xml"))) { }
    }
}
