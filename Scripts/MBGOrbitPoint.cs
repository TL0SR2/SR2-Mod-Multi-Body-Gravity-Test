using ModApi.Flight.Sim;
using UnityEngine;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbitPoint
    {
        public P_V_Pair State;
        public double Time;

        public Vector3d ThrustAcc = new Vector3d(0,0,0);

        public MBGOrbitPoint(P_V_Pair state, double time)
        {
            State = state;
            Time = time;
        }
        public MBGOrbitPoint()
        {
            State = new P_V_Pair();
            Time = 0;
        }
        public MBGOrbitPoint(IOrbitPoint point)
        {
            State = new P_V_Pair(point.Position, point.Velocity);
            Time = point.Time;
        }
        public void Set(P_V_Pair state, double time)
        {
            State = state;
            Time = time;
        }
    }
}