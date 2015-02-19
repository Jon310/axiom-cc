using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellLists;

namespace Axiom.Class.Paladin
{
    class Retribution : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.PaladinRetribution ? WoWClass.Paladin : WoWClass.None; } }
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

        private static async Task<bool> CombatCoroutine(WoWUnit onunit)
        {

            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            await Spell.Cast(S.AvengingWrath, onunit, () => Burst);
            await Spell.Cast(S.HolyAvenger, onunit, () => Me.HasAura(S.AvengingWrath) && Burst);
            await Spell.Cast(S.DivineShield, onunit, () => Me.HealthPercent <= 20 && Weave);

            await Spell.Cast(S.FlashofLight, FlashTarclutch, () => Me.HasAura("Selfless Healer", 3));

            //await Spell.Cast(S.SealofRighteousness, onunit, () => AOE && !Me.HasAura("Seal of Righteousness") && Units.EnemyUnitsSub8.Count() >= 4);
            //await Spell.Cast(S.SealofTruth, onunit, () => !Me.HasAura("Seal of Truth") && Units.EnemyUnitsSub8.Count() < 4);

            await AOE(onunit, Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE);

            await Spell.Cast(S.ExecutionSentence, onunit, () => Burst);
            await Spell.CastOnGround(S.LightsHammer, Me.Location, onunit.IsBoss && Axiom.AOE);
            await Spell.CoCast(S.DivineStorm, onunit, Axiom.AOE && Me.HasAura("Divine Crusader") && Me.HasAura("Final Verdict") && Me.CurrentTarget.Distance <= 8 && MaxHolyPower);
            //Divine Purpose	If Divine Purpose has less than three seconds in duration
            await Spell.CoCast(S.DivineStorm, onunit, Axiom.AOE && Me.HasAuraExpired("Divine Crusader", 3) && Me.CurrentTarget.Distance <= 8);
            await Spell.CoCast(S.TemplarsVerdict, onunit, MaxHolyPower || Me.HasAura("Divine Purpose"));

            await Spell.Cast(S.HammerofWrath, onunit, () => SpellManager.CanCast(S.HammerofWrath));
            await Spell.CoCast(S.Exorcism, onunit, Me.HasAura("Blazing Contempt") && Me.CurrentHolyPower < 3);
            
            await Spell.Cast(S.DivineStorm, onunit, () => Axiom.AOE && Me.HasAura("Divine Crusader") && Me.HasAura("Final Verdict") && onunit.Distance <= 8 && (Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35));

            await Spell.CoCast(S.TemplarsVerdict, onunit, Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35);
            await Spell.Cast(S.HammeroftheRighteous, onunit, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnitsSub8.Count() >= 4 && Axiom.AOE);
            await Spell.Cast(S.CrusaderStrike, onunit, () => Me.CurrentHolyPower <= 4);
            await Spell.Cast(S.Judgment, SecTar, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnits(15).Count() >= 2 && Me.HasAura("Glyph of Double Jeopardy"));
            await Spell.Cast(S.Judgment, onunit, () => Me.CurrentHolyPower <= 4);
            await Spell.Cast(S.DivineStorm, onunit, () => Axiom.AOE && Me.HasAura("Divine Crusader") && Me.HasAura("Final Verdict") && Me.CurrentTarget.Distance <= 8);
            await Spell.CoCast(S.TemplarsVerdict, onunit, Me.HasAura("Divine Purpose"));
            await Spell.CoCast(S.TemplarsVerdict, onunit, Me.CurrentHolyPower == 4);
            await Spell.CoCast(S.Exorcism, onunit, Me.CurrentHolyPower <= 4);
            await Spell.CoCast(S.TemplarsVerdict, onunit);
           
           

            //await Spell.Cast(S.SealofRighteousness, onunit, () => AOE && !Me.HasAura("Seal of Righteousness") && Units.EnemyUnitsSub8.Count() >= 4);
            //await Spell.Cast(S.SealofTruth, onunit, () => !Me.HasAura("Seal of Truth") && Units.EnemyUnitsSub8.Count() < 4);

            //
            //
            
            //await Spell.Cast(S.HammeroftheRighteous, onunit, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnitsSub8.Count() >= 4 && AOE);
            //await Spell.Cast(S.CrusaderStrike, onunit, () => Me.CurrentHolyPower <= 4);
            
            //await Spell.Cast(S.Judgment, SecTar, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnits(15).Count() >= 2 && Me.HasAura("Glyph of Double Jeopardy"));
            //await Spell.Cast(S.Judgment, onunit, () => Me.CurrentHolyPower <= 4);
            //await Spell.Cast(S.TemplarsVerdict, onunit);
            //await Spell.Cast(S.Exorcism, onunit, () => Me.CurrentHolyPower <= 4);
            //await Spell.Cast(S.HolyPrism, onunit);
        
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

//    5.57	divine_storm,if=buff.divine_crusader.react&(holy_power=5|buff.holy_avenger.up&holy_power>=3)&buff.final_verdict.up
            await Spell.CoCast(S.DivineStorm, onunit, Me.HasAura("Divine Crusader") && MaxHolyPower);
//{	0.00	divine_storm,if=buff.divine_crusader.react&(holy_power=5|buff.holy_avenger.up&holy_power>=3)&active_enemies=2&!talent.final_verdict.enabled
//|	10.35	divine_storm,if=(holy_power=5|buff.holy_avenger.up&holy_power>=3)&active_enemies=2&buff.final_verdict.up
//}	0.00	divine_storm,if=buff.divine_crusader.react&(holy_power=5|buff.holy_avenger.up&holy_power>=3)&(talent.seraphim.enabled&cooldown.seraphim.remains<gcd*4)
//~	0.00	templars_verdict,if=holy_power=5|buff.holy_avenger.up&holy_power>=3&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*4)
//!	0.00	templars_verdict,if=buff.divine_purpose.react&buff.divine_purpose.remains<3
//"	0.00	divine_storm,if=buff.divine_crusader.react&buff.divine_crusader.remains<3&!talent.final_verdict.enabled
//#	19.20	final_verdict,if=holy_power=5|buff.holy_avenger.up&holy_power>=3
//$	2.74	final_verdict,if=buff.divine_purpose.react&buff.divine_purpose.remains<3
            await Spell.CoCast(S.TemplarsVerdict, onunit, MaxHolyPower || Me.HasAura("Divine Purpose"));
//%	55.44	hammer_of_wrath
            await Spell.Cast(S.HammerofWrath, onunit, () => SpellManager.CanCast(S.HammerofWrath));
//&	0.00	judgment,if=talent.empowered_seals.enabled&seal.truth&buff.maraads_truth.remains<cooldown.judgment.duration
//'	0.00	judgment,if=talent.empowered_seals.enabled&seal.righteousness&buff.liadrins_righteousness.remains<cooldown.judgment.duration
//(	0.00	judgment,if=talent.empowered_seals.enabled&seal.righteousness&cooldown.avenging_wrath.remains<cooldown.judgment.duration
//)	0.00	exorcism,if=buff.blazing_contempt.up&holy_power<=2&buff.holy_avenger.down
            await Spell.Cast(S.Exorcism, onunit, () => Me.HasAura("Blazing Contempt") && !MaxHolyPower);
//*	0.00	seal_of_truth,if=talent.empowered_seals.enabled&buff.maraads_truth.down
//+	0.00	seal_of_truth,if=talent.empowered_seals.enabled&cooldown.avenging_wrath.remains<cooldown.judgment.duration&buff.liadrins_righteousness.remains>cooldown.judgment.duration
//,	0.00	seal_of_righteousness,if=talent.empowered_seals.enabled&buff.maraads_truth.remains>cooldown.judgment.duration&buff.liadrins_righteousness.down&!buff.avenging_wrath.up&!buff.bloodlust.up
//-	13.68	divine_storm,if=buff.divine_crusader.react&buff.final_verdict.up&(buff.avenging_wrath.up|target.health.pct<35)
            await Spell.CoCast(S.DivineStorm, onunit, Me.HasAura("Divine Crusader") && (Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35));
//:	17.24	divine_storm,if=active_enemies=2&buff.final_verdict.up&(buff.avenging_wrath.up|target.health.pct<35)
//;	32.12	final_verdict,if=buff.avenging_wrath.up|target.health.pct<35
            await Spell.CoCast(S.TemplarsVerdict, onunit, Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35);
//<	0.00	divine_storm,if=buff.divine_crusader.react&active_enemies=2&(buff.avenging_wrath.up|target.health.pct<35)&!talent.final_verdict.enabled
//=	0.00	templars_verdict,if=holy_power=5&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*3)
//>	0.00	templars_verdict,if=holy_power=4&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*4)
//?	0.00	templars_verdict,if=holy_power=3&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*5)
//@	0.00	crusader_strike,if=holy_power<5&talent.seraphim.enabled
//.	87.57	crusader_strike,if=holy_power<=3|(holy_power=4&target.health.pct>=35&buff.avenging_wrath.down)
            await Spell.Cast(S.CrusaderStrike, onunit, () => Me.CurrentHolyPower <= 3 || (Me.CurrentHolyPower == 4 && Me.CurrentTarget.HealthPercent >= 35 && !Me.HasAura(S.AvengingWrath)));
//.	0.00	divine_storm,if=buff.divine_crusader.react&(buff.avenging_wrath.up|target.health.pct<35)&!talent.final_verdict.enabled
//.	62.56	judgment,cycle_targets=1,if=last_judgment_target!=target&glyph.double_jeopardy.enabled&holy_power<5
            await Spell.Cast(S.Judgment, SecTar, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnits(15).Count() >= 2 && Me.HasAura("Glyph of Double Jeopardy"));
            await Spell.Cast(S.Judgment, onunit, () => Me.CurrentHolyPower <= 4);
//.	0.00	exorcism,if=glyph.mass_exorcism.enabled&active_enemies>=2&holy_power<5&!glyph.double_jeopardy.enabled
            await Spell.Cast(S.Exorcism, onunit, () => TalentManager.HasGlyph("Mass Exorcism") && Me.CurrentHolyPower < 5);
//.	0.00	judgment,if=holy_power<5&talent.seraphim.enabled
//.	0.24	judgment,if=holy_power<=3|(holy_power=4&cooldown.crusader_strike.remains>=gcd*2&target.health.pct>35&buff.avenging_wrath.down)
//.	8.80	divine_storm,if=buff.divine_crusader.react&buff.final_verdict.up
            await Spell.CoCast(S.DivineStorm, onunit, Me.HasAura("Divine Crusader") && Me.HasAura("Final Verdict"));
//.	6.50	divine_storm,if=active_enemies=2&holy_power>=4&buff.final_verdict.up
            await Spell.CoCast(S.DivineStorm, onunit, Me.CurrentHolyPower >=4 && Me.HasAura("Final Verdict"));
//.	5.62	final_verdict,if=buff.divine_purpose.react
            await Spell.CoCast(S.FinalVerdict, onunit, Me.HasAura("Divine Purpose") || Me.CurrentHolyPower >= 4);
//.	7.31	final_verdict,if=holy_power>=4
//.	0.00	divine_storm,if=buff.divine_crusader.react&active_enemies=2&holy_power>=4&!talent.final_verdict.enabled
//.	0.00	templars_verdict,if=buff.divine_purpose.react
//.	0.00	divine_storm,if=buff.divine_crusader.react&!talent.final_verdict.enabled
//.	0.00	templars_verdict,if=holy_power>=4&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*5)
//.	0.00	seal_of_truth,if=talent.empowered_seals.enabled&buff.maraads_truth.remains<cooldown.judgment.duration
//.	0.00	seal_of_righteousness,if=talent.empowered_seals.enabled&buff.liadrins_righteousness.remains<cooldown.judgment.duration&!buff.bloodlust.up
//.	0.00	exorcism,if=holy_power<5&talent.seraphim.enabled
            await Spell.CoCast(S.Exorcism, onunit, Me.CurrentHolyPower <= 4);
//.	10.89	exorcism,if=holy_power<=3|(holy_power=4&(cooldown.judgment.remains>=gcd*2&cooldown.crusader_strike.remains>=gcd*2&target.health.pct>35&buff.avenging_wrath.down))
//.	1.00	divine_storm,if=active_enemies=2&holy_power>=3&buff.final_verdict.up
            await Spell.CoCast(S.DivineStorm, onunit, Me.CurrentHolyPower >= 3 && Me.HasAura("Final Verdict"));
//.	1.29	final_verdict,if=holy_power>=3
            await Spell.CoCast(S.TemplarsVerdict, onunit);
//.	0.00	templars_verdict,if=holy_power>=3&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*6)
//.	0.00	holy_prism
            await Spell.Cast(S.HolyPrism, onunit);

            return true;
        }
        #endregion

        #region SecTar

        private static WoWUnit SecTar
        {
            get
            {
                if (!StyxWoW.Me.GroupInfo.IsInParty)
                    return null;
                if (StyxWoW.Me.GroupInfo.IsInParty)
                {
                    var secondTarget = (from unit in ObjectManager.GetObjectsOfType<WoWUnit>(false)
                                        where unit.IsAlive
                                        where unit.IsHostile
                                        where unit.Distance < 30
                                        where unit.IsTargetingMyPartyMember || unit.IsTargetingMyRaidMember
                                        where unit.InLineOfSight
                                        where unit.Guid != Me.CurrentTarget.Guid
                                        select unit).FirstOrDefault();
                    return secondTarget;
                }
                return null;
            }
        }
        #endregion

        #region MaxHolyPower

        private static bool MaxHolyPower
        {
            get
            {
                return
                    Me.CurrentHolyPower == 5 || Me.CurrentHolyPower >= 3 && Me.HasAura(S.HolyAvenger);
            }
        }

        #endregion

        #region FlashTarclutch

        private static WoWUnit FlashTarclutch
        {
            get
            {
                var eHheal = (from unit in ObjectManager.GetObjectsOfTypeFast<WoWPlayer>()
                    where unit.IsAlive
                    where unit.IsInMyPartyOrRaid
                    where unit.Distance < 40
                    where unit.InLineOfSight
                    where unit.HealthPercent <= 35
                    select unit).OrderByDescending(u => u.HealthPercent).LastOrDefault();
                return eHheal;
            }
        }

        #endregion

    }
}
