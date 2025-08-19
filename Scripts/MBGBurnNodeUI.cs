using System;
using System.Collections.Generic;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.MapView.Items;
using Assets.Scripts.Flight.MapView.Orbits;
using Assets.Scripts.Flight.MapView.Orbits.Chain.ManeuverNodes;
using Assets.Scripts.Flight.MapView.Orbits.Chain.ManeuverNodes.Interfaces;
using Assets.Scripts.Flight.MapView.UI;
using Assets.Scripts.Flight.Sim;
using JetBrains.Annotations;
using ModApi;
using ModApi.Craft;
using ModApi.Flight.MapView;
using ModApi.Flight.Sim;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Assets.Scripts
{
    public class MBGBurnNodeUI:MonoBehaviour
    {
        //此甚诡,汝知?
        //孩子们我想对着梅莉穿过的蕾丝边小白袜打胶,我不想写这一坨屎
        //luguanluguanluguanlulushijiandaole
        //PointerDown给的参数太少了,或者说ManeuverNodeManager.AddManeuverNode调用时涉及的东西太多了,绝对不是一个函数能搞定的,那些比我鸡巴毛还乱的OrbitChain要搞清楚的话整个MGBOrbitaLine都得加一堆东西,即便做一个都是空数据的MapInspectorPanel也不止
        public static MBGBurnNodeUI Instance;
        private IInspectorPanel inspectorPanel;
        private InspectorModel inspectorModel;
        public GroupModel Group { get;set; }
        private DeltaVAdjustorModel _adjustorModelNormalAntiNormal;
        private DeltaVAdjustorModel _adjustorModelProgradeRetrograde;
        private DeltaVAdjustorModel _adjustorModelRadialOutRadialIn;
        private SliderModel _sensitivitySliderModel;

        void Start()
        {
            Instance = this;
        }

        public void Toggle()
        {
            try
            {
                inspectorPanel.Visible = !inspectorPanel.Visible;
            }
            catch (Exception)
            {
                CreateBurnNodeUI();
                inspectorPanel.Visible = !inspectorPanel.Visible;
            }
        }

        //_sensitivitySliderModel缺少对应Node,无法调用
        private void CreateBurnNodeUI()
        {
            this._adjustorModelProgradeRetrograde = new DeltaVAdjustorModel(DeltaVAdjustorModelType.ProgradeRetrograde);
            this._adjustorModelNormalAntiNormal = new DeltaVAdjustorModel(DeltaVAdjustorModelType.NormalAntiNormal);
            this._adjustorModelRadialOutRadialIn = new DeltaVAdjustorModel(DeltaVAdjustorModelType.RadialOutRadialIn);
            /*
            this._sensitivitySliderModel = new SliderModel("Sensitivity", (Func<float>) (() =>
            {
                IManeuverNode selectedNode = this.SelectedNode;
                return selectedNode == null ? 1f : selectedNode.DeltaVAdjustmentSensitivityLinear;
            }), (Action<float>) (value => this.SelectedNode.DeltaVAdjustmentSensitivityLinear = value), 0.01f, 2f);
            this._sensitivitySliderModel.Tooltip = "Adjusts how sensitive the delta-v adjustment gizmos are. Lower sensitivity results in smaller delta-v changes when interacting with the planned burn gizmos.";*/
            
            inspectorModel = new InspectorModel("Burn-UI", "<color=red>点火节点测试UI");
            this.Group = new GroupModel("何意味");
            this.Group.Add<DeltaVAdjustorModel>(this._adjustorModelProgradeRetrograde);
            this.Group.Add<DeltaVAdjustorModel>(this._adjustorModelNormalAntiNormal);
            this.Group.Add<DeltaVAdjustorModel>(this._adjustorModelRadialOutRadialIn);
            //this.Group.Add<SliderModel>(this._sensitivitySliderModel);
            inspectorModel.AddGroup(this.Group);
            inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, new InspectorPanelCreationInfo()
            {
                PanelWidth = 400,
                Resizable = true,
            });
        }
    }
}