using System.IO;
using Terraria.ModLoader;
using ImapoRealisticEarthquake.Common.Systems;

namespace ImapoRealisticEarthquake
{
    // Главный класс мода. Логика вынесена в Common/Systems/EarthquakeSystem.cs
    public class ImapoRealisticEarthquakeMod : Mod
    {
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            EarthquakeNetHandler.HandlePacket(reader, whoAmI);
        }
    }
}
