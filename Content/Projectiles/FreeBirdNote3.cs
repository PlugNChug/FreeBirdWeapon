using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FreeBirdWeapon.Content.Projectiles
{
    public class FreeBirdNote3 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 24;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0f;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = 2;
            Projectile.alpha = 55;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        // Basic Homing AI and tile bouncing taken from the Example Mod, with some additions based on Terraria source code
        public override void AI()
        {
            float maxDetectRadius = 400f;
            float projSpeed;
            if (Projectile.velocity.Length() < 6)
                projSpeed = 6f;
            else
                projSpeed = Projectile.velocity.Length();

            int x = (int)(Projectile.Center.X / 16f);
            int y = (int)(Projectile.Center.Y / 16f);

            if (WorldGen.InWorld(x, y) && Main.tile[x, y] != null && Main.tile[x, y].LiquidAmount == byte.MaxValue && Main.tile[x, y].LiquidType == LiquidID.Shimmer && WorldGen.InWorld(x, y - 1) && Main.tile[x, y - 1] != null && Main.tile[x, y - 1].LiquidAmount > 0 && Main.tile[x, y - 1].LiquidType == LiquidID.Shimmer)
            {
                if (Projectile.timeLeft > 20)
                    Projectile.timeLeft = 20;
            }

            if (Projectile.timeLeft < 20)
                Projectile.alpha += 10;

            Projectile.spriteDirection = -Projectile.direction;
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Pixie, 0f, 0f, 80);
                Main.dust[dust].noGravity = true;
                Dust dust2 = Main.dust[dust];
                dust2.velocity *= 0.2f;
            }

            NPC closestNPC = FindClosestNPC(maxDetectRadius);
            if (closestNPC == null)
                return;

            Vector2 goTo = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            Projectile.velocity = (Projectile.velocity * 10f + goTo * projSpeed) / 11f;
            // Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;

            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            foreach (var target in Main.ActiveNPCs)
            {
                if (target.CanBeChasedBy(this, ignoreDontTakeDamage: true) && !target.dontTakeDamage && Collision.CanHit(Projectile.Center, Projectile.width, Projectile.height, target.position, target.width, target.height))
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                }
            }

            return closestNPC;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            {
                Projectile.velocity.X = -oldVelocity.X;
            }

            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            {
                Projectile.velocity.Y = -oldVelocity.Y;
            }

            return false;
        }
    }
}
