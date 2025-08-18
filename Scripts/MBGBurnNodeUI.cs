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
using ModApi.Flight.Sim;
using UnityEngine;

namespace Assets.Scripts
{
    public class MBGBurnNodeUI
    {
        //此甚诡,汝知?
        //孩子们我想对着梅莉穿过的蕾丝边小白袜打胶,我不想写这一坨屎
        //luguanluguanluguanlulushijiandaole
        //PointerDown给的参数太少了,或者说ManeuverNodeManager.AddManeuverNode调用时涉及的东西太多了,绝对不是一个函数能搞定的,那些比我鸡巴毛还乱的OrbitChain要搞清楚的话整个MGBOrbitaLine都得加一堆东西,即便做一个都是空数据的MapInspectorPanel也不止
        public static  ManeuverNodeScript AddMBGManeuverNode(MapOrbitInfo originatingOrbitInfo, double trueAnomalyOnOriginatingOrbit, Vector3d deltaV, bool restoring)
        {
           
            return null;
        }
    }
}