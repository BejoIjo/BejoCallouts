using System;
using System.Collections.Generic;
using System.Linq;

using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

namespace BejoCallouts.Callouts
{
    [CalloutInfo("RapeVictim", CalloutProbability.High)]

    public class RapeVictim : Callout
    {
        private Ped player;
        private Ped victim;
        private Ped taxiDriver;
        private Vehicle playerCar;
        private Vehicle taxi;
        private Blip victimBlip;
        private Blip taxiBlip;
        private Blip homeBlip;
        private Vector3 victimSpawnPoint;

        private GameFiber keyboardActionThread;
        private CalloutState calloutStatus;

        private int dialogWithVictimIndex = 0;
        private string[] dialogWithVictim = new string[] {
            "~g~Officer~s~: Good day Ma'am. Are you allright?",
            "~y~Victim~s~: Uh... oh... Officer, please help me.",
            "~g~Officer~s~: What happened? Where is your clothes?",
            "~y~Victim~s~: Someone just almost raped me.",
            "~g~Officer~s~: Are you injured? How may I help you?",
            "~y~Victim~s~: I'm fine. I just wanna go home!",
        };

        private static Dictionary<Vector3, string> residentPositions = new Dictionary<Vector3, string>()
        {
            { new Vector3(-174.586472f, -1529.06079f, 34.3538475f), "CHAMH" },
            { new Vector3(-129.1117f, -1647.581f, 36.51416f), "CHAMH" },
            { new Vector3(-634.468933f, 209.015f, 74.22549f), "WVINE" },
            { new Vector3(-1042.00476f, -1024.898f, 2.15035677f), "VCANA" },
            { new Vector3(-881.785f, 363.261566f, 85.3623047f), "ROCKF" },
            { new Vector3(-213.437042f, 6359.204f, 31.4922943f), "PALETO" },
            { new Vector3(-1537.281f, -270.0623f, 48.2772522f), "MORN" },
            { new Vector3(-3109.232f, 750.7043f, 24.7018833f), "BHAMCA" },
            { new Vector3(1265.45874f, -648.2275f, 67.92145f), "MIRR" },
            { new Vector3(-406.3421f, 566.5771f, 124.607063f), "CHIL" },
            { new Vector3(-3202.82764f, 1152.40479f, 9.654342f), "CHU" },
            { new Vector3(403.918274f, 2584.74487f, 43.51954f), "HARMO" },
            { new Vector3(803.300537f, 2174.6626f, 53.0706978f), "RTRAK" },
            { new Vector3(723.5682f, 4185.95947f, 40.70923f), "ALAMO" },
            { new Vector3(1779.19812f, 3642.55249f, 34.4773178f), "SANDY" },
            { new Vector3(-1147.96533f, -1523.19849f, 10.6280575f), "DELSOL" },
            { new Vector3(-23.2617626f, -22.5759583f, 73.245224f), "HAWICK" },
            { new Vector3(-33.16286f, -1446.31348f, 31.89138f), "STRAW" },
            { new Vector3(461.550964f, -1585.07043f, 32.79198f), "RANCHO" },
            { new Vector3(252.6372f, -1672.16272f, 29.6631889f), "DAVIS" },
            { new Vector3(65.3823242f, -256.226227f, 52.3538742f), "ALTA" },
            { new Vector3(-1606.673f, -431.795349f, 40.4325371f), "DELPE" },
            { new Vector3(-1679.96436f, -400.317078f, 47.52849f), "PBLUFF" },
            { new Vector3(194.816925f, 3030.92749f, 44.0080147f), "DESRT" }
        };

        public override bool OnBeforeCalloutDisplayed()
        {
            player = Game.LocalPlayer.Character;
            victimSpawnPoint = World.GetNextPositionOnStreet(player.Position.Around(300f));
            AddMinimumDistanceCheck(50f, victimSpawnPoint);

            CalloutMessage = "Rape Victim";
            CalloutPosition = victimSpawnPoint;

            return base.OnBeforeCalloutDisplayed();
        }

