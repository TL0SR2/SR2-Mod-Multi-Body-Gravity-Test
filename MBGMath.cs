using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using MathNet.Numerics.LinearAlgebra.Double;
//using MathNet.Numerics.LinearAlgebra;
namespace Assets.Scripts.Flight.Sim.MBG
{
    public static class MBGMath
    {
        public static bool FloatEqual(double num1, double num2)
        {
            return Math.Abs(num1 - num2) <= 1E-9;
        }
        public static Vector3d LinearInterpolation(Vector3d vec1, Vector3d vec2, double ratio)
        {
            return (1 - ratio) * vec1 + ratio * vec2;
        }
        public static P_V_Pair LinearInterpolation(P_V_Pair vec1, P_V_Pair vec2, double ratio)
        {
            return (1 - ratio) * vec1 + ratio * vec2;
        }


        public static P_V_Pair HermiteInterpolation(P_V_Pair vec1, P_V_Pair vec2, double T1, double T2, double t)
        //Hermite插值法，输出的拟合函数会同时考虑插值点的函数值和导数值。非常适合用于位置-速度二元拟合喵
        {
            Func<double, double, double, double> h0 = (x, x1, x2) => (1 + 2 * (x - x1) / (x2 - x1)) * Math.Pow((x - x2) / (x1 - x2), 2);
            Func<double, double, double, double> h1 = (x, x1, x2) => (1 + 2 * (x - x2) / (x1 - x2)) * Math.Pow((x - x1) / (x2 - x1), 2);
            Func<double, double, double, double> g0 = (x, x1, x2) => (x - x1) * Math.Pow((x - x2) / (x1 - x2), 2);
            Func<double, double, double, double> g1 = (x, x1, x2) => (x - x2) * Math.Pow((x - x1) / (x2 - x1), 2);

            Func<double, double, double, double, double, double, double, double> H3 = (x, x1, x2, y1, y2, yD1, yD2) => y1 * h0(x, x1, x2) + y2 * h1(x, x1, x2) + yD1 * g0(x, x1, x2) + yD2 * g1(x, x1, x2);

            Vector3d Position = new Vector3d(H3(t, T1, T2, vec1[0], vec2[0], vec1[3], vec2[3]), H3(t, T1, T2, vec1[1], vec2[1], vec1[4], vec2[4]), H3(t, T1, T2, vec1[2], vec2[2], vec1[5], vec2[5]));
            Vector3d Velocity = LinearInterpolation(vec1.Velocity, vec2.Velocity, (t - T1) / (T2 - T1));
            return new P_V_Pair(Position, Velocity);
        }

        public static void NumericalIntegration(P_V_Pair startPV, double startTime, double elapsedTime, double Multiplier, out List<P_V_Pair> PVOut)
        {
            P_V_Pair PVPair = startPV;
            double time = startTime;
            CurrentTimeMultiplier = Multiplier;
            int CaculateStep = (int)Math.Floor(elapsedTime / _calculationStepTime);
            PVOut = new List<P_V_Pair> { };
            Debug.Log($"TL0SR2 MBG Math -- Start NumericalIntegration Total Step: {CaculateStep}   elapsedTime:{elapsedTime}  dt: {_calculationStepTime}");
            for (int i = 0; i < CaculateStep; i++)//只适用于固定步长的数值计算方法的代码
            {
                PVOut.Add(PVPair);
                PVPair = MBGMath_CaculationMethod.YoshidaMethod(PVPair, time, GravityFunc);
                time += _calculationStepTime;
            }

        }

        public static void SetMBGCalculationStep(double value)
        {
            _CalculationRealStep = value;
        }
        
        public static Func<double, P_V_Pair, P_V_Pair> TestFunc = (time, input_P_V) =>
        {
            MBGOrbit.TestMethod(time);
            return new P_V_Pair();
        };

        private static double CurrentTimeMultiplier = 1;

        public static double _CalculationRealStep { get; private set; } = 0.01;

        public static double _calculationStepTime
        {
            get
            {
                return GetStepTime(CurrentTimeMultiplier);
            }
        }

        public static double GetStepTime(double Multiplier)
        {
            var T = _CalculationRealStep * Multiplier;
            T = T < 2 ? 2 : T;
            T = T > 10000 ? 10000 : T;
            return T;
        }

        public static Func<double, P_V_Pair, P_V_Pair> RKFunc = (time, input_P_V) =>
        //给出显式RK总的数值模拟迭代函数
        {
            Vector3d GravityAcc = MBGOrbit.CalculateGravityAtTime(input_P_V.Position, time);
            return new P_V_Pair(input_P_V.Velocity, GravityAcc);
        };

        public static Func<double, Vector3d, Vector3d> GravityFunc = (time, inputPosition) =>
        //给出万有引力加速度函数
        {
            return MBGOrbit.CalculateGravityAtTime(inputPosition, time);
        };

        public static Func<double, Vector3d, List<Vector3d>> GravityJacobiFunc = (time, inputPosition) =>
        //给出万有引力加速度函数的空间参量的雅可比矩阵，输出List<Vector3d>共3项，从第0到2项依次为对位置的x,y,z坐标的偏导
        {
            return MBGOrbit.CalculateGravityJacobiAtTime(inputPosition, time);
        };

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
        //在这一部分，由于需要更复杂的内部处理，所以输入函数除先前的RK输入函数外增加了万有引力加速度雅可比矩阵函数

