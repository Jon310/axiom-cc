using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx;
using Styx.WoWInternals.WoWObjects;

namespace Axiom.Helpers
{
    class Item : Axiom
    {
        public static void UseContainerItem(string name)
        {
            var item = StyxWoW.Me.BagItems.FirstOrDefault(x => x.Name == name && x.Usable && x.Cooldown <= 0);
            if (item != null && item.CooldownTimeLeft.Equals(TimeSpan.Zero))
            {
                item.UseContainerItem();
            }
        }

        public static void UseContainerItem(string name, Func<bool> req)
        {
            if (req()) UseContainerItem(name);
        }

        public static void UseContainerItem(int id)
        {
            var item = StyxWoW.Me.BagItems.FirstOrDefault(x => x.Entry == id && x.Usable && x.Cooldown <= 0);
            if (item != null && item.CooldownTimeLeft.Equals(TimeSpan.Zero))
            {
                item.UseContainerItem();
            }

        }

        public static void UseContainerItem(int id, Func<bool> req)
        {
            if (req()) UseContainerItem(id);
        }
    }
}
