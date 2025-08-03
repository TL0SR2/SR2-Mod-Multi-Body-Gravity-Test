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

namespace Assets.Scripts.Flight.Sim.MBG
{
    [HarmonyPatch(typeof(PlanetNode), nameof(PlanetNode.CalculateGravityVector))]
    public class MBGPatch_CalculateGravityVector
    {
        static void Postfix(ref Vector3d __result, PlanetNode __instance, Vector3d position, double mass)
        {
            Vector3d craftSolarPosition = __instance.SolarPosition + position;
            IPlanetNode SunNode = (IPlanetNode)__instance;
            if (MBGOrbit.SunNode == null)
            {
                while (SunNode.Parent != null)
                {
                    SunNode = SunNode.Parent;
                }
                MBGOrbit.SunNode = SunNode;
            }
            else
            {
                SunNode = MBGOrbit.SunNode;
            }
            IReadOnlyList<IPlanetData> planetList = __instance.PlanetData.SolarSystemData.Planets;
            if (MBGOrbit.planetList == null || MBGOrbit.planetList.Count == 0)
            {
                MBGOrbit.planetList = planetList;
            }
            Vector3d TotalGravity = new Vector3d(0, 0, 0);
            foreach (IPlanetData planetData in planetList)
            {
                Vector3d planetSolarPosition = SunNode.FindPlanet(planetData.Name).SolarPosition;
                Vector3d positionVector = planetSolarPosition - craftSolarPosition;
                Vector3d GravityForce = (6.67384E-11 * planetData.Mass * mass / positionVector.sqrMagnitude) * positionVector.normalized;
                TotalGravity += GravityForce;
            }
            __result = TotalGravity;
        }
    }

    [HarmonyPatch]
    public class MBGPatch_ApplyTimeWarpForce
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var craftNodeType = AccessTools.TypeByName("Assets.Scripts.Flight.Sim.CraftNode");
            var method = AccessTools.Method(craftNodeType, "ApplyTimeWarpForce", new[] { typeof(double) });
            return method;
        }
        static bool Prefix(CraftNode __instance, double deltaTime)
        {
            IPlanetNode SunNode = MBGOrbit.SunNode;
            Vector3d craftSolarPosition = __instance.SolarPosition;
            double mass = (double)__instance.CraftMass;

            IReadOnlyList<IPlanetData> planetList = MBGOrbit.planetList;
            Vector3d TotalGravity = new Vector3d(0, 0, 0);
            IPlanetData CurrentPlanet = __instance.Parent.PlanetData;
            foreach (IPlanetData planetData in planetList)
            {
                if (planetData.Name != CurrentPlanet.Name)
                {
                    Vector3d planetSolarPosition = SunNode.FindPlanet(planetData.Name).SolarPosition;
                    Vector3d positionVector = planetSolarPosition - craftSolarPosition;
                    Vector3d GravityForce = (6.67384E-11 * planetData.Mass * mass / positionVector.sqrMagnitude) * positionVector.normalized;
                    TotalGravity += GravityForce;
                }
            }


            var field_timeWarpForceTotal = AccessTools.Field(__instance.GetType(), "_timeWarpForceTotal");
            var field_craftScript = AccessTools.Field(__instance.GetType(), "_craftScript");
            var property_Heading = AccessTools.Property(__instance.GetType(), "Heading");
            Vector3 Temp = (Vector3)field_timeWarpForceTotal.GetValue(__instance);
            Vector3d timeWarpForceTotal = new Vector3d(Temp.x, Temp.y, Temp.z);
            timeWarpForceTotal += TotalGravity;
            CraftScript craftScript = (CraftScript)field_craftScript.GetValue(__instance);
            Quaterniond Heading = (Quaterniond)property_Heading.GetValue(__instance);

            if (__instance.CraftScript != null && timeWarpForceTotal.sqrMagnitude > 0f)
            {
                Vector3d vector = timeWarpForceTotal / __instance.CraftScript.Mass * (float)deltaTime;
                Vector3d velocity = __instance.Orbit.Velocity + vector;
                __instance.SetStateVectorsAtDefaultTime(__instance.Orbit.Position, velocity);
                timeWarpForceTotal = new Vector3d(0, 0, 0);
            }
            CraftControls controls = __instance.Controls;
            if (controls != null && controls.TargetHeading != null)
            {
                Vector3 forward = __instance.CraftScript.CenterOfMass.forward;
                Vector3 toDirection = __instance.GameView.ReferenceFrame.PlanetToFrameVector(__instance.Controls.TargetDirection.Value);
                Quaternion b = Quaternion.FromToRotation(forward, toDirection) * craftScript.Transform.rotation;
                craftScript.Transform.rotation = Quaternion.Lerp(craftScript.Transform.rotation, b, 0.1f * (float)deltaTime);
                Heading = __instance.GameView.ReferenceFrame.FrameToPlanetRotation(craftScript.transform.rotation);
            }

            property_Heading.SetValue(__instance, Heading);
            field_timeWarpForceTotal.SetValue(__instance, (Vector3)timeWarpForceTotal);
            field_craftScript.SetValue(__instance, craftScript);
            return false;
        }
    }
/*
    [HarmonyPatch]
    public class MBGPatch_CraftNode
    {
        private static readonly Dictionary<CraftNode, MBGOrbit> craftNodeOrbitMap = new Dictionary<CraftNode, MBGOrbit>();

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Constructor(
                typeof(CraftNode),
                new Type[] {
                typeof(Vector3d),
                typeof(Vector3d),
                typeof(Quaterniond),
                typeof(FlightState),
                typeof(double),
                typeof(CraftData),
                typeof(Assets.Scripts.Craft.CraftScript)
                });
        }
        
        [HarmonyPostfix]
    public static void Postfix(CraftNode __instance, Vector3d position, Vector3d velocity, Quaterniond heading, FlightState flightState, double primaryMass, CraftData craftData, Assets.Scripts.Craft.CraftScript craftScript)
    {
        try
        {
            // Create a new MBGOrbit instance (customize initialization as needed)
            MBGOrbit mbgOrbit = new MBGOrbit(position, velocity);

            // Store the MBGOrbit instance in the dictionary, keyed by the CraftNode instance
            craftNodeOrbitMap[__instance] = mbgOrbit;

            Debug.Log($"MBGOrbit initialized for CraftNode {__instance.NodeId} with Position: {mbgOrbit.Position}, Velocity: {mbgOrbit.Velocity}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in CraftNode constructor patch: {ex.Message}");
        }
    }
    }*/
}