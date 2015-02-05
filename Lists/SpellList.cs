using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Axiom.Lists
{
    [UsedImplicitly]
    class SpellList : Axiom
    {
        #region ChannedInteruptableSpells
        public static readonly HashSet<int> ChanneledInteruptableSpells = new HashSet<int>
        {
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

        #region Warrior Spells

        public const int Avatar = 107574;

        public const int BattleShout = 6673;

        public const int Bladestorm = 46924;

        public const int BloodBath = 12292;

        public const int BerserkerRage = 18499;

        public const int Charge = 100;

        public const int Cleave = 845;

        public const int ColossusSmash = 167105;

        public const int CommandingShout = 469;

        public const int DemoralizingBanner = 114203;

        public const int DieByTheSword = 118038;

        public const int DragonRoar = 118000;

        public const int Enrage = 12880;

        public const int EnragedRegeneration = 55694;

        public const int Execute = 163201;

        public const int HeroicLeap = 6544;

        public const int HeroicStrike = 78;

        public const int HeroicThrow = 57755;

        public const int ImpendingVictory = 103840;

        public const int MockingBanner = 114192;

        public const int MortalStrike = 12294;

        public const int Overpower = 7384;

        public const int Ravager = 152277;

        public const int Recklessness = 1719;

        public const int Rend = 772;

        public const int Siegebreaker = 176289;

        public const int ShatteringThrow = 64382;

        public const int Shockwave = 46968;

        public const int SkullBanner = 114207;

        public const int Slam = 1464;

        public const int StormBolt = 107570;

        public const int SuddenDeath = 52437;

        public const int SweepingStrikes = 12328;

        public const int ThunderClap = 6343;

        public const int VictoryRush = 34428;

        public const int Whirlwind = 1680;

        #endregion
    }
}
