using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Assets.Scripts.Flight.MapView.Items;
using ModApi.Flight.MapView;
using ModApi.Flight.Sim;
using Assets.Scripts.Craft;
using Assets.Scripts.Flight.MapView.Orbits;
using System.Reflection;
using Assets.Scripts.Flight.Sim.MBG;
using Assets.Scripts.Flight.MapView.Interfaces.Contexts;
using System;
using ModApi.Ioc;
using System.Xml.Linq;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.MapView.Orbits.Chain.ManeuverNodes;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGPatch_ManeuverNodeManagerScript
    {
        [HarmonyPatch]
        public class MBGPatch_InitializeUi
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(ManeuverNodeManagerScript), "Initialize", new Type[] { typeof(ICraftContext), typeof(MapPlayerCraft) });
            }

            public static void Postfix(ManeuverNodeManagerScript __instance)
            {
                //Debug.Log($"ManeuverNodeManagerScript  Layer {__instance.gameObject.layer}");
                //MapPlayerCraft _craft = (MapPlayerCraft)AccessTools.Field(typeof(ManeuverNodeManagerScript), "_craft").GetValue(__instance);
                //MBGPatch_MapCraft.MapCraft_PostScript_Dic[_craft].MBGOrbitLine.maneuverNodeManagerScript = __instance;
                //MBGPatch_MapCraft.MapCraft_PostScript_Dic[_craft].MBGOrbitLine.LateInit(__instance);
            }

        }
    }
}