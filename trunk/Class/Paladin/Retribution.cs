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

            if (await AOE4(onunit, Units.EnemyUnitsSub10.Count() >= 4 && Axiom.AOE))
            {
                return true;
            }

            if (await AOE2(onunit, Units.EnemyUnitsSub10.Count() >= 2 && Axiom.AOE))
            {
                return true;
            }

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
           
        
            return false;
        }

        private static async Task<bool> BuffsCoroutine()
        {
            return false;
        }

        #region Coroutine AOE2
        private static async Task<bool> AOE2(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;
            await Spell.Cast(S.SealofRighteousness, onunit, () => Axiom.AOE && !Me.HasAura("Seal of Righteousness") && Units.EnemyUnitsSub8.Count() >= 2);
            await Spell.CoCast(S.DivineStorm, onunit, (Me.HasAura("Final Verdict") || Me.HasAura("Divine Crusader")) && MaxHolyPower);
            await Spell.CoCast(S.TemplarsVerdict, onunit, MaxHolyPower || Me.HasAura("Divine Purpose"));
            await Spell.Cast(S.HammerofWrath, onunit, () => SpellManager.CanCast(S.HammerofWrath));
            await Spell.Cast(S.Exorcism, onunit, () => Me.HasAura("Blazing Contempt") && !MaxHolyPower);
            await Spell.CoCast(S.DivineStorm, onunit, Me.HasAura("Final Verdict") && (Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35));
            await Spell.CoCast(S.TemplarsVerdict, onunit, Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35);
            //await Spell.Cast(S.HammeroftheRighteous, onunit, () => Units.EnemyUnitsSub8.Count() >= 4 && (Me.CurrentHolyPower <= 3 || (Me.CurrentHolyPower == 4 && Me.CurrentTarget.HealthPercent >= 35 && !Me.HasAura(S.AvengingWrath))));
            await Spell.Cast(S.CrusaderStrike, onunit, () => Me.CurrentHolyPower <= 3 || (Me.CurrentHolyPower == 4 && Me.CurrentTarget.HealthPercent >= 35 && !Me.HasAura(S.AvengingWrath)));
            await Spell.Cast(S.Judgment, SecTar, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnits(15).Count() >= 2 && Me.HasAura("Glyph of Double Jeopardy"));
            await Spell.Cast(S.Judgment, onunit, () => Me.CurrentHolyPower <= 4);
            await Spell.Cast(S.Exorcism, onunit, () => TalentManager.HasGlyph("Mass Exorcism") && Me.CurrentHolyPower < 5);
            await Spell.CoCast(S.DivineStorm, onunit, Me.HasAura("Divine Crusader") && Me.HasAura("Final Verdict"));
            await Spell.CoCast(S.DivineStorm, onunit, Me.CurrentHolyPower >=4 && Me.HasAura("Final Verdict"));
            await Spell.CoCast(S.FinalVerdict, onunit, Me.HasAura("Divine Purpose") || Me.CurrentHolyPower >= 4);
            await Spell.CoCast(S.Exorcism, onunit, Me.CurrentHolyPower <= 4);
            await Spell.CoCast(S.DivineStorm, onunit, Me.CurrentHolyPower >= 3 && Me.HasAura("Final Verdict"));
            await Spell.CoCast(S.TemplarsVerdict, onunit);
            await Spell.Cast(S.HolyPrism, onunit);

            return true;
        }
        #endregion

        #region Coroutine AOE4
        private static async Task<bool> AOE4(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;
            await Spell.CoCast(S.TemplarsVerdict, onunit, MaxHolyPower && !Me.HasAura("Final Verdict"));
            await Spell.CoCast(S.DivineStorm, onunit, Me.HasAura("Final Verdict") && MaxHolyPower);
            await Spell.Cast(S.HammerofWrath, onunit, () => SpellManager.CanCast(S.HammerofWrath));
            await Spell.Cast(S.Exorcism, onunit, () => Me.HasAura("Blazing Contempt") && !MaxHolyPower);
            await Spell.CoCast(S.DivineStorm, onunit, (Me.HasAura("Final Verdict") || Me.HasAura("Divine Crusader")) && (Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35));
            await Spell.CoCast(S.TemplarsVerdict, onunit, (Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35) && !Me.HasAura("Final Verdict"));
            await Spell.Cast(S.HammeroftheRighteous, onunit, () => (Me.CurrentHolyPower <= 3 || (Me.CurrentHolyPower == 4 && Me.CurrentTarget.HealthPercent >= 35 && !Me.HasAura(S.AvengingWrath))));
            await Spell.Cast(S.Exorcism, onunit, () => TalentManager.HasGlyph("Mass Exorcism") && Me.CurrentHolyPower < 5);
            await Spell.Cast(S.Judgment, SecTar, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnits(15).Count() >= 2 && Me.HasAura("Glyph of Double Jeopardy"));
            await Spell.Cast(S.Judgment, onunit, () => Me.CurrentHolyPower <= 4);
            await Spell.CoCast(S.DivineStorm, onunit, Me.HasAura("Divine Crusader") && Me.HasAura("Final Verdict"));
            await Spell.CoCast(S.DivineStorm, onunit, Me.CurrentHolyPower >= 4 && Me.HasAura("Final Verdict"));
            await Spell.CoCast(S.FinalVerdict, onunit, (Me.HasAura("Divine Purpose") || Me.CurrentHolyPower >= 4) && !Me.HasAura("Final Verdict"));
            await Spell.CoCast(S.Exorcism, onunit, Me.CurrentHolyPower <= 4);
            await Spell.CoCast(S.DivineStorm, onunit, Me.CurrentHolyPower >= 3 && Me.HasAura("Final Verdict"));
            await Spell.CoCast(S.TemplarsVerdict, onunit, !Me.HasAura("Final Verdict"));
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
