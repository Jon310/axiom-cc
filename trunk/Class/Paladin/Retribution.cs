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
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellList;

namespace Axiom.Class.Paladin
{
    class Retribution : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.PaladinRetribution ? WoWClass.Paladin : WoWClass.None; } }
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

            await Spell.Cast(S.AvengingWrath, onunit, () => Burst);
            await Spell.Cast(S.HolyAvenger, onunit, () => Me.HasAura(S.AvengingWrath) && Burst);
            await Spell.Cast(S.DivineShield, onunit, () => Me.HealthPercent <= 20 && Weave);

            await Spell.Cast(S.FlashofLight, FlashTarclutch, () => Me.HasAura("Selfless Healer", 3));
            await Spell.Cast(S.SealofRighteousness, onunit, () => AOE && !Me.HasAura("Seal of Righteousness") && Units.EnemyUnitsSub8.Count() >= 4);
            await Spell.Cast(S.SealofTruth, onunit, () => !Me.HasAura("Seal of Truth") && Units.EnemyUnitsSub8.Count() < 4);
            await Spell.Cast(S.ExecutionSentence, onunit, () => Burst);
            await Spell.CastOnGround(S.LightsHammer, Me.Location, Me.CurrentTarget.IsBoss && AOE);
            await Spell.Cast(S.TemplarsVerdict, onunit, () => Me.CurrentHolyPower == 5 || Me.HasAura("Divine Purpose"));
            await Spell.Cast(S.HammerofWrath, onunit);
            await Spell.Cast(S.DivineStorm, onunit, () => AOE && Me.HasAura("Divine Crusader") && Me.HasAura("Final Verdict") && Me.CurrentTarget.Distance <= 8 && (Me.HasAura(S.AvengingWrath) || Me.CurrentTarget.HealthPercent < 35));
            await Spell.Cast(S.HammeroftheRighteous, onunit, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnitsSub8.Count() >= 2 && AOE);
            await Spell.Cast(S.CrusaderStrike, onunit, () => Me.CurrentHolyPower <= 4);
            await Spell.Cast(S.DivineStorm, onunit, () => AOE && Me.HasAura("Divine Crusader") && Me.HasAura("Final Verdict") && Me.CurrentTarget.Distance <= 8);
            await Spell.Cast(S.Judgment, SecTar, () => Me.CurrentHolyPower <= 4 && Units.EnemyUnits(15).Count() >= 2 && Me.HasAura("Glyph of Double Jeopardy"));
            await Spell.Cast(S.Judgment, onunit, () => Me.CurrentHolyPower <= 4);
            await Spell.Cast(S.TemplarsVerdict, onunit);
            await Spell.Cast(S.Exorcism, onunit, () => Me.CurrentHolyPower <= 4);
            await Spell.Cast(S.HolyPrism, onunit);

            if (GeneralSettings.Instance.Movement)
                return await Movement.MoveToTarget(onunit);
            
            return false;
        }

        private static async Task<bool> BuffsCoroutine()
        {
            return false;
        }

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
