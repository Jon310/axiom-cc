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
    class Windwalker : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.MonkWindwalker ? WoWClass.Monk : WoWClass.None; } }

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
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive || Me.IsCasting || Me.IsChanneling) return true;

//6	3.02	invoke_xuen
//7	0.00	storm_earth_and_fire,target=2,if=debuff.storm_earth_and_fire_target.down
//8	0.00	storm_earth_and_fire,target=3,if=debuff.storm_earth_and_fire_target.down
//9	0.00	call_action_list,name=opener,if=talent.serenity.enabled&talent.chi_brew.enabled&cooldown.fists_of_fury.up&time<20

//G	6.97	chi_brew,if=chi.max-chi>=2&((charges=1&recharge_time<=10)|charges=2|target.time_to_die<charges*10)&buff.tigereye_brew.stack<=16
            await ChiBrew();
//H	21.18	tiger_palm,if=!talent.chi_explosion.enabled&buff.tiger_power.remains<6.6
//I	0.00	tiger_palm,if=talent.chi_explosion.enabled&(cooldown.fists_of_fury.remains<5|cooldown.fists_of_fury.up)&buff.tiger_power.remains<5
            await Spell.CoCast(S.TigerPalm, onunit, (!Spell.HasSpell("Chi Explosion") && Me.GetAuraTimeLeft("Tiger Power").TotalSeconds <= 6.6) || (Spell.HasSpell("Chi Explosion") && (Spell.GetCooldownLeft("Fist of Fury").TotalSeconds < 5 || !SpellManager.Spells["Fists of Fury"].Cooldown) && Me.GetAuraTimeLeft("Tiger Power").TotalSeconds < 5) || !Me.HasAura("Tiger Power"));
//J	0.02	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack=20
//K	3.09	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&buff.serenity.up
//L	7.86	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&cooldown.fists_of_fury.up&chi>=3&debuff.rising_sun_kick.up&buff.tiger_power.up
//M	0.00	tigereye_brew,if=talent.hurricane_strike.enabled&buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&cooldown.hurricane_strike.up&chi>=3&debuff.rising_sun_kick.up&buff.tiger_power.up
//N	6.13	tigereye_brew,if=buff.tigereye_brew_use.down&chi>=2&(buff.tigereye_brew.stack>=16|target.time_to_die<40)&debuff.rising_sun_kick.up&buff.tiger_power.up
            await TigereyeBrew();
            
//O	1.95	rising_sun_kick,if=(debuff.rising_sun_kick.down|debuff.rising_sun_kick.remains<3)
            await Spell.CoCast(S.RisingSunKick, onunit, Me.CurrentTarget.HasAuraExpired("Rising Sun Kick", 3) || !Me.CurrentTarget.HasAura("Rising Sun Kick", true));
//P	4.37	serenity,if=chi>=2&buff.tiger_power.up&debuff.rising_sun_kick.up
            await Spell.Cast(S.Serenity, onunit, () => Me.HasAura("Tiger Power") && onunit.HasAura("Rising Sun Kick") && Axiom.Burst);
//Q	15.14	fists_of_fury,if=buff.tiger_power.remains>cast_time&debuff.rising_sun_kick.remains>cast_time&energy.time_to_max>cast_time&!buff.serenity.up
            await Spell.CoCast(S.FistsofFury, onunit, Me.GetAuraTimeLeft("Tiger Power") >= Spell.GetSpellCastTime("Fist of Fury") && Me.CurrentTarget.GetAuraTimeLeft("Rising Sun Kick") >= Spell.GetSpellCastTime("Fist of Fury") && Me.CurrentEnergy + EnergyRegen < 80 && !Me.HasAura(S.Serenity));
//S	1.00	touch_of_death,if=target.health.percent<10&(glyph.touch_of_death.enabled|chi>=3)
            await Spell.CoCast(S.TouchofDeath, onunit, SpellManager.CanCast(S.TouchofDeath) && Axiom.Burst && Me.HasAura("Death Note"));
