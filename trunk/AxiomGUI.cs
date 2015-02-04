using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Axiom.Settings;
using Styx;

namespace Axiom
{
    public partial class AxiomGUI : Form
    {
        public AxiomGUI()
        {
            InitializeComponent();
        }
    }


    public class ClassSettings
    {
        public static Styx.Helpers.Settings Settings;
        public static void Initialize()
        {
            Settings = null;
            switch (Styx.StyxWoW.Me.Class)
            {
                case WoWClass.Paladin:
                    Settings = Paladin.Instance;
                    break;
                case WoWClass.Monk:
                    Settings = Monk.Instance;
                    break;
                case WoWClass.Druid:
                    Settings = Druid.Instance;
                    break;
                case WoWClass.Warlock:
                    Settings = Warlock.Instance;
                    break;
                case WoWClass.Rogue:
                    Settings = Rogue.Instance;
                    break;
                case WoWClass.Hunter:
                    Settings = Hunter.Instance;
                    break;
                case WoWClass.DeathKnight:
                    Settings = DeathKnight.Instance;
                    break;
                case WoWClass.Mage:
                    Settings = Mage.Instance;
                    break;
                case WoWClass.Priest:
                    Settings = Priest.Instance;
                    break;
                case WoWClass.Shaman:
                    Settings = Shaman.Instance;
                    break;
                case WoWClass.Warrior:
                    Settings = Warrior.Instance;
                    break;
                default:
                    break;
            }
            if (Settings != null)
                Settings.Load();
        }
    }
}
