using System;
using HarmonyLib;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;

namespace DialogDice
{
	// Token: 0x0200001D RID: 29
	[HarmonyPatch(typeof(DialogController), "PlayCue")]
	internal static class PlayCue_Patch
	{
		// Token: 0x06000093 RID: 147 RVA: 0x00008433 File Offset: 0x00006633
		private static bool Prefix(DialogController __instance, BlueprintCueBase cue)
		{
			return RollPopup.OnPlayCue(__instance, cue);
		}
	}
}
