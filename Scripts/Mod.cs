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
            DevConsoleService.Instance.RegisterCommand<double>("Multi Body Gravity -- Set Warp Delay Ratio", value => MBGOrbit.SetWarpDelayK(value));
            DevConsoleService.Instance.RegisterCommand<string>("Multi Body Gravity -- Change Orbit Line Reference", value => MBGOrbitLine.MBGOrbitLineChangeReference(value));
            DevConsoleService.Instance.RegisterCommand<double>("Multi Body Gravity -- Set Calculation Time", value => MBGOrbit.SetCalculationTime(value));
            DevConsoleService.Instance.RegisterCommand<double>("Multi Body Gravity -- Set Rotate Reference Angle", value => MBGOrbitLine.SetRotateInitAngle(value));
            DevConsoleService.Instance.RegisterCommand<int>("Multi Body Gravity -- Set Rotate Reference Mode", value => MBGOrbitLine.SetReferenceMode(value));
            DevConsoleService.Instance.RegisterCommand<double>("Multi Body Gravity -- Set Long Prediction Ratio", value => MBGOrbit.SetLongPredictionRatio(value));
            DevConsoleService.Instance.RegisterCommand<int>("Multi Body Gravity -- Set Lagrange Point Reference Mode", value => MBGOrbitLine.ChangeLPointType(value));
            new Harmony("com.TL0SR2.MultiBodyGravityTest").PatchAll();
        }

        public static void CodeAnnotation(string str)
        {
            string temp = str += "Code Annotation doesn't do anything!";
            return;
        }
    }
}