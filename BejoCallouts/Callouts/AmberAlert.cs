using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;

namespace BejoCallouts.Callouts
{
    [CalloutInfo("StolenVehicle", CalloutProbability.High)]
    public class AmberAlert : Callout
    {
        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vector3 spawnPoint;
        private Blip suspectBlip;
        private LHandle pursuit;
        private bool pursuitCreated = false;

        public override bool OnBeforeCalloutDisplayed()
        {
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(200f));
            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);

            AddMinimumDistanceCheck(20f, spawnPoint);
            CalloutMessage = "Stolen Vehicle";
            CalloutPosition = spawnPoint;

            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_GRAND_THEFT_AUTO IN_OR_ON_POSITION", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            suspectVehicle = new Vehicle("ZENTORNO", spawnPoint);
            suspectVehicle.IsPersistent = true;

            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;

            suspectBlip = suspect.AttachBlip();
            suspectBlip.IsFriendly = false;
            suspectBlip.EnableRoute(System.Drawing.Color.Yellow);

            suspect.Tasks.CruiseWithVehicle(20f, VehicleDrivingFlags.Normal);
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!pursuitCreated && Game.LocalPlayer.Character.DistanceTo(suspect.Position) < 30f)
            {
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, suspect);
                Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                pursuitCreated = true;
            }

            if (pursuitCreated && !Functions.IsPursuitStillRunning(pursuit))
            {
                End();
            }
        }

        public override void End()
        {
            base.End();
            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
        }

    }
}
