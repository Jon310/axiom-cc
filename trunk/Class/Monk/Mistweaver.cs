using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellLists;
using MonkSettings = Axiom.Settings.Monk;

namespace Axiom.Class.Monk
{
    public class Mistweaver : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.MonkMistweaver ? WoWClass.Monk : WoWClass.None; } }
        private bool SerpentStance { get { return Me.HasAura("Stance of the Wise Serpent"); } }
        private bool CraneStance { get { return Me.HasAura("Stance of the Spirited Crane"); } }
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
            await Crane(onunit, CraneStance);

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
            if (Me.Mounted) return true;

            await LifeCocoon();

            if (MonkSettings.Instance.PrioritizeSelf)
            {
                if (Me.HealthPercent() <= MonkSettings.Instance.HealthStone)
                    Item.UseContainerItem("Healthstone");

                await Spell.SelfHeal(S.ExpelHarm, () => !TalentManager.HasGlyph("Targeted Expulsion") && Me.HealthPercent < MonkSettings.Instance.ExpelHarm);
                await Spell.Heal(S.ExpelHarm, healtarget, () => TalentManager.HasGlyph("Targeted Expulsion") && healtarget.HealthPercent < MonkSettings.Instance.ExpelHarm);
                await Spell.SelfBuff(S.FortifyingBrew, () => Me.HealthPercent() <= MonkSettings.Instance.FortifyingBrew && AFK);
                await Spell.SelfBuff(S.DiffuseMagic, () => HealManager.NeedCleanseASAP(Me) && AFK);
                await ChiBrew();
            }

            if (SerpentStance)
            {
                await SerpentStatue(StatueCluster(), MonkSettings.Instance.AutoSerpentStatue);
                await ManualStatue(!MonkSettings.Instance.AutoSerpentStatue);

                await ManaTea(MonkSettings.Instance.ManaTea);
                await Uplift(MonkSettings.Instance.Uplift);
                await ChiWave(healtarget);
                await SpinningCraneKick();
                await RenewingMist();
                await ZenSpheres();
                await ChiBurst(healtarget);
                await Spell.SelfBuff(S.Revival, () => HealManager.SmartTargets(MonkSettings.Instance.Revival).Count() >= HealManager.GroupCount / 2);
                await EnvelopingMists();
                await SoothingMist();
                await SurgingMists();
                await Detox(healtarget);
            }

            return false;
        }
        #endregion

        #region RestCoroutine
        private async Task<bool> RestCoroutine()
        {
            return false;
        }
        #endregion

        #region Crane
        private async Task<bool> Crane(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            await SerpentStatue(StatueCluster(), MonkSettings.Instance.AutoSerpentStatue);
            await ManualStatue(!MonkSettings.Instance.AutoSerpentStatue);
            
            await LifeCocoon();
            await Spell.SelfBuff(S.ThunderFocusTea, () => HealManager.CountNearby(Me, 40, MonkSettings.Instance.LifeCocoon) >= 1 && Me.HasAura("Vital Mists", 5));
            await Detox(onunit);
            await ManaTea(MonkSettings.Instance.ManaTea);
            await Spell.SelfHeal(S.ExpelHarm, () => !TalentManager.HasGlyph("Targeted Expulsion") && Me.HealthPercent < MonkSettings.Instance.ExpelHarm);
            await Spell.Heal(S.ExpelHarm, HealManager.Target, () => TalentManager.HasGlyph("Targeted Expulsion") && HealManager.Target.HealthPercent < MonkSettings.Instance.ExpelHarm);
            await ChiBurst(HealManager.Target);
            await Spell.CoCast(S.SurgingMist, HealManager.SmartTarget(100), Me.HasAura("Vital Mists", 5));
            await Spell.Cast(S.SpinningCraneKick, onunit, () => (Units.EnemyUnitsSub8.Count() >= 5 && !TalentManager.IsSelected(16) || Units.EnemyUnitsSub8.Count() >= 3 && TalentManager.IsSelected(16)) && Axiom.AOE);
            await Spell.Cast(S.TigerPalm, onunit, () => (Me.HasAura("Vital Mists", 4) || !Me.HasAura("Tiger Power")) && Me.CurrentChi > 0);
            await Spell.Cast(S.BlackoutKick, onunit, () => !Me.HasAura("Crane's Zeal") && Me.CurrentChi >= 2);
            await Spell.Cast(S.RisingSunKick, onunit, () => Me.CurrentChi >= 2);
            await Spell.Cast(S.ChiWave, onunit);
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 3);
            await ChiBrewFist();
            //await Spell.Cast(S.Jab, onunit, () => Me.CurrentChi <= 3 && !TalentManager.IsSelected(8) || Me.CurrentChi <= 4 && TalentManager.IsSelected(8));
            await Spell.Cast(S.Jab, onunit, () => Me.CurrentChi < Me.ChiInfo.Max);

            return true;
        }
        #endregion

        #region Spells

            #region LifeCocoon
            private async Task<bool> LifeCocoon()
            {
                var cocoontank = HealManager.Tanks.OrderBy(u => u.HealthPercent).LastOrDefault();

                return await Spell.Buff("Life Cocoon", cocoontank, () => cocoontank.HealthPercent() < MonkSettings.Instance.LifeCocoon, "Tank");
            }
            #endregion

            #region ManaTea
            private async Task<bool> ManaTea(int percent)
            {
                if (Me.ManaPercent > percent && Me.GetAuraStackCount("Mana Tea") < 18)
                    return false;

                var currentmana = Me.ManaPercent;

                if (TalentManager.HasGlyph("Mana Tea"))
                {
                    return await Spell.SelfBuff(S.ManaTea, () => Me.ManaPercent < MonkSettings.Instance.ManaTea && Me.GetAuraStackCount("Mana Tea") > 2);
                }

                return await Spell.SelfBuff(S.ManaTea, () => Me.GetAuraStackCount("Mana Tea") >= 2 && currentmana + (4 * 2) < 100, "", true) 
                       && await Coroutine.Wait(2000, () => Spell.StopCasting(() => Me.ManaPercent >= currentmana + 8));
            }
            #endregion

            #region Uplift
            private async Task<bool> Uplift(double healthpct)
            {
                if (Me.CurrentChi < 2)
                    return false;

                var hasRenew = HealManager.SmartTargets(100).Where(hr => hr.HasAura("Renewing Mist"));
                var needRenew = HealManager.SmartTargets(MonkSettings.Instance.RenewingMist).Where(r => !r.HasAura(119611) && r.HealthPercent >= 30);
                var woWUnits = hasRenew as IList<WoWUnit> ?? hasRenew.ToList();

                if (woWUnits.Count() >= 3 && !SpellManager.Spells["Thunder Focus Tea"].Cooldown && needRenew.Count() >= 3)
                {
                    return await Spell.SelfBuff(S.ThunderFocusTea, () => Me.CurrentChi >= 3, "", true) && await Spell.SelfBuff(S.Uplift, () => true);
                }

                return await Spell.SelfBuff(S.Uplift, () => woWUnits.Count(t => t.HealthPercent() <= healthpct) >= 5);
            }
            #endregion

            #region ChiWave
            private static async Task<bool> ChiWave(WoWUnit onunit)
            {
                if (!TalentManager.IsSelected(4))
                    return false;

                var targets = HealManager.SmartTargets(MonkSettings.Instance.ChiWave).Count() + TargetManager.CountNear(onunit, 20);

                if (onunit == null || !onunit.IsValid)
                    return false;

                return await Spell.Heal(S.ChiWave, onunit, () => targets >= MonkSettings.Instance.ChiWaveCount);
            }
            #endregion

            #region ChiBurst
            private static async Task<bool> ChiBurst(WoWUnit onunit)
            {
                if (!TalentManager.IsSelected(6) || SpellManager.HasSpell(S.ChiBurst) || SpellManager.Spells["Chi Burst"].Cooldown)
                    return false;

                if (onunit == null || !onunit.IsValid)
                    return false;

                var pathcount = HealManager.Targets().Count(u => u.RelativeLocation.IsBetween(Me.Location, onunit.Location));
                var pathcount2 = HealManager.Targets().Count(u => u.X.IsBetween(Me.X, onunit.X) && u.Y.IsBetween(Me.Y, onunit.Y));

                var target = Units.GetPathUnits(onunit, Units.FriendlyUnitsNearTarget(40), 5);
                var bursttars = target as IList<WoWUnit> ?? target.ToList();
                var lastmofo = bursttars.OrderBy(u => u.Distance).FirstOrDefault();

                if (onunit.HealthPercent > MonkSettings.Instance.ChiBurst && pathcount >= MonkSettings.Instance.ChiBurstCount)
                {
                    await Movement.FaceTarget(onunit, 5);
                    Log.WriteLog(string.Format("Running Top Chi Burst Code"), Colors.Chartreuse);
                    await CommonCoroutines.SleepForLagDuration();
                    await Spell.Heal(S.ChiBurst, onunit, () => onunit.HealthPercent > MonkSettings.Instance.ChiBurst && pathcount >= MonkSettings.Instance.ChiBurstCount);
                }

                if (bursttars.Count(u => u.HealthPercent < MonkSettings.Instance.ChiBurst) >= MonkSettings.Instance.ChiBurstCount)
                {
                    await Movement.FaceTarget(lastmofo, 5);
                    Log.WriteLog(string.Format("Running Bottom Chi Burst Code"), Colors.Chartreuse);
                    await CommonCoroutines.SleepForLagDuration();
                    await Spell.Heal(S.ChiBurst, lastmofo);
                }

                return false;
            }
            #endregion

            #region SpinningCraneKick
            private async Task<bool> SpinningCraneKick()
            {
                if (!SpellManager.HasSpell("Spinning Crane Kick"))
                    return false;

                if (SpellManager.Spells["Spinning Crane Kick"].Cooldown || !Me.Combat)
                    return false;

                var totaltargets = SerpentStance ? HealManager.CountNearby(Me, 10f, MonkSettings.Instance.SpinningCraneKick) 
                                                : TargetManager.CountNear(Me, 8f);

                if (totaltargets < MonkSettings.Instance.SpinningCraneKickCount)
                    return false;

                await Spell.SelfBuff(S.SpinningCraneKick, () => TalentManager.IsSelected(16));
                await Spell.SelfBuff(S.SpinningCraneKick, () => StyxWoW.Me.ChanneledCastingSpellId != S.SpinningCraneKick && !TalentManager.IsSelected(16));

                return false;
            }
            #endregion

            #region RenewingMist
            private async Task<bool> RenewingMist()
            {
                if (!SpellManager.HasSpell("Renewing Mist"))
                    return false;

                if (SpellManager.Spells["Renewing Mist"].Cooldown)
                    return false;

                var needstoSpread = HealManager.InitialList.Where(hrm => hrm.HasAura("Renewing Mist") && hrm.GetAuraStackCount("Renewing Mist") == 3);
                var onunit = HealManager.SmartTargets(MonkSettings.Instance.RenewingMist).FirstOrDefault(st => !st.HasAura("Renewing Mist"));

                return await Spell.Heal(S.RenewingMist, onunit, () => onunit != null && !onunit.HasAura("Renewing Mist") && !needstoSpread.Any());
            }

            #endregion

            #region ZenSpheres

            private async Task<bool> ZenSpheres()
            {
                if (!SpellManager.HasSpell("Zen Sphere"))
                    return false;

                if (!SpellManager.CanCast("Zen Sphere"))
                    return false;

                var onunit = HealManager.SmartTarget(MonkSettings.Instance.ZenSphere);

                return await Spell.Buff(S.ZenSphere, onunit, () => TalentManager.IsSelected(5) && onunit != null && onunit.Combat &&
                                                                   onunit.IsValid &&  onunit.HasAura(S.ZenSphere) && TargetManager.CountNear(onunit, 10) +
                                                                   HealManager.CountNearby(onunit, 10, MonkSettings.Instance.ZenSphere) >= 3);
            }

            #endregion

            #region EnvelopingMists

            private async Task<bool> EnvelopingMists()
            {
                var onunit = HealManager.SmartTarget(MonkSettings.Instance.EnvelopingMist);

                if (onunit == null || !onunit.IsValid || onunit.HasAura(S.EnvelopingMistBuff) || Me.CurrentChi < 3 ||
                    Me.ChanneledCastingSpellId != S.SoothingMist || Me.ChannelObjectGuid != onunit.Guid)
                    return false;

                return await Spell.Heal(S.EnvelopingMist, onunit);
            }

            #endregion

            #region SoothingMist

            private async Task<bool> SoothingMist()
            {
                WoWUnit onunit = HealManager.SmartTarget(MonkSettings.Instance.SoothingMist);

                if (onunit == null || !onunit.IsValid || CraneStance)
                {
                    return false;
                }

                return await Spell.Heal("Soothing Mist", onunit);
            }

            #endregion

            #region SurgingMists

            private async Task<bool> SurgingMists()
            {
                var onunit = HealManager.SmartTarget(MonkSettings.Instance.SurgingMist);

                if (onunit == null)
                    return false;

                if (!SerpentStance || Me.ChannelObject == null || Me.ChanneledCastingSpellId != S.SoothingMist || Me.ChannelObjectGuid != onunit.Guid)
                    return false;

                return await Spell.Heal(S.SurgingMist, onunit);
            }

            #endregion

            #region Detox (Needs work)

            private async Task<bool> Detox(WoWUnit onunit)
            {
                if (SpellManager.Spells["Detox"].Cooldown ||
                    MonkSettings.Instance.Detox == MonkSettings.DetoxBehaviour.Manually)
                    return false;

                if (MonkSettings.Instance.Detox == MonkSettings.DetoxBehaviour.OnCoolDown)
                    return await Spell.Heal(S.Detox, onunit);

                if (MonkSettings.Instance.Detox == MonkSettings.DetoxBehaviour.OnDebuff)
                    return await Spell.Heal(S.Detox, onunit, () => MonkSettings.Instance.DetoxBuff != "" && onunit.HasAura(MonkSettings.Instance.DetoxBuff));

                return false;
            }

            #endregion

            #region ChiBrew

            private async Task<bool> ChiBrew()
            {
                var currentChi = Me.CurrentChi;

                if (!TalentManager.IsSelected(9))
                    return false;

                return await Spell.SelfBuff(S.ChiBrew, () => Me.ManaPercent <= MonkSettings.Instance.ManaTea && Me.CurrentChi <= (Me.MaxChi - 2) &&
                            Me.GetAuraStackCount("Mana Tea") < 18, "", true) && await Coroutine.Wait(1000, () => Me.CurrentChi == (currentChi + 2) || Me.CurrentChi == Me.MaxChi);
            }

            #endregion

            #region ChiBrewFist

            private async Task<bool> ChiBrewFist()
            {
                var currentChi = Me.CurrentChi;

                if (!TalentManager.IsSelected(9))
                    return false;

                return await Spell.SelfBuff(S.ChiBrew, () => (!Me.HasAura("Crane's Zeal") || !Me.HasAura("Tiger Power") || !Me.CurrentTarget.HasAura(S.RisingSunKick)) && Me.CurrentChi <= (Me.MaxChi - 2) &&
                            Me.GetAuraStackCount("Mana Tea") < 18, "", true) && await Coroutine.Wait(1000, () => Me.CurrentChi == (currentChi + 2) || Me.CurrentChi == Me.MaxChi);
            }

            #endregion

            #region SerpentStatue

            private WoWPoint _statueLocation = new WoWPoint();
            internal IEnumerable<WoWUnit> PossibleStatueTargets { get { return HealManager.Tanks.Where(player => !player.IsMoving); } }
            private static WoWUnit MyStatue { get { return ObjectManager.GetObjectsOfType<WoWUnit>(true).FirstOrDefault(unit => unit != null && unit.Entry == 60849 && unit.Distance <= 20 && unit.CreatedByUnitGuid == Me.Guid); } }

            private async Task<bool> SerpentStatue(WoWUnit onunit, bool reqs)
            {
                if (!reqs)
                    return false;

                if (SpellManager.Spells["Summon Jade Serpent Statue"].Cooldown)
                    return false;

                if (!Me.Combat || Me.IsMoving)
                    return false;

                WoWPoint Location;
                if (Me.GroupInfo.IsInParty && onunit != null &&
                    (TargetManager.BossFight || TargetManager.CountNear(onunit, 8) > 2))
                {
                    if (onunit.IsMoving)
                        return false;

                    if (!onunit.InLineOfSpellSight)
                        return false;

                    Location = WoWMathHelper.CalculatePointFrom(Me.Location, onunit.Location, (float) onunit.Distance/2);

                    if ((Location.Distance(_statueLocation) <= 5 ||
                         !Navigator.CanNavigateFully(Me.Location, Location)) && MyStatue != null)
                        return false;

                    await Spell.SelfBuff("Summon Jade Serpent Statue");

                    if (await Coroutine.Wait(1000, () => Me.CurrentPendingCursorSpell != null))
                    {
                        SpellManager.ClickRemoteLocation(Location);
                        _statueLocation = Location;

                        if (await Coroutine.Wait(1000, () => MyStatue != null))
                        {
                            _statueLocation = Location;
                            return true;
                        }

                        //bool ClickRemoteLocation does not return correctly so for now working around it.
                        Lua.DoString("SpellStopTargeting()");
                        Spell.UpdateSpellHistory("Summon Jade Serpent Statue", 5000, Me);
                        Log.WriteLog("Failed to place Summon Jade Serpent Statue", Colors.Red);
                        return false;
                    }
                }
                if (!Me.GroupInfo.IsInParty || MyStatue == null)
                {
                    if (TargetManager.CountNear(Me, 20) < 1 ||
                        (MyStatue != null && Me.Location.Distance(_statueLocation) <= 10))
                        return false;

                    Location = WoWMathHelper.CalculatePointInFront(Me.Location, (float) Me.Rotation, 3f);

                    if (Navigator.CanNavigateFully(Me.Location, Location))
                    {
                        await Spell.SelfBuff("Summon Jade Serpent Statue");

                        if (await Coroutine.Wait(1000, () => Me.CurrentPendingCursorSpell != null))
                        {
                            SpellManager.ClickRemoteLocation(Location);
                            _statueLocation = Location;

                            if (await Coroutine.Wait(1000, () => MyStatue != null))
                            {
                                _statueLocation = Location;
                                return true;
                            }

                            //bool ClickRemoteLocation does not return correctly so for now working around it.
                            Lua.DoString("SpellStopTargeting()");
                            Spell.UpdateSpellHistory("Summon Jade Serpent Statue", 5000, Me);
                            Log.WriteLog("Failed to place Summon Jade Serpent Statue", Colors.Red);
                            return false;
                        }
                    }
                }
                return false;

            }

            #endregion

            #region StatueCluster

            private WoWUnit StatueCluster()
            {
                //Credit to ChinaJade for this
                //Start by getting a list of All Targets that are between 15 and 40yards from me
                var allMobCandidates = (from mob in HealManager.ValidList where mob.Distance.IsBetween(15, 40) select mob).ToList();

                //Next, we have to count the mobs within 20yard radius of each candidate:

                var selectedTarget = (from candidate in allMobCandidates
                        // NB: We use 'allMobCandidates' here...
                        // The attack only considers candidates with X range, but the attack itself
                        // is an AoE that can snag mobs outside of that range.
                        let mobsSurroundingCandidate =
                            allMobCandidates
                                .Where(u => candidate.Location.Distance(u.Location) <= 20)
                                .ToList()
                        orderby
                            // NB: Negate on purpose...
                            // This sorts the candidates with the most mobs surrounding them
                            // to the front of the list.
                            -mobsSurroundingCandidate.Count
                        select candidate)
                        .FirstOrDefault();
                return selectedTarget;
            }

            #endregion

            #region Manual Statue
            private async Task<bool> ManualStatue(bool reqs)
            {
                if (!reqs)
                    return false;

                if (!SpellManager.CanCast(S.SummonJadeSerpentStatue))
                    return false;

                if (!Lua.GetReturnVal<bool>("return IsLeftAltKeyDown() and not GetCurrentKeyBoardFocus()", 0))
                    return false;

                if (!SpellManager.Cast(S.SummonJadeSerpentStatue))
                    return false;

                if (!await Coroutine.Wait(1000, () => StyxWoW.Me.CurrentPendingCursorSpell != null))
                {
                    Log.WriteLog("Cursor Spell Didnt happen");
                    return false;
                }

                Lua.DoString("if SpellIsTargeting() then CameraOrSelectOrMoveStart() CameraOrSelectOrMoveStop() end");

                await CommonCoroutines.SleepForLagDuration();
                return true;
            }
            #endregion

        #endregion
    }
}
