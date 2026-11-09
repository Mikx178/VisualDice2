using System;
using HarmonyLib;
using Kingmaker.RuleSystem.Rules;

namespace DialogDice
{
	[HarmonyPatch(typeof(RulePartyStatCheck), "OnTrigger")]
	internal static class RulePartyStatCheck_Patch
	{
		// Remembered so SkillCheckResult_Patch can tell whether a result came from a party check.
		private static void Postfix(RulePartyStatCheck __instance)
		{
			RulePartyStatCheck_Patch.Last = __instance;
		}

		public static RulePartyStatCheck Last;
	}
}
