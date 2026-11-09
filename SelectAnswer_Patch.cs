using System;
using HarmonyLib;
using Kingmaker.Controllers.Dialog;

namespace DialogDice
{
	// Token: 0x02000023 RID: 35
	[HarmonyPatch(typeof(DialogController), "SelectAnswer")]
	internal static class SelectAnswer_Patch
	{
		// Token: 0x0600009A RID: 154 RVA: 0x000084BB File Offset: 0x000066BB
		private static bool Prefix()
		{
			return !RollPopup.IsBusy;
		}
	}
}
