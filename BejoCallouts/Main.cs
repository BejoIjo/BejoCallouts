using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rage;
using LSPD_First_Response.Mod.API;
using System.Reflection;


namespace BejoCallouts
{
    public class Main : Plugin
    {
        public override void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedhandler;
            Game.LogTrivial("Plugin BejoCallouts" 
                + Assembly.GetExecutingAssembly().GetName().Version.ToString() 
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
            Functions.RegisterCallout(typeof(Callouts.CarTheftSuspect));
            Functions.RegisterCallout(typeof(Callouts.RapeVictim));
            Game.DisplayNotification("~r~Bejo Callouts~s~ has been successfully loaded. ~g~Enjoy!");
        }


        public static Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower()))
                {
                    return assembly;
                }
            }
            return null;
        }

    }
}
