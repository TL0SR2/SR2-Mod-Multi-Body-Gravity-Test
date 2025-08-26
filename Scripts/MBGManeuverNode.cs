using UnityEngine;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGManeuverNode
    {
        public MBGOrbitLine orbitLine;

        public Vector3d ScreenPosition => this.orbitLine.CoordinateConverter.ConvertSolarToMapView(this.orbitLine.GetPointSolarPosition(this.ManeuverPoint));

        private MBGOrbit orbit => orbitLine.MBGOrbitInfo.MBGOrbit;

        private CraftNode craft => orbit.CurrentCraft;

        private double MaxThrust => craft.CraftScript.FlightData.MaxActiveEngineThrust;

        private double Mass => craft.CraftScript.Mass;

        public double MaxAcc => MaxThrust / Mass;

        public MBGOrbitPoint ManeuverPoint;
        public Vector3d DeltaV;

        public MBGManeuverNodeScript nodeScript { get; private set; }

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

        public MBGManeuverNode(MBGOrbitLine line, MBGOrbitPoint startPoint, Vector3d DV,MBGManeuverNodeScript script)
        {
            orbitLine = line;
            ManeuverPoint = startPoint;
            DeltaV = DV;
            nodeScript = script;
            if ((MaxAcc == 0) && (DV.magnitude != 0))
            {
                Game.Instance.UserInterface.CreateMessageDialog("Warning:No Engine Activeted.");
            }
        }
    }
}