﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.Common;

namespace Axiom.Settings
{
    class Rogue : Styx.Helpers.Settings
    {
        public static Rogue Instance = new Rogue();
        public Rogue() : base(Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings/Axiom/Rogue.xml"))) { }
    }
}
