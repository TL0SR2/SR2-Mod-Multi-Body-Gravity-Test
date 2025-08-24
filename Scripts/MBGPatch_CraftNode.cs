using UnityEngine;
using HarmonyLib;
using ModApi.Flight.Sim;
using System.Reflection;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGPatch_CraftNode
    {

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
}