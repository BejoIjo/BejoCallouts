using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rage;
using LSPD_First_Response.Mod.API;


namespace BejoCallouts
{
    public class Main : Plugin
    {
        public override void Initialize()
        {
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedhandler;
            Game.LogTrivial("Plugin BejoCallouts" 
                + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() 
                + "has been initialized.");
            Game.LogTrivial("Go on duty to fully load BejoCallouts");
        }

        public override void Finally()
        {
            Game.LogTrivial("BejoCallouts has been cleaned up");
        }

        private static void OnOnDutyStateChangedhandler(bool onDuty)
        {
            if (onDuty)
            {
                RegisterCallouts();
            }
        }

        private static void RegisterCallouts()
        {
            Functions.RegisterCallout(typeof(Callouts.AmberAlert));
            Functions.RegisterCallout(typeof(Callouts.RapeVictim));
            Game.DisplayNotification("~r~Bejo Callouts~s~ has been successfully loaded. ~g~Enjoy!");
        }

    }
}
