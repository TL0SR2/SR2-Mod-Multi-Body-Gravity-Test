using System;
using System.Collections.Generic;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.MapView.Interfaces.Contexts;
using Assets.Scripts.Flight.MapView.Items;
using Assets.Scripts.Flight.MapView.Orbits;
using Assets.Scripts.Flight.MapView.Orbits.Chain.ManeuverNodes;
using Assets.Scripts.Flight.MapView.Orbits.Chain.ManeuverNodes.Interfaces;
using Assets.Scripts.Flight.MapView.Orbits.Chain.SoiEncounters;
using Assets.Scripts.Flight.MapView.Orbits.DrawModes;
using Assets.Scripts.Flight.MapView.Orbits.DrawModes.Interfaces.IDrawMode;
using Assets.Scripts.Flight.MapView.Orbits.Interfaces;
using Assets.Scripts.Flight.MapView.UI;
using Assets.Scripts.Flight.Sim;
using ModApi;
using ModApi.Flight.MapView;
using ModApi.Flight.Sim;
using ModApi.Ioc;
using ModApi.Math;
using ModApi.State.MapView;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Vectrosity;
using Assets.Scripts.Flight.MapView;
using Assets.Scripts.Flight.UI;

//当前的开发进度：专注于完成让VectorLine正常绘制的代码（包括绘制和摄像机显示），将轨道点火节点等功能一律关闭
//轨道特殊点（包括与行星的撞击点）暂不启用


namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGOrbitLine : MapItem, ICameraFocusable, IOrbitInteractionEventRecipient
    //MapItem是这样一个类，他包含所有显示在地图上的实际物体和图标
    //ICameraFocusable是这样一个接口，他包含在地图界面上与可被摄像机聚焦的相关对象
    //IOrbitInteractionEventRecipient是这样一个接口，他管理鼠标光标扫过/停留在对象上时的相关方法
    {
        public static event EventHandler<string> ChangeReferencePlanetEvent;
        public override ICameraFocusable AssociatedPlanetCameraFocusable => _playerCraft.PlayerCraft.AssociatedPlanetCameraFocusable;
        //是当前轨道所关联的行星的ICameraFocusable对象喵。可能与视线参考系相关联喵

        event CameraFocusableItemDestroyedHandler ICameraFocusable.Destroyed
        //大概是ICameraFocusable对象在销毁的时候会触发的事件喵
        {
            add
            {
                _cameraFocusableDestroyed = (CameraFocusableItemDestroyedHandler)Delegate.Combine(_cameraFocusableDestroyed, value);
            }
            remove
            {
                _cameraFocusableDestroyed = (CameraFocusableItemDestroyedHandler)Delegate.Remove(_cameraFocusableDestroyed, value);
            }
        }
        public bool IsDrawing
        //指定当前是否绘制的变量
        {
            get
            {
                return _vectrocityLine.points3.Count > 0;
            }
        }
        public int OrbitLineSegments
        //表明当前轨道绘制段数的数值
        {
            get
            {
                return _vectrocityLine.points3.Count;
            }
        }

        public override Vector3 MapPosition
        //设置GameObj在地图上的游戏对象位置（大概
        {
            get
            {
                Vector3d solarPosition;
                if (MBGOrbit != null)
                {
                    solarPosition = MBGOrbit.CurrentCraft.SolarPosition;
                }
                else
                {
                    solarPosition = Vector3.zero;
                }
                return (Vector3)CoordinateConverter.ConvertSolarToMapView(solarPosition);
            }
        }
        public bool OrbitHoveredWithDelay { get; private set; }
        //一个关于光标当前是否停留在对象上的bool值
        IPlanetNode ICameraFocusable.AssociatedPlanet
        //表明当前所在行星（是否可能与视线参考系有关？）
        {
            get
            {
                return _currentplanet;
            }
        }
        bool ICameraFocusable.FocusByClick
        //表明这个ICameraFocusable是否能通过点击来选中并聚焦视角。显然是否定的
        {
            get
            {
                return false;
            }
        }
        ICameraFocusable ICameraFocusable.ItemToFocusOnWhenDeleted
        //表明这个ICameraFocusable在被摧毁的时候视角重置到什么地方。显然与视线参考系有关。
        {
            get
            {
                return base.ItemRegistry.GetPlanet(_currentplanet);
            }
        }
        float ICameraFocusable.MinZoomDistance
        //表明摄像机聚焦在这个ICameraFocusable对象上时，最近能zoom到多近的视角
        {
            get
            {
                return AssociatedPlanetCameraFocusable.MinZoomDistance;
            }
        }
        IOrbitNode ICameraFocusable.OrbitNode
        //大概和ICameraFocusable对象的一些属性有关。怀疑OrbitLine部分不使用此代码
        {
            get
            {
                return MBGOrbit.CurrentCraft.Parent;
            }
        }
        Vector3 ICameraFocusable.Position
        //同上，对于OrbitLine部分意义不明
        {
            get
            {
                return MapPosition;
            }
        }
        public MapItemData Data { get; private set; }
        OrbitInteractionScript.OrbitInteractionDelegate IOrbitInteractionEventRecipient.OnHoverEnter
        //管理光标扫过对象【进入状态】时的委托
        {
            get
            {
                return new OrbitInteractionScript.OrbitInteractionDelegate(OnHoverEnter);
            }
        }
        OrbitInteractionScript.OrbitInteractionDelegate IOrbitInteractionEventRecipient.OnHoverExit
        //管理光标扫过对象【离开状态】时的委托
        {
            get
            {
                return new OrbitInteractionScript.OrbitInteractionDelegate(OnHoverExit);
            }
        }
        OrbitInteractionScript.OrbitInteractionDelegate IOrbitInteractionEventRecipient.OnHoverStay
        //管理光标扫过对象【停留于对象上】时的委托
        {
            get
            {
                return new OrbitInteractionScript.OrbitInteractionDelegate(OnHoverStay);
            }
        }
        private void OnHoverEnter(OrbitInteractionScript source, OrbitInteractionScript.OrbitCursorInfo pointInfo)
        //光标扫过对象【进入状态】时触发的方法
        {
        }

        private void OnHoverExit(OrbitInteractionScript source, OrbitInteractionScript.OrbitCursorInfo pointInfo)
        //光标扫过对象【离开状态】时触发的方法
        {
            OrbitHoveredWithDelay = false;
        }

        private void OnHoverStay(OrbitInteractionScript source, OrbitInteractionScript.OrbitCursorInfo pointInfo)
        //光标扫过对象【停留于对象上】时触发的方法
        //下面的代码与点火计划节点有关。
        {
            if (!_playerCraft.PlayerCraft.ManeuverNodeManager.AnyItemsBeingHoveredWhichPreventManeuverNodeAdder && (double)pointInfo.HoverTime > 0.25)
            {
                OrbitHoveredWithDelay = true;
            }
        }

        public override void Destroy()
        //摧毁GameObj的方法
        {
            base.Destroy();
            CameraFocusableItemDestroyedHandler cameraFocusableDestroyed = _cameraFocusableDestroyed;
            if (cameraFocusableDestroyed != null)
            {
                cameraFocusableDestroyed(this);
            }
            _cameraFocusableDestroyed = null;
        }

        /*
        public static MBGOrbitLine Create(IIocContainer ioc, IMapViewContext mapViewContext, IOrbitNode node, MapItemData data, Color color, string name, Camera mapCamera, Material lineMaterial, bool isSharedMaterial)
        {
            return Create<MBGOrbitLine>(ioc, mapViewContext, node, data, color, name, mapCamera, lineMaterial, isSharedMaterial);
        }
        */

        public static MBGOrbitLine Create(IIocContainer ioc, IMapViewContext mapViewContext, IOrbitNode node, MapItemData data, Color color, string name, Camera mapCamera, Material lineMaterial)
        {
            return Create(ioc, mapViewContext, node, data, color, name, mapCamera, lineMaterial, false);
        }
        /*
        public override void OnAfterCameraPositioned()
        //在摄像机重定向（聚焦视角）到MapItem后（已经聚焦视角）触发的方法
        {
            base.OnAfterCameraPositioned();
            //OrbitUiVerbosity orbitUiVerbosity = _options.OrbitUiVerbosity;
            //bool canShowGlobal = Data.ShowOrbitLine && orbitUiVerbosity != OrbitUiVerbosity.Minimal && !base.OrbitInfo.InContactWithPlanet;
            //this.UpdateIcons(canShowGlobal);
            //this.UpdateText(canShowGlobal);
        }
        */
        public static MBGOrbitLine Create(IIocContainer ioc, IMapViewContext mapViewContext, IOrbitNode node, MapItemData data, Color color, string name, Camera mapCamera, Material lineMaterial, bool isSharedMaterial)
        {
            //Debug.Log("TL0SR2 MBG OrbitLine -- Create ");
            IObjectContainerProvider objectContainerProvider = ioc.Resolve<IObjectContainerProvider>(mapViewContext, false);
            MBGOrbitLine t = MapItem.Create<MBGOrbitLine>(ioc, mapViewContext, node, name, objectContainerProvider.OrbitCanvases, mapCamera, objectContainerProvider.OrbitContainer, null);
            t.name = string.Format("{0}({1})", name, t.GetInstanceID());
            t.Initialize(data, color, lineMaterial, isSharedMaterial);
            return t;
        }

        public override void OnBeforeCameraPositioned()
        //在摄像机重定向（聚焦视角）到MapItem前（已经点击，尚未移动视角）触发的方法
        {
            base.OnBeforeCameraPositioned();
            if (_vectrocityLine != null)
            {
                if (_forceUpdateOrbitLine || base.DrawModeProvider.DrawMode.UpdateReferencePerPoint)
                {
                    _forceUpdateOrbitLine = false;
                    UpdateLine();
                    return;
                }
                RepositionOrbitLine();
            }
        }
        private static Vector4d GetScaledCachePoint(MBGOrbitPoint point, IMapViewCoordinateConverter coordinateConverter)
        //输入轨道点，输出经过地图缩放的坐标点
        {
            Vector3d vector3d = point.State.Position * coordinateConverter.MapScale;
            return new Vector4d(vector3d.x, vector3d.y, vector3d.z, point.Time);
        }
        public void RepositionOrbitLine()
        //设置强制重新绘制的方法
        {
            if (_scaledLocalMBGOrbitPointsCache == null)
            {
                UpdateLine();
                return;
            }
            MBGOrbitLine.RepositionOrbitLine(_scaledLocalMBGOrbitPointsCache, ref _indexOfPrecisePoint, base.DrawModeProvider, MBGOrbitInfo, _vectrocityLine, base.CoordinateConverter);
        }
        public static void RepositionOrbitLine(List<Vector4d> scaledMBGOrbitPointsCache, ref int indexOfPrecisePoint, IDrawModeProvider drawModeProvider, MBGMapOrbitInfo orbitInfo, VectorLine orbitLine, IMapViewCoordinateConverter coordinateConverter)
        //同上
        {
            /*

            int count = orbitLine.points3.Count;
            for (int i = 0; i < count; i++)
            {
                //此处和轨道线绘制的关键节点有关
                MBGOrbitLine.GetPosition(i, scaledMBGOrbitPointsCache, orbitInfo, coordinateConverter, ref indexOfPrecisePoint);
            }
            */
        }

        /*
        private static Vector4d GetPosition(int index, List<Vector4d> scaledOrbitPointsCache, MBGMapOrbitInfo orbitInfo, IMapViewCoordinateConverter coordinateConverter, ref int indexOfPrecisePoint)
        //获取轨道线绘制位置的方法
        {
            Vector4d vector4d = scaledOrbitPointsCache[index];
            double w = vector4d.w;
            if (indexOfPrecisePoint >= 0 && index == indexOfPrecisePoint)
            {
                vector4d = MBGOrbitLine.GetScaledCachePoint(OrbitMath.GetPointAtTrueAnomaly(orbitInfo.OrbitNode.Orbit, orbitInfo.OrbitNode.Orbit.TrueAnomaly), coordinateConverter);
                w = vector4d.w;
                int num = index + 1;
                if (num >= scaledOrbitPointsCache.Count)
                {
                    num = 0;
                }
                Vector3d vector4d2 = scaledOrbitPointsCache[num];
                //double w2 = vector4d2.w;
                if (!OrbitMath.TrueAnomalyBetween(w, lastNu, w2, true) || (lastNu == w2 && lastNu < w))
                {
                    scaledOrbitPointsCache.RemoveAt(index);
                    indexOfPrecisePoint = index + 1;
                    if (indexOfPrecisePoint >= scaledOrbitPointsCache.Count - 1)
                    {
                        indexOfPrecisePoint = 1;
                    }
                    scaledOrbitPointsCache.Insert(indexOfPrecisePoint, vector4d);
                    vector4d = vector4d2;
                    w = vector4d.w;
                }
            }
            //lastNu = w;
            return vector4d;
        }
        */

        protected void Initialize(MapItemData data, Color color, Material lineMaterial, bool isSharedMaterial)
        {
            //base.Initialize(data, color, lineMaterial, isSharedMaterial);
            Debug.Log("TL0SR2 MBG OrbitLine -- Initialize ");
            Data = data;
            Id = gameObject.GetInstanceID();
            //base.OrbitInfo.SetOrbitLine(this);
            IIocContainer ioc = base.Ioc;
            IMapViewContext mapViewContext = base.MapViewContext;
            _lineManager = ioc.Resolve<IOrbitLineManager>(mapViewContext, false);
            _options = ioc.Resolve<IMapOptions>(false);
            _cameraTarget = ioc.Resolve<ICurrentCameraTarget>(mapViewContext, false);
            _navigationTargetProvider = ioc.Resolve<INavigationTargetProvider>(mapViewContext, false);
            _playerCraft = ioc.Resolve<IPlayerCraftProvider>(mapViewContext, false);
            IMapView mapView = ioc.Resolve<IMapView>(mapViewContext, false);
            _isSharedMaterial = isSharedMaterial;
            _lineMaterial = lineMaterial;
            base.Color = color;
            base.Selectable = true;
            //Debug.Log("TL0SR2 MBG OrbitLine -- Initialize Log 1");
            Vector2 value = new Vector2(0.5f, 0f);
            //Debug.Log("TL0SR2 MBG OrbitLine -- Initialize Log 2");
            MBGOrbitInfo = new MBGMapOrbitInfo(Ioc, mapViewContext, null, Camera, this, OrbitInfo.OrbitNode as CraftNode);
            //Debug.Log("TL0SR2 MBG OrbitLine -- Initialize Log 3");
            MBGOrbitLine.ChangeReferencePlanetEvent += (sender, name) => this.ChangeReferencePlanet(name);
            //this._apoapsisIcon = UiUtils.CreateUiIcon(base.InfoCanvas, "Apoapsis", false, new Vector2?(value));
            //this._periapsisIcon = UiUtils.CreateUiIcon(base.InfoCanvas, "Periapsis", false, new Vector2?(value));
            //this._ascendingNodeIcon = UiUtils.CreateUiIcon(base.InfoCanvas, "AscendingNode", false, new Vector2?(value));
            //this._descendingNodeIcon = UiUtils.CreateUiIcon(base.InfoCanvas, "DescendingNode", false, new Vector2?(value));
            //this._targetAscendingNodeIcon = UiUtils.CreateUiIcon(base.InfoCanvas, "AscendingNodeOfTarget", false, new Vector2?(value));
            //this._targetDescendingNodeIcon = UiUtils.CreateUiIcon(base.InfoCanvas, "DescendingNodeOfTarget", false, new Vector2?(value));
            //this._planetIntersectionIcon = UiUtils.CreateUiIcon(base.InfoCanvas, "PlanetIntersection", false, null);
            //this._apoDistanceText = UiUtils.CreateUiText(base.InfoCanvas.transform, "ApoDist", false, TextAlignmentOptions.Bottom);
            //this._periDistanceText = UiUtils.CreateUiText(base.InfoCanvas.transform, "PeriDist", false, TextAlignmentOptions.Bottom);
            /*
			if (Game.InPlanetStudioScene)
            {
                //this._invalidOrbitIcon = UiUtils.CreateUiIcon(base.InfoCanvas, "PlanetIconAlternative", false, null);
                //this._invalidOrbitIcon.color = new Color(0.8f, 0.1f, 0.1f);
                //this._sphereOfInfluence = MapUtils.CreateSoiSphere(base.OrbitInfo.OrbitNode as PlanetNode, base.ItemName, base.gameObject.layer, base.transform, base.CoordinateConverter);
            }
            */
            //Debug.Log("TL0SR2 MBG OrbitLine -- Initialize Log 4");
            _MBGOrbitPointSet = new MBGOrbitPointSet();
            _vectrocityLine = MBGOrbitLine.CreateLine(base.transform, base.Color, base.name, base.gameObject.layer);
            bool isDrawing = mapView.Visible && Data.ShowOrbitLine;
            SetIsDrawing(isDrawing);
            UpdateEventSubscriptions(true);
            //Debug.Log("TL0SR2 MBG OrbitLine -- Initialize Log 5");
            //this.PointCount = MBGOrbitLine.CalculatePointsCount();
        }


        private static VectorLine CreateLine(Transform parent, Color color, string name, int layer)
        //创建轨迹线
        {
            //Debug.Log("TL0SR2 MBG OrbitLine -- CreateLine ");
            List<Vector3> points = new List<Vector3>(150);
            VectorLine vectorLine = new VectorLine($"MBGOrbitLine({name})", points, 2f, LineType.Continuous);
            vectorLine.color = color;
            vectorLine.layer = layer;
            vectorLine.rectTransform.SetParent(parent);
            return vectorLine;
        }

        public void UpdateLine()
        //命令更新轨迹线
        {
            UpdateLine(false);
        }


        private void UpdateLine(bool forceUpdate)
        //同上
        {
            if (Data.ShowOrbitLine || forceUpdate)
            {
                MBGOrbitLine.UpdateLine(this, base.DrawModeProvider, ref _MBGOrbitPointSet, base.CoordinateConverter, ref _scaledLocalMBGOrbitPointsCache, ref _orbitLineRenderer);
            }
        }

        public static void UpdateLine(MBGOrbitLine orbitLine, IDrawModeProvider drawModeProvider, ref MBGOrbitPointSet MBGOrbitPointSet, IMapViewCoordinateConverter coordinateConverter, ref List<Vector4d> scaledPointsCache, ref Renderer lineRenderer)
        //更新轨道线的核心方法
        {

            //Debug.Log("TL0SR2 MBG OrbitLine -- UpdateLine ");
            orbitLine.MBGOrbit = MBGOrbit.GetMBGOrbit(orbitLine.OrbitInfo.OrbitNode as CraftNode);
            orbitLine.MBGOrbitInfo.SetOrbit(orbitLine.MBGOrbit);
            /*
            if (orbitLine.MBGOrbit == null)

            {
                //Debug.LogError("TL0SR2 MBG OrbitLine -- UpdateLine Log -- MBGOrbit Set Failed");
            }
            else
            {
                Debug.Log("TL0SR2 MBG OrbitLine -- UpdateLine Log -- MBGOrbit Set Successful");
            }
            */
            MBGMapOrbitInfo orbitInfo = orbitLine.MBGOrbitInfo;
            //IOrbitNode orbitNode = orbitInfo.OrbitNode;
            MBGOrbit orbit = orbitInfo.MBGOrbit;
            IDrawMode drawMode = drawModeProvider.DrawMode;
            VectorLine vectrocityLine = orbitLine._vectrocityLine;
            //double endNu;
            //MBGOrbitLine.UpdateGetPointsParams(drawModeProvider, orbitInfo, orbitLine.DrawFullOrbit, out endNu);
            //double validTrueAnomalyStart = orbitInfo.ValidTrueAnomalyStart;
            MBGOrbitPointSet = orbit.GetMBGOrbitPointSet();
            if (MBGOrbitPointSet.Count > 0)
            {
                //MBGOrbitPoint point = MBGOrbitPointSet.GetPoint(0);
                //MBGOrbitPoint MBGOrbitPoint = MBGOrbitPointSet.Last(0);
                /*
                if (MBGOrbitPointSet.Closed)
                {
                    MBGOrbitPointSet.AddPoint(point);
                }
                */
                //orbitInfo.SetPlanetIntersection(MBGOrbitPointSet.IntersectsPlanet ? MBGOrbitPointSet.Last(0) : null);
                //int num = (drawMode.UpdateReferencePerPoint || !MapUtils.SamePlanet(orbitInfo.OrbitNode.Parent, drawMode.GetReferenceNode(orbitInfo))) ? (MBGOrbitPointSet.Count - 5) : MBGOrbitPointSet.Count;
                int num = MBGOrbitPointSet.Count;
                vectrocityLine.points3.Clear();
                /*
                vectrocityLine.Uv2.Clear();
                */
                //scaledPointsCache = new List<Vector4d>();
                /*
                if (!drawMode.UpdateReferencePerPoint)
                {
                    drawMode.UpdateReferenceNoderPerOrbit(ref drawModeReferenceInfo, orbitInfo);
                    scaledPointsCache.Clear();
                }
                */
                /*
                IChainableOrbit chainNode = orbitInfo.ChainNode;
                MBGMapOrbitInfo MBGMapOrbitInfo = (chainNode != null) ? chainNode.ListNode.List.First.Value.OrbitInfo : null;
                IChainableOrbit chainNode2 = orbitInfo.ChainNode;
                MBGMapOrbitInfo MBGMapOrbitInfo2 = (chainNode2 != null) ? chainNode2.ListNode.List.Last.Value.OrbitInfo : null;
                double num2 = (MBGMapOrbitInfo != null) ? MBGMapOrbitInfo.StartTime : point.Time;
                double timeSpan = MBGOrbitPointSet.TotalTime;
                bool flag = orbitInfo.ChainNode != null;
                */
                //MBGOrbit orbit2 = orbitLine.OrbitInfo.mbgOrbit;
                //double nuStart = 0.0;
                //orbitLine._indexOfPrecisePoint = -1;
                for (int i = 0; i < num; i++)
                {
                    MBGOrbitPoint point = MBGOrbitPointSet.GetPoint(i);
                    if (point.Time >= MBGOrbit.CurrentTime - 10)
                    {
                        /*
                        if (orbitLine._indexOfPrecisePoint < 0) //&& OrbitMath.TrueAnomalyBetween(orbit2.TrueAnomaly, nuStart, point2.TrueAnomaly, true))
                        {
                            drawModeReferenceInfo = MBGOrbitLine.AddOrbitLinePoint(drawModeProvider, drawModeReferenceInfo, coordinateConverter, scaledPointsCache, orbitInfo, drawMode, vectrocityLine, num2, timeSpan, flag, OrbitMath.GetPointAtTrueAnomaly(orbit2, orbit2.TrueAnomaly), num);
                            orbitLine._indexOfPrecisePoint = i;
                        }
                        drawModeReferenceInfo = MBGOrbitLine.AddOrbitLinePoint(drawModeProvider, drawModeReferenceInfo, coordinateConverter, scaledPointsCache, orbitInfo, drawMode, vectrocityLine, num2, timeSpan, flag, point2, num);
                        //nuStart = point2.TrueAnomaly;
                        */
                        if (orbitLine._currentplanet == null)
                        {
                            orbitLine.ChangeReferencePlanet(MBGOrbit.SunNode);
                        }
                        vectrocityLine.points3.Add((Vector3)coordinateConverter.ConvertSolarToMapView(orbitLine.GetPointSolarPosition(point)));
                        //scaledPointsCache.Add(GetScaledCachePoint(point,coordinateConverter));
                    }
                }
                /*
                if (flag)
                {
                    vectrocityLine.Uv2.RemoveAt(vectrocityLine.Uv2.Count - 1);
                    vectrocityLine.SetUvs(vectrocityLine.Uv2);
                }
                */
                vectrocityLine.Draw3DAuto();
                lineRenderer = vectrocityLine.rectTransform.GetComponent<Renderer>();
                lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;
                if (orbitLine._lineMaterial != null)
                {
                    lineRenderer.sharedMaterial = orbitLine._lineMaterial;
                }
                //orbitLine.OnLineCreated(vectrocityLine);
                vectrocityLine.rectTransform.SetParent(orbitLine.transform);
                bool isAutoDrawing = vectrocityLine.isAutoDrawing;
                return;
            }
            vectrocityLine.points3.Clear();
            //vectrocityLine.Uv2.Clear();
        }

        protected override void OnDestroy()
        //GameObj被摧毁时触发的方法
        {
            base.OnDestroy();
            CameraFocusableItemDestroyedHandler cameraFocusableDestroyed = _cameraFocusableDestroyed;
            if (cameraFocusableDestroyed != null)
            {
                cameraFocusableDestroyed(this);
            }
            _cameraFocusableDestroyed = null;
            if (_vectrocityLine != null)
            {
                VectorLine.Destroy(ref _vectrocityLine);
            }
            UpdateEventSubscriptions(false);
            if (_lineMaterial != null)
            {
                if (!_isSharedMaterial)
                {
                    UnityEngine.Object.Destroy(_lineMaterial);
                }
                _lineMaterial = null;
            }
        }
        private void UpdateEventSubscriptions(bool subscribe)
        //管理事件订阅的方法
        {
            if (subscribe)
            {
                Data.ShowOrbitLineChanged += OnDataShowOrbitLineChanged;
                return;
            }
            Data.ShowOrbitLineChanged -= OnDataShowOrbitLineChanged;
            /*
            if (this._maneuverNodeEventsProvider != null)
            {
                this._maneuverNodeEventsProvider.ManeuverNodeAdjustmentChangingEvent -= this.OnManeuverNodeAdjustmentChanging;
                this._maneuverNodeEventsProvider.ManeuverNodeAdjustmentChangeEndEvent -= this.OnManeuverNodeAdjustmentChangeEnd;
                this._maneuverNodeEventsProvider.ManeuverNodeAdjustmentChangeBeginEvent -= this.OnManeuverNodeAdjustmentChangeBegin;
            }
            */
        }
        private void OnDataShowOrbitLineChanged(bool shouldDraw)
        //当轨道线显示改变的时候会触发的方法
        {
            SetIsDrawing(shouldDraw);
        }
        protected override void OnDisable()
        //GameObj被Disable时触发的方法
        {
            base.OnDisable();
            SetIsDrawing(false);
        }

        protected override void OnEnable()
        //GameObj被Enable时触发的方法
        {
            base.OnEnable();
            if (Data != null && Data.ShowOrbitLine)
            {
                SetIsDrawing(true);
            }
        }
        private void SetIsDrawing(bool drawing)
        //指定重绘的方法，大概
        {
            //Debug.Log($"TL0SR2 MBG OrbitLine -- SetIsDrawing {drawing} ");
            if (drawing != IsDrawing)
            {
                if (drawing)
                {
                    UpdateLine(false);
                    return;
                }
                _vectrocityLine.points3.Clear();
            }
        }
        internal void SetColor(Color color)
        {
            base.Color = color;
            if (IsValidRendering)
            {
                _vectrocityLine.color = color;
            }
        }
        internal void Disable()
        //Disable当前的GameObj
        {
            gameObject.SetActive(false);
        }

        internal void Enable()
        //Enable当前的GameObj
        {
            gameObject.SetActive(true);
        }
        public void ForceUpdate()
        //一个设置轨道线在下一帧强制刷新的方法
        {
            _forceUpdateOrbitLine = true;
        }

        public void ChangeReferencePlanet(string name)
        {
            IPlanetNode planet = MBGOrbit.SunNode.FindPlanet(name);
            if (planet != null)
            {
                _currentplanet = planet;
            }
        }
        public void ChangeReferencePlanet(IPlanetNode planet)
        {
            if (planet != null)
            {
                _currentplanet = planet;
                UpdateLine();
            }
        }

        public static void MBGOrbitLineChangeReference(string name)
        {
            MBGOrbitLine.ChangeReferencePlanetEvent?.Invoke(null, name);
        }

        public Vector3d GetPointSolarPosition(MBGOrbitPoint point)
        {
            Vector3d RelativePosition = point.State.Position - this._currentplanet.GetSolarPositionAtTime(point.Time);
            if (RotateReference == RotateMode.None || (_currentplanet.Parent == null && RotateReference == RotateMode.Revolution))
            {
                //如果不启用旋转追随，或者启用模式为公转追随而且当前行星是系统的恒星（此时公转追随没有意义），那么直接平移坐标即可
                return RelativePosition + this._currentplanet.SolarPosition;
            }
            if (RotateReference == RotateMode.Rotation)
            {
                //如果启用自转追随，那么将相对位置乘上一个旋转矩阵，其旋转角度是初始旋转角度+当前行星的自旋速度乘时间
                double rotateAngle = _currentplanet.PlanetData.AngularVelocity * 57.29578 * point.Time + InitRotateAngle;
                RelativePosition = Quaterniond.Euler(0.0, rotateAngle, 0.0) * RelativePosition;
                return RelativePosition + this._currentplanet.SolarPosition;
            }
            //以上判断排除其他情况之后，剩下在这里计算的是旋转追随模式设置为公转追随并且不是恒星的星球
            Quaternion BasicQ = Quaternion.LookRotation((Vector3)(_currentplanet.GetSolarPositionAtTime(0) - _currentplanet.Parent.GetSolarPositionAtTime(0)));//基准母-子向量
            Quaternion RealQ = Quaternion.LookRotation((Vector3)(_currentplanet.GetSolarPositionAtTime(point.Time) - _currentplanet.Parent.GetSolarPositionAtTime(point.Time)));//时间T时刻的母-子向量
            //只需要把相对位置向量按照相同的旋转角度旋转即可(附加一个初始角度，大概喵)
            float DeltaAngle = Quaternion.Angle(BasicQ, RealQ);//两个旋转之间的角度，单位为角度制
            Quaterniond RotateQ = Quaterniond.FromQuaternion(Quaternion.SlerpUnclamped(BasicQ, RealQ, 1 + (float)InitRotateAngle / DeltaAngle));
            RelativePosition = RotateQ * RelativePosition;
            return RelativePosition + this._currentplanet.SolarPosition;
        }

        public static void SetReferenceMode(int index)
        {
            if (index >= 0 && index <= 2)
            {
                RotateReference = (RotateMode)index;
            }
            else
            {
                Debug.LogWarning("TL0SR2 MBG Orbit Line -- SetReferenceMode -- Invalid Mode Index.");
            }
        }
        public static void SetRotateInitAngle(double degree)
        {
            InitRotateAngle = degree % 360;
        }

        public static RotateMode RotateReference = RotateMode.None;
        //这个值指示轨迹线绘制时是否采用旋转模式

        public static double InitRotateAngle = 0;
        //这个值指示如果采用旋转模式，那么初始旋转角（相对于母-子连线，俯视（从北方看）逆时针旋转）是多少，单位为角度制

        private bool _forceUpdateOrbitLine;
        //一个变量，用来指定轨道线强制重绘
        private List<Vector4d> _scaledLocalMBGOrbitPointsCache;
        //一个列表，管理被地图视角倍率放大之后的点的坐标,其中坐标前三个分量是位置，第四个分量是时间
        private bool _isSharedMaterial;
        //判断是否为共享材质……大概吧喵
        public bool IsValidRendering = true;
        //判断是否有效渲染
        private VectorLine _vectrocityLine;
        //最核心的变量，即利用VectorLine绘制出的轨道线_vectrocityLine
        //核心代码目的即为正确绘制此VectorLine


        private INavigationTargetProvider _navigationTargetProvider;
        //大概和

        private ICurrentCameraTarget _cameraTarget;
        //管理摄像机视角中心的数据，大概喵


        private IMapOptions _options;
        //大概是某种地图选项喵……

        private CameraFocusableItemDestroyedHandler _cameraFocusableDestroyed;
        //大概是和cameraFocusable对象的销毁相关的数据喵……

        private IOrbitLineManager _lineManager;
        //轨道线管理器，大概管理轨道线绘制的一些设置喵

        private MBGOrbit MBGOrbit;
        //……就是自定义的轨道喵……

        private Material _lineMaterial;
        //轨道线绘制使用的材质喵

        private IPlanetNode _currentplanet = MBGOrbit.SunNode;
        //当前的视觉参考系中心喵
        //private DrawModeReferenceInfo _drawModeReferenceInfo;

        public int Id { get; private set; }
        //GameObj的ID

        private IManeuverNodeEventsProvider _maneuverNodeEventsProvider;
        //大概和轨道点火推进节点有关喵
        private IPlayerCraftProvider _playerCraft;
        //大概是这条轨道线对应的飞行器相关的数据

        private int _indexOfPrecisePoint;
        //大概与这条轨道上的精确散点数目有关。可能会用得上喵？

        public MBGOrbitPointSet _MBGOrbitPointSet;
        //轨道位置的散点集
        public MBGMapOrbitInfo MBGOrbitInfo;

        int PointDensity = 1;
        //控制轨道ui显示的打点密度与输入的散点集密度的倍率的值
        //暂不使用，大概
        Renderer _orbitLineRenderer;
        //y用于渲染轨道线的渲染器

        public enum RotateMode
        {
            None = 0,
            //不启用旋转模式
            Rotation = 1,
            //自转追随模式
            Revolution = 2
            //公转追随模式
        }
    }
}