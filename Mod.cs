using Assets.Packages.DevConsole;
using HarmonyLib;

namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi;
    using ModApi.Common;
    using ModApi.Mods;
    using UnityEngine;
    using Assets.Scripts.DevConsole;
    using Assets.Scripts.Flight.Sim.MBG;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod>();

        protected override void OnModInitialized()
        {
            DevConsoleService.Instance.RegisterCommand<double>("Multi Body Gravity -- Set Math Calculation Step", value => MBGMath.SetMBGCalculationStep(value));
            new Harmony("com.TL0SR2.MultiBodyGravityTest").PatchAll();
            DevConsoleApi.RegisterCommand("SetMGBStep", delegate(double input)
            {
                if (input > 0)
                {
                    MBGMath.SetStep(input);
                    Debug.LogFormat("Set Step to {0}", input);
                }
                else
                {
                    Debug.LogFormat("Invalid Step input: {0}", input);
                }
            });
        }

        public static void CodeAnnotation(string str)
        {
            string temp = str += "Code Annotation doesn't do anything!";
            return;
        }
    }
}