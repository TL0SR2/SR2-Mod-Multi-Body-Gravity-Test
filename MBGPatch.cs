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

        private static Dictionary<CraftFlightData, Vector3d> CraftFlightData2GravityFrame2 = new Dictionary<CraftFlightData, Vector3d>();

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


            Vector3d gravityFrame1 = new Vector3d(0, 0, 0);
            Vector3d gravityFrame2 = new Vector3d(0, 0, 0);
            if (!CraftFlightData2GravityFrame2.TryGetValue(__instance, out gravityFrame2))
            {
                CraftFlightData2GravityFrame2.Add(__instance, new Vector3d(0, 0, 0));
            }


            if (gravityFrame2.magnitude > 0)
            {
                gravityFrame1 = gravityFrame2;
                CraftFlightData2GravityFrame2[__instance] = new Vector3d(0, 0, 0);
                Debug.Log("GravityFrame2: " + gravityFrame2);
            }
            else
            {
                CraftNode craftNode = _craftScriptRef(__instance).CraftNode as CraftNode;
                //定期对mbgorbit执行重新计算的方法，以避免出现PVList为空值的情况
                MBGOrbit mbgOrbit = MBGOrbit.GetMBGOrbit(craftNode);
                    if (mbgOrbit == null)
                    {
                        Debug.LogError($"MBGOrbit for CraftNode {craftNode.NodeId} is null");
                        mbgOrbit = new MBGOrbit(craftNode, craftNode.FlightState.Time, craftNode.SolarPosition, craftNode.SolarVelocity);
                        //return true;
                    }

                    if (mbgOrbit.EndTime - MBGOrbit.CurrentTime < MBGOrbit.ForceReCalculateBeforeEnd * Game.Instance.FlightScene.TimeManager.CurrentMode.TimeMultiplier)
                    {
                        mbgOrbit.ForceReCalculation();
                    }
                double deltaTime = __instance.TimeDelta;
                try
                {
                    // P_V_Pair targetPV = MBGOrbit.GetMBGOrbit(craftNode).GetPVPairFromTime(2 * deltaTime + craftNode.Orbit.Time);
                    // gravityFrame1 = (targetPV.Position - craftNode.SolarPosition) / (deltaTime * deltaTime) - 2 * craftNode.SolarVelocity;
                    // gravityFrame2 = (targetPV.Velocity - craftNode.SolarVelocity) / deltaTime - 2 * gravityFrame1;

                    gravityFrame1 = MBGOrbit.CalculateGravityAtTime(craftNode.SolarPosition, craftNode.Orbit.Time);
                    gravityFrame2 = MBGOrbit.CalculateGravityAtTime(craftNode.SolarPosition, craftNode.Orbit.Time);


                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error calculating gravity frame: {ex.Message}");
                    // 跳回原方法
                    Vector3d craftSolarPosition = craftNode.SolarPosition;
                    IPlanetNode SunNode = craftNode.Parent;
                    while (SunNode.Parent != null)
                    {
                        SunNode = SunNode.Parent;
                    }
                    IReadOnlyList<IPlanetData> planetList = craftNode.Parent.PlanetData.SolarSystemData.Planets;

                    Vector3d totalGravity = new Vector3d(0, 0, 0);
                    foreach (IPlanetData planetData in planetList)
                    {
                        Vector3d planetSolarPosition = SunNode.FindPlanet(planetData.Name).SolarPosition;
                        Vector3d positionVector = planetSolarPosition - craftSolarPosition;
                        Vector3d gravityForce = (6.67384E-11 * planetData.Mass / positionVector.sqrMagnitude) * positionVector.normalized;
                        totalGravity += gravityForce;
                    }

                    gravityFrame1 = totalGravity;
                    gravityFrame2 = gravityFrame1;
                }



                //Debug.Log("test" + craftNode.SolarPosition + " " + craftNode.Orbit.Time);

                CraftFlightData2GravityFrame2[__instance] = gravityFrame2;
            }

            _gravityFrameRef(__instance) = ToVector3(gravityFrame1);
            GravityFrameNormalizedRef(__instance) = ToVector3(gravityFrame1).normalized;
            GravityMagnitudeRef(__instance) = (float)gravityFrame1.magnitude;
            return false;

        }

        public static Vector3 ToVector3(Vector3d v)
        {
            return new Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        public static Vector3d ToVector3d(Vector3 v)
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
                        mbgOrbit = new MBGOrbit(__instance, __instance.FlightState.Time, __instance.SolarPosition, __instance.SolarVelocity);
                        //return true;
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

    // [HarmonyPatch]
    // public class MBGPatch_CraftNodeConstructor1
    // {
    //     [HarmonyTargetMethod]
    //     public static MethodBase TargetMethod()
    //     {
    //         return AccessTools.Constructor(
    //             typeof(CraftNode),
    //             new Type[] {
    //                 typeof(Vector3d),
    //                 typeof(Vector3d),
    //                 typeof(Quaterniond),
    //                 typeof(FlightState),
    //                 typeof(double),
    //                 typeof(CraftData),
    //                 typeof(CraftScript)
    //             });
    //     }

    //     [HarmonyPostfix]
    //     public static void Postfix(CraftNode __instance, Vector3d position, Vector3d velocity, Quaterniond heading, FlightState flightState, double primaryMass, CraftData craftData, Assets.Scripts.Craft.CraftScript craftScript)
    //     {
    //         try
    //         {
    //             // 初始化 SunNode 和 planetList
    //             try
    //             {
    //                 InitializeStaticFields(__instance);
    //             }
    //             catch (Exception ex)
    //             {
    //                 Debug.LogError($"CraftNodeConstructor1 补丁错误：{ex.Message}");
    //             }

    //             MBGOrbit mbgOrbit = new MBGOrbit(flightState.Time, position, velocity);
    //             MBGOrbit.SetMBGOrbit(__instance, mbgOrbit);
    //             Debug.Log($"为 CraftNode {__instance.NodeId} 初始化 MBGOrbit 1");
    //         }
    //         catch (Exception ex)
    //         {
    //             Debug.LogError($"CraftNodeConstructor1 补丁错误：{ex.Message}");
    //         }
    //     }
    //     private static void InitializeStaticFields(CraftNode craftNode)
    //     {
    //         if (MBGOrbit.SunNode == null && craftNode.Parent != null)
    //         {
    //             IPlanetNode sunNode = craftNode.Parent;
    //             while (sunNode.Parent != null)
    //             {
    //                 sunNode = sunNode.Parent;
    //             }
    //             MBGOrbit.SunNode = sunNode;
    //         }
    //         if (MBGOrbit.planetList == null || MBGOrbit.planetList.Count == 0)
    //         {
    //             MBGOrbit.planetList = craftNode.Parent?.PlanetData?.SolarSystemData?.Planets;
    //         }
    //     }
    // }



    [HarmonyPatch]
    public class MBGPatch_CraftNodeConstructor2
    {
        // [HarmonyTargetMethod]
        // public static MethodBase TargetMethod()
        // {
        //     return AccessTools.Constructor(
        //         typeof(CraftNode),
        //         new Type[] {
        //             typeof(ICraftNodeData),
        //             typeof(FlightState),
        //             typeof(double),
        //             typeof(CraftData),
        //             typeof(CraftScript),
        //             typeof(XElement)
        //         });
        // }

        // [HarmonyPostfix]
        // public static void Postfix(CraftNode __instance, ICraftNodeData data, FlightState flightState, double primaryMass, CraftData craftData, CraftScript craftScript, XElement pendingXml)
        // {
        //     // 初始化 SunNode 和 planetList
        //     InitializeStaticFields(__instance);

        //     MBGOrbit mbgOrbit = new MBGOrbit(__instance, flightState.Time, data.Position, data.Velocity);
        //     // MBGOrbit.SetMBGOrbit(__instance, mbgOrbit);
        //     Debug.Log($"为 CraftNode {__instance.NodeId} 初始化 MBGOrbit 2");
        // }

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




        [HarmonyPatch(typeof(CraftNode), "FlightUpdate")]
        public class MBGPatch_CraftNode_FlightUpdate
        {
            [HarmonyPostfix]
            public static void Postfix(CraftNode __instance)
            {
                if (MBGOrbit.GetMBGOrbit(__instance) == null)
                {
                    // 初始化 SunNode 和 planetList
                    InitializeStaticFields(__instance);

                    MBGOrbit mbgOrbit = new MBGOrbit(__instance, __instance.FlightState.Time, __instance.SolarPosition, __instance.SolarVelocity);
                    // MBGOrbit.SetMBGOrbit(__instance, mbgOrbit);
                    Debug.Log($"为 CraftNode {__instance.NodeId} 初始化 MBGOrbit 3");
                }

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