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
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;



//6	3.02	invoke_xuen
//7	0.00	storm_earth_and_fire,target=2,if=debuff.storm_earth_and_fire_target.down
//8	0.00	storm_earth_and_fire,target=3,if=debuff.storm_earth_and_fire_target.down
//9	0.00	call_action_list,name=opener,if=talent.serenity.enabled&talent.chi_brew.enabled&cooldown.fists_of_fury.up&time<20

//G	6.97	chi_brew,if=chi.max-chi>=2&((charges=1&recharge_time<=10)|charges=2|target.time_to_die<charges*10)&buff.tigereye_brew.stack<=16
            await ChiBrew();
//H	21.18	tiger_palm,if=!talent.chi_explosion.enabled&buff.tiger_power.remains<6.6
//I	0.00	tiger_palm,if=talent.chi_explosion.enabled&(cooldown.fists_of_fury.remains<5|cooldown.fists_of_fury.up)&buff.tiger_power.remains<5
            await Spell.Cast(S.TigerPalm, onunit, () => (!TalentManager.IsSelected(20) && Me.HasAuraExpired("Tiger Power", 6)) || TalentManager.IsSelected(20) && (Spell.GetCooldownLeft("Keg Smash").TotalSeconds < 5 || !SpellManager.Spells["Fists of Fury"].Cooldown) && Me.HasAuraExpired("Tiger Power", 5));
//J	0.02	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack=20
//K	3.09	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&buff.serenity.up
//L	7.86	tigereye_brew,if=buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&cooldown.fists_of_fury.up&chi>=3&debuff.rising_sun_kick.up&buff.tiger_power.up
//M	0.00	tigereye_brew,if=talent.hurricane_strike.enabled&buff.tigereye_brew_use.down&buff.tigereye_brew.stack>=9&cooldown.hurricane_strike.up&chi>=3&debuff.rising_sun_kick.up&buff.tiger_power.up
//N	6.13	tigereye_brew,if=buff.tigereye_brew_use.down&chi>=2&(buff.tigereye_brew.stack>=16|target.time_to_die<40)&debuff.rising_sun_kick.up&buff.tiger_power.up
            await TigereyeBrew();

//O	1.95	rising_sun_kick,if=(debuff.rising_sun_kick.down|debuff.rising_sun_kick.remains<3)
            await Spell.Cast(S.RisingSunKick, onunit, () => Me.CurrentTarget.HasAuraExpired("Rising Sun Kick", 3));
//P	4.37	serenity,if=chi>=2&buff.tiger_power.up&debuff.rising_sun_kick.up
            await Spell.Cast(S.Serenity, onunit, () => Me.HasAura("Tiger Power") && Me.CurrentTarget.HasAura("Rising Sun Kick") && Axiom.Burst);
//Q	15.14	fists_of_fury,if=buff.tiger_power.remains>cast_time&debuff.rising_sun_kick.remains>cast_time&energy.time_to_max>cast_time&!buff.serenity.up

//S	1.00	touch_of_death,if=target.health.percent<10&(glyph.touch_of_death.enabled|chi>=3)
            await Spell.CoCast(S.TouchofDeath, onunit, SpellManager.CanCast(S.TouchofDeath) && Axiom.Burst && Me.HasAura("Death Note"));
//T	0.00	hurricane_strike,if=energy.time_to_max>cast_time&buff.tiger_power.remains>cast_time&debuff.rising_sun_kick.remains>cast_time&buff.energizing_brew.down
//U	6.15	energizing_brew,if=cooldown.fists_of_fury.remains>6&(!talent.serenity.enabled|(!buff.serenity.remains&cooldown.serenity.remains>4))&energy+energy.regen<50
            await Spell.Cast(S.EnergizingBrew, onunit, () => Spell.GetCooldownLeft("Fists of Fury").TotalSeconds > 6 && (!TalentManager.IsSelected(21) || !Me.HasAura(S.Serenity) && Spell.GetCooldownLeft("Serenity").TotalSeconds > 4) && Me.CurrentEnergy + EnergyRegen < 50);

            await stchix(onunit, Units.EnemyUnitsSub8.Count() < 3 && !TalentManager.IsSelected(20));
//W	0.00	call_action_list,name=st_chix,if=active_enemies=1&talent.chi_explosion.enabled
            await stchix(onunit, Units.EnemyUnitsSub8.Count() == 1 && TalentManager.IsSelected(20));
//X	0.00	call_action_list,name=cleave_chix,if=(active_enemies=2|active_enemies=3)&talent.chi_explosion.enabled
            await Cleave(onunit, (Units.EnemyUnitsSub8.Count() == 2 || Units.EnemyUnitsSub8.Count() == 3) && Axiom.AOE);
