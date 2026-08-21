using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ImapoRealisticEarthquake.Common.Configs;

namespace ImapoRealisticEarthquake.Content.UI
{
    public class EarthquakeUISystem : ModSystem
    {
        internal EarthquakeUIState UiState;
        private UserInterface userInterface;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            UiState = new EarthquakeUIState();
            userInterface = new UserInterface();
            userInterface.SetState(UiState);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.dedServ || !ModContent.GetInstance<EarthquakeConfig>().ShowDebugButton)
                return;

            userInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.dedServ || !ModContent.GetInstance<EarthquakeConfig>().ShowDebugButton)
                return;

            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex == -1)
                return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "RealisticEarthquake: Кнопка теста",
                delegate
                {
                    userInterface?.Draw(Main.spriteBatch, new GameTime());
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
