using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using ImapoRealisticEarthquake.Common.Configs;
using ImapoRealisticEarthquake.Common.Utils;
using ImapoRealisticEarthquake.Content.Projectiles;

namespace ImapoRealisticEarthquake.Common.Systems
{
    public enum EarthquakeState : byte
    {
        Idle,
        Warning,    // гул нарастает, пыль сыпется чаще, самой тряски ещё толком нет (пункт 3, 4)
        Main,       // основное землетрясение: тряска + падение обломков (пункт 1, 9)
        Aftershock  // период слабых повторных толчков (пункт 5)
    }

    public class EarthquakeSystem : ModSystem
    {
        // ==== Публичное состояние, читаемое UI и другими системами ====
        public static EarthquakeState CurrentState { get; private set; } = EarthquakeState.Idle;
        public static float CurrentShakeIntensity;
        public static int CurrentMagnitude;              // 1..10
        public static int TicksRemainingInState;
        public static int TicksUntilNextEarthquake;       // актуально только в Idle (для бонусного сейсмографа)

        private const int TicksPerSecond = 60;

        private int rumbleTickCounter;
        private int dustTickTimer;
        private int ticksUntilNextDebrisSpawn;

        private int aftershockPeriodTicksLeft;
        private int ticksUntilNextAftershockBurst;
        private int aftershockBurstTicksLeft;

        public override void OnWorldLoad()
        {
            ResetToIdle();
        }

        private void ResetToIdle()
        {
            CurrentState = EarthquakeState.Idle;
            CurrentShakeIntensity = 0f;
            TicksUntilNextEarthquake = RollNextEarthquakeDelay();
        }

        private int RollNextEarthquakeDelay()
        {
            EarthquakeConfig config = ModContent.GetInstance<EarthquakeConfig>();
            float avgMinutes = config.AverageIntervalMinutes;
            float variance = avgMinutes * 0.3f;
            float minutes = Math.Max(1f, avgMinutes + Main.rand.NextFloat(-variance, variance));
            return (int)(minutes * 60f * TicksPerSecond);
        }

        public override void PostUpdateEverything()
        {
            // "Часы" землетрясения ведёт только сервер (в мультиплеере) или единственный клиент (в одиночной игре).
            // Обычные клиенты в мультиплеере получают состояние по сети (см. ReceiveState) и просто применяют эффекты.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                TickAuthoritative();
            }

            ApplyLocalEffectsEveryClient();

            if (CurrentShakeIntensity > 0f)
                CurrentShakeIntensity = Math.Max(0f, CurrentShakeIntensity - 0.05f); // плавное затухание тряски
        }

        private void TickAuthoritative()
        {
            switch (CurrentState)
            {
                case EarthquakeState.Idle:
                    TicksUntilNextEarthquake--;
                    if (TicksUntilNextEarthquake <= 0)
                        StartWarning();
                    break;

                case EarthquakeState.Warning:
                    TicksRemainingInState--;
                    if (TicksRemainingInState <= 0)
                        StartMainQuake();
                    break;

                case EarthquakeState.Main:
                    TicksRemainingInState--;
                    RunMainQuakeTick();
                    if (TicksRemainingInState <= 0)
                        EndMainQuakeGoToAftershocks();
                    break;

                case EarthquakeState.Aftershock:
                    RunAftershockTick();
                    break;
            }
        }

        // ================= ЗАПУСК ФАЗ =================

        public void StartWarning(int? forcedMagnitude = null, bool manual = false)
        {
            if (CurrentState == EarthquakeState.Main)
                return; // уже трясёт, не перезапускаем поверх

            EarthquakeConfig config = ModContent.GetInstance<EarthquakeConfig>();
            CurrentMagnitude = forcedMagnitude ?? Main.rand.Next(3, config.MaxMagnitude + 1);
            CurrentState = EarthquakeState.Warning;

            // 10-15 секунд гула перед началом (пункт 3). Для ручного теста делаем короче, чтобы не ждать.
            TicksRemainingInState = manual
                ? Main.rand.Next(3, 6) * TicksPerSecond
                : Main.rand.Next(10, 16) * TicksPerSecond;

            NetSync();
        }

