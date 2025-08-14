using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ModApi.Flight.Sim;
using Assets.Scripts.Flight.Sim;
using ModApi.Planet;
using System.Linq;
using ModApi.Flight;
using Assets.Scripts.Flight.UI;
using System.Diagnostics.SymbolStore;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGGeneralizedPlanet
    {
        public IPlanetNode Planet;
        public bool isSun => Planet.Parent == null;

        public MBGGeneralizedPlanet(IPlanetNode planet)
        {
            Planet = planet;
            //isSun = planet.Parent == null;
        }

        public Vector3d GetSolarPositionAtTime(double time)
        {
            if (isSun)
            {
                return Planet.GetSolarPositionAtTime(time);
            }

            else
            {
                return MBGMath_CaculationMethod.GetLagrangePointPosition(Planet.GetSolarPositionAtTime(time) - Planet.Parent.GetSolarPositionAtTime(time), type, Planet.Parent.PlanetData.Mass, Planet.PlanetData.Mass, Planet.GetSolarVelocityAtTime(time) - Planet.Parent.GetSolarVelocityAtTime(time)) + Planet.Parent.GetSolarPositionAtTime(time);
            }
                
        }
        /*
        public Vector3d GetSolarVelocityAtTime(double time)
        {
            return Planet.GetSolarVelocityAtTime(time);
        }
        */

        public Vector3d SolarPosition
        {
            get
            {

                if (isSun)
                {
                    return Planet.SolarPosition;
                }

                else
                {
                    return MBGMath_CaculationMethod.GetLagrangePointPosition(Planet.SolarPosition - Planet.Parent.SolarPosition, type, Planet.Parent.PlanetData.Mass, Planet.PlanetData.Mass, Planet.SolarVelocity - Planet.Parent.SolarVelocity) + Planet.Parent.SolarPosition;
                }
            }
        }
        public IPlanetNode Parent => Planet.Parent;

        public GeneralizedPlanetType type = GeneralizedPlanetType.Planet;

    }

    public enum GeneralizedPlanetType
    {
        Planet = 0,
        L1 = 1,
        L2 = 2,
        L3 = 3,
        L4 = 4,
        L5 = 5
    }
}