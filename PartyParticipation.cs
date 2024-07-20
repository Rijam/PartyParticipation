using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PartyParticipation
{
	public class PartyParticipation : Mod
	{

	}

	/// <summary>
	/// Contains the detours used by the mod.
	/// </summary>
	public class PartyParticipationSystem : ModSystem
	{
		public override void Load()
		{
			// Load the detours.
			Terraria.On_NPC.UsesPartyHat += NPC_Hook_UsesPartyHat;
			Terraria.On_NPC.UpdateAltTexture += NPC_Hook_UpdateAltTexture;
		}

		/// <summary>
		/// <br>Detour of NPC.UsesPartyHat</br>
		/// <br>Returning true will put a party hat on the Town NPC.</br>
		/// <br>Returning BirthdayParty.PartyIsUp so the party hat only appears during parties.</br>
		/// </summary>
		/// <param name="orig">The original method</param>
		/// <param name="self">The NPC instance</param>
		/// <returns>True if the NPC should wear a party hat and there is a party happen.</returns>
		private static bool NPC_Hook_UsesPartyHat(Terraria.On_NPC.orig_UsesPartyHat orig, NPC self)
		{
			PartyParticipationClientConfig clientConfig = ModContent.GetInstance<PartyParticipationClientConfig>(); // Get the config.

			// For NPCs listed in the Bestiary.
			if (self.IsABestiaryIconDummy && (self.townNPC || NPCID.Sets.ActsLikeTownNPC[self.type]))
			{
				return BirthdayParty.PartyIsUp && clientConfig.AllTownNPCsInBestiary;
			}

			// Tax Collector if the config is enabled.
			if (self.type == NPCID.TaxCollector && clientConfig.TaxCollector)
			{
				return BirthdayParty.PartyIsUp;
			}
			// Old Man if the config is enabled.
			if (self.type == NPCID.OldMan && clientConfig.OldMan)
			{
				return BirthdayParty.PartyIsUp;
			}
			// Zoologist if the config is enabled.
			if (self.type == NPCID.BestiaryGirl && clientConfig.Zoologist)
			{
				return BirthdayParty.PartyIsUp;
			}
			// Dryad if in the shimmer variant and the config is enabled.
			if (self.type == NPCID.Dryad && self.townNpcVariationIndex == 1 && clientConfig.ShimmerDryad)
			{
				return BirthdayParty.PartyIsUp;
			}
			// Skeleton Merchant if config is enabled.
			if (self.type == NPCID.SkeletonMerchant && clientConfig.SkeletonMerchant)
			{
				return BirthdayParty.PartyIsUp;
			}
			// If the NPC is a Town NPC or acts like one, is a modded NPC, and the config is enabled.
			if ((self.townNPC || NPCID.Sets.ActsLikeTownNPC[self.type]) && self.ModNPC != null && clientConfig.AllModdedTownNPCs)
			{
				return BirthdayParty.PartyIsUp;
			}

			return orig(self); // Run the vanilla code if none of the above.
		}

		/// <summary>
		/// <br>Detour of NPC.UpdateAltTexture</br>
		/// <br>Tells the game to change the texture used to the "alt" texture.</br>
		/// <br>Only relevant for NPCs who have alternate textures for parties.</br>
		/// </summary>
		/// <param name="orig">The original method</param>
		/// <param name="self">The NPC instance</param>
		private static void NPC_Hook_UpdateAltTexture(Terraria.On_NPC.orig_UpdateAltTexture orig, NPC self)
		{
			PartyParticipationClientConfig clientConfig = ModContent.GetInstance<PartyParticipationClientConfig>(); // Get the config.

			// Tax Collector, Skeleton Merchant, or Zoologist with their corresponding configs.
			if ((clientConfig.TaxCollector && self.type == NPCID.TaxCollector) || 
				(clientConfig.SkeletonMerchant && self.type == NPCID.SkeletonMerchant) || 
				(clientConfig.Zoologist && self.type == NPCID.BestiaryGirl) ||
				(clientConfig.AllModdedTownNPCs && self.ModNPC != null))
			{
				int oldAltTexture = self.altTexture; // Store the old texture because it is used for the transition effect.
				bool shouldWearHat = BirthdayParty.PartyIsUp || self.ForcePartyHatOn;

				self.altTexture = 0;
				if (shouldWearHat)
				{
					self.altTexture = 1; // Set their texture to their alternate texture for the party.
				}

				if (self.type == NPCID.BestiaryGirl && self.ShouldBestiaryGirlBeLycantrope())
				{
					self.altTexture = 2; // If the Zoologist is transformed, she needs to be the warefox. 
				}

				if (!self.ForcePartyHatOn)
				{
					InvokeMakeTransitionEffectsForTextureChanges(self, oldAltTexture); // Make the confetti effect when they change textures.
				}
			}
			else
			{
				orig(self); // Run the vanilla code.

				// Since the Skeleton Merchant is technically not a Town NPC, their texture doesn't automatically get reset.
				// So, reset the texture if they aren't enabled in the config.
				if (NPCID.Sets.ActsLikeTownNPC[self.type])
				{
					int oldAltTexture = self.altTexture;
					self.altTexture = 0;
					InvokeMakeTransitionEffectsForTextureChanges(self, oldAltTexture); // Make the confetti effect when they change textures.
				}
			}
		}

		/// <summary>
		/// <br>Invokes the vanilla NPC.MakeTransitionEffectsForTextureChanges</br>
		/// <br>Makes the confetti effect when they change textures.</br>
		/// </summary>
		/// <param name="self">The NPC instance.</param>
		/// <param name="oldAltTexture">ID for the previous texture they were using.</param>
		private static void InvokeMakeTransitionEffectsForTextureChanges(NPC self, int oldAltTexture)
		{
			// NPC.MakeTransitionEffectsForTextureChanges(oldAltTexture, self.altTexture);
			// Private, so use reflection to invoke it.
			MethodInfo method = self.GetType().GetMethod("MakeTransitionEffectsForTextureChanges", BindingFlags.NonPublic | BindingFlags.Instance);
			method.Invoke(self, [oldAltTexture, self.altTexture]);
		}
	}

	/// <summary>
	/// Handles the Zoologist's chat, party hat offsets, and fixing the Tax Collector.
	/// </summary>
	public class PartyParticipationNPC : GlobalNPC
	{
		public override void GetChat(NPC npc, ref string chat)
		{
			// If the config is enabled to change the one dialog from the Zoologist,
			// Check that she just said the dialog, then replace it with new dialog.
			if (ModContent.GetInstance<PartyParticipationClientConfig>().ZoologistPartyChat && 
				npc.type == NPCID.BestiaryGirl && 
				chat == Language.GetTextValue("BestiaryGirlSpecialText.Party")) // "So, uh, I can't wear the hat. Sorry. I'm still here for the party, no worries!"
			{
				chat = Language.GetTextValue("Mods.PartyParticipation.NPCs.TownNPCs.Zoologist.NewParty");
			}
		}

		public override bool PreAI(NPC npc)
		{
			// This is necessary because Re-Logic messed up the party sprite by giving it one extra attack frame.
			if (ModContent.GetInstance<PartyParticipationClientConfig>().TaxCollector && npc.type == NPCID.TaxCollector)
			{
				if (npc.altTexture == 1) // If in the alt texture...
				{
					SetPartyTaxCollectorSets(); // Change the frame counts so the texture doesn't scroll.
				}
				else
				{
					ResetTaxCollectorSets(); // Reset it back to the original if not the alt texture.
				}
			}
			return base.PreAI(npc);
		}

		/// <summary> Set the frame counts to the vanilla values</summary>
		internal static void ResetTaxCollectorSets()
		{
			// Vanilla values
			Main.npcFrameCount[NPCID.TaxCollector] = 25;
			NPCID.Sets.ExtraFramesCount[NPCID.TaxCollector] = 9;
			NPCID.Sets.AttackFrameCount[NPCID.TaxCollector] = 4;
		}

		/// <summary> Set the frame counts to +1 of the vanilla values because the texture has an extra frame.</summary>
		internal static void SetPartyTaxCollectorSets()
		{
			// Need 1 extra for each because the texture has one extra frame.
			Main.npcFrameCount[NPCID.TaxCollector] = 26;
			NPCID.Sets.ExtraFramesCount[NPCID.TaxCollector] = 10;
			NPCID.Sets.AttackFrameCount[NPCID.TaxCollector] = 5;
		}

		// Change the position of the party hat to match their heads.
		public override void PartyHatPosition(NPC npc, ref Vector2 position, ref SpriteEffects spriteEffects)
		{
			PartyParticipationClientConfig clientConfig = ModContent.GetInstance<PartyParticipationClientConfig>(); // Get the config.

			if (clientConfig.SkeletonMerchant && npc.type == NPCID.SkeletonMerchant)
			{
				// Move it 5 pixels forward.
				position.X += 5 * npc.spriteDirection;

				// Adjust the height during the walking animation. This is on top of what ever NPCFramingGroup.
				int frame = npc.frame.Y / npc.frame.Height;
				switch (frame)
				{
					case 2:
					case 3:
					case 4:
					case 5:
					case 7:
					case 8:
					case 9:
					case 11:
					case 12:
					case 14:
					case 15:
					case 16:
						position.Y -= 2; // Negative Y is up.
						break;
					case 6:
					case 13:
						position.Y -= 4;
						break;
					case 10:
					default:
						break;
				}
				if (npc.altTexture == 1)
				{
					position.Y += 2;
				}
			}
			if (clientConfig.Zoologist && npc.type == NPCID.BestiaryGirl)
			{
				position.X += 3 * npc.spriteDirection;
				position.Y += 2;
			}
			if (clientConfig.TaxCollector && npc.type == NPCID.TaxCollector)
			{
				int frame = npc.frame.Y / npc.frame.Height;
				switch (frame)
				{
					case 12:
						position.Y += 2;
						position.X += 4 * npc.spriteDirection;
						break;
					case 18: // Sitting
						break;
					default:
						position.X += 4 * npc.spriteDirection;
						break;
				}
			}
		}
		
		private static ITownNPCProfile NPCProfile; // Store the new Tax Collector profile.

		public override void SetStaticDefaults()
		{
			NPCProfile = new TaxCollectorProfile(); // Get the new Tax Collector profile on mod load.
		}

		public override ITownNPCProfile ModifyTownNPCProfile(NPC npc)
		{
			if (npc.type == NPCID.TaxCollector)
			{
				return NPCProfile; // Set the new Tax Collector profile.
			}
			return base.ModifyTownNPCProfile(npc);
		}
	}

	// This is necessary because Re-Logic messed up the party sprite by giving it one extra attack frame.
	public class TaxCollectorProfile : ITownNPCProfile
	{
		private readonly Asset<Texture2D> notShimmeredAndNotParty = ModContent.Request<Texture2D>("Terraria/Images/TownNPCs/TaxCollector_Default");
		private readonly Asset<Texture2D> notShimmeredAndParty = ModContent.Request<Texture2D>("Terraria/Images/TownNPCs/TaxCollector_Default_Party");
		private readonly Asset<Texture2D> shimmeredAndNotParty = ModContent.Request<Texture2D>("Terraria/Images/TownNPCs/Shimmered/TaxCollector_Default");
		private readonly Asset<Texture2D> shimmeredAndParty = ModContent.Request<Texture2D>("Terraria/Images/TownNPCs/Shimmered/TaxCollector_Default_Party");

		public int RollVariation() => 0;
		public string GetNameForVariant(NPC npc) => npc.getNewNPCName();

		public Asset<Texture2D> GetTextureNPCShouldUse(NPC npc)
		{
			// Change the Bestiary portrait to the party one if the there is a party and the tax collector is participating.
			// This is because the party sprite has one extra frame, so it caused the normal sprite to scroll in the Bestiary.
			if (npc.IsABestiaryIconDummy && BirthdayParty.PartyIsUp && ModContent.GetInstance<PartyParticipationClientConfig>().TaxCollector)
			{
				return notShimmeredAndParty;
			}
			else if (npc.IsABestiaryIconDummy)
			{
				return notShimmeredAndNotParty;
			}

			// Shimmered and party
			if (npc.IsShimmerVariant && npc.altTexture == 1)
			{
				return shimmeredAndParty;
			}
			// Shimmered and no party
			if (npc.IsShimmerVariant && npc.altTexture != 1)
			{
				return shimmeredAndNotParty;
			}
			// Not shimmered and party
			if (!npc.IsShimmerVariant && npc.altTexture == 1)
			{
				return notShimmeredAndParty;
			}
			// Not shimmered and no party
			return notShimmeredAndNotParty;
		}
		public int GetHeadTextureIndex(NPC npc)
		{
			if (npc.townNpcVariationIndex == 1)
			{
				return 70; // Head ID for the shimmered Tax Collector.
			}
			return 23; // Head ID for the normal Tax Collector.
		}
	}
}
