using System;
using Assets.Scripts.Flight.MapView.Items;
using Assets.Scripts.Flight.MapView.UI;
using ModApi;
using ModApi.Common.Extensions;
using ModApi.Common.UI;
using ModApi.Flight.MapView;
using ModApi.Flight.Sim;
using ModApi.Math;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Flight.Sim.MBG
{
    //当前主要目标：把球显示出来（
    public class MBGManeuverNodeScript : MapItem, ICameraFocusable
    {
        public event CameraFocusableItemDestroyedHandler Destroyed
        {
            add
            {
                ((ICameraFocusable)_orbitLine).Destroyed += value;
            }

            remove
            {
                ((ICameraFocusable)_orbitLine).Destroyed -= value;
            }
        }

        public static MBGManeuverNodeScript Create(Canvas canvas,Transform parent, MBGOrbitLine orbitLine, MBGOrbitPoint point, Action<MBGManeuverNode> action)
        {
            MBGManeuverNodeScript maneuverNodeScript = new GameObject().AddComponent<MBGManeuverNodeScript>();
            //MBGManeuverNodeScript maneuverNodeScript = MapItem.Create<MBGManeuverNodeScript>(orbitLine.Ioc,orbitLine.MapViewContext,)
            maneuverNodeScript.ConfirmBurn = action;
            maneuverNodeScript.name = "MBGBurnNode";
            maneuverNodeScript.transform.SetParent(parent);
            maneuverNodeScript.transform.localScale = new Vector3(1, 1, 1);
            maneuverNodeScript.Initialize(orbitLine, point,canvas);
            maneuverNodeScript.maneuverNode = new MBGManeuverNode(orbitLine, point, new Vector3d());

            return maneuverNodeScript;
        }

        private void Initialize(MBGOrbitLine orbitLine, MBGOrbitPoint point,Canvas canvas)
        {
            this._orbitLine = orbitLine;
            this._camera = orbitLine.Camera;
            this._point = point;
            this._infoCanvas = canvas;
            this.UpdateManeuverVectors();
            this.InitializeUi();
            MBGPatch_MapCraft.OnAfterCameraPositionedEvent += sender => this.OnAfterCameraPositioned();
            InitComplete = true;
        }


        private void UpdateManeuverVectors()
        {
            this._progradeVec = this._point.State.Velocity.normalized;
            this._radialVec = -MBGOrbit.CalculateGravityAtTime(this._point.State.Position, this._point.Time).normalized;
            this._normalVec = Vector3d.Cross(this._progradeVec, this._radialVec).normalized;
            //u3d中的向量叉乘是左手系喵
        }

        private void InitializeUi()
        {
            /*
            this._nodeAdderGraphicContainer = new GameObject("GraphicContainer");
            this._nodeAdderGraphicContainer.transform.SetParent(NodeAdder.transform);
            this._nodeAdderGraphicContainer.layer = this.gameObject.layer;
            this._addNodeIcon = new GameObject("AddIcon").AddComponent<Image>();
            this._addNodeIcon.sprite = UiUtils.LoadIconSprite("Add");
            this._addNodeIcon.raycastTarget = true;
            this._addNodeIcon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 20f);
            this._addNodeIcon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 20f);
            this._addNodeIcon.transform.SetParent(this._nodeAdderGraphicContainer.transform);
            this._addNodeIcon.gameObject.layer = this.gameObject.layer;
            this._addNodeIcon.enabled = false;
            */

            /*
            GameObject gameObject = new GameObject("infoCanvas");
            this._infoCanvas = gameObject.AddComponent<Canvas>();
            this._infoCanvas.gameObject.layer = this._orbitLine.gameObject.layer;
            this._infoCanvas.transform.SetParent(this._orbitLine.transform);
            this._infoCanvas.gameObject.AddComponent<GraphicRaycaster>();
            this._infoCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            this._infoCanvas.worldCamera = this._orbitLine.Camera;
            this._infoCanvas.overrideSorting = true;
            this._infoCanvas.sortingOrder = -5;
            this._infoCanvas.gameObject.AddMissingComponent<OverrideSortingOnStart>();
            Utilities.FixUnityCanvasSortingBug(_infoCanvas);
            */
            this._maneuverNodeAdjustorContainer = new GameObject("BurnNodeAdjustorContainer").transform;
            this._maneuverNodeAdjustorContainer.SetParent(this._infoCanvas.transform);
            this._maneuverNodeAdjustorContainer.localScale = Vector3.one;
            this._maneuverNodeAdjustorContainer.gameObject.layer = gameObject.layer;


            Color.RGBToHSV(new Color(0.96f, 0.36f, 0.42f), out float h, out float num, out float v);
            Color color = Color.HSVToRGB(h, num * 0.6f, v);
            Color.RGBToHSV(new Color(0.01f, 0.9f, 0.25f), out h, out num, out v);
            Color color2 = Color.HSVToRGB(h, num * 0.6f, v);
            Color.RGBToHSV(new Color(0.28f, 0.38f, 0.91f), out h, out num, out v);
            Color color3 = Color.HSVToRGB(h, num * 0.6f, v);
            this._maneuverNodeAdjustors[0] = this.CreateAdjustor(() => this._progradeVec, "Prograde", color2, true, null);
            this._maneuverNodeAdjustors[1] = this.CreateAdjustor(() => -this._progradeVec, "Retrograde", color2, true, null);
            this._maneuverNodeAdjustors[2] = this.CreateAdjustor(() => this._radialVec, "Radial-out", color3, true, null);
            this._maneuverNodeAdjustors[3] = this.CreateAdjustor(() => -this._radialVec, "Radial-in", color3, true, null);
            this._maneuverNodeAdjustors[4] = this.CreateAdjustor(() => this._normalVec, "Normal", color, true, null);
            this._maneuverNodeAdjustors[5] = this.CreateAdjustor(() => -this._normalVec, "Anti-normal", color, true, null);


            GameObject gameObject2 = new GameObject("BurnNodeSelection");
            gameObject2.transform.SetParent(this._infoCanvas.transform);
            this._selectNodeIcon = gameObject2.AddComponent<Image>();
            this._selectNodeIcon.gameObject.layer = this._infoCanvas.gameObject.layer;
            this._selectNodeIcon.transform.localScale = Vector3.one;
            this._selectNodeIcon.sprite = UiUtils.LoadIconSprite("Sphere");
            this._selectNodeIcon.color = new Color(0, 1, 1, 1f);
            //this._selectNodeIcon.rectTransform.sizeDelta = new Vector2(20f, 20f);
            this._selectNodeIcon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 20f);
            this._selectNodeIcon.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 20f);
            this._selectNodeIcon.enabled = true;
            GameObject gameObject3 = new GameObject("BurnLocked");
            gameObject3.transform.SetParent(this._infoCanvas.transform);
            this._lockedNodeIcon = gameObject3.AddComponent<Image>();
            this._lockedNodeIcon.gameObject.layer = this._infoCanvas.gameObject.layer;
            this._lockedNodeIcon.transform.localScale = Vector3.one;
            this._lockedNodeIcon.sprite = UiUtils.LoadIconSprite("ManeuverLocked");
            this._lockedNodeIcon.rectTransform.sizeDelta = new Vector2(25f, 25f);
            this._lockedNodeIcon.enabled = false;
            this._selectNodeIconSize = new Vector2(this._selectNodeIcon.rectTransform.sizeDelta.x * this._selectNodeIcon.transform.localScale.x, this._selectNodeIcon.rectTransform.sizeDelta.y * this._selectNodeIcon.transform.localScale.y);
            GameObject gameObject4 = new GameObject("BurnNodeDeletion");
            gameObject4.transform.SetParent(this._infoCanvas.transform);
            this._deleteNodeIcon = gameObject4.AddComponent<Image>();
            this._deleteNodeIcon.gameObject.layer = this._infoCanvas.gameObject.layer;
            this._deleteNodeIcon.transform.localScale = Vector3.one;
            this._deleteNodeIcon.sprite = UiUtils.LoadIconSprite("Delete");
            this._deleteNodeIcon.rectTransform.sizeDelta = new Vector2(15f, 15f);
            this._deleteNodeIcon.enabled = false;
        }
        private void SetGizmoState(GizmoState state)
        {
            if (state == GizmoState.Retracted)
            {
                for (int i = 0; i < this._maneuverNodeAdjustors.Length; i++)
                {
                    this._maneuverNodeAdjustors[i].OnDeselected();
                }
            }
            else if (state == GizmoState.Extended)
            {
                if (!Locked)
                {
                    for (int j = 0; j < this._maneuverNodeAdjustors.Length; j++)
                    {
                        this._maneuverNodeAdjustors[j].OnSelected();
                    }
                }
            }
            else
            {
                Debug.Log(string.Format("Unsupported gizmo state {0}", state));
            }
            this._gizmoState = state;
        }
        /*
        private void Update()
        {
            
        }
        */

        public override void OnAfterCameraPositioned()
        {
            if (this.InitComplete)
            {
                //Debug.Log("TL0SR2 -- MBG Maneuver Node Script -- OnAfterCameraPositioned Log 1");
                //base.OnAfterCameraPositioned();
                //Debug.Log("TL0SR2 -- MBG Maneuver Node Script -- OnAfterCameraPositioned Log 2");
                this.UpdateManeuverVectors();
                //Debug.Log("TL0SR2 -- MBG Maneuver Node Script -- OnAfterCameraPositioned Log 3");
                this.UpdatePositions();
                //Debug.Log("TL0SR2 -- MBG Maneuver Node Script -- OnAfterCameraPositioned Log 4");
                this.UpdateUI();
                //Debug.Log("TL0SR2 -- MBG Maneuver Node Script -- OnAfterCameraPositioned Log 5");
            }
        }
        private MBGNodeDeltaVAdjustorScript CreateAdjustor(Func<Vector3d> maneuverVec, string iconName, Color color, bool subscribeToEvents = true, string name = null)
		{
			MBGNodeDeltaVAdjustorScript nodeDeltaVAdjustorScript = MBGNodeDeltaVAdjustorScript.Create(this._infoCanvas, this._maneuverNodeAdjustorContainer, maneuverVec, this, string.IsNullOrEmpty(name) ? iconName : name, iconName, color);
			if (subscribeToEvents)
			{
				nodeDeltaVAdjustorScript.ManeuverNodeAdjustmentChangeBeginEvent += this.OnAdjustorChangeBegin;
				nodeDeltaVAdjustorScript.ManeuverNodeAdjustmentChangingEvent += this.OnAdjustorChanging;
				nodeDeltaVAdjustorScript.ManeuverNodeAdjustmentChangeEndEvent += this.OnAdjustorChangeEnd;
			}
			return nodeDeltaVAdjustorScript;
		}
		private void OnAdjustorChangeBegin(MBGNodeDeltaVAdjustorScript source)
		{
			this._maneuverNodeAdjustorBeingDragged = source;
			this._dvChanged = true;
			this._dvChangeBegin = true;
		}

        private void OnAdjustorChangeEnd(MBGNodeDeltaVAdjustorScript source)
        {
            this._maneuverNodeAdjustorBeingDragged = null;
            this._dvChanged = true;
            this._dvChangeEnd = true;
            this.maneuverNode.DeltaV = this._deltaV;
            this.ConfirmBurn?.Invoke(this.maneuverNode);
		}
        private Vector3d CalculateDeltaV()
        {
            Vector3d vector3d = Vector3d.zero;
            for (int i = 0; i < this._maneuverNodeAdjustors.Length; i++)
            {
                vector3d += this._maneuverNodeAdjustors[i].DeltaV;
            }
            return vector3d;
        }


		private void OnAdjustorChanging(MBGNodeDeltaVAdjustorScript source)
		{
			this._dvChanged = true;
			this._dvChanging = true;
			this.SetDeltaV(this.CalculateDeltaV(), false);
			this.UpdateDeltaVAxisContributions();
			this.ActivateAutolockCooldown();
			Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format("Delta V: {0}", Units.GetVelocityString((float)this.DeltaVMag, Units.UnitPrecisionMode.High)), false, 3f);
		}
        private void ActivateAutolockCooldown()
        {
            this._nextAutoLockAvailability = Time.time + 5f;
        }
        private void SetDeltaV(Vector3d deltaV, bool updateAdjustors)
        {
            if (updateAdjustors)
            {
                for (int i = 0; i < this._maneuverNodeAdjustors.Length; i++)
                {
                    this._maneuverNodeAdjustors[i].SetDeltaV(Vector3.zero);
                }
                this._maneuverNodeAdjustors[0].SetDeltaV(deltaV);
            }
            this._prevDeltaV = this._deltaV;
            this._deltaV = deltaV;
            this.DeltaVMag = deltaV.magnitude;
            this.UpdateDeltaVAxisContributions();
            //this.UpdateBurnInfo();
            return;
        }
		private void UpdateDeltaVAxisContributions()
		{
			Vector3d deltaV = this._deltaV;
			this.DeltaVPrograde = Vector3d.Dot(deltaV, this._progradeVec);
			this.DeltaVRadial = Vector3d.Dot(deltaV, this._radialVec);
			this.DeltaVNormal = Vector3d.Dot(deltaV, this._normalVec);
		}
        private void UpdatePositions()
        {
            Vector3d solarPositionAtCurrent = this._orbitLine.GetPointSolarPosition(this._point);
            Vector3d nodeWorldPosition = this._orbitLine.CoordinateConverter.ConvertSolarToMapView(solarPositionAtCurrent);
            this._nodeWorldPosition = nodeWorldPosition;
            if (this._infoCanvas.worldCamera != null)
            {
                //this._nodeScreenPosition = Utilities.GameWorldToScreenPoint(this._infoCanvas.worldCamera, (Vector3)this._nodeWorldPosition);
                this._nodeScreenPosition = (Vector3)this._nodeWorldPosition;
                this._cameraDistance = Vector3d.Distance(this._nodeWorldPosition, this._infoCanvas.worldCamera.transform.position);
            }
        }

        private void UpdateUI()
        {
            if (!this._orbitLine.Data.ShowOrbitLine)
            {
                //Debug.Log("TL0SR2 MBG Maneuver Node Script -- Update UI -- Log A");
                this._lockedNodeIcon.enabled = false;
                this._selectNodeIcon.enabled = false;
                this._deleteNodeIcon.enabled = false;
                return;
            }
            if (this._nodeScreenPosition.z <= 0f)
            {
                //Debug.Log("TL0SR2 MBG Maneuver Node Script -- Update UI -- Log B");
                this._maneuverNodeAdjustorContainer.gameObject.SetActive(false);
                //this.CompleteGizmoAnimations();
                this._lockedNodeIcon.enabled = false;
                this._selectNodeIcon.enabled = false;
                this._deleteNodeIcon.enabled = false;
                return;
            }
                //Debug.Log("TL0SR2 MBG Maneuver Node Script -- Update UI -- Log C");
            this._maneuverNodeAdjustorContainer.gameObject.SetActive(true);
            this._selectNodeIcon.transform.position = this._nodeScreenPosition;
            this._selectNodeIcon.transform.rotation = Quaternion.LookRotation(this._nodeScreenPosition - this._infoCanvas.worldCamera.transform.position);
            this._lockedNodeIcon.transform.position = this._selectNodeIcon.transform.position;
            double d = Mathd.Tan(0.01745329 * (double)(4 * (Game.Instance.Device.IsMobileBuild ? 3 : 2))) * this._cameraDistance * (double)Game.UiScale;
            Vector3d a = (this._camera.transform.up + this._camera.transform.right).normalized;
            Vector3d vector3d = this._nodeWorldPosition + a * d;
            if (this._infoCanvas.isActiveAndEnabled)
            {
                this._deleteNodeIcon.transform.position = Utilities.GameWorldToScreenPoint(this._infoCanvas.worldCamera, (Vector3)vector3d);
            }
            this._selectNodeIcon.enabled = true;
			this._lockedNodeIcon.enabled = this.Locked;
			this._deleteNodeIcon.enabled = true;
        }
        
        public void CompleteGizmoAnimations()
        {
            this._movementAidGizmo.CompletePendingAnimations();
            MBGNodeDeltaVAdjustorScript[] maneuverNodeAdjustors = this._maneuverNodeAdjustors;
            for (int i = 0; i < maneuverNodeAdjustors.Length; i++)
            {
                maneuverNodeAdjustors[i].CompletePendingAnimations();
            }
        }
        
        
		private void SetMoveAidVisible(bool visible)
        {
            if (visible)
            {
                if (this._gizmoState != GizmoState.Retracted)
                {
                    this.SetGizmoState(GizmoState.Retracted);
                }
                this._movementAidGizmo.OnSelected();
                return;
            }
            this._movementAidGizmo.OnDeselected();
            this.SetGizmoState(GizmoState.Extended);
        }
        
        private MBGNodeDeltaVAdjustorScript _movementAidGizmo;
        //当前正在操作的节点

        private Camera _camera;
        private double _cameraDistance;
        private Vector3d _nodeWorldPosition;
        private Transform _maneuverNodeAdjustorContainer;
        private Canvas _infoCanvas;

        private MBGOrbitLine _orbitLine;
        public MBGOrbitPoint _point { get; private set; }

        private Image _lockedNodeIcon;
        private Image _selectNodeIcon;
        private Image _deleteNodeIcon;
        private Vector2 _selectNodeIconSize;
        private Vector3 _nodeScreenPosition;
        private Vector3d _progradeVec;
        //沿速度方向的矢量
        private Vector3d _radialVec;
        //沿法线方向向外的矢量
        private Vector3d _normalVec;
        //沿垂直方向向上的矢量
		public double DeltaVNormal { get; private set; }
		public double DeltaVPrograde { get; private set; }
		public double DeltaVRadial { get; private set; }
        //以上依次是沿着三个方向的DV值（大概吧喵）
		private MBGNodeDeltaVAdjustorScript _maneuverNodeAdjustorBeingDragged;

        private MBGManeuverNode maneuverNode;
		private bool _dvChanged;

		private bool _dvChangeEnd;
		private bool _dvChangeBegin;
		private bool _dvChanging;
		private Vector3d _deltaV = Vector3d.zero;
		private Vector3d _prevDeltaV;
		private float _nextAutoLockAvailability;
		public double DeltaVMag { get; private set; }
		private GizmoState _gizmoState;
        public bool Locked = false;

        private MBGNodeDeltaVAdjustorScript[] _maneuverNodeAdjustors = new MBGNodeDeltaVAdjustorScript[6];

        public float DeltaVAdjustmentSensitivityLinear
        {
            get
            {
                return this._deltaVAdjustmentSensitivityLinear;
            }
            set
            {
                float num = Mathf.Clamp(value, 0.01f, 2f);
                this._deltaVAdjustmentSensitivityLinear = num;
                this._deltaVAdjustmentSensitivityExpo = Mathf.Pow(num, 1.5f);
            }
        }

        public IPlanetNode AssociatedPlanet => ((ICameraFocusable)_orbitLine).AssociatedPlanet;

        public bool FocusByClick => ((ICameraFocusable)_orbitLine).FocusByClick;

        public ICameraFocusable ItemToFocusOnWhenDeleted => ((ICameraFocusable)_orbitLine).ItemToFocusOnWhenDeleted;

        public float MinZoomDistance => ((ICameraFocusable)_orbitLine).MinZoomDistance;

        public IOrbitNode OrbitNode => ((ICameraFocusable)_orbitLine).OrbitNode;

        public Vector3 Position => (Vector3)_orbitLine.CoordinateConverter.ConvertSolarToMapView(_point.State.Position);

        public override ICameraFocusable AssociatedPlanetCameraFocusable => _orbitLine.AssociatedPlanetCameraFocusable;

        private Action<MBGManeuverNode> ConfirmBurn;
        public bool InitComplete { get; private set; } = false;

        public float _deltaVAdjustmentSensitivityLinear { get; private set; } = 1f;
        public float _deltaVAdjustmentSensitivityExpo { get; private set; } = 1f;
		private enum GizmoState
		{
			// Token: 0x04001910 RID: 6416
			Extended,
			// Token: 0x04001911 RID: 6417
			Retracted
		}
    }
}