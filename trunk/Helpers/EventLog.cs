using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Bots.BGBuddy.Helpers;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;

namespace Axiom.Helpers
{
    class EventLog
    {
        private static bool _combatLogAttached;

        public static void AttachCombatLogEvent()
        {
            if (_combatLogAttached)
                return;

            // DO NOT EDIT THIS UNLESS YOU KNOW WHAT YOU'RE DOING!
            // This ensures we only capture certain combat log events, not all of them.
            // This saves on performance, and possible memory leaks. (Leaks due to Lua table issues.)
            Lua.Events.AttachEvent("COMBAT_LOG_EVENT_UNFILTERED", HandleCombatLog);
            if (
                !Lua.Events.AddFilter(
                    "COMBAT_LOG_EVENT_UNFILTERED",
                    "return args[2] == 'SPELL_CAST_SUCCESS' or args[2] == 'SPELL_AURA_APPLIED' or args[2] == 'SPELL_CAST_FAILED'"))
            {
                Logger.Write(
                    "ERROR: Could not add combat log event filter! - Performance may be horrible, and things may not work properly!");
            }

            Logger.WriteDebug("Attached combat log");
            _combatLogAttached = true;
        }

        public static void DetachCombatLogEvent()
        {
            if (!_combatLogAttached)
                return;

            Logger.WriteDebug("Detached combat log");
            Lua.Events.RemoveFilter("COMBAT_LOG_EVENT_UNFILTERED");
            Lua.Events.DetachEvent("COMBAT_LOG_EVENT_UNFILTERED", HandleCombatLog);
            _combatLogAttached = false;
        }

        public static void HandleCombatLog(object sender, LuaEventArgs args)
        {
            try
            {
                var e = new CombatLog(args.EventName, args.FireTimeStamp, args.Args);
                switch (e.Event)
                {
                    case "SPELL_AURA_APPLIED":
                        if (e.DestName != StyxWoW.Me.Name)
                            Log.WritetoFile(Styx.Common.LogLevel.Diagnostic, string.Format("Affected By: {0}({1})", e.SpellName, e.SpellId));
                        break;
                    case "SPELL_CAST_FAILED":
                        if (e.Args[14].ToString() == "SPELL_FAILED_LINE_OF_SIGHT" && e.SourceName == StyxWoW.Me.Name && e.DestName != "[LuaTValue Type: Nil]")
                            Styx.CommonBot.Blacklist.Add(e.DestUnit, BlacklistFlags.Combat, TimeSpan.FromSeconds(1));
                        if (e.Args[14].ToString() == "No path available")
                        {
                            //Small hack to blacklist spell for 5 seconds if you have a pathing issue.
                            Spell.UpdateSpellHistory(e.SpellName, 5000, e.DestUnit);
                            Lua.DoString("SpellStopTargeting()");
                        }
                        else
                        {
                            Spell.UpdateSpellHistory(e.SpellName, 1000, e.DestUnit);
                        }
                        Log.WriteLog(string.Format("{0} missed, reason {3} => {1}@{2}", e.SpellName, e.DestUnit.safeName(), e.DestUnit.Status(), e.Args[14].ToString()), Colors.Red);
                        break;
                    case "SPELL_CAST_SUCCESS":
                        if (e.DestName != "[LuaTValue Type: Nil]")
                        {
                            Spell.UpdateSpellHistory(e.SpellName, e.Spell.CooldownTimeLeft.TotalMilliseconds, e.DestUnit);
                            if (e.SourceName == StyxWoW.Me.Name)
                                Log.WriteLog(string.Format("Landed {0} => {1}@{2}", e.SpellName, e.DestUnit.safeName(), e.DestUnit.Status()), Colors.Orange);
                        }
                        if (e.DestName == "[LuaTValue Type: Nil]" && e.SourceName == StyxWoW.Me.Name)
                        {
                            Spell.UpdateSpellHistory(e.SpellName, e.Spell.CooldownTimeLeft.TotalMilliseconds, e.DestUnit);
                            Log.WriteLog(string.Format("Landed {0} => {1}@{2}", e.SpellName, "Me", e.DestUnit.Status()), Colors.Orange);
                        }
                        break;
                    case "RANGE_MISSED":
                        Log.WriteLog(string.Format("{0} missed, reason {3} => {1}@{2}", e.SpellName, e.DestUnit.safeName(), e.DestUnit.Status(), e.Args[14].ToString()), Colors.Red);
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
