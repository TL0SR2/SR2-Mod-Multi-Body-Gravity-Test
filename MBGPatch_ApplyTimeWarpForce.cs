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
                    MBGPatch_CraftNode.MBGPatch_CraftNodeConstructor2.InitializeStaticFields(__instance);
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
                //MBGOrbit.CurrentTime = SunNode.FindPlanet(CurrentPlanet.Name).Orbit.Time;
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
                    Vector3d planetSolarPosition = SunNode.FindPlanet(CurrentPlanet.Name).SolarPosition;
                    Vector3d planetSolarVelocity = SunNode.FindPlanet(CurrentPlanet.Name).SolarVelocity;
                    P_V_Pair PlanetPVState = new P_V_Pair(planetSolarPosition, planetSolarVelocity);
                    P_V_Pair State = mbgOrbit.GetPVPairFromTime(MBGOrbit.CurrentTime) - PlanetPVState;
                    //Debug.Log($"TL0SR2 MBG Patch Log -- ApplyTimeWarpForce -- Get Craft State Position {State.Position} Velocity {State.Velocity} Position Length {State.Position.magnitude} Velocity Length {State.Velocity.magnitude}");
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
                Debug.LogError($"ApplyTimeWarpForce 补丁错误发现错误 ");
                Debug.LogException(ex);
                return true; // 发生错误时运行原方法
            }
        }
    }
}