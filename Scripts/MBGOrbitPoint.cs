using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModApi.Flight.Sim;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbitPoint
    {
        public P_V_Pair State;
        public double Time;

        public MBGOrbitPoint(P_V_Pair state, double time)
        {
            State = state;
            Time = time;
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