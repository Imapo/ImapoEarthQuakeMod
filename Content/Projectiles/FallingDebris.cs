using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ImapoRealisticEarthquake.Common.Utils;
using ImapoRealisticEarthquake.Content.Buffs;

namespace ImapoRealisticEarthquake.Content.Projectiles
{
    // Обломок породы, падающий с "потолка". Гравитация, отскок от стен, урон при падении,
    // облако пыли + дебафф при разрушении о землю. Текстуру берём готовую - иконку соответствующего
    // блока-предмета (ItemID), чтобы не рисовать собственный арт.
    public class FallingDebris : ModProjectile
    {
        // Чтобы не плодить кучу классов - индекс материала храним в ai[0] (TileID материала).
        public int MaterialTileType
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        // Случайное вращение куска для визуального разнообразия, хранится в ai[1] как скорость вращения.
        public float SpinSpeed
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override string Texture => "Terraria/Images/Item_0"; // переопределяется в PreDraw, реальная текстура тут не важна

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;   // наносит урон игрокам
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600; // 10 секунд на существование, если ни во что не попадёт
            Projectile.aiStyle = -1;   // своя логика ниже
        }

        private const float Gravity = 0.35f;
        private const float MaxFallSpeed = 11f;
        private bool hasLanded;

        public override void AI()
        {
            // Гравитация
            if (Projectile.velocity.Y < MaxFallSpeed)
                Projectile.velocity.Y = System.Math.Min(Projectile.velocity.Y + Gravity, MaxFallSpeed);

            // Вращение обломка при полёте
            Projectile.rotation += SpinSpeed;

            // Небольшой пылевой след при падении
            if (Main.rand.NextBool(3) && DebrisMaterials.TryGet(MaterialTileType, out var mat))
            {
                Dust.NewDustPerfect(Projectile.Center, mat.DustType, Projectile.velocity * 0.1f, 100, default, 0.8f);
            }
        }

        // Отскок от стен/потолка, оставаясь при этом падающим вниз (пункт 1: "отскакивают от стен")
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (hasLanded)
                return false;

            bool hitWallX = Projectile.velocity.X != oldVelocity.X;
            bool hitWallY = Projectile.velocity.Y != oldVelocity.Y;

            if (hitWallX)
                Projectile.velocity.X = -oldVelocity.X * 0.4f; // слабый отскок по горизонтали

            if (hitWallY)
            {
                // Если падал вниз и врезался в пол снизу - это приземление, обломок разбивается.
                if (oldVelocity.Y > 0)
                {
                    Land();
                    return false;
                }
                Projectile.velocity.Y = -oldVelocity.Y * 0.2f;
            }

            return false;
        }

        private void Land()
        {
            if (hasLanded)
                return;
            hasLanded = true;

            if (DebrisMaterials.TryGet(MaterialTileType, out var mat))
            {
                SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

                // Облако пыли при разрушении (пункт 2)
                for (int i = 0; i < 18; i++)
                {
                    Dust d = Dust.NewDustDirect(Projectile.position - new Vector2(8, 8), Projectile.width + 16, Projectile.height + 16, mat.DustType,
                        Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-2f, 0f), 100, default, 1.3f);
                    d.noGravity = Main.rand.NextBool();
                }

                // Дебафф "пыльная завеса" всем игрокам поблизости от места падения
                foreach (Player player in Main.ActivePlayers)
                {
                    if (Vector2.Distance(player.Center, Projectile.Center) < 250f)
                    {
                        player.AddBuff(ModContent.BuffType<DustyHazeDebuff>(), 180); // 3 секунды
                    }
                }
            }

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!hasLanded && DebrisMaterials.TryGet(MaterialTileType, out var mat))
            {
                for (int i = 0; i < 6; i++)
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, mat.DustType);
            }
        }

        // Урон при столкновении с игроком, зависящий от твёрдости материала (пункт 7)
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Land();
        }

        // Рисуем обломок, используя готовую иконку предмета соответствующего блока - без необходимости в своём арте.
        public override bool PreDraw(ref Color lightColor)
        {
            if (!DebrisMaterials.TryGet(MaterialTileType, out var mat))
                return false;

            var itemTexture = TextureAssets.Item[mat.TextureItemID];
            if (!itemTexture.IsLoaded)
                return false;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = itemTexture.Size() / 2f;
            float scale = 0.55f; // помельче, чтобы выглядело как "обломок", а не целый блок

            Main.EntitySpriteDraw(itemTexture.Value, drawPos, null, lightColor, Projectile.rotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
