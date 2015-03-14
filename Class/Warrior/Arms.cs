using System.Linq;
using System.Threading.Tasks;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using JetBrains.Annotations;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellLists;

namespace Axiom.Class.Warrior
{
    [UsedImplicitly]
    class Arms : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.WarriorArms ? WoWClass.Warrior : WoWClass.None; } }
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

        #region CombatCoroutine
        private static async Task<bool> CombatCoroutine(WoWUnit onunit)
        {
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !onunit.IsAlive) return true;

            if (GeneralSettings.Instance.Targeting)
                TargetManager.EnsureTarget(onunit);

            await Spell.Cast(S.VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));
            await Spell.Cast(S.EnragedRegeneration, onunit, () => Me.HealthPercent <= 50);
            await Spell.Cast(S.DieByTheSword, onunit, () => Me.HealthPercent <= 20);

            //await Item.CoUseHS(50);
            await Leap();

            await Spell.Cast(S.Recklessness, onunit, () => Axiom.Burst && (onunit.HasAura("Colossus Smash", true) || Me.HasAura("Bloodbath") || onunit.HealthPercent < 20));
            await Spell.Cast(S.Avatar, onunit, () => Axiom.Burst && Me.HasAura("Recklessness"));
            await Spell.Cast(S.BloodBath, onunit, () => Axiom.Burst && (Spell.GetCooldownLeft("Colossus Smash").TotalSeconds < 5 || onunit.HasAura("Colossus Smash", true)));

            await AOE(onunit, Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            await Spell.Cast(S.Rend, onunit, () => !onunit.HasAura("Rend", true));
            await Spell.CastOnGround(S.Ravager, onunit, Spell.GetCooldownLeft("Colossus Smash").TotalSeconds < 4 && Axiom.AOE);
            await Spell.Cast(S.Bladestorm, onunit, () => onunit.IsWithinMeleeRange && Axiom.AOE);
            await Spell.Cast(S.ColossusSmash, onunit, () => onunit.HasAura("Rend", true));
            await Spell.Cast(S.MortalStrike, onunit, () => onunit.HealthPercent > 20);
            await Spell.Cast(S.StormBolt, onunit, () => (onunit.HasAura("Colossus Smash", true) || Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 4) && Me.CurrentRage < 90);
            await Spell.Cast(S.Siegebreaker, onunit);
            await Spell.Cast(S.DragonRoar, onunit, () => !onunit.HasAura("Colossus Smash", true) && onunit.Distance <= 8);
            await Spell.Cast(S.Rend, onunit, () => onunit.HasAuraExpired("Rend", 5) && !onunit.HasAura("Colossus Smash", true));
            await Spell.Cast(S.Execute, onunit, () => (onunit.HasAura("Colossus Smash", true) || Me.CurrentRage >= 60 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 || Me.HasAura(S.SuddenDeath)) && SpellManager.CanCast(S.Execute));
            await Spell.Cast(S.ImpendingVictory, onunit, () => onunit.HealthPercent > 20 && Me.CurrentRage < 40 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && Spell.GetCooldownLeft("Mortal Strike").TotalSeconds > 1);
            await Spell.Cast(S.Slam, onunit, () => (Me.CurrentRage > 20 || Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1.35) && onunit.HealthPercent > 20 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            //await Spell.CoCast(ThunderClap, Unit.UnfriendlyUnits(8).Count() >= 3 && Clusters.GetCluster(Me, Unit.UnfriendlyUnits(8), ClusterType.Radius, 8).Any(u => !u.HasAura("Deep Wounds")));
            await Spell.Cast(S.Whirlwind, onunit, () => !TalentManager.IsSelected(9) && (onunit.HealthPercent > 20 && (Me.CurrentRage > 40 || onunit.HasAura("Colossus Smash", true)) && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && Spell.GetCooldownLeft("Mortal Strike").TotalSeconds > 1 && onunit.Distance <= 8));
            await Spell.Cast(S.HeroicThrow, onunit);

            if (GeneralSettings.Instance.Movement)
                return await Movement.MoveToTarget(onunit);

            return false;
        }
        #endregion

        #region BuffsCoroutine

        private static async Task<bool> BuffsCoroutine()
        {
            await Spell.CoCast(S.BattleShout, !Me.HasPartyBuff(Units.Stat.AttackPower));
            return false;
        }

        #endregion

        #region Coroutine AOE
        private static async Task<bool> AOE(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            await Spell.Cast(S.SweepingStrikes, onunit, () => Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            await Spell.Cast(S.Rend, onunit, () => !onunit.HasAura("Rend", true));
            await Spell.Cast(S.Bladestorm, onunit, () => onunit.IsWithinMeleeRange && Axiom.AOE);
            await Spell.Cast(S.ColossusSmash, onunit, () => onunit.HasAura("Rend", true));
            await Spell.Cast(S.MortalStrike, onunit, () => onunit.HealthPercent > 20 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1.5 && Units.EnemyUnitsSub8.Count() <= 5);
            await Spell.Cast(S.Execute, onunit, () => (onunit.HasAura("Colossus Smash", true) || Me.CurrentRage >= 60 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 || Me.HasAura(S.SuddenDeath)) && SpellManager.CanCast(S.Execute));
            await Spell.Cast(S.DragonRoar, onunit, () => !onunit.HasAura("Colossus Smash", true) && onunit.Distance <= 8);
            await Spell.Cast(S.Whirlwind, onunit, () => Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && (onunit.HealthPercent >= 20 || Units.EnemyUnitsSub8.Count() > 9));
            await Spell.Cast(S.Rend, onunit, () => onunit.HasAuraExpired("Rend", 6));
            await Spell.Cast(S.StormBolt, onunit, () => (onunit.HasAura("Colossus Smash", true) || Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 4) && Me.CurrentRage < 90);
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
    }
}
