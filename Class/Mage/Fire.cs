using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using CommonBehaviors.Actions;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using S = Axiom.Lists.SpellLists;

namespace Axiom.Class.Mage
{
    class Fire : Axiom
    {
        #region Overrides
        public override WoWClass Class { get { return Me.Specialization == WoWSpec.MageFire ? WoWClass.Mage : WoWClass.None; } }
        protected override Composite CreateCombat()
        {
            return new ActionRunCoroutine(ret => CombatCoroutine(TargetManager.RangeTarget));
        }
        protected override Composite CreateBuffs()
        {
            return new ActionRunCoroutine(ret => BuffsCoroutine());
        }
        protected override Composite CreatePull()
        {
            return new ActionRunCoroutine(ret => CombatCoroutine(TargetManager.RangeTarget));
        }
        #endregion

        private async Task<bool> CombatCoroutine(WoWUnit onunit)
        {
            if (GeneralSettings.Instance.Targeting)
                TargetManager.EnsureTarget(onunit);

            if (!Me.Combat || Me.Mounted || !Me.GotTarget || !Me.CurrentTarget.IsAlive) return true;

            await Spell.CastOnGround(S.RuneofPower, Me.Location, !Me.HasAura("Rune of Power"));
            await Spell.Cast(S.Pyroblast, onunit, () => Me.HasAura("Pyroblast!") && Me.HasAura("Heating Up"));
            await Spell.Cast(S.LivingBomb, onunit, () => onunit.HasAura("Living Bomb"));
            await Spell.Cast(S.InfernoBlast, onunit, () => Me.HasAura("Heating Up"));
            await Spell.Cast(S.BlastWave, onunit);
            await Spell.Cast(S.Fireball, onunit);
            await Spell.Cast(S.Scorch, onunit);

            return false;
        }

        private async Task<bool> BuffsCoroutine()
        {

            return false;
        }
    }
}
