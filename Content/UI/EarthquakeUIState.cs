using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using ImapoRealisticEarthquake.Common.Systems;

namespace ImapoRealisticEarthquake.Content.UI
{
    // Небольшая кнопка сбоку экрана, при нажатии разворачивается в панель выбора магнитуды (пункт 11).
    public class EarthquakeUIState : UIState
    {
        private UIPanel togglePanel;
        private UIPanel expandedPanel;
        private UIText magnitudeValueText;
        private int selectedMagnitude = 5;
        private bool expanded;

        public override void OnInitialize()
        {
            // --- Кнопка-переключатель, всегда видна в углу экрана ---
            togglePanel = new UIPanel();
            togglePanel.Width.Set(44, 0);
            togglePanel.Height.Set(44, 0);
            togglePanel.HAlign = 1f;
            togglePanel.Top.Set(170, 0f);
            togglePanel.Left.Set(-12, 1f);
            togglePanel.BackgroundColor = new Color(60, 40, 30);
            togglePanel.OnLeftClick += (evt, el) => ToggleExpanded();

            UIText icon = new UIText("EQ", 0.9f) { HAlign = 0.5f, VAlign = 0.5f };
            togglePanel.Append(icon);
            Append(togglePanel);

            // --- Разворачиваемая панель настроек теста ---
            expandedPanel = new UIPanel();
            expandedPanel.Width.Set(200, 0);
            expandedPanel.Height.Set(150, 0);
            expandedPanel.HAlign = 1f;
            expandedPanel.Top.Set(170, 0f);
            expandedPanel.Left.Set(-64, 1f);
            expandedPanel.BackgroundColor = new Color(40, 30, 25);

            UIText title = new UIText("Тест землетрясения", 0.8f) { HAlign = 0.5f };
            title.Top.Set(6, 0f);
            expandedPanel.Append(title);

            UIText subtitle = new UIText("Магнитуда:", 0.75f);
            subtitle.Top.Set(38, 0f);
            subtitle.Left.Set(15, 0f);
            expandedPanel.Append(subtitle);

            UIText minusBtn = new UIText("[ - ]", 0.9f);
            minusBtn.Top.Set(65, 0f);
            minusBtn.Left.Set(15, 0f);
            minusBtn.OnLeftClick += (evt, el) => { selectedMagnitude = System.Math.Max(1, selectedMagnitude - 1); UpdateMagnitudeText(); };
            minusBtn.OnMouseOver += (evt, el) => minusBtn.TextColor = Color.Yellow;
            minusBtn.OnMouseOut += (evt, el) => minusBtn.TextColor = Color.White;
            expandedPanel.Append(minusBtn);

            magnitudeValueText = new UIText(selectedMagnitude.ToString(), 1.1f);
            magnitudeValueText.Top.Set(62, 0f);
            magnitudeValueText.Left.Set(90, 0f);
            expandedPanel.Append(magnitudeValueText);

            UIText plusBtn = new UIText("[ + ]", 0.9f);
            plusBtn.Top.Set(65, 0f);
            plusBtn.Left.Set(140, 0f);
            plusBtn.OnLeftClick += (evt, el) => { selectedMagnitude = System.Math.Min(10, selectedMagnitude + 1); UpdateMagnitudeText(); };
            plusBtn.OnMouseOver += (evt, el) => plusBtn.TextColor = Color.Yellow;
            plusBtn.OnMouseOut += (evt, el) => plusBtn.TextColor = Color.White;
            expandedPanel.Append(plusBtn);

            UIPanel triggerBtn = new UIPanel();
            triggerBtn.Width.Set(170, 0);
            triggerBtn.Height.Set(32, 0);
            triggerBtn.Top.Set(100, 0f);
            triggerBtn.Left.Set(15, 0f);
            triggerBtn.BackgroundColor = new Color(120, 60, 40);
            triggerBtn.OnLeftClick += (evt, el) => TriggerEarthquake();
            triggerBtn.OnMouseOver += (evt, el) => triggerBtn.BackgroundColor = new Color(160, 80, 50);
            triggerBtn.OnMouseOut += (evt, el) => triggerBtn.BackgroundColor = new Color(120, 60, 40);

            UIText triggerLabel = new UIText("Вызвать!", 0.85f) { HAlign = 0.5f, VAlign = 0.5f };
            triggerBtn.Append(triggerLabel);
            expandedPanel.Append(triggerBtn);
        }

        private void UpdateMagnitudeText() => magnitudeValueText.SetText(selectedMagnitude.ToString());

        private void ToggleExpanded()
        {
            expanded = !expanded;
            if (expanded)
                Append(expandedPanel);
            else
                RemoveChild(expandedPanel);
        }

        private void TriggerEarthquake()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                EarthquakeNetHandler.SendManualTriggerRequest(selectedMagnitude);
            else
                ModContent.GetInstance<EarthquakeSystem>().ManualTrigger(selectedMagnitude);

            Main.NewText($"[Тест] Запрошено землетрясение магнитудой {selectedMagnitude}", Color.Yellow);
        }
    }
}
