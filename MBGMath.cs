using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

        public static void NumericalIntegration(Vector3d startPosition, Vector3d startVelocity, int CaculateStep, double startTime, out List<Vector3d> positionOut, out List<Vector3d> velocityOut)
        {
            Vector3d position = startPosition;
            Vector3d velocity = startVelocity;
            double time = startTime;
            positionOut = new List<Vector3d> { };
            velocityOut = new List<Vector3d> { };
            for (int i = 0; i < CaculateStep; i++)
            {
                positionOut.Add(position);
                velocityOut.Add(velocity);

            }
            
        }

        public static void SetMBGCalculationStep(double value)
        {
            _calculationStepTime = value;
        }
        
        public static double _calculationStepTime { get; private set; } = 0.05;
    }
}