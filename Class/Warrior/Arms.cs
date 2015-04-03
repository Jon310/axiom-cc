using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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

            if (Axiom.PvPRotation)
            {
                await PvPCoroutine(onunit);
                return true;
            }

            //if (GeneralSettings.Instance.Targeting)
            //    TargetManager.EnsureTarget(onunit);

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

        #region PvP

        private static async Task<bool> PvPCoroutine(WoWUnit onunit)
        {
            await CoShatterBubbles();
            await Leap();
            await Spell.Cast(S.EnragedRegeneration, Me, () => Me.HealthPercent <= 35);

            if (Me.CurrentTarget.HasAnyAura("Ice Block", "Hand of Protection", "Divine Shield") || !Me.Combat || Me.Mounted) return true;

            await Spell.Cast(S.VictoryRush, onunit, () => Me.HealthPercent <= 90 && Me.HasAura("Victorious"));
            await Spell.Cast(S.Hamstring, onunit, () =>  !Freedoms && !Me.CurrentTarget.IsStunned() && !Me.CurrentTarget.IsCrowdControlled() && !Me.CurrentTarget.IsSlowed() && Me.CurrentTarget.IsPlayer);

            await Spell.Cast(S.Intervene, BestInterveneTarget);
            await CoStormBoltFocus();
            await CoShockwave();

            await Spell.Cast(S.ColossusSmash,onunit);

            await Spell.Cast(S.Bladestorm, onunit, () => Me.CurrentTarget.IsWithinMeleeRange && Me.CurrentTarget.HasAura("Colossus Smash", true) && Burst);
            await Spell.Cast(S.Recklessness, onunit, () => Burst && (Me.CurrentTarget.HasAura("Colossus Smash", true) || Me.HasAura("Bloodbath") || Me.CurrentTarget.HealthPercent < 20) && Me.CurrentTarget.IsWithinMeleeRange);
            await Spell.Cast(S.Avatar, onunit, () => Burst && Me.HasAura("Recklessness"));
            await Spell.Cast(S.BloodBath, onunit, () => Burst && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds < 5 && Me.CurrentTarget.IsWithinMeleeRange);

            await Spell.CoCast(S.SweepingStrikes, onunit, Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);
            await Spell.Cast(S.Rend, onunit, () => !Me.CurrentTarget.HasAura("Rend"));
            await Spell.CoCast(S.Execute, Me.CurrentTarget.HasAura("Colossus Smash", true) || Me.HasAura(S.SuddenDeath) && SpellManager.CanCast(S.Execute) || Me.CurrentRage >= 60 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(S.MortalStrike, onunit, () => Me.CurrentTarget.HealthPercent > 20 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(S.DragonRoar, onunit, () => !Me.CurrentTarget.HasAura("Colossus Smash", true) && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(S.Rend, onunit, () => Me.CurrentTarget.HasAuraExpired("Rend", 5) && !Me.CurrentTarget.HasAura("Colossus Smash", true));
            await Spell.Cast(S.Slam, onunit, () => (Me.CurrentRage > 20 || Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1.35) && onunit.HealthPercent > 20 && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1);
            await Spell.Cast(S.Whirlwind, onunit, () => !TalentManager.IsSelected(9) && (Me.CurrentTarget.HealthPercent > 20 && ((Me.CurrentRage > Me.MaxRage - 30 || Me.CurrentTarget.HasAura("Colossus Smash", true)) && Spell.GetCooldownLeft("Colossus Smash").TotalSeconds > 1 && Spell.GetCooldownLeft("Mortal Strike").TotalSeconds > 1) && Me.CurrentTarget.Distance <= 8));
            await Spell.Cast(S.HeroicThrow, onunit);


            return true;
        }
        #endregion

        #region Pvp Stuff
        private static bool Freedoms
        {
            get
            {
                return Me.CurrentTarget.HasAnyAura("Hand of Freedom", "Ice Block", "Hand of Protection", "Divine Shield", "Cyclone", "Deterrence", "Phantasm", "Windwalk Totem");
            }
        }

        #region Coroutine Stormbolt Focus && Shockwave

        private static async Task<bool> CoStormBoltFocus()
        {
            KeyboardPolling.IsKeyDown(Keys.C);
            if (SpellManager.CanCast("Storm Bolt") && KeyboardPolling.IsKeyDown(Keys.C))
            {
                await Spell.CoCast(S.StormBolt, Me.FocusedUnit);
            }

            return false;
        }

        private static async Task<bool> CoShockwave()
        {
            KeyboardPolling.IsKeyDown(Keys.C);
            if (SpellManager.CanCast("Shockwave") && KeyboardPolling.IsKeyDown(Keys.C) /*&& Clusters.GetClusterCount(Me, Unit.NearbyUnfriendlyUnits, ClusterType.Cone, 9) >= 1*/)
            {
                await Spell.CoCast(S.Shockwave);
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
                    var bestTank = Group.Tanks.OrderBy(t => t.DistanceSqr).FirstOrDefault(t => t.IsAlive);
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

        #region Coroutine Shatter Bubbles
        private static Task<bool> CoShatterBubbles()
        {
            return Spell.CoCast(S.ShatteringThrow,
                        Me.CurrentTarget.IsPlayer &&
                        Me.CurrentTarget.HasAnyAura("Ice Block", "Hand of Protection", "Divine Shield") &&
                        Me.CurrentTarget.InLineOfSight);
        }
        #endregion
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
