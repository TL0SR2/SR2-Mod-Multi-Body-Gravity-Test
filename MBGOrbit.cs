using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ModApi.Flight.Sim;
using Assets.Scripts.Flight.Sim;
using ModApi.Planet;
using System.Linq;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbit
    {

        public static IPlanetNode SunNode { get; set; }
        public static IReadOnlyList<IPlanetData> planetList { get; set; }
        public MBGOrbit(CraftNode craftNode, double startTime, Vector3d startPosition, Vector3d startVelocity)
        {
            SetMBGOrbit(craftNode, this);
            this._startTime = startTime;
            this.MBG_PVList.Add(new P_V_Pair(startPosition, startVelocity));
            try
            {
                FindPlanetInformation();
                // ForceReCalculation();
                Debug.Log($"MBGOrbit.MBGOrbit -- Initialized successfully but ForceReCalculation not called");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MBGOrbit.MBGOrbit -- {ex.Message}");
            }
        }

        public void FindPlanetInformation()
        {
            var CurrentPlanet = CurrentCraft.Parent;
            planetList = CurrentPlanet.PlanetData.SolarSystemData.Planets;
            IPlanetNode sunNode = CurrentPlanet;
            while (sunNode.Parent != null)
            {
                sunNode = sunNode.Parent;
            }
            SunNode = sunNode;
        }
        public void MBG_Numerical_Calculation(double startTime, double elapsedTime)
        {
            int n = (int)Math.Floor((startTime - _startTime) / _listAccuracyTime);
            //int step = (int)Math.Floor(elapsedTime / _listAccuracyTime);
            MBGMath.NumericalIntegration(MBG_PVList[n], n * _listAccuracyTime + _startTime, elapsedTime - elapsedTime % _listAccuracyTime, out List<P_V_Pair> PVList);
            UpdateList<P_V_Pair>(ref MBG_PVList, PVList, n);
            //接下来应该在此处执行激活重绘轨道线的操作
        }

        public void ForceReCalculation()
        {
            MBG_Numerical_Calculation(CurrentTime, _defaultDurationTime);
        }
        public P_V_Pair GetPVPairFromTime(double time)
        {
            double durationTime = time - _startTime;
            int n = (int)Math.Floor(durationTime / _listAccuracyTime);
            if (n < 0 || n > (MBG_PVList.Count - 1))
            {
                Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBGOrbit.GetPVPairFromTime -- Time Out Of Range");
                return P_V_Pair.Zero;
            }
            return MBGMath.Interpolation(MBG_PVList[n], MBG_PVList[n + 1], durationTime / _listAccuracyTime - n);
        }

        public static MBGOrbit GetMBGOrbit(CraftNode craftNode)
        {
            return craftNodeOrbitMap.TryGetValue(craftNode, out MBGOrbit orbit) ? orbit : null;
        }

        public static void SetMBGOrbit(CraftNode craftNode, MBGOrbit orbit)
        {
            if (!craftNodeOrbitMap.ContainsKey(craftNode))
            {
                craftNodeOrbitMap.Add(craftNode, orbit);
            }
            else
            {
                craftNodeOrbitMap[craftNode] = orbit;
            }

        }

        public static void RemoveMBGOrbit(CraftNode craftNode)
        {
            if (craftNodeOrbitMap.ContainsKey(craftNode))
            {
                craftNodeOrbitMap.Remove(craftNode);
            }

        }
        public static Dictionary<double, Vector3d> GetPlanetsPositionAtTime(double time)
        {
            // 输出值Dictionary<double, Vector3d>中每一个k-v对表示一个行星在time时的数据；double表示行星的质量，Vector3d表示行星的位置。
            Dictionary<double, Vector3d> result = new Dictionary<double, Vector3d> { };
            foreach (IPlanetData planetData in planetList)
            {
                IPlanetNode planetNode = SunNode.FindPlanet(planetData.Name);
                double mass = planetNode.PlanetData.Mass;
                Vector3d SolarPosition = planetNode.GetSolarPositionAtTime(time);
                result.Add(mass, SolarPosition);
            }
            return result;
        }
        public static Vector3d CalculateGravityAtTime(Vector3d CraftPosition, double time)
        {
            Vector3d result = new Vector3d(0, 0, 0);
            foreach (var planetData in planetList)
            {
                IPlanetNode planetNode = SunNode.FindPlanet(planetData.Name);
                double planetMass = planetNode.PlanetData.Mass;
                Vector3d planetSolarPosition = planetNode.GetSolarPositionAtTime(time);
                Vector3d deltaPosition = planetSolarPosition - CraftPosition;
                Vector3d GravityAcceleration = GravityConst * planetMass / deltaPosition.sqrMagnitude * deltaPosition.normalized;
                result += GravityAcceleration;
            }
            return result;
        }

        public static List<Vector3d> CalculateGravityJacobiAtTime(Vector3d CraftPosition, double time)
        {
            Vector3d PartialVecX = new Vector3d(0, 0, 0);
            Vector3d PartialVecY = new Vector3d(0, 0, 0);
            Vector3d PartialVecZ = new Vector3d(0, 0, 0);
            foreach (var planetData in planetList)
            {
                IPlanetNode planetNode = SunNode.FindPlanet(planetData.Name);
                double planetMass = planetNode.PlanetData.Mass;
                Vector3d planetSolarPosition = planetNode.GetSolarPositionAtTime(time);
                Vector3d deltaPosition = planetSolarPosition - CraftPosition;
                double Distance = deltaPosition.magnitude;
                double Ki = GravityConst * planetMass / Math.Pow(Distance, 3);
                double Ki2 = 3 * GravityConst * planetMass / Math.Pow(Distance, 5);
                PartialVecX.x += Ki2 * deltaPosition.x * deltaPosition.x - Ki * planetSolarPosition.x;
                PartialVecX.y += Ki2 * deltaPosition.x * deltaPosition.y;
                PartialVecX.z += Ki2 * deltaPosition.x * deltaPosition.z;
                PartialVecY.x += Ki2 * deltaPosition.y * deltaPosition.x;
                PartialVecY.y += Ki2 * deltaPosition.y * deltaPosition.y - Ki * planetSolarPosition.y;
                PartialVecY.z += Ki2 * deltaPosition.y * deltaPosition.z;
                PartialVecZ.x += Ki2 * deltaPosition.z * deltaPosition.x;
                PartialVecZ.y += Ki2 * deltaPosition.z * deltaPosition.y;
                PartialVecZ.z += Ki2 * deltaPosition.z * deltaPosition.z - Ki * planetSolarPosition.z;
            }
            return new List<Vector3d> { PartialVecX, PartialVecY, PartialVecZ };
        }

        public static void UpdateList<T>(ref List<T> originList, List<T> newList, int NewListStartAt)
        {
            // 输入原始列表originList,输入新列表newList,输入位数NewListStartAt作为n
            // 将原始列表的第n位改为新列表的第0位，原始列表的n+1位改为新列表的第1位，以此类推；如果原列表到达结尾，那么将新列表剩余的部分直接添加到原列表的末尾
            for (int i = 0; i < newList.Count; i++)
            {
                if (i + NewListStartAt >= originList.Count)
                {
                    originList.Add(newList[i]);
                }
                else
                {
                    originList[i + NewListStartAt] = newList[i];
                }
            }
        }
        public List<P_V_Pair> MBG_PVList = new List<P_V_Pair> { };

        public List<MBGOrbitSpecialPoint> SpecialPointList = new List<MBGOrbitSpecialPoint> { };

        private static Dictionary<CraftNode, MBGOrbit> craftNodeOrbitMap = new Dictionary<CraftNode, MBGOrbit>();

        public CraftNode CurrentCraft
        {
            get
            {
                return craftNodeOrbitMap.FirstOrDefault(q => q.Value == this).Key;
            }
        }

        private readonly double _startTime;
        public double EndTime { get; private set; }
        public static readonly double GravityConst = 6.674E-11;

        public static double ForceReCalculateBeforeEnd = 1;
        private static double _listAccuracyTime
        {
            get
            {
                return MBGMath._calculationStepTime;
            }
        }
        private static double _defaultDurationTime = 3600;

        public static double CurrentTime
        {
            get => Game.Instance.FlightScene.FlightState.Time;
            //get => Game.Instance.GameState.GetCurrentTime();
        }
    }


    public struct MBGOrbitSpecialPoint
    {
        public MBGOrbitSpecialPointType specialPointType;
        public IPlanetNode planet;
        public double time;
    }
    public enum MBGOrbitSpecialPointType
    {
        Periapsis = 0,
        Apoapsis = 1,
        AscendingNode = 2,
        DescendingNode = 3
    }
}