using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellLists;

namespace Axiom.Class.Warrior
{
    class Protection : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.WarriorProtection ? WoWClass.Warrior : WoWClass.None; } }
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
        #endregion

        private static async Task<bool> CombatCoroutine(WoWUnit target)
        {
            //if (GeneralSettings.Instance.Targeting)
            //    TargetManager.EnsureTarget(target);

            if (Axiom.PvPRotation || GeneralSettings.Instance.PvP)
            {
                await PvP(target);
                return true;
            }

            if (Me.HasAura("Gladiator Stance"))
            {
                await Glad(target);
                return true;
            }

            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

           
            await Spell.Cast(S.BloodBath, target, () => Axiom.Burst && target.IsWithinMeleeRange);
            await Spell.Cast(S.Avatar, target, () => Axiom.Burst && target.IsWithinMeleeRange);

            await Leap();
            await DropMockingBanner();

            await Spell.Cast(S.VictoryRush, target, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));
            await Spell.Cast(S.EnragedRegeneration, target, () => Me.HealthPercent <= 50);
            await Spell.Cast(S.LastStand, target, () => Me.HealthPercent <= 15 && !Me.HasAura("Shield Wall") && Axiom.AFK);
            await Spell.Cast(S.ShieldWall, target, () => Me.HealthPercent <= 30 && !Me.HasAura("Last Stand") && Axiom.AFK);
            await Spell.Cast(S.DemoralizingShout, target, () => Units.EnemyUnitsSub10.Any() && IsCurrentTank() && Me.HealthPercent <= 75);
            await Spell.Cast(S.ImpendingVictory, target, () => Me.HealthPercent <= 60);

            await Spell.Cast(S.ShieldBlock, target, () => !Me.HasAura(S.Ravager) && IsCurrentTank());
            await Spell.Cast("Shield Barrier", target, () => (Me.CurrentRage >= 85) && !Me.HasAura("Shield Barrier") && IsCurrentTank());

            await Spell.Cast(S.HeroicStrike, target, () => Me.CurrentRage > Me.MaxRage - (30 + Me.GetAuraStackCount("Unyielding Strikes") * 5) && (Axiom.Weave || !IsCurrentTank()));
            //await Spell.Cast(S.HeroicStrike, onunit, () => Me.CurrentRage > Me.MaxRage - (30 - Unit.buffStackCount(169685, Me) * 5));
            //await Spell.Cast(S.HeroicStrike, target, () => Me.GetAuraStackCount("Unyielding Strikes") >= 2 && Me.CurrentRage > 85 && Axiom.Weave);
            await Spell.Cast(S.HeroicStrike, target, () => Me.HasAura(S.Ultimatum) || Me.HasAura("Unyielding Strikes", 6));
            //await Spell.Cast(S.HeroicStrike, target, () => (Me.HasAura(S.Ultimatum) || Me.HasAura("Unyielding Strikes", 6) || (Me.CurrentRage > Me.MaxRage - 30 && !IsCurrentTank())) && SpellManager.CanCast("Heroic Strike"));

            await Spell.CastOnGround(S.Ravager, target, Axiom.Burst && target.IsWithinMeleeRange);
            await Spell.Cast(S.DragonRoar, target, () => target.IsWithinMeleeRange && Axiom.Burst);
            await Spell.Cast(S.StormBolt, target);            

            await Spell.CoCast(S.ShieldSlam, target);
            await Spell.CoCast(S.Revenge, target, Me.CurrentRage < 90);

            await Spell.Cast(S.Execute, target, () => (Me.HasAura("Sudden Death") || target.HealthPercent < 20) && Me.CurrentRage > Me.MaxRage - 30 && SpellManager.CanCast(S.Execute));
            await Spell.Cast(S.Devastate, target, () => Me.HasAuraExpired("Unyielding Strikes", 2) && !Me.HasAura("Unyielding Strikes", 6));
            await AOE(target, Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            //await Spell.Cast(S.HeroicThrow, target);

            await Spell.Cast(S.Devastate, target);

            return false;
        }

        private async Task<bool> BuffsCoroutine()
        {

            return false;
        }

        #region Coroutine AOE
        private static async Task<bool> AOE(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            await Spell.Cast(S.Shockwave, onunit, () => Units.EnemyUnitsCone(Me, Units.EnemyUnits(10), 9).Count() >= 3);
            await Spell.Cast(S.Bladestorm, onunit, () => onunit.IsWithinMeleeRange);
            await Spell.Cast(S.ThunderClap, onunit);

            return true;
        }
        #endregion

        #region PvP

        private static async Task<bool> PvP(WoWUnit onunit)
        {

            await Leap();
            await DropMockingBanner();

            if (Me.CurrentTarget.HasAnyAura("Ice Block", "Hand of Protection", "Divine Shield", "Deterrence") || !Me.Combat || Me.Mounted) return true;

            if (StyxWoW.Me.CurrentTarget != null && (!StyxWoW.Me.CurrentTarget.IsWithinMeleeRange || StyxWoW.Me.IsCasting || SpellManager.GlobalCooldown)) return true;

            await Spell.Cast(S.VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));

            await Spell.Cast("Intervene", BestBanner);

            await StormBoltFocus();

            await Spell.Cast("Intervene", BestInterveneTarget);
            //await Spell.CoCast(S.MassSpellReflection, Me.CurrentTarget.IsCasting && Me.CurrentTarget.Distance > 10);
            //await Spell.CoCast(S.ShieldWall, Me.HealthPercent < 40);
            //await Spell.CoCast(S.LastStand, Me.CurrentTarget.HealthPercent > Me.HealthPercent && Me.HealthPercent < 60);
            //await Spell.CoCast(S.DemoralizingShout, Unit.EnemyUnitsSub10.Count() >= 3);
            await Spell.Cast(S.ShieldBarrier, onunit, () => Me.HealthPercent < 40 && Me.CurrentRage >= 100);
            //await Spell.CoCast(S.BerserkerRage, Me.HasAuraWithMechanic(WoWSpellMechanic.Fleeing));
            await Spell.Cast(S.EnragedRegeneration, onunit, () => Me.HealthPercent <= 35);

            if (Me.CurrentTarget.IsWithinMeleeRange && Axiom.Burst)
            {
                await Spell.Cast(S.Avatar, onunit);
                await Spell.Cast(S.BloodBath, onunit);
                await Spell.Cast(S.Bladestorm, onunit);
            }

            await Spell.Cast(S.ShieldCharge, onunit, () => (!Me.HasAura("Shield Charge") && SpellManager.Spells["Shield Slam"].Cooldown) || Spell.GetCharges(S.ShieldCharge) > 1);
            //await Spell.CoCast(S.HeroicStrike, Me.HasAura("Shield Charge") || Me.HasAura("Ultimatum") || Me.CurrentRage >= 90 || Me.HasAura("Unyielding Strikes", 5));

            await Spell.Cast(S.HeroicStrike, onunit, () => (Me.HasAura("Sheld Charge") || (Me.HasAura("Unyielding Strikes") && Me.CurrentRage >= 50 - Me.GetAuraStackCount("Unyielding Strikes") * 5)) && Me.CurrentTarget.HealthPercent > 20);
            await Spell.Cast(S.HeroicStrike, onunit, () => Me.HasAura("Ultimatum") || Me.CurrentRage >= Me.MaxRage - 20 || Me.HasAura("Unyielding Strikes", 5));

            await Spell.Cast(S.ShieldSlam, onunit);
            await Spell.Cast(S.Revenge, onunit);
            await Spell.CastOnGround(S.Ravager, onunit, Burst && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(S.DragonRoar, onunit, () => Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(S.Execute, onunit, () => Me.HasAura("Sudden Death"));
            await Spell.Cast(S.ThunderClap, onunit, () => Axiom.AOE && Units.EnemyUnitsSub8.Count(u => !u.HasAura("Deep Wounds")) >= 1 && Units.EnemyUnitsSub8.Count() >= 2);

            await Spell.Cast(S.Execute, onunit, () => Me.CurrentRage > 60 && SpellManager.CanCast(S.Execute));
            await Spell.Cast(S.Devastate, onunit);

            return true;
        }
        #endregion

        #region Glad

        private static async Task<bool> Glad(WoWUnit onunit)
        {
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            await Spell.Cast(S.MassSpellReflection, () => onunit.IsCasting && onunit.Distance > 10);
            await Spell.Cast(S.ShieldWall, () => Me.HealthPercent < 40);
            await Spell.Cast(S.LastStand, () => onunit.HealthPercent > Me.HealthPercent && Me.HealthPercent < 60);
            await Spell.Cast(S.DemoralizingShout, () => Units.EnemyUnitsSub10.Count() >= 3);
            await Spell.Cast(S.ShieldBarrier, onunit, () => Me.HealthPercent < 40 && Me.CurrentRage >= 100);
            await Spell.Cast(S.VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));
            //await Spell.CoCast(S.BerserkerRage, Me.HasAuraWithMechanic(WoWSpellMechanic.Fleeing));
            await Spell.Cast(S.EnragedRegeneration, onunit, () => Me.HealthPercent <= 50);

            await Leap();

            if (onunit.IsWithinMeleeRange && Axiom.Burst)
            {
                await Spell.Cast(S.Avatar, onunit);
                await Spell.Cast(S.BloodBath, onunit);
                await Spell.Cast(S.Bladestorm, onunit);
            }

            await Spell.Cast(S.ShieldCharge, onunit, () => ((!Me.HasAura("Shield Charge") && !SpellManager.Spells["Shield Slam"].Cooldown) || Spell.GetCharges(S.ShieldCharge) == 2) && SpellManager.CanCast("Shield Charge"));

            await Spell.Cast(S.HeroicStrike, onunit, () => (Me.HasAura("Sheld Charge") || (Me.HasAura("Unyielding Strikes") && Me.CurrentRage >= 50 - Me.GetAuraStackCount("Unyielding Strikes") * 5)) && Me.CurrentTarget.HealthPercent > 20 && SpellManager.CanCast("Heroic Strike"));
            await Spell.Cast(S.HeroicStrike, onunit, () => (Me.HasAura("Ultimatum") || Me.CurrentRage >= Me.MaxRage - 20 || Me.HasAura("Unyielding Strikes", 5)) && SpellManager.CanCast("Heroic Strike"));

            await Spell.Cast(S.ShieldSlam, onunit);
            await Spell.Cast(S.Revenge, onunit);
            await Spell.Cast(S.Execute, onunit, () => Me.HasAura("Sudden Death"));
            await Spell.Cast(S.StormBolt, onunit);
            await Spell.Cast(S.ThunderClap, onunit, () => Axiom.AOE && Units.EnemyUnitsSub8.Any(u => !u.HasAura("Deep Wounds")) && Units.EnemyUnitsSub8.Count() >= 2);
            await Spell.Cast(S.DragonRoar, onunit, () => onunit.Distance <= 8);
            await Spell.Cast(S.ThunderClap, onunit, () => Axiom.AOE && Units.EnemyUnitsSub8.Count() >= 6);
            await Spell.Cast(S.Execute, onunit, () => Me.CurrentRage > 60 && SpellManager.CanCast(S.Execute));
            await Spell.Cast(S.Devastate, onunit);

            return true;
        }

        #endregion

        #region IsCurrentTank()

        private static bool IsCurrentTank()
        {
            return StyxWoW.Me.CurrentTarget.CurrentTargetGuid == StyxWoW.Me.Guid;
        }

        #endregion
        
        #region DefCools

        private static bool DefCools
        {
            get
            {
                return Me.HasAura("Shield Block") || Me.HasAura(S.Ravager) || Me.HasAura(S.LastStand) ||
                       Me.HasAura(S.ShieldWall) || Me.CurrentTarget.HasAura("Demoralizing Shout", true);
            }
        }

        #endregion

        #region Leap
        private static async Task<bool> Leap()
        {
            if (!SpellManager.CanCast(S.HeroicLeap))
                return false;

            if (!Lua.GetReturnVal<bool>("return IsLeftAltKeyDown() and not GetCurrentKeyBoardFocus()", 0))
                return false;

            if (!SpellManager.Cast(S.HeroicLeap))
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

        #region Mocking Banner
        private static async Task<bool> DropMockingBanner()
        {
            if (!SpellManager.CanCast(S.MockingBanner))
                return false;

            if (!KeyboardPolling.IsKeyDown(Keys.G))
                return false;

            if (!SpellManager.Cast(S.MockingBanner))
                return false;

            if (!await Coroutine.Wait(1000, () => StyxWoW.Me.CurrentPendingCursorSpell != null))
            {
                Logging.Write("Cursor Spell Didnt happen");
                return false;
            }

            Lua.DoString("if SpellIsTargeting() then CameraOrSelectOrMoveStart() CameraOrSelectOrMoveStop() end");

            await CommonCoroutines.SleepForLagDuration();
            return true;
        }
        #endregion

        #region Freedoms

        private static bool Freedoms
        {
            get
            {
                return Me.CurrentTarget.HasAnyAura("Hand of Freedom", "Ice Block", "Hand of Protection", "Divine Shield",
                    "Cyclone", "Deterrence", "Phantasm", "Windwalk Totem");
            }
        }

        #endregion

        #region Best Banner
        public static WoWUnit BestBanner//WoWUnit
        {
            get
            {
                if (!StyxWoW.Me.GroupInfo.IsInParty)
                    return null;
                if (StyxWoW.Me.GroupInfo.IsInParty)
                {
                    var closePlayer = Units.FriendlyUnitsNearTarget(6f).OrderBy(t => t.DistanceSqr).FirstOrDefault(t => t.IsAlive);
                    if (closePlayer != null)
                        return closePlayer;
                    var bestBan = (from unit in ObjectManager.GetObjectsOfType<WoWUnit>(false)
                                   //where (unit.Equals(59390) || unit.Equals(59398))
                                   //where unit.Guid.Equals(59390) || unit.Guid.Equals(59398)
                                   where unit.Entry.Equals(59390) || unit.Entry.Equals(59398)
                                   //where (unit.Guid == 59390 || unit.Guid == 59398) 
                                   where unit.InLineOfSight
                                   select unit).FirstOrDefault();
                    return bestBan;
                }
                return null;
            }
        }
        #endregion

        #region Coroutine Stormbolt Focus

        private static async Task<bool> StormBoltFocus()
        {
            KeyboardPolling.IsKeyDown(Keys.C);
            if (SpellManager.CanCast("Storm Bolt") && KeyboardPolling.IsKeyDown(Keys.C))
            {
                await Spell.Cast(S.StormBolt, Me.FocusedUnit);
            }

            return false;
        }

        #endregion

        #region Best Intervene
        public static WoWUnit BestInterveneTarget
        {
            get
            {
                if (!StyxWoW.Me.GroupInfo.IsInParty)
                    return null;
                if (StyxWoW.Me.GroupInfo.IsInParty)
                {
                    var bestTank = HealManager.Tanks.OrderBy(t => t.DistanceSqr).FirstOrDefault(t => t.IsAlive);
                    if (bestTank != null)
                        return bestTank;
                    var bestInt = (from unit in ObjectManager.GetObjectsOfType<WoWPlayer>(false)
                                   where unit.IsAlive
                                   where unit.HealthPercent <= 30
                                   where unit.IsInMyPartyOrRaid
                                   where unit.IsPlayer
                                   where !unit.IsHostile
                                   where unit.InLineOfSight
                                   select unit).FirstOrDefault();
                    return bestInt;
                }
                return null;
            }
        }
        #endregion

        #region IsGlobalCooldown
        public static bool IsGlobalCooldown(bool faceDuring = false, bool allowLagTollerance = true)
        {
            uint latency = allowLagTollerance ? StyxWoW.WoWClient.Latency : 0;
            TimeSpan gcdTimeLeft = SpellManager.GlobalCooldownLeft;
            return gcdTimeLeft.TotalMilliseconds > latency;
        }
        #endregion

        #region WarriorTalents
        enum WarriorTalents
        {
            None = 0,
            Juggernaut,
            DoubleTime,
            Warbringer,
            EnragedRegeneration,
            SecondWind,
            ImpendingVictory,
            StaggeringShout,
            PiercingHowl,
            DisruptingShout,
            Bladestorm,
            Shockwave,
            DragonRoar,
            MassSpellReflection,
            Safeguard,
            Vigilance,
            Avatar,
            Bloodbath,
            StormBolt
        }
        #endregion
        
    }
}
