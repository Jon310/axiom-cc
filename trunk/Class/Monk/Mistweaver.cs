﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellLists;
using MonkSettings = Axiom.Settings.Monk;

namespace Axiom.Class.Monk
{
    public class Mistweaver : Axiom
    {
        const ShapeshiftForm WISE_SERPENT = (ShapeshiftForm)20;
        const ShapeshiftForm SPIRITED_CRANE = (ShapeshiftForm)9;
        protected static readonly LocalPlayer Me = StyxWoW.Me;

        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.MonkMistweaver ? WoWClass.Monk : WoWClass.None; } }
        private bool SerpentStance { get { return Me.HasAura("Stance of the Wise Serpent"); } }
        private bool CraneStance { get { return Me.HasAura("Stance of the Spirited Crane"); } }
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
        protected override Composite CreateHeal()
        {
            return new ActionRunCoroutine(ret => HealCoroutine(HealManager.Target));
        }
        protected override Composite CreateRest()
        {
            return new ActionRunCoroutine(ret => RestCoroutine());
        }
        #endregion

        private async Task<bool> CombatCoroutine(WoWUnit onunit)
        {
            await Crane(onunit, CraneStance);
            //await HealCoroutine(HealManager.Target);

            return false;
        }

        private async Task<bool> BuffsCoroutine()
        {
            return false;
        }

        private async Task<bool> HealCoroutine(WoWUnit healtarget)
        {
            await LifeCocoon();

            if (MonkSettings.Instance.PrioritizeSelf)
            {
                if (Me.HealthPercent() <= MonkSettings.Instance.HealthStone)
                    Item.UseContainerItem("Healthstone");

                await Spell.SelfHeal(S.ExpelHarm, () => !TalentManager.HasGlyph("Targeted Expulsion") && Me.HealthPercent < MonkSettings.Instance.ExpelHarm);
                await Spell.Heal(S.ExpelHarm, healtarget, () => TalentManager.HasGlyph("Targeted Expulsion") && healtarget.HealthPercent < MonkSettings.Instance.ExpelHarm);
                await Spell.SelfBuff(S.FortifyingBrew, () => Me.HealthPercent() <= MonkSettings.Instance.FortifyingBrew);
                await Spell.SelfBuff(S.DiffuseMagic, () => HealManager.NeedCleanseASAP(Me));
                await ChiBrew();
            }

            if (SerpentStance)
            { 
                await ManaTea(MonkSettings.Instance.ManaTea);
                await Uplift(MonkSettings.Instance.Uplift);
                await ChiWave(healtarget);
                await SpinningCraneKick();
                await RenewingMist();
                await ZenSpheres();
                await Spell.SelfBuff(S.Revival, () => HealManager.SmartTargets(MonkSettings.Instance.Revival).Count() >= HealManager.GroupCount / 2);
                await EnvelopingMists();
                await SoothingMist();
                await SurgingMists();
                await Detox(healtarget);
            }

            return false;
        }


        private async Task<bool> RestCoroutine()
        {
            return false;
        }

        private async Task<bool> Crane(WoWUnit onunit, bool reqs)
        {
            if (!reqs) return false;
            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            await Spell.Cast(S.ExpelHarm, onunit, () => Me.HealthPercent <= MonkSettings.Instance.ExpelHarm);
            await Spell.Cast(S.SurgingMist, VitalMistsTar, () => Me.HasAura("Vital Mists", 5));
            await Spell.Cast(S.TigerPalm, onunit, () => (Me.HasAura("Vital Mists", 4) || !Me.HasAura("Tiger Power")) && Me.CurrentChi > 0);
            await Spell.Cast(S.BlackoutKick, onunit, () => !Me.HasAura("Crane's Zeal") && Me.CurrentChi >= 2);
            await Spell.Cast(S.RisingSunKick, onunit, () => Me.CurrentChi >= 2);
            await Spell.Cast(S.ChiWave, onunit);
            await Spell.Cast(S.BlackoutKick, onunit, () => Me.CurrentChi >= 2);
            await Spell.Cast(S.Jab, onunit);

            return true;
        }

        private async Task<bool> LifeCocoon()
        {
            var cocoontank = HealManager.Tanks.OrderBy(u => u.HealthPercent).LastOrDefault();

            return await Spell.Buff("Life Cocoon", cocoontank, () => cocoontank.HealthPercent() < MonkSettings.Instance.LifeCocoon, "Tank");
        }

        private async Task<bool> ManaTea(int percent)
        {
            if (Me.ManaPercent > percent && Me.GetAuraStackCount("Mana Tea") < 18)
                return false;

            var currentmana = Me.ManaPercent;

            if (TalentManager.HasGlyph("Mana Tea"))
            {
                return await Spell.SelfBuff(S.ManaTea, () => Me.ManaPercent < MonkSettings.Instance.ManaTea && Me.GetAuraStackCount("Mana Tea") > 2);
            }

            return await Spell.SelfBuff(S.ManaTea, () => Me.GetAuraStackCount("Mana Tea") >= 2 && currentmana + (4 * 2) < 100, "", true) 
                   && await Coroutine.Wait(2000, () => Spell.StopCasting(() => Me.ManaPercent >= currentmana + 8));
        }

        private async Task<bool> Uplift(double healthpct)
        {
            if (Me.CurrentChi < 2)
                return false;

            var hasRenew = HealManager.SmartTargets(100).Where(hr => hr.HasAura("Renewing Mist"));
            var needRenew = HealManager.SmartTargets(MonkSettings.Instance.RenewingMist).Where(r => !r.HasAura(119611) && r.HealthPercent >= 30);
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

            var targets = HealManager.SmartTargets(MonkSettings.Instance.ChiWave).Count() + TargetManager.CountNear(onunit, 20);

            if (onunit == null || !onunit.IsValid)
                return false;

            return await Spell.Heal("Chi Wave", onunit, () => targets >= MonkSettings.Instance.ChiWaveCount);
        }

        private async Task<bool> SpinningCraneKick()
        {
            if (!SpellManager.HasSpell("Spinning Crane Kick"))
                return false;

            if (SpellManager.Spells["Spinning Crane Kick"].Cooldown || !Me.Combat)
                return false;

            var totaltargets = SerpentStance ? HealManager.CountNearby(Me, 10f, MonkSettings.Instance.SpinningCraneKick) 
                                            : TargetManager.CountNear(Me, 8f);

            if (totaltargets < MonkSettings.Instance.SpinningCraneKickCount)
                return false;

            await Spell.SelfBuff(S.SpinningCraneKick, () => TalentManager.IsSelected(16));
            await Spell.SelfBuff(S.SpinningCraneKick, () => StyxWoW.Me.ChanneledCastingSpellId != S.SpinningCraneKick && !TalentManager.IsSelected(16));

            return false;
        }

        private async Task<bool> RenewingMist()
        {
            if (!SpellManager.HasSpell("Renewing Mist"))
                return false;

            if (SpellManager.Spells["Renewing Mist"].Cooldown)
                return false;

            var needstoSpread = HealManager.InitialList.Where(hrm => hrm.HasAura("Renewing Mist") && hrm.GetAuraStackCount("Renewing Mist") == 3);
            var onunit = HealManager.SmartTargets(MonkSettings.Instance.RenewingMist).FirstOrDefault(st => !st.HasAura("Renewing Mist"));

            return await Spell.Heal(S.RenewingMist, onunit, () => onunit != null && !onunit.HasAura("Renewing Mist") && !needstoSpread.Any());
        }

        private async Task<bool> ZenSpheres()
        {
            if (!SpellManager.HasSpell("Zen Sphere"))
                return false;

            if (SpellManager.Spells["Zen Sphere"].Cooldown)
                return false;

            var onunit = HealManager.SmartTarget(MonkSettings.Instance.ZenSphere);

            return await Spell.Buff(S.ZenSphere, onunit, () => TalentManager.IsSelected(5) &&
                onunit != null &&
                onunit.Combat &&
                onunit.IsValid &&
                !onunit.HasAura(S.ZenSphere) &&
                TargetManager.CountNear(onunit, 10) +
                HealManager.CountNearby(onunit, 10, MonkSettings.Instance.ZenSphere) >= 3);
        }

        private async Task<bool> EnvelopingMists()
        {
            var onunit = HealManager.SmartTarget(MonkSettings.Instance.EnvelopingMist);

                if (onunit == null || !onunit.IsValid || onunit.HasAura(S.EnvelopingMistBuff) || Me.CurrentChi < 3 || Me.ChanneledCastingSpellId != S.SoothingMist || Me.ChannelObjectGuid != onunit.Guid)
                    return false;

                return await Spell.Heal(S.EnvelopingMist, onunit);
        }

        private async Task<bool> SoothingMist()
        {
            WoWUnit onunit = HealManager.SmartTarget(MonkSettings.Instance.SoothingMist);

            if (onunit == null || !onunit.IsValid || CraneStance)
            {
                return false;
            }

            return await Spell.Heal("Soothing Mist", onunit);
        }

        private async Task<bool> SurgingMists()
        {
            var onunit = HealManager.SmartTarget(MonkSettings.Instance.SurgingMist);

            if (onunit == null)
                return false;

            if (!SerpentStance || Me.ChannelObject == null || Me.ChanneledCastingSpellId != S.SoothingMist || Me.ChannelObjectGuid != onunit.Guid) 
                return false;

            return await Spell.Heal(S.SurgingMist, onunit);
        }

        private async Task<bool> Detox(WoWUnit onunit)
        {
            if (SpellManager.Spells["Detox"].Cooldown || MonkSettings.Instance.Detox == Settings.Monk.DetoxBehaviour.Manually)
                return false;

            if (MonkSettings.Instance.Detox == Settings.Monk.DetoxBehaviour.OnCoolDown)
                return await Spell.Heal(S.Detox, onunit);

            if (MonkSettings.Instance.Detox == Settings.Monk.DetoxBehaviour.OnDebuff)
                return await Spell.Heal(S.Detox, onunit, () => MonkSettings.Instance.DetoxBuff != "" && onunit.HasAura(MonkSettings.Instance.DetoxBuff));

            return false;
        }

        private async Task<bool> ChiBrew()
        {
            var currentChi = Me.CurrentChi;

            if (!TalentManager.IsSelected(9))
                return false;

            return await Spell.SelfBuff(S.ChiBrew, () => Me.ManaPercent <= MonkSettings.Instance.ManaTea && Me.CurrentChi <= (Me.MaxChi - 2) && Me.GetAuraStackCount("Mana Tea") < 18, "", true) 
                && await Coroutine.Wait(1000, () => Me.CurrentChi == (currentChi + 2) || Me.CurrentChi == Me.MaxChi);
        }

        #region VitalMistsTar

        private WoWUnit VitalMistsTar
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

        #endregion

    }
}