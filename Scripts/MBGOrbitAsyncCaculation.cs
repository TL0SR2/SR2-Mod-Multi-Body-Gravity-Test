using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbitAsyncCaculation
    {
        public static double MaxCaculateTime = 6E+8;
        //public static double elapsedTime = 60;
        public MBGOrbitAsyncCaculation(MBGOrbit orbit)
        {
            _orbit = orbit;

        }

        public void StartCaculation(MBGOrbitPoint StartPoint, int startN)
        {
            CaculationEnable = true;
            _thread = new Thread(() => CaculationMethod(StartPoint, startN));
            _thread.IsBackground = true;
            _thread.Name = "MBG Caculaion";
            _thread.Start();
        }
        public void StopCaculation()
        {
            CaculationEnable = false;
            _thread.Abort();
        }

        private void CaculationMethod(MBGOrbitPoint StartPoint, int n)
        {
            try
            {
                MBGMath.NumericalIntegration(StartPoint, _orbit.Time_ThrustAcc_Dic,
                    time => CaculationEnable && time <= MBGOrbit.CurrentTime + MaxCaculateTime,
                    point =>
                    {
                        n++;
                        _orbit.AddOrChangePoint(point, n);
                    }
                );
            }
            finally{}
        }
        private bool CaculationEnable = false;
        private MBGOrbit _orbit;

        //private Task _task;

        private Thread _thread;

        public double time { get; private set; }
    }
}