        public override void OnCalloutDisplayed()
        {
            ShowCalloutAreaBlipBeforeAccepting(victimSpawnPoint, 30f);
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CITIZENS_REPORT CIV_ASSIST IN_OR_ON_POSITION OUTRO UNITS_RESPOND_CODE_02", victimSpawnPoint);
            Game.DisplayNotification("Response with ~r~Code 2~s~.");

            base.OnCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            victim = new Ped("a_f_y_topless_01", victimSpawnPoint, 0f);
            victim.IsPersistent = true;
            AnimationSet animSet = new AnimationSet("move_m@drunk@moderatedrunk");
            animSet.LoadAndWait();
            victim.MovementAnimationSet = animSet;
            victim.Tasks.Wander();
            victim.BlockPermanentEvents = true;

            victimBlip = victim.AttachBlip();
            victimBlip.IsFriendly = false;
            victimBlip.EnableRoute(System.Drawing.Color.Red);

            calloutStatus = CalloutState.enroute;

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (calloutStatus == CalloutState.enroute && player.DistanceTo(victim.Position) < 5f)
            {
                calloutStatus = CalloutState.arrivedOnScene;
                victim.MovementAnimationSet = null;
                victim.Tasks.PutHandsUp(5000, player);
                Game.DisplayHelp("To speak with the victim, press ~y~NumPad5~s~ several times.");
                keyboardActionThread = createKeyboardActionThread();
            }

            if (calloutStatus == CalloutState.taxiAway && victim.DistanceTo(player.Position) > 60f )
            {
                calloutEndSuccessfully();
                calloutStatus = CalloutState.finish;
            }

            if (calloutStatus == CalloutState.escortVictim && victim.DistanceTo(homeBlip.Position) < 25f && playerCar.Speed == 0f)
            {
                victim.Tasks.LeaveVehicle(playerCar, LeaveVehicleFlags.None);
                calloutEndSuccessfully();
                calloutStatus = CalloutState.finish;
            }

            if (calloutStatus == CalloutState.finish) { End(); }

        }

