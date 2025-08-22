using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ModApi.Flight.Sim;
using Assets.Scripts.Flight.Sim;
using ModApi.Planet;
using System.Linq;
using ModApi.Flight;
using Assets.Scripts.Flight.UI;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbit
    {
        //public static event EventHandler<double> ChangeTimeEvent;
        public static IPlanetNode SunNode { get; set; }
        public static IReadOnlyList<IPlanetData> planetList { get; set; }
        public MBGOrbit(CraftNode craftNode, double startTime, Vector3d startPosition, Vector3d startVelocity)
        {
            _startTime = startTime;
            MBG_PointList.Add(new MBGOrbitPoint(new P_V_Pair(startPosition, startVelocity), startTime));
            TLPList.Add(new MBGOrbit_Time_ListNPair(startTime, 1, 0));
            SetMBGOrbit(craftNode, this);
            CurrentCraft = craftNode;
            //Time_ThrustAcc_Dic.Add(startTime, new Vector3d());
            //MathCalculator = new MBGMath(this);
            Game.Instance.FlightScene.TimeManager.TimeMultiplierModeChanged += e => ChangeTimeActivate(e);
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
                if (GetCraft(this) != null)
                {
                    double time1 = startTime;
                    if (CalculateAfterWarp)
                    {
                        CalculateAfterWarp = false;
                        time1 += _warpdelay;
                    }

                    int n = GetPVNFromTime(time1, out double Multiplier, out double NTime);
                    EndTime = NTime + elapsedTime * Multiplier;
                    //int n2 = GetPVNFromTime(startTime, out _, out _);
                    //int step = (int)Math.Floor(elapsedTime / _listAccuracyTime);
                    //Debug.Log($"TL0SR2 MBG Orbit Log -- MBG_Numerical_Calculation -- Start Calculation. Data:  n {n}  Total Count {MBG_PointList.Count}  Input PostionLength {MBG_PointList[n].State.Position.magnitude} VelocityLength {MBG_PointList[n].State.Velocity.magnitude} InputSelf Time {MBG_PointList[n].Time}   Time {NTime}");
                    //以下这个部分称为“长期模糊预测”模块。旨在用比较长的步长、比较低的精度来预测出一段更长期的轨迹，便于轨迹线的绘制。默认倍率500.
                    //一般地，长期预测消耗的算力是普通预测的一半
                    MBGMath.NumericalIntegration(MBG_PointList[n],Time_ThrustAcc_Dic, _MaxLongRangeCalculateTime, Multiplier * _LongPredictionRatio * 2, out List<MBGOrbitPoint> LongPointList, true);
                    UpdateList<MBGOrbitPoint>(ref MBG_PointList, LongPointList, n);
                    //警告：结束时间被设置为普通的计算长度结束的时间。长期计算的数据间隔不同，不 应 该 被读取至常规状态喵！！
                    MBGMath.NumericalIntegration(MBG_PointList[n], Time_ThrustAcc_Dic, elapsedTime * Multiplier, Multiplier, out List<MBGOrbitPoint> PointList, false);
                    UpdateList<MBGOrbitPoint>(ref MBG_PointList, PointList, n);
                    //DebugLogPVList(n, 10);
                    //Debug.Log($"TL0SR2 MBG Orbit Log -- MBG_Numerical_Calculation -- Calculation complete. Data:  n {n}  Total Count {MBG_PointList.Count}");
                    CaculationNum++;
                    // //接下来应该在此处执行激活重绘轨道线的操作
                }
            }
            catch (Exception e)
            {
                Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBG_Numerical_Calculation -- Catch Exception");
                Debug.LogException(e);
                Debug.LogError($"TL0SR2 MBG Orbit Log Error -- MBG_Numerical_Calculation -- Detail Data  n {GetPVNFromTime(startTime, out _, out _)}  PVCount  {MBG_PointList.Count}");
            }
        }

        public void DebugLogPVList(int startFrom, int N)
        {
            Debug.Log($"TL0SR2 MBG Orbit Log -- DebugLogPVList Log -- Log Start");
            for (int i = 0; i < N; i++)
            {
                int n = startFrom + i;
                Debug.Log($"TL0SR2 MBG Orbit Log -- DebugLogPVList Log -- Num {n} Value PostionLength {MBG_PointList[n].State.Position.magnitude}  VelocityLength {MBG_PointList[n].State.Velocity.magnitude} ");
            }
            Debug.Log($"TL0SR2 MBG Orbit Log -- DebugLogPVList Log -- Log End");
        }

        public void ForceReCalculation()
        {
            MBG_Numerical_Calculation(CurrentTime, _defaultDurationTime);
        }
        public void ForceReCalculation(double startTime)
        {
            MBG_Numerical_Calculation(startTime, _defaultDurationTime);
        }
        public P_V_Pair GetPVPairFromTime(double time)
        {
            try
            {
                //time -= _warpdelay;
                int n = GetPVNFromTime(time, out double Multiplier, out double NTime);
                //return MBGMath.LinearInterpolation(MBG_PVList[n], MBG_PVList[n + 1], durationTime / _listAccuracyTime - n);
                var Output = MBGMath.HermiteInterpolation(MBG_PointList[n].State, MBG_PointList[n + 1].State, MBG_PointList[n].Time, MBG_PointList[n + 1].Time, time);
                //Debug.Log($"TL0SR2 MBG Orbit Log -- GetPVPairFromTime -- Get Data n {n}  Multiplier {Multiplier}   PVCount {MBG_PointList.Count}  time {time}  NTime {NTime}   IntTime {NTime + MBGMath.GetStepTime(Multiplier)}  nPV PostionLength {MBG_PointList[n].State.Position.magnitude} VelocityLength {MBG_PointList[n].State.Velocity.magnitude} SelfTime {MBG_PointList[n].Time}  n+1PV PostionLength {MBG_PointList[n+1].State.Position.magnitude} VelocityLength {MBG_PointList[n+1].State.Velocity.magnitude} SelfTime {MBG_PointList[n+1].Time}  Output PostionLength {Output.Position.magnitude} VelocityLength {Output.Velocity.magnitude}");
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

                    Debug.Log($"MBGOrbit.RemoveMBGOrbit -- Removed orbit for {craftNode.Name}");
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
        {
            Debug.LogWarning($"Test Method Debug Log -- SunNode  {SunNode}");
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

        public static void ReplenishList(ref List<MBGOrbitPoint> points, int n)
        //将列表补充到第N项不为空的状态
        {
            
        }

        public void AddOrChangeManeuverNode(double startTime, MBGManeuverNode node)
        {
            if (Time_ThrustAcc_Dic.ContainsKey(startTime))
            {
                Time_ThrustAcc_Dic[startTime] = node;
            }
            else
            {
                AddManeuverNode(startTime, node);
            }
        }

        public void AddManeuverNode(double startTime, MBGManeuverNode node)
        {
            if (!AlreadyHaveNodeInTime(Time_ThrustAcc_Dic, startTime, startTime + node.ThrustTime))
            {
                Time_ThrustAcc_Dic.Add(startTime, node);
                ForceReCalculation();
            }
            else
            {
                Game.Instance.UserInterface.CreateMessageDialog("There's already Burn Node in this node burn time.");
            }
        }
        public void RemoveManeuverNode(double startTime)
        {
            Time_ThrustAcc_Dic.Remove(startTime);
            ForceReCalculation();
        }


        public Vector3d GetThrustAcc(double time)
        {
            int n = GetPVNFromTime(time, out double Multiplier, out double T1);
            if (n < MBG_PointList.Count - 1)
            {
                double T2 = MBG_PointList[n + 1].Time;
                Vector3d A1 = MBG_PointList[n].ThrustAcc;
                Vector3d A2 = MBG_PointList[n + 1].ThrustAcc;
                return MBGMath.LinearInterpolation(A1, A2, (time - T1) / (T2 - T1));
            }
            else
            {
                return MBG_PointList[n].ThrustAcc;
            }
        }

        public static bool AlreadyHaveNodeInTime(SortedDictionary<double, MBGManeuverNode> Dic, double StartTime, double EndTime)
        {
            
            int i = 0;
            try
            {
                foreach (var Pair in Dic)
                {
                    if (((i == 0) && (EndTime < (Pair.Key - MBGOrbit.EngineActivateTime))) || ((i >= Dic.Count - 1) && (StartTime > (Pair.Key + Pair.Value.ThrustTime + MBGOrbit.EngineActivateTime))))
                    {
                        return false;
                    }
                    if ((StartTime < (Pair.Key + Pair.Value.ThrustTime + MBGOrbit.EngineActivateTime)) && (EndTime > (Pair.Key - MBGOrbit.EngineActivateTime)))
                    {
                        return true;
                    }
                    i++;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("TL0SR2 MBG Orbit -- GetThrustAcc -- Catch Exception");
                Debug.LogException(e);
                Debug.LogError($"Detailed Data:  i {i}  Count {Dic.Count}");
                return false;
            }
        }
        public static Vector3d GetThrustAcc(SortedDictionary<double, MBGManeuverNode> Dic, double time)
        {
            int i = 0;
            KeyValuePair<double, MBGManeuverNode> TempPair = new KeyValuePair<double, MBGManeuverNode>();
            try
            {
                foreach (var Pair in Dic)
                {
                    if (((i == 0) && (time < (Pair.Key - MBGOrbit.EngineActivateTime))) || ((i >= Dic.Count - 1) && (time > (Pair.Key + Pair.Value.ThrustTime + MBGOrbit.EngineActivateTime))))
                    {
                        return new Vector3d();
                    }
                    if (time < Pair.Key)
                    {
                        if (time < (TempPair.Key + TempPair.Value.ThrustTime))
                        {
                            return TempPair.Value.AccVec;
                        }
                        else if (time < (TempPair.Key + TempPair.Value.ThrustTime + MBGOrbit.EngineActivateTime))
                        {
                            return MBGMath.LinearInterpolation(TempPair.Value.AccVec, new Vector3d(), TempPair.Key + TempPair.Value.ThrustTime, TempPair.Key + TempPair.Value.ThrustTime + MBGOrbit.EngineActivateTime, time);
                        }
                        else if (time > (Pair.Key - MBGOrbit.EngineActivateTime))
                        {
                            return MBGMath.LinearInterpolation(new Vector3d(), Pair.Value.AccVec, Pair.Key - MBGOrbit.EngineActivateTime, Pair.Key, time);
                        }
                        else return new Vector3d();
                    }
                    TempPair = Pair;
                    i++;
                }
                Debug.LogError($"TL0SR2 MBG Orbit -- GetThrustAcc -- Unexpected Code Return -- Detailed Data i {i} Count {Dic.Count}");
                return TempPair.Value.AccVec;
            }
            catch (Exception e)
            {
                Debug.LogError("TL0SR2 MBG Orbit -- GetThrustAcc -- Catch Exception");
                Debug.LogException(e);
                Debug.LogError($"Detailed Data:  i {i}  Count {Dic.Count}");
                return new Vector3d();
            }
        }

        public void ChangeTimeActivate(TimeMultiplierModeChangedEvent e)
        {
            if (GetCraft(this) != null)
            {
                Debug.Log($"TL0SR2 MBG Orbit -- Change Time Mode -- New Time Mode {e.CurrentMode.TimeMultiplier}");
                double NewMultiplier = e.CurrentMode.TimeMultiplier;
                _warpdelay = 0;//WarpDelayK * NewMultiplier;
                if (NewMultiplier > 0)
                {
                    int n = GetPVNFromTime(CurrentTime + _warpdelay, out _, out _);
                    //Debug.Log($"TL0SR2 MBG Orbit -- ChangeTimeActivate -- Add New Node Time {CurrentTime} Multiplier {NewMultiplier} n {n}");
                    TLPList.Add(new MBGOrbit_Time_ListNPair(CurrentTime + _warpdelay, NewMultiplier, n));
                    if (e.EnteredWarpMode)
                    {
                        P_V_Pair state = GetCraftStateAtCurrentTime();
                        Debug.Log($"TL0SR2 MBG Orbit -- Enter Time Warp Mode -- Add New Point   N {n}");
                        MBG_PointList[n] = new MBGOrbitPoint(state, CurrentTime);
                    }
                    CalculateAfterWarp = true;
                    ForceReCalculation();
                }
            }
        }

        public int GetPVNFromTime(double time, out double Multiplier, out double NTime)
        //这个方法输入时间，返回这个时刻【之前】的【最后】的PV列表对应序号n值,并输出这个n对应的时间加速倍率和n值对应的时间。
        {
            
            if (time >= _startTime)
            {
                //time -= _warpdelay;
                int ChangeN = GetListTLPFromTime(time, out Multiplier, out double changeTime);
                int AfterN = (int)Math.Floor((time - changeTime) / MBGMath.GetStepTime(Multiplier));//这个值表示自从时间变化之后到所给时间时经过了多少项
                NTime = changeTime + AfterN * MBGMath.GetStepTime(Multiplier);
                return ChangeN + AfterN;
            }
            Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBGOrbit.GetPVNFromTime -- Time Out Of Range");
            Multiplier = 1;
            NTime = _startTime;
            return 0;
            
            /*
            if (time >= _startTime)
            {
                for (int i = MBG_PointList.Count - 2; i >= 0; i--)
                {
                    var point = MBG_PointList[i];
                    if (point.Time <= time)
                    {
                        Multiplier = (MBG_PointList[i + 1].Time - point.Time) / MBGMath._CalculationRealStep;
                        NTime = point.Time;
                        return i;
                    }
                }
            }
            Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBGOrbit.GetPVNFromTime -- Time Out Of Range");
            Multiplier = 1;
            NTime = _startTime;
            return 0;
            */
        }

        public int GetListTLPFromTime(double time, out double Multiplier, out double changeTime)
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
            //return new P_V_Pair(craft.GetSolarPositionAtTime(CurrentTime), craft.GetSolarVelocityAtTime(CurrentTime));
            //return new P_V_Pair(craft.SolarPosition, craft.SolarVelocity);
            //Debug.Log($"TL0SR2 MBG Orbit -- Get Craft State At Current Time -- Log Data -- Method 1 P {craft.GetSolarPositionAtTime(CurrentTime).magnitude}  V {craft.GetSolarVelocityAtTime(CurrentTime).magnitude}  Method 2 P {craft.SolarPosition.magnitude} V {craft.SolarVelocity.magnitude} Method 3 P {(craft.Position + craft.Parent.SolarPosition).magnitude} V {(craft.Velocity + craft.Parent.SolarVelocity).magnitude}");
            return new P_V_Pair(craft.SolarPosition, craft.SolarVelocity);
        }

        public static void SetWarpDelayK(double value)
        {
            WarpDelayK = value;
        }

        public static void SetCalculationTime(double value)
        {
            _defaultDurationTime = value;
        }

        public static void SetLongPredictionRatio(double value)
        {
            _LongPredictionRatio = value;
        }

        public static void SetMaxLongRangeTime(double value)
        {
            _MaxLongRangeCalculateTime = value;
        }

        public MBGOrbitPointSet GetMBGOrbitPointSet()
        {
            return new MBGOrbitPointSet(MBG_PointList);
        }
        public List<MBGOrbitPoint> MBG_PointList = new List<MBGOrbitPoint> { };

        public List<MBGOrbit_Time_ListNPair> TLPList = new List<MBGOrbit_Time_ListNPair> { };

        public List<MBGOrbitSpecialPoint> SpecialPointList = new List<MBGOrbitSpecialPoint> { };

        private static Dictionary<CraftNode, MBGOrbit> craftNodeOrbitMap = new Dictionary<CraftNode, MBGOrbit>();

        public CraftNode CurrentCraft;

        public MBGMapOrbitInfo orbitInfo;

        private readonly double _startTime;
        //private MBGMath MathCalculator;
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

        private static double _LongPredictionRatio = 500;

        private static double _MaxLongRangeCalculateTime = 86400;

        public static double CurrentTime
        {
            get => Game.Instance.FlightScene.FlightState.Time;
            //get => Game.Instance.GameState.GetCurrentTime();
            //get => SunNode.Orbit.Time;
            //get => Game.Instance.FlightScene.TimeManager.RealTime
            //get => _currentTime;
            //set => _currentTime = value;
        }
        //private static double _currentTime;

        private double _warpdelay = 0;

        private double CurrentTimeFixed
        {
            get => CurrentTime - _warpdelay;
        }

        public int CaculationNum = 0;
        //指定当前轨道已经经过了多少次重新计算

        private SortedDictionary<double, MBGManeuverNode> Time_ThrustAcc_Dic = new SortedDictionary<double, MBGManeuverNode>();

        public static double EngineActivateTime = 1;

        private bool CalculateAfterWarp;

        private static double WarpDelayK = 0.01;
    }

    public struct MBGOrbit_Time_ListNPair
    //这是一个用来描述在时间加速变更的时候保存下相关数据的类。Time描述时间加速【变更】的时间，TimeMultiplier记录变更【后】时间加速的加速倍率，StartN描述在变更时间加速【前】的【最后】一个节点的序号N
    {
        public double Time;
        public double TimeMultiplier;
        public int StartN;

        public MBGOrbit_Time_ListNPair(double time, double Multiplier, int n)
        {
            Time = time;
            TimeMultiplier = Multiplier;
            StartN = n;
        }
    }


    public struct MBGOrbitSpecialPoint
    {
        public MBGOrbitSpecialPointType specialPointType;
        public IPlanetNode planet;
        public MBGOrbitPoint SpecialPoint;
    }
    public enum MBGOrbitSpecialPointType
    {
        Periapsis = 0,
        Apoapsis = 1,
        AscendingNode = 2,
        DescendingNode = 3,
        Impact = 4
    }
}