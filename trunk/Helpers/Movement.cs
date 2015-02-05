using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axiom.Managers;
using Axiom.Settings;
using Buddy.Coroutines;
using Axiom.Helpers;
using JetBrains.Annotations;
using Styx;
using Styx.Common;
using Styx.Pathing;
using Styx.WoWInternals.WoWObjects;

namespace Axiom.Helpers
{
    [UsedImplicitly]
    internal class Movement : Axiom
    {
        public static async Task<bool> MoveToTarget(WoWUnit unit, bool range = false)
        {
            //No Target , No Problem lets ignore the rest
            if (!TargetManager.IsValid(unit))
                return false;

            //Casting?  No need to move
            if (StyxWoW.Me.IsCasting)
                return false;

            //No reason to run across the whole damn zone for one guy
            if (unit.Distance > 100)
            {
                StyxWoW.Me.ClearTarget();
                return await Coroutine.Wait(100, () => StyxWoW.Me.CurrentTarget == null);
            }

            // Move to Ranged
            if (range && unit.Distance > 35)
            {
                Log.WriteLog(LogLevel.Diagnostic, "Moving to " + unit.safeName() + "@" + unit.Location);
                if (Navigator.MoveTo(unit.Location - 35f) == MoveResult.ReachedDestination)
                    return true;
            }

            // Move to Melee
            if (!range && !unit.InRange())
            {
                Log.WriteLog(LogLevel.Diagnostic, "Moving to " + unit.safeName() + "@" + unit.Location);
                if (Navigator.MoveTo(unit.Location - 2f) == MoveResult.ReachedDestination)
                    return true;
            }
            return false;
        }

        //Credit to the Singular Team for the CreateFaceTargetBehavior
        public static async Task<bool> FaceTarget(WoWUnit unit, Spell.SpellFlags type, float viewDegrees = 150f)
        {
            if (unit == null || !unit.IsValid)
                return false;

            if (type == Spell.SpellFlags.Buff || type == Spell.SpellFlags.Heal && unit.IsFriendly())
                return true;

            // even though we may want a tighter conical facing check, allow
            // .. behavior to continue if 150 or better so we can cast while turning
            if (StyxWoW.Me.IsSafelyFacing(unit, viewDegrees))
                return true;

            // special handling for when consumed by Direglob and other mobs we are inside/on top of 
            // .. as facing sometimes won't matter
            if (StyxWoW.Me.InVehicle)
            {
                Log.WritetoFile(string.Format("FaceTarget: don't wait to face {0} since in vehicle", unit.safeName()));
                return true;
            }

            if (!GeneralSettings.Instance.Movement
                && !StyxWoW.Me.IsMoving)
            {
                Log.WritetoFile(Styx.Common.LogLevel.Diagnostic, string.Format("FaceTarget: facing since more than {0} degrees", (long)viewDegrees));
                unit.Face();
                if (await Coroutine.Wait(100, () => StyxWoW.Me.IsSafelyFacing(unit, viewDegrees)))
                    return true;
            }
            // otherwise, indicate behavior complete so begins again while
            // .. waiting for facing to occur
            return false;
        }

    }
}
