using System;
using HarmonyLib;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;

namespace DialogDice
{
	// Token: 0x0200001C RID: 28
	[HarmonyPatch(typeof(DialogController), "PlayCheck")]
	internal static class PlayCheck_Patch
	{
		// Token: 0x06000092 RID: 146 RVA: 0x0000842A File Offset: 0x0000662A
		private static bool Prefix(DialogController __instance, BlueprintCheck check)
		{
			return RollPopup.OnPlayCheck(__instance, check);
		}
	}
}
