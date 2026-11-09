using System;
using System.Collections.Generic;
using System.IO;

namespace DialogDice
{
	// Token: 0x0200002A RID: 42
	public class SoundBank
	{
		// Token: 0x060000E3 RID: 227 RVA: 0x0000BA79 File Offset: 0x00009C79
		public bool Has(string category)
		{
			return this.All(category).Count > 0;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000BA8C File Offset: 0x00009C8C
		public List<SoundBank.Sound> All(string category)
		{
			List<SoundBank.Sound> list;
			if (!this.m_Sounds.TryGetValue(category, out list))
			{
				return new List<SoundBank.Sound>();
			}
			return list;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000BAB0 File Offset: 0x00009CB0
		public float[] Random(string category)
		{
			List<SoundBank.Sound> list = this.All(category);
			if (list.Count != 0)
			{
				return list[this.m_Random.Next(list.Count)].Data;
			}
			return null;
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000BAEC File Offset: 0x00009CEC
		public SoundBank.Sound Find(string category, string name)
		{
			foreach (SoundBank.Sound sound in this.All(category))
			{
				if (string.Equals(sound.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					return sound;
				}
			}
			return null;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000BB50 File Offset: 0x00009D50
		public string Summary()
		{
			List<string> list = new List<string>();
			foreach (string text in SoundBank.Categories)
			{
				int count = this.All(text).Count;
				if (count > 0)
				{
					list.Add(text + " " + count);
				}
			}
			if (list.Count != 0)
			{
				return string.Join(", ", list.ToArray());
			}
			return "none";
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x0000BBC8 File Offset: 0x00009DC8
		public void Load(string folder, Action<string> log)
		{
			this.m_Sounds.Clear();
			if (!Directory.Exists(folder))
			{
				log("Sounds folder missing: " + folder);
				return;
			}
			string[] files = Directory.GetFiles(folder);
			Array.Sort<string>(files, StringComparer.OrdinalIgnoreCase);
			foreach (string text in files)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
				string text2 = SoundBank.CategoryOf(fileNameWithoutExtension);
				if (text2 != null)
				{
					if (!text.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
					{
						log("Only WAV is supported, skipped: " + Path.GetFileName(text));
					}
					else
					{
						try
						{
							SoundBank.Sound sound = new SoundBank.Sound
							{
								Category = text2,
								Name = SoundBank.NameOf(fileNameWithoutExtension, text2),
								Data = NativeAudio.LoadWav(text)
							};
							List<SoundBank.Sound> list;
							if (!this.m_Sounds.TryGetValue(text2, out list))
							{
								list = (this.m_Sounds[text2] = new List<SoundBank.Sound>());
							}
							list.Add(sound);
						}
						catch (Exception ex)
						{
							log(string.Concat(new string[]
							{
								"Sound not readable: ",
								Path.GetFileName(text),
								" (",
								ex.Message,
								")"
							}));
						}
					}
				}
			}
			log("Sounds loaded: " + this.Summary());
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x0000BD38 File Offset: 0x00009F38
		private static string CategoryOf(string name)
		{
			string text = name.ToLowerInvariant();
			foreach (string text2 in SoundBank.Categories)
			{
				if (text.StartsWith(text2))
				{
					return text2;
				}
			}
			return null;
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000BD7C File Offset: 0x00009F7C
		private static string NameOf(string baseName, string category)
		{
			string text = baseName.Substring(category.Length).Trim(new char[] { '_', '-', ' ' }).Replace('_', ' ');
			if (text.Length <= 0)
			{
				return category;
			}
			return text;
		}

		// Token: 0x04000148 RID: 328
		public static readonly string[] Categories = new string[] { "shake", "roll", "success", "failure", "critical", "fumble" };

		// Token: 0x04000149 RID: 329
		private readonly Dictionary<string, List<SoundBank.Sound>> m_Sounds = new Dictionary<string, List<SoundBank.Sound>>();

		// Token: 0x0400014A RID: 330
		private readonly Random m_Random = new Random();

		// Token: 0x0200002B RID: 43
		public class Sound
		{
			// Token: 0x0400014B RID: 331
			public string Category;

			// Token: 0x0400014C RID: 332
			public string Name;

			// Token: 0x0400014D RID: 333
			public float[] Data;
		}
	}
}
