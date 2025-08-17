using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGManeuverNode
    {
        public MBGOrbitLine orbitLine;

        public MBGOrbitPoint ManeuverPoint;
        public Vector3d DeltaV;
        public double ThrustTime;
    }
}