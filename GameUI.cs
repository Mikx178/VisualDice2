using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace DialogDice
{
	// The game's own UI sprites (paper window, purple buttons, wax seals, ...).
	// They are embedded in this DLL as resources (see the Resources folder and DialogDice.csproj)
	// and drawn with IMGUI, so the roll window looks like the rest of the game.
	public static class GameUI
	{
		public static Texture2D Panel;        // UI_BackgroundModalWindow  (paper)
		public static Texture2D Frame;        // UI_ContentTextFrame       (sketched frame)
		public static Texture2D Button;       // UI_Button_Default
		public static Texture2D ButtonHover;  // UI_Button_Hover
		public static Texture2D ButtonClick;  // UI_Button_Click
		public static Texture2D TileBg;       // UI_SlotBackground
		public static Texture2D TileFrame;    // UI_FrameIconSpell
		public static Texture2D Portrait;     // UI_HudCharacterFrameBorder_Default
		public static Texture2D Divider;      // UI_Dialogue_line01        (ink line)
		public static Texture2D StampPass;    // UI_QuestNotification_StampGreen
		public static Texture2D StampFail;    // UI_QuestNotification_StampRed
		public static Texture2D Ring;         // UI_Settings_Difficulty_ActiveDifficulty_Hover
		public static Texture2D Disc;         // UI_Circle

		public static void Ensure()
		{
			if (GameUI.s_Loaded)
			{
				return;
			}
			GameUI.s_Loaded = true;
			GameUI.Panel = GameUI.Load("ui_panel");
			GameUI.Frame = GameUI.Load("ui_frame");
			GameUI.Button = GameUI.Load("ui_button");
			GameUI.ButtonHover = GameUI.Load("ui_button_hover");
			GameUI.ButtonClick = GameUI.Load("ui_button_click");
			GameUI.TileBg = GameUI.Load("ui_tile_bg");
			GameUI.TileFrame = GameUI.Load("ui_tile_frame");
			GameUI.Portrait = GameUI.Load("ui_portrait");
			GameUI.Divider = GameUI.Load("ui_divider");
			GameUI.StampPass = GameUI.Load("ui_stamp_pass");
			GameUI.StampFail = GameUI.Load("ui_stamp_fail");
			GameUI.Ring = GameUI.Load("ui_ring");
			GameUI.Disc = GameUI.Load("ui_disc");
		}

		private static Texture2D Load(string name)
		{
			try
			{
				using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DialogDice.Resources." + name + ".png"))
				{
					if (stream == null)
					{
						Main.Mod.Logger.Log("Embedded texture missing: " + name);
						return null;
					}
					byte[] bytes;
					using (MemoryStream memory = new MemoryStream())
					{
						stream.CopyTo(memory);
						bytes = memory.ToArray();
					}
					Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
					if (!texture.LoadImage(bytes))
					{
						Main.Mod.Logger.Log("Embedded texture unreadable: " + name);
						return null;
					}
					GameUI.Bleed(texture);
					texture.wrapMode = TextureWrapMode.Clamp;
					texture.filterMode = FilterMode.Bilinear;
					texture.hideFlags = HideFlags.HideAndDontSave;
					return texture;
				}
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
				return null;
			}
		}

		// Fully transparent pixels in these sprites carry random (often light) colours. Bilinear filtering
		// blends them into the edges of the art, which shows up as pale fringes and stray lines around borders.
		// This copies the colour of the nearest visible pixels into the transparent ones.
		private static void Bleed(Texture2D texture)
		{
			try
			{
				int w = texture.width;
				int h = texture.height;
				Color32[] px = texture.GetPixels32();
				bool[] solid = new bool[px.Length];
				for (int i = 0; i < px.Length; i++)
				{
					solid[i] = px[i].a > 0;
				}
				for (int pass = 0; pass < 6; pass++)
				{
					bool changed = false;
					Color32[] next = (Color32[])px.Clone();
					bool[] nextSolid = (bool[])solid.Clone();
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							int idx = y * w + x;
							if (solid[idx])
							{
								continue;
							}
							int r = 0;
							int g = 0;
							int b = 0;
							int n = 0;
							for (int dy = -1; dy <= 1; dy++)
							{
								for (int dx = -1; dx <= 1; dx++)
								{
									int nx = x + dx;
									int ny = y + dy;
									if (nx < 0 || ny < 0 || nx >= w || ny >= h)
									{
										continue;
									}
									int j = ny * w + nx;
									if (solid[j])
									{
										r += px[j].r;
										g += px[j].g;
										b += px[j].b;
										n++;
									}
								}
							}
							if (n > 0)
							{
								next[idx] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 0);
								nextSolid[idx] = true;
								changed = true;
							}
						}
					}
					px = next;
					solid = nextSolid;
					if (!changed)
					{
						break;
					}
				}
				texture.SetPixels32(px);
				texture.Apply();
			}
			catch (Exception ex)
			{
				Main.Mod.Logger.LogException(ex);
			}
		}

		// Stretches a texture over a rectangle, tinted with a colour.
		public static void Draw(Rect rect, Texture2D texture, Color color)
		{
			if (texture == null)
			{
				return;
			}
			Color previous = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
			GUI.color = previous;
		}

		// Nine-slice drawing. Border sizes are given in texture pixels and multiplied by "scale",
		// so a big sprite can be shrunk without squashing its corners. (GUIStyle.border cannot do that.)
		public static void Slice(Rect rect, Texture2D texture, float left, float right, float top, float bottom, float scale, Color color)
		{
			if (texture == null)
			{
				return;
			}
			float texW = (float)texture.width;
			float texH = (float)texture.height;
			float l = left * scale;
			float r = right * scale;
			float t = top * scale;
			float b = bottom * scale;
			if (l + r > rect.width)
			{
				float k = rect.width / (l + r);
				l *= k;
				r *= k;
			}
			if (t + b > rect.height)
			{
				float k = rect.height / (t + b);
				t *= k;
				b *= k;
			}
			float[] xs = new float[] { rect.x, rect.x + l, rect.xMax - r, rect.xMax };
			float[] ys = new float[] { rect.y, rect.y + t, rect.yMax - b, rect.yMax };
			float[] us = new float[] { 0f, left / texW, 1f - right / texW, 1f };
			// texture v runs bottom -> top, screen y runs top -> bottom
			float[] vs = new float[] { 1f, 1f - top / texH, bottom / texH, 0f };
			Color previous = GUI.color;
			GUI.color = color;
			for (int row = 0; row < 3; row++)
			{
				for (int col = 0; col < 3; col++)
				{
					float w = xs[col + 1] - xs[col];
					float h = ys[row + 1] - ys[row];
					if (w <= 0f || h <= 0f)
					{
						continue;
					}
					Rect dest = new Rect(xs[col], ys[row], w, h);
					Rect uv = new Rect(us[col], vs[row + 1], us[col + 1] - us[col], vs[row] - vs[row + 1]);
					GUI.DrawTextureWithTexCoords(dest, texture, uv, true);
				}
			}
			GUI.color = previous;
		}

		// A wax-seal "stamp" that scales around the centre of "rect" and fades in.
		public static void Stamp(Rect rect, Texture2D texture, float scale, float alpha)
		{
			if (texture == null)
			{
				return;
			}
			Rect scaled = new Rect(rect.center.x - rect.width * scale / 2f, rect.center.y - rect.height * scale / 2f, rect.width * scale, rect.height * scale);
			GameUI.Draw(scaled, texture, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
		}

		private static bool s_Loaded;
	}
}