//T	0.00	hurricane_strike,if=energy.time_to_max>cast_time&buff.tiger_power.remains>cast_time&debuff.rising_sun_kick.remains>cast_time&buff.energizing_brew.down
//U	6.15	energizing_brew,if=cooldown.fists_of_fury.remains>6&(!talent.serenity.enabled|(!buff.serenity.remains&cooldown.serenity.remains>4))&energy+energy.regen<50
            await Spell.Cast(S.EnergizingBrew, onunit, () => Spell.GetCooldownLeft("Fists of Fury").TotalSeconds > 6 && (!Spell.HasSpell("Serenity") || !Me.HasAura(S.Serenity) && Spell.GetCooldownLeft("Serenity").TotalSeconds > 4) && Me.CurrentEnergy + EnergyRegen < 50);


            await st(onunit, Units.EnemyUnitsSub8.Count() < 3 && !Spell.HasSpell("Chi Explosion"));

//W	0.00	call_action_list,name=st_chix,if=active_enemies=1&talent.chi_explosion.enabled

            await stchix(onunit, Units.EnemyUnitsSub8.Count() == 1 && Spell.HasSpell("Chi Explosion"));

//X	0.00	call_action_list,name=cleave_chix,if=(active_enemies=2|active_enemies=3)&talent.chi_explosion.enabled
            await Cleave(onunit, (Units.EnemyUnitsSub8.Count() == 2 || Units.EnemyUnitsSub8.Count() == 3) && Spell.HasSpell("Chi Explosion") && Axiom.AOE);


//Y	0.00	call_action_list,name=aoe_norjw,if=active_enemies>=3&!talent.rushing_jade_wind.enabled&!talent.chi_explosion.enabled
            await AOEnorjw(onunit, Units.EnemyUnitsSub8.Count() >= 3 && !Spell.HasSpell("Rushing Jade Wind") && !Spell.HasSpell("Chi Explosion") && Axiom.AOE);


//Z	0.00	call_action_list,name=aoe_norjw_chix,if=active_enemies>=4&!talent.rushing_jade_wind.enabled&talent.chi_explosion.enabled
            await AOErjwchix(onunit, Units.EnemyUnitsSub8.Count() >= 4 && !Spell.HasSpell("Rushing Jade Wind") && Spell.HasSpell("Chi Explosion") && Axiom.AOE);


//a	0.00	call_action_list,name=aoe_rjw,if=active_enemies>=3&talent.rushing_jade_wind.enabled
            await Aoerjw(onunit, Units.EnemyUnitsSub8.Count() >= 3 && Spell.HasSpell("Rushing Jade Wind") && !Spell.HasSpell("Chi Explosion") && Axiom.AOE);

        
            //await Spell.CoCast(S.Jab, Me.ChiInfo.Max - Me.CurrentChi >= 1 && SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds >= 1
            //        && (Me.CurrentEnergy + (EnergyRegen*(SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds))) >= 80);




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

        #region Coroutine ST
        private async Task<bool> st(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            Log.WriteQuiet("st");
//actions.st=rising_sun_kick
            await Spell.CoCast(S.RisingSunKick, onunit);
//actions.st+=/blackout_kick,if=buff.combo_breaker_bok.react|buff.serenity.up
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.HasAura("Combo Breaker: Blackout Kick") || Me.HasAura(S.Serenity));
//actions.st+=/tiger_palm,if=buff.combo_breaker_tp.react&buff.combo_breaker_tp.remains<=2
            await Spell.CoCast(S.TigerPalm, onunit, Me.HasAura("Combo Breaker: Tiger Palm") && Me.HasAuraExpired("Combo Breaker: Tiger Palm", 2));
//actions.st+=/chi_wave,if=energy.time_to_max>2&buff.serenity.down
            await Spell.Cast(S.ChiWave, onunit, () => !Me.HasAura(S.Serenity) && Me.CurrentEnergy < 75);
//actions.st+=/chi_burst,if=energy.time_to_max>2&buff.serenity.down

//actions.st+=/zen_sphere,cycle_targets=1,if=energy.time_to_max>2&!dot.zen_sphere.ticking&buff.serenity.down
//actions.st+=/chi_torpedo,if=energy.time_to_max>2&buff.serenity.down
//actions.st+=/blackout_kick,if=chi.max-chi<2
            await Spell.Cast(S.BlackoutKick, onunit, () =>  Me.ChiInfo.Max - Me.CurrentChi < 2);
