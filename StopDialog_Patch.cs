using System;
using HarmonyLib;
using Kingmaker.Controllers.Dialog;

namespace DialogDice
{
	// Token: 0x02000024 RID: 36
	[HarmonyPatch(typeof(DialogController), "StopDialog")]
	internal static class StopDialog_Patch
	{
		// Token: 0x0600009B RID: 155 RVA: 0x000084C5 File Offset: 0x000066C5
		private static void Postfix()
		{
			RollPopup.OnDialogStopped();
		}
	}
}
