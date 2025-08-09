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



            Vector3d gravityFrame = new Vector3d(0, 0, 0);
            int n = 20;
            List<Vector3d> gravityList = new List<Vector3d>(n);
            for (int i = 0; i < n; i++)
            {
                gravityList.Add(new Vector3d(0, 0, 0));
            }
            int index = 0;
            if (!CraftFlightData2GravityFrameList.TryGetValue(__instance, out var tuple))
            {
                Debug.Log("" + n + "" + gravityList.Count);
                CraftFlightData2GravityFrameList.Add(__instance, (gravityList, index));
                // Debug.Log($"Initialized GravityFrameList count: {CraftFlightData2GravityFrameList[__instance].Item1.Count}");
            }
            else
            {
                gravityList = tuple.Item1;
                index = tuple.Item2;
                // Debug.Log($"GravityFrameList Count: {gravityList.Count}");
            }


            if (index >= n || !(gravityList[index].magnitude > 0))
            {
                double frameDeltaTime = __instance.TimeDelta;
                P_V_Pair PVPair = new P_V_Pair(craftNode.SolarPosition, craftNode.SolarVelocity);
                double time = craftNode.Orbit.Time;
                int CaculateStepCount = 20;
                double deltaTime = n * frameDeltaTime / CaculateStepCount;
                for (int i = 0; i < CaculateStepCount; i++)
                {
                    var h = deltaTime;
                    var x_n = time;
                    var y_n = PVPair;
                    Func<double, P_V_Pair, P_V_Pair> func = MBGMath.RKFunc;

                    var k1 = h * func(x_n, y_n);
                    var k2 = h * func(x_n + h / 2, y_n + k1 / 2);
                    var k3 = h * func(x_n + h / 4, y_n + (3 * k1 + k2) / 16);
                    var k4 = h * func(x_n + h / 2, y_n + k3 / 2);
                    var k5 = h * func(x_n + 3.0 / 4.0 * h, y_n + (-3 * k2 + 6 * k3 + 9 * k4) / 16);
                    var k6 = h * func(x_n + h, y_n + (k1 + 4 * k2 + 6 * k3 - 12 * k4 + 8 * k5) / 7);
                    PVPair = y_n + (7 * k1 + 32 * k3 + 12 * k4 + 32 * k5 + 7 * k6) / 90;
                    time += deltaTime;
                }
                P_V_Pair targetPV = PVPair;
                gravityList = GravityFrameListSolver.CalculateCorrectedAccelerations(
                    targetPV.Velocity, craftNode.SolarVelocity,
                    targetPV.Position, craftNode.SolarPosition,
                    MBGOrbit.CalculateGravityAtTime(craftNode.SolarPosition, craftNode.Orbit.Time),
                    MBGOrbit.CalculateGravityAtTime(targetPV.Position, time),
                    n, frameDeltaTime);
                index = 0;
                // P_V_Pair targetPV = MBGOrbit.GetMBGOrbit(craftNode).GetPVPairFromTime(2 * frameDeltaTime + craftNode.Orbit.Time);
                // Debug.Log($"CraftNode: {craftNode.NodeId} solarPosition: {craftNode.SolarPosition} solarVelocity: {craftNode.SolarVelocity}");
                // Debug.Log($"DPV: {targetPV.Position - craftNode.SolarPosition} {targetPV.Velocity - craftNode.SolarVelocity}");
            }

            if (!(gravityList.Count > 0) || gravityList[index].magnitude > 0)
            {
                gravityFrame = gravityList[index];
                CraftFlightData2GravityFrameList[__instance] = (gravityList, index + 1);
                // Debug.Log("GravityFrame: " + gravityFrame);
            }
            Debug.Log($"GravitylistCount: {gravityList.Count}, Index: {index}");

            if (gravityFrame.x == double.NaN || gravityFrame.x == double.PositiveInfinity || gravityFrame.x == 0)
            {
                // Debug.LogError($"GravityFrame is NaN or zero for CraftNode {craftNode.NodeId}");
                gravityFrame = MBGOrbit.CalculateGravityAtTime(craftNode.SolarPosition, craftNode.Orbit.Time);
            }
            else
            {

            }

            // gravityFrame = MBGOrbit.CalculateGravityAtTime(craftNode.SolarPosition, craftNode.Orbit.Time);
            _gravityFrameRef(__instance) = ToVector3(gravityFrame);
            GravityFrameNormalizedRef(__instance) = ToVector3(gravityFrame).normalized;
            GravityMagnitudeRef(__instance) = (float)gravityFrame.magnitude;
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



    public static class GravityFrameListSolver
    {
        private static double F_func(double x)
        {
            if (x >= 0 && x < 0.25)
            {
                return 4.0 * x;
            }
            return -4.0 / 3.0 * (x - 1.0);
        }
        private static double G_func(double x)
        {
            if (x >= 0 && x < 0.75)
            {
                return 4.0 / 3.0 * x;
            }
            return -4.0 * (x - 1.0);
        }

        /// <summary>
        /// 计算一个时间段内每个子步骤的修正后加速度向量序列。
        /// <returns>一个包含 N+1 个修正后加速度向量 (Vector3d) 的列表。如果方程无解，则返回一个空列表。</returns>
        public static List<Vector3d> CalculateCorrectedAccelerations(
            Vector3d finalVelocity, Vector3d initialVelocity,
            Vector3d finalPosition, Vector3d initialPosition,
            Vector3d startGravityAccel, Vector3d endGravityAccel,
            int N, double td)
        {
            // --- 步骤 1: 计算 k1 和 k2 所需的中间量 ---
            double A = 0.0, B = 0.0, E = 0.0, F = 0.0;
            for (int n = 0; n < N; n++)
            {
                double x = (double)n / N;
                double f_val = F_func(x);
                double g_val = G_func(x);
                A += f_val * (N - n);
                B += g_val * (N - n);
                E += f_val;
                F += g_val;
            }

            Vector3d C = finalVelocity - initialVelocity - (startGravityAccel + endGravityAccel) * 0.5 * N * td;

            double td_sq = td * td;
            Vector3d sumTermForD = Vector3d.zero;
            Vector3d delta_g = endGravityAccel - startGravityAccel;
            for (int n = 0; n < N; n++)
            {
                double x = (double)n / N;
                sumTermForD += (startGravityAccel + delta_g * x) * (N - n);
            }
            sumTermForD *= td_sq;
            Vector3d D = finalPosition - initialPosition - initialVelocity * N * td - sumTermForD;

            double det = (td * td * td) * (E * B - A * F);

            if (Math.Abs(det) < 1e-12)
            {
                Console.Error.WriteLine("Determinant is close to zero. Cannot solve for k1, k2. Returning empty list.");
                return new List<Vector3d>(); // 返回空列表表示失败
            }

            Vector3d k1_numerator = C * (B * td_sq) - D * (F * td);
            Vector3d k2_numerator = D * (E * td) - C * (A * td_sq);
            Vector3d k1 = k1_numerator / det;
            Vector3d k2 = k2_numerator / det;

            // --- 步骤 3: 生成并返回修正后的加速度序列 ---
            var correctedAccelerations = new List<Vector3d>(N);

            for (int n = 0; n < N; n++)
            {
                double x = (double)n / N;
                double f_val = F_func(x);
                double g_val = G_func(x);
                Vector3d baseAccel = startGravityAccel + delta_g * x;
                Vector3d correction = k1 * f_val + k2 * g_val;
                Vector3d correctedAccel = baseAccel + correction;
                correctedAccelerations.Add(correctedAccel);
            }

            return correctedAccelerations;
        }
    }








}