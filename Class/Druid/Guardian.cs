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
    class Guardian : Axiom
    {

        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.DruidGuardian ? WoWClass.Druid : WoWClass.None; } }

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

            await Spell.Cast(S.BearForm, () => !Me.HasAura(S.BearForm) && Axiom.AFK);

            await Spell.CoCast(S.SavageDefense, IsCurrentTank());

            await Spell.CoCast(S.Maul, onunit, (Me.HasAura(S.ToothandClaw) /*&& Me.CurrentRage >= 75*/ && IsCurrentTank()) || Me.CurrentRage >= Me.MaxRage - 20/*Me.HasAura(ToothandClaw)*/ && !IsCurrentTank());

            await Spell.Cast(S.BerserkBear, onunit, () => Me.GetAuraTimeLeft("Pulverize").TotalSeconds > 10 && Axiom.Burst);

            await Spell.CoCast(S.FrenziedRegeneration, Me.CurrentRage >= 80 && Me.HealthPercent <= 75 && Axiom.AFK);

            await Spell.Cast(S.CenarionWard, Me, () => Me.HealthPercent <= 65);

            await Spell.CoCast(S.HealingTouch, Me, Me.HasAura(145162) && Me.HealthPercent <= 50);

            await Spell.Cast(S.Pulverize, onunit, () => Me.HasAuraExpired("Pulverize", 3));

            await Spell.CoCast(S.Mangle);

            await Spell.Cast(S.Lacerate, onunit, () => !Me.HasAura(S.BerserkBear) && ((Me.GetAuraTimeLeft("Pulverize").TotalSeconds < 3.6 && onunit.GetAuraStackCount("Lacerate") < 3) || !onunit.HasAura(S.Lacerate)));

            //await Spell.CoCast("Thrash", !Me.CurrentTarget.HasAura("Thrash"));

            await Spell.Cast(S.ThrashBear, onunit, () => onunit.GetAuraTimeLeft("Thrash").TotalSeconds < 4.8 || Units.EnemyUnitsSub8.Count(u => u.HasAuraExpired("Thrash", 4)) >= 1 || Units.EnemyUnitsSub8.Count() >= 3);

            await Spell.Cast(S.Lacerate, onunit);
            
            //await AOE(onunit, Units.EnemyUnitsSub10.Count() >= 3 && Axiom.AOE);

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

        #region Is Tank
        static bool IsCurrentTank()
        {
            return StyxWoW.Me.CurrentTarget.CurrentTargetGuid == StyxWoW.Me.Guid;
        }
        #endregion

    }
}
