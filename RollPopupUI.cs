using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogDice
{
	// Everything the roll window draws. The look comes from the game's own UI sprites (see GameUI):
	// a paper window, ink dividers, purple buttons, framed bonus tiles and wax-seal stamps.
	// All coordinates are in a virtual 1920x1080 space; OnGUI scales them to the real screen.
	public partial class RollPopup
	{
		// Colours tuned for dark ink on the paper background.
		private static readonly Color Ink = new Color(0.20f, 0.15f, 0.11f);
		private static readonly Color Muted = new Color(0.40f, 0.33f, 0.27f);
		private static readonly Color Accent = new Color(0.47f, 0.20f, 0.12f);
		private static readonly Color Good = new Color(0.16f, 0.43f, 0.16f);
		private static readonly Color Bad = new Color(0.60f, 0.15f, 0.12f);
		private static readonly Color RuleColor = new Color(0.24f, 0.17f, 0.11f, 0.92f);
		private static readonly Color FrameColor = new Color(0.31f, 0.23f, 0.16f, 0.85f);
		private static readonly Color TileCodeColor = new Color(0.95f, 0.82f, 0.5f);
		private static readonly Color ButtonIdle = new Color(0.96f, 0.93f, 0.86f);
		private static readonly Color ButtonHot = new Color(1f, 0.88f, 0.55f);
		private static readonly Color TipText = new Color(0.92f, 0.88f, 0.78f);
		private static readonly Color TipGold = new Color(0.95f, 0.8f, 0.45f);
		private static readonly Color GlowGood = new Color(0.45f, 0.85f, 0.35f);
		private static readonly Color GlowBad = new Color(0.93f, 0.35f, 0.3f);

		private const string Dot = "   ·   ";

		// 0..1 heartbeat used by everything that pulses while the window waits for a click.
		private static float Beat()
		{
			return 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
		}

		private Texture2D m_Dimmer;
		private Texture2D m_TileBack;
		private Texture2D m_Glow;
		private Texture2D m_TooltipBg;
		private Texture2D m_PanelFallback;
		private GUIStyle m_Title;
		private GUIStyle m_Subtitle;
		private GUIStyle m_Caption;
		private GUIStyle m_Huge;
		private GUIStyle m_Big;
		private GUIStyle m_Body;
		private GUIStyle m_Small;
		private GUIStyle m_RowLabel;
		private GUIStyle m_RowValue;
		private GUIStyle m_Button;
		private GUIStyle m_TileCode;
		private GUIStyle m_Center;
		private GUIStyle m_CenterSmall;
		private GUIStyle m_CenterCaption;
		private GUIStyle m_Banner;

		private void OnGUI()
		{
			if (RollPopup.s_State == RollPopup.State.None || this.m_Info == null)
			{
				return;
			}
			this.EnsureStyles();
			float scale = (float)Screen.height / 1080f;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
			float width = (float)Screen.width / scale;
			Color previousColor = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(Main.Settings.DimAmount));
			GUI.DrawTexture(new Rect(0f, 0f, width, 1080f), this.m_Dimmer);
			GUI.color = previousColor;
			if (this.m_UseTray)
			{
				this.DrawTrayLayout(width, scale);
			}
			else if (Main.Settings.Bg3Style)
			{
				this.DrawBg3Layout(width, scale);
			}
			else
			{
				this.DrawRingLayout(width, scale);
			}
			Event current = Event.current;
			if (current.type == EventType.KeyDown && (current.keyCode == KeyCode.Space || current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
			{
				RollPopup.s_ButtonPressed = true;
				current.Use();
			}
		}

		private void EnsureStyles()
		{
			if (this.m_Title != null)
			{
				return;
			}
			GameUI.Ensure();
			this.m_Dimmer = Art.Solid(new Color(0f, 0f, 0f, 1f));
			this.m_TileBack = Art.Solid(new Color(0.16f, 0.04f, 0.03f, 1f));
			this.m_Glow = Art.Glow(128);
			this.m_TooltipBg = Art.VerticalGradient(new Color(0.13f, 0.11f, 0.14f, 0.97f), new Color(0.06f, 0.05f, 0.07f, 0.97f));
			this.m_PanelFallback = Art.Solid(new Color(0.78f, 0.74f, 0.66f, 1f));
			this.m_Title = this.MakeStyle(GUI.skin.label, 36, FontStyle.Bold, TextAnchor.MiddleCenter, Accent);
			this.m_Subtitle = this.MakeStyle(GUI.skin.label, 21, FontStyle.Normal, TextAnchor.MiddleCenter, Ink);
			this.m_Caption = this.MakeStyle(GUI.skin.label, 17, FontStyle.Bold, TextAnchor.MiddleLeft, Muted);
			this.m_Huge = this.MakeStyle(GUI.skin.label, 60, FontStyle.Bold, TextAnchor.MiddleLeft, Accent);
			this.m_Big = this.MakeStyle(GUI.skin.label, 50, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
			this.m_Body = this.MakeStyle(GUI.skin.label, 21, FontStyle.Normal, TextAnchor.MiddleLeft, Ink);
			this.m_Small = this.MakeStyle(GUI.skin.label, 15, FontStyle.Normal, TextAnchor.UpperLeft, Muted);
			this.m_Small.wordWrap = true;
			this.m_RowLabel = this.MakeStyle(GUI.skin.label, 19, FontStyle.Bold, TextAnchor.MiddleLeft, Ink);
			this.m_RowValue = this.MakeStyle(GUI.skin.label, 21, FontStyle.Bold, TextAnchor.MiddleRight, Ink);
			this.m_Button = this.MakeStyle(GUI.skin.label, 24, FontStyle.Bold, TextAnchor.MiddleCenter, ButtonIdle);
			this.m_TileCode = this.MakeStyle(GUI.skin.label, 24, FontStyle.Bold, TextAnchor.MiddleCenter, TileCodeColor);
			this.m_Center = this.MakeStyle(GUI.skin.label, 21, FontStyle.Normal, TextAnchor.MiddleCenter, Ink);
			this.m_CenterSmall = this.MakeStyle(GUI.skin.label, 16, FontStyle.Normal, TextAnchor.MiddleCenter, Muted);
			this.m_CenterSmall.wordWrap = true;
			this.m_CenterCaption = this.MakeStyle(GUI.skin.label, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Muted);
			this.m_Banner = this.MakeStyle(GUI.skin.label, 54, FontStyle.Bold, TextAnchor.MiddleCenter, Good);
		}

		private GUIStyle MakeStyle(GUIStyle source, int size, FontStyle fontStyle, TextAnchor anchor, Color color)
		{
			GUIStyle style = new GUIStyle(source)
			{
				fontSize = size,
				fontStyle = fontStyle,
				alignment = anchor,
				wordWrap = false
			};
			if (this.m_Font != null)
			{
				style.font = this.m_Font;
			}
			style.normal.textColor = color;
			return style;
		}

		// Copy of a style with another size / anchor / colour (the originals stay untouched).
		private GUIStyle Styled(GUIStyle source, int size, TextAnchor anchor, Color color)
		{
			GUIStyle style = new GUIStyle(source)
			{
				fontSize = size,
				alignment = anchor
			};
			style.normal.textColor = color;
			return style;
		}

		// ------------------------------------------------------------------ texts

		private string ButtonLabel()
		{
			if (RollPopup.s_State != RollPopup.State.Result)
			{
				return "";
			}
			return "Continue";
		}

		private string WhoRolls()
		{
			string text = "";
			if (this.m_Info.Unit != null)
			{
				text = this.m_Info.IsParty
					? (this.m_Info.Unit.CharacterName + " rolls for the party (best score)")
					: (this.m_Info.Unit.CharacterName + " rolls");
			}
			if (!string.IsNullOrEmpty(this.m_Info.Context))
			{
				text = (text.Length > 0) ? (this.m_Info.Context + Dot + text) : this.m_Info.Context;
			}
			return text;
		}

		private bool DCHidden
		{
			get
			{
				return this.m_Info.HideDC && RollPopup.s_State != RollPopup.State.Result;
			}
		}

		private string DCText()
		{
			if (!this.DCHidden)
			{
				return this.m_Info.DC.ToString();
			}
			return "?";
		}

		private string ChanceText()
		{
			if (!this.DCHidden)
			{
				return "Chance " + this.m_Info.ChancePercent + " %";
			}
			return "Chance unknown";
		}

		private string NeededText()
		{
			if (this.DCHidden)
			{
				return "DC hidden";
			}
			int needed = this.m_Info.Needed;
			if (needed <= 1)
			{
				return "Cannot fail";
			}
			if (needed <= 20)
			{
				return needed + " or higher";
			}
			return "Out of reach";
		}

		private string NeededFormula()
		{
			if (this.DCHidden)
			{
				return "Your bonus " + Breakdown.Signed(this.m_Info.Bonus);
			}
			string bonus = (this.m_Info.Bonus < 0) ? ("−" + (-this.m_Info.Bonus)) : this.m_Info.Bonus.ToString();
			return "DC " + this.m_Info.DC + " − bonus " + bonus + " = " + this.m_Info.Needed;
		}

		private string NaturalNote()
		{
			if (this.m_Info.D20 == 20)
			{
				return "Natural 20. Not an automatic success on skill checks.";
			}
			if (this.m_Info.D20 == 1)
			{
				return "Natural 1. Not an automatic failure on skill checks.";
			}
			return "";
		}

		private string MarginText()
		{
			int margin = this.m_Info.Total - this.m_Info.DC;
			return (margin >= 0) ? ("Beat by " + margin) : ("Missed by " + (-margin));
		}

		private string RollSummary()
		{
			return "d20 " + this.m_Info.D20 + "  " + Breakdown.Signed(this.m_Info.Bonus) + "  =  " + this.m_Info.Total;
		}

		// ------------------------------------------------------------------ game-styled building blocks

		private void DrawPanel(Rect panel)
		{
			if (GameUI.Panel == null)
			{
				GUI.DrawTexture(panel, this.m_PanelFallback);
				return;
			}
			GameUI.Slice(panel, GameUI.Panel, 40f, 40f, 40f, 40f, 1f, Color.white);
		}

		// Thin ink line, like the dividers in the game's journal and dialogue windows.
		private void DrawRule(float centerX, float y, float width)
		{
			GameUI.Draw(new Rect(centerX - width / 2f, y - 2f, width, 4f), GameUI.Divider, RuleColor);
		}

		private void DrawRuleLeft(float x, float y, float width)
		{
			GameUI.Draw(new Rect(x, y - 2f, width, 4f), GameUI.Divider, RuleColor);
		}

		// Sketched frame used for the cards of the tray layout.
		private void DrawCard(Rect card)
		{
			GameUI.Slice(card, GameUI.Frame, 48f, 48f, 48f, 48f, 0.8f, FrameColor);
		}

		// Dark frame (same sprite the game uses around character portraits).
		private void DrawDarkFrame(Rect inner)
		{
			GameUI.Slice(new Rect(inner.x - 8f, inner.y - 8f, inner.width + 16f, inner.height + 16f), GameUI.Portrait, 8f, 8f, 8f, 8f, 1f, Color.white);
		}

		// Die rectangle with the "hit" squash/bounce and the shake of the big hit.
		private Rect HitRect(Rect ring)
		{
			float s = 1f;
			float ox = 0f;
			float oy = 0f;
			if (RollPopup.s_State == RollPopup.State.AwaitRoll && Main.Settings.PulseDie)
			{
				bool hovering = ring.Contains(Event.current.mousePosition);
				s = hovering ? 1.08f : 1f + 0.07f * RollPopup.Beat();
			}
			if (RollPopup.s_State == RollPopup.State.Landed)
			{
				float tl = this.m_LandedTime;
				s = 1f - 0.05f * Mathf.Min(1f, tl / HitDelay) + 0.10f * Mathf.Exp(-6f * tl) * Mathf.Cos(14f * tl);
			}
			else if (RollPopup.s_State == RollPopup.State.Result)
			{
				float ts = this.m_ResultTime;
				s = 1f + 0.22f * Mathf.Exp(-4.5f * ts) * Mathf.Cos(11f * ts);
				float amp = (this.m_Info.D20 == 20 || this.m_Info.D20 == 1) ? 12f : (this.m_Info.Passed ? 0f : 6f);
				if (!Main.Settings.ShakeOnResult)
				{
					amp = 0f;
				}
				float k = amp * Mathf.Exp(-6f * ts);
				ox = Mathf.Sin(ts * 70f + this.m_Seed) * k;
				oy = Mathf.Cos(ts * 83f + this.m_Seed) * k * 0.7f;
			}
			Vector2 c = ring.center + new Vector2(ox, oy);
			return new Rect(c.x - ring.width * s / 2f, c.y - ring.height * s / 2f, ring.width * s, ring.height * s);
		}

		// Light flash + faint ring on landing (small hit); white flash + strong ring on the verdict (big hit).
		private void DrawHitEffects(Rect ring)
		{
			Rect disc = new Rect(ring.x + 16f, ring.y + 16f, ring.width - 32f, ring.height - 32f);
			if (RollPopup.s_State == RollPopup.State.Landed)
			{
				float tl = this.m_LandedTime;
				if (tl < 0.25f)
				{
					GameUI.Draw(disc, GameUI.Disc, new Color(1f, 1f, 1f, 0.28f * Mathf.Exp(-12f * tl)));
				}
				if (tl < 0.45f)
				{
					float k = 1f - Mathf.Exp(-5f * tl);
					this.DrawExpandingRing(ring, 1f + 0.45f * k, new Color(0.75f, 0.75f, 0.85f, 0.45f * (1f - tl / 0.45f)));
				}
			}
			else if (RollPopup.s_State == RollPopup.State.Result)
			{
				float ts = this.m_ResultTime;
				if (ts < 0.35f)
				{
					GameUI.Draw(disc, GameUI.Disc, new Color(1f, 1f, 1f, 0.6f * Mathf.Exp(-9f * ts)));
				}
				if (ts < 0.8f)
				{
					float k = 1f - Mathf.Exp(-4.5f * ts);
					Color tint = this.Tint();
					this.DrawExpandingRing(ring, 1f + 0.95f * k, new Color(tint.r, tint.g, tint.b, 0.75f * (1f - ts / 0.8f)));
				}
			}
		}

		private void DrawExpandingRing(Rect ring, float scale, Color color)
		{
			Rect r = new Rect(ring.center.x - ring.width * scale / 2f, ring.center.y - ring.height * scale / 2f, ring.width * scale, ring.height * scale);
			GameUI.Draw(r, GameUI.Ring, color);
		}

		// Clicking the die rolls it (Space / Enter still work too).
		private void DrawDieClick(Rect dieRect, Rect hintRect)
		{
			if (RollPopup.s_State != RollPopup.State.AwaitRoll)
			{
				return;
			}
			if (Main.Settings.ShowClickHint)
			{
				bool hover = dieRect.Contains(Event.current.mousePosition);
				float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
				Color hint = hover ? ButtonHot : TipGold;
				hint.a = hover ? 1f : 0.55f + 0.35f * pulse;
				GUI.Label(hintRect, "CLICK THE DIE TO ROLL", this.Styled(this.m_CenterCaption, 18, TextAnchor.MiddleCenter, hint));
			}
			if (GUI.Button(dieRect, GUIContent.none, GUIStyle.none))
			{
				RollPopup.s_ButtonPressed = true;
			}
		}

		private void DrawButton(Rect panel)
		{
			string text = this.ButtonLabel();
			if (text.Length == 0)
			{
				return;
			}
			Rect rect = new Rect(panel.x + (panel.width - 266f) / 2f, panel.yMax - 58f - 34f, 266f, 58f);
			bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
			bool hover = rect.Contains(Event.current.mousePosition);
			bool pressed = hover && GUIUtility.hotControl != 0;
			Texture2D skin = pressed ? GameUI.ButtonClick : (hover ? GameUI.ButtonHover : GameUI.Button);
			if (Main.Settings.Bg3Style && !this.m_UseTray)
			{
				RollPopup.FillRect(rect, pressed ? new Color(0.35f, 0.22f, 0.08f) : (hover ? new Color(0.80f, 0.58f, 0.24f) : new Color(0.62f, 0.42f, 0.15f)));
				RollPopup.FillRect(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), pressed ? new Color(0.08f, 0.05f, 0.03f) : (hover ? new Color(0.24f, 0.16f, 0.08f) : new Color(0.15f, 0.10f, 0.07f)));
			}
			else
			{
				GameUI.Slice(rect, skin, 30f, 30f, 26f, 26f, 0.5f, Color.white);
			}
			this.m_Button.normal.textColor = hover ? ButtonHot : ButtonIdle;
			GUI.Label(rect, text, this.m_Button);
			if (clicked)
			{
				RollPopup.s_ButtonPressed = true;
			}
		}

		// The wax seal that "stamps" onto the window when the roll is resolved.
		private void DrawStamp(Rect target)
		{
			if (RollPopup.s_State != RollPopup.State.Result || !Main.Settings.ShowStamp)
			{
				return;
			}
			Texture2D stamp = this.m_Info.Passed ? GameUI.StampPass : GameUI.StampFail;
			if (stamp == null)
			{
				return;
			}
			float t = Mathf.Clamp01(this.m_ResultTime / 0.25f);
			float eased = 1f - (1f - t) * (1f - t);
			GameUI.Stamp(target, stamp, Mathf.Lerp(1.8f, 1f, eased), t);
		}

		private void DrawPortrait(Rect rect)
		{
			Sprite sprite = null;
			try
			{
				if (this.m_Info.Unit != null && this.m_Info.Unit.Descriptor.Portrait != null)
				{
					sprite = this.m_Info.Unit.Descriptor.Portrait.SmallPortrait;
				}
			}
			catch (Exception)
			{
			}
			if (sprite == null)
			{
				return;
			}
			Art.DrawSprite(rect, sprite);
			this.DrawDarkFrame(rect);
		}

		// ------------------------------------------------------------------ ring layout

		private void DrawRingLayout(float width, float scale)
		{
			Rect panel = new Rect((width - 780f) / 2f, 40f, 780f, 1000f);
			this.DrawPanel(panel);
			float cx = panel.center.x;
			float y = panel.y + 16f;
			this.DrawPortrait(new Rect(panel.x + 52f, y + 34f, 64f, 80f));
			GUI.Label(new Rect(panel.x + 130f, y + 30f, panel.width - 260f, 44f), Breakdown.StatName(this.m_Info.Stat), this.m_Title);
			GUI.Label(new Rect(panel.x + 130f, y + 74f, panel.width - 260f, 30f), this.WhoRolls(), this.m_Subtitle);
			this.DrawRule(cx, y + 128f, panel.width - 150f);
			GUI.Label(new Rect(panel.x, y + 138f, panel.width, 22f), "DIFFICULTY (DC)", this.m_CenterCaption);
			GUIStyle dcStyle = this.Styled(this.m_Huge, 64, TextAnchor.MiddleCenter, Ink);
			GUI.Label(new Rect(panel.x, y + 160f, panel.width, 70f), this.DCText(), dcStyle);
			Rect ring = new Rect(cx - 165f, y + 244f, 330f, 330f);
			this.m_RingCenter = ring.center;
			this.DrawRing(ring);
			this.DrawDie(this.HitRect(ring), scale, false);
			this.DrawHitEffects(ring);
			this.DrawDieClick(ring, new Rect(ring.x, ring.yMax - 44f, ring.width, 26f));
			this.DrawSparks();
			this.DrawStamp(new Rect(ring.xMax - 110f, ring.yMax - 112f, 118f, 124f));
			float below = ring.yMax + 14f;
			if (RollPopup.s_State == RollPopup.State.Result)
			{
				this.DrawRingOutcome(panel, below);
			}
			else
			{
				this.DrawRingOdds(panel, below);
			}
			this.DrawRule(cx, y + 712f, panel.width - 150f);
			string caption = (this.m_Info.Unit != null) ? ("BONUS FROM " + this.m_Info.Unit.CharacterName.ToUpperInvariant()) : "BONUS";
			GUI.Label(new Rect(panel.x, y + 722f, panel.width, 22f), caption, this.m_CenterCaption);
			float tilesY = y + 752f;
			this.DrawTiles(panel, tilesY);
			GUIStyle totalStyle = this.Styled(this.m_RowLabel, 22, TextAnchor.MiddleCenter, Accent);
			string total = (RollPopup.s_State == RollPopup.State.Result) ? this.RollSummary() : ("Total bonus " + Breakdown.Signed(this.m_Info.Bonus));
			GUI.Label(new Rect(panel.x, y + 852f, panel.width, 28f), total, totalStyle);
			this.DrawButton(panel);
			this.DrawTileTooltip(panel, tilesY);
		}

		// The die sits on a dark disc inside the silver ring from the game's difficulty selector.
		private void DrawRing(Rect ring)
		{
			float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f);
			Color glow;
			if (RollPopup.s_State == RollPopup.State.Result)
			{
				Color tint = this.Tint();
				glow = new Color(tint.r, tint.g, tint.b, (this.m_Info.D20 == 20 || this.m_Info.D20 == 1) ? 0.75f : 0.5f);
			}
			else if (RollPopup.s_State == RollPopup.State.Rolling || RollPopup.s_State == RollPopup.State.Landed)
			{
				glow = new Color(1f, 0.78f, 0.4f, 0.45f + 0.15f * pulse);
			}
			else
			{
				glow = new Color(1f, 0.78f, 0.4f, Main.Settings.PulseDie ? (0.22f + 0.38f * RollPopup.Beat()) : (0.18f + 0.1f * pulse));
			}
			if (!Main.Settings.RingGlow)
			{
				glow.a = 0f;
			}
			float spread = ring.width * 0.3f;
			GameUI.Draw(new Rect(ring.x - spread, ring.y - spread, ring.width + 2f * spread, ring.height + 2f * spread), this.m_Glow, new Color(glow.r, glow.g, glow.b, glow.a * 0.7f));
			GameUI.Draw(new Rect(ring.x + 16f, ring.y + 16f, ring.width - 32f, ring.height - 32f), GameUI.Disc, new Color(0.1f, 0.08f, 0.09f, 0.96f));
			GameUI.Draw(ring, GameUI.Ring, Color.white);
		}

		private void DrawRingOdds(Rect panel, float y)
		{
			GUI.Label(new Rect(panel.x, y, panel.width, 22f), "NEEDED ON THE D20", this.m_CenterCaption);
			int needed = this.m_Info.Needed;
			bool special = this.DCHidden || needed <= 1 || needed > 20;
			GUIStyle style = this.Styled(this.m_Huge, special ? 36 : 44, TextAnchor.MiddleCenter, (!this.DCHidden && needed > 20) ? Bad : Accent);
			GUI.Label(new Rect(panel.x, y + 20f, panel.width, 50f), this.NeededText(), style);
			GUI.Label(new Rect(panel.x, y + 70f, panel.width, 24f), this.ChanceText() + Dot + this.NeededFormula(), this.m_CenterSmall);
		}

		private void DrawRingOutcome(Rect panel, float y)
		{
			bool passed = this.m_Info.Passed;
			Color color = passed ? Good : Bad;
			float t = Mathf.Clamp01(this.m_ResultTime / 0.25f);
			GameUI.Draw(new Rect(panel.center.x - 230f, y - 14f, 460f, 96f), this.m_Glow, new Color(color.r, color.g, color.b, 0.2f * t));
			GUIStyle banner = this.Styled(this.m_Banner, Mathf.RoundToInt(Mathf.Lerp(70f, 54f, t)), TextAnchor.MiddleCenter, color);
			GUI.Label(new Rect(panel.x, y, panel.width, 64f), passed ? "SUCCESS" : "FAILURE", banner);
			GUI.Label(new Rect(panel.x, y + 62f, panel.width, 24f), this.m_Info.Total + " vs DC " + this.m_Info.DC + Dot + this.MarginText(), this.m_Center);
			string note = this.NaturalNote();
			if (note.Length > 0)
			{
				GUI.Label(new Rect(panel.x, y + 88f, panel.width, 22f), note, this.m_CenterSmall);
			}
		}

		private void DrawTiles(Rect panel, float y)
		{
			List<BonusLine> lines = this.m_Info.Lines;
			int count = lines.Count;
			if (count == 0)
			{
				GUI.Label(new Rect(panel.x, y + 20f, panel.width, 24f), "No bonus", this.m_CenterSmall);
				return;
			}
			float strip = (float)count * TileSize + (float)(count - 1) * TileGap;
			float left = panel.center.x - strip / 2f;
			for (int i = 0; i < count; i++)
			{
				Rect tile = new Rect(left + (float)i * (TileSize + TileGap), y, TileSize, TileSize);
				this.DrawTile(tile, lines[i]);
				GUI.Label(tile, new GUIContent("", "tile" + i), GUIStyle.none);
				GUIStyle value = this.Styled(this.m_RowValue, 20, TextAnchor.MiddleCenter, (lines[i].Value >= 0) ? Ink : Bad);
				GUI.Label(new Rect(tile.x - 6f, tile.yMax + 4f, tile.width + 12f, 24f), Breakdown.Signed(lines[i].Value), value);
			}
		}

		private void DrawTile(Rect tile, BonusLine line)
		{
			Rect back = new Rect(tile.x - 3f, tile.y - 3f, tile.width + 6f, tile.height + 6f);
			GUI.DrawTexture(back, this.m_TileBack);
			GameUI.Draw(back, GameUI.TileBg, Color.white);
			bool iconDrawn = false;
			if (line.Icon != null)
			{
				try
				{
					Art.DrawSprite(new Rect(tile.x + 5f, tile.y + 5f, tile.width - 10f, tile.height - 10f), line.Icon);
					iconDrawn = true;
				}
				catch (Exception)
				{
				}
			}
			if (!iconDrawn)
			{
				GUI.Label(tile, line.Code ?? "", this.m_TileCode);
			}
			GameUI.Slice(new Rect(tile.x - 6f, tile.y - 6f, tile.width + 12f, tile.height + 12f), GameUI.TileFrame, 16f, 16f, 16f, 16f, 0.78f, Color.white);
		}

		// Dark tooltip (like the game's own tooltips) above the tile the mouse is over.
		private void DrawTileTooltip(Rect panel, float tilesY)
		{
			string tooltip = GUI.tooltip;
			if (string.IsNullOrEmpty(tooltip) || !tooltip.StartsWith("tile"))
			{
				return;
			}
			int index;
			if (!int.TryParse(tooltip.Substring(4), out index) || index < 0 || index >= this.m_Info.Lines.Count)
			{
				return;
			}
			BonusLine line = this.m_Info.Lines[index];
			int count = this.m_Info.Lines.Count;
			float strip = (float)count * TileSize + (float)(count - 1) * TileGap;
			float tileX = panel.center.x - strip / 2f + (float)index * (TileSize + TileGap);
			GUIStyle description = this.Styled(this.m_Small, 16, TextAnchor.UpperLeft, Muted);
			description.normal.textColor = new Color(0.78f, 0.74f, 0.66f);
			description.wordWrap = true;
			float textHeight = description.CalcHeight(new GUIContent(line.Description), 296f);
			float height = 42f + textHeight + 14f;
			float x = Mathf.Clamp(tileX + TileSize / 2f - 160f, panel.x + 40f, panel.xMax - 320f - 40f);
			Rect box = new Rect(x, tilesY - height - 16f, 320f, height);
			GUI.DrawTexture(new Rect(box.x - 6f, box.y - 6f, box.width + 12f, box.height + 12f), this.m_TooltipBg);
			this.DrawDarkFrame(box);
			GUI.Label(new Rect(box.x + 12f, box.y + 10f, 296f, 26f), line.Label, this.Styled(this.m_RowLabel, 19, TextAnchor.MiddleLeft, TipText));
			GUI.Label(new Rect(box.x + 12f, box.y + 10f, 296f, 26f), Breakdown.Signed(line.Value), this.Styled(this.m_RowValue, 20, TextAnchor.MiddleRight, TipGold));
			GUI.Label(new Rect(box.x + 12f, box.y + 40f, 296f, textHeight), line.Description, description);
		}

		// ------------------------------------------------------------------ tray layout

		private void DrawTrayLayout(float width, float scale)
		{
			Rect panel = new Rect((width - 1100f) / 2f, 50f, 1100f, 960f);
			this.DrawPanel(panel);
			float y = panel.y + 14f;
			GUI.Label(new Rect(panel.x, y + 26f, panel.width, 44f), Breakdown.StatName(this.m_Info.Stat) + " check", this.m_Title);
			GUI.Label(new Rect(panel.x, y + 72f, panel.width, 30f), this.WhoRolls(), this.m_Subtitle);
			Rect tray = new Rect(panel.x + 60f, y + 116f, panel.width - 120f, 288f);
			this.DrawDie(tray, scale, true);
			this.DrawDieClick(tray, new Rect(tray.x, tray.yMax - 40f, tray.width, 26f));
			this.DrawDarkFrame(tray);
			Rect left = new Rect(panel.x + 60f, y + 424f, 440f, 396f);
			Rect right = new Rect(left.xMax + 20f, left.y, panel.xMax - 60f - left.xMax - 20f, left.height);
			this.DrawCard(left);
			this.DrawCard(right);
			if (RollPopup.s_State == RollPopup.State.Result)
			{
				this.DrawTrayOutcome(left);
			}
			else
			{
				this.DrawTrayOdds(left);
			}
			this.DrawTrayBonuses(right);
			this.DrawButton(panel);
		}

		private void DrawTrayOdds(Rect card)
		{
			float x = card.x + 56f;
			float w = card.width - 112f;
			float y = card.y + 40f;
			GUI.Label(new Rect(x, y, w, 24f), "Difficulty (DC)", this.m_Caption);
			GUI.Label(new Rect(x, y + 22f, w, 56f), this.DCText(), this.m_Big);
			y += 106f;
			this.DrawRuleLeft(x, y, w);
			y += 14f;
			GUI.Label(new Rect(x, y, w, 24f), "Needed on the d20", this.m_Caption);
			int needed = this.m_Info.Needed;
			bool special = this.DCHidden || needed <= 1 || needed > 20;
			GUIStyle style = this.Styled(this.m_Huge, special ? 36 : 44, TextAnchor.MiddleLeft, (!this.DCHidden && needed > 20) ? Bad : Accent);
			GUI.Label(new Rect(x, y + 22f, w, 72f), this.NeededText(), style);
			GUI.Label(new Rect(x, y + 98f, w, 28f), this.ChanceText(), this.m_Body);
			GUI.Label(new Rect(x, y + 130f, w, 44f), "Calculation: " + this.NeededFormula(), this.m_Small);
		}

		private void DrawTrayOutcome(Rect card)
		{
			float x = card.x + 56f;
			float w = card.width - 112f;
			float y = card.y + 40f;
			bool passed = this.m_Info.Passed;
			GUIStyle banner = this.Styled(this.m_Huge, 46, TextAnchor.MiddleLeft, passed ? Good : Bad);
			GUI.Label(new Rect(x, y, w - 110f, 64f), passed ? "Success" : "Failure", banner);
			this.DrawStamp(new Rect(card.xMax - 56f - 104f, card.y + 30f, 104f, 110f));
			y += 82f;
			GUI.Label(new Rect(x, y, w, 30f), this.RollSummary(), this.m_Body);
			GUI.Label(new Rect(x, y + 32f, w, 30f), "Needed: " + this.m_Info.DC + " (DC)", this.m_Body);
			y += 76f;
			this.DrawRuleLeft(x, y, w);
			y += 14f;
			GUI.Label(new Rect(x, y, w, 30f), this.MarginText(), this.m_Body);
			string note = this.NaturalNote();
			if (note.Length > 0)
			{
				GUI.Label(new Rect(x, y + 34f, w, 60f), note, this.m_Small);
			}
		}

		private void DrawTrayBonuses(Rect card)
		{
			float x = card.x + 56f;
			float w = card.width - 112f;
			float y = card.y + 40f;
			string caption = (this.m_Info.Unit != null) ? ("Bonus from " + this.m_Info.Unit.CharacterName) : "Bonus";
			GUI.Label(new Rect(x, y, w, 24f), caption, this.m_Caption);
			y += 32f;
			if (RollPopup.s_State == RollPopup.State.Result)
			{
				this.DrawTrayRow(x, y, w, "d20", "Your roll", this.m_Info.D20.ToString(), Accent);
				y += 34f;
			}
			foreach (BonusLine line in this.m_Info.Lines)
			{
				this.DrawTrayRow(x, y, w, line.Label, line.Description, Breakdown.Signed(line.Value), (line.Value >= 0) ? Ink : Bad);
				y += 34f;
			}
			this.DrawRuleLeft(x, y + 2f, w);
			y += 10f;
			GUIStyle labelStyle = this.Styled(this.m_RowLabel, 22, TextAnchor.MiddleLeft, Accent);
			GUIStyle valueStyle = this.Styled(this.m_RowLabel, 22, TextAnchor.MiddleRight, Accent);
			if (RollPopup.s_State == RollPopup.State.Result)
			{
				GUI.Label(new Rect(x, y, w, 28f), "Total", labelStyle);
				GUI.Label(new Rect(x, y, w, 28f), this.m_Info.Total.ToString(), valueStyle);
				return;
			}
			GUI.Label(new Rect(x, y, w, 28f), "Total bonus", labelStyle);
			GUI.Label(new Rect(x, y, w, 28f), Breakdown.Signed(this.m_Info.Bonus), valueStyle);
		}

		private void DrawTrayRow(float x, float y, float w, string label, string description, string value, Color valueColor)
		{
			GUI.Label(new Rect(x, y, w - 70f, 22f), label, this.m_RowLabel);
			GUI.Label(new Rect(x, y + 17f, w - 70f, 18f), description, this.m_Small);
			GUI.Label(new Rect(x, y, w, 26f), value, this.Styled(this.m_RowValue, this.m_RowValue.fontSize, TextAnchor.MiddleRight, valueColor));
		}
	}
}
