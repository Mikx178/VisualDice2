using System;
using UnityEngine;

namespace DialogDice
{
	// Procedurally painted panel in the style of the Baldur's Gate 3 roll window:
	// a worn bronze frame holding dark cartouches (header, dice arena, bonus strip).
	// Coordinates are in the 600 x 960 layout space of the window.
	public static class Bg3Art
	{
		public const float PanelWidth = 600f;
		public const float PanelHeight = 960f;

		public static readonly Rect Header = new Rect(40f, 40f, 520f, 160f);
		public static readonly Rect Arena = new Rect(40f, 214f, 520f, 476f);
		public static readonly Rect Info = new Rect(40f, 704f, 520f, 152f);

		public static Texture2D Panel(int w, int h)
		{
			float k = PanelWidth / (float)w;
			Color32[] px = new Color32[w * h];
			for (int ty = 0; ty < h; ty++)
			{
				for (int tx = 0; tx < w; tx++)
				{
					float x = ((float)tx + 0.5f) * k;
					float y = ((float)ty + 0.5f) * k;
					Color c = Bg3Art.Shade(x, y, k);
					px[(h - 1 - ty) * w + tx] = c;
				}
			}
			Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
			texture.SetPixels32(px);
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.filterMode = FilterMode.Bilinear;
			texture.Apply();
			texture.hideFlags = HideFlags.HideAndDontSave;
			return texture;
		}

		private static Color Shade(float x, float y, float k)
		{
			float outer = Bg3Art.RoundRectDist(x, y, 0f, 0f, PanelWidth, PanelHeight, 30f);
			float alpha = Mathf.Clamp01(0.5f - outer / k);
			if (alpha <= 0f)
			{
				return new Color(0f, 0f, 0f, 0f);
			}
			float n = Bg3Art.Noise(x * 0.3f, y * 0.3f) * 0.55f + Bg3Art.Noise(x * 1.4f, y * 1.4f) * 0.45f;
			Color bronze = Color.Lerp(new Color(0.20f, 0.12f, 0.05f), new Color(0.44f, 0.28f, 0.10f), n);
			float vignette = 0.82f + 0.18f * Mathf.Clamp01(1f - Mathf.Abs(y - PanelHeight * 0.5f) / (PanelHeight * 0.55f));
			bronze = new Color(bronze.r * vignette, bronze.g * vignette, bronze.b * vignette, 1f);
			Color color = bronze;
			float inside = -outer;
			if (inside < 3f)
			{
				color = new Color(0.09f, 0.05f, 0.02f, 1f);
			}
			else if (inside < 5.5f)
			{
				color = new Color(0.82f, 0.60f, 0.24f, 1f);
			}
			else if (inside > 15f && inside < 17f)
			{
				color = Color.Lerp(bronze, new Color(0.16f, 0.09f, 0.04f, 1f), 0.7f);
			}
			float best = 9999f;
			float bestTop = 0f;
			float bestHeight = 1f;
			Rect[] rects = new Rect[] { Bg3Art.Header, Bg3Art.Arena, Bg3Art.Info };
			float[] radii = new float[] { 40f, 30f, 26f };
			for (int i = 0; i < rects.Length; i++)
			{
				float d = Bg3Art.RoundRectDist(x, y, rects[i].x, rects[i].y, rects[i].width, rects[i].height, radii[i]);
				if (d < best)
				{
					best = d;
					bestTop = rects[i].y;
					bestHeight = rects[i].height;
				}
			}
			if (best < -3.5f)
			{
				float t = Mathf.Clamp01((y - bestTop) / bestHeight);
				Color fill = Color.Lerp(new Color(0.14f, 0.11f, 0.13f), new Color(0.06f, 0.05f, 0.065f), t);
				float edge = 0.72f + 0.28f * Mathf.Clamp01(-best / 36f);
				float grain = 0.9f + 0.2f * Bg3Art.Noise(x * 0.9f, y * 0.9f);
				color = new Color(fill.r * edge * grain, fill.g * edge * grain, fill.b * edge * grain, 1f);
			}
			else if (best < -1.5f)
			{
				color = new Color(0.78f, 0.55f, 0.20f, 1f);
			}
			else if (best < 2f)
			{
				color = new Color(0.07f, 0.04f, 0.02f, 1f);
			}
			color.a = alpha;
			return color;
		}

		// Signed distance to a rounded rectangle (negative inside).
		private static float RoundRectDist(float px, float py, float rx, float ry, float rw, float rh, float r)
		{
			float cx = rx + rw * 0.5f;
			float cy = ry + rh * 0.5f;
			float qx = Mathf.Abs(px - cx) - (rw * 0.5f - r);
			float qy = Mathf.Abs(py - cy) - (rh * 0.5f - r);
			float ox = Mathf.Max(qx, 0f);
			float oy = Mathf.Max(qy, 0f);
			return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
		}

		private static float Hash(int x, int y)
		{
			unchecked
			{
				int h = x * 374761393 + y * 668265263;
				h = (h ^ (h >> 13)) * 1274126177;
				h = h ^ (h >> 16);
				return (float)(h & 65535) / 65535f;
			}
		}

		private static float Noise(float x, float y)
		{
			int xi = Mathf.FloorToInt(x);
			int yi = Mathf.FloorToInt(y);
			float fx = x - (float)xi;
			float fy = y - (float)yi;
			fx = fx * fx * (3f - 2f * fx);
			fy = fy * fy * (3f - 2f * fy);
			float a = Mathf.Lerp(Bg3Art.Hash(xi, yi), Bg3Art.Hash(xi + 1, yi), fx);
			float b = Mathf.Lerp(Bg3Art.Hash(xi, yi + 1), Bg3Art.Hash(xi + 1, yi + 1), fx);
			return Mathf.Lerp(a, b, fy);
		}
	}
}
