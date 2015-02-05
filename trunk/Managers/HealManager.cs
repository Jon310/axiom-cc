using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axiom.Helpers;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Axiom.Managers
{
    class HealManager : Axiom
    {
        public static double GroupCount = new double();
        public static Styx.Common.Helpers.WaitTimer SpamDelay = Styx.Common.Helpers.WaitTimer.OneSecond;

        public static bool IsPhased(WoWUnit unit)
        {
            return unit.GetAllAuras().Any(a => a.SpellId == 144850 || a.SpellId == 144849 || a.SpellId == 144851);
        }
        public static HashSet<int> HealNPCs = new HashSet<int>()
        {
            72311, //King Varian
            72302, //Lady Jaina
            73910, //Vanessa Windrunner
            62442, //Tsulong
            71604, //Immerseus Spawn
            71995, //Leven Puri fight
            71996, //Rook puri fight
            72000, //Sun puri fight
            71357, //Wrathion Challenge
            87321, //Healing Dummy
            78884, //Living Mushroom
            78868 //Revujanating Mushroom
        };
        public static List<WoWUnit> InitialList
        {

            get
            {
                // Grab party+raid member + myself GUIDs
                WoWGuid[] guids =
                    StyxWoW.Me.GroupInfo.RaidMemberGuids.Union(StyxWoW.Me.GroupInfo.PartyMemberGuids).Distinct().ToArray();
                List<WoWUnit> _initiallist = (
                    from p in ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                    where p.IsFriendly && (guids.Any(g => g == p.Guid) || HealNPCs.Any(h => h == p.Entry))
                    select p).ToList();
                _initiallist.Add(StyxWoW.Me);
                GroupCount = _initiallist.Count();
                if (_initiallist.Count != 0 && SpamDelay.IsFinished)
                {
                    Log.WritetoFile(Styx.Common.LogLevel.Diagnostic, "HealManager List: " + _initiallist.Count + " Entries");
                    SpamDelay.Reset();
                }
                return _initiallist;
            }
        }
        public static List<WoWUnit> ValidList
        {
            get
            {
                return InitialList.Where(u => IsValid(u)).ToList();
            }
        }
        public static IEnumerable<WoWUnit> Targets(double minhealthpct = 100, double maxdistance = 40)
        {
            return InitialList.Where(unit => IsValid(unit) &&
                                (unit.HealthPercent <= minhealthpct ||
                                unit.GetPredictedHealthPercent() <= minhealthpct) &&
                                unit.Distance <= maxdistance).ToList();
        }
        public static bool IsValid(WoWUnit unit)
        {
            if (unit == null ||
                !unit.IsValid)
                return false;
            return unit != null &&
                     unit.IsValid &&
                     unit.CanSelect &&
                     unit.InLineOfSpellSight &&
                     unit.IsAlive &&
                     !HealManager.IsPhased(unit) &&
                     !Styx.CommonBot.Blacklist.Contains(unit, BlacklistFlags.Combat);
        }
        public static WoWUnit CleanseTarget
        {
            get
            {
                return HealManager.InitialList.Where(unit => unit.IsValid && unit.Distance < 40 && HealManager.NeedCleanseASAP(unit)).FirstOrDefault();
            }
        }
        public static List<WoWUnit> Tanks
        {
            get
            {
                if (!Me.GroupInfo.IsInParty)
                    return new List<WoWUnit>() { StyxWoW.Me.ToUnit() };
                List<WoWUnit> TankList = new List<WoWUnit>();
                List<WoWPartyMember> MyTanks = StyxWoW.Me.GroupInfo.RaidMembers.Union(StyxWoW.Me.GroupInfo.PartyMembers).Distinct().Where(mbr => mbr.IsMainAssist || mbr.IsMainTank).ToList();
                foreach (WoWPartyMember Pm in MyTanks)
                {
                    if (InitialList.Where(t => IsValid(t) && t.Distance <= 100).Contains(Pm.ToPlayer()))
                    {
                        TankList.Add(Pm.ToPlayer());
                    }
                }
                if (TankList.Count() == 0)
                    TankList.Add(StyxWoW.Me.ToUnit());
                return TankList;

            }
        }
        public static List<WoWGuid> TankGuids
        {
            get
            {
                List<WoWGuid> TankList = new List<WoWGuid>();
                List<WoWPartyMember> MyTanks = StyxWoW.Me.GroupInfo.RaidMembers.Union(StyxWoW.Me.GroupInfo.PartyMembers).Distinct().Where(mbr => mbr.IsMainAssist || mbr.IsMainTank).ToList();
                foreach (WoWPartyMember Pm in MyTanks)
                {
                    if (InitialList.Contains(Pm.ToPlayer()))
                    {
                        TankList.Add(Pm.Guid);
                    }

                }
                return TankList;
            }
        }

        #region HealTargeting
        public static WoWUnit SmartTarget(double MinHealthPct)
        {
            //First Lets Build a complete List of Possible Targets
            var AllTargets =
                (from t in HealManager.InitialList
                 where IsValid(t) && t.GetPredictedHealthPercent() <= MinHealthPct && t.TotalAbsorbs != t.MaxHealth
                 select t).ToList();
            WoWUnit BestTarget =
                (from t in AllTargets
                 orderby calcweight(t)
                 //percentage = (int * 100 / total)
                 select t).FirstOrDefault();
            if (BestTarget != null && BestTarget.IsValid)
                Log.WritetoFile(Styx.Common.LogLevel.Diagnostic, "Healing Target selected:" + BestTarget.SafeName + "@" + BestTarget.HealthPercent.ToString() + "HP" + " &" + Math.Round(calcweight(BestTarget)).ToString() + "weight");
            return BestTarget;
        }
        //public static WoWUnit SmartTarget(string spell)
        //{
        //    double estimatedheal = PriorityHealing.CalculateHPIncrease(spell);
        //    //First Lets Build a complete List of Possible Targets
        //    var AllTargets =
        //        (from t in HealManager.InitialList
        //         where IsValid(t) && t.GetPredictedHealth() <= (t.CurrentHealth + estimatedheal) && t.TotalAbsorbs != t.MaxHealth &&
        //         !Spells.SpellHistoryContainsKey(spell, t.Guid) &&
        //         (t.CurrentHealth + estimatedheal) <= t.MaxHealth &&
        //         t.Distance <= Spells.SpellRange(spell, t)
        //         select t).ToList();
        //    var BestTarget =
        //        (from t in AllTargets
        //         orderby calcweight(t)
        //         //percentage = (int * 100 / total)
        //         select t).FirstOrDefault();
        //    //if (BestTarget != null && BestTarget.IsValid)
        //    //    LogManager.WriteLog("Smart Target selected:"+ BestTarget.SafeName + "@" + BestTarget.HealthPercent.ToString() + "HP" + " &" + Math.Round(calcweight(BestTarget)).ToString() + "weight");
        //    return BestTarget;
        //}
        //public static IOrderedEnumerable<WoWUnit> SmartTargets(string spell)
        //{
        //    double estimatedheal = PriorityHealing.CalculateHPIncrease(spell);
        //    //First Lets Build a complete List of Possible Targets
        //    var AllTargets =
        //        (from t in HealManager.InitialList
        //         where IsValid(t) && t.GetPredictedHealth() <= (t.CurrentHealth + estimatedheal) && t.TotalAbsorbs != t.MaxHealth &&
        //         !Spells.SpellHistoryContainsKey(spell, t.Guid) &&
        //         (t.CurrentHealth + estimatedheal) <= t.MaxHealth &&
        //         t.Distance <= Spells.SpellRange(spell, t)
        //         select t).ToList();
        //    IOrderedEnumerable<WoWUnit> BestTargets =
        //        (from t in AllTargets
        //         orderby calcweight(t)
        //         //percentage = (int * 100 / total)
        //         select t);
        //    //if (BestTarget != null && BestTarget.IsValid)
        //    //    LogManager.WriteLog("Smart Target selected:"+ BestTarget.SafeName + "@" + BestTarget.HealthPercent.ToString() + "HP" + " &" + Math.Round(calcweight(BestTarget)).ToString() + "weight");
        //    return BestTargets;
        //}
        public static IOrderedEnumerable<WoWUnit> SmartTargets(double MinHealthPct, double range = 40)
        {
            //First Lets Build a complete List of Possible Targets
            var AllTargets =
                (from t in HealManager.InitialList
                 where IsValid(t) && t.GetPredictedHealthPercent() <= MinHealthPct && t.TotalAbsorbs != t.MaxHealth &&
                 t.Distance <= range
                 select t).ToList();
            IOrderedEnumerable<WoWUnit> BestTargets =
                (from t in AllTargets
                 orderby calcweight(t)
                 //percentage = (int * 100 / total)
                 select t);
            return BestTargets;
        }
        private static double calcweight(WoWUnit unit)
        {
            if (unit == null || !unit.IsValid || !unit.IsAlive || !Me.GroupInfo.IsInParty)
                return 0;
            if (!unit.IsPlayer)
                return unit.HealthPercent * 2;
            WoWPartyMember member = Me.GroupInfo.RaidMembers.Union(Me.GroupInfo.PartyMembers).Distinct().Where(mbr => mbr.Guid == unit.Guid).FirstOrDefault();
            double multiplier = 2;
            if (member.Role == WoWPartyMember.GroupRole.Healer)
                multiplier = 1.5;
            if (member.Role == WoWPartyMember.GroupRole.Tank)
                multiplier = 1;
            return unit.HealthPercent * multiplier;
        }
        public static WoWUnit GetTarget(double healthpct, float range = 40)
        {
            IEnumerable<WoWUnit> HealTargets = HealManager.Targets(healthpct, range);
            return HealTargets.OrderBy(unit => unit.HealthPercent).FirstOrDefault();
        }
        public static WoWUnit Target
        {
            get
            {
                return GetTarget(100);
            }
        }
        public static IEnumerable<WoWUnit> NeedMyAura(string Aura, double healthpct, float range = 40)
        {
            return HealManager.Targets(healthpct, range).Where(unit => !unit.HasAura(Aura)).OrderBy(unit => unit.HealthPercent);
        }
        public static bool NeedCleanseASAP(WoWUnit unit)
        {
            foreach (KeyValuePair<string, WoWAura> aura in unit.Auras)
            {
                if (aura.Value.IsHarmful && (aura.Value.Spell.DispelType == WoWDispelType.Magic || aura.Value.Spell.DispelType == WoWDispelType.Poison || aura.Value.Spell.DispelType == WoWDispelType.Disease))
                    return true;
            }
            return false;
        }
        public static double CountNearby(WoWObject unitCenter, float distance, double healthPercent)
        {
            if (!TargetManager.IsValid(unitCenter))
                return 0;
            return Targets(healthPercent).Count(unit => unitCenter.Location.Distance(unit.Location) <= distance);
        }

        #endregion
    }
}
