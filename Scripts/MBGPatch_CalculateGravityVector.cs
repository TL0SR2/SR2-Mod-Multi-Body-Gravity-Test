using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using ModApi.Craft;
using System.Reflection;
using System;
using Assets.Scripts.Craft.FlightData;

namespace Assets.Scripts.Flight.Sim.MBG
{
    [HarmonyPatch(typeof(CraftFlightData), "UpdateGravityForce")]
    class MBGPatch_CalculateGravityVector
    {
        private static readonly AccessTools.FieldRef<CraftFlightData, ICraftScript> _craftScriptRef = AccessTools.FieldRefAccess<CraftFlightData, ICraftScript>("_craftScript");
        private static readonly AccessTools.FieldRef<CraftFlightData, Vector3> _gravityFrameRef = AccessTools.FieldRefAccess<CraftFlightData, Vector3>("_gravityFrame");

        public static readonly AccessTools.FieldRef<CraftFlightData, Vector3> GravityFrameNormalizedRef = AccessTools.FieldRefAccess<CraftFlightData, Vector3>("<GravityFrameNormalized>k__BackingField");
        public static readonly AccessTools.FieldRef<CraftFlightData, float> GravityMagnitudeRef = AccessTools.FieldRefAccess<CraftFlightData, float>("<GravityMagnitude>k__BackingField");
        public static readonly AccessTools.FieldRef<CraftFlightData, Vector3d> GravityRef = AccessTools.FieldRefAccess<CraftFlightData, Vector3d>("<Gravity>k__BackingField");

        private static Dictionary<CraftFlightData, (List<Vector3d>, int)> CraftFlightData2GravityFrameList = new Dictionary<CraftFlightData, (List<Vector3d>, int)>();

        private static bool FindFields = false;



        [HarmonyPrefix]
        static bool Prefix(CraftFlightData __instance)
        {
            if (FindFields)
            {
                var fields = typeof(CraftFlightData).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    Debug.Log($"Field: {field.Name}, Type: {field.FieldType}");
                }
                FindFields = false;
            }

            var craftNode = _craftScriptRef(__instance).CraftNode as CraftNode;
            var referenceFrame = craftNode.CraftScript.ReferenceFrame;
            //MBGOrbit.CurrentTime = craftNode.Orbit.Time;
            //定期对mbgorbit执行重新计算的方法，以避免出现PVList为空值的情况
            MBGOrbit mbgOrbit = MBGOrbit.GetMBGOrbit(craftNode);
            if (mbgOrbit == null)
            {
                // Debug.LogError($"MBGOrbit for CraftNode {craftNode.NodeId} is null");
                mbgOrbit = new MBGOrbit(craftNode, craftNode.FlightState.Time, craftNode.SolarPosition, craftNode.SolarVelocity);
                //return true;
            }

            if (mbgOrbit.EndTime - MBGOrbit.CurrentTime < MBGOrbit.ForceReCalculateBeforeEnd * Math.Max(Game.Instance.FlightScene.TimeManager.CurrentMode.TimeMultiplier, 1))
            {
                Debug.Log("TL0SR2 MBG -- CalculateGravityVector -- Near End Time. Force ReCalculate.");
                mbgOrbit.ForceReCalculation();
            }

            // Debug.Log("mbg calc: " + craftNode.Orbit.Time + " craft: " + craftNode.Name);


            var time = Time.fixedTime - FlightState_set_Time_Patch.LastUpdateFixedTime + FlightState_set_Time_Patch.LastUpdateTime;

            var gravityFrame = MBGOrbit.CalculateGravityAtTime(referenceFrame.FrameToPlanetPosition(craftNode.CraftScript.FramePosition) + craftNode.Parent.GetSolarPositionAtTime(time), time);

            Debug.Log($"gravityFrame: {{ {gravityFrame.x:E3}, {gravityFrame.y:E3}, {gravityFrame.z:E3} }}");

            _gravityFrameRef(__instance) = gravityFrame.ToVector3();
           
            GravityMagnitudeRef(__instance) = (float)gravityFrame.magnitude;

            GravityFrameNormalizedRef(__instance) = (referenceFrame.PlanetToFrameVector(craftNode.Parent.CalculateGravityVector(referenceFrame.FrameToPlanetPosition(craftNode.CraftScript.FramePosition), 1.0))).normalized;

            return false;

        }
        public static Vector3d ToVector3d(UnityEngine.Vector3 v)
        {
            return new Vector3d((double)v.x, (double)v.y, (double)v.z);
        }

    }


    // [HarmonyPatch(typeof(PlanetNode), nameof(PlanetNode.CalculateGravityVector))]
    // public class MBGPatch_CalculateGravityVector
    // {
    //     private static bool FindFields = true;
    //     static void Postfix(ref Vector3d __result, PlanetNode __instance, Vector3d position, double mass)
    //     {

    //         if (FindFields)
    //         {
    //             var fields = typeof(CraftFlightData).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
    //             foreach (var field in fields)
    //             {
    //                 Debug.Log($"Field: {field.Name}, Type: {field.FieldType}");
    //             }
    //             FindFields = false;
    //         }

    //         Vector3d craftSolarPosition = __instance.SolarPosition + position;
    //         IPlanetNode SunNode = (IPlanetNode)__instance;
    //         while (SunNode.Parent != null)
    //         {
    //             SunNode = SunNode.Parent;
    //         }
    //         /*if (MBGOrbit.SunNode == null)
    //         {
    //             while (SunNode.Parent != null)
    //             {
    //                 SunNode = SunNode.Parent;
    //             }
    //             MBGOrbit.SunNode = SunNode;
    //         }
    //         else
    //         {
    //             SunNode = MBGOrbit.SunNode;
    //         }*/
    //         IReadOnlyList<IPlanetData> planetList = __instance.PlanetData.SolarSystemData.Planets;
    //         /*if (MBGOrbit.PlanetList == null || MBGOrbit.PlanetList.Count == 0)
    //         {
    //             MBGOrbit.planetList = planetList;
    //         }*/
    //         Vector3d TotalGravity = new Vector3d(0, 0, 0);
    //         foreach (IPlanetData planetData in planetList)
    //         {
    //             Vector3d planetSolarPosition = SunNode.FindPlanet(planetData.Name).SolarPosition;
    //             Vector3d positionVector = planetSolarPosition - craftSolarPosition;
    //             Vector3d GravityForce = (6.67384E-11 * planetData.Mass * mass / positionVector.sqrMagnitude) * positionVector.normalized;
    //             TotalGravity += GravityForce;
    //         }
    //         __result = TotalGravity;
    //     }
    // }


    /*
    [HarmonyPatch]
    public class MBGPatch_SetModeImmediate
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var craftNodeType = AccessTools.TypeByName("Assets.Scripts.Flight.TimeManager");
            var method = AccessTools.Method(craftNodeType, "SetModeImmediate");
            return method;
        }


        static void Prefix(TimeManager __instance, int modeIndex, bool forceChange)
        {
            FieldInfo modeIndexField = AccessTools.Field(typeof(TimeManager), "_modeIndex");
            if (forceChange || (int)modeIndexField.GetValue(__instance) != modeIndex || __instance.CurrentMode == null)
            {
                __instance.TimeMultiplierModeChanging += e => MBGOrbit.ChangeTimeStatic(e);
            }
        }
    }
    */




}