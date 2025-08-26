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
using ModApi.Flight;
using ModApi;


namespace Assets.Scripts.Flight.Sim.MBG
{

    [HarmonyPatch(typeof(FlightState), "Time", MethodType.Setter)]
    public static class FlightState_set_Time_Patch
    {
        public static double LastUpdateTime = 0;
        public static double LastUpdateFixedTime = 0;
        static void Postfix(double value)
        {
            LastUpdateTime = value;
            LastUpdateFixedTime = Time.fixedTime;
        }
    }
}