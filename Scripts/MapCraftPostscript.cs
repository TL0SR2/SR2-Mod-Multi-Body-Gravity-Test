using Assets.Scripts.Flight.MapView.Items;
using HarmonyLib;

namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MapCraftPostscript
    {
        public MapCraft Craft;

        public MBGOrbitLine MBGOrbitLine;

        public int LastUpdateCalculateNum = 0;

        public MapCraftPostscript(MapCraft craft)
        {
            Craft = craft;
        }

        public MBGOrbit GetOrbit()
        {
            CraftNode craft = (CraftNode)AccessTools.Field(typeof(MapCraft), "_craftNode").GetValue(Craft);
            return MBGOrbit.GetMBGOrbit(craft);
        }
    }
}