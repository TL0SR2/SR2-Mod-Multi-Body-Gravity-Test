using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Assets.Scripts.Flight.MapView.Items;
using Assets.Scripts.Flight.MapView.Orbits;
using System.Reflection;
using Assets.Scripts.Flight.MapView.Interfaces.Contexts;
using System;
using ModApi.Ioc;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.MapView.Orbits.Chain.ManeuverNodes;


namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGPatch_ManeuverNodeScript
    {
        public static Dictionary<ManeuverNodeScript, MBGOrbitLine> ManeuverNodeScript_MBGOrbitLine_Dic = new Dictionary<ManeuverNodeScript, MBGOrbitLine>() { };

        [HarmonyPatch]
        public class MBGPatch_InitializeUi
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(ManeuverNodeScript), "InitializeUi", new Type[] { typeof(IDrawModeProvider) });
            }

            public static void Postfix(ManeuverNodeScript __instance)
            {
                Canvas _infoCanvas = (Canvas)AccessTools.Field(typeof(ManeuverNodeScript), "_infoCanvas").GetValue(__instance);
                ManeuverNodeScript_MBGOrbitLine_Dic[__instance].AddPointerNotifications(_infoCanvas);
            }

        }
        [HarmonyPatch]
        public class MBGPatch_Initialize
        {
            [HarmonyTargetMethod]
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(ManeuverNodeScript), "Initialize", new Type[] { typeof(ICraftContext), typeof(MapOrbitLine), typeof(Vector3d), typeof(NodeListChangeCategory) });
            }

            public static bool Prefix(ManeuverNodeScript __instance,ICraftContext craftContext,MapOrbitLine orbitLine)
            {
                IIocContainer ioc = orbitLine.Ioc;
                IMapViewContext context = ioc.Resolve<IMapViewContext>(craftContext, false);
                IPlayerCraftProvider playerCraftProvider = ioc.Resolve<IPlayerCraftProvider>(context, false);
                MapPlayerCraft playerCraft = playerCraftProvider.PlayerCraft;
                MBGOrbitLine MBGOrbitLine = MBGPatch_MapCraft.MapCraft_PostScript_Dic[playerCraft].MBGOrbitLine;
                MBGOrbitLine.GameObjLayer = __instance.gameObject.layer;
                //Debug.Log($"Mane Node Layer {__instance.gameObject.layer}");
                ManeuverNodeScript_MBGOrbitLine_Dic.Add(__instance, MBGOrbitLine);
                return true;
            }

        }
    }
}