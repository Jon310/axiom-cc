using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Axiom.Helpers;
using Axiom.Settings;
using Styx;

namespace Axiom
{
    public partial class AxiomGUI : Form
    {
        #region Form Dragging API Support
        //The SendMessage function sends a message to a window or windows.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        //ReleaseCapture releases a mouse capture
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern bool ReleaseCapture();
        #endregion

        public AxiomGUI()
        {
            InitializeComponent();
        }

        private void Overlay_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xa1, 0x2, 0);
            }
        }

        private void On_Load(object sender, EventArgs e)
        {
            ClassSettings.Initialize();
            propertyGrid1.SelectedObject = ClassSettings.Settings;
            propertyGrid2.SelectedObject = GeneralSettings.Instance;
        }

        private void On_Exit(object sender, EventArgs e)
        {
            Log.WriteLog("Saving Settings");
            ClassSettings.Settings.Save();
            ClassSettings.Initialize();
            GeneralSettings.Instance.Save();
            this.Close();
        }

        private void saveExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            On_Exit(sender, e);
        }

        private void openOverlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Axiom.ShowOverlay = true;
            Overlay.ShowOverlay();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            On_Exit(sender, e);
        }

        private void dumpSpellsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Lists.SpellLists.SpellDump();
        }
    }


    public static class ClassSettings
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
            }
            if (Settings != null)
                Settings.Load();
        }
    }
}
