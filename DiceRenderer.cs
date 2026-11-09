using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogDice
{
	// Token: 0x0200000E RID: 14
	public class DiceRenderer
	{
		// Token: 0x06000040 RID: 64 RVA: 0x00004120 File Offset: 0x00002320
		public DiceRenderer(D20Shape shape, Font font)
		{
			this.m_Shape = shape;
			this.m_Font = font;
			Shader shader;
			if ((shader = Shader.Find("Hidden/Internal-Colored")) == null)
			{
				shader = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
			}
			Shader shader2 = shader;
			if (shader2 == null)
			{
				throw new InvalidOperationException("No usable shader for dice rendering");
			}
			this.m_Colored = new Material(shader2);
			this.m_Colored.hideFlags = HideFlags.HideAndDontSave;
			this.m_Colored.SetInt("_SrcBlend", 5);
			this.m_Colored.SetInt("_DstBlend", 10);
			this.m_Colored.SetInt("_Cull", 0);
			this.m_Colored.SetInt("_ZWrite", 0);
			this.m_Colored.SetInt("_ZTest", 8);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x000041EC File Offset: 0x000023EC
		private void SetCamera(Vector3 position, Vector3 target, Vector3 light)
		{
			this.CameraPos = position;
			this.LightDir = light;
			this.m_Forward = (target - position).normalized;
			this.m_Right = Vector3.Cross(Vector3.up, this.m_Forward).normalized;
			this.m_Up = Vector3.Cross(this.m_Forward, this.m_Right);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00004254 File Offset: 0x00002454
		public void DrawSingle(Rect rect, float scale, DiceFrame die, int[] labels, Color glow)
		{
			this.m_PixelScale = scale;
			this.SetCamera(DiceRenderer.SingleCameraPos, Vector3.zero, DiceRenderer.SingleLight);
			float num = -DiceRenderer.SingleCameraPos.z;
			float num2 = Mathf.Min(rect.width, rect.height) * 0.36f * Mathf.Clamp(Main.Settings.DieSize, 0.5f, 1.5f) * num;
			this.m_Focal = num2 * scale;
			this.m_Center = new Vector2(rect.center.x * scale, rect.center.y * scale);
			GL.PushMatrix();
			GL.LoadPixelMatrix(0f, (float)Screen.width, (float)Screen.height, 0f);
			this.m_Colored.SetPass(0);
			GL.Begin(GL.TRIANGLES);
			this.DrawDieFaces(die, glow);
			GL.End();
			if (this.m_Font != null)
			{
				this.DrawDigits(die, labels);
			}
			GL.PopMatrix();
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00004334 File Offset: 0x00002534
		public void Draw(Rect rect, float scale, DiceFrame die, int[] labels, Color glow)
		{
			this.m_PixelScale = scale;
			this.SetCamera(DiceRenderer.TrayCameraPos, DiceRenderer.TrayCameraTarget, DiceRenderer.TrayLight);
			this.FitTray(rect);
			GL.PushMatrix();
			GL.LoadPixelMatrix(0f, (float)Screen.width, (float)Screen.height, 0f);
			this.m_Colored.SetPass(0);
			GL.Begin(GL.TRIANGLES);
			this.DrawTrayBack();
			this.DrawShadow(die.Position);
			this.DrawDieFaces(die, glow);
			this.DrawTrayFront();
			GL.End();
			if (this.m_Font != null)
			{
				this.DrawDigits(die, labels);
			}
			GL.PopMatrix();
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000043DC File Offset: 0x000025DC
		private void FitTray(Rect rect)
		{
			float num = 7.7f;
			float num2 = 4.8999996f;
			Vector3[] array = new Vector3[]
			{
				new Vector3(-num, 0f, -num2),
				new Vector3(num, 0f, -num2),
				new Vector3(-num, 0f, num2),
				new Vector3(num, 0f, num2),
				new Vector3(-num, 1.4f, -num2),
				new Vector3(num, 1.4f, -num2),
				new Vector3(-num, 1.4f, num2),
				new Vector3(num, 1.4f, num2)
			};
			float num3 = float.MaxValue;
			float num4 = float.MinValue;
			float num5 = float.MaxValue;
			float num6 = float.MinValue;
			foreach (Vector3 vector in array)
			{
				Vector3 vector2 = vector - this.CameraPos;
				float num7 = Vector3.Dot(vector2, this.m_Forward);
				float num8 = Vector3.Dot(vector2, this.m_Right) / num7;
				float num9 = -Vector3.Dot(vector2, this.m_Up) / num7;
				num3 = Mathf.Min(num3, num8);
				num4 = Mathf.Max(num4, num8);
				num5 = Mathf.Min(num5, num9);
				num6 = Mathf.Max(num6, num9);
			}
			float num10 = Mathf.Min(rect.width / (num4 - num3), rect.height / (num6 - num5)) * 0.96f;
			this.m_Focal = num10 * this.m_PixelScale;
			float num11 = (num3 + num4) / 2f * num10;
			float num12 = (num5 + num6) / 2f * num10;
			this.m_Center = new Vector2((rect.center.x - num11) * this.m_PixelScale, (rect.center.y - num12) * this.m_PixelScale);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00004610 File Offset: 0x00002810
		private Vector2 Project(Vector3 p)
		{
			Vector3 vector = p - this.CameraPos;
			float num = Vector3.Dot(vector, this.m_Forward);
			return new Vector2(this.m_Center.x + Vector3.Dot(vector, this.m_Right) / num * this.m_Focal, this.m_Center.y - Vector3.Dot(vector, this.m_Up) / num * this.m_Focal);
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00004680 File Offset: 0x00002880
		private Color Shade(Color baseColor, Vector3 normal, float ambient)
		{
			float num = Mathf.Max(0f, Vector3.Dot(normal, this.LightDir));
			float num2 = ambient + (1f - ambient) * num;
			return new Color(baseColor.r * num2, baseColor.g * num2, baseColor.b * num2, baseColor.a);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000046D8 File Offset: 0x000028D8
		private void Tri(Vector3 a, Vector3 b, Vector3 c, Color color)
		{
			GL.Color(color);
			Vector2 vector = this.Project(a);
			Vector2 vector2 = this.Project(b);
			Vector2 vector3 = this.Project(c);
			GL.Vertex3(vector.x, vector.y, 0f);
			GL.Vertex3(vector2.x, vector2.y, 0f);
			GL.Vertex3(vector3.x, vector3.y, 0f);
		}

		// Token: 0x06000048 RID: 72 RVA: 0x0000474C File Offset: 0x0000294C
		private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
		{
			this.Tri(a, b, c, color);
			this.Tri(a, c, d, color);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00004768 File Offset: 0x00002968
		private void Box(Vector3 min, Vector3 max, Color color)
		{
			Vector3[][] array = new Vector3[][]
			{
				new Vector3[]
				{
					new Vector3(min.x, max.y, min.z),
					new Vector3(max.x, max.y, min.z),
					new Vector3(max.x, max.y, max.z),
					new Vector3(min.x, max.y, max.z)
				},
				new Vector3[]
				{
					new Vector3(min.x, min.y, min.z),
					new Vector3(max.x, min.y, min.z),
					new Vector3(max.x, max.y, min.z),
					new Vector3(min.x, max.y, min.z)
				},
				new Vector3[]
				{
					new Vector3(min.x, min.y, max.z),
					new Vector3(max.x, min.y, max.z),
					new Vector3(max.x, max.y, max.z),
					new Vector3(min.x, max.y, max.z)
				},
				new Vector3[]
				{
					new Vector3(min.x, min.y, min.z),
					new Vector3(min.x, min.y, max.z),
					new Vector3(min.x, max.y, max.z),
					new Vector3(min.x, max.y, min.z)
				},
				new Vector3[]
				{
					new Vector3(max.x, min.y, min.z),
					new Vector3(max.x, min.y, max.z),
					new Vector3(max.x, max.y, max.z),
					new Vector3(max.x, max.y, min.z)
				}
			};
			Vector3[] array2 = new Vector3[]
			{
				Vector3.up,
				Vector3.back,
				Vector3.forward,
				Vector3.left,
				Vector3.right
			};
			for (int i = 0; i < array.Length; i++)
			{
				Vector3 vector = (array[i][0] + array[i][2]) / 2f;
				if (Vector3.Dot(array2[i], this.CameraPos - vector) > 0f)
				{
					this.Quad(array[i][0], array[i][1], array[i][2], array[i][3], this.Shade(color, array2[i], 0.45f));
				}
			}
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00004BF4 File Offset: 0x00002DF4
		private void DrawTrayBack()
		{
			float num = 7f;
			float num2 = 4.2f;
			float num3 = 1.4f;
			float num4 = 0.7f;
			this.Quad(new Vector3(-num, 0f, -num2), new Vector3(num, 0f, -num2), new Vector3(num, 0f, num2), new Vector3(-num, 0f, num2), this.Shade(Main.Settings.GetFeltColor(), Vector3.up, 0.6f));
			Color color = new Color(0f, 0f, 0f, 0.28f);
			Color color2 = new Color(0f, 0f, 0f, 0f);
			this.Gradient(new Vector3(-num, 0f, num2), new Vector3(num, 0f, num2), new Vector3(num, 0f, num2 - 1.4f), new Vector3(-num, 0f, num2 - 1.4f), color, color2);
			this.Gradient(new Vector3(-num, 0f, -num2), new Vector3(-num, 0f, num2), new Vector3(-num + 1.2f, 0f, num2), new Vector3(-num + 1.2f, 0f, -num2), color, color2);
			this.Gradient(new Vector3(num, 0f, num2), new Vector3(num, 0f, -num2), new Vector3(num - 1.2f, 0f, -num2), new Vector3(num - 1.2f, 0f, num2), color, color2);
			this.Box(new Vector3(-num - num4, 0f, num2), new Vector3(num + num4, num3, num2 + num4), DiceRenderer.WoodColor);
			this.Box(new Vector3(-num - num4, 0f, -num2), new Vector3(-num, num3, num2), DiceRenderer.WoodColor);
			this.Box(new Vector3(num, 0f, -num2), new Vector3(num + num4, num3, num2), DiceRenderer.WoodColor);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00004DE8 File Offset: 0x00002FE8
		private void DrawTrayFront()
		{
			float num = 7f;
			float num2 = 4.2f;
			this.Box(new Vector3(-num - 0.7f, 0f, -num2 - 0.7f), new Vector3(num + 0.7f, 1.4f, -num2), DiceRenderer.WoodColor);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00004E3C File Offset: 0x0000303C
		private void Gradient(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color from, Color to)
		{
			Vector2 vector = this.Project(a);
			Vector2 vector2 = this.Project(b);
			Vector2 vector3 = this.Project(c);
			Vector2 vector4 = this.Project(d);
			GL.Color(from);
			GL.Vertex3(vector.x, vector.y, 0f);
			GL.Color(from);
			GL.Vertex3(vector2.x, vector2.y, 0f);
			GL.Color(to);
			GL.Vertex3(vector3.x, vector3.y, 0f);
			GL.Color(from);
			GL.Vertex3(vector.x, vector.y, 0f);
			GL.Color(to);
			GL.Vertex3(vector3.x, vector3.y, 0f);
			GL.Color(to);
			GL.Vertex3(vector4.x, vector4.y, 0f);
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00004F24 File Offset: 0x00003124
		private void DrawShadow(Vector3 diePos)
		{
			float num = Mathf.Max(0f, diePos.y - 0.8f);
			float num2 = Mathf.Lerp(0.55f, 0.15f, Mathf.Clamp01(num / 3f));
			float num3 = 1.05f + num * 0.25f;
			Vector3 vector = new Vector3(diePos.x + num * 0.3f, 0.01f, diePos.z + num * 0.35f);
			Color color = new Color(0f, 0f, 0f, num2);
			Color color2 = new Color(0f, 0f, 0f, 0f);
			Vector2 vector2 = this.Project(vector);
			for (int i = 0; i < 24; i++)
			{
				float num4 = (float)i * 3.1415927f * 2f / 24f;
				float num5 = (float)(i + 1) * 3.1415927f * 2f / 24f;
				Vector2 vector3 = this.Project(vector + new Vector3(Mathf.Cos(num4), 0f, Mathf.Sin(num4)) * num3);
				Vector2 vector4 = this.Project(vector + new Vector3(Mathf.Cos(num5), 0f, Mathf.Sin(num5)) * num3);
				GL.Color(color);
				GL.Vertex3(vector2.x, vector2.y, 0f);
				GL.Color(color2);
				GL.Vertex3(vector3.x, vector3.y, 0f);
				GL.Color(color2);
				GL.Vertex3(vector4.x, vector4.y, 0f);
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x000050D4 File Offset: 0x000032D4
		private bool FaceVisible(DiceFrame die, int face, out Vector3 normal, out Vector3 center)
		{
			normal = die.Rotation * this.m_Shape.Normals[face];
			center = die.Position + die.Rotation * this.m_Shape.Centers[face];
			return Vector3.Dot(normal, this.CameraPos - center) > 0f;
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00005164 File Offset: 0x00003364
		private void DrawDieFaces(DiceFrame die, Color glow)
		{
			Color color = Color.Lerp(Main.Settings.GetDieColor(), glow, glow.a);
			color.a = 1f;
			for (int i = 0; i < this.m_Shape.Faces.Length; i++)
			{
				Vector3 vector;
				Vector3 vector2;
				if (this.FaceVisible(die, i, out vector, out vector2))
				{
					Color color2 = this.Shade(color, vector, 0.35f);
					float num = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(Vector3.Reflect(-this.LightDir, vector), (this.CameraPos - vector2).normalized)), 12f);
					color2 = Color.Lerp(color2, Color.white, num * 0.35f);
					int[] array = this.m_Shape.Faces[i];
					Vector3 vector3 = die.Position + die.Rotation * this.m_Shape.Vertices[array[0]];
					Vector3 vector4 = die.Position + die.Rotation * this.m_Shape.Vertices[array[1]];
					Vector3 vector5 = die.Position + die.Rotation * this.m_Shape.Vertices[array[2]];
					this.Tri(vector3, vector4, vector5, new Color(color2.r * 0.45f, color2.g * 0.45f, color2.b * 0.45f, 1f));
					this.Tri(Vector3.Lerp(vector3, vector2, 0.09f), Vector3.Lerp(vector4, vector2, 0.09f), Vector3.Lerp(vector5, vector2, 0.09f), color2);
				}
			}
		}

		// Token: 0x06000050 RID: 80 RVA: 0x0000533C File Offset: 0x0000353C
		// The rolled number lights up once the die has landed (0 = no highlight).
		public int HighlightValue;

		public Color HighlightColor = Color.white;

		public float HighlightPulse;

		private void DrawDigits(DiceFrame die, int[] labels)
		{
			this.m_Font.RequestCharactersInTexture("0123456789.", 64, FontStyle.Bold);
			this.m_Font.material.SetPass(0);
			GL.Begin(GL.QUADS);
			for (int i = 0; i < this.m_Shape.Faces.Length; i++)
			{
				Vector3 vector;
				Vector3 vector2;
				if (this.FaceVisible(die, i, out vector, out vector2))
				{
					Vector3 vector3 = die.Position + die.Rotation * this.m_Shape.Vertices[this.m_Shape.Faces[i][0]];
					Vector3 normalized = (vector3 - vector2).normalized;
					Vector3 normalized2 = Vector3.Cross(normalized, -vector).normalized;
					Vector3 vector4 = vector2 + vector * 0.002f;
					float num = Mathf.Clamp01(Vector3.Dot(vector, (this.CameraPos - vector2).normalized) * 1.6f);
					Color color = this.Shade(Main.Settings.GetDigitColor(), vector, 0.55f);
					color.a = num;
					string text = ((labels[i] == 6 || labels[i] == 9) ? (labels[i] + ".") : labels[i].ToString());
					if (this.HighlightValue > 0 && labels[i] == this.HighlightValue)
					{
						// soft bloom: the number drawn a few times around itself, then a bright copy on top
						Color bloom = new Color(this.HighlightColor.r, this.HighlightColor.g, this.HighlightColor.b, num * (0.30f + 0.25f * this.HighlightPulse));
						float spread = 0.03f + 0.012f * this.HighlightPulse;
						Vector3 bumped = vector4 + vector * 0.002f;
						this.DrawText(text, bumped + normalized2 * spread, normalized2, normalized, bloom);
						this.DrawText(text, bumped - normalized2 * spread, normalized2, normalized, bloom);
						this.DrawText(text, bumped + normalized * spread, normalized2, normalized, bloom);
						this.DrawText(text, bumped - normalized * spread, normalized2, normalized, bloom);
						Color bright = Color.Lerp(this.HighlightColor, Color.white, 0.35f + 0.4f * this.HighlightPulse);
						bright.a = num;
						this.DrawText(text, bumped + vector * 0.002f, normalized2, normalized, bright);
					}
					else
					{
						if (this.HighlightValue > 0)
						{
							color.a = num * 0.45f;
						}
						this.DrawText(text, vector4, normalized2, normalized, color);
					}
				}
			}
			GL.End();
		}

		// Token: 0x06000051 RID: 81 RVA: 0x000054A8 File Offset: 0x000036A8
		private void DrawText(string text, Vector3 origin, Vector3 right, Vector3 up, Color color)
		{
			float num = 0f;
			int num2 = int.MinValue;
			int num3 = int.MaxValue;
			List<CharacterInfo> list = new List<CharacterInfo>();
			foreach (char c in text)
			{
				CharacterInfo characterInfo;
				if (this.m_Font.GetCharacterInfo(c, out characterInfo, 64, FontStyle.Bold))
				{
					list.Add(characterInfo);
					num += (float)characterInfo.advance;
					if (c != '.')
					{
						num2 = Mathf.Max(num2, characterInfo.maxY);
						num3 = Mathf.Min(num3, characterInfo.minY);
					}
				}
			}
			if (list.Count == 0 || num2 == int.MinValue)
			{
				return;
			}
			float num4 = 0.36f / (float)(num2 - num3);
			float num5 = -num / 2f;
			float num6 = (float)(num2 + num3) / 2f;
			GL.Color(color);
			foreach (CharacterInfo characterInfo2 in list)
			{
				Vector3 vector = origin + right * ((num5 + (float)characterInfo2.minX) * num4) + up * (((float)characterInfo2.minY - num6) * num4);
				Vector3 vector2 = origin + right * ((num5 + (float)characterInfo2.maxX) * num4) + up * (((float)characterInfo2.minY - num6) * num4);
				Vector3 vector3 = origin + right * ((num5 + (float)characterInfo2.maxX) * num4) + up * (((float)characterInfo2.maxY - num6) * num4);
				Vector3 vector4 = origin + right * ((num5 + (float)characterInfo2.minX) * num4) + up * (((float)characterInfo2.maxY - num6) * num4);
				this.Vertex(vector, characterInfo2.uvBottomLeft);
				this.Vertex(vector2, characterInfo2.uvBottomRight);
				this.Vertex(vector3, characterInfo2.uvTopRight);
				this.Vertex(vector4, characterInfo2.uvTopLeft);
				num5 += (float)characterInfo2.advance;
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x000056E4 File Offset: 0x000038E4
		private void Vertex(Vector3 p, Vector2 uv)
		{
			Vector2 vector = this.Project(p);
			GL.TexCoord2(uv.x, uv.y);
			GL.Vertex3(vector.x, vector.y, 0f);
		}

		// Token: 0x04000056 RID: 86
		private const float SingleDieFill = 0.36f;

		// Token: 0x04000057 RID: 87
		private const float WallThickness = 0.7f;

		// Token: 0x04000058 RID: 88
		private const float DigitHeight = 0.36f;

		// Token: 0x04000059 RID: 89
		private const int FontSize = 64;

		// Token: 0x0400005A RID: 90
		private static readonly Vector3 TrayCameraPos = new Vector3(0f, 15f, -10.5f);

		// Token: 0x0400005B RID: 91
		private static readonly Vector3 TrayCameraTarget = new Vector3(0f, 0f, 0.4f);

		// Token: 0x0400005C RID: 92
		private static readonly Vector3 TrayLight = new Vector3(-0.35f, 1f, -0.45f).normalized;

		// Token: 0x0400005D RID: 93
		private static readonly Vector3 SingleCameraPos = new Vector3(0f, 0f, -9f);

		// Token: 0x0400005E RID: 94
		private static readonly Vector3 SingleLight = new Vector3(-0.45f, 0.65f, -0.6f).normalized;

		// Token: 0x0400005F RID: 95
		private static readonly Color DieColor = new Color(0.62f, 0.07f, 0.09f);

		// Token: 0x04000060 RID: 96
		private static readonly Color DigitColor = new Color(1f, 0.86f, 0.5f);

		// Token: 0x04000061 RID: 97
		private static readonly Color FeltColor = new Color(0.09f, 0.22f, 0.16f);

		// Token: 0x04000062 RID: 98
		private static readonly Color WoodColor = new Color(0.42f, 0.25f, 0.13f);

		// Token: 0x04000063 RID: 99
		private readonly D20Shape m_Shape;

		// Token: 0x04000064 RID: 100
		private readonly Material m_Colored;

		// Token: 0x04000065 RID: 101
		private readonly Font m_Font;

		// Token: 0x04000066 RID: 102
		private Vector3 CameraPos;

		// Token: 0x04000067 RID: 103
		private Vector3 LightDir;

		// Token: 0x04000068 RID: 104
		private Vector3 m_Right;

		// Token: 0x04000069 RID: 105
		private Vector3 m_Up;

		// Token: 0x0400006A RID: 106
		private Vector3 m_Forward;

		// Token: 0x0400006B RID: 107
		private float m_Focal;

		// Token: 0x0400006C RID: 108
		private Vector2 m_Center;

		// Token: 0x0400006D RID: 109
		private float m_PixelScale;
	}
}
