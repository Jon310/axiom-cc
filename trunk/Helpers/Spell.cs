using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using Axiom.Helpers;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Axiom.Helpers
{
    public class Spell : Axiom
    {
        public static readonly LocalPlayer Me = StyxWoW.Me;
        public static WoWUnit LastCastTarget;

        #region Enums
        public enum CanCastResult
        {
            InvalidTarget,
            GCD,
            NoSpell,
            DCP,
            Moving,
            Casting,
            CoolDown,
            Energy,
            LOS,
            Inhibited,
            Friendly,
            Range,
            Drinking,
            Dead,
            Success,
            Channeling,
            PointlessHeal,
            Mounted
        }

        public enum SpellFlags
        {
            Buff,
            Heal,
            Normal,
            FreeCast
        }
        #endregion

        #region CastSpell

        public static async Task<bool> CastSpell(string spell, WoWUnit onunit, Func<bool> reqs,
            SpellFlags type = SpellFlags.Normal, string reason = "", bool ignoregcd = false)
        {
            if (!reqs())
            {
                return false;
            }
            CanCastResult CanCastr = CanCast(spell, onunit, type, ignoregcd);
            if (CanCastr != CanCastResult.Success)
            {
                if (GeneralSettings.Instance.LogCanCastResults)
                    Log.WritetoFile(LogLevel.Diagnostic,
                        string.Format(
                            "Cast on " + onunit.safeName() + " Failed For " + spell + " reason: CanCast ({0})",
                            CanCastr.ToString()));
                return false;
            }
            if (!await Movement.FaceTarget(onunit, type))
                return false;
            if (SpellManager.Cast(spell, onunit))
            {
                LastCastTarget = onunit;
                Log.WritetoFile(LogLevel.Diagnostic,
                    String.Format("Casting {0} => {1}@{2} r: {3}", spell, onunit.safeName(), onunit.Status(), reason));
                return true;
            }
            return false;
        }

        public static async Task<bool> CastSpell(int spell, WoWUnit onunit, Func<bool> reqs,
            SpellFlags type = SpellFlags.Normal, string reason = "", bool ignoregcd = false)
        {
            var sp = WoWSpell.FromId(spell);
            var sname = sp != null ? sp.Name : "#" + spell;
            if (!reqs())
            {
                return false;
            }
            CanCastResult CanCastr = CanCast(sname, onunit, type, ignoregcd);
            if (CanCastr != CanCastResult.Success)
            {
                if (GeneralSettings.Instance.LogCanCastResults)
                    Log.WritetoFile(LogLevel.Diagnostic,
                        string.Format(
                            "Cast on " + onunit.safeName() + " Failed For " + sname + " reason: CanCast ({0})",
                            CanCastr.ToString()));
                return false;
            }
            if (!await Movement.FaceTarget(onunit, type))
                return false;
            if (SpellManager.Cast(spell, onunit))
            {
                LastCastTarget = onunit;
                Log.WritetoFile(LogLevel.Diagnostic,
                    String.Format("Casting {0} => {1}@{2} r: {3}", sname, onunit.safeName(), onunit.Status(), reason));
                return true;
            }
            return false;
        }

        #endregion

        #region CanCast

        public static CanCastResult CanCast(string strspell, WoWUnit unit, SpellFlags type, bool ignoregcd)
        {
            if (!Me.IsAlive || Me.IsDead || Me.IsGhost || Me.CurrentHealth == 0)
                return CanCastResult.Dead;

            if (SpellManager.GlobalCooldown && ignoregcd == false)
                return CanCastResult.GCD;

            SpellFindResults results;
            if (!SpellManager.FindSpell(strspell, out results))
                return CanCastResult.NoSpell;

            WoWSpell spell = results.Override ?? results.Original;

            if (unit == null || !unit.IsValid)
                return CanCastResult.InvalidTarget;

            if (SpellHistoryContainsKey(strspell, unit.Guid))
                return CanCastResult.DCP;

            if (Me.IsMoving && !spell.IsMeleeSpell && spell.CastTime != 0 &&
                (!IsChanneled(strspell) && spell.Name != "Cobra Shot"))
                return CanCastResult.Moving;

            if (Me.IsCasting && !Me.IsChanneling)
                return CanCastResult.Casting;

            if (Me.ChanneledSpell != null)
            {
                //if there is no cooldown to the spell ignore this
                if (spell.BaseCooldown >= 3000)
                    return CanCastResult.Channeling;
            }

            if (spell.Cooldown)
                return CanCastResult.CoolDown;

            if (type != SpellFlags.FreeCast && HaveEnergy(strspell))
                return CanCastResult.Energy;

            if ((!unit.InLineOfSight && unit.IsWithinMeleeRange) ||
                (!unit.InLineOfSpellSight && !unit.IsWithinMeleeRange))
                return CanCastResult.LOS;

            if (Me.Mounted && !GeneralSettings.Instance.AutoDismount)
                return CanCastResult.Mounted;

            if (Me.DebuffCC() && type != SpellFlags.Buff)
                return CanCastResult.Inhibited;

            if (unit.IsFriendly() && type == SpellFlags.Normal)
                return CanCastResult.Friendly;

            if (spell.HasRange)
            {
                if (unit.Distance >= spell.MaxRange + unit.CombatReach)
                    return CanCastResult.Range;
            }
            else
            {
                if (!unit.IsWithinMeleeRange)
                    return CanCastResult.Range;
            }

            if (Me.HasAura("Drink"))
                return CanCastResult.Drinking;

            if (type == SpellFlags.Heal && unit.CurrentHealth == unit.MaxHealth)
                return CanCastResult.PointlessHeal;

            return CanCastResult.Success;
        }

        #endregion

        #region Simplicity Wrappers
        public static Task<bool> Buff(string spell, WoWUnit onunit, string reason = "")
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.Buff, reason);
        }
        public static Task<bool> Buff(string spell, WoWUnit onunit, Func<bool> req, string reason = "")
        {
            return CastSpell(spell, onunit, req, SpellFlags.Buff, reason);
        }
        public static Task<bool> FreeBuff(string spell, WoWUnit onunit, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.Buff, reason, ignoregcd);
        }
        public static Task<bool> FreeBuff(string spell, WoWUnit onunit, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, req, SpellFlags.Buff, reason, ignoregcd);
        }
        public static Task<bool> SelfBuff(string spell, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, Me, () => true, SpellFlags.Buff, reason, ignoregcd);
        }
        public static Task<bool> SelfBuff(string spell, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, Me, req, SpellFlags.Buff, reason, ignoregcd);
        }
        public static Task<bool> Heal(string spell, WoWUnit onunit, string reason = "")
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.Heal, reason);
        }
        public static Task<bool> Heal(string spell, WoWUnit onunit, Func<bool> req, string reason = "")
        {
            return CastSpell(spell, onunit, req, SpellFlags.Heal, reason);
        }
        public static Task<bool> SelfHeal(string spell, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, Me, () => true, SpellFlags.Heal, reason, ignoregcd);
        }
        public static Task<bool> SelfHeal(string spell, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, Me, req, SpellFlags.Heal, reason, ignoregcd);
        }
        public static Task<bool> Cast(string spell, WoWUnit onunit, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.Normal, reason, ignoregcd);
        }
        public static Task<bool> Cast(string spell, WoWUnit onunit, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, req, SpellFlags.Normal, reason, ignoregcd);
        }
        public static Task<bool> FreeCast(string spell, WoWUnit onunit, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.FreeCast, reason, ignoregcd);
        }
        public static Task<bool> FreeCast(string spell, WoWUnit onunit, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, req, SpellFlags.FreeCast, reason, ignoregcd);
        }
        #endregion

        #region Simplicity Wrappers Int
        public static Task<bool> Buff(int spell, WoWUnit onunit, string reason = "")
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.Buff, reason);
        }
        public static Task<bool> Buff(int spell, WoWUnit onunit, Func<bool> req, string reason = "")
        {
            return CastSpell(spell, onunit, req, SpellFlags.Buff, reason);
        }
        public static Task<bool> FreeBuff(int spell, WoWUnit onunit, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.Buff, reason, ignoregcd);
        }
        public static Task<bool> FreeBuff(int spell, WoWUnit onunit, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, req, SpellFlags.Buff, reason, ignoregcd);
        }
        public static Task<bool> SelfBuff(int spell, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, Me, () => true, SpellFlags.Buff, reason, ignoregcd);
        }
        public static Task<bool> SelfBuff(int spell, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, Me, req, SpellFlags.Buff, reason, ignoregcd);
        }
        public static Task<bool> Heal(int spell, WoWUnit onunit, string reason = "")
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.Heal, reason);
        }
        public static Task<bool> Heal(int spell, WoWUnit onunit, Func<bool> req, string reason = "")
        {
            return CastSpell(spell, onunit, req, SpellFlags.Heal, reason);
        }
        public static Task<bool> SelfHeal(int spell, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, Me, () => true, SpellFlags.Heal, reason, ignoregcd);
        }
        public static Task<bool> SelfHeal(int spell, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, Me, req, SpellFlags.Heal, reason, ignoregcd);
        }
        public static Task<bool> Cast(int spell, WoWUnit onunit, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.Normal, reason, ignoregcd);
        }
        public static Task<bool> Cast(int spell, WoWUnit onunit, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, req, SpellFlags.Normal, reason, ignoregcd);
        }
        public static Task<bool> FreeCast(int spell, WoWUnit onunit, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, () => true, SpellFlags.FreeCast, reason, ignoregcd);
        }
        public static Task<bool> FreeCast(int spell, WoWUnit onunit, Func<bool> req, string reason = "", bool ignoregcd = false)
        {
            return CastSpell(spell, onunit, req, SpellFlags.FreeCast, reason, ignoregcd);
        }
        #endregion

        #region Properties
        public static bool HaveEnergy(string Name)
        {
            string lua = String.Format("return IsUsableSpell(\"{0}\")", Name);
            return Lua.GetReturnVal<bool>(lua, 2);
        }

        public static bool IsChanneled(string spell)
        {
            SpellFindResults results;
            if (SpellManager.FindSpell(spell, out results))
            {
                if (results.Override != null)
                    return results.Override.AttributesEx == SpellAttributesEx.Channeled1 || results.Override.AttributesEx == SpellAttributesEx.Channeled2;
                return results.Original.AttributesEx == SpellAttributesEx.Channeled1 || results.Original.AttributesEx == SpellAttributesEx.Channeled2;
            }
            return false;
        }

        public static bool SpellHistoryContainsKey(string spell, WoWGuid unitguid)
        {

            LastCastSpell LCS = SpellHistory.Where(s => spell == s.SpellName && (s.UnitGUID.IsValid && s.UnitGUID == unitguid) &&
                   DateTime.UtcNow.Subtract(s.CurrentTime).TotalMilliseconds <= s.ExpiryTime).FirstOrDefault();
            return LCS.SpellName != null;
        }

        public static void UpdateSpellHistory(string spellName, double expiryTime, WoWUnit unit)
        {
            if (!TargetManager.IsValid(unit))
                return;
            PruneSpellHistory();
            if (unit != null) SpellHistory.Add(new LastCastSpell(spellName, 0, expiryTime, DateTime.UtcNow, unit.Guid));
        }

        private static void PruneSpellHistory()
        {
            SpellHistory.RemoveAll(s => DateTime.UtcNow.Subtract(s.CurrentTime).TotalMilliseconds >= s.ExpiryTime);
        }

        private static List<LastCastSpell> SpellHistory = new List<LastCastSpell>();
        public struct LastCastSpell
        {
            public string SpellName { get; set; }
            public int SpellID { get; set; }
            public double ExpiryTime { get; set; }
            public DateTime CurrentTime { get; set; }
            public WoWGuid UnitGUID { get; set; }
            public LastCastSpell(string spellName, int spellid, double expiryTime, DateTime Now, WoWGuid unitguid)
                : this()
            {

                SpellName = spellName;
                SpellID = spellid;
                ExpiryTime = expiryTime;
                CurrentTime = Now;
                UnitGUID = unitguid;
            }
        }
        #endregion
    }
}
