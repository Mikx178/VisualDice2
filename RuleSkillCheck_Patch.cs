using System;
using HarmonyLib;
using Kingmaker.RuleSystem.Rules;

namespace DialogDice
{
	[HarmonyPatch(typeof(RuleSkillCheck), "OnTrigger")]
	internal static class RuleSkillCheck_Patch
	{
		private static void Postfix(RuleSkillCheck __instance)
		{
			try
			{
				// Lets an interactive lock / map check grab the rule the game just rolled.
				RollPopup.TryCaptureInteractionRule(__instance);
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
		}
	}
}
