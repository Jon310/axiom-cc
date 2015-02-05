using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Axiom.Helpers;
using Axiom.Lists;
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
using S = Axiom.Lists.SpellList;

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

        private static async Task<bool> CombatCoroutine(WoWUnit onunit)
        {
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            if (GeneralSettings.Instance.Targeting)
                TargetManager.EnsureTarget(onunit);

            await Spell.Cast(S.VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));
            await Spell.Cast(S.EnragedRegeneration, onunit, () => Me.HealthPercent <= 50);
            await Spell.Cast(S.DieByTheSword, onunit, () => Me.HealthPercent <= 20);

            //await Item.CoUseHS(50);
            await Leap();

            await Spell.Cast(S.Recklessness, onunit, () => Axiom.Burst && (onunit.HasAura("Colossus Smash", true) || Me.HasAura("Bloodbath") || Me.CurrentTarget.HealthPercent < 20));
            await Spell.Cast(S.Avatar, onunit, () => Axiom.Burst && Me.HasAura("Recklessness"));
            await Spell.Cast(S.BloodBath, onunit, () => Axiom.Burst && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds < 5);

            await AOE(onunit, Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            await Spell.Cast(S.Rend, onunit, () => !Me.CurrentTarget.HasAura("Rend", true));
            await Spell.CastOnGround(S.Ravager, Me.CurrentTarget.Location, Spell.GetCooldownLeft("Colossus Smash").TotalSeconds < 4 && Axiom.AOE);
            await Spell.Cast(S.Bladestorm, onunit, () => Me.CurrentTarget.IsWithinMeleeRange && Axiom.AOE);
            await Spell.Cast(S.ColossusSmash, onunit, () => Me.CurrentTarget.HasAura("Rend", true));
            await Spell.Cast(S.MortalStrike, onunit, () => Me.CurrentTarget.HealthPercent > 20 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(S.StormBolt, onunit, () => (Me.CurrentTarget.HasAura("Colossus Smash", true) || Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 4) && Me.CurrentRage < 90);
            await Spell.Cast(S.Siegebreaker, onunit);
            await Spell.Cast(S.DragonRoar, onunit, () => !Me.CurrentTarget.HasAura("Colossus Smash", true) && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(S.Rend, onunit, () => Me.CurrentTarget.HasAuraExpired("Rend", 5) && !Me.CurrentTarget.HasAura("Colossus Smash", true));
            await Spell.Cast(S.Execute, onunit, () => Me.CurrentTarget.HasAura("Colossus Smash", true) || Me.HasAura(S.SuddenDeath) || Me.CurrentRage >= 60 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(S.ImpendingVictory, onunit, () => Me.CurrentTarget.HealthPercent > 20 && Me.CurrentRage < 40 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && Spell.GetCooldownLeft("Mortal Strike").TotalSeconds > 1);
            //await Spell.CoCast(ThunderClap, Unit.UnfriendlyUnits(8).Count() >= 3 && Clusters.GetCluster(Me, Unit.UnfriendlyUnits(8), ClusterType.Radius, 8).Any(u => !u.HasAura("Deep Wounds")));
            await Spell.Cast(S.Whirlwind, onunit, () => Me.CurrentTarget.HealthPercent > 20 && (Me.CurrentRage > 40 || Me.CurrentTarget.HasAura("Colossus Smash", true)) && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && Spell.GetCooldownLeft("Mortal Strike").TotalSeconds > 1 && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(S.HeroicThrow, onunit);

            if (GeneralSettings.Instance.Movement)
                return await Movement.MoveToTarget(onunit);

            return false;
        }

        private static async Task<bool> BuffsCoroutine()
        {

            return false;
        }

        #region Coroutine AOE
        private static async Task<bool> AOE(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            await Spell.Cast(S.SweepingStrikes, onunit, () => Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            await Spell.Cast(S.Rend, onunit, () => !Me.CurrentTarget.HasAura("Rend", true));
            await Spell.Cast(S.Bladestorm, onunit, () => Me.CurrentTarget.IsWithinMeleeRange && Axiom.AOE);
            await Spell.Cast(S.ColossusSmash, onunit, () => Me.CurrentTarget.HasAura("Rend", true));
            await Spell.Cast(S.MortalStrike, onunit, () => Me.CurrentTarget.HealthPercent > 20 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1.5 && Units.EnemyUnitsSub8.Count() <= 5);
            await Spell.Cast(S.Execute, onunit, () => Me.HasAura(S.SuddenDeath) || Me.CurrentTarget.HasAura("Colossus Smash", true) || Me.CurrentRage >= 60 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(S.DragonRoar, onunit, () => !Me.CurrentTarget.HasAura("Colossus Smash", true) && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(S.Whirlwind, onunit, () => Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && (Me.CurrentTarget.HealthPercent >= 20 || Units.EnemyUnitsSub8.Count() > 9));
            await Spell.Cast(S.Rend, onunit, () => Me.CurrentTarget.HasAuraExpired("Rend", 6));
            await Spell.Cast(S.StormBolt, onunit, () => (Me.CurrentTarget.HasAura("Colossus Smash", true) || Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 4) && Me.CurrentRage < 90);
            await Spell.Cast(S.HeroicThrow, onunit);

            if (!GeneralSettings.Instance.Movement)
                return await Movement.MoveToTarget(onunit);

            return true;
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
