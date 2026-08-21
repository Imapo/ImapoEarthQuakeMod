using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ImapoRealisticEarthquake.Common.Utils
{
    // Отвечает за пункт 6: определяем, есть ли над игроком "рыхлый" потолок,
    // который может осыпаться, или там камень/что-то прочнее (укрытие).
    public static class CeilingScanner
    {
        // Сколько тайлов вверх максимум сканируем в поисках потолка.
        private const int MaxScanTiles = 70;

        /// <summary>
        /// Ищет ближайший сплошной тайл над указанной мировой точкой.
        /// Возвращает true и материал, только если этот тайл - "рыхлая" порода.
        /// Если первый найденный сплошной тайл - камень/руда/что угодно твёрже - возвращает false (это укрытие).
        /// Если тайлов вообще нет (открытое небо) - тоже false (падать неоткуда).
        /// </summary>
        public static bool TryFindCrumblingCeiling(Vector2 worldPos, out Point ceilingTile, out DebrisMaterial material)
        {
            material = default;
            ceilingTile = default;

            int tileX = (int)(worldPos.X / 16f);
            int tileY = (int)(worldPos.Y / 16f);

            if (tileX < 10 || tileX > Main.maxTilesX - 10 || tileY < 10)
                return false;

            for (int y = tileY - 1; y > tileY - MaxScanTiles && y > 10; y--)
            {
                if (!WorldGen.InWorld(tileX, y))
                    break;

                Tile tile = Main.tile[tileX, y];
                if (tile == null || !tile.HasTile)
                    continue;

                if (!Main.tileSolid[tile.TileType])
                    continue; // платформы, факелы и т.д. не считаются потолком

                // Нашли первый сплошной тайл сверху - это и есть "потолок" над этой колонкой.
                if (DebrisMaterials.TryGet(tile.TileType, out material))
                {
                    ceilingTile = new Point(tileX, y);
                    return true;
                }

                // Тайл твёрдый (камень или прочнее) - надёжное укрытие, ничего не осыпается.
                return false;
            }

            return false; // потолка не нашли вообще (открытое небо)
        }

        /// <summary>
        /// Грубая эвристика "находится ли игрок в помещении" (пункт 4):
        /// если сплошной потолок находится относительно близко над головой - считаем, что это помещение/пещера,
        /// частицы пыли там должны падать чаще.
        /// </summary>
        public static bool IsIndoors(Player player)
        {
            int tileX = (int)(player.Center.X / 16f);
            int tileY = (int)(player.Center.Y / 16f);

            for (int y = tileY - 1; y > tileY - 20 && y > 10; y--)
            {
                if (!WorldGen.InWorld(tileX, y))
                    break;

                Tile tile = Main.tile[tileX, y];
                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                    return true; // потолок ближе 20 тайлов - считаем помещением/пещерой
            }

            return false;
        }
    }
}
