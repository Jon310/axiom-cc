using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Axiom.Helpers;
using Axiom.Managers;
using Bots.DungeonBuddy.Helpers;
using Styx;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Axiom.Helpers
{
    public static class Units
    {
        private static readonly LocalPlayer Me = StyxWoW.Me;

        #region EnemyUnits

        public static IEnumerable<WoWUnit> EnemyUnits(int maxSpellDist)
        {
            var typeWoWUnit = typeof(WoWUnit);
            var typeWoWPlayer = typeof(WoWPlayer);
            var objectList = ObjectManager.ObjectList;
            return (from t1 in objectList
                    let type = t1.GetType()
                    where type == typeWoWUnit ||
                        type == typeWoWPlayer
                    select t1 as WoWUnit into t
                    where t != null && TargetManager.IsValid(t) && t.InRange()
                    select t).ToList();
        }

        #endregion

        #region EnemyUnitsSub40

        public static IEnumerable<WoWUnit> EnemyUnitsSub40
        {
            get { return EnemyUnits(40); }
        }

        #endregion

        #region EnemyUnitsSub10

        public static IEnumerable<WoWUnit> EnemyUnitsSub10
        {
            get { return EnemyUnits(10); }
        }

        #endregion

        #region EnemyUnitsSub8

        public static IEnumerable<WoWUnit> EnemyUnitsSub8
        {
            get { return EnemyUnits(8); }
        }

        #endregion

        #region EnemyUnitsMelee

        public static IEnumerable<WoWUnit> EnemyUnitsMelee
        {
            get { return EnemyUnits(Me.MeleeRange().ToString(CultureInfo.InvariantCulture).ToInt32()); }
        }

        #endregion


        public static bool InRange(this WoWUnit unit)
        {
            if (!TargetManager.IsValid(unit))
                return false;
            if (unit.Guid == Me.Guid)
                return true;
            if (unit.Distance <= (float)Math.Max(5f, Me.CombatReach + 1.3333334f + unit.CombatReach))
                return true;
            if (unit.IsWithinMeleeRange)
                return true;
            return false;
        }

        public static bool IsFriendly(this WoWUnit Target)
        {
            if (!HealManager.IsValid(Target))
                return false;
            if (Target.Guid == Me.Guid)
                return true;
            if (HealManager.InitialList.Contains(Target.ToPlayer()))
                return true;
            if (Me.CurrentMap.IsArena && !Me.GroupInfo.IsInCurrentParty(Target.Guid))
                return false;
            if (Target.IsFriendly)
                return true;
            if (Target.IsPlayer && Target.ToPlayer().FactionGroup == Me.FactionGroup)
                return true;
            return false;
        }

        public static string Status(this WoWUnit unit)
        {
            if (!TargetManager.IsValid(unit))
                return "Unknown";
            if (Spell.Me.Role == WoWPartyMember.GroupRole.Tank && unit.IsHostile)
                return unit.ThreatInfo.RawPercent + "%Threat";
            if (Spell.Me.Role == WoWPartyMember.GroupRole.Damage && !unit.IsFriendly)
                return Math.Round(Spell.Me.EnergyPercent) + "%Energy" + Math.Round(unit.HealthPercent) + "%HP's" + Spell.Me.ComboPoints + "CP's";
            if (Spell.Me.Role == WoWPartyMember.GroupRole.Healer)
                return Math.Round(unit.HealthPercent()) + "%HP's " + Math.Round(Spell.Me.PowerPercent) + "%" + Spell.Me.PowerType + " " + Spell.Me.CurrentChi + "CP's";
            return Math.Round(unit.HealthPercent) + "%HP's";
        }

        public static double HealthPercent(this WoWUnit unit)
        {
            if (unit == null || !unit.IsValid)
                return Double.MinValue;
            return Math.Max(unit.GetPredictedHealthPercent(), unit.HealthPercent);
        }

        #region DebuffCC
        public static bool DebuffCC(this WoWUnit target)
        {
            {

                if (!target.IsPlayer)
                {
                    return false;
                }
                if (target.Stunned)
                {
                    Log.WriteLog("Stunned!", Colors.Red);
                    return true;
                }
                if (target.Silenced)
                {
                    Log.WriteLog("Silenced", Colors.Red);
                    return true;
                }
                if (target.Dazed)
                {
                    Log.WriteLog("Dazed", Colors.Red);
                    return true;
                }

                WoWAuraCollection Auras = target.GetAllAuras();

                return Auras.Any(a => a.Spell != null && a.Spell.SpellEffects.Any(
                se => se.AuraType == WoWApplyAuraType.ModConfuse
                    || se.AuraType == WoWApplyAuraType.ModCharm
                    || se.AuraType == WoWApplyAuraType.ModFear
                    || se.AuraType == WoWApplyAuraType.ModPacify
                    || se.AuraType == WoWApplyAuraType.ModPacifySilence
                    || se.AuraType == WoWApplyAuraType.ModPossess
                    || se.AuraType == WoWApplyAuraType.ModStun
                ));
            }
        }
        #endregion
    }
}
