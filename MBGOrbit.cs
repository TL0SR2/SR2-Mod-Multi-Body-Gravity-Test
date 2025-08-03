using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ModApi.Flight.Sim;
using Assets.Scripts.Flight.Sim;
using ModApi.Planet;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbit
    {
        public static IPlanetNode SunNode = null;
        public static IReadOnlyList<IPlanetData> planetList = null;
        public MBGOrbit(double startTime, Vector3d startPostion, Vector3d startVelocity)
        {
            this._startTime = startTime;
            this.MBG_PositionList.Add(startPostion);
            this.MBG_VelocityList.Add(startVelocity);
        }
        public List<Vector3d> MBG_PositionList = new List<Vector3d> { };
        public List<Vector3d> MBG_VelocityList = new List<Vector3d> { };

        public List<MBGOrbitSpecialPoint> SpecialPointList = new List<MBGOrbitSpecialPoint> { };

        public void MBG_Numerical_Calculation(double startTime, double elapsedTime)
        {

        }

        public void ForceReCaculation()
        {

        }

        public static void SetMBGcalculationStep(double value)
        {
            _calculationStepTime = value;
        }
        public static void SetMBGlistAccuracy(double value)
        {
            _listAccuracyTime = value;
        }
        public Vector3d GetPositionFromTime(double time)
        {
            double durationTime = time - _startTime;
            int n = (int)Math.Floor(durationTime / _listAccuracyTime);
            if (n < 0 || n > (MBG_PositionList.Count-1))
            {
                Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBGOrbit.GetPositionFromTime -- Time Out Of Range");
                return new Vector3d(0, 0, 0);
            }
            return MBGMath.Interpolation(MBG_PositionList[n], MBG_PositionList[n + 1], durationTime / _listAccuracyTime - n);
        }
        public Vector3d GetVelocityFromTime(double time)
        {
            double durationTime = time - _startTime;
            int n = (int)Math.Floor(durationTime / _listAccuracyTime);
            if (n < 0 || n > (MBG_VelocityList.Count-1))
            {
                Debug.LogError("TL0SR2 MBG Orbit Log Error -- MBGOrbit.GetVelocityFromTime -- Time Out Of Range");
                return new Vector3d(0, 0, 0);
            }
            return MBGMath.Interpolation(MBG_VelocityList[n], MBG_VelocityList[n + 1], durationTime / _listAccuracyTime - n);
        }
        private double _startTime;
        private static double _calculationStepTime = 0.01;
        private static double _listAccuracyTime = 0.1;
        private static double _defaultDurationTime = 3600;
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