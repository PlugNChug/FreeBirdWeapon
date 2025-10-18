using FreeBirdWeapon.Common.Config;
using FreeBirdWeapon.Content.Items.Weapons;
using Microsoft.Build.Tasks;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FreeBirdWeapon.Common.Players
{
    internal class FreeBirdSceneEffect : ModSceneEffect
    {
        public override int Music => 0;
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player)
        {
            // Suppress music if local player is holding guitar OR if close to someone else holding guitar
            if (player.GetModPlayer<FreeBirdPlayer>().holdingFreeBirdWeapon)
                return true;
            
            // Check if any other player nearby is broadcasting music
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (i == player.whoAmI || !Main.player[i].active)
                    continue;
                    
                FreeBirdPlayer fbPlayer = Main.player[i].GetModPlayer<FreeBirdPlayer>();
                if (fbPlayer.holdingFreeBirdWeapon || fbPlayer.remoteMusicPlaying)
                {
                    float distance = Vector2.Distance(player.Center, Main.player[i].Center);
                    // Suppress music when within hearing range (100 tiles = 1600 pixels)
                    if (distance <= 1600f)
                        return true;
                }
            }
            
            return false;
        }
    }
    internal class FreeBirdPlayer : ModPlayer
    {
        // Bool values that will be checked every frame to see if the player is doing anything with the Free Bird Guitar
        public bool holdingFreeBirdWeapon = false;
        public bool usingFreeBirdWeapon = false;

        // For remote music playback
        public bool remoteMusicPlaying = false;
        public bool remotePlayerUsing = false;
        private bool remoteMusicJustStarted = false; // Track if we need to fade in

        // For when the player is using the guitar
        private int tickCounter;

        // Music synchronization
        private long musicStartTime = 0; // Game time when music started
        private int syncTimer = 0; // Timer to sync music state
        private const int SYNC_INTERVAL = 30; // Sync every 30 frames (0.5 seconds)

        // Track the broadcasting player (first to hold guitar)
        public static int broadcastingPlayer = -1;
        
        // Allocate sound slots for both the backing track and the solo
        private SlotId backingTrackSlot;
        private SlotId soloSlot;
        
        // Remote player sound slots (for listening to other players' music)
        private SlotId remoteBackingTrackSlot;
        private SlotId remoteSoloSlot;

        // These are the sounds that will be played when interacting with the guitar. Both sounds classified as music.
        private SoundStyle backingTrack = new SoundStyle(BackingTrackChooser(), 0, SoundType.Music);
        private SoundStyle solo = new SoundStyle(SoloChooser(), 0, SoundType.Music);

        // This variable exists so the sounds above abide by the game's sound settings
        private float musicVolume = Main.musicVolume * 0.8f;
        
        // Proximity settings (in pixels, 1 tile = 16 pixels)
        private const float MAX_HEARING_DISTANCE = 1600f; // 100 tiles
        private const float MAX_VOLUME_DISTANCE = 160f;   // 10 tiles
        
        // Doppler effect settings
        private Vector2 lastRemotePlayerPosition = Vector2.Zero;
        private const float DOPPLER_STRENGTH = 0.15f; // Subtle doppler effect

        private static string BackingTrackChooser()
        {
            if (ModContent.GetInstance<MusicConfig>().MidiSong)
                return "FreeBirdWeapon/Assets/BackingTrackMidi";
            return "FreeBirdWeapon/Assets/BackingTrack";
        }

        private static string SoloChooser()
        {
            if (ModContent.GetInstance<MusicConfig>().MidiSong)
                return "FreeBirdWeapon/Assets/SoloMidi";
            return "FreeBirdWeapon/Assets/Solo";
        }

        // Make the game initialize the above sounds. Most likely will lag for a second upon entering a world.
        public override void OnEnterWorld()
        {
            backingTrackSlot = SoundEngine.PlaySound(backingTrack, Player.position);
            SoundEngine.TryGetActiveSound(backingTrackSlot, out var activeBackingTrack);
            activeBackingTrack.Stop();

            soloSlot = SoundEngine.PlaySound(solo, Player.position);
            SoundEngine.TryGetActiveSound(soloSlot, out var activeSolo);
            activeSolo.Stop();
            
            // Initialize remote slots
            remoteBackingTrackSlot = SoundEngine.PlaySound(backingTrack, Player.position);
            SoundEngine.TryGetActiveSound(remoteBackingTrackSlot, out var remoteBackingTrack);
            remoteBackingTrack.Stop();

            remoteSoloSlot = SoundEngine.PlaySound(solo, Player.position);
            SoundEngine.TryGetActiveSound(remoteSoloSlot, out var remoteSolo);
            remoteSolo.Stop();
            
            // Reset broadcasting player when entering world
            if (Main.netMode == NetmodeID.SinglePlayer)
                broadcastingPlayer = -1;
        }

        // Constantly checks if the player is interacting (holding, using) with the guitar.
        public override void PostUpdate()
        {
            // Only run on clients, not on dedicated servers
            if (Main.netMode == NetmodeID.Server)
                return;
            
            // Handle local player's own music
            if (Player.whoAmI == Main.myPlayer)
            {
                HandleLocalMusic();
                
                // Check if we should be listening to any remote player's music
                CheckRemoteMusicFromAllPlayers();
            }
        }
        
        private void HandleLocalMusic()
        {
            // These flags check if the sounds are currently playing
            bool flag = SoundEngine.TryGetActiveSound(backingTrackSlot, out var activeBackingTrack);
            bool flag2 = SoundEngine.TryGetActiveSound(soloSlot, out var activeSolo);
            
            // Handle broadcasting player assignment
            if (holdingFreeBirdWeapon)
            {
                // If no one is broadcasting, or if the broadcasting player stopped, claim it
                if (broadcastingPlayer == -1 || 
                    !Main.player[broadcastingPlayer].active || 
                    !Main.player[broadcastingPlayer].GetModPlayer<FreeBirdPlayer>().holdingFreeBirdWeapon)
                {
                    broadcastingPlayer = Player.whoAmI;
                    musicStartTime = Main.GameUpdateCount;
                }
                
                // Sync music state to other players
                if (broadcastingPlayer == Player.whoAmI && Main.netMode != NetmodeID.SinglePlayer)
                {
                    syncTimer++;
                    if (syncTimer >= SYNC_INTERVAL)
                    {
                        syncTimer = 0;
                        SendMusicSync();
                    }
                }
            }
            else if (broadcastingPlayer == Player.whoAmI)
            {
                // We stopped holding, release broadcasting rights
                broadcastingPlayer = -1;
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    SendMusicStop();
                }
            }

            // If the player is holding the guitar and the sounds are not playing, start playing the sounds
            if (!flag && holdingFreeBirdWeapon) 
            {
                if (Main.gameInactive)
                    return;
                    
                backingTrackSlot = SoundEngine.PlaySound(backingTrack, Player.position);
                soloSlot = SoundEngine.PlaySound(solo, Player.position);
                musicStartTime = Main.GameUpdateCount;
                
                // Local music starts at full volume immediately
                if (SoundEngine.TryGetActiveSound(backingTrackSlot, out var newBackingTrack))
                    newBackingTrack.Volume = musicVolume;
                if (SoundEngine.TryGetActiveSound(soloSlot, out var newSolo))
                    newSolo.Volume = 0; // Solo stays muted until used
            } 
            else if (holdingFreeBirdWeapon)   // If the sounds are already being played
            {
                try
                {
                    if (Main.gameInactive)
                    {
                        activeBackingTrack.Stop();
                        activeSolo.Stop();
                        return;
                    }
                    // Constantly update the backing track to be positioned at the player
                    activeBackingTrack.Position = Player.position;

                    // Keep volume at full for local player
                    activeBackingTrack.Volume = musicVolume;
                }
                catch
                {
                    // Do nothing
                }
            } 
            else if (flag && !holdingFreeBirdWeapon)  // If the sounds are being played AND the player is no longer holding the guitar
            {
                try
                {
                    // Constantly update the backing track to be positioned at the player
                    activeBackingTrack.Position = Player.position;

                    // Keep lowering the volume of the backing tracks until it reaches a certain point
                    activeBackingTrack.Volume *= 0.95f;
                    if (activeBackingTrack.Volume <= 0.02f)
                    {
                        activeBackingTrack.Volume = 0;
                        activeBackingTrack.Stop();
                        activeSolo.Stop();
                    }
                }
                catch 
                {
                    // Do nothing
                }
            }

            if (flag2 && usingFreeBirdWeapon)   // If the player is pressing and/or holding the use button
            {
                // Update the solo's position constantly to be at the player
                activeSolo.Position = Player.position;
                // Matches the use animation time for the guitar
                tickCounter = 15;
            }
            else if (flag2 && tickCounter > 0)    // If the tick counter was set to its max value
            {
                activeSolo.Position = Player.position;
                // Decrement the counter
                tickCounter--;

                // Give the solo a baseline volume so it can be multiplied further
                if (activeSolo.Volume <= 0)
                    activeSolo.Volume = 0.1f;

                // Quickly raise the volume of the solo and backing track if it's below the volume threshold
                if (activeSolo.Volume < musicVolume)
                {
                    activeBackingTrack.Volume *= 1.5f;
                    activeSolo.Volume *= 1.5f;
                }
                else
                {
                    activeBackingTrack.Volume = musicVolume;
                    activeSolo.Volume = musicVolume;
                }
            }
            else if (flag2)   // In other words, when guitar is not being used but still held
            {
                // Update the solo's position constantly to be at the player
                activeSolo.Position = Player.position;

                // Swiftly lower the volume of the solo
                activeSolo.Volume *= 0.75f;
                if (activeSolo.Volume <= 0.02f)
                    activeSolo.Volume = 0;
            }
        }
        
        private void CheckRemoteMusicFromAllPlayers()
        {
            // This runs ONLY on the local player
            // Check all other players to see if any of them are broadcasting
            
            // Get local player's FreeBirdPlayer for remote music state
            FreeBirdPlayer localFBPlayer = Main.LocalPlayer.GetModPlayer<FreeBirdPlayer>();
            
            // If local player is holding guitar, prioritize their own music - don't listen to remote
            if (localFBPlayer.holdingFreeBirdWeapon)
            {
                // Stop any remote music that might be playing
                if (localFBPlayer.remoteMusicPlaying)
                {
                    localFBPlayer.StopRemoteMusic();
                }
                return;
            }
            
            Player broadcastingPlayerInstance = null;
            
            // Find ANY player who is holding the guitar (they are broadcasting)
            // Loop through all players instead of relying on static variable
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                // Skip local player and inactive players
                if (i == Main.myPlayer || !Main.player[i].active)
                    continue;
                
                Player otherPlayer = Main.player[i];
                FreeBirdPlayer fbPlayer = otherPlayer.GetModPlayer<FreeBirdPlayer>();
                
                // If this player is holding the guitar, they're our broadcaster!
                if (fbPlayer.holdingFreeBirdWeapon)
                {
                    broadcastingPlayerInstance = otherPlayer;
                    break; // Found one, that's enough
                }
            }
            
            // If no one is broadcasting or we can't find them, stop remote music
            if (broadcastingPlayerInstance == null)
            {
                if (localFBPlayer.remoteMusicPlaying)
                {
                    localFBPlayer.StopRemoteMusic();
                }
                return;
            }
            
            // We found the broadcasting player, play their music
            PlayRemoteMusicFrom(broadcastingPlayerInstance);
        }
        
        private void PlayRemoteMusicFrom(Player remotePlayer)
        {
            // Get the LOCAL player's FreeBirdPlayer instance (this is where remote music state lives)
            FreeBirdPlayer localFBPlayer = Main.LocalPlayer.GetModPlayer<FreeBirdPlayer>();
            
            // Get the REMOTE player's FreeBirdPlayer instance (for their state like using weapon)
            FreeBirdPlayer remoteFBPlayer = remotePlayer.GetModPlayer<FreeBirdPlayer>();
            
            // Calculate distance from local player to remote broadcasting player
            float distance = Vector2.Distance(Main.LocalPlayer.Center, remotePlayer.Center);
            
            // Only play if within hearing range
            if (distance > MAX_HEARING_DISTANCE)
            {
                if (localFBPlayer.remoteMusicPlaying)
                {
                    localFBPlayer.StopRemoteMusic();
                }
                return;
            }
            
            // Calculate volume based on distance (linear fade)
            float volumeMultiplier = 1f;
            if (distance > MAX_VOLUME_DISTANCE)
            {
                // Linear interpolation from 1.0 at MAX_VOLUME_DISTANCE to 0.1 at MAX_HEARING_DISTANCE
                volumeMultiplier = 1f - ((distance - MAX_VOLUME_DISTANCE) / (MAX_HEARING_DISTANCE - MAX_VOLUME_DISTANCE)) * 0.9f;
            }
            
            // Calculate doppler effect (subtle pitch shift based on velocity)
            float pitchShift = CalculateDopplerPitch(remotePlayer);
            
            // Get or start remote music (use LOCAL player's sound slots!)
            bool remoteBackingFlag = SoundEngine.TryGetActiveSound(localFBPlayer.remoteBackingTrackSlot, out var remoteBackingTrack);
            bool remoteSoloFlag = SoundEngine.TryGetActiveSound(localFBPlayer.remoteSoloSlot, out var remoteSolo);
            
            if (!remoteBackingFlag && !Main.gameInactive)
            {
                
                // Start playing remote music
                localFBPlayer.remoteBackingTrackSlot = SoundEngine.PlaySound(backingTrack, remotePlayer.position);
                localFBPlayer.remoteSoloSlot = SoundEngine.PlaySound(solo, remotePlayer.position);
                localFBPlayer.remoteMusicPlaying = true;
                localFBPlayer.remoteMusicJustStarted = true; // Flag for fade-in
                
                // Start at 0 volume for fade-in effect
                if (SoundEngine.TryGetActiveSound(localFBPlayer.remoteBackingTrackSlot, out remoteBackingTrack))
                {
                    remoteBackingTrack.Volume = 0f;
                }
                if (SoundEngine.TryGetActiveSound(localFBPlayer.remoteSoloSlot, out remoteSolo))
                {
                    remoteSolo.Volume = 0f;
                }
            }
            else if (remoteBackingFlag && remoteSoloFlag)
            {
                // Update existing remote music
                try
                {
                    if (Main.gameInactive)
                    {
                        remoteBackingTrack.Stop();
                        remoteSolo.Stop();
                        localFBPlayer.remoteMusicPlaying = false;
                        return;
                    }
                    
                    // Update position
                    remoteBackingTrack.Position = remotePlayer.position;
                    remoteSolo.Position = remotePlayer.position;
                    
                    // Update volume based on distance
                    float targetBackingVolume = musicVolume * volumeMultiplier;
                    
                    // Only fade in if music just started, otherwise instant volume changes
                    if (localFBPlayer.remoteMusicJustStarted)
                    {
                        if (remoteBackingTrack.Volume < targetBackingVolume)
                        {
                            // Give it a baseline to multiply from if starting from 0
                            if (remoteBackingTrack.Volume <= 0.01f)
                                remoteBackingTrack.Volume = 0.05f;
                            else
                                remoteBackingTrack.Volume = Math.Min(remoteBackingTrack.Volume * 1.2f, targetBackingVolume);
                        }
                        else
                        {
                            remoteBackingTrack.Volume = targetBackingVolume;
                            localFBPlayer.remoteMusicJustStarted = false; // Fade-in complete
                        }
                    }
                    else
                    {
                        // Instant volume changes when already playing
                        remoteBackingTrack.Volume = targetBackingVolume;
                    }
                    
                    // Solo volume depends on whether remote player is using weapon
                    float targetSoloVolume = remoteFBPlayer.remotePlayerUsing ? musicVolume * volumeMultiplier : 0f;
                    if (remoteFBPlayer.remotePlayerUsing)
                    {
                        if (remoteSolo.Volume < targetSoloVolume)
                        {
                            remoteSolo.Volume = Math.Min(remoteSolo.Volume * 1.5f, targetSoloVolume);
                        }
                        else
                        {
                            remoteSolo.Volume = targetSoloVolume;
                        }
                    }
                    else
                    {
                        // Fade out solo when not using
                        remoteSolo.Volume *= 0.75f;
                        if (remoteSolo.Volume <= 0.02f)
                            remoteSolo.Volume = 0;
                    }
                    
                    // Apply doppler effect
                    remoteBackingTrack.Pitch = pitchShift;
                    remoteSolo.Pitch = pitchShift;
                    
                    localFBPlayer.remoteMusicPlaying = true;
                }
                catch
                {
                    // Do nothing
                }
            }
            
            // Store position for next frame's doppler calculation
            localFBPlayer.lastRemotePlayerPosition = remotePlayer.Center;
        }
        
        private void StopRemoteMusic()
        {
            if (SoundEngine.TryGetActiveSound(remoteBackingTrackSlot, out var remoteBackingTrack))
            {
                remoteBackingTrack.Stop();
            }
            if (SoundEngine.TryGetActiveSound(remoteSoloSlot, out var remoteSolo))
            {
                remoteSolo.Stop();
            }
            remoteMusicPlaying = false;
            remoteMusicJustStarted = false; // Reset fade flag
        }
        
        private float CalculateDopplerPitch(Player remotePlayer)
        {
            // Calculate relative velocity
            Vector2 currentPos = remotePlayer.Center;
            Vector2 velocity = (currentPos - lastRemotePlayerPosition);
            
            // Calculate direction to listener
            Vector2 toListener = Main.LocalPlayer.Center - currentPos;
            float distance = toListener.Length();
            
            if (distance < 1f)
                return 0f;
            
            toListener.Normalize();
            
            // Project velocity onto the direction to listener
            float approachVelocity = Vector2.Dot(velocity, toListener);
            
            // Apply subtle doppler effect (positive = approaching = higher pitch)
            return MathHelper.Clamp(approachVelocity * DOPPLER_STRENGTH * 0.01f, -0.1f, 0.1f);
        }
        

        
        // Network sync methods
        private void SendMusicSync()
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)FreeBirdWeapon.MessageType.FreeBirdMusicSync);
            packet.Write((byte)Player.whoAmI);
            packet.Write(holdingFreeBirdWeapon);
            packet.Write(usingFreeBirdWeapon);
            packet.Write(musicStartTime);
            packet.Send();
        }
        
        private void SendMusicStop()
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)FreeBirdWeapon.MessageType.FreeBirdMusicStop);
            packet.Write((byte)Player.whoAmI);
            packet.Send();
        }
        
        public void ReceiveMusicSync(bool isHolding, bool isUsing, long musicTime)
        {
            // Always update the state for this player instance (even for local player in multiplayer scenarios)
            holdingFreeBirdWeapon = isHolding;
            remotePlayerUsing = isUsing;
            
            // Update music start time if we're starting fresh
            if (musicStartTime == 0)
            {
                musicStartTime = musicTime;
            }
        }
        
        public void ReceiveMusicStop()
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                holdingFreeBirdWeapon = false;
                remotePlayerUsing = false;
                StopRemoteMusic();
            }
        }

        public override void PostUpdateRunSpeeds()
        {
            bool speedCheck = false;
            if (Player.HeldItem.type == ModContent.ItemType<FreeBirdGuitar>())
                speedCheck = true;
            if (speedCheck)
            {
                Player.maxRunSpeed *= 2f;
                Player.accRunSpeed *= 2f;
                Player.runAcceleration *= 1.2f;
            }
            
            // Add sparkle particles if this player is broadcasting music
            if (broadcastingPlayer == Player.whoAmI && holdingFreeBirdWeapon)
            {
                // Spawn sparkles around the player
                if (Main.rand.NextBool(3)) // 33% chance each frame for varied effect
                {
                    // Random position around player
                    Vector2 position = Player.Center + new Vector2(
                        Main.rand.NextFloat(-30f, 30f),
                        Main.rand.NextFloat(-40f, 20f)
                    );
                    
                    // Create sparkle dust with varied colors
                    int dustType = Main.rand.Next(new int[] { 
                        DustID.YellowStarDust,  // Yellow sparkles
                        DustID.PurpleTorch,     // Purple sparkles
                        DustID.IceTorch,        // Blue sparkles
                        DustID.RainbowMk2,      // Rainbow sparkles
                        DustID.TreasureSparkle  // Treasure sparkles
                    });
                    
                    Dust dust = Dust.NewDustDirect(position, 0, 0, dustType, 0f, 0f, 100, default(Color), 1.2f);
                    dust.velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.5f, -0.5f));
                    dust.noGravity = true;
                    dust.fadeIn = 1.2f;
                }
            }
        }

        public override void ResetEffects()
        {
            // Only reset flags for the LOCAL player
            // Remote players' flags are controlled by network sync
            if (Player.whoAmI == Main.myPlayer)
            {
                holdingFreeBirdWeapon = false;
                usingFreeBirdWeapon = false;
            }
        }
    }
}