        public static P_V_Pair MidpointMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func, Func<double, Vector3d, List<Vector3d>> JacobiFunc)
        //中点法  GL1，二阶精度，保辛
        {
            //计算公式： k1 = h * func(x_n + h / 2, y_n + k1 / 2), y_n+1 = y_n + k1

            //牛顿迭代法目标函数F = h * func(x_n + h / 2, y_n + k / 2) - k，维度6
            Func<P_V_Pair, P_V_Pair> F = k => h * func(x_n + h / 2, y_n + k / 2) - k;
            //对k各项参数的雅可比矩阵形如下：（每列对应k的一个分量，每行对应F的一个分量）
            // -1 0 0 h/2 0 0
            // 0 -1 0 0 h/2 0
            // 0 0 -1 0 0 h/2
            // x a b -1 0 0
            // a y c 0 -1 0
            // b c z 0 0 -1
            //a,b,c,x,y,z的值在下面给出 他们都是关于输入值k的函数
            Func<P_V_Pair, double> x = k => h / 2 * JacobiFunc(x_n + h / 2, y_n.Position + k.Position / 2)[0].x;
            Func<P_V_Pair, double> y = k => h / 2 * JacobiFunc(x_n + h / 2, y_n.Position + k.Position / 2)[1].y;
            Func<P_V_Pair, double> z = k => h / 2 * JacobiFunc(x_n + h / 2, y_n.Position + k.Position / 2)[2].z;
            Func<P_V_Pair, double> a = k => h / 2 * JacobiFunc(x_n + h / 2, y_n.Position + k.Position / 2)[0].y;
            Func<P_V_Pair, double> b = k => h / 2 * JacobiFunc(x_n + h / 2, y_n.Position + k.Position / 2)[0].z;
            Func<P_V_Pair, double> c = k => h / 2 * JacobiFunc(x_n + h / 2, y_n.Position + k.Position / 2)[1].z;
            //通过一次性求解线性方程组可以找到牛顿法迭代器的输入函数，输入函数维度6，六个分量分别命名为m1~m6
            

            Func<List<P_V_Pair>, List<P_V_Pair>> JFFunction = list =>
            {
                P_V_Pair k = list[0];
                P_V_Pair pair = J6I_FCalculation_Full(h / 2, x(k), y(k), z(k), a(k), b(k), c(k), F(k));
                return new List<P_V_Pair> { pair };
            };

            P_V_Pair k0 = h * func(x_n, y_n);


            P_V_Pair k1 = NewtonIteration(JFFunction, new List<P_V_Pair> { k0 })[0];
            return y_n + k1;
        }

