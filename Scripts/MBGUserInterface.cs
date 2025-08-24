using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ModApi.Ui;
using UnityEngine;
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
        //已知bug:因为我这个b偷懒直接try-catch太好用了你们知道吗所以会出现再次toggleUI的时候这个鸡巴玩意的数值显示会重置的bug
        //反正待会在修
        //我操什么叫做这个就是released的时候UI???
        public void ToggleMGBUI()
        {
            //已禁用
            MBGBurnNodeUI.Instance.Toggle();
            try
            {
                inspectorPanel.Visible = !inspectorPanel.Visible;
            }
            catch (Exception)
            {
                CreateInspectorPanel();
                inspectorPanel.Visible = !inspectorPanel.Visible;
            }
            //不开玩笑,处于一种很诡异的原因,你去写和null比较的方法啥的反而出现更多bug,为了项目尽快推进直接使用try-catch反而能让UI部分先跑起来
            
            
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
            
            var AdjustStep = new TextButtonModel("设置步长", b =>
            {
                UiSetStep("输入步长<br>" +
                          "步长越小计算精度越高，但计算时间也越长。<br>你鸡巴の敢瞎输入其他数据类我杀了你"
                          );
            });
            var SetInitRotateAngle = new TextButtonModel("设置初始旋转角", b =>
            {
                UiSetInitRotateAngle("输入初始旋转角<br>" +
                                     "采用旋转模式时的初始旋转角,单位是角度值<br>没事别瞎动这个"
                );
            });
            inspectorModel.Add(AdjustStep);
            inspectorModel.Add(SetInitRotateAngle);
            
                
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

        private void UiSetStep(string message)
        {
            InputDialogScript dialog = Game.Instance.UserInterface.CreateInputDialog();
            dialog.MessageText = message;
            dialog.InputText = "0.001";
            dialog.OkayClicked += (ModApi.Ui.InputDialogScript.InputDialogDelegate) (d =>
            {
                d.Close();
                float result;
                if (float.TryParse(dialog.InputText, out result))
                    MBGMath.SetMBGCalculationStep(result);
                else
                    //Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Invalid input, it has to be a number.");
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage("无效输入，输入必须是数字。");
            });
        }
        private void UiSetInitRotateAngle(string message)
        {
            InputDialogScript dialog = Game.Instance.UserInterface.CreateInputDialog();
            dialog.MessageText = message;
            dialog.InputText = "0";
            dialog.OkayClicked += (ModApi.Ui.InputDialogScript.InputDialogDelegate) (d =>
            {
                d.Close();
                float result;
                if (float.TryParse(dialog.InputText, out result))
                    MBGOrbitLine.SetRotateInitAngle(result);
                else
                    //Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Invalid input, it has to be a number.");
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage("你是傻逼吗?，输入必须是数字。");
            });
        }
    }
}