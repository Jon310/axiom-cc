using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axiom.Helpers;
using NotNamed.Helpers;
using NotNamed.Lists;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Axiom.Managers
{
    internal class TargetManager : Axiom
    {
        public static Styx.Common.Helpers.WaitTimer SpamDelay = Styx.Common.Helpers.WaitTimer.OneSecond;
        private static List<WoWUnit> _initialunits = new List<WoWUnit>();

        public static List<WoWUnit> InitialUnits
        {
            get
            {
                _initialunits = ObjectManager.GetObjectsOfType<WoWUnit>(true, false).Where(IsValid).ToList();
                if (_initialunits.Count != 0 && SpamDelay.IsFinished)
                {
                    Log.WritetoFile(Styx.Common.LogLevel.Diagnostic,
                        "TargetManager List: " + _initialunits.Count + " Entries");
                    SpamDelay.Reset();
                }
                return _initialunits;
            }
        }

        public static bool IsValid(WoWUnit unit)
        {
            if (unit == null ||
                !unit.IsValid)
                return false;
            return unit.CanSelect &&
                   unit.Attackable &&
                   !unit.IsFriendly &&
                   !unit.IsPet &&
                   unit.CreatedByUnit == null &&
                   !unit.IsCritter &&
                   !unit.IsPetBattleCritter &&
                   !Styx.CommonBot.Blacklist.Contains(unit, BlacklistFlags.Combat);

        }

        public static bool IsValid(WoWObject o)
        {
            if (o == null ||
                !o.IsValid)
                return false;
            return true;
        }

        public static WoWUnit NeedTaunt
        {
            get
            {
                return InitialUnits.Where(unit =>
                    !HealManager.Tanks.Any(
                        tank => tank.GetThreatInfoFor(unit).ThreatStatus == ThreatStatus.SecurelyTanking))
                    .OrderBy(unit => unit.ThreatInfo.ThreatValue)
                    .FirstOrDefault();
            }
        }

        public static WoWUnit TankSmartTarget(double distance = 0)
        {
            {
                //First Lets Build a complete List of Possible Targets
                var AllTargets =
                    (from t in InitialUnits
                        where IsValid(t) &&
                              t.Combat &&
                              (distance == 0 && Units.InRange(t) || t.Distance <= distance)
                        select t).ToList();
                var BestTarget =
                    (from t in AllTargets
                        orderby calcaggroweight(t)
                        //percentage = (int * 100 / total)
                        select t).FirstOrDefault();
                if (BestTarget != null && BestTarget.IsValid)
                    Log.WritetoFile(Styx.Common.LogLevel.Diagnostic,
                        "Smart Target selected:" + BestTarget.SafeName + "@" + BestTarget.HealthPercent.ToString() +
                        "HP" + " &" + Math.Round(calcaggroweight(BestTarget)).ToString() + "weight");
                return BestTarget;
            }
        }

        private static double calcaggroweight(WoWUnit unit)
        {
            if (unit == null || !unit.IsValid || !unit.IsAlive || !Me.GroupInfo.IsInParty)
                return 0;
            double multiplier = 2;
            if ((Me.GroupInfo.IsInRaid && unit.IsTargetingMyRaidMember) ||
                (Me.GroupInfo.IsInParty && unit.IsTargetingMyPartyMember))
                multiplier = 3;
            //First lets make sure that any fellow groupmates don't have aggro
            if (HealManager.Tanks.Any(tank => tank.GetThreatInfoFor(unit).ThreatStatus == ThreatStatus.SecurelyTanking))
                multiplier = 1;
            return unit.ThreatInfo.RawPercent*multiplier;
        }

        public static WoWUnit InterruptTarget
        {
            get { return InterruptTargets.FirstOrDefault(); }
        }

        public static List<WoWUnit> InterruptTargets
        {
            get
            {
                return TargetManager.InitialUnits
                    .Where(unit =>
                        (unit.CastingSpell != null &&
                         unit.CanInterruptCurrentSpellCast) ||
                        unit.ChannelObject != null &&
                        SpellList.ChanneledInteruptableSpells.Contains(unit.ChanneledCastingSpellId))
                    .ToList();
            }
        }

        public static WoWUnit NeedCCUnits
        {
            get
            {
                return TargetManager.InitialUnits.Where(unit =>
                    unit.InLineOfSight &&
                    !unit.DebuffCC())
                    .OrderByDescending(unit =>
                        unit.HealthPercent)
                    .FirstOrDefault();
            }
        }

        public static void EnsureTarget(WoWUnit onunit)
        {
            if (!IsValid(Styx.StyxWoW.Me.CurrentTarget) && onunit != null && onunit.IsValid && onunit.IsAlive)
            {
                Log.WriteLog(Styx.Common.LogLevel.Diagnostic, "No Target...Reselecting");
                onunit.Target();
                return;
            }
            if (Me.Combat && Me.CurrentTarget != null && !StyxWoW.Me.CurrentTarget.IsAlive)
            {
                StyxWoW.Me.ClearTarget();
                return;
            }
        }

        public static bool BossFight
        {
            get
            {
                return
                    InitialUnits.Where(
                        unit =>
                            unit.IsBoss && unit.Classification == WoWUnitClassificationType.Elite && unit.Distance < 40)
                        .Count() != 0;
            }
        }

        public static WoWUnit MeleeTarget
        {
            get { return SmartTarget(); }
        }

        public static WoWUnit RangeTarget
        {
            get { return SmartTarget(true, 40); }
        }

        public static WoWUnit SmartTarget(bool Range = false, double Distance = 40)
        {
            //First Lets Build a complete List of Possible Targets
            var AllTargets =
                (from t in InitialUnits
                    where IsValid(t) &&
                          Range
                        ? (t.Distance + t.CombatReach) <= Distance
                        : t.IsWithinMeleeRange &&
                          //If your in a group and not a tank lets not pull anything that isn't already aggroed on us and isn't withing reach.
                          (!Me.GroupInfo.IsInParty || t.IsWithinMeleeRange || t.IsTargetingMeOrPet ||
                           t.IsTargetingMyPartyMember ||
                           t.IsTargetingMyRaidMember ||
                           t.ThreatInfo.ThreatStatus != ThreatStatus.UnitNotInThreatTable ||
                           HealManager.InitialList.Any(gp => gp.Combat && gp.CurrentTarget == t)) &&
                          t.IsAlive
                    select t).ToList();
            if (Me.CurrentTarget != null && !AllTargets.Contains(Me.CurrentTarget)) AllTargets.Add(Me.CurrentTarget);
            WoWUnit BestTarget =
                (from t in AllTargets
                    orderby Distance, t.HealthPercent
                    //percentage = (int * 100 / total)
                    select t).FirstOrDefault();
            if (BestTarget != null && BestTarget.IsValid)
                Log.WritetoFile(Styx.Common.LogLevel.Diagnostic,
                    "Hostile Target selected:" + BestTarget.SafeName + "@" + BestTarget.HealthPercent.ToString() + "HP@" +
                    Math.Round(BestTarget.Distance) + "Distance");
            return BestTarget;
        }

        public static double CountNear(WoWObject unitCenter, float distance)
        {
            if (!TargetManager.IsValid(unitCenter))
                return 0;
            return
                TargetManager.InitialUnits.Where(
                    unit =>
                        unit.ThreatInfo.ThreatStatus != ThreatStatus.UnitNotInThreatTable &&
                        unitCenter.Location.Distance(unit.Location) <= distance).Count();

        }

        public static double CountUnitsTargetingMe()
        {
            return TargetManager.InitialUnits.Where(unit => unit.IsTargetingMeOrPet).Count();
        }

        public static IEnumerable<WoWUnit> HasMyAura(string Aura, float range = 40)
        {
            return TargetManager.InitialUnits.Where(unit => unit.HasAura(Aura));
        }
    }
}
