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
using Action = Styx.TreeSharp.Action;

namespace Axiom.Class.Warrior
{
    class Protection : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.WarriorProtection ? WoWClass.Warrior : WoWClass.None; } }
        protected override Composite CreateCombat()
        {
            return new ActionRunCoroutine(ret => CombatCoroutine(TargetManager.MeleeTarget));
        }
        protected override Composite CreateBuffs()
        {
            return new ActionRunCoroutine(ret => BuffsCoroutine());
        }
        protected override Composite CreatePull()
        {
            return new ActionRunCoroutine(ret => CombatCoroutine(TargetManager.MeleeTarget));
        }
        #endregion

        private async Task<bool> CombatCoroutine(WoWUnit onunit)
        {
            if (GeneralSettings.Instance.Targeting)
                TargetManager.EnsureTarget(onunit);

            if (Axiom.PvPRotation || GeneralSettings.Instance.PvP)
            {
                await PvP(onunit);
                return true;
            }

            if (Me.HasAura("Gladiator Stance"))
            {
                await Glad(onunit);
                return true;
            }

            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            await Spell.Cast(BloodBath, onunit, () => Axiom.Burst && Me.CurrentTarget.IsWithinMeleeRange);
            await Spell.Cast(Avatar, onunit, () => Axiom.Burst && Me.CurrentTarget.IsWithinMeleeRange);

            await Leap();
            await DropMockingBanner();

            await Spell.Cast(VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));
            await Spell.Cast(EnragedRegeneration, onunit, () => Me.HealthPercent <= 50);
            await Spell.Cast(LastStand, onunit, () => Me.HealthPercent <= 15 && !Me.HasAura("Shield Wall") && Axiom.AFK);
            await Spell.Cast(ShieldWall, onunit, () => Me.HealthPercent <= 30 && !Me.HasAura("Last Stand") && Axiom.AFK);
            await Spell.Cast(DemoralizingShout, onunit, () => Units.EnemyUnitsSub10.Any() && IsCurrentTank() && Me.HealthPercent <= 75);
            await Spell.Cast(ImpendingVictory, onunit, () => Me.HealthPercent <= 60);
            
            //shield_block,if=!(debuff.demoralizing_shout.up|buff.ravager.up|buff.shield_wall.up|buff.last_stand.up|buff.enraged_regeneration.up|buff.shield_block.up)
            await Spell.Cast(ShieldBlock, onunit, () => !DefCools && IsCurrentTank());
            //shield_barrier,if=buff.shield_barrier.down&((buff.shield_block.down&action.shield_block.charges_fractional<0.75)|rage>=85)
            await Spell.Cast("Shield Barrier", onunit, () => (Me.CurrentRage >= 85 /*|| !SpellManager.CanCast(ShieldBlock) && Me.CurrentRage >= 85*/) && !Me.HasAura("Shield Barrier") && IsCurrentTank());

            //await Spell.CoCast(HeroicStrike, Me.CurrentRage > Me.MaxRage - (30 - Unit.buffStackCount(169685, Me) * 5));
            await Spell.Cast(HeroicStrike, onunit, () => Me.HasAura(Ultimatum) || Me.HasAura("Unyielding Strikes", 6) || (Me.CurrentRage > Me.MaxRage - 30 && !IsCurrentTank()));
            await Spell.CastOnGround(Ravager, onunit.Location, Axiom.Burst && Me.CurrentTarget.IsWithinMeleeRange);
            await Spell.Cast(DragonRoar, onunit, () => Me.CurrentTarget.IsWithinMeleeRange && Axiom.Burst);
            await Spell.Cast(StormBolt, onunit);            
            
            await Spell.Cast(ShieldSlam, onunit);
            await Spell.Cast(Revenge, onunit, () => Me.CurrentRage < 90);

            await Spell.Cast(Execute, onunit, () => Me.HasAura("Sudden Death") || Me.CurrentRage > Me.MaxRage - 30 && Axiom.Burst);
            await Spell.Cast(Devastate, onunit, () => Me.HasAuraExpired("Unyielding Strikes", 2) && !Me.HasAura("Unyielding Strikes", 6));
            await AOE(onunit, Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            await Spell.Cast(HeroicThrow, onunit);

            if (GeneralSettings.Instance.Movement)
                return await Movement.MoveToTarget(onunit);

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

            await Spell.Cast(Shockwave, onunit, () => Units.EnemyUnitsCone(Me, Units.EnemyUnits(10), 9).Count() >= 3);
            await Spell.Cast(Bladestorm, onunit, () => Me.CurrentTarget.IsWithinMeleeRange);
            await Spell.Cast(ThunderClap, onunit);

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

            await Spell.Cast(VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));

            await Spell.Cast("Intervene", BestBanner);

            await StormBoltFocus();

            await Spell.Cast("Intervene", BestInterveneTarget);
            //await Spell.CoCast(MassSpellReflection, Me.CurrentTarget.IsCasting && Me.CurrentTarget.Distance > 10);
            //await Spell.CoCast(ShieldWall, Me.HealthPercent < 40);
            //await Spell.CoCast(LastStand, Me.CurrentTarget.HealthPercent > Me.HealthPercent && Me.HealthPercent < 60);
            //await Spell.CoCast(DemoralizingShout, Unit.EnemyUnitsSub10.Count() >= 3);
            await Spell.Cast(ShieldBarrier, onunit, () => Me.HealthPercent < 40 && Me.CurrentRage >= 100);
            //await Spell.CoCast(BerserkerRage, Me.HasAuraWithMechanic(WoWSpellMechanic.Fleeing));
            await Spell.Cast(EnragedRegeneration, onunit, () => Me.HealthPercent <= 35);

            if (Me.CurrentTarget.IsWithinMeleeRange && Axiom.Burst)
            {
                await Spell.Cast(Avatar, onunit);
                await Spell.Cast(BloodBath, onunit);
                await Spell.Cast(Bladestorm, onunit);
            }

            await Spell.Cast(ShieldCharge, onunit, () => (!Me.HasAura("Shield Charge") && SpellManager.Spells["Shield Slam"].Cooldown) || Spell.GetCharges(ShieldCharge) > 1);
            //await Spell.CoCast(HeroicStrike, Me.HasAura("Shield Charge") || Me.HasAura("Ultimatum") || Me.CurrentRage >= 90 || Me.HasAura("Unyielding Strikes", 5));

            await Spell.Cast(HeroicStrike, onunit, () => (Me.HasAura("Sheld Charge") || (Me.HasAura("Unyielding Strikes") && Me.CurrentRage >= 50 - Me.GetAuraStackCount("Unyielding Strikes") * 5)) && Me.CurrentTarget.HealthPercent > 20);
            await Spell.Cast(HeroicStrike, onunit, () => Me.HasAura("Ultimatum") || Me.CurrentRage >= Me.MaxRage - 20 || Me.HasAura("Unyielding Strikes", 5));

            await Spell.Cast(ShieldSlam, onunit);
            await Spell.Cast(Revenge, onunit);
            await Spell.CastOnGround(Ravager, onunit.Location, Burst && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(DragonRoar, onunit, () => Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(Execute, onunit, () => Me.HasAura("Sudden Death"));
            await Spell.Cast(ThunderClap, onunit, () => Axiom.AOE && Units.EnemyUnitsSub8.Count(u => !u.HasAura("Deep Wounds")) >= 1 && Units.EnemyUnitsSub8.Count() >= 2);

            await Spell.Cast(Execute, onunit, () => Me.CurrentRage > 60 && Me.CurrentTarget.HealthPercent < 20);
            await Spell.Cast(Devastate, onunit);

            return true;
        }
        #endregion

        #region Glad

        private static async Task<bool> Glad(WoWUnit onunit)
        {
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            //await Spell.CoCast(MassSpellReflection, Me.CurrentTarget.IsCasting && Me.CurrentTarget.Distance > 10);
            //await Spell.CoCast(ShieldWall, Me.HealthPercent < 40);
            //await Spell.CoCast(LastStand, Me.CurrentTarget.HealthPercent > Me.HealthPercent && Me.HealthPercent < 60);
            //await Spell.CoCast(DemoralizingShout, Unit.EnemyUnitsSub10.Count() >= 3);
            await Spell.Cast(ShieldBarrier, onunit, () => Me.HealthPercent < 40 && Me.CurrentRage >= 100);
            await Spell.Cast(VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));
            //await Spell.CoCast(BerserkerRage, Me.HasAuraWithMechanic(WoWSpellMechanic.Fleeing));
            await Spell.Cast(EnragedRegeneration, onunit, () => Me.HealthPercent <= 50);

            await Leap();

            if (Me.CurrentTarget.IsWithinMeleeRange && Axiom.Burst)
            {
                await Spell.Cast(Avatar, onunit);
                await Spell.Cast(BloodBath, onunit);
                await Spell.Cast(Bladestorm, onunit);
            }

            await Spell.Cast(ShieldCharge, onunit, () => (!Me.HasAura("Shield Charge") && !SpellManager.Spells["Shield Slam"].Cooldown) || Spell.GetCharges(ShieldCharge) == 2);
            //await Spell.CoCast(HeroicStrike, Me.HasAura("Shield Charge") || Me.HasAura("Ultimatum") || Me.CurrentRage >= 90 || Me.HasAura("Unyielding Strikes", 5));

            await Spell.Cast(HeroicStrike, onunit, () => (Me.HasAura("Sheld Charge") || (Me.HasAura("Unyielding Strikes") && Me.CurrentRage >= 50 - Me.GetAuraStackCount("Unyielding Strikes") * 5)) && Me.CurrentTarget.HealthPercent > 20);
            await Spell.Cast(HeroicStrike, onunit, () => Me.HasAura("Ultimatum") || Me.CurrentRage >= Me.MaxRage - 20 || Me.HasAura("Unyielding Strikes", 5));

            await Spell.Cast(ShieldSlam, onunit);
            await Spell.Cast(Revenge, onunit);
            await Spell.Cast(Execute, onunit, () => Me.HasAura("Sudden Death"));
            await Spell.Cast(StormBolt, onunit);
            await Spell.Cast(ThunderClap, onunit, () => Axiom.AOE && Units.EnemyUnitsSub8.Any(u => !u.HasAura("Deep Wounds")) && Units.EnemyUnitsSub8.Count() >= 2);
            await Spell.Cast(DragonRoar, onunit, () => Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(ThunderClap, onunit, () => Axiom.AOE && Units.EnemyUnitsSub8.Count() >= 6);
            await Spell.Cast(Execute, onunit, () => Me.CurrentRage > 60 && Me.CurrentTarget.HealthPercent < 20);
            await Spell.Cast(Devastate, onunit);

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
                return Me.HasAura("Shield Block") || Me.HasAura(Ravager) || Me.HasAura(LastStand) ||
                       Me.HasAura(ShieldWall) || Me.CurrentTarget.HasAura("Demoralizing Shout", true);
            }
        }

        #endregion

        #region Leap
        private static async Task<bool> Leap()
        {
            if (!SpellManager.CanCast(HeroicLeap))
                return false;

            if (!Lua.GetReturnVal<bool>("return IsLeftAltKeyDown() and not GetCurrentKeyBoardFocus()", 0))
                return false;

            if (!SpellManager.Cast(HeroicLeap))
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
            if (!SpellManager.CanCast(MockingBanner))
                return false;

            if (!KeyboardPolling.IsKeyDown(Keys.G))
                return false;

            if (!SpellManager.Cast(MockingBanner))
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
                await Spell.Cast(StormBolt, Me.FocusedUnit);
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

        #region Warrior Spells
        private const int Avatar = 107574,
                          BattleShout = 6673,
                          Bladestorm = 46924,
                          BloodBath = 12292,
                          Bloodthirst = 23881,
                          BerserkerRage = 18499,
                          Charge = 100,
                          Cleave = 845,
                          ColossusSmash = 86346,
                          CommandingShout = 469,
                          DemoralizingBanner = 114203,
                          DemoralizingShout = 1160,
                          Devastate = 20243,
                          DieByTheSword = 118038,
                          DragonRoar = 118000,
                          Enrage = 12880,
                          EnragedRegeneration = 55694,
                          Execute = 5308,
                          HeroicLeap = 6544,
                          HeroicStrike = 78,
                          HeroicThrow = 57755,
                          ImpendingVictory = 103840,
                          LastStand = 12975,
                          MassSpellReflection = 114028,
                          MockingBanner = 114192,
                          MortalStrike = 12294,
                          Overpower = 7384,
                          RagingBlow = 85288,
                          RallyingCry = 97462,
                          Recklessness = 1719,
                          Ravager = 152277,
                          Revenge = 6572,
                          ShatteringThrow = 64382,
                          ShieldBarrier = 112048,
                          ShieldBlock = 2565,
                          ShieldCharge = 156321,
                          ShieldSlam = 23922,
                          ShieldWall = 871,
                          Shockwave = 46968,
                          SkullBanner = 114207,
                          Slam = 1464,
                          StormBolt = 107570,
                          SweepingStrikes = 12328,
                          ThunderClap = 6343,
                          Ultimatum = 122510,
                          UnyieldingStrikes = 169685,
                          VictoryRush = 34428,
                          Whirlwind = 1680,
                          WildStrike = 100130;
        #endregion

    }
}
