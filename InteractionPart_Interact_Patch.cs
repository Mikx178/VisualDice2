using System;
using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.View.MapObjects;

namespace DialogDice
{
	// Token: 0x0200001F RID: 31
	[HarmonyPatch(typeof(InteractionPart), "Interact")]
	internal static class InteractionPart_Interact_Patch
	{
		// Token: 0x06000095 RID: 149 RVA: 0x0000848C File Offset: 0x0000668C
		private static bool Prefix(InteractionPart __instance, UnitEntityData user, ItemEntity toolItem)
		{
			return RollPopup.OnInteract(__instance, user, toolItem);
		}
	}
}
