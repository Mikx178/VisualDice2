using System;
using System.Collections.Generic;

namespace DialogDice
{
	// Token: 0x02000014 RID: 20
	public class DiceSound : IDisposable
	{
		// Token: 0x06000064 RID: 100 RVA: 0x000069A0 File Offset: 0x00004BA0
		public DiceSound(NativeAudio audio, SoundBank bank, Func<float> volume, Func<string, string> choice)
		{
			this.m_Audio = audio;
			this.m_Bank = bank;
			this.m_Volume = volume;
			this.m_Choice = choice;
			for (int i = 0; i < 4; i++)
			{
				this.m_Ticks[i] = NativeAudio.FromMono(SoundSynth.Tick(i));
				this.m_Clacks[i] = NativeAudio.FromMono(SoundSynth.Clack(i));
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00006A65 File Offset: 0x00004C65
		public bool HasRecordedDice
		{
			get
			{
				return this.m_Bank.Has("roll");
			}
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00006A78 File Offset: 0x00004C78
		private void Play(float[] sound, float gain, float maxSeconds = 0f)
		{
			if (this.m_Audio == null || sound == null)
			{
				return;
			}
			this.m_Audio.MasterVolume = Math.Max(0f, Math.Min(1f, this.m_Volume()));
			this.m_Audio.Play(sound, gain, maxSeconds, 1f);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00006AD0 File Offset: 0x00004CD0
		public void PlayShake()
		{
			this.Play(this.m_Bank.Random("shake"), 1f, 0f);
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00006AF4 File Offset: 0x00004CF4
		public void PlayLanding(float gain = 1f)
		{
			float[] array = this.m_Bank.Random("roll");
			if (array != null)
			{
				this.Play(array, 1f * gain, 0f);
				return;
			}
			this.Play(this.m_Clacks[this.m_Random.Next(this.m_Clacks.Length)], 0.85f * gain, 0f);
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00006B58 File Offset: 0x00004D58
		public void PlayImpact(float strength)
		{
			if (strength < 0.35f)
			{
				this.Play(this.m_Ticks[this.m_Random.Next(this.m_Ticks.Length)], 0.2f + strength, 0f);
				return;
			}
			this.Play(this.m_Clacks[this.m_Random.Next(this.m_Clacks.Length)], 0.5f + 0.5f * strength, 0f);
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00006BCC File Offset: 0x00004DCC
		private static string FallbackOf(string outcome)
		{
			if (outcome == "critical")
			{
				return "success";
			}
			if (!(outcome == "fumble"))
			{
				return null;
			}
			return "failure";
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00006BF8 File Offset: 0x00004DF8
		public List<DiceSound.Choice> ChoicesFor(string outcome)
		{
			List<DiceSound.Choice> list = new List<DiceSound.Choice>
			{
				new DiceSound.Choice
				{
					Value = "",
					Label = "Random"
				}
			};
			foreach (SoundBank.Sound sound in this.m_Bank.All(outcome))
			{
				list.Add(new DiceSound.Choice
				{
					Value = outcome + "/" + sound.Name,
					Label = sound.Name
				});
			}
			string text = DiceSound.FallbackOf(outcome);
			if (text != null)
			{
				foreach (SoundBank.Sound sound2 in this.m_Bank.All(text))
				{
					list.Add(new DiceSound.Choice
					{
						Value = text + "/" + sound2.Name,
						Label = sound2.Name + " (" + ((text == "success") ? "Success" : "Failure") + ")"
					});
				}
			}
			list.Add(new DiceSound.Choice
			{
				Value = "~",
				Label = "Generated"
			});
			list.Add(new DiceSound.Choice
			{
				Value = "-",
				Label = "Off"
			});
			return list;
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00006DB4 File Offset: 0x00004FB4
		private float[] Resolve(string outcome)
		{
			string text = this.m_Choice(outcome) ?? "";
			if (text == "-")
			{
				return null;
			}
			if (text != "~")
			{
				int num = text.IndexOf('/');
				if (num > 0)
				{
					SoundBank.Sound sound = this.m_Bank.Find(text.Substring(0, num), text.Substring(num + 1));
					if (sound != null)
					{
						return sound.Data;
					}
				}
				float[] array = this.m_Bank.Random(outcome);
				string text2 = DiceSound.FallbackOf(outcome);
				if (array == null && text2 != null)
				{
					array = this.m_Bank.Random(text2);
				}
				if (array != null)
				{
					return array;
				}
			}
			if (outcome != null)
			{
				if (outcome == "critical")
				{
					return this.m_Critical;
				}
				if (outcome == "fumble")
				{
					return this.m_Fumble;
				}
				if (outcome == "success")
				{
					return this.m_Success;
				}
			}
			return this.m_Failure;
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00006EA4 File Offset: 0x000050A4
		public void PlayOutcome(bool passed, int d20)
		{
			bool crit = d20 == 20 && passed;
			bool fumble = d20 == 1 && !passed;
			string text = crit ? "critical" : (fumble ? "fumble" : (passed ? "success" : "failure"));
			this.Play(this.Resolve(text), 0.4f, (crit || fumble) ? 10f : 4.5f);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00006F00 File Offset: 0x00005100
		public void Preview(string what)
		{
			if (what != null)
			{
				if (what == "shake")
				{
					this.PlayShake();
					return;
				}
				if (what == "roll")
				{
					this.PlayLanding(1f);
					return;
				}
				if (what == "success")
				{
					this.PlayOutcome(true, 10);
					return;
				}
				if (what == "failure")
				{
					this.PlayOutcome(false, 10);
					return;
				}
				if (what == "critical")
				{
					this.PlayOutcome(true, 20);
					return;
				}
				if (!(what == "fumble"))
				{
					return;
				}
				this.PlayOutcome(false, 1);
			}
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00006F9D File Offset: 0x0000519D
		public void Dispose()
		{
			if (this.m_Audio != null)
			{
				this.m_Audio.Dispose();
			}
		}

		// Token: 0x04000087 RID: 135
		public const float TickThreshold = 0.35f;

		// Token: 0x04000088 RID: 136
		private const float OutcomeSeconds = 4.5f;

		// Token: 0x04000089 RID: 137
		private const float NaturalSeconds = 10f;

		// Token: 0x0400008A RID: 138
		private const float FadeSeconds = 1f;

		// Token: 0x0400008B RID: 139
		private const float DiceGain = 1f;

		// Token: 0x0400008C RID: 140
		private const float OutcomeGain = 0.4f;

		// Token: 0x0400008D RID: 141
		public const string ChoiceRandom = "";

		// Token: 0x0400008E RID: 142
		public const string ChoiceGenerated = "~";

		// Token: 0x0400008F RID: 143
		public const string ChoiceNone = "-";

		// Token: 0x04000090 RID: 144
		private readonly NativeAudio m_Audio;

		// Token: 0x04000091 RID: 145
		private readonly SoundBank m_Bank;

		// Token: 0x04000092 RID: 146
		private readonly Func<float> m_Volume;

		// Token: 0x04000093 RID: 147
		private readonly Random m_Random = new Random();

		// Token: 0x04000094 RID: 148
		private readonly float[][] m_Ticks = new float[4][];

		// Token: 0x04000095 RID: 149
		private readonly float[][] m_Clacks = new float[4][];

		// Token: 0x04000096 RID: 150
		private readonly float[] m_Success = NativeAudio.FromMono(SoundSynth.Success());

		// Token: 0x04000097 RID: 151
		private readonly float[] m_Critical = NativeAudio.FromMono(SoundSynth.Critical());

		// Token: 0x04000098 RID: 152
		private readonly float[] m_Failure = NativeAudio.FromMono(SoundSynth.Failure());

		// Token: 0x04000099 RID: 153
		private readonly float[] m_Fumble = NativeAudio.FromMono(SoundSynth.Fumble());

		// Token: 0x0400009A RID: 154
		private readonly Func<string, string> m_Choice;

		// Token: 0x0400009B RID: 155
		public static readonly string[] Outcomes = new string[] { "success", "failure", "critical", "fumble" };

		// Token: 0x02000015 RID: 21
		public struct Choice
		{
			// Token: 0x0400009C RID: 156
			public string Value;

			// Token: 0x0400009D RID: 157
			public string Label;
		}
	}
}
