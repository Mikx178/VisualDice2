using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using UnityEngine;

namespace DialogDice
{
	// Token: 0x02000025 RID: 37
	public partial class RollPopup : MonoBehaviour
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600009C RID: 156 RVA: 0x000084CC File Offset: 0x000066CC
		public static bool IsBusy
		{
			get
			{
				return RollPopup.s_State != RollPopup.State.None || RollPopup.s_Queue.Count > 0 || RollPopup.s_HasDeferredCue;
			}
		}

		// Token: 0x0600009D RID: 157 RVA: 0x000084EC File Offset: 0x000066EC
		public static bool OnPlayCheck(DialogController controller, BlueprintCheck check)
		{
			if (RollPopup.s_RunOriginalFor == check)
			{
				RollPopup.s_RunOriginalFor = null;
				return true;
			}
			if (!Main.Enabled || check == null || RollPopup.IsHeadless(controller))
			{
				return true;
			}
			if (check.Hidden && !Main.Settings.ShowHiddenChecks)
			{
				return true;
			}
			RollPopup.PendingCheck pendingCheck = new RollPopup.PendingCheck
			{
				Controller = controller,
				Check = check
			};
			if (RollPopup.IsInBookPage(controller))
			{
				RollPopup.ExecuteCheck(pendingCheck, false);
				if (RollPopup.s_Captured != null)
				{
					pendingCheck.Replay = RollPopup.s_Captured;
					RollPopup.s_Queue.Enqueue(pendingCheck);
				}
				RollPopup.s_Captured = null;
				return false;
			}
			RollPopup.s_Queue.Enqueue(pendingCheck);
			return false;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00008598 File Offset: 0x00006798
		private static bool IsInBookPage(DialogController controller)
		{
			return RollPopup.s_PlayingBookPageCount != null && (int)RollPopup.s_PlayingBookPageCount.GetValue(controller) > 0;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x000085BC File Offset: 0x000067BC
		public static bool OnPlayCue(DialogController controller, BlueprintCueBase cue)
		{
			if (!RollPopup.s_DeferNextCue)
			{
				return true;
			}
			RollPopup.s_DeferNextCue = false;
			RollPopup.s_DeferredCue = cue;
			RollPopup.s_DeferredController = controller;
			RollPopup.s_HasDeferredCue = true;
			return false;
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x000085E0 File Offset: 0x000067E0
		public static void OnResult(SkillCheckResult result, int dc, int dcAdjustment, bool isParty)
		{
			if (RollPopup.s_Capturing)
			{
				RollPopup.s_Captured = new RollPopup.Captured?(new RollPopup.Captured
				{
					Result = result,
					DC = dc,
					DCAdjustment = dcAdjustment,
					IsParty = isParty
				});
			}
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00008628 File Offset: 0x00006828
		public static void OnDialogStopped()
		{
			List<RollPopup.PendingCheck> list = new List<RollPopup.PendingCheck>();
			foreach (RollPopup.PendingCheck pendingCheck in RollPopup.s_Queue)
			{
				if (pendingCheck.IsInteraction)
				{
					list.Add(pendingCheck);
				}
			}
			RollPopup.s_Queue.Clear();
			foreach (RollPopup.PendingCheck pendingCheck2 in list)
			{
				RollPopup.s_Queue.Enqueue(pendingCheck2);
			}
			if (RollPopup.s_State == RollPopup.State.AwaitRoll && !RollPopup.s_Current.IsInteraction)
			{
				RollPopup.s_State = RollPopup.State.None;
			}
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x000086F0 File Offset: 0x000068F0
		private static bool IsHeadless(DialogController controller)
		{
			return RollPopup.s_HeadlessMode != null && (bool)RollPopup.s_HeadlessMode.GetValue(controller);
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00008720 File Offset: 0x00006920
		private void Start()
		{
			this.m_Shape = new D20Shape();
			try
			{
				this.m_Font = Font.CreateDynamicFontFromOSFont(new string[] { "Palatino Linotype", "Book Antiqua", "Georgia", "Times New Roman" }, 64);
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
			RollPopup.s_Instance = this;
			RollPopup.ReloadSounds();
			try
			{
				NativeAudio nativeAudio = NativeAudio.TryCreate(new Action<string>(Main.Mod.Logger.Log));
				this.m_Sound = new DiceSound(nativeAudio, this.m_Bank, () => Main.Settings.Volume, new Func<string, string>(Main.Settings.ChoiceFor));
			}
			catch (Exception ex2)
			{
				Main.Mod.Logger.LogException(ex2);
			}
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00008818 File Offset: 0x00006A18
		private void OnDestroy()
		{
			if (this.m_Sound != null)
			{
				this.m_Sound.Dispose();
			}
			this.m_Sound = null;
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x060000A5 RID: 165 RVA: 0x00008834 File Offset: 0x00006A34
		public static string SoundSummary
		{
			get
			{
				if (!(RollPopup.s_Instance != null))
				{
					return "none";
				}
				return RollPopup.s_Instance.m_Bank.Summary();
			}
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00008858 File Offset: 0x00006A58
		public static void ReloadSounds()
		{
			if (RollPopup.s_Instance == null)
			{
				return;
			}
			try
			{
				RollPopup.s_Instance.m_Bank.Load(Path.Combine(Main.Mod.Path, "Sounds"), new Action<string>(Main.Mod.Logger.Log));
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x000088D0 File Offset: 0x00006AD0
		public static List<DiceSound.Choice> SoundChoices(string outcome)
		{
			if (!(RollPopup.s_Instance != null) || RollPopup.s_Instance.m_Sound == null)
			{
				return null;
			}
			return RollPopup.s_Instance.m_Sound.ChoicesFor(outcome);
		}

		// Opens the roll window with made-up data (from the settings menu), so the UI can be previewed.
		public static void PreviewCheck()
		{
			if (RollPopup.s_Instance == null || RollPopup.IsBusy)
			{
				return;
			}
			RollPopup.s_Queue.Enqueue(new RollPopup.PendingCheck { Preview = true });
		}

		private static CheckInfo PreviewInfo()
		{
			CheckInfo info = new CheckInfo
			{
				Stat = StatType.SkillPersuasion,
				DC = 15,
				Context = "Preview",
				Lines = new List<BonusLine>
				{
					new BonusLine("Ranks", "Skill ranks", 5, "Rk"),
					new BonusLine("Charisma", "Ability modifier", 3, "Ch"),
					new BonusLine("Class skill", "Class skill bonus", 3, "CS"),
					new BonusLine("Other", "Smaller bonuses combined", 1, "\u2026")
				}
			};
			info.Bonus = 12;
			return info;
		}

		private void StartPreviewRoll()
		{
			int d20 = UnityEngine.Random.Range(1, 21);
			this.m_Info.HasRoll = true;
			this.m_Info.D20 = d20;
			this.m_Info.Total = d20 + this.m_Info.Bonus;
			this.m_Info.Passed = this.m_Info.Total >= this.m_Info.DC;
			this.BeginRollAnimation();
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00008938 File Offset: 0x00006B38
		public static void PreviewSound(string what)
		{
			if (RollPopup.s_Instance != null && RollPopup.s_Instance.m_Sound != null)
			{
				RollPopup.s_Instance.m_Sound.Preview(what);
			}
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00008964 File Offset: 0x00006B64
		private void Update()
		{
			if (!Main.Enabled)
			{
				this.RunWithoutPopup();
				return;
			}
			if (RollPopup.s_State == RollPopup.State.None && !RollPopup.s_HasDeferredCue && RollPopup.s_Queue.Count > 0)
			{
				this.BeginCheck(RollPopup.s_Queue.Dequeue());
			}
			bool flag = RollPopup.s_ButtonPressed;
			RollPopup.s_ButtonPressed = false;
			switch (RollPopup.s_State)
			{
			case RollPopup.State.AwaitRoll:
				if (flag || !Main.Settings.RollYourself)
				{
					this.StartRoll();
					return;
				}
				break;
			case RollPopup.State.Rolling:
			{
				float rollEnd = this.m_Roll.Duration + (this.m_UseTray ? 0.25f : 0f);
				this.m_RollTime += Time.unscaledDeltaTime;
				if (flag)
				{
					this.m_RollTime = rollEnd;
				}
				this.PlayImpacts();
				if (this.m_RollTime >= rollEnd)
				{
					this.EnterLanded();
					return;
				}
				break;
			}
			case RollPopup.State.Landed:
				// small hit: the d20 has landed; after a beat the big hit shows the verdict
				this.m_LandedTime += Time.unscaledDeltaTime;
				if (flag || this.m_LandedTime >= HitDelay)
				{
					this.EnterResult();
					return;
				}
				break;
			case RollPopup.State.Result:
				this.m_ResultTime += Time.unscaledDeltaTime;
				this.UpdateSparks(Time.unscaledDeltaTime);
				if (flag)
				{
					RollPopup.s_State = RollPopup.State.None;
					RollPopup.PlayDeferredCue();
				}
				break;
			default:
				return;
			}
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00008A68 File Offset: 0x00006C68
		private void RunWithoutPopup()
		{
			if (RollPopup.s_State == RollPopup.State.AwaitRoll)
			{
				RollPopup.ExecuteCheck(RollPopup.s_Current, false);
			}
			RollPopup.ReleasePause();
			RollPopup.s_State = RollPopup.State.None;
			RollPopup.PlayDeferredCue();
			while (RollPopup.s_Queue.Count > 0)
			{
				RollPopup.PendingCheck pendingCheck = RollPopup.s_Queue.Dequeue();
				if (pendingCheck.Replay == null)
				{
					RollPopup.ExecuteCheck(pendingCheck, false);
				}
			}
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00008AC8 File Offset: 0x00006CC8
		private void BeginCheck(RollPopup.PendingCheck pending)
		{
			RollPopup.s_Current = pending;
			this.m_UseTray = Main.Settings.UseTray;
			if (pending.Preview)
			{
				this.m_Info = RollPopup.PreviewInfo();
			}
			else if (pending.IsInteraction)
			{
				this.m_Info = this.PredictInteraction(pending);
				RollPopup.HoldPause();
			}
			else
			{
				this.m_Info = Breakdown.Predict(pending.Controller, pending.Check);
			}
			this.m_IdleLabels = this.m_Shape.DefaultLabels();
			if (this.m_UseTray)
			{
				this.m_IdlePose = DiceSim.RestingPose(this.m_Shape, UnityEngine.Random.rotationUniform);
			}
			else
			{
				this.m_IdlePose = new DiceFrame
				{
					Position = Vector3.zero,
					Rotation = DiceSim.FacingPose(this.m_Shape, this.m_IdleLabels, 20)
				};
			}
			this.m_Roll = null;
			RollPopup.s_State = RollPopup.State.AwaitRoll;
			if (pending.Replay != null)
			{
				this.StartAnimation(pending.Replay.Value, null);
			}
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00008BB8 File Offset: 0x00006DB8
		private void StartRoll()
		{
			RollPopup.ReleasePause();
			if (RollPopup.s_Current.Preview)
			{
				this.StartPreviewRoll();
				return;
			}
			string text = ((this.m_Info != null) ? this.m_Info.Context : null);
			RollPopup.ExecuteCheck(RollPopup.s_Current, true);
			if (RollPopup.s_Captured == null)
			{
				RollPopup.s_State = RollPopup.State.None;
				RollPopup.PlayDeferredCue();
				return;
			}
			this.StartAnimation(RollPopup.s_Captured.Value, text);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00008C18 File Offset: 0x00006E18
		private void StartAnimation(RollPopup.Captured c, string context)
		{
			this.m_Info = Breakdown.FromResult(c.Result, c.DC, c.DCAdjustment, c.IsParty);
			this.m_Info.Context = context;
			this.BeginRollAnimation();
		}

		private void BeginRollAnimation()
		{
			int num = Mathf.Clamp(this.m_Info.D20, 1, 20);
			this.m_Roll = (this.m_UseTray ? DiceSim.Roll(this.m_Shape, num, Main.Settings.SpinSpeed, Main.Settings.RollLength) : DiceSim.Spin(this.m_Shape, num, Main.Settings.SpinSpeed, Main.Settings.RollLength, Main.Settings.Bg3Style));
			this.m_RollTime = 0f;
			this.m_NextImpact = 0;
			this.m_RecordedDice = this.m_Sound != null && Main.Settings.Sounds && this.m_Sound.HasRecordedDice;
			this.m_LandingPlayed = false;
			this.m_LandingTime = this.LandingTime(this.m_Roll);
			if (this.m_RecordedDice && !this.m_UseTray)
			{
				this.m_Sound.PlayShake();
			}
			RollPopup.s_State = RollPopup.State.Rolling;
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00008D08 File Offset: 0x00006F08
		private float LandingTime(DiceRoll roll)
		{
			if (roll.ImpactTimes.Count == 0)
			{
				return roll.Duration;
			}
			if (!this.m_UseTray)
			{
				int best = 0;
				for (int j = 1; j < roll.ImpactTimes.Count; j++)
				{
					if (roll.ImpactStrengths[j] >= roll.ImpactStrengths[best])
					{
						best = j;
					}
				}
				return roll.ImpactTimes[best];
			}
			for (int i = 0; i < roll.ImpactTimes.Count; i++)
			{
				if (roll.ImpactStrengths[i] >= 0.35f)
				{
					return roll.ImpactTimes[i];
				}
			}
			return roll.ImpactTimes[0];
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00008D8C File Offset: 0x00006F8C
		private void EnterLanded()
		{
			if (this.m_Info.Bonus == 0)
			{
				// no bonus step: the landing is the big hit
				this.EnterResult();
				return;
			}
			RollPopup.s_State = RollPopup.State.Landed;
			this.m_LandedTime = 0f;
		}

		private void EnterResult()
		{
			RollPopup.s_State = RollPopup.State.Result;
			this.m_ResultTime = 0f;
			this.m_Seed = UnityEngine.Random.Range(0f, 1000f);
			this.SpawnSparks();
			if (this.m_Sound != null && Main.Settings.Sounds)
			{
				this.m_Sound.PlayOutcome(this.m_Info.Passed, this.m_Info.D20);
			}
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00008DE8 File Offset: 0x00006FE8
		private static void ExecuteCheck(RollPopup.PendingCheck pending, bool deferCue)
		{
			if (pending.Preview)
			{
				return;
			}
			if (pending.IsInteraction)
			{
				RollPopup.ExecuteInteraction(pending);
				return;
			}
			RollPopup.s_Captured = null;
			RollPopup.s_Capturing = true;
			RollPopup.s_DeferNextCue = deferCue;
			RollPopup.s_RunOriginalFor = pending.Check;
			try
			{
				if (pending.Controller != null && pending.Controller.Dialog != null)
				{
					RollPopup.s_PlayCheck.Invoke(pending.Controller, new object[] { pending.Check });
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
			finally
			{
				RollPopup.s_Capturing = false;
				RollPopup.s_DeferNextCue = false;
				RollPopup.s_RunOriginalFor = null;
			}
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00008EAC File Offset: 0x000070AC
		private static void PlayDeferredCue()
		{
			if (!RollPopup.s_HasDeferredCue)
			{
				return;
			}
			RollPopup.s_HasDeferredCue = false;
			DialogController dialogController = RollPopup.s_DeferredController;
			BlueprintCueBase blueprintCueBase = RollPopup.s_DeferredCue;
			RollPopup.s_DeferredController = null;
			RollPopup.s_DeferredCue = null;
			try
			{
				if (dialogController != null && dialogController.Dialog != null)
				{
					RollPopup.s_PlayCue.Invoke(dialogController, new object[] { blueprintCueBase });
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00008F28 File Offset: 0x00007128
		private void PlayImpacts()
		{
			if (this.m_Sound == null || !Main.Settings.Sounds)
			{
				this.m_NextImpact = this.m_Roll.ImpactTimes.Count;
				return;
			}
			if (this.m_RecordedDice)
			{
				if (!this.m_LandingPlayed && this.m_RollTime >= this.m_LandingTime)
				{
					this.m_LandingPlayed = true;
					this.m_Sound.PlayLanding(1f);
				}
				return;
			}
			while (this.m_NextImpact < this.m_Roll.ImpactTimes.Count && this.m_Roll.ImpactTimes[this.m_NextImpact] <= this.m_RollTime)
			{
				this.m_Sound.PlayImpact(this.m_Roll.ImpactStrengths[this.m_NextImpact]);
				this.m_NextImpact++;
			}
		}

		// Token: 0x060000BE RID: 190 RVA: 0x000093AC File Offset: 0x000075AC
		// A natural 20 / 1 is only special when it really passed / failed: neither is automatic on skill checks.
		private bool IsCrit
		{
			get
			{
				return this.m_Info != null && this.m_Info.D20 == 20 && this.m_Info.Passed;
			}
		}

		private bool IsFumble
		{
			get
			{
				return this.m_Info != null && this.m_Info.D20 == 1 && !this.m_Info.Passed;
			}
		}

		// Settled colours: gold = natural 20 that succeeds, violet = natural 20 that fails,
		// red = natural 1 that fails, teal = natural 1 that succeeds, green = success, orange-red = failure.
		private Color Tint()
		{
			bool ok = this.m_Info.Passed;
			if (this.m_Info.D20 == 20)
			{
				return ok ? new Color(1f, 0.85f, 0.2f) : new Color(0.72f, 0.4f, 0.95f);
			}
			if (this.m_Info.D20 == 1)
			{
				return ok ? new Color(0.25f, 0.82f, 0.85f) : new Color(0.9f, 0.2f, 0.2f);
			}
			return ok ? new Color(0.35f, 0.8f, 0.4f) : new Color(0.85f, 0.35f, 0.3f);
		}

		private Color DieGlow()
		{
			if (RollPopup.s_State == RollPopup.State.AwaitRoll && Main.Settings.PulseDie)
			{
				return new Color(1f, 0.8f, 0.45f, 0.05f + 0.17f * RollPopup.Beat());
			}
			if (RollPopup.s_State != RollPopup.State.Result || (Main.Settings.Bg3Style && !this.m_UseTray))
			{
				return new Color(0f, 0f, 0f, 0f);
			}
			float num = 0.25f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f);
			if (this.m_Info.D20 == 20 || this.m_Info.D20 == 1)
			{
				Color tint = this.Tint();
				return new Color(tint.r, tint.g, tint.b, num + 0.2f);
			}
			return new Color(0f, 0f, 0f, 0f);
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00009468 File Offset: 0x00007668
		private void DrawDie(Rect rect, float scale, bool tray)
		{
			if (Event.current.type != EventType.Repaint || this.m_RendererFailed)
			{
				return;
			}
			try
			{
				if (this.m_Renderer == null)
				{
					this.m_Renderer = new DiceRenderer(this.m_Shape, this.m_Font);
				}
				DiceFrame diceFrame = ((this.m_Roll != null) ? this.m_Roll.FrameAt(this.m_RollTime) : this.m_IdlePose);
				int[] array = ((this.m_Roll != null) ? this.m_Roll.Labels : this.m_IdleLabels);
				this.m_Renderer.HighlightValue = 0;
				if ((RollPopup.s_State == RollPopup.State.Result || RollPopup.s_State == RollPopup.State.Landed) && this.m_Info != null)
				{
					this.m_Renderer.HighlightValue = Mathf.Clamp(this.m_Info.D20, 1, 20);
					this.m_Renderer.HighlightColor = (RollPopup.s_State == RollPopup.State.Landed) ? new Color(0.8f, 0.73f, 0.58f) : ((Main.Settings.Bg3Style && !this.m_UseTray) ? new Color(1f, 0.9f, 0.62f) : this.Tint());
					this.m_Renderer.HighlightPulse = (RollPopup.s_State == RollPopup.State.Landed) ? 0f : 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
				}
				if (tray)
				{
					this.m_Renderer.Draw(rect, scale, diceFrame, array, this.DieGlow());
				}
				else
				{
					this.m_Renderer.DrawSingle(rect, scale, diceFrame, array, this.DieGlow());
				}
			}
			catch (Exception ex)
			{
				this.m_RendererFailed = true;
				Main.Mod.Logger.LogException(ex);
			}
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00009B14 File Offset: 0x00007D14
		public static bool OnInteract(InteractionPart part, UnitEntityData user, ItemEntity toolItem)
		{
			bool flag;
			try
			{
				RollPopup.PendingCheck pendingCheck;
				if (part == RollPopup.s_RunOriginalInteraction)
				{
					RollPopup.s_RunOriginalInteraction = null;
					flag = true;
				}
				else if (part == null || user == null || !Main.Enabled || !user.IsPlayerFaction)
				{
					flag = true;
				}
				else if (Game.Instance.Player != null && Game.Instance.Player.IsInCombat)
				{
					flag = true;
				}
				else if (!RollPopup.TryPlanInteraction(part, user, toolItem, out pendingCheck))
				{
					flag = true;
				}
				else
				{
					RollPopup.s_Queue.Enqueue(pendingCheck);
					flag = false;
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
				flag = true;
			}
			return flag;
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00009BB8 File Offset: 0x00007DB8
		private static bool TryPlanInteraction(InteractionPart part, UnitEntityData user, ItemEntity toolItem, out RollPopup.PendingCheck pending)
		{
			pending = new RollPopup.PendingCheck
			{
				Interaction = part,
				User = user,
				ToolItem = toolItem
			};
			if (!part.CanInteract())
			{
				return false;
			}
			InteractionSkillCheckPart interactionSkillCheckPart = part as InteractionSkillCheckPart;
			if (interactionSkillCheckPart == null)
			{
				return Main.Settings.InteractiveLocks && RollPopup.TryPlanLock(part, user, ref pending);
			}
			if (!Main.Settings.InteractiveMapChecks)
			{
				return false;
			}
			InteractionSkillCheckSettings settings = interactionSkillCheckPart.Settings;
			if (settings == null || (settings.OnlyCheckOnce && interactionSkillCheckPart.AlreadyUsed))
			{
				return false;
			}
			pending.Skill = (StatTypeHelper.IsSkill(interactionSkillCheckPart.SkillOverride) ? interactionSkillCheckPart.SkillOverride : settings.Skill);
			if (pending.Skill == (StatType)0)
			{
				return false;
			}
			pending.BaseDC = ((interactionSkillCheckPart.DCOverride != 0) ? interactionSkillCheckPart.DCOverride : settings.GetDC());
			pending.HideDC = settings.HideDC;
			pending.Context = RollPopup.DisplayName(settings);
			return true;
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00009CA4 File Offset: 0x00007EA4
		private static bool TryPlanLock(InteractionPart part, UnitEntityData user, ref RollPopup.PendingCheck pending)
		{
			InteractionSettings settings = part.Settings;
			if (settings != null && settings.Trap != null && settings.Trap.Data != null && settings.Trap.Data.TrapActive)
			{
				return false;
			}
			if (part.AlreadyUnlocked)
			{
				return false;
			}
			List<InteractionRestrictionPart> list = ((RollPopup.s_GetOrderedRestrictions != null) ? (RollPopup.s_GetOrderedRestrictions.Invoke(part, null) as List<InteractionRestrictionPart>) : null);
			if (list == null || list.Count == 0)
			{
				return false;
			}
			DisableDeviceRestrictionPart disableDeviceRestrictionPart = list[0] as DisableDeviceRestrictionPart;
			if (disableDeviceRestrictionPart == null || disableDeviceRestrictionPart.Unlocked || disableDeviceRestrictionPart.Jammed)
			{
				return false;
			}
			if (user.Stats.SkillThievery.BaseValue <= 0 || Game.Instance.Player.CapitalPartyMode)
			{
				return false;
			}
			pending.Skill = (StatType)27; // StatType.SkillThievery
			pending.BaseDC = disableDeviceRestrictionPart.DC;
			pending.Context = "Picking the lock";
			return true;
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00009D8C File Offset: 0x00007F8C
		private static string DisplayName(InteractionSkillCheckSettings settings)
		{
			try
			{
				if (settings.DisplayName != null && settings.DisplayName.String != null)
				{
					return settings.DisplayName.String;
				}
			}
			catch (Exception)
			{
			}
			return null;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00009DE0 File Offset: 0x00007FE0
		public static bool TryCaptureInteractionRule(RuleSkillCheck rule)
		{
			if (!RollPopup.s_CapturingInteraction || RollPopup.s_InteractionRule != null)
			{
				return false;
			}
			RollPopup.s_InteractionRule = rule;
			return true;
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00009DFC File Offset: 0x00007FFC
		private CheckInfo PredictInteraction(RollPopup.PendingCheck pending)
		{
			CheckInfo checkInfo = Breakdown.PredictFor(pending.User, pending.Skill, pending.BaseDC);
			checkInfo.HideDC = pending.HideDC;
			checkInfo.Context = pending.Context;
			return checkInfo;
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00009E40 File Offset: 0x00008040
		private static void ExecuteInteraction(RollPopup.PendingCheck pending)
		{
			RollPopup.s_Captured = null;
			RollPopup.s_InteractionRule = null;
			RollPopup.s_CapturingInteraction = true;
			RollPopup.s_RunOriginalInteraction = pending.Interaction;
			try
			{
				if (pending.Interaction != null && pending.User != null)
				{
					pending.Interaction.Interact(pending.User, pending.ToolItem);
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
			finally
			{
				RollPopup.s_CapturingInteraction = false;
				RollPopup.s_RunOriginalInteraction = null;
			}
			RuleSkillCheck ruleSkillCheck = RollPopup.s_InteractionRule;
			RollPopup.s_InteractionRule = null;
			if (ruleSkillCheck == null)
			{
				return;
			}
			RollPopup.s_Captured = new RollPopup.Captured?(new RollPopup.Captured
			{
				Result = new SkillCheckResult(ruleSkillCheck, ruleSkillCheck.Initiator),
				DC = ruleSkillCheck.DC,
				DCAdjustment = ruleSkillCheck.DifficultyDCAdjustment,
				IsParty = false
			});
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00009F3C File Offset: 0x0000813C
		private static void HoldPause()
		{
			try
			{
				if (Game.Instance != null && !Game.Instance.IsPaused)
				{
					Game.Instance.IsPaused = true;
					RollPopup.s_PausedByUs = true;
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00009F94 File Offset: 0x00008194
		private static void ReleasePause()
		{
			if (!RollPopup.s_PausedByUs)
			{
				return;
			}
			RollPopup.s_PausedByUs = false;
			try
			{
				if (Game.Instance != null)
				{
					Game.Instance.IsPaused = false;
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000AE14 File Offset: 0x00009014
		private void SpawnSparks()
		{
			this.m_Sparks.Clear();
			if (this.m_UseTray || !Main.Settings.Sparks)
			{
				return;
			}
			int num = ((this.m_Info.D20 == 20) ? 70 : (this.m_Info.Passed ? 26 : 0));
			Color color = (Main.Settings.Bg3Style && !this.m_UseTray) ? new Color(1f, 0.78f, 0.3f) : this.Tint();
			for (int i = 0; i < num; i++)
			{
				float num2 = UnityEngine.Random.Range(0f, 6.2831855f);
				float num3 = UnityEngine.Random.Range(140f, (this.m_Info.D20 == 20) ? 460f : 280f);
				float num4 = UnityEngine.Random.Range(0.7f, 1.6f);
				this.m_Sparks.Add(new RollPopup.Spark
				{
					Position = this.m_RingCenter + new Vector2(Mathf.Cos(num2), Mathf.Sin(num2)) * 330f * 0.3f,
					Velocity = new Vector2(Mathf.Cos(num2), Mathf.Sin(num2)) * num3,
					Life = num4,
					MaxLife = num4,
					Size = UnityEngine.Random.Range(8f, 18f),
					Color = color
				});
			}
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000AF8C File Offset: 0x0000918C
		private void UpdateSparks(float dt)
		{
			for (int i = this.m_Sparks.Count - 1; i >= 0; i--)
			{
				RollPopup.Spark spark = this.m_Sparks[i];
				spark.Life -= dt;
				if (spark.Life <= 0f)
				{
					this.m_Sparks.RemoveAt(i);
				}
				else
				{
					spark.Velocity *= 1f - 1.6f * dt;
					spark.Velocity.y = spark.Velocity.y + 220f * dt;
					spark.Position += spark.Velocity * dt;
					this.m_Sparks[i] = spark;
				}
			}
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000B054 File Offset: 0x00009254
		private void DrawSparks()
		{
			if (this.m_Sparks.Count == 0 || Event.current.type != EventType.Repaint)
			{
				return;
			}
			Color color = GUI.color;
			foreach (RollPopup.Spark spark in this.m_Sparks)
			{
				float num = Mathf.Clamp01(spark.Life / spark.MaxLife);
				GUI.color = new Color(spark.Color.r, spark.Color.g, spark.Color.b, num);
				float num2 = spark.Size * (0.5f + 0.5f * num);
				GUI.DrawTexture(new Rect(spark.Position.x - num2 / 2f, spark.Position.y - num2 / 2f, num2, num2), this.m_Glow);
			}
			GUI.color = color;
		}


		// Token: 0x040000E1 RID: 225
		private const float ResultDelay = 0.25f;

		// Token: 0x040000E5 RID: 229
		private const float TileSize = 70f;

		// Token: 0x040000E6 RID: 230
		private const float TileGap = 12f;

		// Token: 0x040000EE RID: 238
		private static readonly Queue<RollPopup.PendingCheck> s_Queue = new Queue<RollPopup.PendingCheck>();

		// Token: 0x040000EF RID: 239
		private static readonly MethodInfo s_PlayCheck = AccessTools.Method(typeof(DialogController), "PlayCheck", null, null);

		// Token: 0x040000F0 RID: 240
		private static readonly MethodInfo s_PlayCue = AccessTools.Method(typeof(DialogController), "PlayCue", null, null);

		// Token: 0x040000F1 RID: 241
		private static readonly FieldInfo s_HeadlessMode = AccessTools.Field(typeof(DialogController), "m_HeadlessMode");

		// Token: 0x040000F2 RID: 242
		private static readonly FieldInfo s_PlayingBookPageCount = AccessTools.Field(typeof(DialogController), "m_PlayingBookPageCount");

		// Token: 0x040000F3 RID: 243
		private static RollPopup.State s_State = RollPopup.State.None;

		// Token: 0x040000F4 RID: 244
		private static RollPopup.PendingCheck s_Current;

		// Token: 0x040000F5 RID: 245
		private static BlueprintCheck s_RunOriginalFor;

		// Token: 0x040000F6 RID: 246
		private static bool s_Capturing;

		// Token: 0x040000F7 RID: 247
		private static RollPopup.Captured? s_Captured;

		// Token: 0x040000F8 RID: 248
		private static bool s_DeferNextCue;

		// Token: 0x040000F9 RID: 249
		private static bool s_HasDeferredCue;

		// Token: 0x040000FA RID: 250
		private static BlueprintCueBase s_DeferredCue;

		// Token: 0x040000FB RID: 251
		private static DialogController s_DeferredController;

		// Token: 0x040000FC RID: 252
		private static bool s_ButtonPressed;

		// Token: 0x040000FD RID: 253
		private D20Shape m_Shape;

		// Token: 0x040000FE RID: 254
		private DiceRenderer m_Renderer;

		// Token: 0x040000FF RID: 255
		private DiceSound m_Sound;

		// Token: 0x04000100 RID: 256
		private Font m_Font;

		// Token: 0x04000101 RID: 257
		private bool m_RendererFailed;

		// Token: 0x04000102 RID: 258
		private DiceRoll m_Roll;

		// Token: 0x04000103 RID: 259
		private DiceFrame m_IdlePose;

		// Token: 0x04000104 RID: 260
		private int[] m_IdleLabels;

		// Token: 0x04000105 RID: 261
		private float m_RollTime;

		// Token: 0x04000106 RID: 262
		private float m_ResultTime;

		private float m_LandedTime;

		private float m_Seed;

		private static float HitDelay
		{
			get
			{
				return Mathf.Max(0.01f, Main.Settings.ResultPause);
			}
		}

		// Token: 0x04000107 RID: 263
		private int m_NextImpact;

		// Token: 0x04000108 RID: 264
		private CheckInfo m_Info;

		// Token: 0x04000109 RID: 265
		private bool m_UseTray;

		// Token: 0x0400011A RID: 282
		private static RollPopup s_Instance;

		// Token: 0x0400011B RID: 283
		private readonly SoundBank m_Bank = new SoundBank();

		// Token: 0x0400011C RID: 284
		private bool m_RecordedDice;

		// Token: 0x0400011D RID: 285
		private bool m_LandingPlayed;

		// Token: 0x0400011E RID: 286
		private float m_LandingTime;

		// Token: 0x0400011F RID: 287
		private static InteractionPart s_RunOriginalInteraction;

		// Token: 0x04000120 RID: 288
		private static bool s_CapturingInteraction;

		// Token: 0x04000121 RID: 289
		private static RuleSkillCheck s_InteractionRule;

		// Token: 0x04000122 RID: 290
		private static bool s_PausedByUs;

		// Token: 0x04000123 RID: 291
		private static readonly MethodInfo s_GetOrderedRestrictions = AccessTools.Method(typeof(InteractionPart), "GetOrderedRestrictions", null, null);

		// Token: 0x04000124 RID: 292
		private readonly List<RollPopup.Spark> m_Sparks = new List<RollPopup.Spark>();

		// Token: 0x04000125 RID: 293
		private Vector2 m_RingCenter;

		// Token: 0x02000026 RID: 38
		private enum State
		{
			// Token: 0x04000130 RID: 304
			None,
			// Token: 0x04000131 RID: 305
			AwaitRoll,
			// Token: 0x04000132 RID: 306
			Rolling,
			Landed,
			// Token: 0x04000133 RID: 307
			Result
		}

		// Token: 0x02000027 RID: 39
		private struct PendingCheck
		{
			// Token: 0x17000009 RID: 9
			// (get) Token: 0x060000E2 RID: 226 RVA: 0x0000BA6B File Offset: 0x00009C6B
			public bool IsInteraction
			{
				get
				{
					return this.Interaction != null;
				}
			}

			// Token: 0x04000134 RID: 308
			public DialogController Controller;

			// Token: 0x04000135 RID: 309
			public BlueprintCheck Check;

			// Token: 0x04000136 RID: 310
			public InteractionPart Interaction;

			// Token: 0x04000137 RID: 311
			public UnitEntityData User;

			// Token: 0x04000138 RID: 312
			public ItemEntity ToolItem;

			// Token: 0x04000139 RID: 313
			public StatType Skill;

			// Token: 0x0400013A RID: 314
			public int BaseDC;

			// Token: 0x0400013B RID: 315
			public bool HideDC;

			// Token: 0x0400013C RID: 316
			public string Context;

			// Token: 0x0400013D RID: 317
			public RollPopup.Captured? Replay;

			public bool Preview;
		}

		// Token: 0x02000028 RID: 40
		private struct Captured
		{
			// Token: 0x0400013E RID: 318
			public SkillCheckResult Result;

			// Token: 0x0400013F RID: 319
			public int DC;

			// Token: 0x04000140 RID: 320
			public int DCAdjustment;

			// Token: 0x04000141 RID: 321
			public bool IsParty;
		}

		// Token: 0x02000029 RID: 41
		private struct Spark
		{
			// Token: 0x04000142 RID: 322
			public Vector2 Position;

			// Token: 0x04000143 RID: 323
			public Vector2 Velocity;

			// Token: 0x04000144 RID: 324
			public float Life;

			// Token: 0x04000145 RID: 325
			public float MaxLife;

			// Token: 0x04000146 RID: 326
			public float Size;

			// Token: 0x04000147 RID: 327
			public Color Color;
		}
	}
}
