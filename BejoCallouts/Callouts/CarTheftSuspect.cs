using System;
using System.Collections.Generic;
using System.Linq;

using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;
using System.Reflection;

namespace BejoCallouts.Callouts
{
    [CalloutInfo("CarTheftSuspect", CalloutProbability.High)]
    public class CarTheftSuspect : Callout
    {

        private class CarParkLocation
        {
            public Vector3 location;
            public float heading;
            public String area;
            public String street;

            public CarParkLocation(Vector3 location, float heading, String area, String street)
            {
                this.location = location;
                this.heading = heading;
                this.area = area;
                this.street = street;
            }
        }

        private Ped suspect;
        private Blip suspectBlip;
        Weapon suspectWeapon;
        private Vehicle targetVehicle;
        private CarParkLocation carParkLocation;
        private Vector3 spawnPoint;
        private bool meetSuspect = false;
        private bool isSuspectFlee = false;
        private bool isFinished = false;
        private GameFiber actionThread;

        private static List<CarParkLocation> carParkLocations = new List<CarParkLocation>()
        {
            { new CarParkLocation(new Vector3(-590.7476f, -1179.55042f, 17.0160637f), 1.4497335f, "DELSOL", "Calais Ave") },
            { new CarParkLocation(new Vector3(-477.2011f, -757.533936f, 35.000576f), 90.87332f, "KOREAT", "Vespucci Blvd") },
            { new CarParkLocation(new Vector3(2737.23169f, 4401.6333f, 47.93565f), 325.752136f, "SANCHIA", "East Joshua Road") },
            { new CarParkLocation(new Vector3(-973.229553f, -2693.1748f, 13.4501247f), 315.1874f, "AIRP", "New Empire Way") },
            { new CarParkLocation(new Vector3(-1344.37817f, -405.5609f, 35.69489f), 208.352142f, "MORN", "North Rockford Dr")},
            { new CarParkLocation(new Vector3(34.8810234f, -1727.8833f, 28.77234f), 49.5344849f, "DAVIS", "Davis Ave") },
            { new CarParkLocation(new Vector3(35.8754578f, -716.7427f, 43.54372f), 157.37439f, "PBOX", "Power St") },
            { new CarParkLocation(new Vector3(-866.058533f, -1122.903f, 6.712111f), 299.341278f, "VCANA", "Palomino Ave") },
            { new CarParkLocation(new Vector3(-844.004639f, -1232.5918f, 6.583072f), 139.092773f, "DELSOL", "Tackle St") },
            { new CarParkLocation(new Vector3(1691.32019f, 4774.4165f, 41.3132362f), 269.619171f, "GRAPES", "Grapeseed Ave") },
            { new CarParkLocation(new Vector3(939.4187f, -34.0937424f, 78.19348f), 330.83902f, "CHIL", "Mirror Park Blvd") },
            { new CarParkLocation(new Vector3(1423.945f, 3624.97778f, 34.24978f), 11.8894777f, "SANDY", "Algonquin Blvd") },
            { new CarParkLocation(new Vector3(333.875275f, -216.858643f, 53.5551338f), 251.687988f, "ALTA", "Meteor St") },
            { new CarParkLocation(new Vector3(-765.727051f, 5524.557f, 32.8810234f), 28.44484f, "PALFOR", "Procopio Promenade") },
            { new CarParkLocation(new Vector3(-500.387543f, 271.061768f, 82.79144f), 84.823616f, "WVINE", "Eclipse Blvd") },
            { new CarParkLocation(new Vector3(-3144.4624f, 1110.22168f, 20.1722488f), 100.412071f,"CHU", "Great Ocean Hwy") },
            { new CarParkLocation(new Vector3(2011.75269f, 3073.13647f, 46.4597359f), 332.689728f, "DESRT", "Panorama Dr") },
            { new CarParkLocation(new Vector3(1748.57031f, 3714.11914f, 33.4892464f), 205.016464f, "SANDY", "Mountain View Dr") },
            { new CarParkLocation(new Vector3(492.0294f, -24.0112724f, 77.18911f), 56.8159676f, "HAWICK", "Meteor St") },
            { new CarParkLocation(new Vector3(-396.223419f, 6090.21973f, 30.8968067f ), 129.206909f, "PALETO", "Paleto Blvd") },
        };

        private string[] carModels = new string[] { "DILETTANTE", "SCHAFTER2", "COMET2", "PRIMO", "TAILGATER", "SULTAN" };
        private string[] suspectModels = new string[] { "A_M_M_Soucent_04", "a_m_y_salton_01", "a_m_m_skidrow_01", "a_m_y_stbla_01", "a_m_y_polynesian_01" };

        private String getRandomModel(string[] modelList)
        {
            int randomNum = new Random().Next(0, modelList.Length);
            return modelList[randomNum];
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            carParkLocation = getNearestCarParkLocation(Game.LocalPlayer.Character.Position);
            spawnPoint = carParkLocation.location;
            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);

