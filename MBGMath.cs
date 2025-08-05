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
        public static P_V_Pair Interpolation(P_V_Pair vec1, P_V_Pair vec2, double ratio)
        {
            return (1 - ratio) * vec1 + ratio * vec2;
        }

        public static void NumericalIntegration(P_V_Pair startPV, double startTime, double elapsedTime, out List<P_V_Pair> PVOut)
        {
            P_V_Pair PVPair = startPV;
            double time = startTime;
            int CaculateStep = (int)Math.Floor(elapsedTime / _calculationStepTime);
            PVOut = new List<P_V_Pair> { };
            for (int i = 0; i < CaculateStep; i++)//只适用于固定步长的数值计算方法的代码
            {
                PVOut.Add(PVPair);
                PVPair = MBGMath_CaculationMethod.ClassicRK4Method(PVPair, time, GravityFunc);
                time += _calculationStepTime;
            }

        }

        public static void SetMBGCalculationStep(double value)
        {
            _calculationStepTime = value;
        }


        public static double _calculationStepTime { get; private set; } = 0.05;

        public static Func<double, P_V_Pair, P_V_Pair> GravityFunc = (time, input_P_V) =>
        //给出总的数值模拟迭代函数
        {
            Vector3d GravityAcc = MBGOrbit.CalculateGravityAtTime(input_P_V.Position, time);
            return new P_V_Pair(input_P_V.Velocity, GravityAcc);
        };

        public static Func<double, Vector3d, List<Vector3d>> GravityJacobiFunc = (time, input_P_V) =>
        //给出万有引力加速度函数的空间参量的雅可比矩阵，输出List<Vector3d>共3项，从第0到2项依次为对位置的x,y,z坐标的偏导,每一项的三个分量为在xyz方向上的分量
        {

        }

    }

    public static class MBGMath_CaculationMethod
    {
        //这个类中全部为可以选用的数值计算方法，参数统一：输入参数P_V_Pair y_n表示位置-速度组，double x_n表示时刻，func委托表示由时间和PV对得到PV对导数的函数。输出值表示位置-速度组。
        //
        private static double h
        {
            get
            {
                return MBGMath._calculationStepTime;
            }
        }
        //表示数值计算的时间步长

        //第一部分：显式RK方法

        public static P_V_Pair EulerMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //显式欧拉法  RK1，一阶精度
        {
            var k1 = h * func(x_n, y_n);
            return y_n + k1;
        }
        public static P_V_Pair ImprovedEulerMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //预测-修正法  RK2，二阶精度
        {
            var k1 = h * func(x_n, y_n);
            var k2 = h * func(x_n + h, y_n + k1);
            return y_n + (k1 + k2) / 2;
        }
        public static P_V_Pair ExplicitMidpointMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //显式中点法  RK2，二阶精度
        {
            var k1 = h * func(x_n, y_n);
            var k2 = h * func(x_n + h / 2, y_n + k1 / 2);
            return y_n + k2;
        }
        public static P_V_Pair ClassicRK3Method(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //经典三阶龙格库塔法  RK3，三阶精度
        {
            var k1 = h * func(x_n, y_n);
            var k2 = h * func(x_n + h / 2, y_n + k1 / 2);
            var k3 = h * func(x_n + h, y_n - k1 + 2 * k2);
            return y_n + (k1 + 4 * k2 + k3) / 6;
        }
        public static P_V_Pair ClassicRK4Method(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //经典四阶龙格库塔法  RK4，四阶精度
        {
            var k1 = h * func(x_n, y_n);
            var k2 = h * func(x_n + h / 2, y_n + k1 / 2);
            var k3 = h * func(x_n + h / 2, y_n + k2 / 2);
            var k4 = h * func(x_n + h, y_n + k3);
            return y_n + (k1 + 2 * k2 + 2 * k3 + k4) / 6;
        }
        public static P_V_Pair GillMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //Gill方法  RK4，四阶精度
        {
            var k1 = h * func(x_n, y_n);
            var k2 = h * func(x_n + h / 2, y_n + k1 / 2);
            var k3 = h * func(x_n + h / 2, y_n + (Math.Sqrt(2) - 1) / 2 * k1 + (2 - Math.Sqrt(2)) / 2 * k2);
            var k4 = h * func(x_n + h, y_n - Math.Sqrt(2) / 2 * k2 + (Math.Sqrt(2) + 2) / 2 * k3);
            return y_n + (k1 + (2 - Math.Sqrt(2)) * k2 + (2 + Math.Sqrt(2)) * k3 + k4) / 6;
        }

        public static P_V_Pair LawsonMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //Lawson方法  RK6，五阶精度
        {
            var k1 = h * func(x_n, y_n);
            var k2 = h * func(x_n + h / 2, y_n + k1 / 2);
            var k3 = h * func(x_n + h / 4, y_n + (3 * k1 + k2) / 16);
            var k4 = h * func(x_n + h / 2, y_n + k3 / 2);
            var k5 = h * func(x_n + 3 / 4 * h, y_n + (-3 * k2 + 6 * k3 + 9 * k4) / 16);
            var k6 = h * func(x_n + h, y_n + (k1 + 4 * k2 + 6 * k3 - 12 * k4 + 8 * k5) / 7);
            return y_n + (7 * k1 + 32 * k3 + 12 * k4 + 32 * k5 + 7 * k6) / 90;
        }
        public static P_V_Pair NystromMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //Nystrom方法  RK6，五阶精度
        {
            var k1 = h * func(x_n, y_n);
            var k2 = h * func(x_n + h / 3, y_n + k1 / 3);
            var k3 = h * func(x_n + 2 / 5 * h, y_n + (4 * k1 + 6 * k2) / 25);
            var k4 = h * func(x_n + h, y_n + (k1 - 12 * k2 + 15 * k3) / 4);
            var k5 = h * func(x_n + 2 / 3 * h, y_n + (6 * k1 + 90 * k2 - 50 * k3 + 8 * k4) / 81);
            var k6 = h * func(x_n + 4 / 5 * h, y_n + (6 * k1 + 36 * k2 + 10 * k3 + 8 * k4) / 75);
            return y_n + (23 * k1 + 125 * k3 - 81 * k5 + 125 * k6) / 192;
        }
        public static P_V_Pair ButcherMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //Butcher方法  RK7，六阶精度
        {
            var k1 = h * func(x_n, y_n);
            var k2 = h * func(x_n + h / 3, y_n + k1 / 3);
            var k3 = h * func(x_n + 2 / 3 * h, y_n + 2 / 3 * k2);
            var k4 = h * func(x_n + h / 3, y_n + (k1 + 4 * k2 - k3) / 12);
            var k5 = h * func(x_n + h / 2, y_n + (-1 * k1 + 18 * k2 - 3 * k3 - 6 * k4) / 16);
            var k6 = h * func(x_n + h / 2, y_n + (9 * k2 - 3 * k3 - 6 * k4 + 4 * k5) / 8);
            var k7 = h * func(x_n + h / 2, y_n + (9 * k1 - 36 * k2 + 63 * k3 + 72 * k4 - 64 * k6) / 44);
            return y_n + (11 * k1 + 81 * k3 + 81 * k4 - 32 * k5 - 32 * k6 + 11 * k7) / 120;
        }

        //第二部分：隐式RK方法--GL方法

        public static P_V_Pair MidpointMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func)
        //中点法  GL1，二阶精度，保辛
        {
            //计算公式： k1 = h * func(x_n + h / 2, y_n + k1 / 2)
            
        }

        //杂项

        public static readonly int n = 5;//牛顿法的迭代次数
        public static List<P_V_Pair> NewtonIteration(Func<List<P_V_Pair>, List<P_V_Pair>> JFFunction, List<P_V_Pair> startPosition)
        //牛顿迭代法求函数零点，输入函数为目标函数的雅可比逆与函数的乘积（即在迭代时会用到的J^-1 F），输入起始位置，输出(近似的)零点
        {
            List<P_V_Pair> x = startPosition;
            for (int i = 0; i < n; i++)
            {
                x -= JFFunction(x);
            }
            return x;
        }
    }
    public struct P_V_Pair
    {
        public P_V_Pair(double px, double py, double pz, double vx, double vy, double vz)
        {
            this.px = px;
            this.py = py;
            this.pz = pz;
            this.vx = vx;
            this.vy = vy;
            this.vz = vz;
        }
        public P_V_Pair(Vector3d Position, Vector3d Velocity)
        {
            px = Position.x;
            py = Position.y;
            pz = Position.z;
            vx = Velocity.x;
            vy = Velocity.y;
            vz = Velocity.z;
        }
        public static P_V_Pair operator +(P_V_Pair pair1, P_V_Pair pair2)
        {
            return new P_V_Pair(pair1.Position + pair2.Position, pair1.Velocity + pair2.Velocity);
        }
        public static P_V_Pair operator -(P_V_Pair pair1, P_V_Pair pair2)
        {
            return new P_V_Pair(pair1.Position - pair2.Position, pair1.Velocity - pair2.Velocity);
        }
        public static P_V_Pair operator *(P_V_Pair pair, double d)
        {
            return new P_V_Pair(pair.Position * d, pair.Velocity * d);
        }
        public static P_V_Pair operator *(double d, P_V_Pair pair)
        {
            return new P_V_Pair(d * pair.Position, d * pair.Velocity);
        }
        public static P_V_Pair operator /(P_V_Pair pair, double d)
        {
            return new P_V_Pair(pair.Position / d, pair.Velocity / d);
        }
        public static P_V_Pair operator -(P_V_Pair a)
        {
            return new P_V_Pair(-a.Position, -a.Velocity);
        }
        public static bool operator ==(P_V_Pair pair1, P_V_Pair pair2)
        {
            return pair1.Position == pair2.Position && pair1.Velocity == pair2.Velocity;
        }
        public static bool operator !=(P_V_Pair pair1, P_V_Pair pair2)
        {
            return pair1.Position != pair2.Position || pair1.Velocity != pair2.Velocity;
        }
        public override bool Equals(object other)
        {
            if (!(other is P_V_Pair))
            {
                return false;
            }
            P_V_Pair pair = (P_V_Pair)other;
            return pair == this;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Velocity.GetHashCode() << 2;
        }

        public double this[int index]
        {
            get
            {
                return index switch
                {
                    0 => this.px,
                    1 => this.py,
                    2 => this.pz,
                    3 => this.vx,
                    4 => this.vy,
                    5 => this.vz,
                    _ => throw new IndexOutOfRangeException("Invalid P_V_Pair index!"),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        this.px = value;
                        return;
                    case 1:
                        this.py = value;
                        return;
                    case 2:
                        this.pz = value;
                        return;
                    case 3:
                        this.vx = value;
                        return;
                    case 4:
                        this.vy = value;
                        return;
                    case 5:
                        this.vz = value;
                        return;
                    default:
                        throw new IndexOutOfRangeException("Invalid P_V_Pair index!");
                }
            }
        }
        
        public readonly double SqrMagnitude
        {
            get
            {
                return px * px + py * py + pz * pz + vx * vx + vy * vy + vz * vz;
            }
        }
        public double Magnitude
        {
            get
            {
                return Math.Sqrt(SqrMagnitude);
            }
        }
        public static readonly P_V_Pair Zero = new P_V_Pair(0, 0, 0, 0, 0, 0);

        public double px;
        public double py;
        public double pz;
        public double vx;
        public double vy;
        public double vz;

        public Vector3d Position
        {
            get
            {
                return new Vector3d(px, py, pz);
            }
        }
        public Vector3d Velocity
        {
            get
            {
                return new Vector3d(vx, vy, vz);
            }
        }
    }
}