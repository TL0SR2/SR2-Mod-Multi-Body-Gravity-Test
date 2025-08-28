using UnityEngine;
using HarmonyLib;
using Assets.Scripts.State;

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