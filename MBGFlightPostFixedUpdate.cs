using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Assets.Scripts.Flight.Sim;
using ModApi.Planet;
using ModApi.Flight.Sim;
using Assets.Scripts.Craft;
using ModApi.Craft;
using System.Reflection;
using Assets.Scripts.Flight.Sim.MBG;
using Assets.Scripts.State;
using System;
using ModApi.Scripts.State;
using System.Xml.Linq;
using Assets.Scripts.Craft.FlightData;
using ModApi.GameLoop.Interfaces;
using ModApi.GameLoop;
using Assets.Scripts.GameLoop;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGFlightPostFixedUpdate : IFlightPreFixedUpdate
    {
        public bool StartMethodCalled { get; set; }

        public void FlightPreFixedUpdate(in FlightFrameData frame)
        {
            // Debug.Log("MBGFlightPostFixedUpdate called with frame: " + frame.FlightScene.FlightState.Time + " craft: " + frame.FlightScene.CraftNode.Name);
        }

        public int GetInstanceID()
        {
            return this.GetHashCode();
        }
    }





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