//actions.st+=/expel_harm,if=chi.max-chi>=2&health.percent<95
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.HealthPercent < 95);
//actions.st+=/jab,if=chi.max-chi>=2
            await Spell.CoCast(S.Jab, onunit, Me.ChiInfo.Max - Me.CurrentChi >= 2);
            await Spell.CoCast(S.TigerPalm, onunit, Me.HasAura("Combo Breaker: Tiger Palm"));
            return true;
        }
        #endregion

        #region Coroutine STChix
        private static async Task<bool> stchix(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            Log.WriteQuiet("stchix");
//.	5.09	chi_explosion,if=chi>=2&buff.combo_breaker_ce.react&cooldown.fists_of_fury.remains>2
            await Spell.CoCast(S.ChiExplosionWW, Me.CurrentChi >= 2 && Me.HasAura("Combo Breaker: Chi Explosion") && Spell.GetCooldownLeft("Fist of Fury").TotalSeconds > 2);
//.	1.69	tiger_palm,if=buff.combo_breaker_tp.react&buff.combo_breaker_tp.remains<=2
            await Spell.Cast(S.TigerPalm, onunit, () => Me.HasAura("Combo Breaker: Tiger Palm") && Me.GetAuraTimeLeft("Combo Breaker: Tiger Palm").TotalSeconds <= 2);
//.	19.23	rising_sun_kick
            await Spell.CoCast(S.RisingSunKick, onunit);
//.	0.00	chi_wave,if=energy.time_to_max>2
            await Spell.Cast(S.ChiWave, onunit, () => !Me.HasAura(S.Serenity) && Me.CurrentEnergy < 75);
//.	7.01	chi_burst,if=energy.time_to_max>2
//.	0.00	zen_sphere,cycle_targets=1,if=energy.time_to_max>2&!dot.zen_sphere.ticking
//.	9.78	tiger_palm,if=chi=4&!buff.combo_breaker_tp.react
            await Spell.Cast(S.TigerPalm, onunit, () => Me.CurrentChi >= 4 && !Me.HasAura("Combo Breaker: Tiger Palm"));
//.	15.21	chi_explosion,if=chi>=3&cooldown.fists_of_fury.remains>4
            await Spell.CoCast(S.ChiExplosionWW, Me.CurrentChi >= 3 && Spell.GetCooldownLeft("Fist of Fury").TotalSeconds > 4);
//.	0.00	chi_torpedo,if=energy.time_to_max>2
//.	0.83	expel_harm,if=chi.max-chi>=2&health.percent<95
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.HealthPercent < 95);
//.	62.69	jab,if=chi.max-chi>=2
            await Spell.CoCast(S.Jab, onunit, Me.ChiInfo.Max - Me.CurrentChi >= 2);
            await Spell.CoCast(S.TigerPalm, onunit, Me.HasAura("Combo Breaker: Tiger Palm"));
            return true;
        }
        #endregion

        #region Coroutine Cleave
        private static async Task<bool> Cleave(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            Log.WriteQuiet("cleave");
//actions.cleave_chix=chi_explosion,if=chi>=4&cooldown.fists_of_fury.remains>4
            await Spell.Cast(S.ChiExplosionWW, onunit, () => Me.CurrentChi >= 4 && Spell.GetCooldownLeft("Fist of Fury").TotalSeconds > 4);
//actions.cleave_chix+=/tiger_palm,if=buff.combo_breaker_tp.react&buff.combo_breaker_tp.remains<=2
            await Spell.Cast(S.TigerPalm, onunit, () => Me.HasAura("Combo Breaker: Tiger Palm") && Me.HasAuraExpired("Combo Breaker: Tiger Palm", 2));
//actions.cleave_chix+=/chi_wave,if=energy.time_to_max>2&buff.serenity.down
            await Spell.Cast(S.ChiWave, onunit, () => !Me.HasAura(S.Serenity) && Me.CurrentEnergy < 75);
//actions.cleave_chix+=/chi_burst,if=energy.time_to_max>2&buff.serenity.down
//actions.cleave_chix+=/zen_sphere,cycle_targets=1,if=energy.time_to_max>2&!dot.zen_sphere.ticking
//actions.cleave_chix+=/chi_torpedo,if=energy.time_to_max>2
//actions.cleave_chix+=/expel_harm,if=chi.max-chi>=2&health.percent<95
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.HealthPercent < 95);
//actions.cleave_chix+=/jab,if=chi.max-chi>=2
            await Spell.CoCast(S.Jab, onunit, Me.ChiInfo.Max - Me.CurrentChi >= 2);
            await Spell.Cast(S.TigerPalm, onunit, () => Me.HasAura("Combo Breaker: Tiger Palm"));
            return true;
        }
        #endregion

        #region Coroutine AOEnorjw
        private static async Task<bool> AOEnorjw(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            Log.WriteQuiet("AOEnorjw");
//actions.aoe=chi_explosion,if=chi>=4&cooldown.fists_of_fury.remains>4
            await Spell.CoCast(S.ChiExplosionWW, onunit, Me.CurrentChi >= 4 && Spell.GetCooldownLeft("Fist of Fury").TotalSeconds > 4);
//actions.aoe+=/rising_sun_kick,if=chi=chi.max
            await Spell.CoCast(S.RisingSunKick, onunit, Me.CurrentChi == Me.MaxChi);
//actions.aoe+=/chi_wave,if=energy.time_to_max>2&buff.serenity.down
            await Spell.Cast(S.ChiWave, onunit, () => !Me.HasAura(S.Serenity) && Me.CurrentEnergy < 75);
//actions.aoe+=/chi_burst,if=energy.time_to_max>2&buff.serenity.down
//actions.aoe+=/zen_sphere,cycle_targets=1,if=energy.time_to_max>2&!dot.zen_sphere.ticking
//actions.aoe+=/chi_torpedo,if=energy.time_to_max>2
//actions.aoe+=/spinning_crane_kick
            await Spell.Cast(S.SpinningCraneKick, onunit);
            await Spell.CoCast(S.TigerPalm, onunit, Me.HasAura("Combo Breaker: Tiger Palm"));
            return true;
        }
        #endregion
        
        #region Coroutine AOErjwchix
        private static async Task<bool> AOErjwchix(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            Log.WriteQuiet("AOErjwchix");
//actions.aoe_rjw=chi_explosion,if=chi>=4&cooldown.fists_of_fury.remains>4
            await Spell.CoCast(S.ChiExplosionWW, onunit, Me.CurrentChi >= 4 && Spell.GetCooldownLeft("Fist of Fury").TotalSeconds > 4);
//actions.aoe_rjw+=/rushing_jade_wind
            await Spell.Cast(S.RushingJadeWind, onunit);
//actions.aoe_rjw+=/chi_wave,if=energy.time_to_max>2&buff.serenity.down
            await Spell.Cast(S.ChiWave, onunit, () => !Me.HasAura(S.Serenity) && Me.CurrentEnergy < 75);
//actions.aoe_rjw+=/chi_burst,if=energy.time_to_max>2&buff.serenity.down
//actions.aoe_rjw+=/zen_sphere,cycle_targets=1,if=energy.time_to_max>2&!dot.zen_sphere.ticking
//actions.aoe_rjw+=/blackout_kick,if=buff.combo_breaker_bok.react|buff.serenity.up
//actions.aoe_rjw+=/tiger_palm,if=buff.combo_breaker_tp.react&buff.combo_breaker_tp.remains<=2
            await Spell.CoCast(S.TigerPalm, onunit, Me.HasAura("Combo Breaker: Tiger Palm") && Me.HasAuraExpired("Combo Breaker: Tiger Palm", 2));
//actions.aoe_rjw+=/blackout_kick,if=chi.max-chi<2&(cooldown.fists_of_fury.remains>3|!talent.rushing_jade_wind.enabled)
//actions.aoe_rjw+=/chi_torpedo,if=energy.time_to_max>2
//actions.aoe_rjw+=/expel_harm,if=chi.max-chi>=2&health.percent<95
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.HealthPercent < 95);
//actions.aoe_rjw+=/jab,if=chi.max-chi>=2
            await Spell.CoCast(S.Jab, onunit, Me.ChiInfo.Max - Me.CurrentChi >= 2);
            await Spell.CoCast(S.TigerPalm, onunit, Me.HasAura("Combo Breaker: Tiger Palm"));
            return true;
        }
        #endregion

        #region Coroutine Aoerjw
        private async Task<bool> Aoerjw(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;

            Log.WriteQuiet("Aoerjw");
            //actions.aoe_rjw+=/rushing_jade_wind
            await Spell.Cast(S.RushingJadeWind, onunit);
            //actions.aoe_rjw+=/chi_wave,if=energy.time_to_max>2&buff.serenity.down
            await Spell.Cast(S.ChiWave, onunit, () => !Me.HasAura(S.Serenity) && Me.CurrentEnergy < 75);
            //actions.aoe_rjw+=/chi_burst,if=energy.time_to_max>2&buff.serenity.down
            //actions.aoe_rjw+=/zen_sphere,cycle_targets=1,if=energy.time_to_max>2&!dot.zen_sphere.ticking
            //actions.aoe_rjw+=/blackout_kick,if=buff.combo_breaker_bok.react|buff.serenity.up
            await Spell.CoCast(S.BlackoutKick, onunit, Me.HasAura("Combo Breaker: Blackout Kick") || Me.HasAura(S.Serenity));
            //actions.aoe_rjw+=/tiger_palm,if=buff.combo_breaker_tp.react&buff.combo_breaker_tp.remains<=2
            await Spell.CoCast(S.TigerPalm, onunit, Me.HasAura("Combo Breaker: Tiger Palm") && Me.HasAuraExpired("Combo Breaker: Tiger Palm", 2));
            //actions.aoe_rjw+=/blackout_kick,if=chi.max-chi<2&(cooldown.fists_of_fury.remains>3|!talent.rushing_jade_wind.enabled)
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.ChiInfo.Max - Me.CurrentChi < 2 && Spell.GetCooldownLeft("Fist of Fury").TotalSeconds > 3);
            //actions.aoe_rjw+=/chi_torpedo,if=energy.time_to_max>2
            //actions.aoe_rjw+=/expel_harm,if=chi.max-chi>=2&health.percent<95
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.HealthPercent < 95);
            //actions.aoe_rjw+=/jab,if=chi.max-chi>=2
            await Spell.CoCast(S.Jab, onunit, Me.ChiInfo.Max - Me.CurrentChi >= 2);
            await Spell.CoCast(S.TigerPalm, onunit, Me.HasAura("Combo Breaker: Tiger Palm"));
            return true;
        }
        #endregion

        #region Chi Brew

        private async Task<bool> ChiBrew()
        {
            if (!SpellManager.HasSpell(S.ChiBrew))
                return false;

            if (SpellManager.Spells["Chi Brew"].Cooldown)
                return false;

            if (Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.GetAuraStackCount("Tigereye Brew") <= 16 &&
                (Spell.GetCharges(S.ChiBrew) == 1 && SpellManager.Spells["Chi Brew"].CooldownTimeLeft.TotalSeconds < 10) ||
                Spell.GetCharges(S.ChiBrew) == 2)
            {
                await Spell.CoCast(S.ChiBrew);
            }

            return false;
        }

        #endregion

        #region Tigereye Brew

        private async Task<bool> TigereyeBrew()
        {

//J	0.02	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack=20
//K	3.09	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&buff.serenity.up
//L	7.86	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&cooldown.fists_of_fury.up&chi>=3&debuff.rising_sun_kick.up&buff.tiger_power.up
//M	0.00	tigereye_brew,if=talent.hurricane_strike.enabled&buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&cooldown.hurricane_strike.up&chi>=3&debuff.rising_sun_kick.up&buff.tiger_power.up
//N	6.13	tigereye_brew,if=buff.tigereye_brew_use.down&chi>=2&(buff.tigereye_brew.stack>=16|target.time_to_die<40)&debuff.rising_sun_kick.up&buff.tiger_power.up
            if (!Me.HasAura(S.TigereyeBrew) && (Me.GetAuraStackCount("Tigereye Brew") == 20 ||
                Me.GetAuraStackCount("Tigereye Brew") >= 9 && Me.HasAura(S.Serenity) ||
                Me.GetAuraStackCount("Tigereye Brew") >= 9 && !SpellManager.Spells["Fists of Fury"].Cooldown && Me.CurrentChi >= 3 && Me.HasAura("Tiger Power") && Me.CurrentTarget.HasAura("Rising Sun Kick") ||
                Me.CurrentChi >= 2 && Me.HasAura("Tiger Power") && Me.CurrentTarget.HasAura("Rising Sun Kick") && Me.GetAuraStackCount("Tigereye Brew") >= 16))
            {
                await Spell.CoCast(S.TigereyeBrew);
            }

            return false;
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

        private static double TimeToMax
        {
            get
            {
                return (105 - Me.CurrentEnergy) * (1.0 / EnergyRegen);
            }
        }
        #endregion
    }
}
