using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Settings;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using UnityEngine;

namespace DialogDice
{
	// Token: 0x02000006 RID: 6
	public static class Breakdown
	{
		// Token: 0x06000013 RID: 19 RVA: 0x000025DC File Offset: 0x000007DC
		public static CheckInfo Predict(DialogController controller, BlueprintCheck check)
		{
			CheckInfo checkInfo = new CheckInfo
			{
				Stat = check.Type,
				DCAdjustment = Breakdown.DifficultyDCBonus()
			};
			checkInfo.DC = check.GetDC() + checkInfo.DCAdjustment;
			try
			{
				UnitEntityData unitEntityData = check.GetTargetUnit() ?? controller.ActingUnit;
				if (unitEntityData != null)
				{
					checkInfo.Unit = unitEntityData;
				}
				else
				{
					checkInfo.IsParty = true;
					int num = int.MinValue;
					foreach (UnitEntityData unitEntityData2 in Game.Instance.Player.Party)
					{
						if (unitEntityData2.Descriptor.State.CanAct)
						{
							int num2 = Breakdown.StatValue(unitEntityData2, check.Type);
							if (num2 > num)
							{
								num = num2;
								checkInfo.Unit = unitEntityData2;
							}
						}
					}
				}
				if (checkInfo.Unit != null)
				{
					checkInfo.Bonus = Breakdown.StatValue(checkInfo.Unit, check.Type);
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
			checkInfo.Lines = Breakdown.Build(checkInfo.Unit, checkInfo.Stat, checkInfo.Bonus);
			return checkInfo;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002730 File Offset: 0x00000930
		public static CheckInfo PredictFor(UnitEntityData unit, StatType stat, int baseDC)
		{
			CheckInfo checkInfo = new CheckInfo
			{
				Stat = stat,
				Unit = unit,
				DCAdjustment = Breakdown.DifficultyDCBonus()
			};
			checkInfo.DC = baseDC + checkInfo.DCAdjustment;
			try
			{
				checkInfo.Bonus = Breakdown.StatValue(unit, stat);
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
			checkInfo.Lines = Breakdown.Build(unit, stat, checkInfo.Bonus);
			return checkInfo;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000027B4 File Offset: 0x000009B4
		public static CheckInfo FromResult(SkillCheckResult result, int dc, int dcAdjustment, bool isParty)
		{
			CheckInfo checkInfo = new CheckInfo
			{
				Stat = result.StatType,
				Unit = result.ActingUnit,
				IsParty = isParty,
				DC = dc,
				DCAdjustment = dcAdjustment,
				HasRoll = true,
				D20 = result.D20,
				Total = result.RollResult,
				Passed = result.Passed,
				Bonus = result.RollResult - result.D20
			};
			checkInfo.Lines = Breakdown.Build(checkInfo.Unit, checkInfo.Stat, checkInfo.Bonus);
			return checkInfo;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002854 File Offset: 0x00000A54
		private static int StatValue(UnitEntityData unit, StatType type)
		{
			ModifiableValue stat = unit.Stats.GetStat(type);
			ModifiableValueAttributeStat modifiableValueAttributeStat = stat as ModifiableValueAttributeStat;
			if (modifiableValueAttributeStat != null)
			{
				return modifiableValueAttributeStat.Bonus;
			}
			if (stat == null)
			{
				return 0;
			}
			return stat.ModifiedValue;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002898 File Offset: 0x00000A98
		private static List<BonusLine> Build(UnitEntityData unit, StatType type, int bonus)
		{
			List<BonusLine> list = new List<BonusLine>();
			try
			{
				ModifiableValue modifiableValue = ((unit != null) ? unit.Stats.GetStat(type) : null);
				if (modifiableValue != null)
				{
					Breakdown.AddStat(modifiableValue, list);
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
				list.Clear();
			}
			int num = 0;
			foreach (BonusLine bonusLine in list)
			{
				num += bonusLine.Value;
			}
			if (bonus != num)
			{
				list.Add(new BonusLine("Situational", "Effects that only apply to this check", bonus - num, "\u00B1"));
			}
			list.RemoveAll((BonusLine l) => l.Value == 0);
			if (list.Count > 6)
			{
				int num2 = 0;
				for (int i = 5; i < list.Count; i++)
				{
					num2 += list[i].Value;
				}
				list.RemoveRange(5, list.Count - 5);
				list.Add(new BonusLine("Other", "Smaller bonuses combined", num2, "\u2026"));
			}
			return list;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000029E4 File Offset: 0x00000BE4
		private static void AddStat(ModifiableValue stat, List<BonusLine> lines)
		{
			ModifiableValueSkill modifiableValueSkill = stat as ModifiableValueSkill;
			ModifiableValueDependant modifiableValueDependant = stat as ModifiableValueDependant;
			ModifiableValueAttributeStat modifiableValueAttributeStat = stat as ModifiableValueAttributeStat;
			string text = Breakdown.StatName(stat.Type);
			if (modifiableValueSkill != null)
			{
				int baseValue = modifiableValueSkill.BaseValue;
				lines.Add(new BonusLine("Ranks", "Skill ranks in " + text, baseValue, "Rk"));
				if (modifiableValueSkill.ClassSkill && baseValue > 0)
				{
					lines.Add(new BonusLine("Class skill", text + " is a class skill", 3, "CS"));
				}
				if (modifiableValueSkill.BaseAttribute != null)
				{
					lines.Add(Breakdown.AttributeLine(modifiableValueSkill.BaseAttribute, text));
				}
				Breakdown.AddModifiers(stat, lines);
				return;
			}
			if (modifiableValueDependant != null)
			{
				lines.Add(new BonusLine("Base value", text, modifiableValueDependant.BaseValue, "BV"));
				if (modifiableValueDependant.BaseStat != null)
				{
					Breakdown.AddStat(modifiableValueDependant.BaseStat, lines);
				}
				Breakdown.AddModifiers(stat, lines);
				return;
			}
			if (modifiableValueAttributeStat != null)
			{
				lines.Add(Breakdown.AttributeLine(modifiableValueAttributeStat, null));
				return;
			}
			lines.Add(new BonusLine(text, "Character value", stat.ModifiedValue, Breakdown.Abbreviation(text)));
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002AF8 File Offset: 0x00000CF8
		private static BonusLine AttributeLine(ModifiableValueAttributeStat attribute, string skillName)
		{
			string text = Breakdown.StatName(attribute.Type);
			string text2 = string.Concat(new object[]
			{
				text,
				" ",
				attribute.ModifiedValue,
				" gives ",
				Breakdown.Signed(attribute.Bonus)
			});
			if (skillName != null)
			{
				text2 = text2 + ", basis for " + skillName;
			}
			return new BonusLine(text, text2, attribute.Bonus, Breakdown.Abbreviation(text));
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002B72 File Offset: 0x00000D72
		private static string Abbreviation(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return "?";
			}
			if (name.Length > 3)
			{
				return name.Substring(0, 3).ToUpperInvariant();
			}
			return name.ToUpperInvariant();
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002BA0 File Offset: 0x00000DA0
		private static void AddModifiers(ModifiableValue stat, List<BonusLine> lines)
		{
			foreach (ModifiableValue.Modifier modifier in stat.GetDisplayModifiers())
			{
				if (modifier.ModValue != 0 && !StatModifiersBreakdown.ModifierDisabled(stat, modifier))
				{
					ModifierDescriptor modifierDescriptor = ((modifier.ModDescriptor == (ModifierDescriptor)0) ? (ModifierDescriptor)43 : modifier.ModDescriptor);
					string text = Breakdown.DescriptorName(modifierDescriptor);
					IUIDataProvider iuidataProvider = modifier.Source;
					if (iuidataProvider == null)
					{
						iuidataProvider = modifier.ItemSource;
					}
					string text2 = null;
					Sprite sprite = null;
					if (iuidataProvider != null)
					{
						ItemEnchantment itemEnchantment = iuidataProvider as ItemEnchantment;
						if (itemEnchantment != null && itemEnchantment.Owner != null)
						{
							text2 = itemEnchantment.Owner.Name;
							sprite = itemEnchantment.Owner.Icon;
						}
						else
						{
							text2 = iuidataProvider.Name;
							sprite = iuidataProvider.Icon;
						}
					}
					BonusLine bonusLine = (string.IsNullOrEmpty(text2) ? new BonusLine(text, "Bonus with no named source", modifier.ModValue, "+") : new BonusLine(text2, text, modifier.ModValue, Breakdown.Abbreviation(text2)));
					bonusLine.Icon = sprite;
					lines.Add(bonusLine);
				}
			}
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002CC8 File Offset: 0x00000EC8
		public static string Signed(int value)
		{
			if (value < 0)
			{
				return "\u2212" + -value;
			}
			return "+" + value;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002CF0 File Offset: 0x00000EF0
		public static string StatName(StatType stat)
		{
			try
			{
				string text = LocalizedTexts.Instance.Stats.GetText(stat);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
			catch (Exception)
			{
			}
			return stat.ToString();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002D3C File Offset: 0x00000F3C
		private static string DescriptorName(ModifierDescriptor descriptor)
		{
			try
			{
				string name = LocalizedTexts.Instance.AbilityModifiers.GetName(descriptor);
				if (!string.IsNullOrEmpty(name))
				{
					return name;
				}
			}
			catch (Exception)
			{
			}
			return descriptor.ToString();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002D88 File Offset: 0x00000F88
		public static int DifficultyDCBonus()
		{
			int num;
			try
			{
				StatsAdjustmentsType statsAdjustmentsType = SettingsRoot.Difficulty.StatsAdjustments;
				num = BlueprintRoot.Instance.DifficultyList.GetAdjustmentPreset(statsAdjustmentsType).SkillCheckDCBonus;
			}
			catch (Exception)
			{
				num = 0;
			}
			return num;
		}

		// Token: 0x04000014 RID: 20
		private const int MaxLines = 6;
	}
}
