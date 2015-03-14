using System;
using System.Collections.Generic;
using System.Linq;
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
using MonkSettings = Axiom.Settings.Monk;


namespace Axiom.Class.Monk
{
    [UsedImplicitly]
    class Brewmaster : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.MonkBrewmaster ? WoWClass.Monk : WoWClass.None; } }

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
        protected override Composite CreateRest()
        {
            return new ActionRunCoroutine(ret => RestCoroutine());
        }
        #endregion

        #region CombatCoroutine
        private async Task<bool> CombatCoroutine(WoWUnit onunit)
        {
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            await ChiBrew();

            await Spell.Cast(S.ElusiveBrew, onunit, () => Me.HasAura("Elusive Brew", 9) && !Me.HasAura(S.ElusiveBrew) && IsCurrentTank());
            await Spell.CoCast(S.Serenity, onunit, Me.CurrentChi >= 2 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 6 && Axiom.Burst);
            
            await AOE(onunit, Units.EnemyUnitsSub10.Count() >= 3 && Axiom.AOE);

            //ST Rot
            await Spell.CoCast(S.PurifyingBrew, onunit, !Spell.HasSpell("Chi Explosion") && Me.HasAura("Heavy Stagger"));
            await Spell.Cast(S.BlackoutKick, onunit, () => !Me.HasAura("Shuffle") && Me.CurrentChi >= 2);
            await Spell.CoCast(S.PurifyingBrew, onunit, Me.HasAura("Serenity") && (HasStagger(Stagger.Light) || HasStagger(Stagger.Moderate) || HasStagger(Stagger.Heavy)));
            await Spell.Cast(S.PurifyingBrew, onunit, () => !Spell.HasSpell("Chi Explosion") && HasStagger(Stagger.Moderate) && HasShuffle());
            await Spell.Cast(S.Guard, onunit, () => Me.CurrentChi >= 2 && Me.HealthPercent <= 80 && IsCurrentTank() && !Me.HasAura(S.Guard) && Spell.GetCharges(S.Guard) == 2 && Axiom.AFK);
            //actions.st+=/chi_brew,if=target.health.percent<10&cooldown.touch_of_death.remains=0&chi<=3&chi>=1&(buff.shuffle.remains>=6|target.time_to_die<buff.shuffle.remains)&!glyph.touch_of_death.enabled
            
            await Spell.CoCast(S.TouchofDeath, onunit, SpellManager.CanCast(S.TouchofDeath) && Axiom.Burst && Me.HasAura("Death Note"));

            await Spell.Cast(S.KegSmash, onunit, () => !Me.HasAura("Serenity") && Me.ChiInfo.Max - Me.CurrentChi >= 1);
            //actions.st+=/chi_burst,if=(energy+(energy.regen*gcd))<100
            await Spell.Cast(S.ChiWave, onunit);
            await Spell.Cast(S.ZenSphere, Me, () => !Me.HasAura(S.ZenSphere));
            await Spell.Cast(S.ChiExplosionBM, onunit, () => Me.CurrentChi >= 3);
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 4 && !Spell.HasSpell("Chi Explosion"));
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 2 && !HasShuffle() || Me.HasAura("Serenity") && !Spell.HasSpell("Chi Explosion"));
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 1 && Me.HealthPercent <= 90 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1);
            //await Spell.Cast(S.Jab, onunit, () => Me.MaxChi - Me.CurrentChi >= 1 && (Me.CurrentEnergy >= 80 || Spell.GetCooldownLeft("Keg Smash").TotalSeconds >= 3) && !Me.HasAura("Serenity"));
            await Spell.CoCast(S.Jab, Me.ChiInfo.Max - Me.CurrentChi >= 1 && SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds >= 1
                    && (Me.CurrentEnergy + (EnergyRegen*(SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds))) >= 80);
            //await Spell.Cast(S.Jab, onunit, () => Me.MaxChi - Me.CurrentChi >= 1 && Me.CurrentEnergy >= 70 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1 && !Me.HasAura("Serenity"));

            await Spell.Cast(S.TigerPalm, onunit, () => (!Me.HasAura("Serenity") || !Me.HasAura("Tiger Power")) && !SpellManager.CanCast(S.KegSmash));

            return false;
        }
        #endregion

        #region RestCoroutine
        private async Task<bool> RestCoroutine()
        {
            if (Me.IsDead || SpellManager.GlobalCooldown)
                return false;

            if (!(Me.HealthPercent < 60) || Me.IsMoving || Me.IsCasting || Me.Combat || Me.HasAura("Food") ||
                Styx.CommonBot.Inventory.Consumable.GetBestFood(false) == null)
                return false;

            Styx.CommonBot.Rest.FeedImmediate();
            return await Coroutine.Wait(1000, () => Me.HasAura("Food"));
        }
        #endregion

        #region BuffsCoroutine
        private async Task<bool> BuffsCoroutine()
        {
            await Spell.CoCast(S.LegacyoftheEmperor, !Me.HasPartyBuff(Units.Stat.Stats) || !Me.HasPartyBuff(Units.Stat.CriticalStrike));
            return false;
        }
        #endregion

        #region Coroutine AOE
        private static async Task<bool> AOE(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            await Spell.CoCast(S.PurifyingBrew, onunit,  Me.HasAura("Heavy Stagger"));
            await Spell.Cast(S.BlackoutKick, onunit, () => !Me.HasAura("Shuffle"));
            await Spell.CoCast(S.PurifyingBrew, onunit, Me.HasAura("Serenity") && (HasStagger(Stagger.Light) || HasStagger(Stagger.Moderate) || HasStagger(Stagger.Heavy)));
            await Spell.Cast(S.PurifyingBrew, onunit, () => !Spell.HasSpell("Chi Explosion") && HasStagger(Stagger.Moderate) && HasShuffle());
            await Spell.Cast(S.Guard, onunit, () => Me.CurrentChi >= 2 && Me.HealthPercent <= 80 && IsCurrentTank() && !Me.HasAura(S.Guard) && Spell.GetCharges(S.Guard) == 2);
            await Spell.CoCast(S.TouchofDeath, onunit, SpellManager.CanCast(S.TouchofDeath) && Axiom.Burst && Me.HasAura("Death Note"));
            await Spell.Cast(S.KegSmash, onunit, () => !Me.HasAura("Serenity") && Me.ChiInfo.Max - Me.CurrentChi >= 1);
            await Spell.CoCast(S.RushingJadeWind, !Me.HasAura("Serenity") && Me.MaxChi - Me.CurrentChi >= 1 && Spell.HasSpell("Rushing Jade Wind") && !SpellManager.CanCast(S.KegSmash));
            //actions.st+=/chi_burst,if=(energy+(energy.regen*gcd))<100
            await Spell.Cast(S.ChiWave, onunit);
            await Spell.Cast(S.ZenSphere, Me, () => !Me.HasAura(S.ZenSphere));
            await Spell.Cast(S.ChiExplosionBM, onunit, () => Me.CurrentChi >= 4);
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 4 && !Spell.HasSpell("Chi Explosion"));
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 2 && !HasShuffle() || Me.HasAura("Serenity") && !Spell.HasSpell("Chi Explosion"));
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 1 && Me.HealthPercent <= 90 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1);

            await Spell.CoCast(S.Jab, Me.ChiInfo.Max - Me.CurrentChi >= 1 && SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds >= 1
                    && (Me.CurrentEnergy + (EnergyRegen * (SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds))) >= 80);

            await Spell.Cast(S.TigerPalm, onunit, () => (!Me.HasAura("Serenity") || !Me.HasAura("Tiger Power")) && !SpellManager.CanCast(S.KegSmash));
            //actions.aoe=purifying_brew,if=stagger.heavy
            //actions.aoe+=/blackout_kick,if=buff.shuffle.down
            //actions.aoe+=/purifying_brew,if=buff.serenity.up
            //actions.aoe+=/purifying_brew,if=!talent.chi_explosion.enabled&stagger.moderate&buff.shuffle.remains>=6
            //actions.aoe+=/guard,if=(charges=1&recharge_time<5)|charges=2|target.time_to_die<15
            //actions.aoe+=/guard,if=incoming_damage_10s>=health.max*0.5
            //actions.aoe+=/chi_brew,if=target.health.percent<10&cooldown.touch_of_death.remains=0&chi<=3&chi>=1&(buff.shuffle.remains>=6|target.time_to_die<buff.shuffle.remains)&!glyph.touch_of_death.enabled
            //actions.aoe+=/touch_of_death,if=target.health.percent<10&(buff.shuffle.remains>=6|target.time_to_die<=buff.shuffle.remains)&!glyph.touch_of_death.enabled
            //actions.aoe+=/breath_of_fire,if=(chi>=3|buff.serenity.up)&buff.shuffle.remains>=6&dot.breath_of_fire.remains<=2.4&!talent.chi_explosion.enabled
            //actions.aoe+=/keg_smash,if=chi.max-chi>=1&!buff.serenity.remains
            //actions.aoe+=/touch_of_death,if=target.health.percent<10&glyph.touch_of_death.enabled
            //actions.aoe+=/rushing_jade_wind,if=chi.max-chi>=1&!buff.serenity.remains&talent.rushing_jade_wind.enabled
            //actions.aoe+=/chi_burst,if=(energy+(energy.regen*gcd))<100
            //actions.aoe+=/chi_wave,if=(energy+(energy.regen*gcd))<100
            //actions.aoe+=/zen_sphere,cycle_targets=1,if=talent.zen_sphere.enabled&!dot.zen_sphere.ticking&(energy+(energy.regen*gcd))<100
            //actions.aoe+=/chi_explosion,if=chi>=4
            //actions.aoe+=/blackout_kick,if=chi>=4
            //actions.aoe+=/blackout_kick,if=buff.shuffle.remains<=3&cooldown.keg_smash.remains>=gcd
            //actions.aoe+=/blackout_kick,if=buff.serenity.up
            //actions.aoe+=/expel_harm,if=chi.max-chi>=1&cooldown.keg_smash.remains>=gcd&(energy+(energy.regen*(cooldown.keg_smash.remains)))>=80
            //actions.aoe+=/jab,if=chi.max-chi>=1&cooldown.keg_smash.remains>=gcd&cooldown.expel_harm.remains>=gcd&(energy+(energy.regen*(cooldown.keg_smash.remains)))>=80
            //actions.aoe+=/tiger_palm

            return false;
        }
        #endregion

        #region Spells

            #region Chi Brew

            private async Task<bool> ChiBrew()
            {
                if (!SpellManager.HasSpell(S.ChiBrew))
                    return false;

                if (SpellManager.Spells["Chi Brew"].Cooldown)
                    return false;

                if (SpellManager.HasSpell(S.ChiBrew) && Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.GetAuraStackCount("Elusive Brew") <= 10 &&
                    (Spell.GetCharges(S.ChiBrew) == 1 && SpellManager.Spells["Chi Brew"].CooldownTimeLeft.TotalSeconds < 5) ||
                    Spell.GetCharges(S.ChiBrew) == 2)
                {
                    await Spell.CoCast(S.ChiBrew);
                }

                if (Me.CurrentChi < 1 && HasStagger(Stagger.Heavy) || (Me.CurrentChi < 2 && !Me.HasAura("Shuffle")))
                {
                    await Spell.CoCast(S.ChiBrew);
                }

                return false;
            }

            #endregion

            #region Stagger

            public static bool HasStagger(Stagger stagger)
            {
                if (stagger == Stagger.Light)
                    return Me.HasAnyAura("Light Stagger", "Moderate Stagger", "Heavy Stagger");
                if (stagger == Stagger.Moderate)
                    return Me.HasAnyAura("Moderate Stagger", "Heavy Stagger");
                if (stagger == Stagger.Heavy)
                    return Me.HasAura("Heavy Stagger");

                return false;
            }

            public enum Stagger
            {
                Light,
                Moderate,
                Heavy
            }

            #endregion

        #endregion

        #region Shuffle
        static bool HasShuffle()
        {
            return Me.HasAura("Shuffle") && Me.GetAuraTimeLeft("Shuffle").TotalSeconds > 3;
        }
        #endregion
        
        #region Is Tank
        static bool IsCurrentTank()
        {
            return StyxWoW.Me.CurrentTarget.CurrentTargetGuid == StyxWoW.Me.Guid;
        }
        #endregion

        #region EnergyRegen
        private static double EnergyRegen
        {
            get
            {
                return Lua.GetReturnVal<float>("return GetPowerRegen()", 1);
            }
        }
        #endregion
    }
}
