using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ModApi.Ui;
using UnityEngine;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Flight.GameView;
using ModApi.Math;
using ModApi.Scenes.Events;
using ModApi.Ui.Inspector;
using Assets.Scripts.Flight.Sim.MBG;

namespace Assets.Scripts
{
    //虽然听起来很扯淡,但是这个直接继承自MonoBehaviour的类,在Unity中需要attach在一个prefab上,才他妈可以正常运行,
    public class MBGUserInterface : MonoBehaviour
    {
        public const string MBGUIButtomId = "toggle-MGB-orbit-ui-buttom";
        public static MBGUserInterface Instance;
        private IInspectorPanel inspectorPanel;
        private InspectorModel inspectorModel;
        
        public List<string> PlanetNameList { get; private set; } = new List<string>();
        public List<string> LagrangePointModeList { get; private set; } = new List<string>();
        

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Instance = this;
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Flight.NavPanel, OnBuildFlightMBGUI);
        }

        private void Update()
        {

        }

        #region 事件

       

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                if (Game.Instance.FlightScene.CraftNode!= null)
                {
                    //MBGOrbitLine.MBGOrbitLineChangeReference(Game.Instance.FlightScene.CraftNode.Parent.Name);
                }
                
                PlanetNameList.Clear();
                LagrangePointModeList.Clear();
                foreach (var VARIABLE in Game.Instance.FlightScene.FlightState.SolarSystemData.Planets)
                {
                    PlanetNameList.Add(VARIABLE.Name);
                }
                LagrangePointModeList.Add("无");
                LagrangePointModeList.Add("L1");
                LagrangePointModeList.Add("L2");
                LagrangePointModeList.Add("L3");
                LagrangePointModeList.Add("L4");
                LagrangePointModeList.Add("L5");
            }
        }

         
        

        

        #endregion
        
        private static void OnBuildFlightMBGUI(BuildUserInterfaceXmlRequest request)
        {
            Debug.Log("OnBuildFlightMBGUI");
            var inspectButton = request.XmlDocument
                .Descendants(XmlLayoutConstants.XmlNamespace + "ContentButton")
                .First(x => (string)x.Attribute("id") == "toggle-flight-inspector");
            inspectButton.Parent.Add(
                new XElement(
                    XmlLayoutConstants.XmlNamespace + "ContentButton",
                    new XAttribute("id", MBGUIButtomId),
                    new XAttribute("class", "panel-button audio-btn-click"),
                    new XAttribute("tooltip", "Toggle MGB UI."),
                    new XAttribute("name", "NavPanel.ToggleMGBUI"),
                    new XElement(
                        XmlLayoutConstants.XmlNamespace + "Image",
                        new XAttribute("class", "panel-button-icon"),
                        //贴图先用原版的
                        new XAttribute("sprite", "Ui/Sprites/Flight/IconMapView"))));
            Debug.Log("干");
        }
        public void ToggleMGBUI()
        { 
            try
            {
                inspectorPanel.Visible = !inspectorPanel.Visible;
            }
            catch (Exception)
            {
                
                CreateInspectorPanel();
                inspectorPanel.Visible = !inspectorPanel.Visible;
            }
        }

        private void CreateInspectorPanel()
        {
            inspectorModel = new InspectorModel("BGM-UI-Settings", "<color=green>多体引力测试UI");
            inspectorModel.Add(new DropdownModel(
                "当前星球参考系",
                () => Game.Instance.FlightScene.CraftNode == null
                    ? "你妈的这个地方null了"
                    : MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Name,
                value => MBGOrbitLine.MBGOrbitLineChangeReference(value),
                this.PlanetNameList));
            GroupModel groupModelLagrangePoint = new GroupModel("拉格朗日点设置");
            groupModelLagrangePoint.Add(new DropdownModel(
                "拉格朗日点模式",
                () => Game.Instance.FlightScene.CraftNode == null
                    ? "你妈的这个地方null了"    
                    : GetCurrentLagrangePointMode(MBGOrbitLine.Instance.GetCurrentPlanet().type),
                value =>SetLagrangePointMode(value),
                this.LagrangePointModeList));
            
            inspectorModel.AddGroup(groupModelLagrangePoint);
            inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, new InspectorPanelCreationInfo()
            {
                PanelWidth = 400,
                Resizable = true,
            });
        }

        private string GetCurrentLagrangePointMode(GeneralizedPlanetType e)
        {
            if(MBGOrbitLine.Instance.GetCurrentPlanet().isSun)
                return "无";
            switch (e)
            {
                case GeneralizedPlanetType.Planet:
                    return "无";
                case GeneralizedPlanetType.L1:
                    return $"{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Parent.Name}--{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Name} L1";
                case GeneralizedPlanetType.L2:
                    return $"{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Parent.Name}--{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Name} L2";
                case GeneralizedPlanetType.L3:
                    return $"{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Parent.Name}--{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Name} L3";
                case GeneralizedPlanetType.L4:
                    return $"{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Parent.Name}--{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Name} L4";
                case GeneralizedPlanetType.L5:
                    return $"{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Parent.Name}--{MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Name} L5";
            }
            return "NULL(几把的这里歇逼了)";
        }

        private void SetLagrangePointMode(string value)
        {
            if (MBGOrbitLine.Instance.GetCurrentPlanet().isSun)
            {
                return;
            }
            switch (value)
            {
                case "无":
                    MBGOrbitLine.Instance.ChangeLPointTypeMethod(GeneralizedPlanetType.Planet);
                    break;
                case "L1":
                    MBGOrbitLine.Instance.ChangeLPointTypeMethod(GeneralizedPlanetType.L1);
                    break;
                case "L2":
                    MBGOrbitLine.Instance.ChangeLPointTypeMethod(GeneralizedPlanetType.L2);
                    break;
                case "L3":
                    MBGOrbitLine.Instance.ChangeLPointTypeMethod(GeneralizedPlanetType.L3);
                    break;
                case "L4":
                    MBGOrbitLine.Instance.ChangeLPointTypeMethod(GeneralizedPlanetType.L4);
                    break;
                case "L5":
                    MBGOrbitLine.Instance.ChangeLPointTypeMethod(GeneralizedPlanetType.L5);
                    break;
            }
        }
    }
}