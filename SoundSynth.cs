using System;

namespace DialogDice
{
	// Token: 0x0200002C RID: 44
	public static class SoundSynth
	{
		// Token: 0x060000EE RID: 238 RVA: 0x0000BE38 File Offset: 0x0000A038
		public static float[] Tick(int seed)
		{
			Random random = new Random(seed * 7919 + 3);
			double num = 2800.0 + random.NextDouble() * 1400.0;
			float[] array = SoundSynth.New(0.04);
			double num2 = 0.0;
			for (int i = 0; i < array.Length; i++)
			{
				double num3 = SoundSynth.T(i);
				double num4 = random.NextDouble() * 2.0 - 1.0;
				double num5 = num4 - num2;
				num2 = num4;
				array[i] = (float)(0.6 * num5 * Math.Exp(-num3 * 190.0) + 0.5 * Math.Sin(6.283185307179586 * num * num3) * Math.Exp(-num3 * 230.0));
			}
			return SoundSynth.Normalize(array, 0.8);
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000BF34 File Offset: 0x0000A134
		public static float[] Clack(int seed)
		{
			Random random = new Random(seed * 104729 + 11);
			double num = 0.9 + random.NextDouble() * 0.2;
			float[] array = SoundSynth.New(0.3);
			SoundSynth.AddClackHit(array, 0.0, 1.0, num, random);
			SoundSynth.AddClackHit(array, 0.045 + random.NextDouble() * 0.01, 0.33, num * 1.03, random);
			SoundSynth.AddClackHit(array, 0.078 + random.NextDouble() * 0.01, 0.13, num * 1.06, random);
			return SoundSynth.Normalize(array, 0.9);
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000C010 File Offset: 0x0000A210
		private static void AddClackHit(float[] buf, double start, double gain, double j, Random rng)
		{
			int num = (int)(start * 44100.0);
			for (int i = num; i < buf.Length; i++)
			{
				double num2 = SoundSynth.T(i - num);
				double num3 = 0.55 * Math.Sin(10367.255756846318 * j * num2) * Math.Exp(-num2 * 55.0) + 0.35 * Math.Sin(24504.422698000388 * j * num2 + 0.3) * Math.Exp(-num2 * 85.0) + 0.22 * Math.Sin(38327.430373795476 * j * num2) * Math.Exp(-num2 * 140.0);
				double num4 = 0.6 * Math.Sin(879.645943005142 * num2) * Math.Exp(-num2 * 35.0) * (1.0 - Math.Exp(-num2 * 900.0));
				double num5 = (rng.NextDouble() * 2.0 - 1.0) * Math.Exp(-num2 * 500.0) * 0.6;
				buf[i] += (float)(gain * (num3 + num4 + num5));
			}
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000C1B8 File Offset: 0x0000A3B8
		public static float[] Success()
		{
			float[] array = SoundSynth.New(2.0);
			double[] array2 = new double[] { 523.25, 659.25, 783.99, 1046.5 };
			for (int i = 0; i < array2.Length; i++)
			{
				SoundSynth.AddBell(array, (double)i * 0.085, array2[i], 0.55 + 0.1 * (double)i);
			}
			SoundSynth.AddPad(array, 0.05, 1.7, new double[] { 261.63, 329.63, 392.0 }, 0.12);
			return SoundSynth.Normalize(array, 0.85);
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000C2B8 File Offset: 0x0000A4B8
		public static float[] Critical()
		{
			float[] array = SoundSynth.New(2.8);
			double[] array2 = new double[] { 523.25, 659.25, 783.99, 1046.5 };
			double[] array3 = new double[] { 1318.5, 1567.98, 2093.0 };
			for (int i = 0; i < array2.Length; i++)
			{
				SoundSynth.AddBell(array, (double)i * 0.08, array2[i], 0.6);
			}
			for (int j = 0; j < array3.Length; j++)
			{
				SoundSynth.AddBell(array, 0.36 + (double)j * 0.08, array3[j], 0.45);
			}
			SoundSynth.AddPad(array, 0.0, 2.4, new double[] { 130.81, 261.63, 329.63, 392.0 }, 0.14);
			Random random = new Random(20);
			for (int k = 0; k < 14; k++)
			{
				SoundSynth.AddBell(array, 0.45 + random.NextDouble() * 1.4, 2600.0 + random.NextDouble() * 2400.0, 0.12);
			}
			SoundSynth.AddBoom(array, 0.0, 65.0, 0.5);
			return SoundSynth.Normalize(array, 0.9);
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000C424 File Offset: 0x0000A624
		public static float[] Failure()
		{
			float[] array = SoundSynth.New(1.9);
			SoundSynth.AddTone(array, 0.0, 0.32, 311.13, 0.5, 0.0);
			SoundSynth.AddTone(array, 0.2, 0.34, 233.08, 0.5, 0.0);
			SoundSynth.AddTone(array, 0.42, 1.3, 185.0, 0.65, 0.035);
			SoundSynth.AddBoom(array, 0.42, 80.0, 0.35);
			return SoundSynth.Normalize(array, 0.8);
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000C50C File Offset: 0x0000A70C
		public static float[] Fumble()
		{
			float[] array = SoundSynth.New(2.2);
			SoundSynth.AddTone(array, 0.0, 0.32, 311.13, 0.5, 0.0);
			SoundSynth.AddTone(array, 0.2, 0.34, 233.08, 0.5, 0.0);
			SoundSynth.AddTone(array, 0.42, 1.6, 92.5, 0.8, 0.05);
			SoundSynth.AddTone(array, 0.42, 1.4, 185.0, 0.35, 0.05);
			SoundSynth.AddRumble(array, 0.4, 0.9, 0.45);
			SoundSynth.AddBoom(array, 0.42, 55.0, 0.5);
			return SoundSynth.Normalize(array, 0.85);
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000C648 File Offset: 0x0000A848
		private static void AddBell(float[] buf, double start, double freq, double gain)
		{
			int num = (int)(start * 44100.0);
			for (int i = num; i < buf.Length; i++)
			{
				double num2 = SoundSynth.T(i - num);
				double num3 = Math.Exp(-num2 * 2.4) * (1.0 - Math.Exp(-num2 * 400.0));
				double num4 = 2.0 * Math.Exp(-num2 * 3.5) * Math.Sin(8.859291283123216 * freq * num2);
				double num5 = Math.Sin(6.283185307179586 * freq * num2 + num4) + 0.22 * Math.Sin(12.566370614359172 * freq * num2) * Math.Exp(-num2 * 4.0);
				buf[i] += (float)(gain * num3 * num5);
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000C740 File Offset: 0x0000A940
		private static void AddPad(float[] buf, double start, double length, double[] freqs, double gain)
		{
			int num = (int)(start * 44100.0);
			int num2 = Math.Min(buf.Length, num + (int)(length * 44100.0));
			for (int i = num; i < num2; i++)
			{
				double num3 = SoundSynth.T(i - num);
				double num4 = (1.0 - Math.Exp(-num3 * 6.0)) * Math.Max(0.0, 1.0 - num3 / length);
				double num5 = 0.0;
				foreach (double num6 in freqs)
				{
					num5 += Math.Sin(6.283185307179586 * num6 * num3) + 0.3 * Math.Sin(6.283185307179586 * num6 * 2.003 * num3);
				}
				buf[i] += (float)(gain * num4 * num5 / (double)freqs.Length);
			}
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000C850 File Offset: 0x0000AA50
		private static void AddTone(float[] buf, double start, double length, double freq, double gain, double sag)
		{
			int num = (int)(start * 44100.0);
			int num2 = Math.Min(buf.Length, num + (int)((length + 0.4) * 44100.0));
			double num3 = 0.0;
			for (int i = num; i < num2; i++)
			{
				double num4 = SoundSynth.T(i - num);
				double num5 = freq * (1.0 - sag * Math.Min(1.0, num4 / length));
				num3 += 6.283185307179586 * num5 / 44100.0;
				double num6 = (1.0 - Math.Exp(-num4 * 40.0)) * Math.Exp(-num4 * (2.6 / length));
				double num7 = 0.0;
				for (int j = 1; j <= 6; j++)
				{
					num7 += Math.Sin(num3 * (double)j) / Math.Pow((double)j, 1.6);
				}
				buf[i] += (float)(gain * num6 * num7 * 0.6);
			}
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000C984 File Offset: 0x0000AB84
		private static void AddBoom(float[] buf, double start, double freq, double gain)
		{
			int num = (int)(start * 44100.0);
			for (int i = num; i < buf.Length; i++)
			{
				double num2 = SoundSynth.T(i - num);
				double num3 = freq * (1.0 + 0.6 * Math.Exp(-num2 * 18.0));
				buf[i] += (float)(gain * Math.Sin(6.283185307179586 * num3 * num2) * Math.Exp(-num2 * 6.0) * (1.0 - Math.Exp(-num2 * 300.0)));
			}
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000CA3C File Offset: 0x0000AC3C
		private static void AddRumble(float[] buf, double start, double length, double gain)
		{
			Random random = new Random(1);
			int num = (int)(start * 44100.0);
			int num2 = Math.Min(buf.Length, num + (int)(length * 44100.0));
			double num3 = 0.0;
			for (int i = num; i < num2; i++)
			{
				double num4 = SoundSynth.T(i - num);
				num3 += 0.02 * (random.NextDouble() * 2.0 - 1.0 - num3);
				double num5 = Math.Sin(3.141592653589793 * num4 / length);
				buf[i] += (float)(gain * 6.0 * num3 * num5);
			}
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000CAFC File Offset: 0x0000ACFC
		private static float[] New(double seconds)
		{
			return new float[(int)(seconds * 44100.0)];
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000CB0F File Offset: 0x0000AD0F
		private static double T(int sample)
		{
			return (double)sample / 44100.0;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000CB20 File Offset: 0x0000AD20
		private static float[] Normalize(float[] buf, double peak)
		{
			float num = 0f;
			foreach (float num2 in buf)
			{
				num = Math.Max(num, Math.Abs(num2));
			}
			if (num <= 0f)
			{
				return buf;
			}
			float num3 = (float)(peak / (double)num);
			for (int j = 0; j < buf.Length; j++)
			{
				buf[j] *= num3;
			}
			int num4 = Math.Min(buf.Length, 441);
			for (int k = 0; k < num4; k++)
			{
				buf[buf.Length - 1 - k] *= (float)k / (float)num4;
			}
			return buf;
		}

		// Token: 0x0400014E RID: 334
		public const int SampleRate = 44100;

		// Token: 0x0400014F RID: 335
		private const double TwoPi = 6.283185307179586;
	}
}
