using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PartyParticipation
{
	public class PartyParticipationClientConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Header("WearPartyHat")]

		[DefaultValue(true)]
		public bool Zoologist { get; set; }

		[DefaultValue(true)]
		public bool OldMan { get; set; }

		[DefaultValue(true)]
		public bool TaxCollector { get; set; }

		[DefaultValue(true)]
		public bool SkeletonMerchant { get; set; }

		[DefaultValue(true)]
		public bool ShimmerDryad { get; set; }

		[DefaultValue(false)]
		public bool AllModdedTownNPCs { get; set; }

		[DefaultValue(false)]
		public bool AllTownNPCsInBestiary { get; set; }

		[Header("TownNPCChat")]

		[DefaultValue(true)]
		public bool ZoologistPartyChat { get; set; }

		public override void OnChanged()
		{
			if (!Main.gameMenu) // Don't run the code on the main menu.
			{
				if (TaxCollector && Terraria.GameContent.Events.BirthdayParty.PartyIsUp)
				{
					PartyParticipationNPC.SetPartyTaxCollectorSets(); // Change the frame counts to +1 the original.
				}
				else if (!TaxCollector)
				{
					PartyParticipationNPC.ResetTaxCollectorSets(); // Reset the frame counts.
				}
			}
		}
	}
}
