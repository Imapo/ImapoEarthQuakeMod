using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ImapoRealisticEarthquake.Common.Players;

namespace ImapoRealisticEarthquake.Common.Systems
{
    // Отвечает за пункт 2: рисуем полупрозрачную "пыльную" дымку по краям экрана,
    // пока у локального игрока активен дебафф DustyHazeDebuff ("снижение видимости").
    // Также рисует бонусный HUD сейсмографа.
    public class EarthquakeVisualsSystem : ModSystem
    {
        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return;

            EarthquakePlayer eqPlayer = player.GetModPlayer<EarthquakePlayer>();

            DrawDustyHaze(spriteBatch, eqPlayer);
            DrawSeismographHud(spriteBatch, eqPlayer);
        }

        private void DrawDustyHaze(SpriteBatch spriteBatch, EarthquakePlayer eqPlayer)
        {
            if (eqPlayer.DustyHazeTimeLeft <= 0)
                return;

            // Максимум дымки - примерно 180 тиков (3 секунды), плавно затухает к концу.
            float strength = MathHelper.Clamp(eqPlayer.DustyHazeTimeLeft / 180f, 0f, 1f);
            float alpha = MathHelper.Lerp(0f, 0.55f, strength);

            Color hazeColor = new Color(150, 120, 90) * alpha; // пыльно-коричневый оттенок

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle fullScreen = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(pixel, fullScreen, hazeColor);
            spriteBatch.End();
        }

        // БОНУС: если у игрока надет сейсмограф - показываем в углу экрана текущую фазу/обратный отсчёт.
        private void DrawSeismographHud(SpriteBatch spriteBatch, EarthquakePlayer eqPlayer)
        {
            if (!eqPlayer.HasSeismograph)
                return;

            string text = EarthquakeSystem.CurrentState switch
            {
                EarthquakeState.Idle => $"Сейсмограф: спокойно ({FormatTime(EarthquakeSystem.TicksUntilNextEarthquake)} до толчков)",
                EarthquakeState.Warning => $"Сейсмограф: НАРАСТАЮЩИЙ ГУЛ! Толчки через {FormatTime(EarthquakeSystem.TicksRemainingInState)}",
                EarthquakeState.Main => $"Сейсмограф: ЗЕМЛЕТРЯСЕНИЕ! Магнитуда {EarthquakeSystem.CurrentMagnitude}/10",
                EarthquakeState.Aftershock => "Сейсмограф: возможны афтершоки",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(text))
                return;

            Vector2 pos = new Vector2(16, 100);
            Color color = EarthquakeSystem.CurrentState == EarthquakeState.Main ? Color.OrangeRed : Color.LightGoldenrodYellow;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            Terraria.Utils.DrawBorderString(spriteBatch, text, pos, color, 0.9f);
            spriteBatch.End();
        }

        private static string FormatTime(int ticks)
        {
            int totalSeconds = System.Math.Max(0, ticks / 60);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
