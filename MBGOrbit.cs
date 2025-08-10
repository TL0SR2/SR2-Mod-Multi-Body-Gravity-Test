using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ModApi.Flight.Sim;
using Assets.Scripts.Flight.Sim;
using ModApi.Planet;
using System.Linq;
using ModApi.Flight;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbit
    {
        //public static event EventHandler<double> ChangeTimeEvent;
        public static IPlanetNode SunNode { get; set; }
        public static IReadOnlyList<IPlanetData> planetList { get; set; }
        public MBGOrbit(CraftNode craftNode, double startTime, Vector3d startPosition, Vector3d startVelocity)
        {
            this._startTime = startTime;
            this.MBG_PVList.Add(new P_V_Pair(startPosition, startVelocity));
            this.TLPList.Add(new MBGOrbit_Time_ListNPair(startTime, 1, 0));
            SetMBGOrbit(craftNode, this);
            CurrentCraft = craftNode;
            Game.Instance.FlightScene.TimeManager.TimeMultiplierModeChanging += e => this.ChangeTimeActivate(e);
            try
            {
                FindPlanetInformation();
                ForceReCalculation();
                Debug.Log($"MBGOrbit.MBGOrbit -- Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MBGOrbit.MBGOrbit -- Find Exception during Init");
                Debug.LogException(ex);
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
            try
            {
                int n = GetPVNFromTime(startTime, out double Multiplier, out double NTime);
                //int step = (int)Math.Floor(elapsedTime / _listAccuracyTime);
                Debug.Log($"TL0SR2 MBG Orbit Log -- MBG_Numerical_Calculation -- Start Calculation. Data:  n {n}  Total Count {MBG_PVList.Count}  Input PostionLength {MBG_PVList[n].Position.magnitude} VelocityLength {MBG_PVList[n].Velocity.magnitude} Time {NTime}");
                MBGMath.NumericalIntegration(MBG_PVList[n], NTime, elapsedTime * Multiplier, Multiplier, out List<P_V_Pair> PVList);
                UpdateList<P_V_Pair>(ref MBG_PVList, PVList, n);
                EndTime = NTime + elapsedTime * Multiplier;
                DebugLogPVList(n, 10);
                //Debug.Log($"TL0SR2 MBG Orbit Log -- MBG_Numerical_Calculation -- Calculation complete. Data:  n {n}  Total Count {MBG_PVList.Count}");
                //接下来应该在此处执行激活重绘轨道线的操作
            }
            catch (Exception e)
            {
                Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBG_Numerical_Calculation -- Catch Exception");
                Debug.LogException(e);
                Debug.LogError($"TL0SR2 MBG Orbit Log Error -- MBG_Numerical_Calculation -- Detail Data  n {GetPVNFromTime(startTime, out _, out _)}  PVCount  {MBG_PVList.Count}");
            }
        }

        public void DebugLogPVList(int startFrom, int N)
        {
            Debug.Log($"TL0SR2 MBG Orbit Log -- DebugLogPVList Log -- Log Start");
            for (int i = 0; i < N; i++)
            {
                int n = startFrom + i;
                Debug.Log($"TL0SR2 MBG Orbit Log -- DebugLogPVList Log -- Num {n} Value PostionLength {MBG_PVList[i].Position.magnitude}  VelocityLength {MBG_PVList[n].Velocity.magnitude} ");
            }
            Debug.Log($"TL0SR2 MBG Orbit Log -- DebugLogPVList Log -- Log End");
        }

        public void ForceReCalculation()
        {
            MBG_Numerical_Calculation(CurrentTime, _defaultDurationTime);
        }
        public P_V_Pair GetPVPairFromTime(double time)
        {
            try
            {
                int n = GetPVNFromTime(time, out double Multiplier, out double NTime);
                //return MBGMath.LinearInterpolation(MBG_PVList[n], MBG_PVList[n + 1], durationTime / _listAccuracyTime - n);
                var Output = MBGMath.HermiteInterpolation(MBG_PVList[n], MBG_PVList[n + 1], NTime, NTime + MBGMath.GetStepTime(Multiplier), time);
                Debug.Log($"TL0SR2 MBG Orbit Log -- GetPVPairFromTime -- Get Data n {n}  Multiplier {Multiplier}   PVCount {MBG_PVList.Count}  time {time}  NTime {NTime}   IntTime {NTime + MBGMath.GetStepTime(Multiplier)}  nPV PostionLength {MBG_PVList[n].Position.magnitude} VelocityLength {MBG_PVList[n].Velocity.magnitude}  n+1PV PostionLength {MBG_PVList[n+1].Position.magnitude} VelocityLength {MBG_PVList[n+1].Velocity.magnitude}  Output PostionLength {Output.Position.magnitude} VelocityLength {Output.Velocity.magnitude}");
                return Output;
            }
            catch (Exception e)
            {
                Debug.LogError("TL0SR2 MBG Orbit Log Error -- GetPVPairFromTime -- Catch Exception");
                Debug.LogException(e);
                return new P_V_Pair();
            }
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
                try
                {
                    var Orbit = GetMBGOrbit(craftNode);
                    Game.Instance.FlightScene.TimeManager.TimeMultiplierModeChanging -= e => Orbit.ChangeTimeActivate(e);
                }
                finally
                {
                    craftNodeOrbitMap.Remove(craftNode);

                    Debug.Log($"MBGOrbit.RemoveMBGOrbit -- Removed orbit for {craftNode}");
                }
            }

        }

        public static CraftNode GetCraft(MBGOrbit orbit)
        {
            return craftNodeOrbitMap.FirstOrDefault(q => q.Value == orbit).Key;
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
        
        public static void TestMethod(double time)
        {   Debug.LogWarning($"Test Method Debug Log -- SunNode  {SunNode}");
            foreach (var planetData in planetList)
            {
                Debug.LogWarning($"Test Method Debug Log -- PlanetData  {planetData}");
                try
                {
                    IPlanetNode planetNode = SunNode.FindPlanet(planetData.Name);
                    Debug.LogWarning($"Test Method Debug Log -- PlanetNode  {planetNode}");
                    Vector3d planetSolarPosition = planetNode.GetSolarPositionAtTime(time);
                    Debug.LogWarning($"Test Method Debug Log -- PlanetPosition  {planetSolarPosition}");
                }
                catch (Exception e)
                {
                    Debug.LogError("Test Method Debug Log Error");
                    Debug.LogException(e);
                }
            }
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

        public void ChangeTimeActivate(TimeMultiplierModeChangedEvent e)
        {
            Debug.Log("TL0SR2 MBG Orbit -- Change Time Mode");
            double NewMultiplier = e.CurrentMode.TimeMultiplier;
            if (NewMultiplier > 0)
            {
                int n = GetPVNFromTime(CurrentTime,out _,out _);
                Debug.Log($"TL0SR2 MBG Orbit -- ChangeTimeActivate -- Add New Node Time {CurrentTime} Multiplier {NewMultiplier} n {n}");
                TLPList.Add(new MBGOrbit_Time_ListNPair(CurrentTime, NewMultiplier, n));
                if (e.EnteredWarpMode)
                {
                    Debug.Log("TL0SR2 MBG Orbit -- Enter Time Warp Mode");
                    MBG_PVList[n] = GetCraftStateAtCurrentTime();
                }
                ForceReCalculation();
            }
        }

        public int GetPVNFromTime(double time, out double Multiplier, out double NTime)
        //这个方法输入时间，返回这个时刻【之前】的【最后】的PV列表对应序号n值,并输出这个n对应的时间加速倍率和n值对应的时间。
        {
            if (time >= _startTime)
            {
                int ChangeN = GetListTLPFromTime(time, out Multiplier, out double changeTime);
                int AfterN = (int)Math.Floor((time - changeTime) / MBGMath.GetStepTime(Multiplier));//这个值表示自从时间变化之后到所给时间时经过了多少项
                NTime = changeTime + AfterN * MBGMath.GetStepTime(Multiplier);
                return ChangeN + AfterN;
            }
            Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBGOrbit.GetPVNFromTime -- Time Out Of Range");
            Multiplier = 1;
            NTime = _startTime;
            return 0;
        }

        public int GetListTLPFromTime(double time, out double Multiplier,out double changeTime)
        //这个方法输入时间，返回这个时刻【之前】的【最后】一个时间加速变化节点的PV列表对应序号n值,并输出在这个n之后对应的时间加速倍率和时间变化时的对应时间。
        {
            try
            {
                /*if (TLPList.Last().Time <= time)
                {
                    Multiplier = TLPList.Last().TimeMultiplier;
                    changeTime = TLPList.Last().Time;
                    return TLPList.Last().StartN;
                }*/
                for (int i = TLPList.Count - 1; i >= 0; i--)
                {
                    MBGOrbit_Time_ListNPair pair = TLPList[i];
                    if (pair.Time <= time)
                    {
                        Multiplier = TLPList[i].TimeMultiplier;
                        changeTime = TLPList[i].Time;
                        return TLPList[i].StartN;
                    }
                }
                if (time >= _startTime)
                {
                    Multiplier = 1;
                    changeTime = _startTime;
                    return 0;
                }

                Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBGOrbit.GetListTLPFromTime -- Time Out Of Range");
                Multiplier = 1;
                changeTime = _startTime;
                return 0;
            }
            catch (Exception e)
            {
                Debug.LogError("TL0SR2 MBG Orbit Log Error -- Catch Exception");
                Debug.LogException(e);
                Debug.LogError($"TL0SR2 MBG Orbit Log Error -- Related Detail: time {time}  ");
                Multiplier = 1;
                changeTime = _startTime;
                return 0;
            }
        }

        public P_V_Pair GetCraftStateAtCurrentTime()
        {
            var craft = CurrentCraft;
            return new P_V_Pair(craft.GetSolarPositionAtTime(CurrentTime),craft.GetSolarVelocityAtTime(CurrentTime));
        }
        public List<P_V_Pair> MBG_PVList = new List<P_V_Pair> { };

        public List<MBGOrbit_Time_ListNPair> TLPList = new List<MBGOrbit_Time_ListNPair> { };

        public List<MBGOrbitSpecialPoint> SpecialPointList = new List<MBGOrbitSpecialPoint> { };

        private static Dictionary<CraftNode, MBGOrbit> craftNodeOrbitMap = new Dictionary<CraftNode, MBGOrbit>();

        public CraftNode CurrentCraft;

        private readonly double _startTime;
        public double EndTime { get; private set; }
        public static readonly double GravityConst = 6.674E-11;

        public static double ForceReCalculateBeforeEnd = 5;

        public static double TimeMultiplier
        {
            get
            {
                return Game.Instance.FlightScene.TimeManager.CurrentMode.TimeMultiplier;
            }
        }
        private static double _defaultDurationTime = 60;

        public static double CurrentTime
        {
            //get => Game.Instance.FlightScene.FlightState.Time;
            //get => Game.Instance.GameState.GetCurrentTime();
            get => SunNode.Orbit.Time;
        }
    }

    public struct MBGOrbit_Time_ListNPair
    //这是一个用来描述在时间加速变更的时候保存下相关数据的类。Time描述时间加速【变更】的时间，TimeMultiplier记录变更【后】时间加速的加速倍率，StartN描述在变更时间加速【前】的【最后】一个节点的序号N
    {
        public double Time;
        public double TimeMultiplier;
        public int StartN;

        public MBGOrbit_Time_ListNPair(double time, double Multiplier, int n)
        {
            this.Time = time;
            this.TimeMultiplier = Multiplier;
            this.StartN = n;
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