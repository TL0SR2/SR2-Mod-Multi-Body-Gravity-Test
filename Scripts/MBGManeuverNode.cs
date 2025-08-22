using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using ModApi.Craft;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGManeuverNode
    {
        public MBGOrbitLine orbitLine;

        private MBGOrbit orbit => orbitLine.MBGOrbitInfo.MBGOrbit;

        private CraftNode craft => orbit.CurrentCraft;

        private double MaxThrust => craft.CraftScript.FlightData.MaxActiveEngineThrust;

        private double Mass => craft.CraftScript.Mass;

        public double MaxAcc => MaxThrust / Mass;

        public MBGOrbitPoint ManeuverPoint;
        public Vector3d DeltaV;

        public Vector3d AccVec => MaxAcc * DeltaV.normalized;
        public double ThrustTime
        {
            get
            {
                if (DeltaV.magnitude == 0) return 0;
                else return DeltaV.magnitude / MaxAcc;
            }
        }
        /*
        public MBGManeuverNode(double time)
        {
            orbitLine = null;
            ManeuverPoint = new MBGOrbitPoint(new P_V_Pair(), time);
            DeltaV = Vector3d.zero;
        }
        */

        public MBGManeuverNode(MBGOrbitLine line, MBGOrbitPoint startPoint, Vector3d DV)
        {
            orbitLine = line;
            ManeuverPoint = startPoint;
            DeltaV = DV;
            if ((MaxAcc == 0) && (DV.magnitude != 0))
            {
                Game.Instance.UserInterface.CreateMessageDialog("Warning:No Engine Activeted.");
            }
        }
    }
}