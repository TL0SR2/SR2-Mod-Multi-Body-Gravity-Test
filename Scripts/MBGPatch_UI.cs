using System;
using Assets.Scripts.Flight.UI;
using HarmonyLib;
using UnityEngine;
namespace Assets.Scripts
{
    //狗日的啥卵jundroo这个地方自己写的有问题没更新害的我只能用harmony了
    //总之这个patch就敢一件事情:那就是用harmonyLib让MBGUserInterface里面新建的按钮的onClick能正常工作
        [HarmonyPatch(typeof(NavPanelController), "LayoutRebuilt")]
        class LayoutRebuiltPatch
        {
            static bool Prefix(NavPanelController __instance)
            {
                try
                {
                    __instance.xmlLayout.GetElementById(MBGUserInterface.MBGUIButtomId)
                        .AddOnClickEvent(MBGUserInterface.Instance.ToggleMGBUI, false);
                }
                catch (Exception e)
                {
                    Debug.LogFormat("Error while adding click event to{0}", e);
                }

                return true;
            }
        }
    
}