        private void StartMainQuake()
        {
            CurrentState = EarthquakeState.Main;
            TicksRemainingInState = Main.rand.Next(15, 21) * TicksPerSecond; // 15-20 секунд (пункт 1)
            ticksUntilNextDebrisSpawn = 0;
            rumbleTickCounter = 0;

            EarthquakeConfig config = ModContent.GetInstance<EarthquakeConfig>();
            if (config.ShowChatMessages)
                BroadcastMessage($"Земля начинает трястись под ногами... Магнитуда: {CurrentMagnitude}/10!", new Color(235, 140, 50));

            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.7f, Volume = 0.9f });

            NetSync();
        }

        private void EndMainQuakeGoToAftershocks()
        {
            EarthquakeConfig config = ModContent.GetInstance<EarthquakeConfig>();

            if (config.EnableAftershocks)
            {
                CurrentState = EarthquakeState.Aftershock;
                aftershockPeriodTicksLeft = config.AftershockPeriodSeconds * TicksPerSecond;
                ticksUntilNextAftershockBurst = Main.rand.Next(5, 20) * TicksPerSecond;
                TicksRemainingInState = aftershockPeriodTicksLeft;

                if (config.ShowChatMessages)
                    BroadcastMessage("Основные толчки стихают... но земля ещё может вздрогнуть.", new Color(180, 200, 140));
            }
            else
            {
                EndEverything();
            }

            NetSync();
        }

        private void EndEverything()
        {
            EarthquakeConfig config = ModContent.GetInstance<EarthquakeConfig>();
            if (config.ShowChatMessages)
                BroadcastMessage("Землетрясение закончилось.", new Color(140, 220, 140)); // пункт 8

            ResetToIdle();
            NetSync();
        }

        // ================= ОСНОВНАЯ ФАЗА =================

        private void RunMainQuakeTick()
        {
            EarthquakeConfig config = ModContent.GetInstance<EarthquakeConfig>();

            // Сила тряски экрана пропорциональна магнитуде, но ограничена разумным пределом (пункт 9).
            CurrentShakeIntensity = MathHelper.Clamp(CurrentMagnitude * 0.75f, 1.5f, 11f);

            ticksUntilNextDebrisSpawn--;
            if (ticksUntilNextDebrisSpawn <= 0)
            {
                float debrisPerSecond = 1.2f + CurrentMagnitude * 0.55f; // выше магнитуда - чаще обломки
                ticksUntilNextDebrisSpawn = Math.Max(4, (int)(TicksPerSecond / debrisPerSecond));

                SpawnDebrisNearAllPlayers(chancePerPlayer: 0.8f, maxPerPlayer: 1 + CurrentMagnitude / 5);
            }
        }

        private void RunAftershockTick()
        {
            aftershockPeriodTicksLeft--;
            TicksRemainingInState = aftershockPeriodTicksLeft;

            ticksUntilNextAftershockBurst--;
            if (ticksUntilNextAftershockBurst <= 0)
            {
                ticksUntilNextAftershockBurst = Main.rand.Next(8, 25) * TicksPerSecond;
                StartAftershockBurst();
            }

            if (aftershockPeriodTicksLeft <= 0)
                EndEverything();
        }

        private void StartAftershockBurst()
        {
            aftershockBurstTicksLeft = Main.rand.Next(2, 5) * TicksPerSecond;
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -1.1f, Volume = 0.5f });
            SpawnDebrisNearAllPlayers(chancePerPlayer: 0.3f, maxPerPlayer: 2);
        }

        // ================= ЭФФЕКТЫ, ПРИМЕНЯЕМЫЕ КАЖДЫМ КЛИЕНТОМ ЛОКАЛЬНО =================

        private void ApplyLocalEffectsEveryClient()
        {
            if (CurrentState == EarthquakeState.Aftershock && aftershockBurstTicksLeft > 0)
            {
                aftershockBurstTicksLeft--;
                float burstShake = MathHelper.Clamp(CurrentMagnitude * 0.35f, 0.5f, 4f);
                CurrentShakeIntensity = Math.Max(CurrentShakeIntensity, burstShake);
            }

            HandleRumbleSound();
            HandleWarningDust();
        }

        private void HandleRumbleSound()
        {
            if (CurrentState != EarthquakeState.Warning)
            {
                rumbleTickCounter = 0;
                return;
            }

            rumbleTickCounter++;
            if (rumbleTickCounter % 45 != 0) // играем импульс гула примерно раз в 0.75 сек
                return;

            float totalWarningTicks = 15f * TicksPerSecond;
            float progress = 1f - MathHelper.Clamp(TicksRemainingInState / totalWarningTicks, 0f, 1f);
            float volume = MathHelper.Lerp(0.15f, 0.9f, progress);
            float pitch = MathHelper.Lerp(-0.6f, -1f, progress);

            // Имитация нарастающего низкочастотного гула стандартным звуком на пониженной высоте тона (пункт 3).
            // Чтобы использовать собственный сэмпл, положите файл в Assets/Sounds/Rumble.ogg и замените строку ниже на:
            // SoundEngine.PlaySound(new SoundStyle("RealisticEarthquake/Assets/Sounds/Rumble") { Volume = volume, Pitch = pitch });
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = pitch, Volume = volume });
        }

        private void HandleWarningDust()
        {
            bool activePhase = CurrentState == EarthquakeState.Warning || CurrentState == EarthquakeState.Main;
            if (!activePhase)
                return;

            dustTickTimer++;

            foreach (Player player in Main.ActivePlayers)
            {
                bool indoors = CeilingScanner.IsIndoors(player);
                int interval = indoors ? 6 : 16; // в помещении/пещере пыль падает чаще (пункт 4)

                if (dustTickTimer % interval != 0)
                    continue;

                Vector2 searchOrigin = player.Center + new Vector2(Main.rand.Next(-200, 200), 0);
                if (CeilingScanner.TryFindCrumblingCeiling(searchOrigin, out Point ceilingTile, out DebrisMaterial mat))
                {
                    Vector2 pos = new Vector2(ceilingTile.X * 16 + 8, ceilingTile.Y * 16 + 16);
                    Dust.NewDustPerfect(pos, mat.DustType, new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(0.5f, 1.5f)), 150, default, 0.9f);
                }
            }
        }

        // ================= СПАВН ОБЛОМКОВ (пункты 1, 6, 7) =================

        private void SpawnDebrisNearAllPlayers(float chancePerPlayer, int maxPerPlayer)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return; // обломки спавнит только сервер/синглплеер, дальше они синхронизируются как обычные projectile

            EarthquakeConfig config = ModContent.GetInstance<EarthquakeConfig>();

            foreach (Player player in Main.ActivePlayers)
            {
                if (Main.rand.NextFloat() > chancePerPlayer)
                    continue;

                int spawned = 0;
                for (int attempt = 0; attempt < 5 && spawned < maxPerPlayer; attempt++)
                {
                    float xOffset = Main.rand.NextFloat(-350f, 350f);
                    Vector2 searchOrigin = player.Center + new Vector2(xOffset, -50f);

                    if (!CeilingScanner.TryFindCrumblingCeiling(searchOrigin, out Point ceilingTile, out DebrisMaterial mat))
                        continue; // либо укрытие (камень+), либо открытое небо - обломков нет

                    Vector2 spawnPos = new Vector2(ceilingTile.X * 16 + 8, ceilingTile.Y * 16 + 16);

                    // Чем твёрже порода (mat.Hardness) и выше магнитуда - тем больше урон (пункт 7, 9).
                    float baseDamage = 16f * mat.Hardness * config.DebrisDamageMultiplier * (1f + CurrentMagnitude * 0.07f);

                    int proj = Projectile.NewProjectile(
                        Entity.GetSource_NaturalSpawn(),
                        spawnPos,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), 1f),
                        ModContent.ProjectileType<FallingDebris>(),
                        (int)baseDamage,
                        3f,
                        Main.myPlayer);

                    if (Main.projectile[proj].ModProjectile is FallingDebris debris)
                    {
                        debris.MaterialTileType = mat.TileType;
                        debris.SpinSpeed = Main.rand.NextFloat(-0.2f, 0.2f);
                        Main.projectile[proj].netUpdate = true;
                    }

                    spawned++;
                }
            }
        }

        // ================= РУЧНОЙ ВЫЗОВ (пункт 11) =================

        public void ManualTrigger(int magnitude)
        {
            magnitude = Math.Clamp(magnitude, 1, 10);
            StartWarning(forcedMagnitude: magnitude, manual: true);
        }

        // ================= СЕТЬ (упрощённая синхронизация для мультиплеера) =================

        private void NetSync()
        {
            if (Main.netMode == NetmodeID.Server)
                EarthquakeNetHandler.SendState();
        }

        public static void ReceiveState(EarthquakeState state, int magnitude, int ticksRemaining)
        {
            CurrentState = state;
            CurrentMagnitude = magnitude;
            TicksRemainingInState = ticksRemaining;
        }

        // ================= ВСПОМОГАТЕЛЬНОЕ =================

        private void BroadcastMessage(string text, Color color)
        {
            if (Main.netMode == NetmodeID.Server)
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
            else
                Main.NewText(text, color); // пункт 8
        }
    }
}
