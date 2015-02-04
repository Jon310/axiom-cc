using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Hunter : Styx.Helpers.Settings
    {
        public static Hunter Instance = new Hunter();
        public Hunter() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Hunter.xml"))) { }
    }
}
