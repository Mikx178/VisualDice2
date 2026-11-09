using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogDice
{
	// The Baldur's Gate 3 style single-die window: dark panel with a bronze frame, "Difficulty Class"
	// header, a dark arena where the silver d20 tumbles in, gold sparks and a gold verdict line.
	public partial class RollPopup
	{
		private static readonly Color BgGold = new Color(0.93f, 0.74f, 0.32f);
		private static readonly Color BgText = new Color(0.94f, 0.91f, 0.84f);
		private static readonly Color BgMuted = new Color(0.66f, 0.60f, 0.52f);
		private static readonly Color BgBad = new Color(0.90f, 0.40f, 0.32f);

		private Texture2D m_Bg3Panel;

		private static void FillRect(Rect rect, Color color)
		{
			Color previous = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, Texture2D.whiteTexture);
			GUI.color = previous;
		}

		private static Rect Local(Rect panel, Rect local)
		{
			return new Rect(panel.x + local.x, panel.y + local.y, local.width, local.height);
		}

		private void DrawBg3Layout(float width, float scale)
		{
			if (this.m_Bg3Panel == null)
			{
				this.m_Bg3Panel = Bg3Art.Panel(300, 480);
			}
			Rect panel = new Rect((width - Bg3Art.PanelWidth) / 2f, 60f, Bg3Art.PanelWidth, Bg3Art.PanelHeight);
			GUI.DrawTexture(panel, this.m_Bg3Panel);
			Rect header = RollPopup.Local(panel, Bg3Art.Header);
			Rect arena = RollPopup.Local(panel, Bg3Art.Arena);
			Rect info = RollPopup.Local(panel, Bg3Art.Info);

			// header: portrait, stat, who rolls, difficulty class
			this.DrawPortrait(new Rect(header.x + 30f, header.y + 44f, 56f, 70f));
			GUI.Label(new Rect(header.x, header.y + 10f, header.width, 28f), Breakdown.StatName(this.m_Info.Stat).ToUpperInvariant(), this.Styled(this.m_Title, 22, TextAnchor.MiddleCenter, BgGold));
			GUI.Label(new Rect(header.x + 100f, header.y + 36f, header.width - 200f, 22f), this.WhoRolls(), this.Styled(this.m_CenterSmall, 15, TextAnchor.MiddleCenter, BgMuted));
			GUI.Label(new Rect(header.x, header.y + 60f, header.width, 20f), "DIFFICULTY CLASS", this.Styled(this.m_CenterCaption, 15, TextAnchor.MiddleCenter, BgText));
			bool rolled = RollPopup.s_State != RollPopup.State.AwaitRoll;
			Color dcColor = rolled ? new Color(0.80f, 0.62f, 0.38f, 0.75f) : Color.white;
			GUI.Label(new Rect(header.x, header.y + 80f, header.width, 72f), this.DCText(), this.Styled(this.m_Huge, 60, TextAnchor.MiddleCenter, dcColor));

			// arena: the die
			Vector2 c = arena.center;
			this.m_RingCenter = c;
			Rect dieRect = new Rect(c.x - 165f, c.y - 165f, 330f, 330f);
			if (RollPopup.s_State == RollPopup.State.AwaitRoll && Main.Settings.PulseDie && Main.Settings.RingGlow)
			{
				GameUI.Draw(new Rect(c.x - 210f, c.y - 210f, 420f, 420f), this.m_Glow, new Color(0.95f, 0.88f, 0.70f, 0.07f + 0.16f * RollPopup.Beat()));
			}
			this.DrawDie(this.HitRect(dieRect), scale, false);
			this.DrawBg3Hit(c);
			this.DrawDieClick(dieRect, new Rect(arena.x, arena.yMax - 40f, arena.width, 26f));
			this.DrawSparks();
			if (RollPopup.s_State == RollPopup.State.Result)
			{
				this.DrawBg3Outcome(arena);
			}
			else
			{
				this.DrawBg3Odds(arena);
			}

			// bonus strip
			string caption = (this.m_Info.Unit != null) ? ("BONUS FROM " + this.m_Info.Unit.CharacterName.ToUpperInvariant()) : "BONUS";
			GUI.Label(new Rect(info.x, info.y + 6f, info.width, 20f), caption, this.Styled(this.m_CenterCaption, 15, TextAnchor.MiddleCenter, BgMuted));
			float tilesY = info.y + 30f;
			this.DrawTilesBg3(info, tilesY);
			string total = (RollPopup.s_State == RollPopup.State.Result) ? this.RollSummary() : ("Total bonus " + Breakdown.Signed(this.m_Info.Bonus));
			GUI.Label(new Rect(info.x, info.y + 128f, info.width, 22f), total, this.Styled(this.m_RowLabel, 19, TextAnchor.MiddleCenter, BgGold));
			this.DrawButton(panel);
			this.DrawTileTooltip(panel, tilesY);
		}

		// Golden flash when the die lands and when the verdict appears.
		private void DrawBg3Hit(Vector2 c)
		{
			float a = 0f;
			if (RollPopup.s_State == RollPopup.State.Landed)
			{
				a = 0.35f * Mathf.Exp(-9f * this.m_LandedTime);
			}
			else if (RollPopup.s_State == RollPopup.State.Result)
			{
				a = 0.55f * Mathf.Exp(-5f * this.m_ResultTime);
			}
			if (a > 0.01f)
			{
				GameUI.Draw(new Rect(c.x - 230f, c.y - 230f, 460f, 460f), this.m_Glow, new Color(BgGold.r, BgGold.g, BgGold.b, a));
			}
		}

		private void DrawBg3Odds(Rect arena)
		{
			float y = arena.yMax - 100f;
			GUI.Label(new Rect(arena.x, y, arena.width, 18f), "NEEDED ON THE D20", this.Styled(this.m_CenterCaption, 15, TextAnchor.MiddleCenter, BgMuted));
			int needed = this.m_Info.Needed;
			bool special = this.DCHidden || needed <= 1 || needed > 20;
			Color color = (!this.DCHidden && needed > 20) ? BgBad : BgGold;
			GUI.Label(new Rect(arena.x, y + 16f, arena.width, 44f), this.NeededText(), this.Styled(this.m_Huge, special ? 30 : 38, TextAnchor.MiddleCenter, color));
			GUI.Label(new Rect(arena.x, y + 62f, arena.width, 22f), this.ChanceText() + Dot + this.NeededFormula(), this.Styled(this.m_CenterSmall, 15, TextAnchor.MiddleCenter, BgMuted));
		}

		private void DrawBg3Outcome(Rect arena)
		{
			bool passed = this.m_Info.Passed;
			Color color = passed ? BgGold : BgBad;
			float t = Mathf.Clamp01(this.m_ResultTime / 0.3f);
			float bandY = arena.yMax - 120f;
			GameUI.Draw(new Rect(arena.x - 30f, bandY - 12f, arena.width + 60f, 48f), this.m_Glow, new Color(color.r, color.g, color.b, 0.55f * t));
			RollPopup.FillRect(new Rect(arena.x + 20f, bandY + 10f, arena.width - 40f, 3f), new Color(color.r, color.g, color.b, 0.85f * t));
			GUIStyle banner = this.Styled(this.m_Banner, Mathf.RoundToInt(Mathf.Lerp(46f, 34f, t)), TextAnchor.MiddleCenter, new Color(color.r, color.g, color.b, t));
			GUI.Label(new Rect(arena.x, bandY + 20f, arena.width, 44f), passed ? "SUCCESS" : "FAILURE", banner);
			GUI.Label(new Rect(arena.x, bandY + 64f, arena.width, 24f), this.m_Info.Total + " vs DC " + this.m_Info.DC + Dot + this.MarginText(), this.Styled(this.m_Center, 18, TextAnchor.MiddleCenter, BgText));
			string note = this.NaturalNote();
			if (note.Length > 0)
			{
				GUI.Label(new Rect(arena.x, bandY + 88f, arena.width, 22f), note, this.Styled(this.m_CenterSmall, 14, TextAnchor.MiddleCenter, BgMuted));
			}
		}

		private void DrawTilesBg3(Rect info, float y)
		{
			List<BonusLine> lines = this.m_Info.Lines;
			int count = lines.Count;
			if (count == 0)
			{
				GUI.Label(new Rect(info.x, y + 20f, info.width, 24f), "No bonus", this.Styled(this.m_CenterSmall, 16, TextAnchor.MiddleCenter, BgMuted));
				return;
			}
			float strip = (float)count * TileSize + (float)(count - 1) * TileGap;
			float left = info.center.x - strip / 2f;
			for (int i = 0; i < count; i++)
			{
				Rect tile = new Rect(left + (float)i * (TileSize + TileGap), y, TileSize, TileSize);
				this.DrawTile(tile, lines[i]);
				GUI.Label(tile, new GUIContent("", "tile" + i), GUIStyle.none);
				GUIStyle value = this.Styled(this.m_RowValue, 20, TextAnchor.MiddleCenter, (lines[i].Value >= 0) ? BgText : BgBad);
				GUI.Label(new Rect(tile.x - 6f, tile.yMax + 4f, tile.width + 12f, 24f), Breakdown.Signed(lines[i].Value), value);
			}
		}
	}
}
