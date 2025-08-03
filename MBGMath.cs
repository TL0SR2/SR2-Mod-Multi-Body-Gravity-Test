using System;
using UnityEngine;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public static class MBGMath
    {
        public static bool FloatEqual(double num1, double num2)
        {
            return Math.Abs(num1 - num2) <= 1E-9;
        }
        public static Vector3d Interpolation(Vector3d vec1, Vector3d vec2, double ratio)
        {
            return (1 - ratio) * vec1 + ratio * vec2;
        }
    }
}