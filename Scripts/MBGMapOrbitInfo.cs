using System;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.MapView.Interfaces.Contexts;
using Assets.Scripts.Flight.MapView.Items;
using Assets.Scripts.Flight.MapView.Orbits;
using Assets.Scripts.Flight.MapView.Orbits.Chain;
using Assets.Scripts.Flight.MapView.Orbits.Chain.ManeuverNodes;
using Assets.Scripts.Flight.Sim;
using ModApi.Common.Events;
using ModApi.Flight.MapView;
using ModApi.Flight.Sim;
using ModApi.Ioc;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGMapOrbitInfo
    {
        public MBGMapOrbitInfo(IIocContainer ioc, IMapViewContext mapViewContext, MBGOrbit orbit, Camera mapCamera, MBGOrbitLine orbitLine, CraftNode craft)
        {
            MBGMapOrbitInfo info = this;
            Camera = mapCamera;
            MBGOrbit = orbit;
            _craftnode = craft;
            _orbitLine = orbitLine;
            _ioc = ioc;
            _mapViewContext = mapViewContext;
            UnityEventDispatcher.Instance.ExecuteYield<WaitForEndOfFrame>(delegate ()
            {
                IItemRegistry itemRegistry = ioc.Resolve<IItemRegistry>(info._mapViewContext, false);
                info._mapItem = itemRegistry.GetItem(craft);
            });
            _drawModeProvider = ioc.Resolve<IDrawModeProvider>(_mapViewContext, false);
            _playerCraftProvider = ioc.Resolve<IPlayerCraftProvider>(_mapViewContext, false);
            CoordinateConverter = ioc.Resolve<IMapViewCoordinateConverter>(_mapViewContext, false);
            orbit.orbitInfo = this;
        }
        public double EndTime => double.PositiveInfinity;
        public int Id => _orbitLine.Id;
        public bool InContactWithPlanet => _craftnode != null && _craftnode.InContactWithPlanet;
        public Color OrbitColor => _orbitLine.Color;
        public IOrbitInteractionEventRecipient OrbitInteractionEventRecipient => _orbitLine;
        public Camera Camera { get; private set; }
        public IMapViewCoordinateConverter CoordinateConverter { get; internal set; }

        public void SetOrbit(MBGOrbit mbgOrbit)
        {
            MBGOrbit = mbgOrbit;
            mbgOrbit.orbitInfo = this;
        }

        public void DestroyOrbitLine()
        {
            _orbitLine.Destroy();
        }
        public void DisableOrbitLine()
        {
            _orbitLine.Disable();
        }
        public void EnableOrbitLine()
        {
            _orbitLine.Enable();
        }
        public void ForceOrbitLineUpdate()
        {
            _orbitLine.ForceUpdate();
        }
        public bool IsAssociatedWith(MapOrbitLine orbitLine)
        {
            return _orbitLine == orbitLine;
        }
        /*
		public bool IsAssociatedWith(ICameraFocusable cameraFocusable)
        {
            MapOrbitInfo mapOrbitInfo = null;
            if (cameraFocusable is OrbitChainNodeScript)
            {
                mapOrbitInfo = (cameraFocusable as OrbitChainNodeScript).OrbitInfo;
            }
            else if (cameraFocusable is MapItem)
            {
                mapOrbitInfo = (cameraFocusable as MapItem).OrbitInfo;
            }
            return this == mapOrbitInfo;
        }*/

        private CraftNode _craftnode;
        private IIocContainer _ioc;
        private MapItem _mapItem;
        private IDrawModeProvider _drawModeProvider;
        private IPlayerCraftProvider _playerCraftProvider;
        private IMapViewContext _mapViewContext;
        public MBGOrbit MBGOrbit { get; private set; }
        public MBGOrbitLine _orbitLine { get; private set; }
        public MBGOrbitPoint PlanetIntersection;
        //如果与行星撞击，给出撞击点
    }
}