            CalloutMessage = "Car Theft Suspect";
            CalloutPosition = spawnPoint;

            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_SUSPICIOUS_PERSON IN_OR_ON_POSITION", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            // prepare target vehicle
            targetVehicle = new Vehicle(getRandomModel(carModels), spawnPoint, carParkLocation.heading);
            targetVehicle.IsPersistent = true;

            // prepare the suspect
            suspect = new Ped(getRandomModel(suspectModels), targetVehicle.GetOffsetPosition(new Vector3(-1.5f, -6f, 0f)), targetVehicle.Heading);
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;

            suspectBlip = suspect.AttachBlip();
            suspectBlip.IsFriendly = false;
            suspectBlip.EnableRoute(System.Drawing.Color.Red);

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!meetSuspect && Game.LocalPlayer.Character.DistanceTo(suspect.Position) < 30f)
            {
                meetSuspect = true;
                actionThread = GameFiber.StartNew(delegate
                {
                    suspect.Tasks.GoStraightToPosition(targetVehicle.GetOffsetPositionRight(-1.3f), 1f, targetVehicle.Heading + 270f, 1f, 10000).WaitForCompletion();
                    suspect.Tasks.PlayAnimation(new AnimationDictionary("veh@break_in@0h@p_m_zero@"), "std_force_entry_ds", 1f, AnimationFlags.None);
                    GameFiber.Sleep(900);
                    Rage.Native.NativeFunction.CallByName<uint>("SMASH_VEHICLE_WINDOW", targetVehicle, 0);
                    GameFiber.Sleep(3500);
                    Game.DisplayHelp("Arrest the ~r~car thief!");

                    // randomize the suspect's actions
                    int randomNum = new Random().Next(1, 4);
                    if (randomNum == 1)
                    {
                        suspectWeapon = new Weapon(new WeaponAsset("WEAPON_PISTOL"), suspect.Position, 24);
                        suspectWeapon.GiveTo(suspect);
                        Rage.Native.NativeFunction.CallByName<uint>("TASK_COMBAT_PED", suspect, Game.LocalPlayer.Character, 0, 16);
                    }
                    else if (randomNum == 2)
                    {
                        suspect.Tasks.ReactAndFlee(Game.LocalPlayer.Character);
                        isSuspectFlee = true;                    }
                    else if (randomNum == 3)
                    {
                        suspect.Tasks.PutHandsUp(7000, Game.LocalPlayer.Character);
                    }
                });

            }

            if (meetSuspect && suspect.IsDead)
            {
                handleEnding("All unit ~y~Code 4~s~. ~b~Suspect is dead~s~. No further units required.",
                    "CODE_4 SUSPECT_DOWN NO_FURTHER_UNITS");
            }
            if (meetSuspect && suspect.IsCuffed)
            {
                handleEnding("All unit ~y~Code 4~s~. ~b~Suspect is in custody~s~. No further units required.",
                    "CODE_4 SUSPECT_ARRESTED NO_FURTHER_UNITS");
                handleCourtCase();
            }
            if (meetSuspect && suspect.DistanceTo(Game.LocalPlayer.Character.Position) > 100f)
            {
                handleEnding("All unit ~y~Code 4~s~. ~b~Suspect is fleeing~s~. No further units required.",
                    "CODE_4 CRIME_SUSPECTS_FLEEING_CRIME NO_FURTHER_UNITS");
            }
        }

        private void handleEnding(String notification, String scannerAudio)
        {
            Game.DisplayNotification(notification);
            Functions.PlayScannerAudio(scannerAudio);
            isFinished = true;
            End();
        }

        private void handleCourtCase()
        {
            if (IsLSPDFRPluginRunning("LSPDFR+", new Version("1.6.5.0")))
            {
                Persona persona = Functions.GetPersonaForPed(suspect);
                if (isSuspectFlee)
                {
                    LSPDFRPlusFunctions.CreateNewCourtCase(persona, "Attempted stealing a vehicle, Resisting arrest", 95, "Sentenced to 1 year in prison");
                }
                else
                {
                    LSPDFRPlusFunctions.CreateNewCourtCase(persona, "Attempted stealing a vehicle", 90, "Sentenced to 8 months in prison");
                }
            }
        }

        private CarParkLocation getNearestCarParkLocation(Vector3 refLocation)
        {
            CarParkLocation closestCarParkLocation = (from element in carParkLocations
                                       orderby element.location.DistanceTo2D(refLocation)
                                       select element).First();
            return closestCarParkLocation;
        }

        public override void End()
        {
            if (actionThread != null && actionThread.IsAlive) { actionThread.Abort(); }
            if (isFinished)
            {
                if (suspectWeapon.Exists()) { suspectWeapon.Dismiss(); }
                if (suspect.Exists()) { suspect.Dismiss(); }
                if (targetVehicle.Exists()) { targetVehicle.Dismiss(); }
            } else
            {
                if (suspectWeapon.Exists()) { suspectWeapon.Delete(); }
                if (suspect.Exists()) { suspect.Delete(); }
                if (targetVehicle.Exists()) { targetVehicle.Delete(); }
            }

            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            base.End();
        }

        public static bool IsLSPDFRPluginRunning(string Plugin, Version minversion = null)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName();
                if (an.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || an.Version.CompareTo(minversion) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