        public static P_V_Pair HollingsworthMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func, Func<double, Vector3d, List<Vector3d>> JacobiFunc)
        //Hollingswort方法  GL2，四阶精度，保辛
        {
            //计算公式：
            // k1 = h * func(x_n + (3 - Math.Sqrt(3)) / 6 * h, y_n + (3 * k1 + (3 - 2 * Math.Sqrt(3)) * k2) / 12)
            // k2 = h * func(x_n + (3 + Math.Sqrt(3)) / 6 * h, y_n + (3 * k2 + (3 + 2 * Math.Sqrt(3)) * k1) / 12)
            // y_n+1 = y_n + (k1 + k2) / 2

            //第一部分：构造雅可比矩阵参量
            //牛顿迭代法的目标函数F=(F1,F2)，F1和F2分别是上面两个计算公式移项之后得到的目标函数，自变量为K=(k1,k2)。
            double[] tList = new double[] { h / 4, (3 - 2 * Math.Sqrt(3)) / 12 * h, (3 + 2 * Math.Sqrt(3)) / 12 * h, h / 4 };
            double[][] plist = new double[][]
            {
                new double[]{(3 - Math.Sqrt(3)) / 6 ,1 / 4,(3 - 2 * Math.Sqrt(3)) / 12},
                new double[]{(3 + Math.Sqrt(3)) / 6 ,(3 + 2 * Math.Sqrt(3)) / 12,1 / 4}
            };
            Func<List<P_V_Pair>, List<P_V_Pair>> A3p = CreateA3MatrixParam
            (
                plist,
                (t, p) => JacobiFunc(x_n + t, y_n.Position + p)
            );
            //第二部分：构造目标函数

            Func<List<P_V_Pair>, List<P_V_Pair>> F = CreateTargetFunction(plist, (t, p) => func(x_n + t, y_n + p));

            //第三部分：构造迭代函数

            Func<List<P_V_Pair>, List<P_V_Pair>> JFFunction = K =>
            {
                return J12I_FCalculation_Full(A3p(K), tList, F(K));
            };

            //第四部分：牛顿法迭代，数据处理

            P_V_Pair k0 = h * func(x_n, y_n);

            List<P_V_Pair> KOut = NewtonIteration(JFFunction, new List<P_V_Pair> { k0, k0 });

            P_V_Pair k1 = KOut[0];
            P_V_Pair k2 = KOut[1];

            return y_n + (k1 + k2) / 2;
        }

        public static P_V_Pair KuntzmannMethod(P_V_Pair y_n, double x_n, Func<double, P_V_Pair, P_V_Pair> func, Func<double, Vector3d, List<Vector3d>> JacobiFunc)
        //KuntzmannMethod方法  GL3，六阶精度，保辛
        {
            //计算公式：
            // k1 = h * func(x_n + (5 - Math.Sqrt(15)) / 10 * h, y_n + 5/36 * k1 + (10 - 3*Math.Sqrt(15)) / 45 * k2 + (25 - 6*Math.Sqrt(15)) / 180 * k3)
            // k2 = h * func(x_n + h/2, y_n + (10 + 3*Math.Sqrt(15)) / 72 * k1 + 2/9 * k2 + (10 - 3*Math.Sqrt(15)) / 72 * k3)
            // k3 = h * func(x_n + (5 + Math.Sqrt(15)) / 10 * h, y_n + (25 + 6*Math.Sqrt(15)) / 180 * k1 + (10 + 3*Math.Sqrt(15)) / 45 * k2 + 5/36 * k3)
            // y_n+1 = y_n + (5*k1+8*k2+5*k3)/18

            //第一部分：构造雅可比矩阵参量
            //牛顿迭代法的目标函数F=(F1,F2,F3)，F1，F2和F3分别是上面三个计算公式移项之后得到的目标函数，自变量为K=(k1,k2,k3)。

            double[] tList = new double[] { 5 / 36 * h, (10 - 3 * Math.Sqrt(15)) / 45 * h, (25 - 6 * Math.Sqrt(15)) / 180 * h, (10 + 3 * Math.Sqrt(15)) / 72 * h, 2 / 9 * h, (10 - 3 * Math.Sqrt(15)) / 72 * h, (25 + 6 * Math.Sqrt(15)) / 180 * h, (10 + 3 * Math.Sqrt(15)) / 45 * h, 5 / 36 * h };
            double[][] plist = new double[][]
            {
                new double[]{(5 - Math.Sqrt(15)) / 10 , 5/36,(10 - 3*Math.Sqrt(15)) / 45,(25 - 6*Math.Sqrt(15)) / 180},
                new double[]{1/2,                       (10 + 3*Math.Sqrt(15)) / 72,2/9,(10 - 3*Math.Sqrt(15)) / 72},
                new double[]{(5 + Math.Sqrt(15)) / 10 , (25 + 6*Math.Sqrt(15)) / 180,(10 + 3*Math.Sqrt(15)) / 45,5/36}
            };
            Func<List<P_V_Pair>, List<P_V_Pair>> A3p = CreateA3MatrixParam
            (
                plist,
                (t, p) => JacobiFunc(x_n + t, y_n.Position + p)
            );
            //第二部分：构造目标函数

            Func<List<P_V_Pair>, List<P_V_Pair>> F = CreateTargetFunction(plist, (t, p) => func(x_n + t, y_n + p));

            //第三部分：构造迭代函数

            Func<List<P_V_Pair>, List<P_V_Pair>> JFFunction = K =>
            {
                return J18I_FCalculation_Full(A3p(K), tList, F(K));
            };

            //第四部分：牛顿法迭代，数据处理

            P_V_Pair k0 = h * func(x_n, y_n);

            List<P_V_Pair> KOut = NewtonIteration(JFFunction, new List<P_V_Pair> { k0, k0, k0 });

            P_V_Pair k1 = KOut[0];
            P_V_Pair k2 = KOut[1];
            P_V_Pair k3 = KOut[2];

            return y_n + (5 * k1 + 8 * k2 + 5 * k3) / 18;
        }


        public static Func<double, double, double, double, double, double, double, P_V_Pair, Vector3d> J6I_FCalculation = (t, x, y, z, a, b, c, F) =>
        //专门针对隐式RK中出现的矩阵优化的，求雅可比矩阵的逆与目标函数的乘积矢量从而得到牛顿法迭代需要的迭代函数的方法。
        //矩阵形如：
        // -1 0 0 t 0 0
        // 0 -1 0 0 t 0
        // 0 0 -1 0 0 t
        // x a b -1 0 0
        // a y c 0 -1 0
        // b c z 0 0 -1
        //输入参量为：如上矩阵中的诸元x,y,z,a,b,c,t，输入六维的矢量F
        //输出为：上面这个矩阵的逆矩阵与矢量F的乘积矢量是六维的，这个方法计算并输出这个六维矢量的后三个分量。前三个由此可以很容易计算得到。
        //这部分运算在手动化简之后通过mathematica一次性输出完成,然后手动化简整理并进行对称性检查得到
        //对于对角元不是-1的情况，只要对角元非0，就可以对整个矩阵做乘法使得对角元为-1，然后输入新的参量；对于对角元为0的情况，直接拆分成两个矩阵分别求逆即可
        {
            Vector3d result = new Vector3d(0, 0, 0);
            result.x =
                (
                x * F[0] +
                a * F[1] +
                b * F[2] +
                F[3]) +
            t * (
                (a * a + b * b + 2 * a * b * c * t - x * (y + z)) * F[0] +
                (b * c - a * z) * F[1] +
                (a * c - b * y) * F[2] -
                (y + z) * F[3] +
                a * F[4] +
                b * F[5]) +
            t * t * (
                (x * y * z - x * c * c - y * b * b - z * a * a) * F[0] +
                (y * z - c * c) * F[3] -
                (a * z - b * c) * F[4] +
                (a * c - b * y) * F[5]);//对称性检查可以得知交换性来源于矩阵的行列式计算

            result.y =
                (
                y * F[1] +
                c * F[2] +
                a * F[0] +
                F[4]) +
            t * (
                (c * c + a * a + 2 * a * b * c * t - y * (z + x)) * F[1] +
                (a * b - c * x) * F[2] +
                (b * c - a * z) * F[0] -
                (z + x) * F[4] +
                c * F[5] +
                a * F[3]) +
            t * t * (
                (x * y * z - x * c * c - y * b * b - z * a * a) * F[1] +
                (x * z - b * b) * F[4] -
                (c * x - a * b) * F[5] +
                (c * b - a * z) * F[3]);

            result.z =
                (
                z * F[2] +
                b * F[0] +
                c * F[1] +
                F[5]) +
            t * (
                (b * b + c * c + 2 * a * b * c * t - z * (x + y)) * F[2] +
                (a * c - b * y) * F[0] +
                (a * b - c * x) * F[1] -
                (x + y) * F[5] +
                b * F[3] +
                c * F[4]) +
            t * t * (
                (x * y * z - x * c * c - y * b * b - z * a * a) * F[2] +
                (x * y - a * a) * F[5] -
                (b * y - a * c) * F[3] +
                (a * b - c * x) * F[4]);

            result /=
            -1 +
            t * (x + y + z) +
            t * t * (a * a + b * b + c * c - (x * y + y * z + z * x)) +
            t * t * t * (2 * a * b * c - a * a * z - b * b * y - c * c * x + x * y * z);

            return result;

            //补注：实际上这个方法相当于对原矩阵作初等行变换之后提取其中的A3方阵部分求逆，可以用以下方方法和一些代数运算等价地简化。但是由于此方法完成早于下面的方法，所以不再做类似化简操作。
        };
        public static Func<double, double, double, double, double, double, double, P_V_Pair, P_V_Pair> J6I_FCalculation_Full = (t, x, y, z, a, b, c, F) =>
        //主要介绍参考上面的计算参数。这个方法在上述方法的基础上计算出前三个分量，输出完整的六分量矢量。
        {
            Vector3d m456Vec = J6I_FCalculation(t, x, y, z, a, b, c, F);
            Vector3d m123Vec = new Vector3d(t * m456Vec.x - F[0], t * m456Vec.y - F[1], t * m456Vec.z - F[2]);
            return new P_V_Pair(m123Vec, m456Vec);
        };

        public static Func<double, double, double, double, double, double, double, P_V_Pair, P_V_Pair> J6_FCalculation = (t, x, y, z, a, b, c, F) =>
        //主要介绍参考上面的计算参数。这个方法不求逆，直接求出这个形式的矩阵与输入向量的积。
        {
            return new P_V_Pair(t * F[3] - F[0], t * F[4] - F[1], t * F[5] - F[2], x * F[0] + a * F[1] + b * F[2] - F[3], a * F[0] + y * F[1] + c * F[2] - F[4], b * F[0] + c * F[1] + z * F[2] - F[5]);
        };

        public static Func<List<P_V_Pair>, double[], List<P_V_Pair>, List<P_V_Pair>> J12I_FCalculation_Full = (pL, tL, F) =>
        //GL2中使用的雅可比矩阵称为J12。这个方法输入矩阵参量和输入函数，输出12维牛顿迭代法函数矢量。
        //矩阵形如：（J6矩阵如上，用x,y,z,a,b,c,t代表；J6N是对角元为0的J6矩阵。）
        // J61 J6N2
        // J6N3 J64
        //输入的pL共4项，每项依次代表J61，J6N2到J64的J6矩阵对应的A3矩阵参量，每项内依次为xyzabc，tL同理。
        {
            double t11 = tL[0];
            double t12 = tL[1];
            double t21 = tL[2];
            double t22 = tL[3];
            //第一部分：初等行变换，消去J61与J64的右下角分块，便于提取A3矩阵组合成A6矩阵。S1到S4是行变换的时候乘的系数。
            P_V_Pair inputVec = new P_V_Pair(F[0][3], F[0][4], F[0][5], F[1][3], F[1][4], F[1][5]);
            P_V_Pair remainVec = new P_V_Pair(F[0][0], F[0][1], F[0][2], F[1][0], F[1][1], F[1][2]);
            double S1 = t22 / (t11 * t22 - t12 * t21);
            double S2 = -t12 / (t11 * t22 - t12 * t21);
            double S3 = -t21 / (t11 * t22 - t12 * t21);
            double S4 = t11 / (t11 * t22 - t12 * t21);
            inputVec[0] += S1 * remainVec[0] + S2 * remainVec[3];
            inputVec[1] += S1 * remainVec[1] + S2 * remainVec[4];
            inputVec[2] += S1 * remainVec[2] + S2 * remainVec[5];
            inputVec[3] += S3 * remainVec[0] + S4 * remainVec[3];
            inputVec[4] += S3 * remainVec[1] + S4 * remainVec[4];
            inputVec[5] += S3 * remainVec[2] + S4 * remainVec[5];
            var A311p = pL[0];
            A311p[0] -= S1;
            A311p[1] -= S1;
            A311p[2] -= S1;
            pL[0] = A311p;
            var A322p = pL[3];
            A322p[0] -= S4;
            A322p[1] -= S4;
            A322p[2] -= S4;
            pL[3] = A322p;

            //第二部分：A6阵求解 求解得到输出矢量的第1，2，3，7，8，9项组成的核心矢量
            P_V_Pair Out1 = A6I_VCalculation(pL, inputVec);

            //第三部分：求出剩余部分，即第4，5，6，10，11，12项

            P_V_Pair Out2 = new P_V_Pair();
            Out2[0] = (t22 * (remainVec[0] + Out1[0]) - t12 * (remainVec[3] + Out1[3])) / (t11 * t22 - t21 * t12);
            Out2[1] = (t22 * (remainVec[1] + Out1[1]) - t12 * (remainVec[4] + Out1[4])) / (t11 * t22 - t21 * t12);
            Out2[2] = (t22 * (remainVec[2] + Out1[2]) - t12 * (remainVec[5] + Out1[5])) / (t11 * t22 - t21 * t12);
            Out2[3] = (-t21 * (remainVec[0] + Out1[0]) + t11 * (remainVec[3] + Out1[3])) / (t11 * t22 - t21 * t12);
            Out2[4] = (-t21 * (remainVec[1] + Out1[1]) + t11 * (remainVec[4] + Out1[4])) / (t11 * t22 - t21 * t12);
            Out2[5] = (-t21 * (remainVec[2] + Out1[2]) + t11 * (remainVec[5] + Out1[5])) / (t11 * t22 - t21 * t12);

            return new List<P_V_Pair>
            {
                new P_V_Pair(Out1[0],Out1[1],Out1[2],Out2[0],Out2[1],Out2[2]),
                new P_V_Pair(Out1[3],Out1[4],Out1[5],Out2[3],Out2[4],Out2[5])
            };
        };

        public static Func<List<P_V_Pair>, double[], List<P_V_Pair>, List<P_V_Pair>> J18I_FCalculation_Full = (pL, tL, F) =>
        //GL3中使用的雅可比矩阵称为J18。这个方法输入矩阵参量和输入函数，输出18维牛顿迭代法函数矢量。
        //矩阵形如：（标识同J12
        // J61 J6N2 J6N3
        // J6N4 J65 J6N6
        // J6N7 J6N8 J69
        //输入的pL共9项，每项依次代表J61，J6N2到J69的J6矩阵对应的A3矩阵参量，每项内依次为xyzabc，tL同理。
        {
            double t11 = tL[0];
            double t12 = tL[1];
            double t13 = tL[2];
            double t21 = tL[3];
            double t22 = tL[4];
            double t23 = tL[5];
            double t31 = tL[6];
            double t32 = tL[7];
            double t33 = tL[8];
            //第一部分：初等行变换，消去J61，J65和J69的右下角分块，便于提取A3矩阵组合成A9矩阵。S1到S9是行变换的时候乘的系数，具体分布是这样的：消去J61时，从上到下依次为S1到S3；J65对应S4到S6；J69对应S7到S9。
            //S1到S9的计算实际上相当于求一个三阶矩阵的逆矩阵。
            double[] inputVec = new double[] { F[0][3], F[0][4], F[0][5], F[1][3], F[1][4], F[1][5], F[2][3], F[2][4], F[2][5] };
            double[] remainVec = new double[] { F[0][0], F[0][1], F[0][2], F[1][0], F[1][1], F[1][2], F[2][0], F[2][1], F[2][2] };
            double det = t11 * t22 * t33 + t12 * t23 * t31 + t13 * t21 * t32 - t11 * t23 * t32 - t12 * t21 * t33 - t13 * t22 * t31;
            double S1 = (t22 * t33 - t23 * t32) / det;
            double S2 = (t13 * t32 - t12 * t33) / det;
            double S3 = (t12 * t23 - t13 * t22) / det;
            double S4 = (t23 * t31 - t21 * t33) / det;
            double S5 = (t11 * t33 - t13 * t31) / det;
            double S6 = (t13 * t21 - t11 * t23) / det;
            double S7 = (t21 * t32 - t22 * t31) / det;
            double S8 = (t12 * t31 - t11 * t32) / det;
            double S9 = (t11 * t22 - t12 * t21) / det;
            inputVec[0] += S1 * remainVec[0] + S2 * remainVec[3] + S3 * remainVec[6];
            inputVec[1] += S1 * remainVec[1] + S2 * remainVec[4] + S3 * remainVec[7];
            inputVec[2] += S1 * remainVec[2] + S2 * remainVec[5] + S3 * remainVec[8];
            inputVec[3] += S4 * remainVec[0] + S5 * remainVec[3] + S6 * remainVec[6];
            inputVec[4] += S4 * remainVec[1] + S5 * remainVec[4] + S6 * remainVec[7];
            inputVec[5] += S4 * remainVec[2] + S5 * remainVec[5] + S6 * remainVec[8];
            inputVec[6] += S7 * remainVec[0] + S8 * remainVec[3] + S9 * remainVec[6];
            inputVec[7] += S7 * remainVec[1] + S8 * remainVec[4] + S9 * remainVec[7];
            inputVec[8] += S7 * remainVec[2] + S8 * remainVec[5] + S9 * remainVec[8];
            var A311p = pL[0];
            A311p[0] -= S1;
            A311p[1] -= S1;
            A311p[2] -= S1;
            pL[0] = A311p;
            var A322p = pL[4];
            A322p[0] -= S5;
            A322p[1] -= S5;
            A322p[2] -= S5;
            pL[4] = A322p;
            var A333p = pL[8];
            A322p[0] -= S9;
            A322p[1] -= S9;
            A322p[2] -= S9;
            pL[8] = A333p;

            //第二部分：A9阵求解 求解得到输出矢量的第1，2，3，7，8，9，13，14，15项组成的核心矢量
            double[] Out1 = A9I_VCalculation(pL, inputVec);

            //第三部分：求出剩余部分，即第4，5，6，10，11，12，16，17，18项

            double[] Out2 = new double[9];
            Out2[0] = S1 * (Out1[0] + remainVec[0]) + S2 * (Out1[3] + remainVec[3]) + S3 * (Out1[6] + remainVec[6]);
            Out2[1] = S1 * (Out1[1] + remainVec[1]) + S2 * (Out1[4] + remainVec[4]) + S3 * (Out1[7] + remainVec[7]);
            Out2[2] = S1 * (Out1[2] + remainVec[2]) + S2 * (Out1[5] + remainVec[5]) + S3 * (Out1[8] + remainVec[8]);
            Out2[3] = S4 * (Out1[0] + remainVec[0]) + S5 * (Out1[3] + remainVec[3]) + S6 * (Out1[6] + remainVec[6]);
            Out2[4] = S4 * (Out1[1] + remainVec[1]) + S5 * (Out1[4] + remainVec[4]) + S6 * (Out1[7] + remainVec[7]);
            Out2[5] = S4 * (Out1[2] + remainVec[2]) + S5 * (Out1[5] + remainVec[5]) + S6 * (Out1[8] + remainVec[8]);
            Out2[6] = S7 * (Out1[0] + remainVec[0]) + S8 * (Out1[3] + remainVec[3]) + S9 * (Out1[6] + remainVec[6]);
            Out2[7] = S7 * (Out1[1] + remainVec[1]) + S8 * (Out1[4] + remainVec[4]) + S9 * (Out1[7] + remainVec[7]);
            Out2[8] = S7 * (Out1[2] + remainVec[2]) + S8 * (Out1[5] + remainVec[5]) + S9 * (Out1[8] + remainVec[8]);


            return new List<P_V_Pair>
            {
                new P_V_Pair(Out1[0],Out1[1],Out1[2],Out2[0],Out2[1],Out2[2]),
                new P_V_Pair(Out1[3],Out1[4],Out1[5],Out2[3],Out2[4],Out2[5]),
                new P_V_Pair(Out1[6],Out1[7],Out1[8],Out2[6],Out2[7],Out2[8]),
            };
        };


        public static Func<double, double, double, double, double, double, Vector3d, Vector3d> A3I_VCalculation = (x, y, z, a, b, c, F) =>
        //形如这样的三阶方阵：
        // x a b
        // a y c
        // b c z
        //求出它的逆，并与输入矢量F相乘，返回这个乘积。
        {
            Vector3d result = new Vector3d(0, 0, 0);
            result.x = F.y * (b * c - a * z) + F.z * (a * c - b * y) + F.x * (y * z - c * c);
            result.y = F.z * (a * b - c * x) + F.x * (b * c - a * z) + F.y * (x * z - b * b);
            result.z = F.x * (a * c - b * y) + F.y * (a * b - c * x) + F.z * (x * y - a * a);
            result /= 2 * a * b * c - c * c * x - b * b * y - a * a * z + x * y * z;
            return result;
        };

        public static Func<double, double, double, double, double, double, Vector3d, Vector3d> A3_VCalculation = (x, y, z, a, b, c, F) =>
        //形如这样的三阶方阵：
        // x a b
        // a y c
        // b c z
        //直接将它与输入矢量F相乘，返回这个乘积。
        {
            Vector3d result = new Vector3d(0, 0, 0);
            result.x = F.y * (b * c - a * z) + F.z * (a * c - b * y) + F.x * (y * z - c * c);
            result.y = F.z * (a * b - c * x) + F.x * (b * c - a * z) + F.y * (x * z - b * b);
            result.z = F.x * (a * c - b * y) + F.y * (a * b - c * x) + F.z * (x * y - a * a);
            result /= 2 * a * b * c - c * c * x - b * b * y - a * a * z + x * y * z;
            return result;
        };

        public static Func<List<P_V_Pair>, P_V_Pair, P_V_Pair> A6I_VCalculation = (pL, F) =>
        //将上面两个方法计算的方阵称为A3方阵。将4个A3方阵拼在一起得到一个新的方阵称为A6方阵。求出他的逆，并与输入的六维矢量F相乘，输出这个乘积。
        //A6方阵形式如下：
        // A31 A32
        // A33 A34
        //每个A3方阵有6个独立参数，四个A3方阵可以各不相同，因此输入的List<P_V_Pair>应该包含4组值，顺序按A31到A34矩阵，每组值内的第0项到5项依次为A3方阵的xyzabc值。
        {
            /*
            Matrix<double> A31 = Matrix<double>.Build.DenseOfColumnArrays(pL[0].ToA3Array());
            Matrix<double> A32 = Matrix<double>.Build.DenseOfColumnArrays(pL[1].ToA3Array());
            Matrix<double> A33 = Matrix<double>.Build.DenseOfColumnArrays(pL[2].ToA3Array());
            Matrix<double> A34 = Matrix<double>.Build.DenseOfColumnArrays(pL[3].ToA3Array());

            Matrix<double> A6 = Matrix<double>.Build.DenseOfMatrixArray(new Matrix<double>[,] { { A31, A32 }, { A33, A34 } });
            */

            var x1 = pL[0][0];
            var y1 = pL[0][1];
            var z1 = pL[0][2];
            var a1 = pL[0][3];
            var b1 = pL[0][4];
            var c1 = pL[0][5];
            var x2 = pL[1][0];
            var y2 = pL[1][1];
            var z2 = pL[1][2];
            var a2 = pL[1][3];
            var b2 = pL[1][4];
            var c2 = pL[1][5];
            var x3 = pL[2][0];
            var y3 = pL[2][1];
            var z3 = pL[2][2];
            var a3 = pL[2][3];
            var b3 = pL[2][4];
            var c3 = pL[2][5];
            var x4 = pL[3][0];
            var y4 = pL[3][1];
            var z4 = pL[3][2];
            var a4 = pL[3][3];
            var b4 = pL[3][4];
            var c4 = pL[3][5];

            double[,] A6 = new double[,]
            {
                {x1,a1,b1,x2,a2,b2},
                {a1,y1,c1,a2,y2,c2},
                {b1,c1,z1,b2,c2,z2},
                {x3,a3,b3,x4,a4,b4},
                {a3,y3,c3,a4,y4,c4},
                {b3,c3,z3,b4,c4,z4}
            };

            return new P_V_Pair(Matrix_I_V_Calculation(A6, 6, F.ToDoubleArray()));

        };

        public static Func<List<P_V_Pair>, double[], double[]> A9I_VCalculation = (pL, F) =>
        //将9个A3方阵拼在一起得到一个新的方阵称为96方阵。求出他的逆，并与输入的9维矢量F相乘，输出这个乘积。
        //A9方阵形式如下：
        // A31 A32 A33
        // A34 A35 A36
        // A37 A38 A39
        //每个A3方阵有6个独立参数，9个A3方阵可以各不相同，因此输入的List<P_V_Pair>应该包含9组值，顺序按A31到A39矩阵，每组值内的第0项到5项依次为A3方阵的xyzabc值。
        {
            double[,] A9 = new double[9,9];

            for (int i = 0; i < 9; i++)
            {
                int CenterRow = 3 * (i / 3) + 1;
                int CenterColumn = 3 * (i % 3) + 1;
                double x = pL[i][0];
                double y = pL[i][1];
                double z = pL[i][2];
                double a = pL[i][3];
                double b = pL[i][4];
                double c = pL[i][5];
                A9[CenterRow - 1, CenterColumn - 1] = x;
                A9[CenterRow - 1, CenterColumn] = a;
                A9[CenterRow - 1, CenterColumn + 1] = b;
                A9[CenterRow, CenterColumn - 1] = a;
                A9[CenterRow, CenterColumn] = y;
                A9[CenterRow, CenterColumn + 1] = c;
                A9[CenterRow + 1, CenterColumn - 1] = b;
                A9[CenterRow + 1, CenterColumn] = c;
                A9[CenterRow + 1, CenterColumn + 1] = z;
            }

            return Matrix_I_V_Calculation(A9, 9, F);

        };


        public static Func<double[][], Func<double,Vector3d, List<Vector3d>>, Func<List<P_V_Pair>, List<P_V_Pair>>> CreateA3MatrixParam = (pL, Func) =>
        //输入系统参量列表tL，这个数组的结构是这样的，第一项指定他对应第几个k/f值，第二项指定这个目标函数内的时间附加量，剩余的参量依次指定各项的系数（参考butcher表），输入引力雅可比函数Func，输出这样一个函数：对这个函数输入参量K，将会输出一个六元矢量列表，其中，每一项的分量是由K和系数决定的J6矩阵中的A3矩阵的参量，依次为xyzabc；列表中元素的顺序由pL指定。
        {
            Func<List<P_V_Pair>, List<P_V_Pair>> OutFunc = K =>
            {
                List<P_V_Pair> outList = new List<P_V_Pair>{};
                for (int i = 0; i < pL.Length; i++)
                {
                    for (int k = 1; k < pL[i].Length; k++)
                    {
                        Vector3d inputVec = new Vector3d(0, 0, 0);
                        for (int j = 1; j < pL[i].Length; j++)
                        {
                            inputVec += pL[i][j] * K[j - 1].Position;
                        }
                        List<Vector3d> JacobiVector = Func(pL[i][0] * h, inputVec);
                        P_V_Pair outVec = new P_V_Pair(JacobiVector[0][0], JacobiVector[1][1], JacobiVector[2][2], JacobiVector[0][1], JacobiVector[0][2], JacobiVector[1][2]);
                        outVec *= h * pL[i][k];
                        outList.Add(outVec);
                    }
                }
                return outList;
            };
            return OutFunc;
        };

        public static Func<List<P_V_Pair>, List<P_V_Pair>> CreateTargetFunction(double[][] paramList, Func<double, P_V_Pair, P_V_Pair> func)
        {
            Func<List<P_V_Pair>, List<P_V_Pair>> OutFunc = K =>
            {
                List<P_V_Pair> outList = new List<P_V_Pair> { };
                for (int i = 0; i < paramList.Length; i++)
                {
                    P_V_Pair inputVec = new P_V_Pair();
                    for (int j = 1; j < paramList[i].Length; j++)
                    {
                        inputVec += paramList[i][j] * K[j - 1];
                    }
                    P_V_Pair fout = h * func(paramList[i][0] * h, inputVec) - K[i];
                    outList.Add(fout);
                }
                return outList;
            };
            return OutFunc;
        }


        public static double[] Matrix_I_V_Calculation(double[,] matrix, int n, double[] v)
        //输入一个矩阵，输入阶数，输出将矩阵求逆之后与向量相乘的结果（基于矩阵的LU分解）
        {
            Matrix_LUDecomposition(matrix, n, out double[,] L, out double[,] U);
            double[] y = new double[n];
            for (int i = 0; i < n; i++)
            {
                y[i] = v[i];
                for (int j = 0; j < i; j++)
                {
                    y[i] -= L[i, j] * y[j];
                }
            }
            double[] x = new double[n];
            for (int i = n - 1; i <= 0; i--)
            {
                x[i] = y[i];
                for (int j = n - 1; j > i; j--)
                {
                    x[i] -= U[i, j] * x[j];
                }
                x[i] /= U[i, i];
            }
            return x;
        }


        public static void Matrix_LUDecomposition(double[,] A, int n,out double[,] L,out double[,] U)
        //输入矩阵元,输入矩阵阶数，进行LU分解,输出列表包括两项，依次为L矩阵和U矩阵
        {
            L = new double[n,n];
            U = new double[n,n];
            for (int k = 0; k < n; k++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (j < k)
                    {
                        U[k, j] = 0;
                        L[j, k] = 0;
                    }
                    else
                    {
                        U[k, j] = A[k, j];
                        L[j, k] = A[j, k];
                        for (int m = 0; m < k; m++)
                        {
                            U[k, j] -= U[m, j] * L[k, m];
                            L[j, k] -= L[j, m] * U[m, k];
                        }
                        L[j, k] /= U[k, k];
                    }
                }
            }
        }


        //第三部分：其他数值计算方法

        //Störmer方法：这一方法是双步方法，输入yn-1和yn，输出yn+1。具有计算速度很快并且在保守场内很好保持能量守恒特性的优点。

        public static P_V_Pair StormerMethod(P_V_Pair y_0, P_V_Pair y_1, double x_0, Func<double, Vector3d, Vector3d> func)
        //Stormer，二阶精度，输入y0和y1以及y0对应时间x0，输入万有引力函数，输出y2
        {
            Vector3d p0 = y_0.Position;
            Vector3d p1 = y_1.Position;
            Vector3d p2 = 2 * p1 - p0 + h * h * func(x_0 + h, p1);
            Vector3d v1 = y_1.Velocity;
            Vector3d v2 = v1 + h / 2 * (func(x_0 + h, p1) + func(x_0 + 2 * h, p2));//此为梯形积分法估计速度，优点为有保持能量守恒的趋势
            //Vector3d v2 = (3 * p2 - 4 * p1 + p0) / 2 * h;//此为中心差分法估计速度，优点为计算速度快
            return new P_V_Pair(p2, v2);
        }

        //Verlet方法：经典的保辛的单步算法。

        public static P_V_Pair VerletMethod(P_V_Pair y_n, double x_n, Func<double, Vector3d, Vector3d> func)
        //Verlet方法，二阶精度，保辛。单步计算，输入yn和xn，输入万有引力函数，输出下一步状态。
        {
            Vector3d pn = y_n.Position;
            Vector3d vn = y_n.Velocity;
            Vector3d v1 = vn + h / 2 * func(x_n, pn);
            Vector3d po = pn + h * v1;
            Vector3d vo = v1 + h / 2 * func(x_n + h, po);
            return new P_V_Pair(po, vo);
        }


        //Yoshida方法：更高级别的显式保辛算法。基于可分离的哈密顿方程开发，具有计算速度快，精度高而且保辛的优点。没有缺点喵（叉腰

        public static P_V_Pair YoshidaMethod(P_V_Pair y_n, double x_n, Func<double, Vector3d, Vector3d> F)
        //Yoshida方法,四阶精度，保辛，单步计算，输入万有引力函数func.
        {
            //w0 = -Math.Pow(2, 1 / 3) / (2 - Math.Pow(2, 1 / 3));
            //w1 = 1 / (2 - Math.Pow(2, 1 / 3));
            //w2 = (w0+w1) /2
            double w0 = -1.7024143839193152680953756179429 * h;
            double w1 = 1.3512071919596576340476878089715 * h;
            double w2 = -0.1756035959798288170238439044857 * h;
            Vector3d p = y_n.Position;
            Vector3d v = y_n.Velocity;
            double t = x_n;

            //double[] dt = new double[] { w1 * h, w0 * h, w1 * h };

            v += w1 * F(t, p) / 2;
            p += v * w1;
            t += w1;
            v += w2 * F(t, p);

            //v += dt[1] * F(t, p) / 2;
            p += v * w0;
            t += w0;
            v += w2 * F(t, p);

            //v += dt[2] * F(t, p) / 2;
            p += v * w1;
            t += w1;
            v += w1 * F(t, p) / 2;

            return new P_V_Pair(p, v);
        }


        //杂项
        public static readonly int n = 10;//牛顿法的迭代次数
        public static List<P_V_Pair> NewtonIteration(Func<List<P_V_Pair>, List<P_V_Pair>> JFFunction, List<P_V_Pair> startPosition)
        //牛顿迭代法求函数零点，输入函数为目标函数的雅可比逆与函数的乘积矢量（即在迭代时会用到的J^-1 F），输入起始位置，输出(近似的)零点
        {
            List<P_V_Pair> x = startPosition;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < x.Count; j++)
                {
                    x[j] -= JFFunction(x)[j];
                }
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
        public P_V_Pair(double[] list)
        {
            px = list[0];
            py = list[1];
            pz = list[2];
            vx = list[3];
            vy = list[4];
            vz = list[5];
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

        public override string ToString()
        {
            return $"({px},{py},{pz},{vx},{vy},{vz})";
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

        public double[] ToDoubleArray()
        {
            return new double[] { px, py, pz, vx, vy, vz };
        }

        public double[][] ToA3Array()
        {
            return new double[][] { new double[] { px, vx, vy }, new double[] { vx, py, vz }, new double[] { vy, vz, pz } };
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