using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Mage : Styx.Helpers.Settings
    {
        public static Mage Instance = new Mage();
        public Mage() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Mage.xml"))) { }
    }
}
