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

            //D	49.43	gift_of_the_ox,if=buff.gift_of_the_ox.react&incoming_damage_1500ms
            //E	0.00	diffuse_magic,if=incoming_damage_1500ms&buff.fortifying_brew.down
            //F	0.00	dampen_harm,if=incoming_damage_1500ms&buff.fortifying_brew.down&buff.elusive_brew_activated.down
            //G	2.00	fortifying_brew,if=incoming_damage_1500ms&(buff.dampen_harm.down|buff.diffuse_magic.down)&buff.elusive_brew_activated.down
            //H	17.63	elusive_brew,if=buff.elusive_brew_stacks.react>=9&(buff.dampen_harm.down|buff.diffuse_magic.down)&buff.elusive_brew_activated.down
            //I	0.00	invoke_xuen,if=talent.invoke_xuen.enabled&target.time_to_die>15&buff.shuffle.remains>=3&buff.serenity.down
            //J	0.00	serenity,if=talent.serenity.enabled&cooldown.keg_smash.remains>6
            //L	0.67	touch_of_death,if=target.health.percent<10&cooldown.touch_of_death.remains=0&((!glyph.touch_of_death.enabled&chi>=3&target.time_to_die<8)|(glyph.touch_of_death.enabled&target.time_to_die<5))
            //M	0.00	call_action_list,name=st,if=active_enemies<3
            //N	0.00	call_action_list,name=aoe,if=active_enemies>=3

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

        #region Spells

            #region Chi Brew

            private async Task<bool> ChiBrew()
            {
                if (SpellManager.Spells["Chi Brew"].Cooldown)
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

    }
}
