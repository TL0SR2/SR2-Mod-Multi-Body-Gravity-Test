using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModApi.Craft;
using UnityEngine;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGManeuverNode
    {
        public MBGOrbitLine orbitLine;

        private MBGOrbit orbit => orbitLine.MBGOrbitInfo.MBGOrbit;

        private CraftNode craft => orbit.CurrentCraft;

        private double MaxThrust => craft.CraftScript.FlightData.MaxActiveEngineThrustUnscaled;

        private double Mass => craft.CraftScript.FlightData.CurrentMassUnscaled;

        public double MaxAcc => MaxThrust / Mass;

        public MBGOrbitPoint ManeuverPoint;
        public Vector3d DeltaV;
        public double ThrustTime => DeltaV.magnitude / MaxAcc;

        public MBGManeuverNode(MBGOrbitLine line, MBGOrbitPoint startPoint, Vector3d DV)
        {
            orbitLine = line;
            ManeuverPoint = startPoint;
            DeltaV = DV;
        }
    }
}