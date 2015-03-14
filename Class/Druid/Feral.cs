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


namespace Axiom.Class.Druid
{
    [UsedImplicitly]
    class Feral : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.DruidFeral ? WoWClass.Druid : WoWClass.None; } }

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
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !onunit.IsAlive) return true;

            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !onunit.IsAlive) return true;

            await Spell.CoCast(S.IncarnationKingoftheJungle, Me.HasAura("Tiger's Fury") && Axiom.Burst && SpellManager.HasSpell("Incarnation: King of the Jungle"));
            await Spell.CoCast(S.NaturesVigil, Axiom.Burst && SpellManager.HasSpell("Nature's Vigil") && Me.HasAura("Tiger's Fury"));
            await Spell.CoCast(S.BerserkCat, Me.HasAura(S.IncarnationKingoftheJungle) && Me.HasAura("Tiger's Fury") && Axiom.Burst);

            await Spell.CoCast(S.FerociousBite, onunit.HasAura("Rip", true) && onunit.GetAuraTimeLeft("Rip").TotalSeconds <= 3 && onunit.HealthPercent <= 25);
            
            await Spell.CoCast(S.CenarionWard, HealManager.SmartTarget(70), SpellManager.HasSpell("Cenarion Ward"));

            await Spell.CoCast(S.HealingTouch, HealManager.SmartTarget(100), Me.HasAura("Predatory Swiftness") && !Me.HasAura(S.Bloodtalons) && (Me.GetAuraTimeLeft("Predatory Swiftness").TotalSeconds <= 1.5 || Me.ComboPoints >= 4 || onunit.GetAuraTimeLeft("Rake").TotalSeconds <= 4));
            
            await Spell.CoCast(S.SavageRoar, Me.GetAuraTimeLeft("Savage Roar").TotalSeconds <= 3 || ((Me.HasAura(S.BerserkCat) || Spell.GetCooldownLeft("Tiger's Fury").TotalSeconds <= 3) && Me.HasAuraExpired("Savage Roar", 12)) && Me.ComboPoints == 5 || !Me.HasAura("Savage Roar"));
            
            await Spell.CoCast(S.TigersFury, Me.CurrentEnergy <= 30 && !Me.HasAura("Clearcasting"));
            
            await Spell.CoCast(S.ForceofNature, SpellManager.HasSpell("Force of Nature") && (Spell.GetCharges("Force of Nature") == 3));
            
            await Spell.CoCast(S.FerociousBite, Me.ComboPoints >= 5 && onunit.HealthPercent <= 25 && onunit.HasAura("Rip", true));
            
            await Spell.CoCast(S.Rip, Me.HasAura("Savage Roar") && Me.ComboPoints == 5 && (onunit.GetAuraTimeLeft("Rip").TotalSeconds <= 7 || !onunit.HasAura("Rip", true)));
            
            //await Spell.CoCast(S.SavageRoar, SavageRoarTimer);
            
            await Spell.CoCast(S.ThrashCat, Units.EnemyUnitsSub8.Count() >= 2 && Axiom.AOE &&  Units.EnemyUnitsSub8.Count(u => u.HasAuraExpired("Thrash", 4)) >= 1);
            
            await Spell.CoCast(S.Rake, onunit.HasAuraExpired("Rake", 4) && Me.HasAura("Savage Roar"));
            
            //await Spell.CoCast("Thrash", onunit.GetAuraTimeLeft("Thrash").TotalSeconds < 3 && (onunit.GetAuraTimeLeft("Rip").TotalSeconds >= 8 &&
            //                               (Me.GetAuraTimeLeft("Savage Roar").TotalSeconds >= 12 || Me.HasAura("Berserk") || Me.ComboPoints == 5)));
            
            await Spell.CoCast(S.FerociousBite, Me.HasAura("Savage Roar") && Me.ComboPoints == 5 && onunit.HasAura("Rip", true) && onunit.GetAuraTimeLeft("Rip").TotalSeconds > 7 && Me.CurrentEnergy > 50);
            
            await Spell.CoCast(S.Swipe, Units.EnemyUnitsSub8.Count() >= 3 && Axiom.AOE && Me.ComboPoints < 5);
            
            await Spell.CoCast(S.Shred, (Units.EnemyUnitsSub8.Count() < 3 || !Axiom.AOE) && Me.ComboPoints < 5);
            
            await Spell.CoCast(S.Rejuvenation, HealManager.GetTarget(70), Me.ManaPercent >= 40  && !HealManager.GetTarget(70).HasAura("Rejuvination", true));
            
            return false;            

            //await AOE(onunit, Units.EnemyUnitsSub10.Count() >= 3 && Axiom.AOE);
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
            return false;
        }
        #endregion

        #region Coroutine AOE
        private static async Task<bool> AOE(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;



            return true;
        }
        #endregion

        #region Savage Roar
        private static bool SavageRoarTimer
        {
            get
            {
                return
                       Me.HasAuraExpired("Savage Roar", 3) && Me.ComboPoints > 0 && Me.GetAuraTimeLeft("Savage Roar").TotalSeconds + 2 > Me.CurrentTarget.GetAuraTimeLeft("Rip").TotalSeconds ||
                       Me.HasAuraExpired("Savage Roar", 6) && Me.ComboPoints >= 5 && Me.GetAuraTimeLeft("Savage Roar").TotalSeconds + 2 <= Me.CurrentTarget.GetAuraTimeLeft("Rip").TotalSeconds ||
                       Me.HasAuraExpired("Savage Roar", 12) && Me.ComboPoints >= 5 && Me.GetAuraTimeLeft("Savage Roar").TotalSeconds <= Me.CurrentTarget.GetAuraTimeLeft("Rip").TotalSeconds + 6
            ;
            }
        }
        #endregion

        #region Healing Proc's
        private static WoWUnit HTtar
        {
            get
            {
                var eHheal = (from unit in ObjectManager.GetObjectsOfTypeFast<WoWPlayer>()
                              where unit.IsAlive
                              where unit.IsInMyPartyOrRaid
                              where unit.Distance < 40
                              where unit.InLineOfSight
                              where unit.HealthPercent <= 100
                              select unit).OrderByDescending(u => u.HealthPercent).LastOrDefault();
                return eHheal;
            }
        }

        private static WoWUnit HTtarPvP
        {
            get
            {
                var eHheal = (from unit in ObjectManager.GetObjectsOfTypeFast<WoWPlayer>()
                              where unit.IsAlive
                              where unit.IsInMyPartyOrRaid
                              where unit.Distance < 40
                              where unit.InLineOfSight
                              where unit.HealthPercent <= 80
                              select unit).OrderByDescending(u => u.HealthPercent).LastOrDefault();
                return eHheal;
            }
        }

        private static WoWUnit WardTar
        {
            get
            {
                var eHheal = (from unit in ObjectManager.GetObjectsOfTypeFast<WoWPlayer>()
                              where unit.IsAlive
                              where unit.IsInMyPartyOrRaid
                              where unit.Distance < 40
                              where unit.InLineOfSight
                              where unit.HealthPercent <= 70
                              select unit).OrderByDescending(u => u.HealthPercent).LastOrDefault();
                return eHheal;
            }
        }

        private static WoWUnit RejuvTar
        {
            get
            {
                var eHheal = (from unit in ObjectManager.GetObjectsOfTypeFast<WoWPlayer>()
                              where unit.IsAlive
                              where !unit.HasAura("Rejuvenation", true)
                              where unit.IsInMyPartyOrRaid
                              where unit.Distance < 40
                              where unit.InLineOfSight
                              where unit.HealthPercent <= 75
                              select unit).OrderByDescending(u => u.HealthPercent).LastOrDefault();
                return eHheal;
            }
        }
        #endregion


        #region Is Tank
        static bool IsCurrentTank()
        {
            return StyxWoW.Me.CurrentTarget.CurrentTargetGuid == StyxWoW.Me.Guid;
        }
        #endregion

    }
}
