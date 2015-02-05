using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axiom.Helpers;
using Axiom.Managers;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Axiom.Class.Warrior
{
    class Arms : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.WarriorArms ? WoWClass.Warrior : WoWClass.None; } }
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
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            //await Spell.Cast("Colossus Smash", onunit);
            //await Spell.Cast("Mortal Strike", onunit);

            await Spell.Cast(VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));
            await Spell.Cast(EnragedRegeneration, onunit, () => Me.HealthPercent <= 50);
            await Spell.Cast(DieByTheSword, onunit, () => Me.HealthPercent <= 20);

            //await Item.CoUseHS(50);
            await Leap();

            await Spell.Cast(Recklessness, onunit, () => Axiom.Burst && (onunit.HasAura("Colossus Smash", true) || Me.HasAura("Bloodbath") || Me.CurrentTarget.HealthPercent < 20));
            await Spell.Cast(Avatar, onunit, () => Axiom.Burst && Me.HasAura("Recklessness"));
            await Spell.Cast(BloodBath, onunit, () => Axiom.Burst && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds < 5);

            await AOE(onunit, Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            await Spell.Cast(Rend, onunit, () => !Me.CurrentTarget.HasAura("Rend", true));
            await Spell.CastOnGround(Ravager, Me.CurrentTarget.Location, Spell.GetCooldownLeft("Colossus Smash").TotalSeconds < 4 && Axiom.AOE);
            await Spell.Cast(Bladestorm, onunit, () => Me.CurrentTarget.IsWithinMeleeRange && Axiom.AOE);
            await Spell.Cast(ColossusSmash, onunit, () => Me.CurrentTarget.HasAura("Rend", true));
            await Spell.Cast(MortalStrike, onunit, () => Me.CurrentTarget.HealthPercent > 20 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(StormBolt, onunit, () => (Me.CurrentTarget.HasAura("Colossus Smash", true) || Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 4) && Me.CurrentRage < 90);
            await Spell.Cast(Siegebreaker, onunit);
            await Spell.Cast(DragonRoar, onunit, () => !Me.CurrentTarget.HasAura("Colossus Smash", true) && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(Rend, onunit, () => Me.CurrentTarget.HasAuraExpired("Rend", 5) && !Me.CurrentTarget.HasAura("Colossus Smash", true));
            await Spell.Cast(Execute, onunit, () => Me.CurrentTarget.HasAura("Colossus Smash", true) || Me.HasAura(SuddenDeath) || Me.CurrentRage >= 60 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(ImpendingVictory, onunit, () => Me.CurrentTarget.HealthPercent > 20 && Me.CurrentRage < 40 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && Spell.GetCooldownLeft("Mortal Strike").TotalSeconds > 1);
            //await Spell.CoCast(ThunderClap, Unit.UnfriendlyUnits(8).Count() >= 3 && Clusters.GetCluster(Me, Unit.UnfriendlyUnits(8), ClusterType.Radius, 8).Any(u => !u.HasAura("Deep Wounds")));
            await Spell.Cast(Whirlwind, onunit, () => Me.CurrentTarget.HealthPercent > 20 && (Me.CurrentRage > 40 || Me.CurrentTarget.HasAura("Colossus Smash", true)) && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && Spell.GetCooldownLeft("Mortal Strike").TotalSeconds > 1 && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(HeroicThrow, onunit);


            //await Spell.Cast("Colossus Smash", onunit, () => Me.RagePercent > 30);
            //await Spell.Cast("Mortal Strike", onunit);
            

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

            await Spell.Cast(SweepingStrikes, onunit, () => Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            await Spell.Cast(Rend, onunit, () => !Me.CurrentTarget.HasAura("Rend", true));
            await Spell.Cast(Bladestorm, onunit, () => Me.CurrentTarget.IsWithinMeleeRange && Axiom.AOE);
            await Spell.Cast(ColossusSmash, onunit, () => Me.CurrentTarget.HasAura("Rend", true));
            await Spell.Cast(MortalStrike, onunit, () => Me.CurrentTarget.HealthPercent > 20 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1.5 && Units.EnemyUnitsSub8.Count() <= 5);
            await Spell.Cast(Execute, onunit, () => Me.HasAura(SuddenDeath) || Me.CurrentTarget.HasAura("Colossus Smash", true) || Me.CurrentRage >= 60 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(DragonRoar, onunit, () => !Me.CurrentTarget.HasAura("Colossus Smash", true) && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(Whirlwind, onunit, () => Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && (Me.CurrentTarget.HealthPercent >= 20 || Units.EnemyUnitsSub8.Count() > 9));
            await Spell.Cast(Rend, onunit, () => Me.CurrentTarget.HasAuraExpired("Rend", 6));
            await Spell.Cast(StormBolt, onunit, () => (Me.CurrentTarget.HasAura("Colossus Smash", true) || Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 4) && Me.CurrentRage < 90);
            await Spell.Cast(HeroicThrow, onunit);

            return true;
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
                          BerserkerRage = 18499,
                          Charge = 100,
                          Cleave = 845,
                          ColossusSmash = 167105,
                          CommandingShout = 469,
                          DemoralizingBanner = 114203,
                          DieByTheSword = 118038,
                          DragonRoar = 118000,
                          Enrage = 12880,
                          EnragedRegeneration = 55694,
                          Execute = 163201,
                          HeroicLeap = 6544,
                          HeroicStrike = 78,
                          HeroicThrow = 57755,
                          ImpendingVictory = 103840,
                          MockingBanner = 114192,
                          MortalStrike = 12294,
                          Overpower = 7384,
                          Ravager = 152277,
                          Recklessness = 1719,
                          Rend = 772,
                          Siegebreaker = 176289,
                          ShatteringThrow = 64382,
                          Shockwave = 46968,
                          SkullBanner = 114207,
                          Slam = 1464,
                          StormBolt = 107570,
                          SuddenDeath = 52437,
                          SweepingStrikes = 12328,
                          ThunderClap = 6343,
                          VictoryRush = 34428,
                          Whirlwind = 1680;
        #endregion

    }
}
