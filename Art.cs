using System;
using UnityEngine;

namespace DialogDice
{
	// Small procedural helpers (flat colours, glow) and a sprite blitter.
	// The "real" UI art comes from the game's own sprites, see GameUI.
	public static class Art
	{
		public static Texture2D Solid(Color color)
		{
			Texture2D texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, color);
			texture.Apply();
			texture.hideFlags = HideFlags.HideAndDontSave;
			return texture;
		}

		public static Texture2D VerticalGradient(Color top, Color bottom)
		{
			Texture2D texture = new Texture2D(1, 128, TextureFormat.RGBA32, false);
			for (int i = 0; i < 128; i++)
			{
				texture.SetPixel(0, i, Color.Lerp(bottom, top, (float)i / 127f));
			}
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			texture.hideFlags = HideFlags.HideAndDontSave;
			return texture;
		}

		public static Texture2D Glow(int size)
		{
			return Art.Radial(size, delegate(float r)
			{
				float a = Mathf.Clamp01(1f - r);
				return new Color(1f, 1f, 1f, a * a);
			});
		}

		private static Texture2D Radial(int size, Art.RadialFn fn)
		{
			Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
			Color[] pixels = new Color[size * size];
			float half = (float)size / 2f;
			for (int i = 0; i < size; i++)
			{
				for (int j = 0; j < size; j++)
				{
					float dx = ((float)j + 0.5f - half) / half;
					float dy = ((float)i + 0.5f - half) / half;
					pixels[i * size + j] = fn(Mathf.Sqrt(dx * dx + dy * dy));
				}
			}
			texture.SetPixels(pixels);
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.Apply();
			texture.hideFlags = HideFlags.HideAndDontSave;
			return texture;
		}

		public static void DrawSprite(Rect rect, Sprite sprite)
		{
			Texture2D texture = sprite.texture;
			if (texture == null)
			{
				return;
			}
			Rect textureRect = sprite.textureRect;
			Rect uv = new Rect(textureRect.x / (float)texture.width, textureRect.y / (float)texture.height, textureRect.width / (float)texture.width, textureRect.height / (float)texture.height);
			GUI.DrawTextureWithTexCoords(rect, texture, uv);
		}

		private delegate Color RadialFn(float r);
	}
}