        private GameFiber createKeyboardActionThread()
        {
            return GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();
                    if (Game.IsKeyDown(System.Windows.Forms.Keys.NumPad5))
                    {
                        triggerDialogAction();
                    }
                    else if (Game.IsKeyDown(System.Windows.Forms.Keys.NumPad7))
                    {
                        triggerEscortAction();
                    }
                    else if (Game.IsKeyDown(System.Windows.Forms.Keys.NumPad0))
                    {
                        triggerCallTaxiAction();
                    }
                    else if (Game.IsKeyDown(System.Windows.Forms.Keys.NumPad1))
                    {
                        calloutStatus = CalloutState.finish;
                    }
                }
            });
        }

        private void triggerDialogAction()
        {
            if (victim.DistanceTo(player.Position) < 3f)
            {
                if (dialogWithVictimIndex < dialogWithVictim.Length)
                {
                    Game.DisplaySubtitle(dialogWithVictim[dialogWithVictimIndex]);
                    dialogWithVictimIndex++;
                }
                else
                {
                    Game.DisplayHelp("Press ~y~NumPad0~s~ to call a taxi or press ~y~Numpad7~s~ to give her a ride home.");
                }
            } else
            {
                Game.DisplayHelp("Please move ~y~closer~s~ to the ~b~victim~s~.");
            }
        }

        private void triggerEscortAction()
        {
            playerCar = getNearbyPoliceCar();
            if (playerCar != null)
            {
                Game.DisplayHelp("Escort the ~y~victim~s~ to her ~b~house~s~.");
                escortPedHome(victim, playerCar);
                calloutStatus = CalloutState.escortVictim;
            }
            else
            {
                Game.DisplayHelp("There's ~r~no police car~s~ in the vicinity.");
                calloutStatus = CalloutState.finish;
            }
        }

        private void triggerCallTaxiAction()
        {
            Game.DisplayHelp("Calling ~b~Los Santos Taxi~s~ hotline.");
            playAnimPhoneCall(player, "prop_police_phone");
            GameFiber.Sleep(3000);
            Game.DisplayHelp("~b~Taxi~s~ will be coming shortly. Please wait.");
            callTaxiForPed(victim);
            calloutStatus = CalloutState.taxiAway;
        }

        private Vector3 getNearestResident()
        {
            Vector3 closestResident = (from element in residentPositions.Keys
                              orderby element.DistanceTo2D(player.Position)
                              select element).First();
            return closestResident;
        }

        private Vehicle getNearbyPoliceCar()
        {
            Vehicle[] nearbyCars = player.GetNearbyVehicles(10);
            foreach (Vehicle car in nearbyCars)
            {
                if (car.IsPoliceVehicle)
                {
                    return car;
                }
            }
            return null;
        }

        private void playAnimPhoneCall(Ped ped, String phoneModelName)
        {
            GameFiber.StartNew(delegate
            {
                ped.Tasks.PlayAnimation(new AnimationDictionary("cellphone@"), "cellphone_call_in", 1.5f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.StayInEndFrame);
                GameFiber.Sleep(300);
                Rage.Object cellphone = new Rage.Object(phoneModelName, ped.Position);
                int boneIndex = ped.GetBoneIndex(PedBoneId.RightPhHand);
                cellphone.AttachTo(ped, boneIndex, new Vector3(0f, 0f, 0f), new Rotator(0f, 0f, 0f));
                GameFiber.Sleep(3000);
                ped.Tasks.PlayAnimation(new AnimationDictionary("cellphone@"), "cellphone_call_out", 1f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask);
                GameFiber.Sleep(150);
                cellphone.Detach();
                cellphone.Delete();
            });
        }

        private void callTaxiForPed(Ped passanger)
        {
            Vector3 taxiStartPos = World.GetNextPositionOnStreet(passanger.Position.Around(120f));
            while (taxiStartPos.DistanceTo(passanger.Position) <= 30f)
            {
                taxiStartPos = World.GetNextPositionOnStreet(passanger.Position.Around(120f));
            }
            taxi = new Vehicle("TAXI", taxiStartPos);
            taxiBlip = taxi.AttachBlip();
            taxiBlip.IsFriendly = true;
            taxiDriver = taxi.CreateRandomDriver();

            taxiDriver.Tasks.DriveToPosition(taxi, passanger.Position, 20f, VehicleDrivingFlags.Emergency, 5f).WaitForCompletion();
            passanger.Tasks.EnterVehicle(taxi, 2).WaitForCompletion();
            taxiDriver.Tasks.DriveToPosition(taxi, taxiStartPos, 208f, VehicleDrivingFlags.Normal, 10f);
        }

        private void escortPedHome(Ped ped, Vehicle car)
        {
            ped.Tasks.EnterVehicle(car, 15000, 2).WaitForCompletion();
            Vector3 pedHome = getNearestResident();
            homeBlip = new Blip(pedHome);
            homeBlip.IsFriendly = true;
            homeBlip.EnableRoute(System.Drawing.Color.Blue);
        }

        public override void End()
        {
            Game.LogTrivial("Ending Bejo Callouts - Rape Victim.");
            if (keyboardActionThread != null && keyboardActionThread.IsAlive) { keyboardActionThread.Abort(); }
            if (calloutStatus != CalloutState.finish)
            {
                if (victim.Exists()) { victim.Delete(); }
                if (taxiDriver.Exists()) { taxiDriver.Delete(); }
                if (taxi.Exists()) { taxi.Delete(); }

            }
            else
            {
                if (victim.Exists()) { victim.Dismiss(); }
                if (taxiDriver.Exists()) { taxiDriver.Dismiss(); }
                if (taxi.Exists()) { taxi.Dismiss(); }
            }
            if (victimBlip.Exists()) { victimBlip.Delete(); }
            if (taxiBlip.Exists()) { taxiBlip.Delete(); }
            if (homeBlip.Exists()) { homeBlip.Delete(); }
            base.End();
        }

        private void calloutEndSuccessfully()
        {
            Game.DisplayNotification("All unit ~y~Code 4~s~. ~b~Victim is safe~s~. No further units required.");
            Functions.PlayScannerAudio("CODE_4 NO_FURTHER_UNITS");
        }
    }

    public enum CalloutState
    {
        enroute,
        arrivedOnScene,
        taxiEnroute,
        taxiAway,
        escortVictim,
        finish
    }
}
