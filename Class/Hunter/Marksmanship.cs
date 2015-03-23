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


namespace Axiom.Class.Hunter
{
    [UsedImplicitly]
    class Marksmanship : Axiom
    {
        #region Overrides
        static WoWUnit Pet { get { return StyxWoW.Me.Pet; } }

        public override WoWClass Class { get { return Me.Specialization == WoWSpec.HunterMarksmanship ? WoWClass.Hunter : WoWClass.None; } }

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
            //CreateMisdirectionBehavior();
            await Spell.Cast(S.MendPet, Pet, () => Me.GotAlivePet && Pet.HealthPercent < 60 && !Pet.HasAura("Mend Pet"));
            await Spell.Cast(S.KillShot, onunit, () => Me.CurrentTarget.HealthPercent <= 20);
            await Spell.Cast(S.ChimaeraShot, onunit);
            await Spell.Cast(S.RapidFire, onunit, () =>  Axiom.Burst);
            await Spell.CoCast(S.Stampede, Axiom.Burst && (Me.HasAura("Rapid Fire") || WeHaveBloodlust));
            await CarefulAim(onunit, onunit.HealthPercent > 80 || Me.HasAura("Rapid Fire"));
            await Spell.Cast(S.AMurderofCrows, onunit, () => Axiom.Burst);
            await Spell.Cast(S.DireBeast, onunit);
            await Spell.Cast(S.GlaiveToss, onunit);
            await Spell.Cast(S.Powershot, onunit);
            await Spell.Cast(S.Barrage, onunit, () =>  Axiom.Weave);
            await Spell.Cast(S.AimedShot, onunit);
            await Spell.Cast(S.SteadyShot, onunit);

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

        #region Careful Aim
        private static async Task<bool> CarefulAim(WoWUnit onunit, bool reqs)
        {
            if (!reqs)
                return false;
            await Spell.Cast(S.Powershot, onunit);
            await Spell.Cast(S.Barrage, onunit, () => Axiom.Weave);
            await Spell.Cast(S.AimedShot, onunit);
            await Spell.Cast(S.GlaiveToss, onunit);
            await Spell.Cast(S.SteadyShot, onunit);

            return true;
        }
        #endregion

        private static bool WeHaveBloodlust
        {
            get
            {
                return Me.HasAnyAura(StyxWoW.Me.IsHorde ? "Bloodlust" : "Heroism", "Time Warp", "Ancient Hysteria");
            }
        }

    }
}
