using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ImapoRealisticEarthquake.Content.Buffs
{
    // Дебафф "Пыльная завеса": сам по себе не наносит урона, служит флагом,
    // по которому EarthquakeVisualsSystem рисует затемнение/дымку на экране (см. пункт 2 ТЗ).
    public class DustyHazeDebuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Obstructed; // временно используем вид дебаффа "затруднённое дыхание", можно заменить своей иконкой

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Лёгкое ослабление обзора: слегка снижаем радиус освещения игрока,
            // визуально это читается как "пыль вокруг, видно хуже".
            player.GetModPlayer<Common.Players.EarthquakePlayer>().DustyHazeTimeLeft =
                System.Math.Max(player.GetModPlayer<Common.Players.EarthquakePlayer>().DustyHazeTimeLeft, player.buffTime[buffIndex]);
        }
    }
}
