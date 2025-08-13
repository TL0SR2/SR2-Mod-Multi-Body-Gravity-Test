using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbitPointSet
    {
        public MBGOrbitPointSet() { }
        public MBGOrbitPointSet(List<MBGOrbitPoint> points)
        {
            Update(points);
        }
        public void Update(List<MBGOrbitPoint> points)
        {
            _points = points;
            TotalTime = points.Last().Time - points[0].Time;
        }
        public MBGOrbitPoint GetPoint(int index)
        {
            return _points[index];
        }
        public MBGOrbitPoint Last(int indexFromEnd = 0)
        {
            return _points[_points.Count - indexFromEnd - 1];
        }
        public void Add(MBGOrbitPoint point)
        {
            _points.Add(point);
        }
        public int Count => _points.Count;
        private List<MBGOrbitPoint> _points = new List<MBGOrbitPoint> { };
        public double TotalTime { get; private set; } = 0;
        public bool IntersectsPlanet;
        //一个用于指示当前轨迹是否与行星碰撞的值
        public int IntersectsPlanetNum;
        //如果与行星碰撞，给出碰撞点的N值
    }
}