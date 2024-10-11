using FreeBirdWeapon.Common.Config;
using FreeBirdWeapon.Common.Player;
using FreeBirdWeapon.Content.Projectiles;
using log4net.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using static Terraria.ModLoader.ModContent;

namespace FreeBirdWeapon.Content.Items.Weapons
{
    public class FreeBirdGuitar : ModItem
    {
        public const int USECONSTANT = 15;

        bool altMode = false;
        // int lightCounter = 0;
        int delay = USECONSTANT;
        int delay2 = 6;

        string tooltipModePart1;
        string tooltipModePart2;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Find the line that contains the dummy string from the localization text
            int index = tooltips.FindIndex(static line => line.Text.Contains("<MODE>"));
            if (index >= 0)
            {
                // ... and then replace it
                ref string text = ref tooltips[index].Text;
                text = text.Replace("<MODE>", $"{tooltipModePart1}");
            }

            index = tooltips.FindIndex(static line => line.Text.Contains("<MODE2>"));
            if (index >= 0)
            {
                // ... and then replace it
                ref string text = ref tooltips[index].Text;
                text = text.Replace("<MODE2>", $"{tooltipModePart2}");
            }
        }

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {
            // We call GetDamage() when it's in the player's inventory so it updates constantly.
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.width = 70;
            Item.height = 58;
            Item.scale = 1f;
            Item.useTime = USECONSTANT;
            Item.useAnimation = USECONSTANT;
            Item.holdStyle = ItemHoldStyleID.HoldGuitar;
            Item.useStyle = ItemUseStyleID.Guitar;
            Item.useTurn = false;
            // Item.axe = 40;
            // Item.hammer = 100;
            Item.knockBack = 2;
            Item.value = Item.buyPrice(gold: 75);
            Item.rare = ItemRarityID.Red;
            Item.autoReuse = true;
            tooltipModePart1 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode1Part1");
            tooltipModePart2 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode1Part2");
        }

