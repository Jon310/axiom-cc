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

            //await ChiBrew();

            //H	17.63	elusive_brew,if=buff.elusive_brew_stacks.react>=9&(buff.dampen_harm.down|buff.diffuse_magic.down)&buff.elusive_brew_activated.down
            await Spell.Cast(S.ElusiveBrew, onunit, () => Me.HasAura("Elusive Brew", 9) && !Me.HasAura(ElusiveBrew) && IsCurrentTank());
            //I	0.00	invoke_xuen,if=talent.invoke_xuen.enabled&target.time_to_die>15&buff.shuffle.remains>=3&buff.serenity.down
            //J	0.00	serenity,if=talent.serenity.enabled&cooldown.keg_smash.remains>6
            await Spell.CoCast(S.Serenity, onunit, Me.CurrentChi >= 2 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 6 && Axiom.Burst);
            //L	0.67	touch_of_death,if=target.health.percent<10&cooldown.touch_of_death.remains=0&((!glyph.touch_of_death.enabled&chi>=3&target.time_to_die<8)|(glyph.touch_of_death.enabled&target.time_to_die<5))
            //M	0.00	call_action_list,name=st,if=active_enemies<3
            //N	0.00	call_action_list,name=aoe,if=active_enemies>=3
            
            if (await AOE(onunit, Units.EnemyUnitsSub10.Count() >= 3 && Axiom.AOE))
            {
                return true;
            }

            //ST Rot
            //actions.st=purifying_brew,if=!talent.chi_explosion.enabled&stagger.heavy
            await Spell.CoCast(S.PurifyingBrew, onunit, !TalentManager.IsSelected(20) && Me.HasAura("Heavy Stagger"));
            //actions.st+=/blackout_kick,if=buff.shuffle.down
            await Spell.Cast(S.BlackoutKick, onunit, () => !Me.HasAura("Shuffle"));
            //actions.st+=/purifying_brew,if=buff.serenity.up
            await Spell.CoCast(S.PurifyingBrew, onunit, Me.HasAura("Serenity") && (HasStagger(Stagger.Light) || HasStagger(Stagger.Medium) || HasStagger(Stagger.Heavy)));
            //actions.st+=/purifying_brew,if=!talent.chi_explosion.enabled&stagger.moderate&buff.shuffle.remains>=6
            await Spell.Cast(S.PurifyingBrew, onunit, () => !TalentManager.IsSelected(20) && HasStagger(Stagger.Medium) && HasShuffle());
            //actions.st+=/guard,if=(charges=1&recharge_time<5)|charges=2|target.time_to_die<15
            await Spell.Cast(S.Guard, onunit, () => Me.CurrentChi >= 2 && Me.HealthPercent <= 80 && IsCurrentTank() && !Me.HasAura(S.Guard) && Spell.GetCharges(S.Guard) == 2);
            //actions.st+=/guard,if=incoming_damage_10s>=health.max*0.5
            //actions.st+=/chi_brew,if=target.health.percent<10&cooldown.touch_of_death.remains=0&chi<=3&chi>=1&(buff.shuffle.remains>=6|target.time_to_die<buff.shuffle.remains)&!glyph.touch_of_death.enabled
            //actions.st+=/touch_of_death,if=target.health.percent<10&(buff.shuffle.remains>=6|target.time_to_die<=buff.shuffle.remains)&!glyph.touch_of_death.enabled
            await Spell.CoCast(S.TouchofDeath, onunit, SpellManager.CanCast(S.TouchofDeath) && Axiom.Burst && Me.HasAura("Death Note"));
            //actions.st+=/keg_smash,if=chi.max-chi>=1&!buff.serenity.remains
            await Spell.Cast(S.KegSmash, onunit, () => !Me.HasAura("Serenity") && Me.MaxChi - Me.CurrentChi >= 1);
            //actions.st+=/touch_of_death,if=target.health.percent<10&glyph.touch_of_death.enabled
            //actions.st+=/chi_burst,if=(energy+(energy.regen*gcd))<100
            //actions.st+=/chi_wave,if=(energy+(energy.regen*gcd))<100
            await Spell.Cast(S.ChiWave, onunit);
            //actions.st+=/zen_sphere,cycle_targets=1,if=talent.zen_sphere.enabled&!dot.zen_sphere.ticking&(energy+(energy.regen*gcd))<100
            await Spell.Cast(S.ZenSphere, Me, () => !Me.HasAura(S.ZenSphere));
            //actions.st+=/chi_explosion,if=chi>=3
            await Spell.Cast(S.ChiExplosion, onunit, () => Me.CurrentChi >= 3);
            //actions.st+=/blackout_kick,if=chi>=4
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 4 && !TalentManager.IsSelected(20));
            //actions.st+=/blackout_kick,if=buff.shuffle.remains<=3&cooldown.keg_smash.remains>=gcd
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 2 && !HasShuffle() || Me.HasAura("Serenity") && !TalentManager.IsSelected(20));
            //actions.st+=/blackout_kick,if=buff.serenity.up
            //actions.st+=/expel_harm,if=chi.max-chi>=1&cooldown.keg_smash.remains>=gcd&(energy+(energy.regen*(cooldown.keg_smash.remains)))>=80
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.MaxChi - Me.CurrentChi >= 1 && Me.HealthPercent <= 90 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1);
            //actions.st+=/jab,if=chi.max-chi>=1&cooldown.keg_smash.remains>=gcd&cooldown.expel_harm.remains>=gcd&(energy+(energy.regen*(cooldown.keg_smash.remains)))>=80
            await Spell.Cast(S.Jab, onunit, () => Me.MaxChi - Me.CurrentChi >= 1 && Me.CurrentEnergy >= 70 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1 && !Me.HasAura("Serenity"));
            //actions.st+=/tiger_palm
            await Spell.Cast(S.TigerPalm, onunit, () => !Me.HasAura("Serenity") || !Me.HasAura("Tiger Power"));

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
            await Spell.CoCast(S.PurifyingBrew, onunit, Me.HasAura("Serenity") && (HasStagger(Stagger.Light) || HasStagger(Stagger.Medium) || HasStagger(Stagger.Heavy)));
            await Spell.Cast(S.PurifyingBrew, onunit, () => !TalentManager.IsSelected(20) && HasStagger(Stagger.Medium) && HasShuffle());
            await Spell.Cast(S.Guard, onunit, () => Me.CurrentChi >= 2 && Me.HealthPercent <= 80 && IsCurrentTank() && !Me.HasAura(S.Guard) && Spell.GetCharges(S.Guard) == 2);
            await Spell.CoCast(S.TouchofDeath, onunit, SpellManager.CanCast(S.TouchofDeath) && Axiom.Burst && Me.HasAura("Death Note"));
            await Spell.Cast(S.KegSmash, onunit, () => !Me.HasAura("Serenity") && Me.MaxChi - Me.CurrentChi >= 2);
            await Spell.Cast(S.RushingJadeWind, () => !Me.HasAura("Serenity") && Me.MaxChi - Me.CurrentChi >= 1 && TalentManager.IsSelected(16));
            //actions.st+=/chi_burst,if=(energy+(energy.regen*gcd))<100
            await Spell.Cast(S.ChiWave, onunit);
            await Spell.Cast(S.ZenSphere, Me, () => !Me.HasAura(S.ZenSphere));
            await Spell.Cast(S.ChiExplosion, onunit, () => Me.CurrentChi >= 4);
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 4 && !TalentManager.IsSelected(20));
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 2 && !HasShuffle() || Me.HasAura("Serenity") && !TalentManager.IsSelected(20));
            //actions.st+=/expel_harm,if=chi.max-chi>=1&cooldown.keg_smash.remains>=gcd&(energy+(energy.regen*(cooldown.keg_smash.remains)))>=80
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.MaxChi - Me.CurrentChi >= 1 && Me.HealthPercent <= 90 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1);
            //actions.st+=/jab,if=chi.max-chi>=1&cooldown.keg_smash.remains>=gcd&cooldown.expel_harm.remains>=gcd&(energy+(energy.regen*(cooldown.keg_smash.remains)))>=80
            await Spell.Cast(S.Jab, onunit, () => Me.MaxChi - Me.CurrentChi >= 1 && Me.CurrentEnergy >= 70 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1 && !Me.HasAura("Serenity"));
            await Spell.Cast(S.TigerPalm, onunit, () => !Me.HasAura("Serenity") || !Me.HasAura("Tiger Power"));
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

            return true;
        }
        #endregion

        #region Spells

            #region Chi Brew

            private async Task<bool> ChiBrew()
            {
                if (SpellManager.Spells["Chi Brew"].Cooldown || !TalentManager.IsSelected(9))
                    return false;
                
                if (TalentManager.IsSelected(9) && Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.GetAuraStackCount("Elusive Brew") <= 10 &&
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
                    return Me.HasAura("Light Stagger");
                if (stagger == Stagger.Medium)
                    return Me.HasAura("Medium Stagger");
                if (stagger == Stagger.Heavy)
                    return Me.HasAura("Heavy Stagger");

                return false;
            }

            public enum Stagger
            {
                Light,
                Medium,
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

            #region Monk Spells
            private const int BlackoutKick = 100784,
                              BreathofFire = 115181,
                              ChiWave = 115098,
                              ElusiveBrew = 115308,
                              ExpelHarm = 115072,
                              Guard = 115295,
                              Jab = 100780,
                              KegSmash = 121253,
                              PurifyingBrew = 119582,
                              RushingJadeWind = 116847,
                              SpearHandStrike = 116705,
                              SpinningCraneKick = 101546,
                              StanceoftheSturdyOx = 115069,
                              SummonBlackOxStatue = 115315,
                              TigerPalm = 100787,
                              TouchofDeath = 115080,
                              ZenMeditation = 115176,
                              ZenSphere = 124081;
            #endregion

    }
}
