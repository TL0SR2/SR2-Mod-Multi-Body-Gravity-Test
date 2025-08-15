using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Assets.Scripts.Flight.MapView.Items;
using ModApi.Flight.MapView;
using ModApi.Flight.Sim;
using Assets.Scripts.Craft;
using ModApi.Craft;
using System.Reflection;
using Assets.Scripts.Flight.Sim.MBG;
using Assets.Scripts.State;
using System;
using ModApi.Scripts.State;
using System.Xml.Linq;
using Assets.Scripts.Flight.MapView.UI;
using Assets.Scripts.Ui;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGPatch_MapCraft
    {
        public static Dictionary<MapCraft, MapCraftPostscript> MapCraft_PostScript_Dic = new Dictionary<MapCraft, MapCraftPostscript> { };
        public static Type MapCraftType = AccessTools.TypeByName("Assets.Scripts.Flight.MapView.Items.MapCraft");

        
        [HarmonyPatch]
        public class MBGPatch_Constructor
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Constructor(MapCraftType, new Type[] { });
            }

            public static void Postfix(MapCraft __instance)
            {
                MapCraftPostscript postscript = new MapCraftPostscript(__instance);
                MapCraft_PostScript_Dic.Add(__instance, postscript);
            }

        }

        [HarmonyPatch]
        public class MBGPatch_CreateOrbitLine
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(MapCraftType, "CreateOrbitLine", new Type[] { });
            }

            public static void Postfix(MapCraft __instance)
            {
                //Shader _orbitLineShader = (Shader)AccessTools.Field(typeof(MapCraft), "_orbitLineShader").GetValue(__instance);
                Material _orbitLineMaterial = (Material)AccessTools.Field(typeof(MapCraft), "_orbitLineMaterial").GetValue(__instance);
                Material lineMaterial = UnityEngine.Object.Instantiate<Material>(_orbitLineMaterial);

                MBGOrbitLine MBGOrbitLine = MBGOrbitLine.Create(__instance.Ioc, __instance.MapViewContext, __instance.OrbitInfo.OrbitNode, __instance.Data, UiUtils.GetSortedOrbitLineColor(0), "PlayerOrbit", __instance.Camera, lineMaterial);
                MapCraft_PostScript_Dic[__instance].MBGOrbitLine = MBGOrbitLine;
            }

        }

        [HarmonyPatch]
        public class MBGPatch_OnBeforeCameraPositioned
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(MapCraftType, "OnBeforeCameraPositioned", new Type[] { typeof(bool) });
            }

            public static void Postfix(MapCraft __instance, bool mapViewVisible)
            {
                IMapOptions _options = (IMapOptions)AccessTools.Field(typeof(MapCraft), "_options").GetValue(__instance);
                bool flag1 = _options.Craft.ContinuouslyUpdateChain;
                //如果轨道满足重绘条件，flag1 = true
                var IsApplyingThrustMethod = AccessTools.Method(typeof(MapCraft), "IsApplyingThrust", new Type[] { });
                bool flag4 = MapCraft_PostScript_Dic[__instance].LastUpdateCalculateNum != MapCraft_PostScript_Dic[__instance].GetOrbit().CaculationNum || (bool)IsApplyingThrustMethod.Invoke(__instance, new object[] { });
                //如果轨道改变了，flag4 = true
                bool _nodeListChanged = (bool)AccessTools.Field(typeof(MapCraft), "_nodeListChanged").GetValue(__instance);
                bool _chainSelectionChanged = (bool)AccessTools.Field(typeof(MapCraft), "_chainSelectionChanged").GetValue(__instance);
                //bool flag6 = __instance.CheckAndCreateEncounter();
                bool _orbitLineDirty = (bool)AccessTools.Field(typeof(MapCraft), "_orbitLineDirty").GetValue(__instance);
                bool flag11 = mapViewVisible && (flag1 || flag4 || _nodeListChanged || _chainSelectionChanged || _orbitLineDirty);
                if (flag11)
                {
                    MapCraft_PostScript_Dic[__instance].MBGOrbitLine.UpdateLine();
                }
            }

        }
        [HarmonyPatch]
        public class MBGPatch_OnDestroy
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(MapCraftType, "OnDestroy", new Type[] { });
            }

            public static void Postfix(MapCraft __instance)
            {
                MapCraft_PostScript_Dic[__instance].MBGOrbitLine?.Destroy();
                try
                {
                    MapCraft_PostScript_Dic.Remove(__instance);
                }
                finally{}
            }

        }
    }
}