//Y	0.00	call_action_list,name=aoe_norjw,if=active_enemies>=3&!talent.rushing_jade_wind.enabled&!talent.chi_explosion.enabled
            await AOEnorjw(Units.EnemyUnitsSub8.Count() >= 3 && !TalentManager.IsSelected(16) && !TalentManager.IsSelected(20) && Axiom.AOE);
//Z	0.00	call_action_list,name=aoe_norjw_chix,if=active_enemies>=4&!talent.rushing_jade_wind.enabled&talent.chi_explosion.enabled
            await AOErjwchix(onunit, Units.EnemyUnitsSub8.Count() >= 4 && !TalentManager.IsSelected(16) && TalentManager.IsSelected(20) && Axiom.AOE);
//a	0.00	call_action_list,name=aoe_rjw,if=active_enemies>=3&talent.rushing_jade_wind.enabled
            await Aoerjw(onunit, Units.EnemyUnitsSub8.Count() >= 3 && TalentManager.IsSelected(16) && !TalentManager.IsSelected(20) && Axiom.AOE);

            


            await AOE(onunit, Units.EnemyUnitsSub10.Count() >= 3 && Axiom.AOE);

            await Spell.Cast(S.BlackoutKick, onunit, () => !Me.HasAura("Shuffle"));



            await Spell.Cast(S.ChiWave, onunit);

            await Spell.Cast(S.ZenSphere, Me, () => !Me.HasAura(S.ZenSphere));

            await Spell.Cast(S.ChiExplosion, onunit, () => Me.CurrentChi >= 3);

            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 4 && !TalentManager.IsSelected(20));

            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 2 && Me.HasAura("Serenity") && !TalentManager.IsSelected(20));

            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 1 && Me.HealthPercent <= 90 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1);

            await Spell.CoCast(S.Jab, Me.ChiInfo.Max - Me.CurrentChi >= 1 && SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds >= 1
                    && (Me.CurrentEnergy + (EnergyRegen*(SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds))) >= 80);

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


            await Spell.Cast(S.BlackoutKick, onunit, () => !Me.HasAura("Shuffle"));

            await Spell.CoCast(S.TouchofDeath, onunit, SpellManager.CanCast(S.TouchofDeath) && Axiom.Burst && Me.HasAura("Death Note"));
            await Spell.Cast(S.RushingJadeWind, () => !Me.HasAura("Serenity") && Me.MaxChi - Me.CurrentChi >= 1 && TalentManager.IsSelected(16));
            //actions.st+=/chi_burst,if=(energy+(energy.regen*gcd))<100
            await Spell.Cast(S.ChiWave, onunit);
            await Spell.Cast(S.ZenSphere, Me, () => !Me.HasAura(S.ZenSphere));
            await Spell.Cast(S.ChiExplosion, onunit, () => Me.CurrentChi >= 4);
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 4 && !TalentManager.IsSelected(20));
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 2 && !HasShuffle() || Me.HasAura("Serenity") && !TalentManager.IsSelected(20));
            await Spell.Cast(S.ExpelHarm, onunit, () => Me.ChiInfo.Max - Me.CurrentChi >= 1 && Me.HealthPercent <= 90 && Spell.GetCooldownLeft("Keg Smash").TotalSeconds > 1);

            await Spell.CoCast(S.Jab, Me.ChiInfo.Max - Me.CurrentChi >= 1 && SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds >= 1
                    && (Me.CurrentEnergy + (EnergyRegen * (SpellManager.Spells["Keg Smash"].CooldownTimeLeft.TotalSeconds))) >= 80);

            await Spell.Cast(S.TigerPalm, onunit, () => !Me.HasAura("Serenity") || !Me.HasAura("Tiger Power"));


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
                
                if (TalentManager.IsSelected(9) && Me.ChiInfo.Max - Me.CurrentChi >= 2 && Me.GetAuraStackCount("Tigereye Brew") <= 16 &&
                    (Spell.GetCharges(S.ChiBrew) == 1 && SpellManager.Spells["Chi Brew"].CooldownTimeLeft.TotalSeconds < 10) ||
                    Spell.GetCharges(S.ChiBrew) == 2)
                {
                    await Spell.CoCast(S.ChiBrew);
                }

                //if (Me.CurrentChi < 1 && HasStagger(Stagger.Heavy) || (Me.CurrentChi < 2 && !Me.HasAura("Shuffle")))
                //{
                //    await Spell.CoCast(S.ChiBrew);
                //}

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
                    Me.GetAuraStackCount("Tigereye Brew") >= 9 && !SpellManager.Spells["Fists of Fury"].Cooldown ))
                {
                    await Spell.CoCast(S.ChiBrew);
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
        #endregion
    }
}