        public void GetDamage()
        {
            if (GetInstance<GameConfig>().CustomDamage)
            {
                Item.damage = int.Parse(GetInstance<GameConfig>().CustomDamageAmount);
            }
            else if (GetInstance<GameConfig>().ScalingDamage)
            {
                Item.damage = 12;
                if (NPC.downedSlimeKing)
                    Item.damage += 3;
                if (NPC.downedBoss1)
                    Item.damage += 3;
                if (NPC.downedBoss2)
                    Item.damage += 3;
                if (NPC.downedBoss3)
                    Item.damage += 3;
                if (NPC.downedQueenBee || NPC.downedDeerclops)
                    Item.damage += 4;
                if (Main.hardMode)
                    Item.damage += 12; // 40 damage by this point if all prior bosses defeated
                if (NPC.downedQueenSlime)
                    Item.damage += 5;
                if (NPC.downedFishron || NPC.downedEmpressOfLight)  // If-else block accounts for the case where the player wants to skip straight to Fishron in the run
                    Item.damage += 25;  // 75 damage assuming Queen Slime defeated
                else
                {
                    if (NPC.downedMechBoss1)
                        Item.damage += 3;   // 48 damage assuming Queen Slime defeated
                    if (NPC.downedMechBoss1)
                        Item.damage += 3;   // 51 damage
                    if (NPC.downedMechBoss3)
                        Item.damage += 3;   // 54 damage
                    if (NPC.downedPlantBoss)
                        Item.damage += 6;   // 60 damage
                    if (NPC.downedGolemBoss)
                        Item.damage += 10;   // 70 damage
                }
                if (NPC.downedAncientCultist)
                    Item.damage += 10;  // 80 damage by this point if all prior bosses defeated. By this point, this should match the non-damage scaling version of the weapon since it would be around this point that one would get it
                if (NPC.downedMoonlord && Main.hardMode)
                    Item.damage += 70;  // 150 damage, basically makes this an OP endgame weapon
                

            }
            else
            {
                Item.damage = 80;
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["altMode"] = altMode;
        }

        public override void LoadData(TagCompound tag)
        {
            altMode = tag.GetBool("altMode");

            if (altMode)
            {
                Item.axe = 40;
                Item.hammer = 100;
                Item.knockBack = 8;
                Item.tileBoost = 2;
                Item.useTime = 6;
                Item.holdStyle = ItemHoldStyleID.None;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.DamageType = DamageClass.MeleeNoSpeed;
                Item.noUseGraphic = true;
                ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = false;
                tooltipModePart1 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode2Part1");
                tooltipModePart2 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode2Part2");
            }
            else
            {
                Item.axe = 0;
                Item.hammer = 0;
                Item.knockBack = 2;
                Item.tileBoost = 0;
                Item.useTime = USECONSTANT;
                Item.holdStyle = ItemHoldStyleID.HoldGuitar;
                Item.useStyle = ItemUseStyleID.Guitar;
                Item.DamageType = DamageClass.Ranged;
                Item.noUseGraphic = false;
                ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
                tooltipModePart1 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode1Part1");
                tooltipModePart2 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode1Part2");
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(altMode);
            /*writer.Write(Item.holdStyle);
            writer.Write(Item.useStyle);
            writer.Write(Item.noUseGraphic);
            writer.Write(tooltipModePart1);
            writer.Write(tooltipModePart2);*/
        }

        public override void NetReceive(BinaryReader reader)
        {
            altMode = reader.ReadBoolean();
            if (altMode)
            {
                Item.axe = 40;
                Item.hammer = 100;
                Item.knockBack = 8f;
                Item.tileBoost = 2;
                Item.useTime = 6;
                Item.holdStyle = ItemHoldStyleID.None;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.DamageType = DamageClass.MeleeNoSpeed;
                ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
                Item.noUseGraphic = true;
                tooltipModePart1 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode2Part1");
                tooltipModePart2 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode2Part2");
            }
                
            else
            {
                Item.axe = 0;
                Item.hammer = 0;
                Item.knockBack = 2;
                Item.tileBoost = 0;
                Item.useTime = USECONSTANT;
                Item.holdStyle = ItemHoldStyleID.HoldGuitar;
                Item.useStyle = ItemUseStyleID.Guitar;
                Item.DamageType = DamageClass.Ranged;
                Item.noUseGraphic = false;
                ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = false;
                tooltipModePart1 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode1Part1");
                tooltipModePart2 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode1Part2");
            }
                
            /*Item.holdStyle = reader.ReadInt32();
            Item.useStyle = reader.ReadInt32();
            Item.noUseGraphic = reader.ReadBoolean();
            tooltipModePart1 = reader.ReadString();
            tooltipModePart2 = reader.ReadString();*/
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? PrefixChance(int pre, UnifiedRandom rand)
        {
            return false;
        }

        public override void OnCreated(ItemCreationContext context)
        {
            PrefixChance(-3, new UnifiedRandom());
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (altMode && delay > 0)
                {
                    delay--;
                }
                if (!altMode && delay2 > 0)
                {
                    delay2--;
                    if (delay2 == 0)
                    {
                        Item.noUseGraphic = false;
                        Item.NetStateChanged();
                    }
                }


                /*if (lightCounter < 30)
                    Lighting.AddLight(player.position, new Vector3(0.8f, 0.8f, 0f));
                else if (lightCounter < 60)
                    Lighting.AddLight(player.position, new Vector3(0f, 0.8f, 0.8f));
                else if (lightCounter <= 90)
                {
                    Lighting.AddLight(player.position, new Vector3(0.8f, 0f, 0.8f));
                    if (lightCounter == 90)
                        lightCounter = 0;
                }
                lightCounter++;*/
                player.GetModPlayer<FreeBirdPlayer>().holdingFreeBirdWeapon = true;
            }
        }

        public override void UseAnimation(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (player.altFunctionUse != 2)
                {
                    player.GetModPlayer<FreeBirdPlayer>().usingFreeBirdWeapon = true;
                }

                if (altMode && player.altFunctionUse != 2)
                    // Why not use Item.shoot? It's because it's normally not compatible with Item.axe/Item.hamaxe (and Item.pick), since shooting overrides tile breaking.
                    // Drills and chainsaws are "shot," but have special functionalities to their projectiles that I couldn't replicate for some reason.
                    // That, and I just found it easier to manually code in a custom swinging projectile.
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.RotatedRelativePoint(player.MountedCenter), new Vector2(player.direction * (float)Math.Sqrt(3) / 2, -0.5f), ProjectileType<FreeBirdHamaxeSwing>(), player.GetWeaponDamage(Item), Item.knockBack, Main.myPlayer);
            }
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
            {
                return true;
            }

            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.None;
                if (altMode && delay <= 0)
                {
                    Item.axe = 0;
                    Item.hammer = 0;
                    Item.knockBack = 2;
                    Item.tileBoost = 0;
                    Item.useTime = USECONSTANT;
                    Item.holdStyle = ItemHoldStyleID.HoldGuitar;
                    Item.useStyle = ItemUseStyleID.Guitar;
                    Item.DamageType = DamageClass.Ranged;
                    ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = false;
                    altMode = false;
                    delay = Item.useAnimation;
                    delay2 = 6;
                    SoundEngine.PlaySound(SoundID.DrumClosedHiHat, player.position);
                    tooltipModePart1 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode1Part1");
                    tooltipModePart2 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode1Part2");
                    Item.NetStateChanged();
                }
                else
                {
                    Item.axe = 40;
                    Item.hammer = 100;
                    Item.knockBack = 8f;
                    Item.tileBoost = 2;
                    Item.useTime = 6;
                    Item.holdStyle = ItemHoldStyleID.None;
                    Item.useStyle = ItemUseStyleID.Swing;
                    Item.DamageType = DamageClass.MeleeNoSpeed;
                    ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
                    Item.noUseGraphic = true;
                    altMode = true;
                    if (delay == Item.useAnimation)
                        SoundEngine.PlaySound(SoundID.DrumClosedHiHat, player.position);
                    tooltipModePart1 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode2Part1");
                    tooltipModePart2 = Language.GetTextValue("Mods.FreeBirdWeapon.Items.FreeBirdGuitar.Mode2Part2");
                    Item.NetStateChanged();
                }
                return true;
            }
            else
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    if (altMode)
                    {
                        // Something could go here... but nope
                    }
                    else
                    {
                        // A portion of this code is copied and cleaned from the Vanilla Terraria Projectile.cs file
                        float speed = 6f;
                        Vector2 posAdjust = Main.LocalPlayer.RotatedRelativePoint(Main.LocalPlayer.MountedCenter);
                        float xVal = Main.mouseX + Main.screenPosition.X - posAdjust.X;
                        float yVal = Main.mouseY + Main.screenPosition.Y - posAdjust.Y;
                        if (Main.LocalPlayer.gravDir == -1f)
                            yVal = Main.screenHeight - Main.mouseY + Main.screenPosition.Y - posAdjust.Y;
                        float adjustedDistanceValue = (float)(speed / Math.Sqrt(xVal * xVal + yVal * yVal));
                        xVal *= adjustedDistanceValue;
                        yVal *= adjustedDistanceValue;
                        Vector2 velocity = new Vector2(xVal, yVal) + player.velocity;

                        if (velocity.X > 0f)
                        {
                            Main.LocalPlayer.direction = 1;
                            player.itemLocation = new Vector2(player.Center.X - 36f, player.MountedCenter.Y + 36f);
                        }
                        else if (velocity.X < 0f)
                        {
                            Main.LocalPlayer.direction = -1;
                            player.itemLocation = new Vector2(player.Center.X + 36f, player.MountedCenter.Y + 36f);
                        }

                        var type = Main.rand.Next([ProjectileType<FreeBirdNote1>(), ProjectileType<FreeBirdNote2>(), ProjectileType<FreeBirdNote3>()]);
                        Projectile.NewProjectile(player.GetSource_ItemUse(Item), posAdjust, velocity, type, player.GetWeaponDamage(Item), Item.knockBack, Main.myPlayer);
                    }
                }
            }
            return true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (!altMode)
            {
                return true;
            }
            Texture2D itemTexture = Request<Texture2D>("FreeBirdWeapon/Content/Items/Weapons/FreeBirdGuitarAlt").Value;
            spriteBatch.Draw(itemTexture, position, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (!altMode)
            {
                return true;
            }
            Texture2D itemTexture = Request<Texture2D>("FreeBirdWeapon/Content/Items/Weapons/FreeBirdGuitarAlt").Value;
            spriteBatch.Draw(itemTexture, Item.Center - Main.screenPosition, null, lightColor, rotation, Item.Size * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void HoldStyle(Player player, Rectangle heldItemFrame)
        {
            if (!altMode)
            {
                if (player.direction == 1)
                    player.itemLocation = new Vector2(player.Center.X - 36f, player.MountedCenter.Y + 36f);
                else
                    player.itemLocation = new Vector2(player.Center.X + 36f, player.MountedCenter.Y + 36f);
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (!altMode)
            {
                if (player.direction == 1)
                    player.itemLocation = new Vector2(player.Center.X - 36f, player.MountedCenter.Y + 36f);
                else
                    player.itemLocation = new Vector2(player.Center.X + 36f, player.MountedCenter.Y + 36f);
            }
        }

        public override void UpdateInventory(Player player)
        {
            GetDamage();
        }

        public override void AddRecipes()
        {
            if (GetInstance<GameConfig>().EasyCraft)
            {
                Recipe recipe = CreateRecipe();
                recipe.AddRecipeGroup(RecipeGroupID.Wood, 20);
                recipe.AddRecipeGroup("CopperBar", 2);
                recipe.AddTile(TileID.Anvils);
                recipe.Register();
            } else
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.TheAxe);
                recipe.AddRecipeGroup(RecipeGroupID.Fragment, 5);
                recipe.AddIngredient(ItemID.IllegalGunParts);
                recipe.AddRecipeGroup(RecipeGroupID.Birds, 5);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }
}
