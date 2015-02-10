using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellLists;

namespace Axiom.Class.Monk
{
    public class Mistweaver : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.WarriorArms ? WoWClass.Warrior : WoWClass.None; } }
        private static bool SerpentStance { get { return Me.HasAura("Stance of the Wise Serpent"); } }
        private static bool CraneStance { get { return Me.HasAura("Stance of the Spirited Crane"); } }
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
        protected override Composite CreateHeal()
        {
            return new ActionRunCoroutine(ret => HealCoroutine(HealManager.Target));
        }
        protected override Composite CreateRest()
        {
            return new ActionRunCoroutine(ret => RestCoroutine());
        }
        #endregion

        private static async Task<bool> CombatCoroutine(WoWUnit onunit)
        {
            await HealCoroutine(HealManager.Target);

            return false;
        }

        private static async Task<bool> BuffsCoroutine()
        {
            return false;
        }

        private static async Task<bool> HealCoroutine(WoWUnit healtarget)
        {
            await LifeCocoon();

            if (Settings.Monk.PrioritizeSelf)
            {
                if (Me.HealthPercent() <= Settings.Monk.HealthStone)
                    Item.UseContainerItem("Healthstone");

                await Spell.SelfHeal(S.ExpelHarm, () => TargetManager.CountNear(Me, 10) >= 1);
                await Spell.SelfBuff(S.FortifyingBrew, () => Me.HealthPercent() <= Settings.Monk.FortifyingBrew);
                await Spell.SelfBuff(S.DiffuseMagic, () => HealManager.NeedCleanseASAP(Me));
                await ChiBrew();
            }

            await ManaTea(Settings.Monk.ManaTea);
            await Uplift(Settings.Monk.Uplift);
            await ChiWave(healtarget);
            await SpinningCraneKick();
            await RenewingMist();
            await ZenSpheres();
            await Spell.SelfBuff(S.Revival, () => HealManager.SmartTargets(Settings.Monk.Revival).Count() >= HealManager.GroupCount / 2);
            await EnvelopingMists();
            await SoothingMist();
            await SurgingMists();
            await Detox(healtarget);

            return false;
        }

        private static async Task<bool> RestCoroutine()
        {
            return false;
        }

        private static async Task<bool> LifeCocoon()
        {
            var cocoontank = HealManager.Tanks.OrderBy(u => u.HealthPercent).LastOrDefault();

            return await Spell.Buff("Life Cocoon", cocoontank, () => cocoontank.HealthPercent() < Settings.Monk.LifeCocoon, "Tank");
        }

        private static async Task<bool> ManaTea(int percent)
        {
            if (Me.ManaPercent > percent && Me.GetAuraStackCount("Mana Tea") < 18)
                return false;

            var currentmana = Me.ManaPercent;

            if (TalentManager.HasGlyph("Mana Tea"))
            {
                return await Spell.SelfBuff(S.ManaTea, () => Me.ManaPercent < Settings.Monk.ManaTea && Me.GetAuraStackCount("Mana Tea") > 2);
            }

            return await Spell.SelfBuff(S.ManaTea, () => Me.GetAuraStackCount("Mana Tea") >= 2 && currentmana + (4 * 2) < 100, "", true) 
                   && await Coroutine.Wait(2000, () => Spell.StopCasting(() => Me.ManaPercent >= currentmana + 8));
        }

        private static async Task<bool> Uplift(double healthpct)
        {
            if (Me.CurrentChi < 2)
                return false;

            var hasRenew = HealManager.SmartTargets(100).Where(hr => hr.HasAura("Renewing Mist"));
            var needRenew = HealManager.SmartTargets(Settings.Monk.RenewingMist).Where(r => !r.HasAura(119611) && r.HealthPercent >= 30);
            var woWUnits = hasRenew as IList<WoWUnit> ?? hasRenew.ToList();

            if (woWUnits.Count() >= 3 && !SpellManager.Spells["Thunder Focus Tea"].Cooldown && needRenew.Count() >= 3)
            {
                return await Spell.SelfBuff(S.ThunderFocusTea, () => Me.CurrentChi >= 3, "", true) && await Spell.SelfBuff(S.Uplift, () => true);
            }

            return await Spell.SelfBuff(S.Uplift, () => woWUnits.Count(t => t.HealthPercent() <= healthpct) >= 5);
        }

        private static async Task<bool> ChiWave(WoWUnit onunit)
        {
            if (!TalentManager.IsSelected(4))
                return false;

            var targets = HealManager.SmartTargets(Settings.Monk.ChiWave).Count() + TargetManager.CountNear(onunit, 20);

            if (onunit == null || !onunit.IsValid)
                return false;

            return await Spell.Heal("Chi Wave", onunit, () => targets >= Settings.Monk.ChiWaveCount);
        }

        private static async Task<bool> SpinningCraneKick()
        {
            if (SpellManager.Spells["Spinning Crane Kick"].Cooldown || !Me.Combat)
                return false;

            var totaltargets = SerpentStance ? HealManager.CountNearby(Me, Settings.Monk.SpinningCraneKickCount, 8) 
                                            : TargetManager.CountNear(Me, 8);

            if (totaltargets < Settings.Monk.SpinningCraneKickCount)
                return false;

            if (TalentManager.IsSelected(16) && await Spell.SelfBuff(S.SpinningCraneKick))
            {
                return true;
            }

            return await Spell.SelfBuff(S.SpinningCraneKick, () => StyxWoW.Me.ChanneledCastingSpellId != S.SpinningCraneKick);
        }

        private static async Task<bool> RenewingMist()
        {
            if (SpellManager.Spells["Renewing Mist"].Cooldown)
                return false;

            var needstoSpread = HealManager.InitialList.Where(hrm => hrm.HasAura("Renewing Mist") && hrm.GetAuraStackCount("Renewing Mist") == 3);
            var onunit = HealManager.SmartTargets(Settings.Monk.RenewingMist).FirstOrDefault(st => !st.HasAura("Renewing Mist"));

            return await Spell.Heal(S.RenewingMist, onunit, () => onunit != null && !onunit.HasAura("Renewing Mist") && !needstoSpread.Any());
        }

        private static async Task<bool> ZenSpheres()
        {
            if (SpellManager.Spells["Zen Sphere"].Cooldown)
                return false;

            var onunit = HealManager.SmartTarget(Settings.Monk.ZenSphere);

            return await Spell.Buff(S.ZenSphere, onunit, () => TalentManager.IsSelected(5) &&
                onunit != null &&
                onunit.Combat &&
                onunit.IsValid &&
                !onunit.HasAura(S.ZenSphere) &&
                TargetManager.CountNear(onunit, 10) +
                HealManager.CountNearby(onunit, 10, Settings.Monk.ZenSphere) >= 3);
        }

        private static async Task<bool> EnvelopingMists()
        {
                var onunit = HealManager.SmartTarget(Settings.Monk.EnvelopingMist);

                if (onunit == null || !onunit.IsValid || onunit.HasAura(S.EnvelopingMistBuff) || Me.CurrentChi < 3 || Me.ChanneledCastingSpellId != S.SoothingMist || Me.ChannelObjectGuid != onunit.Guid)
                    return false;

                return await Spell.Heal(S.EnvelopingMist, onunit);
        }

        private static async Task<bool> SoothingMist()
        {
                var onunit = HealManager.SmartTarget(Settings.Monk.SoothingMist);

                if (onunit == null || !onunit.IsValid || CraneStance)
                    return false;

                return await Spell.Heal(S.SoothingMist, onunit);
        }

        private static async Task<bool> SurgingMists()
        {
            var onunit = HealManager.SmartTarget(Settings.Monk.SurgingMist);

            if (onunit == null)
                return false;

            if (!SerpentStance || Me.ChannelObject == null || Me.ChanneledCastingSpellId != S.SoothingMist || Me.ChannelObjectGuid != onunit.Guid) 
                return false;

            return await Spell.Heal(S.SurgingMist, onunit);
        }

        private static async Task<bool> Detox(WoWUnit onunit)
        {
            if (SpellManager.Spells["Detox"].Cooldown || Settings.Monk.Detox == Settings.Monk.DetoxBehaviour.Manually)
                return false;

            if (Settings.Monk.Detox == Settings.Monk.DetoxBehaviour.OnCoolDown)
                return await Spell.Heal(S.Detox, onunit);

            if (Settings.Monk.Detox == Settings.Monk.DetoxBehaviour.OnDebuff)
                return await Spell.Heal(S.Detox, onunit, () => Settings.Monk.DetoxBuff != "" && onunit.HasAura(Settings.Monk.DetoxBuff));

            return false;
        }

        private static async Task<bool> ChiBrew()
        {
            var currentChi = Me.CurrentChi;

            if (!TalentManager.IsSelected(9))
                return false;

            return await Spell.SelfBuff(S.ChiBrew, () => Me.ManaPercent <= Settings.Monk.ManaTea && Me.CurrentChi <= (Me.MaxChi - 2) && Me.GetAuraStackCount("Mana Tea") < 18, "", true) 
                && await Coroutine.Wait(1000, () => Me.CurrentChi == (currentChi + 2) || Me.CurrentChi == Me.MaxChi);
        }
    }
}
