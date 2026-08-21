using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ImapoRealisticEarthquake.Common.Configs
{
    // ServerSide - чтобы в мультиплеере у всех была одна и та же настройка (её определяет хост/сервер).
    public class EarthquakeConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("ЧастотаЗемлетрясений")]

        [DefaultValue(20f)]
        [Range(5f, 60f)]
        [Increment(1f)]
        [Slider]
        [Tooltip("Среднее время между землетрясениями, в минутах.\nРеальный интервал будет случайно колебаться на ±30% от этого значения, чтобы землетрясения не были предсказуемыми по таймеру.")]
        public float AverageIntervalMinutes;

        [Header("Магнитуда")]

        [DefaultValue(9)]
        [Range(3, 10)]
        [Tooltip("Максимальная случайная магнитуда, которая может выпасть (по 10-балльной шкале).\nМинимальная магнитуда всегда 3.")]
        public int MaxMagnitude;

        [Header("Афтершоки")]

        [DefaultValue(true)]
        [Tooltip("Включить слабые повторные толчки (афтершоки) после основного землетрясения.")]
        public bool EnableAftershocks;

        [DefaultValue(150)]
        [Range(30, 300)]
        [Tooltip("Примерная продолжительность периода афтершоков после основного землетрясения, в секундах.")]
        public int AftershockPeriodSeconds;

        [Header("ИнтерфейсИОповещения")]

        [DefaultValue(true)]
        [Tooltip("Показывать сообщения в чат о начале и конце землетрясения.")]
        public bool ShowChatMessages;

        [DefaultValue(true)]
        [Tooltip("Показывать на экране кнопку ручного вызова землетрясения (удобно для тестирования).\nВидна только вам, на геймплей других игроков не влияет.")]
        public bool ShowDebugButton;

        [Header("УронИРазрушения")]

        [DefaultValue(1f)]
        [Range(0.1f, 3f)]
        [Slider]
        [Tooltip("Общий множитель урона от падающих обломков. 1.0 - стандартный урон.")]
        public float DebrisDamageMultiplier;

        [DefaultValue(true)]
        [Tooltip("Бонус: во время землетрясения приглушать фоновую музыку и делать акцент на гуле/грохоте.")]
        public bool ImmersiveAudioDucking;
    }
}
