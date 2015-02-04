using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CommonBehaviors.Actions;
using Axiom.Helpers;
using Axiom.Managers;
using Axiom.Settings;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Routines;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Axiom
{
    public partial class Axiom : CombatRoutine
    {
        public static readonly LocalPlayer Me = StyxWoW.Me;

        #region Overrides
        public override string Name { get { return "Axiom"; } }
        public override WoWClass Class { get { return WoWClass.None; } }
        public override bool WantButton { get { return true; } }
        public override void OnButtonPress() { AxiomGUI GUI = new AxiomGUI(); GUI.ShowDialog(); }

        public override Composite HealBehavior
        {
            get { return CreateHeal(); }
        }

        public override Composite CombatBehavior
        {
            get { return CreateCombat(); }
        }

        public override Composite PullBehavior
        {
            get { return CreatePull(); }
        }

        public override Composite PreCombatBuffBehavior
        {
            get { return CreateBuffs(); }
        }

        public override Composite RestBehavior
        {
            get { return CreateRest(); }
        }

        public override void Pulse()
        {
            try
            {
                if (Me.Class == WoWClass.Hunter || Me.Class == WoWClass.DeathKnight || 
                    Me.Class == WoWClass.Warlock || Me.Class == WoWClass.Mage)
                        PetManager.Pulse();
            }
            catch (Exception e)
            {
                Logging.WriteException(e);
                throw;
            }
        }

        public override void Initialize()
        {
            GeneralSettings.Instance.Load();
            BotEvents.OnBotStarted += onBotStartEvent;
            BotEvents.OnBotStopped += onBotStopEvent;
        }

        public override void ShutDown()
        {
            BotEvents.OnBotStarted -= onBotStartEvent;
            BotEvents.OnBotStopped -= onBotStopEvent;
        }
        #endregion

        private void onBotStartEvent(object o)
        {
            InitializeOnce();
            EventLog.AttachCombatLogEvent();
            RegisterHotkeys();
        }

        private void onBotStopEvent(object o)
        {
            EventLog.DetachCombatLogEvent();
            UnregisterHotkeys();
        }

        private void InitializeOnce()
        {
            ClassSettings.Initialize();
            
            switch (BotManager.Current.Name)
            {
                case "LazyRaider":
                    GeneralSettings.Instance.DisableMovement = true;
                    Log.WriteLog("Movement Disabled - LazyRaider detected");
                    break;
                case "Enyo (Buddystore)":
                    GeneralSettings.Instance.DisableMovement = true;
                    Log.WriteLog("Movement Disabled - Tyreal detected");
                    break;
                case "Questing":
                    GeneralSettings.Instance.DisableMovement = false;
                    GeneralSettings.Instance.DisableTargeting = false;
                    break;
                case "Combat Bot":
                    GeneralSettings.Instance.DisableMovement = false;
                    GeneralSettings.Instance.DisableTargeting = false;
                    break;
                case "RaidBot":
                    GeneralSettings.Instance.DisableMovement = false;
                    GeneralSettings.Instance.DisableTargeting = false;
                    break;
                default:
                    GeneralSettings.Instance.DisableMovement = false;
                    GeneralSettings.Instance.DisableTargeting = false;
                    Log.WriteLog(string.Format("Movement Enabled - Bot - {0} detected", BotManager.Current.Name));
                    break;
            }

            TalentManager.Init();
            GeneralSettings.Instance.Save();
            Log.WriteLog(string.Format("Axiom Loaded"), Colors.Orange);
        }
        
        #region Hooks

        protected virtual Composite CreateCombat()
        {
            return new HookExecutor("Axiom_Combat_Root",
                "Root composite for Axiom combat. Rotations will be plugged into this hook.",
                new ActionAlwaysFail());
        }

        protected virtual Composite CreateBuffs()
        {
            return new HookExecutor("Axiom_Buffs_Root",
                "Root composite for Axiom buffs. Rotations will be plugged into this hook.",
                new ActionAlwaysFail());
        }

        protected virtual Composite CreateHeal()
        {
            return new HookExecutor("Axiom_Heals_Root",
                "Root composite for Axiom heals. Rotations will be plugged into this hook.",
                new ActionAlwaysFail());
        }

        protected virtual Composite CreateRest()
        {
            return new HookExecutor("Axiom_Rest_Root",
                "Root composite for Axiom Resting. Rotations will be plugged into this hook.",
                new ActionAlwaysFail());
        }

        protected virtual Composite CreatePull()
        {
            return new HookExecutor("Axiom_Pull_Root",
                "Root composite for Axiom Pulling. Rotations will be plugged into this hook.",
                new ActionAlwaysFail());
        }

        #endregion

    }
}
