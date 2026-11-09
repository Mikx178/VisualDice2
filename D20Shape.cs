using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogDice
{
	// Token: 0x0200000F RID: 15
	public class D20Shape
	{
		// Token: 0x06000054 RID: 84 RVA: 0x00005824 File Offset: 0x00003A24
		public D20Shape()
		{
			float num = (1f + Mathf.Sqrt(5f)) / 2f;
			List<Vector3> list = new List<Vector3>();
			foreach (float num2 in new float[] { -1f, 1f })
			{
				foreach (float num3 in new float[]
				{
					-num,
					num
				})
				{
					list.Add(new Vector3(0f, num2, num3));
					list.Add(new Vector3(num2, num3, 0f));
					list.Add(new Vector3(num3, 0f, num2));
				}
			}
			this.Vertices = new Vector3[list.Count];
			for (int k = 0; k < list.Count; k++)
			{
				this.Vertices[k] = list[k].normalized;
			}
			float num4 = float.MaxValue;
			for (int l = 1; l < this.Vertices.Length; l++)
			{
				num4 = Mathf.Min(num4, Vector3.Distance(this.Vertices[0], this.Vertices[l]));
			}
			List<int[]> list2 = new List<int[]>();
			for (int m = 0; m < 12; m++)
			{
				for (int n = m + 1; n < 12; n++)
				{
					for (int num5 = n + 1; num5 < 12; num5++)
					{
						if (this.IsEdge(m, n, num4) && this.IsEdge(n, num5, num4) && this.IsEdge(m, num5, num4))
						{
							Vector3 vector = this.Vertices[m];
							Vector3 vector2 = this.Vertices[n];
							Vector3 vector3 = this.Vertices[num5];
							if (Vector3.Dot(Vector3.Cross(vector2 - vector, vector3 - vector), vector + vector2 + vector3) < 0f)
							{
								list2.Add(new int[] { m, num5, n });
							}
							else
							{
								list2.Add(new int[] { m, n, num5 });
							}
						}
					}
				}
			}
			this.Faces = list2.ToArray();
			this.Normals = new Vector3[this.Faces.Length];
			this.Centers = new Vector3[this.Faces.Length];
			for (int num6 = 0; num6 < this.Faces.Length; num6++)
			{
				Vector3 vector4 = this.Vertices[this.Faces[num6][0]];
				Vector3 vector5 = this.Vertices[this.Faces[num6][1]];
				Vector3 vector6 = this.Vertices[this.Faces[num6][2]];
				this.Centers[num6] = (vector4 + vector5 + vector6) / 3f;
				this.Normals[num6] = Vector3.Cross(vector5 - vector4, vector6 - vector4).normalized;
			}
			this.Opposite = new int[this.Faces.Length];
			for (int num7 = 0; num7 < this.Faces.Length; num7++)
			{
				int num8 = 0;
				for (int num9 = 0; num9 < this.Faces.Length; num9++)
				{
					if (Vector3.Dot(this.Normals[num7], this.Normals[num9]) < Vector3.Dot(this.Normals[num7], this.Normals[num8]))
					{
						num8 = num9;
					}
				}
				this.Opposite[num7] = num8;
			}
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00005C79 File Offset: 0x00003E79
		private bool IsEdge(int i, int j, float edge)
		{
			return Mathf.Abs(Vector3.Distance(this.Vertices[i], this.Vertices[j]) - edge) < 0.01f;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00005CB0 File Offset: 0x00003EB0
		public int[] DefaultLabels()
		{
			int[] array = new int[this.Faces.Length];
			int num = 1;
			for (int i = 0; i < this.Faces.Length; i++)
			{
				if (array[i] == 0)
				{
					array[i] = num;
					array[this.Opposite[i]] = 21 - num;
					num++;
				}
			}
			return array;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00005CFC File Offset: 0x00003EFC
		public int TopFace(Quaternion rotation)
		{
			int num = 0;
			float num2 = float.MinValue;
			for (int i = 0; i < this.Faces.Length; i++)
			{
				float y = (rotation * this.Normals[i]).y;
				if (y > num2)
				{
					num2 = y;
					num = i;
				}
			}
			return num;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00005D4C File Offset: 0x00003F4C
		public void ForceTop(int[] labels, int topFace, int value)
		{
			int num = Array.IndexOf<int>(labels, value);
			if (num < 0 || num == topFace)
			{
				return;
			}
			int num2 = this.Opposite[topFace];
			int num3 = this.Opposite[num];
			int num4 = labels[topFace];
			labels[topFace] = labels[num];
			labels[num] = num4;
			if (num != num2)
			{
				num4 = labels[num2];
				labels[num2] = labels[num3];
				labels[num3] = num4;
			}
		}

		// Token: 0x0400006E RID: 110
		public readonly Vector3[] Vertices;

		// Token: 0x0400006F RID: 111
		public readonly int[][] Faces;

		// Token: 0x04000070 RID: 112
		public readonly Vector3[] Normals;

		// Token: 0x04000071 RID: 113
		public readonly Vector3[] Centers;

		// Token: 0x04000072 RID: 114
		public readonly int[] Opposite;
	}
}
