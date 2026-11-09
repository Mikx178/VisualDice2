using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DialogDice
{
	// Token: 0x02000018 RID: 24
	public sealed class NativeAudio : IDisposable
	{
		// Token: 0x0600007E RID: 126 RVA: 0x000079B8 File Offset: 0x00005BB8
		private NativeAudio()
		{
			NativeAudio.WaveFormat waveFormat = new NativeAudio.WaveFormat
			{
				wFormatTag = 1,
				nChannels = 2,
				nSamplesPerSec = 44100,
				wBitsPerSample = 16,
				nBlockAlign = 4,
				nAvgBytesPerSec = 176400,
				cbSize = 0
			};
			int num = NativeAudio.waveOutOpen(out this.m_Device, NativeAudio.WaveMapper, ref waveFormat, IntPtr.Zero, IntPtr.Zero, 0);
			if (num != 0)
			{
				throw new InvalidOperationException("waveOutOpen failed: " + num);
			}
			int num2 = Marshal.SizeOf(typeof(NativeAudio.WaveHeader));
			int num3 = 4096;
			for (int i = 0; i < 4; i++)
			{
				this.m_Data[i] = Marshal.AllocHGlobal(num3);
				this.m_Headers[i] = Marshal.AllocHGlobal(num2);
				NativeAudio.WaveHeader waveHeader = new NativeAudio.WaveHeader
				{
					lpData = this.m_Data[i],
					dwBufferLength = (uint)num3
				};
				Marshal.StructureToPtr<NativeAudio.WaveHeader>(waveHeader, this.m_Headers[i], false);
				NativeAudio.waveOutPrepareHeader(this.m_Device, this.m_Headers[i], num2);
			}
			this.m_Thread = new Thread(new ThreadStart(this.Run))
			{
				IsBackground = true,
				Name = "DialogDice audio"
			};
			this.m_Thread.Start();
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00007BE0 File Offset: 0x00005DE0
		public static NativeAudio TryCreate(Action<string> log)
		{
			NativeAudio nativeAudio;
			try
			{
				nativeAudio = new NativeAudio();
			}
			catch (Exception ex)
			{
				log("Windows audio not available: " + ex.Message);
				nativeAudio = null;
			}
			return nativeAudio;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00007C24 File Offset: 0x00005E24
		public void Play(float[] stereo, float gain, float maxSeconds, float fadeSeconds)
		{
			if (stereo == null || stereo.Length == 0)
			{
				return;
			}
			NativeAudio.Voice voice = new NativeAudio.Voice
			{
				Data = stereo,
				Gain = gain,
				FadeStart = ((maxSeconds > 0f) ? ((int)(maxSeconds * 44100f)) : int.MaxValue),
				FadeFrames = Math.Max(1, (int)(fadeSeconds * 44100f))
			};
			lock (this.m_Lock)
			{
				this.m_Voices.Add(voice);
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000081 RID: 129 RVA: 0x00007CBC File Offset: 0x00005EBC
		public int VoiceCount
		{
			get
			{
				int count;
				lock (this.m_Lock)
				{
					count = this.m_Voices.Count;
				}
				return count;
			}
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00007D04 File Offset: 0x00005F04
		public void StopAll()
		{
			lock (this.m_Lock)
			{
				this.m_Voices.Clear();
			}
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00007D4C File Offset: 0x00005F4C
		private void Run()
		{
			int num = Marshal.SizeOf(typeof(NativeAudio.WaveHeader));
			while (this.m_Running)
			{
				for (int i = 0; i < 4; i++)
				{
					uint num2 = (uint)Marshal.ReadInt32(this.m_Headers[i], this.m_FlagsOffset);
					if (!this.m_Queued[i] || (num2 & 1U) != 0U)
					{
						this.MixInto(this.m_Data[i]);
						NativeAudio.waveOutWrite(this.m_Device, this.m_Headers[i], num);
						this.m_Queued[i] = true;
					}
				}
				Thread.Sleep(5);
			}
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00007DF4 File Offset: 0x00005FF4
		private void MixInto(IntPtr target)
		{
			Array.Clear(this.m_Mix, 0, this.m_Mix.Length);
			float num = Math.Max(0f, Math.Min(1f, this.MasterVolume));
			lock (this.m_Lock)
			{
				for (int i = this.m_Voices.Count - 1; i >= 0; i--)
				{
					NativeAudio.Voice voice = this.m_Voices[i];
					int num2 = voice.Data.Length / 2;
					int num3 = (int)Math.Min((long)num2, (long)voice.FadeStart + (long)voice.FadeFrames);
					int num4 = 0;
					while (num4 < 1024 && voice.Frame < num3)
					{
						float num5 = voice.Gain;
						if (voice.Frame > voice.FadeStart)
						{
							float num6 = 1f - (float)(voice.Frame - voice.FadeStart) / (float)voice.FadeFrames;
							num5 *= num6 * num6;
						}
						this.m_Mix[num4 * 2] += voice.Data[voice.Frame * 2] * num5;
						this.m_Mix[num4 * 2 + 1] += voice.Data[voice.Frame * 2 + 1] * num5;
						num4++;
						voice.Frame++;
					}
					if (voice.Frame >= num3)
					{
						this.m_Voices.RemoveAt(i);
					}
				}
			}
			for (int j = 0; j < this.m_Mix.Length; j++)
			{
				float num7 = this.m_Mix[j] * num;
				if (num7 > 0.9f || num7 < -0.9f)
				{
					num7 = (float)Math.Sign(num7) * (0.9f + 0.1f * (float)Math.Tanh((double)((Math.Abs(num7) - 0.9f) * 10f)));
				}
				this.m_Pcm[j] = (short)(num7 * 32767f);
			}
			Marshal.Copy(this.m_Pcm, 0, target, this.m_Pcm.Length);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00008030 File Offset: 0x00006230
		public void Dispose()
		{
			this.m_Running = false;
			if (this.m_Thread != null)
			{
				this.m_Thread.Join(500);
			}
			NativeAudio.waveOutReset(this.m_Device);
			int num = Marshal.SizeOf(typeof(NativeAudio.WaveHeader));
			for (int i = 0; i < 4; i++)
			{
				NativeAudio.waveOutUnprepareHeader(this.m_Device, this.m_Headers[i], num);
				Marshal.FreeHGlobal(this.m_Headers[i]);
				Marshal.FreeHGlobal(this.m_Data[i]);
			}
			NativeAudio.waveOutClose(this.m_Device);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000080E0 File Offset: 0x000062E0
		public static float[] FromMono(float[] mono)
		{
			float[] array = new float[mono.Length * 2];
			for (int i = 0; i < mono.Length; i++)
			{
				array[i * 2] = (array[i * 2 + 1] = mono[i]);
			}
			return array;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x0000811C File Offset: 0x0000631C
		public static float[] LoadWav(string path)
		{
			byte[] array = File.ReadAllBytes(path);
			if (array.Length < 12 || array[0] != 82 || array[1] != 73 || array[2] != 70 || array[3] != 70 || array[8] != 87)
			{
				throw new InvalidDataException("not a WAV file");
			}
			int num = 12;
			int num2 = 1;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = -1;
			int num7 = 0;
			while (num + 8 <= array.Length)
			{
				string @string = Encoding.ASCII.GetString(array, num, 4);
				int num8 = BitConverter.ToInt32(array, num + 4);
				if (@string == "fmt ")
				{
					num2 = (int)BitConverter.ToUInt16(array, num + 8);
					num3 = (int)BitConverter.ToUInt16(array, num + 10);
					num4 = BitConverter.ToInt32(array, num + 12);
					num5 = (int)BitConverter.ToUInt16(array, num + 22);
					if (num2 == 65534 && num8 >= 40)
					{
						num2 = (int)BitConverter.ToUInt16(array, num + 32);
					}
				}
				else if (@string == "data")
				{
					num6 = num + 8;
					num7 = Math.Min(num8, array.Length - num6);
				}
				num += 8 + num8 + (num8 & 1);
			}
			if (num6 < 0 || num3 == 0 || num4 == 0)
			{
				throw new InvalidDataException("incomplete WAV file");
			}
			int num9 = num5 / 8;
			int num10 = num7 / (num9 * num3);
			float[] array2 = new float[num10 * 2];
			for (int i = 0; i < num10; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					int num11 = Math.Min(j, num3 - 1);
					int num12 = num6 + (i * num3 + num11) * num9;
					array2[i * 2 + j] = NativeAudio.Sample(array, num12, num5, num2);
				}
			}
			if (num4 != 44100)
			{
				return NativeAudio.Resample(array2, num4);
			}
			return array2;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x000082C0 File Offset: 0x000064C0
		private static float Sample(byte[] d, int o, int bits, int format)
		{
			if (format == 3 && bits == 32)
			{
				return BitConverter.ToSingle(d, o);
			}
			if (bits <= 16)
			{
				if (bits == 8)
				{
					return (float)(d[o] - 128) / 128f;
				}
				if (bits == 16)
				{
					return (float)BitConverter.ToInt16(d, o) / 32768f;
				}
			}
			else
			{
				if (bits == 24)
				{
					return (float)((((int)d[o] << 8) | ((int)d[o + 1] << 16) | ((int)d[o + 2] << 24)) >> 8) / 8388608f;
				}
				if (bits == 32)
				{
					return (float)BitConverter.ToInt32(d, o) / 2.1474836E+09f;
				}
			}
			throw new InvalidDataException("not supported: " + bits + " bit");
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00008368 File Offset: 0x00006568
		private static float[] Resample(float[] stereo, int fromRate)
		{
			int num = stereo.Length / 2;
			int num2 = (int)((long)num * 44100L / (long)fromRate);
			float[] array = new float[num2 * 2];
			double num3 = (double)fromRate / 44100.0;
			for (int i = 0; i < num2; i++)
			{
				double num4 = (double)i * num3;
				int num5 = (int)num4;
				float num6 = (float)(num4 - (double)num5);
				int num7 = Math.Min(num5 + 1, num - 1);
				array[i * 2] = stereo[num5 * 2] + (stereo[num7 * 2] - stereo[num5 * 2]) * num6;
				array[i * 2 + 1] = stereo[num5 * 2 + 1] + (stereo[num7 * 2 + 1] - stereo[num5 * 2 + 1]) * num6;
			}
			return array;
		}

		// Token: 0x0600008A RID: 138
		[DllImport("winmm.dll")]
		private static extern int waveOutOpen(out IntPtr hwo, IntPtr deviceId, ref NativeAudio.WaveFormat format, IntPtr callback, IntPtr instance, int flags);

		// Token: 0x0600008B RID: 139
		[DllImport("winmm.dll")]
		private static extern int waveOutPrepareHeader(IntPtr hwo, IntPtr header, int size);

		// Token: 0x0600008C RID: 140
		[DllImport("winmm.dll")]
		private static extern int waveOutUnprepareHeader(IntPtr hwo, IntPtr header, int size);

		// Token: 0x0600008D RID: 141
		[DllImport("winmm.dll")]
		private static extern int waveOutWrite(IntPtr hwo, IntPtr header, int size);

		// Token: 0x0600008E RID: 142
		[DllImport("winmm.dll")]
		private static extern int waveOutReset(IntPtr hwo);

		// Token: 0x0600008F RID: 143
		[DllImport("winmm.dll")]
		private static extern int waveOutClose(IntPtr hwo);

		// Token: 0x040000BA RID: 186
		public const int Rate = 44100;

		// Token: 0x040000BB RID: 187
		private const int Channels = 2;

		// Token: 0x040000BC RID: 188
		private const int BufferFrames = 1024;

		// Token: 0x040000BD RID: 189
		private const int BufferCount = 4;

		// Token: 0x040000BE RID: 190
		private const uint WhdrDone = 1U;

		// Token: 0x040000BF RID: 191
		private readonly object m_Lock = new object();

		// Token: 0x040000C0 RID: 192
		private readonly List<NativeAudio.Voice> m_Voices = new List<NativeAudio.Voice>();

		// Token: 0x040000C1 RID: 193
		private readonly IntPtr m_Device;

		// Token: 0x040000C2 RID: 194
		private readonly IntPtr[] m_Headers = new IntPtr[4];

		// Token: 0x040000C3 RID: 195
		private readonly IntPtr[] m_Data = new IntPtr[4];

		// Token: 0x040000C4 RID: 196
		private readonly bool[] m_Queued = new bool[4];

		// Token: 0x040000C5 RID: 197
		private readonly float[] m_Mix = new float[2048];

		// Token: 0x040000C6 RID: 198
		private readonly short[] m_Pcm = new short[2048];

		// Token: 0x040000C7 RID: 199
		private readonly Thread m_Thread;

		// Token: 0x040000C8 RID: 200
		private readonly int m_FlagsOffset = Marshal.OffsetOf(typeof(NativeAudio.WaveHeader), "dwFlags").ToInt32();

		// Token: 0x040000C9 RID: 201
		private volatile bool m_Running = true;

		// Token: 0x040000CA RID: 202
		public volatile float MasterVolume = 1f;

		// Token: 0x040000CB RID: 203
		private static readonly IntPtr WaveMapper = new IntPtr(-1);

		// Token: 0x02000019 RID: 25
		private sealed class Voice
		{
			// Token: 0x040000CC RID: 204
			public float[] Data;

			// Token: 0x040000CD RID: 205
			public int Frame;

			// Token: 0x040000CE RID: 206
			public float Gain;

			// Token: 0x040000CF RID: 207
			public int FadeStart;

			// Token: 0x040000D0 RID: 208
			public int FadeFrames;
		}

		// Token: 0x0200001A RID: 26
		[StructLayout(LayoutKind.Sequential, Pack = 2)]
		private struct WaveFormat
		{
			// Token: 0x040000D1 RID: 209
			public ushort wFormatTag;

			// Token: 0x040000D2 RID: 210
			public ushort nChannels;

			// Token: 0x040000D3 RID: 211
			public int nSamplesPerSec;

			// Token: 0x040000D4 RID: 212
			public int nAvgBytesPerSec;

			// Token: 0x040000D5 RID: 213
			public ushort nBlockAlign;

			// Token: 0x040000D6 RID: 214
			public ushort wBitsPerSample;

			// Token: 0x040000D7 RID: 215
			public ushort cbSize;
		}

		// Token: 0x0200001B RID: 27
		private struct WaveHeader
		{
			// Token: 0x040000D8 RID: 216
			public IntPtr lpData;

			// Token: 0x040000D9 RID: 217
			public uint dwBufferLength;

			// Token: 0x040000DA RID: 218
			public uint dwBytesRecorded;

			// Token: 0x040000DB RID: 219
			public IntPtr dwUser;

			// Token: 0x040000DC RID: 220
			public uint dwFlags;

			// Token: 0x040000DD RID: 221
			public uint dwLoops;

			// Token: 0x040000DE RID: 222
			public IntPtr lpNext;

			// Token: 0x040000DF RID: 223
			public IntPtr reserved;
		}
	}
}
