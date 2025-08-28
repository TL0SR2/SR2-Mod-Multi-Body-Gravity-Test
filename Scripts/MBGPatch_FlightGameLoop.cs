using UnityEngine;
using HarmonyLib;
using System;
using Assets.Scripts.GameLoop;

namespace Assets.Scripts.Flight.Sim.MBG
{


    [HarmonyPatch(typeof(FlightGameLoop), "Awake")]
    public static class FlightGameLoop_Awake_Patch
    {
        static void Postfix(FlightGameLoop __instance)
        {
            var customPostFixedUpdateScript = new MBGFlightPostFixedUpdate();
            var scriptsField = AccessTools.Field(typeof(FlightGameLoop), "_scripts");
            if (scriptsField == null) throw new NullReferenceException("Cannot find field '_scripts'");
            var scriptsCollection = scriptsField.GetValue(__instance);
            var registerMethod = scriptsCollection.GetType().GetMethod("Register");
            if (registerMethod == null) throw new NullReferenceException("Cannot find 'Register' method in FlightUpdateGroupCollection.");
            registerMethod.Invoke(scriptsCollection, new object[] { customPostFixedUpdateScript });
            Debug.Log("MBGFlightPostFixedUpdate: Custom script registered with the game loop.");
        }

    }

}