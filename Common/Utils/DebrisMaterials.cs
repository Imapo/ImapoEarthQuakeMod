using System.Collections.Generic;
using Terraria.ID;

namespace ImapoRealisticEarthquake.Common.Utils
{
    // Описание одного "осыпающегося" материала.
    public struct DebrisMaterial
    {
        public string Name;
        public int TileType;         // ID настоящего тайла в мире (для проверки потолка)
        public int TextureItemID;    // ID предмета, чью иконку используем как текстуру обломка (чтобы не рисовать свою)
        public float Hardness;       // множитель урона/прочности. Чем больше - тем твёрже порода
        public int DustType;         // тип пыли для эффекта разрушения

        public DebrisMaterial(string name, int tileType, int textureItemId, float hardness, int dustType)
        {
            Name = name;
            TileType = tileType;
            TextureItemID = textureItemId;
            Hardness = hardness;
            DustType = dustType;
        }
    }

    public static class DebrisMaterials
    {
        // Только "рыхлые" породы могут осыпаться. Камень, руды и всё, что твёрже камня - надёжное укрытие.
        // Твёрдость расставлена согласно ТЗ: снег - мягче всего, земля/грязь/глина - средне,
        // песчаник/затвердевший песок/лёд - самые твёрдые из "рыхлых" материалов.
        public static readonly Dictionary<int, DebrisMaterial> SoftTiles = new()
        {
            [TileID.SnowBlock] = new DebrisMaterial("Снег", TileID.SnowBlock, ItemID.SnowBlock, 0.5f, DustID.Snow),
            [TileID.Slush] = new DebrisMaterial("Слякоть", TileID.Slush, ItemID.SlushBlock, 0.55f, DustID.Snow),

            [TileID.Dirt] = new DebrisMaterial("Земля", TileID.Dirt, ItemID.DirtBlock, 1.0f, DustID.Dirt),
            [TileID.Mud] = new DebrisMaterial("Грязь", TileID.Mud, ItemID.MudBlock, 1.0f, DustID.Mud),
            [TileID.Silt] = new DebrisMaterial("Ил", TileID.Silt, ItemID.SiltBlock, 1.05f, DustID.Dirt),
            [TileID.Ash] = new DebrisMaterial("Пепел", TileID.Ash, ItemID.AshBlock, 1.1f, DustID.Ash),

            [TileID.ClayBlock] = new DebrisMaterial("Глина", TileID.ClayBlock, ItemID.ClayBlock, 1.25f, DustID.Clentaminator_Blue),

            [TileID.Sand] = new DebrisMaterial("Песок", TileID.Sand, ItemID.SandBlock, 1.4f, DustID.Sand),
            [TileID.Crimsand] = new DebrisMaterial("Багровый песок", TileID.Crimsand, ItemID.CrimsandBlock, 1.4f, DustID.CrimsonPlants),
            [TileID.Ebonsand] = new DebrisMaterial("Порочный песок", TileID.Ebonsand, ItemID.EbonsandBlock, 1.4f, DustID.CorruptPlants),
            [TileID.Pearlsand] = new DebrisMaterial("Жемчужный песок", TileID.Pearlsand, ItemID.PearlsandBlock, 1.4f, DustID.HallowedPlants),

            // Самые твёрдые из "рыхлых" - песчаник (спрессованный песок) и лёд.
            [TileID.HardenedSand] = new DebrisMaterial("Песчаник", TileID.HardenedSand, ItemID.HardenedSand, 1.8f, DustID.Sand),
            [TileID.CorruptHardenedSand] = new DebrisMaterial("Порочный песчаник", TileID.CorruptHardenedSand, ItemID.CorruptHardenedSand, 1.8f, DustID.CorruptPlants),
            [TileID.CrimsonHardenedSand] = new DebrisMaterial("Багровый песчаник", TileID.CrimsonHardenedSand, ItemID.CrimsonHardenedSand, 1.8f, DustID.CrimsonPlants),
            [TileID.HallowHardenedSand] = new DebrisMaterial("Священный песчаник", TileID.HallowHardenedSand, ItemID.HallowHardenedSand, 1.8f, DustID.HallowedPlants),

            [TileID.IceBlock] = new DebrisMaterial("Лёд", TileID.IceBlock, ItemID.IceBlock, 2.0f, DustID.Ice),
            // У порочного и кровавого льда нет отдельных ID предметов - переиспользуем текстуру обычного льда.
            [TileID.CorruptIce] = new DebrisMaterial("Порочный лёд", TileID.CorruptIce, ItemID.IceBlock, 2.0f, DustID.Ice),
            [TileID.FleshIce] = new DebrisMaterial("Кровавый лёд", TileID.FleshIce, ItemID.IceBlock, 2.0f, DustID.Blood),
            // Священного льда как отдельного тайла в ванильной Terraria не существует, поэтому строка убрана.
        };

        public static bool TryGet(int tileType, out DebrisMaterial material)
        {
            return SoftTiles.TryGetValue(tileType, out material);
        }
    }
}
