using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Axiom.Helpers;
using Axiom.Lists;
using Axiom.Managers;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using JetBrains.Annotations;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellLists;
using ShamanSettings = Axiom.Settings.Shaman;

namespace Axiom.Class.Shaman
{
    [UsedImplicitly]
    class Restoration : Axiom
    {
        private readonly int _cancelHeal = Math.Max(95, Math.Max(ShamanSettings.Instance.HealingWave, ShamanSettings.Instance.HealingSurge));

        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.ShamanRestoration ? WoWClass.Shaman : WoWClass.None; } }

        protected override Composite CreateCombat()
        {
            return new ActionRunCoroutine(ret => CombatCoroutine(Me.CurrentTarget));
        }
        protected override Composite CreateBuffs()
        {
            return new ActionRunCoroutine(ret => BuffsCoroutine());
        }
        protected override Composite CreatePull()
        {
            return new ActionRunCoroutine(ret => CombatCoroutine(Me.CurrentTarget));
        }
        protected override Composite CreateHeal()
        {
            return new ActionRunCoroutine(ret => HealCoroutine(HealManager.Target));
        }
        protected override Composite CreateRest()
        {
            return new ActionRunCoroutine(ret => RestCoroutine());
        }
        #endregion

        #region CombatCoroutine
        private async Task<bool> CombatCoroutine(WoWUnit onunit)
        {


            return false;
        }
        #endregion

        #region BuffsCoroutine
        private async Task<bool> BuffsCoroutine()
        {

            return false;
        }
        #endregion

        #region HealCoroutine

        private async Task<bool> HealCoroutine(WoWUnit healtarget)
        {
            await EarthShield();
            await SpiritLinkTotem();
            await AncestralHealingWave(healtarget);

            await Ascendance();

            await HealingTideTotem();
            await HealingStreamTotem();
            await Spell.CoCast(S.ManaTideTotem, !Totems.Exist(WoWTotemType.Water) && Me.ManaPercent < 80);

            await HealingRain();
            await ChainHeal();

            await HealingWave(healtarget);
            await HealingSurge(healtarget);

            await RiptideTank();
            await TidalWavesReUp();
            await RollRiptide();

            return false;
        }

        #endregion

        #region RestCoroutine
        private async Task<bool> RestCoroutine()
        {
            if (Me.IsDead || SpellManager.GlobalCooldown)
                return false;

            if (!(Me.ManaPercent < 60) || Me.IsMoving || Me.IsCasting || Me.Combat || Me.HasAura("Drink") ||
                Styx.CommonBot.Inventory.Consumable.GetBestDrink(false) == null)
                return false;

            Styx.CommonBot.Rest.DrinkImmediate();
            return await Coroutine.Wait(1000, () => Me.HasAura("Drink"));
        }
        #endregion

        #region Spells

            #region EarthShield

            private async Task<bool> EarthShield()
            {
                return await Spell.Buff(S.EarthShield, GetBestEarthShieldTarget());
            }

            private static WoWGuid _guidLastEarthShield;
            private static WoWUnit GetBestEarthShieldTarget()
            {
                WoWUnit target = null;

                if (HealManager.InitialList.Any(m => m.HasAura("Earth Shield")))
                    return null;

                if (IsValidEarthShieldTarget(RaFHelper.Leader))
                    target = RaFHelper.Leader;
                else
                {
                    target = HealManager.Tanks.FirstOrDefault(IsValidEarthShieldTarget);
                    if (Me.Combat && target == null)
                    {
                        target = HealManager.InitialList.Where(u => u.Combat && IsValidEarthShieldTarget(u))
                            .OrderByDescending(u => u.MaxHealth)
                            .FirstOrDefault();
                    }
                }

                _guidLastEarthShield = target != null ? target.Guid : WoWGuid.Empty;
                return target;
            }

            private static bool IsValidEarthShieldTarget(WoWUnit unit)
            {
                if (unit == null || !unit.IsValid || !unit.IsAlive || HealManager.InitialList.All(g => g.Guid != unit.Guid) || unit.Distance > 99)
                    return false;

                return unit.HasAura("Earth Shield") || !unit.HasAnyAura("Earth Shield", "Water Shield", "Lightning Shield");
            }

            #endregion

            #region SpiritLinkTotem

            private async Task<bool> SpiritLinkTotem()
            {
                if (!Me.Combat && (!StyxWoW.Me.GroupInfo.IsInParty || !StyxWoW.Me.GroupInfo.IsInRaid))
                    return false;

                Spell.CoCast(S.SpiritLinkTotem, 
                    HealManager.InitialList.Count(p => p.GetPredictedHealthPercent() < ShamanSettings.Instance.SpiritLinkTotem && p.Distance <= 10f) >= ShamanSettings.Instance.MinSpiritLinkCount);

                return false;
            } 

            #endregion

            #region AncestralHealingWave

            private async Task<bool> AncestralHealingWave(WoWUnit onunit)
            {
                if ((!Me.Combat || !onunit.Combat) && onunit.GetPredictedHealthPercent() >= ShamanSettings.Instance.AncestralSwiftness)
                    return false;

                return await Spell.SelfBuff(S.AncestralSwiftness) && (await Spell.CoCast(S.HealingSurge, onunit) || await Spell.CoCast(S.HealingWave, onunit));
            } 

            #endregion

            #region HealingTideTotem

            private async Task<bool> HealingTideTotem()
            {
                if (!Me.Combat && (!StyxWoW.Me.GroupInfo.IsInParty || !StyxWoW.Me.GroupInfo.IsInRaid))
                    return false;


                return await Spell.CoCast(S.HealingTideTotem, HealManager.InitialList.Count(p =>
                                    p.GetPredictedHealthPercent() < ShamanSettings.Instance.HealingTideTotem &&
                                    p.Distance <= 40f) >= ShamanSettings.Instance.MinHealingTideCount);
            }

            #endregion

            #region HealingStreamTotem

            private async Task<bool> HealingStreamTotem()
            {
                if (!Me.Combat && (!StyxWoW.Me.GroupInfo.IsInParty || !StyxWoW.Me.GroupInfo.IsInRaid))
                    return false;

                if (Totems.Exist(WoWTotemType.Water))
                    return false;

                if (SpellManager.Spells["Healing Stream Totem"].Cooldown)
                    return false;

                var needhealingstream = HealManager.InitialList.Count(p => p.GetPredictedHealthPercent() < ShamanSettings.Instance.HealingStreamTotem && p.Distance <= 40f) >= 1;

                return await Spell.CoCast(S.HealingStreamTotem, needhealingstream);
            }

            #endregion

            #region HealingRain

            private async Task<bool> HealingRain()
            {
                if (!Me.Combat && (!StyxWoW.Me.GroupInfo.IsInParty || !StyxWoW.Me.GroupInfo.IsInRaid))
                    return false;
            
                return await Spell.CastOnGround(S.HealingRain, GetBestHealingRainTarget(), true);
            }

            private static WoWUnit GetBestHealingRainTarget()
            {
                if (!Me.Combat)
                    return null;

                if (!SpellManager.CanCast("Healing Rain"))
                {
                    return null;
                }

                // note: expensive, but worth it to optimize placement of Healing Rain by
                // finding location with most heals, but if tied one with most living targets also
                // build temp list of targets that could use heal and are in range + radius
                List<WoWUnit> coveredTargets = HealManager.InitialList.Where(u => u.IsAlive && u.DistanceSqr < 50 * 50).ToList();
                List<WoWUnit> coveredRainTargets = coveredTargets.Where(u => u.HealthPercent < ShamanSettings.Instance.HealingRain).ToList();

                // search all targets to find best one in best location to use as anchor for cast on ground
                var t = coveredTargets
                    .Where(u => u.DistanceSqr < 40 * 40)
                    .Select(p => new
                    {
                        Player = p,
                        Count = coveredRainTargets.Count(pp => pp.Location.DistanceSqr(p.Location) < 12 * 12),
                        Covered = coveredTargets.Count(pp => pp.Location.DistanceSqr(p.Location) < 12 * 12)
                    })
                    .OrderByDescending(v => v.Count)
                    .ThenByDescending(v => v.Covered)
                    .DefaultIfEmpty(null)
                    .FirstOrDefault();

                if (t == null || t.Count < ShamanSettings.Instance.MinHealingRainCount) return null;
                Log.WritetoFile(string.Format("Healing Rain Target:  found {0} with {1} nearby under {2}%", t.Player.SafeName, t.Count, ShamanSettings.Instance.HealingRain));
                return t.Player;
            }

            #endregion

            #region ChainHeal

            private async Task<bool> ChainHeal()
            {
                if (!Me.Combat && (!StyxWoW.Me.GroupInfo.IsInParty || !StyxWoW.Me.GroupInfo.IsInRaid))
                    return false;

                if (GetBestChainHealTarget() == null)
                    return false;
 
                if (!GetBestChainHealTarget().HasMyAura(S.Riptide))
                {
                    if (await Spell.Buff(S.Riptide, GetBestChainHealTarget()))
                    {
                        TidalWaveRefresh();
                        return true;
                    }
                }

                if (await Spell.CoCast(S.ChainHeal, GetBestChainHealTarget()))
                {
                    TidalWaveRefresh();
                    return true;
                }

                return false;
            }

            private static WoWUnit GetBestChainHealTarget()
            {
                if (!SpellManager.CanCast("Chain Heal"))
                {
                    return null;
                }

                var chainHealHopRange = TalentManager.Glyphs.Contains("Chaining") ? 25f : 12.5f;

                // search players with Riptide first
                var targetInfo = ChainHealRiptidePlayers
                    .Select(p => new { Unit = p, Count = GetChainedClusterCount(p, ChainHealPlayers, chainHealHopRange) })
                    .OrderByDescending(v => v.Count)
                    .ThenByDescending(v => HealManager.Tanks.Any(t => t.Guid == v.Unit.Guid))
                    .DefaultIfEmpty(null)
                    .FirstOrDefault();

                WoWUnit target = targetInfo == null ? null : targetInfo.Unit;
                int count = targetInfo == null ? 0 : targetInfo.Count;

                // too few hops? then search any group member
                if (count < ShamanSettings.Instance.MinChainHealCount)
                {
                    target = GetBestUnitForCluster(ChainHealPlayers, chainHealHopRange);
                    if (target != null)
                    {
                        count = GetChainedCluster(target, ChainHealPlayers, chainHealHopRange).Count();
                        if (count < ShamanSettings.Instance.MinChainHealCount)
                            target = null;
                    }
                }

                if (target != null)
                    Log.WritetoFile(string.Format("Chain Heal Target:  found {0} with {1} nearby under {2}%", target.SafeName, count, ShamanSettings.Instance.ChainHeal));

                return target;
            }

            public static WoWUnit GetBestUnitForCluster(IEnumerable<WoWUnit> units, float clusterRange)
            {
                if (units == null || !units.Any())
                    return null;

                    return (from u in units
                            select new { Count = GetChainedClusterCount(u, units, clusterRange), Unit = u }).OrderByDescending(a => a.Count).
                        FirstOrDefault().Unit;
            }

            static IEnumerable<WoWUnit> GetChainedCluster(WoWUnit target, IEnumerable<WoWUnit> otherUnits, float chainRange)
            {
                var chainRangeSqr = chainRange * chainRange;
                var chainedTargets = new List<WoWUnit> { target };
                WoWUnit chainTarget;
                while ((chainTarget = GetChainTarget(target, otherUnits, chainedTargets, chainRangeSqr)) != null)
                {
                    chainedTargets.Add(chainTarget);
                    target = chainTarget;
                }
                return chainedTargets;
            }

            public static int GetChainedClusterCount(WoWUnit target, IEnumerable<WoWUnit> otherUnits, float chainRange, SimpleBooleanDelegate avoid = null)
            {
                if (avoid == null)
                    return GetChainedCluster(target, otherUnits, chainRange).Count();

                int cnt = 0;
                foreach (var u in GetChainedCluster(target, otherUnits, chainRange))
                {
                    cnt++;
                    if (avoid(u))
                    {
                        cnt = 0;
                        break;
                    }
                }
                return cnt;
            }

            static WoWUnit GetChainTarget(WoWUnit from, IEnumerable<WoWUnit> otherUnits, List<WoWUnit> currentChainTargets, float chainRangeSqr)
            {
                return otherUnits
                    .Where(u => !currentChainTargets.Contains(u) && from.Location.DistanceSqr(u.Location) <= chainRangeSqr)
                    .OrderBy(u => from.Location.DistanceSqr(u.Location))
                    .FirstOrDefault();
            }

            private static IEnumerable<WoWUnit> ChainHealPlayers
            {
                get
                {
                    // TODO: Decide if we want to do this differently to ensure we take into account the T12 4pc bonus. (Not removing RT when using CH)
                    return HealManager.InitialList
                        .Where(u => u.IsAlive && u.DistanceSqr < 40 * 40 && u.GetPredictedHealthPercent() < ShamanSettings.Instance.ChainHeal)
                        .Select(u => u);
                }
            }

            private static IEnumerable<WoWUnit> ChainHealRiptidePlayers
            {
                get
                {
                    // TODO: Decide if we want to do this differently to ensure we take into account the T12 4pc bonus. (Not removing RT when using CH)
                    return HealManager.InitialList
                        .Where(u => u.IsAlive && u.DistanceSqr < 40 * 40 && u.GetPredictedHealthPercent() < ShamanSettings.Instance.ChainHeal && u.HasMyAura(S.Riptide))
                        .Select(u => u);
                }
            }

            #endregion

            #region HealingWave

            private async Task<bool> HealingWave(WoWUnit onunit)
            {
                if (onunit.HealthPercent >= ShamanSettings.Instance.HealingWave)
                    return false;

                await Spell.CoCast(S.UnleashLife, onunit);

                if (await Spell.CoCast(S.HealingWave, onunit, true, onunit.HealthPercent > _cancelHeal))
                {
                    TidalWaveConsume();
                    return true;
                }

                return false;
            } 

            #endregion

            #region HealingSurge

            private async Task<bool> HealingSurge(WoWUnit onunit)
            {
                if (onunit.HealthPercent >= ShamanSettings.Instance.HealingSurge)
                    return false;

                await Spell.CoCast(S.UnleashLife, onunit);

                if (await Spell.CoCast(S.HealingSurge, onunit, true, onunit.HealthPercent > _cancelHeal))
                {
                    TidalWaveConsume();
                    return true;
                }

                return false;
            } 

            #endregion

            #region Ascendance

            private async Task<bool> Ascendance()
            {
                if (!ShamanSettings.Instance.UseAscendance && (!StyxWoW.Me.GroupInfo.IsInParty || !StyxWoW.Me.GroupInfo.IsInRaid))
                    return false;

                return await Spell.SelfBuff(S.Ascendance, () => 
                    HealManager.InitialList.Count(p => p.GetPredictedHealthPercent() < ShamanSettings.Instance.Ascendance) >= ShamanSettings.Instance.MinAscendanceCount);
            }
        
            #endregion

            #region Riptide

            private async Task<bool> RiptideTank()
            {
                if (GetBestRiptideTankTarget() == null || GetBestRiptideTankTarget().HasMyAura(S.Riptide))
                    return false;

                if (await Spell.CoCast(S.Riptide, GetBestRiptideTankTarget()))
                {
                    TidalWaveRefresh();
                    return true;
                }

                return false;
            }

            private async Task<bool> RollRiptide()
            {
                var rollCount = HealManager.InitialList.Count(u => u.IsAlive && u.HasMyAura(S.Riptide));

                if (GetBestRiptideTarget() == null)
                    return false;

                if (rollCount > ShamanSettings.Instance.RollRiptideCount || GetBestRiptideTarget().HasMyAura(S.Riptide))
                    return false;

                if (await Spell.CoCast(S.Riptide, GetBestRiptideTarget()))
                {
                    Log.WriteLog("RollRiptide");
                    TidalWaveRefresh();
                    return true;
                }

                return false;
            } 

            private async Task<bool> TidalWavesReUp()
            {
                if (Me.HasAura(S.TidalWaves))
                    return false;

                if (await Spell.CoCast(S.Riptide, GetBestRiptideTarget()))
                {
                    Log.WriteLog("TidalWavesReUp");
                    TidalWaveRefresh();
                    return true;
                }

                return false;
            }

            private static WoWUnit GetBestRiptideTarget()
            {
                var chainHealHopRange = TalentManager.Glyphs.Contains("Chaining") ? 25f : 12.5f;

                WoWUnit ripTarget = GetBestUnitForCluster(ChainHealPlayers, chainHealHopRange);

                if (ripTarget != null)
                    Log.WriteQuiet(string.Format("GetBestRiptideTarget: found optimal target {0}, hasmyaura={1} with {2} ms left", ripTarget.SafeName, ripTarget.HasMyAura(S.Riptide), (int)ripTarget.GetAuraTimeLeft("Riptide").TotalMilliseconds));

                return ripTarget;
            }

            private static WoWUnit GetBestRiptideTankTarget()
            {
                WoWUnit ripTarget = HealManager.Tanks.Where(u => u.IsAlive && u.Combat && u.DistanceSqr < 40 * 40 && !u.HasMyAura(S.Riptide) && u.InLineOfSpellSight).OrderBy(u => u.HealthPercent).FirstOrDefault();
                if (ripTarget != null)
                    Log.WriteQuiet(string.Format("GetBestRiptideTankTarget: found tank {0}, hasmyaura={1} with {2} ms left", ripTarget.SafeName, ripTarget.HasMyAura(S.Riptide), (int)ripTarget.GetAuraTimeLeft("Riptide").TotalMilliseconds));
                return ripTarget;
            }

            #endregion

            #region Tidal Waves Bookkeeping

            private static int _tidalWaveStacksAudit = 0;

            private static void TidalWaveRefresh()
            {
                _tidalWaveStacksAudit = 2;
            }

            private static void TidalWaveConsume()
            {
                if (_tidalWaveStacksAudit > 0)
                    _tidalWaveStacksAudit--;
            }

            private static int TidalWaveAuditCount()
            {
                return _tidalWaveStacksAudit;
            }

            private bool simpleTidalWavesNeeded()
            {
                return Me.HasAura("Tidal Waves");
            }

            private static bool IsTidalWavesNeeded
            {
                get
                {
                    // WoWAura tw = Me.GetAuraByName("Tidal Waves");
                    var stacks = Me.GetAuraStackCount("Tidal Waves");

                    // 2 stacks means we don't have an issue
                    if (stacks >= 2)
                    {
                        Log.WriteQuiet(string.Format("Tidal Waves={0}", stacks));
                        return false;
                    }

                    // 1 stack? special case and a spell that will consume it is in progress or our audit count shows its gone
                    if (stacks == 1 && TidalWaveAuditCount() > 0)
                    {
                        Log.WriteQuiet(string.Format("Tidal Waves={0}", stacks));
                        return false;
                    }

                    Log.WritetoFile(string.Format("Tidal Waves={0} and Audit={1}, gcd={2}", stacks, TidalWaveAuditCount(), SpellManager.GlobalCooldown));
                    return true;
                }
            }

            #endregion

        #endregion
    }
}
