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

namespace Assets.Scripts.Flight.Sim.MBG
{
    [HarmonyPatch(typeof(PlanetNode), nameof(PlanetNode.CalculateGravityVector))]
    public class MBGPatch_CalculateGravityVector
    {
        static void Postfix(ref Vector3d __result, PlanetNode __instance, Vector3d position, double mass)
        {
            Vector3d craftSolarPosition = __instance.SolarPosition + position;
            IPlanetNode SunNode = (IPlanetNode)__instance;
            while (SunNode.Parent != null)
            {
                SunNode = SunNode.Parent;
            }
            /*if (MBGOrbit.SunNode == null)
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
            }*/
            IReadOnlyList<IPlanetData> planetList = __instance.PlanetData.SolarSystemData.Planets;
            /*if (MBGOrbit.PlanetList == null || MBGOrbit.PlanetList.Count == 0)
            {
                MBGOrbit.planetList = planetList;
            }*/
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
        return AccessTools.Method(craftNodeType, "ApplyTimeWarpForce", new[] { typeof(double) });
    }

    static bool Prefix(CraftNode __instance, double deltaTime)
    {
        try
        {
            // 确保 SunNode 和 planetList 已初始化
            if (MBGOrbit.SunNode == null || MBGOrbit.planetList == null || MBGOrbit.planetList.Count == 0)
            {
                MBGPatch_CraftNodeConstructor2.InitializeStaticFields(__instance);
            }

            IPlanetNode SunNode = MBGOrbit.SunNode;
            if (SunNode == null)
            {
                Debug.LogError("MBGOrbit.SunNode is null");
                return true; // 跳回原方法
            }

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
                    Vector3d GravityForce = (MBGOrbit.GravityConst * planetData.Mass * mass / positionVector.sqrMagnitude) * positionVector.normalized;
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
                MBGOrbit mbgOrbit = MBGOrbit.GetMBGOrbit(__instance);
                if (mbgOrbit == null)
                {
                    Debug.LogError($"MBGOrbit for CraftNode {__instance.NodeId} is null");
                    return true;
                }

                if (mbgOrbit.EndTime - MBGOrbit.CurrentTime < MBGOrbit.ForceReCalculateBeforeEnd * Game.Instance.FlightScene.TimeManager.CurrentMode.TimeMultiplier)
                {
                    mbgOrbit.ForceReCalculation();
                }
                //此处已经替换成自己计算得到的位置信息
                P_V_Pair State = mbgOrbit.GetPVPairFromTime(MBGOrbit.CurrentTime);
                Vector3d vector = timeWarpForceTotal / __instance.CraftScript.Mass * (float)deltaTime;
                Vector3d velocity = State.Velocity + vector;
                __instance.SetStateVectorsAtDefaultTime(State.Position, velocity);
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
        catch (Exception ex)
        {
            Debug.LogError($"ApplyTimeWarpForce 补丁错误：{ex.Message}");
            return true; // 发生错误时运行原方法
        }
    }
    
}

    [HarmonyPatch]
    public class MBGPatch_CraftNodeConstructor1
    {
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
                    typeof(CraftScript)
                });
        }

        [HarmonyPostfix]
        public static void Postfix(CraftNode __instance, Vector3d position, Vector3d velocity, Quaterniond heading, FlightState flightState, double primaryMass, CraftData craftData, Assets.Scripts.Craft.CraftScript craftScript)
        {
            try
            {
                // 初始化 SunNode 和 planetList
                InitializeStaticFields(__instance);

                MBGOrbit mbgOrbit = new MBGOrbit(flightState.Time, position, velocity);
                MBGOrbit.SetMBGOrbit(__instance, mbgOrbit);
                Debug.Log($"为 CraftNode {__instance.NodeId} 初始化 MBGOrbit");
            }
            catch (Exception ex)
            {
                Debug.LogError($"CraftNodeConstructor1 补丁错误：{ex.Message}");
            }
        }
        private static void InitializeStaticFields(CraftNode craftNode)
        {
            if (MBGOrbit.SunNode == null && craftNode.Parent != null)
            {
                IPlanetNode sunNode = craftNode.Parent;
                while (sunNode.Parent != null)
                {
                    sunNode = sunNode.Parent;
                }
                MBGOrbit.SunNode = sunNode;
            }
            if (MBGOrbit.planetList == null || MBGOrbit.planetList.Count == 0)
            {
                MBGOrbit.planetList = craftNode.Parent?.PlanetData?.SolarSystemData?.Planets;
            }
        }
    }



    [HarmonyPatch]
    public class MBGPatch_CraftNodeConstructor2
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Constructor(
                typeof(CraftNode),
                new Type[] {
                    typeof(ICraftNodeData),
                    typeof(FlightState),
                    typeof(double),
                    typeof(CraftData),
                    typeof(CraftScript),
                    typeof(XElement)
                });
        }

        [HarmonyPostfix]
        public static void Postfix(CraftNode __instance, ICraftNodeData data, FlightState flightState, double primaryMass, CraftData craftData, CraftScript craftScript, XElement pendingXml)
        {
            try
            {
                // 初始化 SunNode 和 planetList
                InitializeStaticFields(__instance);

                MBGOrbit mbgOrbit = new MBGOrbit(flightState.Time, data.Position, data.Velocity);
                MBGOrbit.SetMBGOrbit(__instance, mbgOrbit);
                Debug.Log($"为 CraftNode {__instance.NodeId} 初始化 MBGOrbit");
            }
            catch (Exception ex)
            {
                Debug.LogError($"CraftNodeConstructor2 补丁错误：{ex.Message}");
            }
        }

        // 辅助方法：初始化静态字段
        public static void InitializeStaticFields(CraftNode craftNode)
        {
            if (MBGOrbit.SunNode == null && craftNode.Parent != null)
            {
                IPlanetNode sunNode = craftNode.Parent;
                while (sunNode.Parent != null)
                {
                    sunNode = sunNode.Parent;
                }
                MBGOrbit.SunNode = sunNode;
            }
            if (MBGOrbit.planetList == null || MBGOrbit.planetList.Count == 0)
            {
                MBGOrbit.planetList = craftNode.Parent?.PlanetData?.SolarSystemData?.Planets;
            }
        }
    }

    [HarmonyPatch]
    public class MBGPatch_DestroyCraft
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var craftNodeType = AccessTools.TypeByName("Assets.Scripts.Flight.Sim.CraftNode");
            var method = AccessTools.Method(craftNodeType, "DestroyCraft");
            return method;
        }

        static void Postfix(CraftNode __instance)
        {
            MBGOrbit.RemoveMBGOrbit(__instance);        
        }
    }
}