using System;
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
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Axiom
{
    public partial class Axiom : CombatRoutine
    {
        protected static readonly LocalPlayer Me = StyxWoW.Me;

        #region Overrides
        public override string Name { get { return string.Format("Axiom - {0}.", Me.Specialization); } }
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
                if (Movements)
                {
                    StopMoving.Pulse();

                    if (Me.IsDead || Me.IsGhost)
                    {
                        StopMoving.Now();
                        Me.ClearTarget();
                    }

                    if (Me.Combat && !Me.GotTarget && Me.IsMoving)
                    {
                        StopMoving.Now();
                        // NYI
                        //TargetPvP.TargetClosest();
                    }

                    if (Me.CurrentTarget != null && !Me.IsSafelyFacing(Me.CurrentTarget, 40))
                        WoWMovement.Face(Me.CurrentTargetGuid);

                    Movement.PulseMovement();
                }

                    

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
            RegisterHotkeys();
            InitializeOnce();
            EventLog.AttachCombatLogEvent();
            
        }

        private void onBotStopEvent(object o)
        {
            EventLog.DetachCombatLogEvent();
            UnregisterHotkeys();
        }

        private static void InitializeOnce()
        {
            ClassSettings.Initialize();
            
            switch (BotManager.Current.Name)
            {
                case "LazyRaider":
                    GeneralSettings.Instance.Movement = false;
                    break;
                case "Enyo (Buddystore)":
                    GeneralSettings.Instance.Movement = false;
                    break;
                case "Questing":
                    GeneralSettings.Instance.Movement = true;
                    GeneralSettings.Instance.Targeting = true;
                    Log.WriteLog(string.Format("Movement Enabled - Bot - {0} detected", BotManager.Current.Name));
                    break;
                case "Akatosh Quester":
                    GeneralSettings.Instance.Movement = true;
                    GeneralSettings.Instance.Targeting = true;
                    Log.WriteLog(string.Format("Movement Enabled - Bot - {0} detected", BotManager.Current.Name));
                    break;
                case "BGBuddy":
                    GeneralSettings.Instance.Movement = true;
                    GeneralSettings.Instance.Targeting = true;
                    GeneralSettings.Instance.PvP = true;
                    Log.WriteLog(string.Format("Movement Enabled - Bot - {0} detected", BotManager.Current.Name));
                    break;
                case "BGFarmer [Millz]":
                    GeneralSettings.Instance.Movement = true;
                    GeneralSettings.Instance.Targeting = true;
                    GeneralSettings.Instance.PvP = true;
                    Log.WriteLog(string.Format("Movement Enabled - Bot - {0} detected", BotManager.Current.Name));
                    break;
                case "Combat Bot":
                    GeneralSettings.Instance.Movement = true;
                    GeneralSettings.Instance.Targeting = true;
                    Log.WriteLog(string.Format("Movement Enabled - Bot - {0} detected", BotManager.Current.Name));
                    break;
                case "Grind Bot":
                    GeneralSettings.Instance.Movement = true;
                    Log.WriteLog(string.Format("Movement Enabled - Bot - {0} detected", BotManager.Current.Name));
                    break;
                case "Raid Bot":
                    GeneralSettings.Instance.Movement = false;
                    break;
                case "RaidBot Improved":
                    GeneralSettings.Instance.Movement = false;
                    break;
                default:
                    GeneralSettings.Instance.Movement = false;
                    GeneralSettings.Instance.Targeting = true;
                    Log.WriteLog(string.Format("Botbase - {0} detected", BotManager.Current.Name));
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
