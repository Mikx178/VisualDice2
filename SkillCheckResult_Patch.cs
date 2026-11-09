using System;
using HarmonyLib;
using Kingmaker.Controllers.Dialog;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;

namespace DialogDice
{
	[HarmonyPatch(typeof(SkillCheckResult), MethodType.Constructor, new Type[]
	{
		typeof(RuleStatCheck),
		typeof(UnitEntityData)
	})]
	internal static class SkillCheckResult_Patch
	{
		private static void Postfix(SkillCheckResult __instance, RuleStatCheck e)
		{
			RuleSkillCheck ruleSkillCheck = e as RuleSkillCheck;
			int dc = ((ruleSkillCheck != null) ? ruleSkillCheck.DC : e.DifficultyClass);
			int dcAdjustment = ((ruleSkillCheck != null) ? ruleSkillCheck.DifficultyDCAdjustment : 0);
			RulePartyStatCheck last = RulePartyStatCheck_Patch.Last;
			RollPopup.OnResult(__instance, dc, dcAdjustment, last != null && last.StatCheck == e);
		}
	}
}
