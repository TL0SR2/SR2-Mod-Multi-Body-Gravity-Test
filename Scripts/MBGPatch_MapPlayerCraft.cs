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
    public class MBGPatch_MapPlayerCraft
    {
        [HarmonyPatch]
        public class MBGPatch_Initialize
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(MapPlayerCraft), "Initialize", new Type[] { typeof(CraftNode) });
            }

            public static void Postfix(MapPlayerCraft __instance)
            {
                __instance.OrbitInteractionScript.HoverEnter += MBGPatch_MapCraft.MapCraft_PostScript_Dic[__instance].MBGOrbitLine.OnHoverEnter;
                __instance.OrbitInteractionScript.HoverExit += MBGPatch_MapCraft.MapCraft_PostScript_Dic[__instance].MBGOrbitLine.OnHoverExit;
                __instance.OrbitInteractionScript.HoverStay += MBGPatch_MapCraft.MapCraft_PostScript_Dic[__instance].MBGOrbitLine.OnHoverStay;
            }

        }
    }
}