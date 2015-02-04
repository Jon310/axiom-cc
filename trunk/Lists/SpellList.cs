using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotNamed.Lists
{
    class SpellList
    {
        #region ChannedInteruptableSpells
        public static readonly HashSet<int> ChanneledInteruptableSpells = new HashSet<int> {
           5143, // Arcane Missiles, // 
           42650, // Army of the Dead, // 
           10, // Blizzard, // 
           64843, // Divine Hymn, // 
           689, // Drain Life, // 
           89420, // Drain Life, // 
           1120, // Drain Soul, // 
           755, // Health Funnel, // 
           1949, // Hellfire, // 
           85403, // Hellfire, // 
           16914, // Hurricane, // 
           64901, // Hymn of Hope, // 
           50589, // Immolation Aura, // 
           15407, // Mind Flay, // 
           47540, // Penance, // 
           5740, // Rain of Fire, // 
           740, // Tranquility, // 
           103103, // Malefic Grasp //
        };
        #endregion
    }
}
