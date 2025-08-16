using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.VisualStyles;
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
using ModApi.Craft;
using ModApi.Flight.Sim;

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
        public readonly List<string> LagrangePointModeList = new List<string>(new[] { "无", "L1", "L2", "L3", "L4", "L5" });
        public readonly List<string> RotateReferenceList = new List<string>(new[] { "无", "自转追随模式", "公转追随模式"});
        

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Instance = this;
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Flight.NavPanel, OnBuildFlightMBGUI);
            MBGOrbitLine.ChangeReferencePlanetEvent+=OnChangeReferencePlanetEvent;
        }
        
        private void Update()
        {
        }

        #region 事件

       

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                PlanetNameList.Clear();
                
                Game.Instance.FlightScene.PlayerChangedSoi+=OnPlayerChangedSoi;
                foreach (var planet in Game.Instance.FlightScene.FlightState.SolarSystemData.Planets)
                {
                    PlanetNameList.Add(planet.Name);
                }
               

                if (Game.Instance.FlightScene.CraftNode != null && MBGOrbitLine.Instance != null)
                {
                    CreateInspectorPanel();
                }
                else
                {
                    Debug.LogWarning("Cannot create inspector panel: CraftNode or MBGOrbitLine is null");
                }
            }
        }

        private void OnChangeReferencePlanetEvent(object sender, string name)
        {
            
        }
        

        private void OnPlayerChangedSoi(ICraftNode craftNode, IPlanetNode planetNode)
        {

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
            //吃粑粑去吧鸡巴的,孩子们try-catch太好用了你们知道吗
            /*
            try
            {
                if (inspectorPanel == null)
                {
                    if (Game.Instance.FlightScene?.CraftNode != null)
                    {
                        CreateInspectorPanel();
                        if (inspectorPanel== null)
                        {
                            Debug.LogWarning("inspectorPanel怎么还是null,你心里没点b数吗");
                            return;
                        }
                    
                        inspectorPanel.Visible = true;
                    }
                    else
                    {
                        Debug.LogWarning("Craft node都没有,你toggle个鸡巴");
                        return;
                    }
                }
                if (inspectorPanel != null)
                {
                    inspectorPanel.Visible = !inspectorPanel.Visible;
                }
            }
            catch (Exception )
            {
                CreateInspectorPanel();
                inspectorPanel.Visible = !inspectorPanel.Visible;
                
            }*/
            
        }

        private void CreateInspectorPanel()
        {
            
            inspectorModel = new InspectorModel("BGM-UI-Settings", "<color=green>多体引力测试UI");

            // Dropdown for Current Planet Reference
            inspectorModel.Add(new DropdownModel(
                "当前星球参考系",
                () =>
                {
                    if (Game.Instance?.FlightScene?.CraftNode == null)
                        return "CraftNode is null";
                    if (MBGOrbitLine.Instance == null || MBGOrbitLine.Instance.GetCurrentPlanet()?.Planet == null)
                        return "No planet available";
                    return MBGOrbitLine.Instance.GetCurrentPlanet().Planet.Name;
                },
                value =>
                {
                    MBGOrbitLine.MBGOrbitLineChangeReference(value);
                    
                },
                this.PlanetNameList));

            
            // Dropdown for Lagrange Point Mode
            inspectorModel.Add(new DropdownModel(
                "拉格朗日点模式",
                () =>
                {
                    if (Game.Instance?.FlightScene?.CraftNode == null)
                        return "CraftNode is null";
                    if (MBGOrbitLine.Instance == null)
                        return "MBGOrbitLine is null";
                    var currentPlanet = MBGOrbitLine.Instance?.GetCurrentPlanet();
                    if (currentPlanet == null)
                        return "No planet available";
                    return "TEST"; //MBGOrbitLine.Instance.GetLagrangeReferenceType().ToString();
                },
                value =>
                {
                    Debug.LogFormat("LagrangePointModeList set to: " + value);
                    if (value == null)
                        return;
                    SetLagrangePointMode(value);
                },
                this.LagrangePointModeList));
            //轨道绘制旋转模式
            inspectorModel.Add(new DropdownModel(
                "轨道绘制旋转模式",
                () =>
                {
                    if (Game.Instance?.FlightScene?.CraftNode == null)
                        return "CraftNode is null";
                    if (MBGOrbitLine.Instance == null)
                        return "MBGOrbitLine is null";
                    var currentPlanet = MBGOrbitLine.Instance?.GetCurrentPlanet();
                    if (currentPlanet == null)
                        return "No planet available";
                    return "TEST"; //MBGOrbitLine.Instance.GetLagrangeReferenceType().ToString();
                },
                value =>
                {
                    Debug.LogFormat("Set Rotate Reference: " + value);
                    if (value == null)
                        return;
                    SetRotateReference(value);
                },
                this.RotateReferenceList));
            //步长增减
            var AddStep = new LabelButtonModel("增加步长", b =>
            {
                MBGMath.AddMBGCalculationStep();
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"步长设置为{MBGMath._CalculationRealStep}",true,5f);
            });
            AddStep.Label= "增加步长";
            
            var MinusStep = new LabelButtonModel("减少步长", b =>
            {
                MBGMath.MinusMBGCalculationStep();
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"步长设置为{MBGMath._CalculationRealStep}",true,5f);
            });
            MinusStep.Label= "减少步长";
            
            inspectorModel.Add(AddStep);
            inspectorModel.Add(MinusStep);
                
            inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, new InspectorPanelCreationInfo()
            {
                PanelWidth = 400,
                Resizable = true,
            });
        }
        
        private void SetLagrangePointMode(string value)
        {
            try
            {
                if (value==("L1"))
                {
                    Debug.Log("Set L1");
                    MBGOrbitLine.ChangeLPointType(1);
                    Debug.Log("Set L1 success");
                    return;
                }

                if (value == ("L2"))
                {
                    Debug.Log("Set L2");
                    MBGOrbitLine.ChangeLPointType(2);
                    Debug.Log("Set L2 success");
                    return; 
                }
                if (value == ("L3"))
                {
                    Debug.Log("Set L3");
                    MBGOrbitLine.ChangeLPointType(3);
                    Debug.Log("Set L3 success");
                    return;
                }

                if (value == ("L4"))
                {
                    Debug.Log("Set L4");
                    MBGOrbitLine.ChangeLPointType(4);
                    Debug.Log("Set L4 success");
                    return;
                }
                if (value == ("L5"))
                {
                    Debug.Log("Set L5");
                    MBGOrbitLine.ChangeLPointType(5);
                    Debug.Log("Set L5 success");
                    return;
                }

                else
                {
                    Debug.Log("Set Default");
                    MBGOrbitLine.ChangeLPointType(0);
                    Debug.Log("Set Default success");
                    return;
                }
            }
            catch (Exception)
            {
                Debug.Log("Set Default,但是这他妈不正常");
                MBGOrbitLine.ChangeLPointType(0);
            }
        }

        private void SetRotateReference(string value)
        {
            switch (value)
            {
                case "自转追随模式":
                    MBGOrbitLine.SetReferenceMode(1);
                    break;
                case "公转追随模式":
                    MBGOrbitLine.SetReferenceMode(2);
                    break;
                default:
                    MBGOrbitLine.SetReferenceMode(0);
                    break;
            }
        }
    }
}