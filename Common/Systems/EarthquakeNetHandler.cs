using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ImapoRealisticEarthquake.Common.Systems
{
    // Простая сеть: сервер широковещательно рассылает текущее состояние землетрясения,
    // клиенты могут отправить запрос на ручной вызов (кнопка на экране, пункт 11).
    public static class EarthquakeNetHandler
    {
        private const byte PacketType_StateSync = 0;
        private const byte PacketType_ManualTriggerRequest = 1;

        public static void SendState()
        {
            ModPacket packet = ModContent.GetInstance<ImapoRealisticEarthquakeMod>().GetPacket();
            packet.Write(PacketType_StateSync);
            packet.Write((byte)EarthquakeSystem.CurrentState);
            packet.Write((byte)EarthquakeSystem.CurrentMagnitude);
            packet.Write(EarthquakeSystem.TicksRemainingInState);
            packet.Send();
        }

        public static void SendManualTriggerRequest(int magnitude)
        {
            ModPacket packet = ModContent.GetInstance<ImapoRealisticEarthquakeMod>().GetPacket();
            packet.Write(PacketType_ManualTriggerRequest);
            packet.Write((byte)magnitude);
            packet.Send();
        }

        public static void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte packetType = reader.ReadByte();

            switch (packetType)
            {
                case PacketType_StateSync:
                    {
                        var state = (EarthquakeState)reader.ReadByte();
                        int magnitude = reader.ReadByte();
                        int ticksRemaining = reader.ReadInt32();
                        EarthquakeSystem.ReceiveState(state, magnitude, ticksRemaining);
                        break;
                    }

                case PacketType_ManualTriggerRequest:
                    {
                        int magnitude = reader.ReadByte();
                        if (Main.netMode == NetmodeID.Server)
                            ModContent.GetInstance<EarthquakeSystem>().ManualTrigger(magnitude);
                        break;
                    }
            }
        }
    }
}
