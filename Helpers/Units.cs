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
using Styx.Common;
using Styx.CommonBot;
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

        #region EnemyUnitsCone

        public static IEnumerable<WoWUnit> EnemyUnitsCone(WoWUnit target, IEnumerable<WoWUnit> otherUnits, float distance)
        {
            var targetLoc = target.Location;
            // most (if not all) player cone spells are 90 degrees.
            return otherUnits.Where(u => target.IsSafelyFacing(u, 90) && u.Location.Distance(targetLoc) <= distance);
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

        #region FriendlyUnitsNearTarget
        public static IEnumerable<WoWUnit> FriendlyUnitsNearTarget(float distance)
        {
            var dist = distance * distance;
            var curTarLocation = StyxWoW.Me.CurrentTarget.Location;
            return ObjectManager.GetObjectsOfType<WoWUnit>(false, false).Where(
                        p => TargetManager.IsValid(p) && p.IsFriendly && p.Location.DistanceSqr(curTarLocation) <= dist).ToList();
        }
        #endregion

        #region GetPathUnits

        public static IEnumerable<WoWUnit> GetPathUnits(WoWUnit target, IEnumerable<WoWUnit> otherUnits, float distance)
        {
            var myLoc = StyxWoW.Me.Location;
            var targetLoc = target.Location;
            return otherUnits.Where(u => u.Location.GetNearestPointOnSegment(myLoc, targetLoc).Distance(u.Location) <= distance);
        }

        #endregion

        #region Auras
        public static bool HasAura(this WoWUnit unit, int auraid, int stacks = 0)
        {
            return HasAura(unit, 0, stacks);
        }
        public static bool HasAura(this WoWUnit unit, int auraid, int msLeft = 0, int stacks = 0)
        {
            if (unit == null)
                return false;
            WoWAura result = unit.GetAllAuras().Where(a => a.CreatorGuid == StyxWoW.Me.Guid && a.SpellId == auraid && !a.IsPassive).FirstOrDefault();
            if (result == null)
                return false;

            if (result.TimeLeft.TotalMilliseconds < msLeft && msLeft != 0)
                return false;

            if (result.StackCount < stacks && stacks != 0)
                return false;

            return true;
        }
        public static bool HasAura(this WoWUnit unit, string aura, int stacks)
        {
            return HasAura(unit, aura, stacks, null);
        }
        private static bool HasAura(this WoWUnit unit, string aura, int stacks, WoWUnit creator)
        {
            return unit.GetAllAuras().Any(a => a.Name == aura && a.StackCount >= stacks && (creator == null || a.CreatorGuid == creator.Guid));
        }
        public static bool HasAura(this WoWUnit unit, string name, bool MyAurasOnly)
        {
            if (unit == null)
                return false;
            WoWAura result = unit.GetAllAuras().Where(a => a.CreatorGuid == StyxWoW.Me.Guid && a.Name == name && !a.IsPassive).FirstOrDefault();
            if (result == null)
                return false;
            return true;
        }
        public static bool HasAnyAura(this WoWUnit unit, params string[] auraNames)
        {
            var auras = unit.GetAllAuras();
            var hashes = new HashSet<string>(auraNames);
            return auras.Any(a => hashes.Contains(a.Name));
        }
        public static uint AuraTimeLeft(this WoWUnit unit, int aura)
        {
            if (!unit.IsValid)
                return 0;

            WoWAura result = unit.GetAllAuras().Where(a => a.CreatorGuid == StyxWoW.Me.Guid && a.SpellId == aura && !a.IsPassive).FirstOrDefault();

            if (result == null)
                return 0;

            return result.Duration;
        }
        public static TimeSpan GetAuraTimeLeft(this WoWUnit onUnit, string auraName, bool fromMyAura = true)
        {
            WoWAura wantedAura =
                onUnit.GetAllAuras().Where(a => a != null && a.Name == auraName && a.TimeLeft > TimeSpan.Zero && (!fromMyAura || a.CreatorGuid == StyxWoW.Me.Guid)).FirstOrDefault();

            return wantedAura != null ? wantedAura.TimeLeft : TimeSpan.Zero;
        }
        public static uint GetAuraStackCount(this WoWUnit unit, string aura)
        {
            if (unit != null && unit.IsValid)
            {
                WoWAura S = unit.Auras.Values.Where(a => a.Name == aura && a.CreatorGuid == Me.Guid).FirstOrDefault();
                if (S != null)
                {
                    Log.WritetoFile(LogLevel.Diagnostic, String.Format("{0} has {1} stacks of {2}", unit.safeName(), unit.Auras[aura].StackCount, aura));
                    return S.StackCount;
                }
            }
            return UInt32.MinValue;
        }
        public static bool HasAuraExpired(this WoWUnit u, string aura, int secs = 3, bool myAura = true)
        {
            return u.HasAuraExpired(aura, aura, secs, myAura);
        }
        public static bool HasAuraExpired(this WoWUnit u, string spell, string aura, int secs = 3, bool myAura = true)
        {
            // need to compare millisecs even though seconds are provided.  otherwise see it as expired 999 ms early because
            // .. of loss of precision
            return SpellManager.HasSpell(spell) && u.GetAuraTimeLeft(aura, myAura).TotalSeconds <= secs;
        }
        #endregion

        #region InRange

        public static bool InRange(this WoWUnit unit)
        {
            if (!TargetManager.IsValid(unit))
                return false;
            if (unit.Guid == Me.Guid)
                return true;
            if (unit.Distance <= (float) Math.Max(5f, Me.CombatReach + 1.3333334f + unit.CombatReach))
                return true;
            if (unit.IsWithinMeleeRange)
                return true;
            return false;
        }

        #endregion

        #region IsFriendly

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

        #endregion

        #region Status

        public static string Status(this WoWUnit unit)
        {
            if (!TargetManager.IsValid(unit))
                return "Unknown";
            if (Spell.Me.Role == WoWPartyMember.GroupRole.Tank && unit.IsHostile)
                return unit.ThreatInfo.RawPercent + "%Threat";
            if (Spell.Me.Role == WoWPartyMember.GroupRole.Damage && !unit.IsFriendly)
                return Math.Round(Spell.Me.EnergyPercent) + "%Energy" + Math.Round(unit.HealthPercent) + "%HP's" +
                       Spell.Me.ComboPoints + "CP's";
            if (Spell.Me.Role == WoWPartyMember.GroupRole.Healer)
                return Math.Round(unit.HealthPercent()) + "%HP's " + Math.Round(Spell.Me.PowerPercent) + "%" +
                       Spell.Me.PowerType + " " + Spell.Me.CurrentChi + "CP's";
            return Math.Round(unit.HealthPercent) + "%HP's";
        }

        #endregion

        #region HealthPercent (Predicted and Current)

        public static double HealthPercent(this WoWUnit unit)
        {
            if (unit == null || !unit.IsValid)
                return Double.MinValue;
            return Math.Max(unit.GetPredictedHealthPercent(), unit.HealthPercent);
        }

        #endregion

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
