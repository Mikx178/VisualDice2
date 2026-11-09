using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogDice
{
	// Token: 0x02000011 RID: 17
	public class DiceRoll
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00005D9C File Offset: 0x00003F9C
		public float Duration
		{
			get
			{
				return (float)this.Frames.Count / 60f * this.Stretch;
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00005DB0 File Offset: 0x00003FB0
		public DiceFrame FrameAt(float time)
		{
			time = time / Mathf.Max(0.05f, this.Stretch);
			int num = Mathf.Clamp(Mathf.FloorToInt(time * 60f), 0, this.Frames.Count - 1);
			int num2 = Mathf.Min(num + 1, this.Frames.Count - 1);
			float num3 = Mathf.Clamp01(time * 60f - (float)num);
			return new DiceFrame
			{
				Position = Vector3.Lerp(this.Frames[num].Position, this.Frames[num2].Position, num3),
				Rotation = Quaternion.Slerp(this.Frames[num].Rotation, this.Frames[num2].Rotation, num3)
			};
		}

		// Slows the whole animation down (or speeds it up) without changing the simulation.
		public float Stretch = 1f;

		public void ApplyStretch(float stretch)
		{
			this.Stretch = Mathf.Max(0.05f, stretch);
			for (int i = 0; i < this.ImpactTimes.Count; i++)
			{
				this.ImpactTimes[i] = this.ImpactTimes[i] * this.Stretch;
			}
		}

		// Token: 0x04000075 RID: 117
		public const float FrameRate = 60f;

		// Token: 0x04000076 RID: 118
		public readonly List<DiceFrame> Frames = new List<DiceFrame>();

		// Token: 0x04000077 RID: 119
		public readonly List<float> ImpactTimes = new List<float>();

		// Token: 0x04000078 RID: 120
		public readonly List<float> ImpactStrengths = new List<float>();

		// Token: 0x04000079 RID: 121
		public int[] Labels;
	}
}
