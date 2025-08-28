using System;
using System.Collections.Generic;
using Assets.Scripts.Flight.MapView.UI;
using Assets.Scripts.Ui;
using ModApi;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vectrosity;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGNodeDeltaVAdjustorScript : MonoBehaviour, IDragHandler, IEventSystemHandler, IEndDragHandler, IBeginDragHandler, IDisposable, ICanvasScaleChangeHandler
    {


        public event MBGNodeDeltaVAdjustorScript.AdjustorChangeDelegate ManeuverNodeAdjustmentChangeBeginEvent;

        public event MBGNodeDeltaVAdjustorScript.AdjustorChangeDelegate ManeuverNodeAdjustmentChangeEndEvent;

        public event MBGNodeDeltaVAdjustorScript.AdjustorChangeDelegate ManeuverNodeAdjustmentChangingEvent;
		public Vector3d DeltaV
		{
			get
			{
				return this._deltaV;
			}
		}

        public Vector2 CurrentDragPos
        {
            get
            {
                return this._currentMousePos;
            }
        }

        public bool DisableDraggingWhenFacingCamera { get; set; } = true;

        public bool ExtensionEnabled { get; set; } = true;

        public bool IsDragging
        {
            get
            {
                return this._dragging;
            }
        }

        public bool IsSelected
        {
            get
            {
                return this._selected;
            }
        }
		public void AdjustDeltaV(float input)
		{
			Vector3d a = ManeuverVec * (double)input;
			double d = (double)ManeuverNodeScript._deltaVAdjustmentSensitivityExpo * ManeuverNodeScript._deltaVAdjustmentSensitivityLinear;
			this._deltaV += a * GetDvScalar(ManeuverNodeScript) * d;
		}
		public void SetDeltaV(Vector3d deltaV)
		{
			this._deltaV = deltaV;
		}
		public void SetDeltaV(double value)
		{
			this._deltaV = ManeuverVec * value;
		}
		protected void OnGizmoDragged(float gizmoPercent)
		{
			this.AdjustDeltaV(gizmoPercent);
		}
		private static double GetDvScalar(MBGManeuverNodeScript ManeuverNodeScript)
		{
			return Mathd.Lerp(0.0, ManeuverNodeScript._point.State.Velocity.magnitude, (double)Time.unscaledDeltaTime / 5.0);
		}

        public MBGManeuverNodeScript ManeuverNodeScript { get; private set; }

        public Vector3d ManeuverVec {  get; private set; }

        public static MBGNodeDeltaVAdjustorScript Create(Canvas canvas, Transform parent, Func<Vector3d> maneuverVec, MBGManeuverNodeScript node, string name, string iconName, Color lineColor)
        {
            MBGNodeDeltaVAdjustorScript t = new GameObject(name).AddComponent<MBGNodeDeltaVAdjustorScript>();
            t.transform.SetParent(parent);
            t.Initialize(canvas, maneuverVec, node, iconName, lineColor);
            return t;
        }

        public void CompletePendingAnimations()
        {
            this.DoSelectionChangingAnimations(true);
        }

        public virtual void Dispose()
        {
            this.ManeuverNodeAdjustmentChangeBeginEvent = null;
            this.ManeuverNodeAdjustmentChangeEndEvent = null;
            this.ManeuverNodeAdjustmentChangingEvent = null;
        }

        public void ForceStopDrag()
        {
            this.StopDrag();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                this._dragging = true;
                this._startingMousePos = eventData.position;
                MBGNodeDeltaVAdjustorScript.AdjustorChangeDelegate maneuverNodeAdjustmentChangeBeginEvent = this.ManeuverNodeAdjustmentChangeBeginEvent;
                if (maneuverNodeAdjustmentChangeBeginEvent == null)
                {
                    return;
                }
                maneuverNodeAdjustmentChangeBeginEvent(this);
            }
        }

        public void OnCanvasScaleChanged(float canvasScaleFactor)
        {
            this.CreateConnectingLine();
        }

        public void OnDrag(PointerEventData eventData)
        {
            this._currentMousePos = eventData.position;
            this._dragVec = this._startingMousePos - this._currentMousePos;
            this._dragState = MBGNodeDeltaVAdjustorScript.DragState.Drag;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            this.StopDrag();
        }

        public void UpdateVector()
        {
            this.ManeuverVec = this._maneuverVecFunc();
        }

        internal void OnDeselected()
        {
            Debug.Log("TL0SR2 MBG Node DeltaV Adjustor Script -- On Deselecte");
            this._selected = false;
            this._selectionChanging = true;
            this._selectionChangedTime = Time.unscaledTime;
        }
        internal void OnSelected()
        {
            Debug.Log("TL0SR2 MBG Node DeltaV Adjustor Script -- On Selected");
            this._selected = true;
            this._selectionChanging = true;
            this._selectionChangedTime = Time.unscaledTime;
            this._icon.gameObject.SetActive(true);
            this._connectingLine.rectTransform.gameObject.SetActive(true);
        }
        protected virtual void Awake()
        {
            double num = (double)(4 * (Game.Instance.Device.IsMobileBuild ? 2 : 1));
            double d = 0.01745329 * num;
            this._lineScaleBaseSize = Mathd.Tan(d);
        }

        public void FixedUpdate() 
        {
            Debug.Log($"TL0SR2 MBG Node DeltaV Adjustor Script -- FixedUpdate");
        }

        public void Update()
        {
            Debug.Log($"TL0SR2 MBG Node DeltaV Adjustor Script -- Update");
        }

        public void LateUpdate()
        {
            Debug.Log($"TL0SR2 MBG Node DeltaV Adjustor Script -- LateUpdate -- Data  _selectionChanging {_selectionChanging}  _selected {_selected}");
            if (this._dragVec != Vector2.zero && !Utilities.Input.AnyMouseButton())
            {
                Debug.LogWarning("dragVec is nonzero yet no mouse buttons are down...OnDragEnd wasn't called when mouse was released.");
                this.OnEndDrag(null);
            }
            if (this._selectionChanging)
            {
                this.DoSelectionChangingAnimations(false);
            }
            if (this._selectionChanging || this._selected)
            {
                float uiScale = Game.UiScale;
                Vector3d nodeWorldPosition = this.ManeuverNodeScript.WorldPosition;
                double d = this._lineScaleBaseSize * Vector3.Distance((Vector3)nodeWorldPosition, ManeuverNodeScript.Camera.transform.position) * (double)this._adjustorExtensionPercent * (double)uiScale;
                Vector3d vector3d = nodeWorldPosition + Vector3d.Scale(this.ManeuverVec.normalized, Vector3d.one * d);
                //Vector2 vector = Utilities.GameWorldToScreenPoint(this._canvas.worldCamera, (Vector3)vector3d);
                Vector2 vector = (Vector2)(Vector3)this.ManeuverNodeScript.CoordinateConverter.ConvertSolarToMapView(vector3d);
                //Vector即是当前拖动球应该处于的屏幕位置
                Vector2 vector2 = (Vector2)Utilities.GameWorldToScreenPoint(ManeuverNodeScript.Camera, (Vector3)nodeWorldPosition);
                this._maneuverScreenVec = (vector - vector2).normalized;
                //Vector2是对应推进矢量在屏幕上的投影矢量
                Vector3 vector3 = vector;
                if (this._dragging || this._dragState == MBGNodeDeltaVAdjustorScript.DragState.End)
                {
                    if (this.ExtensionEnabled && !this._selectionChanging)
                    {
                        float num = Vector3.Distance(Utilities.GameWorldToScreenPoint(ManeuverNodeScript.Camera, (Vector3)nodeWorldPosition), vector);
                        //num是该拖动球到点火节点的屏幕距离
                        float num2 = Vector2.Dot(this._currentMousePos - vector, this._maneuverScreenVec);
                        if (num != 0f && num2 != 0f)
                        {
                            float num3 = num * (float)((num2 >= 0f) ? 3 : -1);
                            float num4 = num * ((num2 >= 0f) ? 0f : 0.1f);
                            float num5 = Mathf.Sign(num2) * (Mathf.Max(0f, Math.Abs(num2) - num4) / Math.Max(0f, Math.Abs(num3) - num4));
                            num5 = Mathf.Clamp(num5, -1f, 1f);
                            vector3 += this._maneuverScreenVec * ((num2 >= 0f) ? Mathf.Min(num2, num3) : Mathf.Max(num2, num3));
                            float gizmoPercent = num5 * ((num5 >= 0f) ? 1f : 0.25f);
                            this.OnGizmoDragged(gizmoPercent);
                        }
                    }
                    MBGNodeDeltaVAdjustorScript.DragState dragState = this._dragState;
                    if (dragState != MBGNodeDeltaVAdjustorScript.DragState.Drag)
                    {
                        if (dragState != MBGNodeDeltaVAdjustorScript.DragState.End)
                        {
                            throw new InvalidOperationException("Unsupported drag state");
                        }
                        MBGNodeDeltaVAdjustorScript.AdjustorChangeDelegate maneuverNodeAdjustmentChangeEndEvent = this.ManeuverNodeAdjustmentChangeEndEvent;
                        if (maneuverNodeAdjustmentChangeEndEvent != null)
                        {
                            maneuverNodeAdjustmentChangeEndEvent(this);
                        }
                        this._dragState = MBGNodeDeltaVAdjustorScript.DragState.Drag;
                    }
                    else
                    {
                        MBGNodeDeltaVAdjustorScript.AdjustorChangeDelegate maneuverNodeAdjustmentChangingEvent = this.ManeuverNodeAdjustmentChangingEvent;
                        if (maneuverNodeAdjustmentChangingEvent != null)
                        {
                            maneuverNodeAdjustmentChangingEvent(this);
                        }
                    }
                }
                this._icon.transform.position = vector3;
                this._icon.transform.rotation = Quaternion.LookRotation(this.ManeuverNodeScript.Camera.transform.position - this._icon.transform.position);
                float num6 = Mathf.Min(this.GetIconTransparency(vector3d), 0.8f);
                this._iconColor.a = num6;
                this._icon.color = this._iconColor;
                if (this.DisableDraggingWhenFacingCamera)
                {
                    this._icon.raycastTarget = (double)num6 > 0.4;
                }
                else
                {
                    this._icon.raycastTarget = true;
                }
                Vector3 vector4 = this._connectingLine.rectTransform.InverseTransformPoint(vector2);
                Vector3 vector5 = this._icon.transform.localPosition;
                Vector3 vector6 = vector5 - vector4;
                vector5 = vector4 + vector6.normalized * (vector6.magnitude - this._iconSize * 0.5f * uiScale);
                this._connectingLine.points2[0] = vector4;
                this._connectingLine.points2[1] = vector5;
                this._connectingLineColor.a = num6;
                this._connectingLine.color = this._connectingLineColor;
                this._connectingLine.Draw();
            }
        }

        private void CreateConnectingLine()
        {
            if (this._connectingLine != null)
            {
                VectorLine.Destroy(ref this._connectingLine);
            }
            this._connectingLine = new VectorLine(base.name + "_line", new List<Vector2>(2), 2f);
            this._connectingLine.rectTransform.gameObject.layer = base.gameObject.layer;
            this._connectingLine.rectTransform.gameObject.transform.SetParent(base.transform.parent, false);
            this._connectingLine.color = this._connectingLineColor;
        }

        private void DoSelectionChangingAnimations(bool completeImmediately)
        {
            if (!this._selectionChanging)
            {
                return;
            }
            if (completeImmediately || this._selectionChangedTime + 0.15f <= Time.unscaledTime)
            {
                this._selectionChanging = false;
                if (this._selected)
                {
                    this._adjustorExtensionPercent = 1f;
                    return;
                }
                this._icon.gameObject.SetActive(false);
                this._connectingLine.rectTransform.gameObject.SetActive(false);
                this._adjustorExtensionPercent = 0f;
                return;
            }
            else
            {
                float num = Time.unscaledTime - this._selectionChangedTime;
                if (this._selected)
                {
                    this._adjustorExtensionPercent = Mathf.Lerp(0f, 1f, num / 0.15f);
                    return;
                }
                this._adjustorExtensionPercent = Mathf.Lerp(1f, 0f, num / 0.15f);
                return;
            }
        }
        private float GetIconTransparency(Vector3d worldSpaceAdjustorPosition)
        {
            Vector3d normalized = (worldSpaceAdjustorPosition - this.ManeuverNodeScript._point.State.Position).normalized;
            return Mathf.Pow(1f - Mathf.Abs(Vector3.Dot(this._canvas.worldCamera.transform.forward, (Vector3)normalized)), 0.5f);
        }

        private void Initialize(Canvas canvas, Func<Vector3d> maneuverVec, MBGManeuverNodeScript node, string iconName, Color lineColor)
        {
            //this.gameObject.SetActive(true);
            this._canvas = canvas;
            //this._orbitInfoProvider = orbitInfoProvider;
            //this._mapOptions = ioc.Resolve<IMapOptions>(false);
            this._maneuverVecFunc = maneuverVec;
            this.ManeuverNodeScript = node;
            //this._drawModeProvider = drawModeProvider;
            //this._positionProvider = positionProvider;
            this.UpdateVector();
            this._icon = base.gameObject.AddComponent<Image>();
            this._icon.gameObject.layer = base.gameObject.layer;
            this._icon.sprite = UiUtils.LoadIconSprite(iconName);
            this._icon.transform.localScale = Vector3.one * 0.25f;
            this._iconSize = this._icon.rectTransform.sizeDelta.x * this._icon.transform.localScale.x;
            this._iconColor = Color.white;
            this._connectingLineColor = lineColor;
            this.CreateConnectingLine();
        }

        private void OnDestroy()
        {
            this.Dispose();
        }


        private void StopDrag()
        {
            this._dragVec = Vector2.zero;
            this._dragging = false;
            this._dragState = MBGNodeDeltaVAdjustorScript.DragState.End;
        }
        private const float SelectionChangedAnimDuration = 0.15f;

        private float _adjustorExtensionPercent;
        private Canvas _canvas;
        private VectorLine _connectingLine;

        private Color _connectingLineColor;
        private Vector2 _currentMousePos;

        private bool _dragging;

        private MBGNodeDeltaVAdjustorScript.DragState _dragState = MBGNodeDeltaVAdjustorScript.DragState.End;

        private Vector2 _dragVec;

        //private IDrawModeProvider _drawModeProvider;

        private Image _icon;

        private Color _iconColor;
        private float _iconSize;

        private double _lineScaleBaseSize;

        private Vector3 _maneuverScreenVec;

        private Func<Vector3d> _maneuverVecFunc;

        //private IMapOptions _mapOptions;

        //private IManeuverNodePositionProvider _positionProvider;
        private bool _selected;

        private float _selectionChangedTime;

        private bool _selectionChanging;

        private Vector2 _startingMousePos;
		private Vector3d _deltaV;
		//private IOrbitInfoProvider _orbitInfoProvider;

        public delegate void AdjustorChangeDelegate(MBGNodeDeltaVAdjustorScript source);

        private enum DragState
        {
            Drag,
            End
        }
    }
}