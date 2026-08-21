using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ImapoRealisticEarthquake.Common.Players;

namespace ImapoRealisticEarthquake.Content.Items
{
    // БОНУС: аксессуар-сейсмограф. Пока надет - показывает на экране обратный отсчёт до следующего
    // землетрясения и предупреждает о начавшейся фазе гула чуть раньше остальных (см. EarthquakeVisualsSystem).
    // Текстуру временно переиспользуем от "Золотых часов" (тематически похоже - тоже "прибор с отсчётом").
    // Замените Texture на свой спрайт, когда он появится: "RealisticEarthquake/Content/Items/Seismograph".
    public class Seismograph : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.GoldWatch;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Blue;
            Item.maxStack = 1;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<EarthquakePlayer>().HasSeismograph = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 15)
                .AddIngredient(ItemID.IronBar, 5)
                .AddIngredient(ItemID.GoldWatch, 1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
