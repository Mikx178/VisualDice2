using System;
using UnityEngine;

namespace DialogDice
{
	// Token: 0x02000012 RID: 18
	public static class DiceSim
	{
		// Token: 0x0600005C RID: 92 RVA: 0x00005E94 File Offset: 0x00004094
		public static DiceRoll Roll(D20Shape shape, int value)
		{
			return DiceSim.Roll(shape, value, 1f, 1f);
		}

		public static DiceRoll Roll(D20Shape shape, int value, float speed, float length)
		{
			speed = Mathf.Clamp(speed, 0.5f, 2.2f);
			DiceRoll diceRoll = new DiceRoll();
			Vector3 vector = new Vector3(-5.7f, 3.2f, UnityEngine.Random.Range(-2.2f, 2.2f));
			Vector3 vector2 = new Vector3(UnityEngine.Random.Range(15f, 20f), UnityEngine.Random.Range(0f, 3f), UnityEngine.Random.Range(-4f, 4f));
			Quaternion quaternion = UnityEngine.Random.rotationUniform;
			Vector3 vector3 = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(14f, 24f);
			vector2 *= speed;
			vector3 *= speed;
			float num = 0.004166667f;
			float num2 = -1f;
			int num3 = 0;
			int num4 = Mathf.RoundToInt(270f);
			for (int i = 0; i < num4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					vector2.y += -42f * num;
					vector += vector2 * num;
					quaternion = DiceSim.Integrate(quaternion, vector3, num);
					bool flag = false;
					foreach (DiceSim.Plane plane in DiceSim.s_Planes)
					{
						float num5 = 0f;
						for (int l = 0; l < shape.Vertices.Length; l++)
						{
							Vector3 vector4 = quaternion * shape.Vertices[l];
							Vector3 vector5 = vector + vector4;
							float num6 = plane.Offset - Vector3.Dot(plane.Normal, vector5);
							if (num6 > 0f)
							{
								num5 = Mathf.Max(num5, num6);
								if (plane.Normal == Vector3.up)
								{
									flag = true;
								}
								Vector3 vector6 = vector2 + Vector3.Cross(vector3, vector4);
								float num7 = Vector3.Dot(vector6, plane.Normal);
								if (num7 < 0f)
								{
									Vector3 vector7 = Vector3.Cross(vector4, plane.Normal);
									float num8 = -(1f + plane.Restitution) * num7 / (1f + 2.6f * vector7.sqrMagnitude);
									vector2 += plane.Normal * (num8 * 1f);
									vector3 += Vector3.Cross(vector4, plane.Normal * num8) * 2.6f;
									Vector3 vector8 = vector6 - plane.Normal * num7;
									float magnitude = vector8.magnitude;
									if (magnitude > 0.0001f)
									{
										Vector3 vector9 = -vector8 / magnitude;
										Vector3 vector10 = Vector3.Cross(vector4, vector9);
										float num9 = Mathf.Min(0.45f * num8, magnitude / (1f + 2.6f * vector10.sqrMagnitude));
										vector2 += vector9 * (num9 * 1f);
										vector3 += Vector3.Cross(vector4, vector9 * num9) * 2.6f;
									}
									float num10 = (float)i / 60f;
									if (-num7 > 2.5f && num10 - num2 > 0.06f)
									{
										num2 = num10;
										diceRoll.ImpactTimes.Add(num10);
										diceRoll.ImpactStrengths.Add(Mathf.Clamp01(-num7 / 18f));
									}
								}
							}
						}
						vector += plane.Normal * num5;
					}
					if (flag)
					{
						vector3 *= 1f - 1.8f * num;
						vector2.x *= 1f - 0.9f * num;
						vector2.z *= 1f - 0.9f * num;
					}
				}
				diceRoll.Frames.Add(new DiceFrame
				{
					Position = vector,
					Rotation = quaternion
				});
				if (vector2.sqrMagnitude < 0.04f && vector3.sqrMagnitude < 0.09f && vector.y < 1.2f)
				{
					num3++;
				}
				else
				{
					num3 = 0;
				}
				if (num3 > 12)
				{
					break;
				}
			}
			DiceSim.Settle(shape, diceRoll);
			int num11 = shape.TopFace(diceRoll.Frames[diceRoll.Frames.Count - 1].Rotation);
			diceRoll.Labels = shape.DefaultLabels();
			shape.ForceTop(diceRoll.Labels, num11, value);
			diceRoll.ApplyStretch(length);
			return diceRoll;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000062FC File Offset: 0x000044FC
		private static void Settle(D20Shape shape, DiceRoll roll)
		{
			DiceFrame diceFrame = roll.Frames[roll.Frames.Count - 1];
			int num = shape.TopFace(diceFrame.Rotation);
			Vector3 vector = diceFrame.Rotation * shape.Normals[num];
			Quaternion quaternion = Quaternion.FromToRotation(vector, Vector3.up) * diceFrame.Rotation;
			float num2 = float.MaxValue;
			foreach (Vector3 vector2 in shape.Vertices)
			{
				num2 = Mathf.Min(num2, (quaternion * vector2).y);
			}
			Vector3 vector3 = new Vector3(diceFrame.Position.x, -num2, diceFrame.Position.z);
			for (int j = 1; j <= 12; j++)
			{
				float num3 = Mathf.SmoothStep(0f, 1f, (float)j / 12f);
				roll.Frames.Add(new DiceFrame
				{
					Position = Vector3.Lerp(diceFrame.Position, vector3, num3),
					Rotation = Quaternion.Slerp(diceFrame.Rotation, quaternion, num3)
				});
			}
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00006440 File Offset: 0x00004640
		private static Quaternion Integrate(Quaternion q, Vector3 w, float dt)
		{
			Quaternion quaternion = new Quaternion(w.x, w.y, w.z, 0f) * q;
			float num = 0.5f * dt;
			Quaternion quaternion2 = new Quaternion(q.x + quaternion.x * num, q.y + quaternion.y * num, q.z + quaternion.z * num, q.w + quaternion.w * num);
			float num2 = Mathf.Sqrt(quaternion2.x * quaternion2.x + quaternion2.y * quaternion2.y + quaternion2.z * quaternion2.z + quaternion2.w * quaternion2.w);
			return new Quaternion(quaternion2.x / num2, quaternion2.y / num2, quaternion2.z / num2, quaternion2.w / num2);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00006534 File Offset: 0x00004734
		public static Quaternion FacingPose(D20Shape shape, int[] labels, int value)
		{
			int num = Array.IndexOf<int>(labels, value);
			if (num < 0)
			{
				num = 0;
			}
			Vector3 vector = shape.Normals[num];
			Vector3 normalized = (shape.Vertices[shape.Faces[num][0]] - shape.Centers[num]).normalized;
			Quaternion quaternion = Quaternion.LookRotation(vector, normalized);
			Quaternion quaternion2 = Quaternion.LookRotation(Vector3.back, Vector3.up);
			return quaternion2 * Quaternion.Inverse(quaternion);
		}

		// Single-die spin: a clean in-place tumble that decelerates (ease-out quartic, like the reference
		// overlay) and ends exactly on the rolled face. The "landing" feel comes from the hit effects.
		public static DiceRoll Spin(D20Shape shape, int value)
		{
			return DiceSim.Spin(shape, value, 1f, 1f);
		}

		public static DiceRoll Spin(D20Shape shape, int value, float speed, float length)
		{
			return DiceSim.Spin(shape, value, speed, length, false);
		}

		// "travel" makes the die drift in from a corner of the arena and settle in the middle (BG3 style).
		public static DiceRoll Spin(D20Shape shape, int value, float speed, float length, bool travel)
		{
			DiceRoll diceRoll = new DiceRoll();
			diceRoll.Labels = shape.DefaultLabels();
			Quaternion target = DiceSim.FacingPose(shape, diceRoll.Labels, value);
			Vector3 axis1 = UnityEngine.Random.onUnitSphere;
			Vector3 axis2 = UnityEngine.Random.onUnitSphere;
			float total = UnityEngine.Random.Range(900f, 1080f) * speed;
			float turnRate = UnityEngine.Random.Range(40f, 70f) * speed;
			float duration = 1.4f * Mathf.Max(0.2f, length);
			int frames = Mathf.RoundToInt(duration * 60f);
			int lastStep = -1;
			float lastTick = -1f;
			Vector3 lastPos = Vector3.zero;
			Vector2 lastVel = Vector2.zero;
			float lastBounce = -1f;
			for (int i = 0; i <= frames; i++)
			{
				float p = (float)i / (float)frames;
				float time = p * duration;
				float angle = total * Mathf.Pow(1f - p, 4f);
				Vector3 axis = (Quaternion.AngleAxis(time * turnRate * (1f - p), axis2) * axis1).normalized;
				Vector3 pos = Vector3.zero;
				float amp = (1f - p) * (1f - p);
				if (travel)
				{
					pos = new Vector3(1.7f * amp * Mathf.Cos(7.2f * p + 0.5f), 2.1f * amp * Mathf.Sin(5.6f * p + 1.15f), 0f);
				}
				diceRoll.Frames.Add(new DiceFrame
				{
					Position = pos,
					Rotation = Quaternion.AngleAxis(angle, axis) * target
				});
				if (travel)
				{
					Vector2 vel = (i > 0) ? new Vector2(pos.x - lastPos.x, pos.y - lastPos.y) : Vector2.zero;
					bool flip = i > 1 && (vel.x * lastVel.x < 0f || vel.y * lastVel.y < 0f);
					if (flip && amp > 0.02f && time - lastBounce > 0.12f)
					{
						diceRoll.ImpactTimes.Add(time);
						diceRoll.ImpactStrengths.Add(Mathf.Clamp(0.25f + 0.45f * amp, 0.25f, 0.7f));
						lastBounce = time;
					}
					lastPos = pos;
					lastVel = vel;
				}
				// quiet ticks that spread out as the die slows down
				int step = Mathf.FloorToInt(angle / 80f);
				if (step != lastStep)
				{
					if (lastStep >= 0 && angle > 40f && time - lastTick > 0.06f)
					{
						diceRoll.ImpactTimes.Add(time);
						diceRoll.ImpactStrengths.Add(UnityEngine.Random.Range(0.08f, 0.22f));
						lastTick = time;
					}
					lastStep = step;
				}
			}
			diceRoll.ImpactTimes.Add(duration);
			diceRoll.ImpactStrengths.Add(0.8f);
			return diceRoll;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00006784 File Offset: 0x00004984
		private static float EaseOutBack(float t)
		{
			float num = t - 1f;
			return 1f + 2.2f * num * num * num + 1.2f * num * num;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x000067B4 File Offset: 0x000049B4
		public static DiceFrame RestingPose(D20Shape shape, Quaternion randomRotation)
		{
			int num = shape.TopFace(randomRotation);
			Quaternion quaternion = Quaternion.FromToRotation(randomRotation * shape.Normals[num], Vector3.up) * randomRotation;
			float num2 = float.MaxValue;
			foreach (Vector3 vector in shape.Vertices)
			{
				num2 = Mathf.Min(num2, (quaternion * vector).y);
			}
			return new DiceFrame
			{
				Position = new Vector3(0f, -num2, 0f),
				Rotation = quaternion
			};
		}

		// Token: 0x0400007A RID: 122
		public const float TrayHalfX = 7f;

		// Token: 0x0400007B RID: 123
		public const float TrayHalfZ = 4.2f;

		// Token: 0x0400007C RID: 124
		public const float WallHeight = 1.4f;

		// Token: 0x0400007D RID: 125
		private const int SubSteps = 4;

		// Token: 0x0400007E RID: 126
		private const float MaxSeconds = 4.5f;

		// Token: 0x0400007F RID: 127
		private const float Gravity = -42f;

		// Token: 0x04000080 RID: 128
		private const float InvMass = 1f;

		// Token: 0x04000081 RID: 129
		private const float InvInertia = 2.6f;

		// Token: 0x04000082 RID: 130
		private const float Friction = 0.45f;

		// Token: 0x04000083 RID: 131
		private static readonly DiceSim.Plane[] s_Planes = new DiceSim.Plane[]
		{
			new DiceSim.Plane
			{
				Normal = Vector3.up,
				Offset = 0f,
				Restitution = 0.32f
			},
			new DiceSim.Plane
			{
				Normal = Vector3.right,
				Offset = -7f,
				Restitution = 0.5f
			},
			new DiceSim.Plane
			{
				Normal = Vector3.left,
				Offset = -7f,
				Restitution = 0.5f
			},
			new DiceSim.Plane
			{
				Normal = Vector3.forward,
				Offset = -4.2f,
				Restitution = 0.5f
			},
			new DiceSim.Plane
			{
				Normal = Vector3.back,
				Offset = -4.2f,
				Restitution = 0.5f
			}
		};

		// Token: 0x02000013 RID: 19
		private struct Plane
		{
			// Token: 0x04000084 RID: 132
			public Vector3 Normal;

			// Token: 0x04000085 RID: 133
			public float Offset;

			// Token: 0x04000086 RID: 134
			public float Restitution;
		}
	}
}
