using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx;
using Styx.WoWInternals.WoWObjects;

namespace Axiom.Helpers
{
    public static class Extensions
    {
        public static bool IsBetween<T>(this T item, T start, T end)
        {
            return Comparer<T>.Default.Compare(item, start) >= 0
                && Comparer<T>.Default.Compare(item, end) <= 0;
        }
        public static string safeName(this WoWUnit unit)
        {
            if (unit != null)
            {
                return (unit.Name == StyxWoW.Me.Name) ? "Myself" : unit.Name;
            }
            return "No Target";
        }
        public static bool IsNumeric(this String str)
        {
            try
            {
                Double.Parse(str.ToString());
                return true;
            }
            catch
            {
            }
            return false;
        }
    }
}
