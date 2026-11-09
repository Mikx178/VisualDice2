using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace DialogDice
{
	public static class Main
	{
		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			Main.Mod = modEntry;
			Main.Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
			modEntry.OnToggle = new Func<UnityModManager.ModEntry, bool, bool>(Main.OnToggle);
			modEntry.OnGUI = new Action<UnityModManager.ModEntry>(Main.OnGUI);
			modEntry.OnSaveGUI = new Action<UnityModManager.ModEntry>(Main.OnSaveGUI);
			new Harmony(modEntry.Info.Id).PatchAll(Assembly.GetExecutingAssembly());
			GameObject gameObject = new GameObject("DialogDice");
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<RollPopup>();
			Main.Enabled = true;
			return true;
		}

		private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
		{
			Main.Enabled = value;
			return true;
		}

		private static void OutcomeRow(string outcome, string label)
		{
			List<DiceSound.Choice> list = RollPopup.SoundChoices(outcome);
			if (list == null || list.Count == 0)
			{
				return;
			}
			string current = Main.Settings.ChoiceFor(outcome) ?? "";
			int num = list.FindIndex((DiceSound.Choice c) => c.Value == current);
			if (num < 0)
			{
				num = 0;
			}
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label(label, new GUILayoutOption[] { GUILayout.Width(120f) });
			if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(30f) }))
			{
				num = (num + list.Count - 1) % list.Count;
			}
			GUILayout.Label(list[num].Label, new GUILayoutOption[] { GUILayout.Width(260f) });
			if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(30f) }))
			{
				num = (num + 1) % list.Count;
			}
			Main.Settings.SetChoice(outcome, list[num].Value);
			if (GUILayout.Button("Preview", new GUILayoutOption[] { GUILayout.Width(100f) }))
			{
				RollPopup.PreviewSound(outcome);
			}
			GUILayout.EndHorizontal();
		}

		private static void OnGUI(UnityModManager.ModEntry modEntry)
		{
			Main.Settings.RollYourself = GUILayout.Toggle(Main.Settings.RollYourself, " Roll the dice yourself", new GUILayoutOption[0]);
			Main.Settings.ShowHiddenChecks = GUILayout.Toggle(Main.Settings.ShowHiddenChecks, " Show hidden checks", new GUILayoutOption[0]);
			Main.Settings.Sounds = GUILayout.Toggle(Main.Settings.Sounds, " Dice sounds", new GUILayoutOption[0]);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("Volume", new GUILayoutOption[] { GUILayout.Width(100f) });
			Main.Settings.Volume = GUILayout.HorizontalSlider(Main.Settings.Volume, 0f, 1f, new GUILayoutOption[] { GUILayout.Width(200f) });
			GUILayout.Label(Mathf.RoundToInt(Main.Settings.Volume * 100f) + " %", new GUILayoutOption[] { GUILayout.Width(60f) });
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("Sounds: " + RollPopup.SoundSummary, new GUILayoutOption[0]);
			if (GUILayout.Button("Reload", new GUILayoutOption[] { GUILayout.Width(120f) }))
			{
				RollPopup.ReloadSounds();
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label("Preview dice:", new GUILayoutOption[] { GUILayout.Width(120f) });
			foreach (string[] preview in Main.s_Previews)
			{
				if (GUILayout.Button(preview[1], new GUILayoutOption[] { GUILayout.Width(110f) }))
				{
					RollPopup.PreviewSound(preview[0]);
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.Label("Outcome sounds:", new GUILayoutOption[0]);
			foreach (string[] outcome in Main.s_OutcomeLabels)
			{
				Main.OutcomeRow(outcome[0], outcome[1]);
			}
			Main.Settings.UseTray = GUILayout.Toggle(Main.Settings.UseTray, " Dice tray instead of ring", new GUILayoutOption[0]);
			if (GUILayout.Button("Preview roll window (close this menu first)", new GUILayoutOption[] { GUILayout.Width(360f) }))
			{
				RollPopup.PreviewCheck();
			}
			GUILayout.Space(10f);
			Main.Settings.InteractiveMapChecks = GUILayout.Toggle(Main.Settings.InteractiveMapChecks, " Roll climbing, jumping and similar checks yourself", new GUILayoutOption[0]);
			Main.Settings.InteractiveLocks = GUILayout.Toggle(Main.Settings.InteractiveLocks, " Roll lock picking yourself", new GUILayoutOption[0]);
			Main.LookGUI();
		}

		private class ColorPreset
		{
			public ColorPreset(string name, float r, float g, float b)
			{
				this.Name = name;
				this.R = r;
				this.G = g;
				this.B = b;
			}

			public string Name;

			public float R;

			public float G;

			public float B;
		}

		private static readonly ColorPreset[] s_DiePresets = new ColorPreset[]
		{
			new ColorPreset("Silver", 0.50f, 0.52f, 0.58f),
			new ColorPreset("Crimson", 0.62f, 0.07f, 0.09f),
			new ColorPreset("Ivory", 0.86f, 0.82f, 0.70f),
			new ColorPreset("Obsidian", 0.07f, 0.07f, 0.09f),
			new ColorPreset("Royal blue", 0.12f, 0.20f, 0.66f),
			new ColorPreset("Emerald", 0.06f, 0.45f, 0.22f),
			new ColorPreset("Amethyst", 0.42f, 0.16f, 0.62f),
			new ColorPreset("Gold", 0.78f, 0.58f, 0.12f)
		};

		private static readonly ColorPreset[] s_DigitPresets = new ColorPreset[]
		{
			new ColorPreset("Gold", 1f, 0.86f, 0.5f),
			new ColorPreset("White", 1f, 1f, 1f),
			new ColorPreset("Black", 0.05f, 0.05f, 0.05f),
			new ColorPreset("Silver", 0.8f, 0.82f, 0.88f),
			new ColorPreset("Cyan", 0.45f, 0.9f, 1f)
		};

		private static readonly ColorPreset[] s_FeltPresets = new ColorPreset[]
		{
			new ColorPreset("Green", 0.09f, 0.22f, 0.16f),
			new ColorPreset("Blue", 0.08f, 0.14f, 0.28f),
			new ColorPreset("Burgundy", 0.26f, 0.07f, 0.10f),
			new ColorPreset("Charcoal", 0.12f, 0.12f, 0.13f)
		};

		private static float SliderRow(string label, float value, float min, float max, string format, string suffix)
		{
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Label(label, new GUILayoutOption[] { GUILayout.Width(170f) });
			value = GUILayout.HorizontalSlider(value, min, max, new GUILayoutOption[] { GUILayout.Width(220f) });
			GUILayout.Label(value.ToString(format) + suffix, new GUILayoutOption[] { GUILayout.Width(80f) });
			GUILayout.EndHorizontal();
			return value;
		}

		private static float ChannelSlider(string label, float value)
		{
			GUILayout.Label(label, new GUILayoutOption[] { GUILayout.Width(18f) });
			return GUILayout.HorizontalSlider(value, 0f, 1f, new GUILayoutOption[] { GUILayout.Width(110f) });
		}

		private static void ColorRow(string label, ColorPreset[] presets, ref float r, ref float g, ref float b)
		{
			GUILayout.Label(label, new GUILayoutOption[0]);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			for (int i = 0; i < presets.Length; i++)
			{
				if (GUILayout.Button(presets[i].Name, new GUILayoutOption[] { GUILayout.Width(95f) }))
				{
					r = presets[i].R;
					g = presets[i].G;
					b = presets[i].B;
				}
			}
			Rect swatch = GUILayoutUtility.GetRect(60f, 22f, new GUILayoutOption[] { GUILayout.Width(60f) });
			Color previous = GUI.color;
			GUI.color = new Color(r, g, b, 1f);
			GUI.DrawTexture(swatch, Texture2D.whiteTexture);
			GUI.color = previous;
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			r = Main.ChannelSlider("R", r);
			g = Main.ChannelSlider("G", g);
			b = Main.ChannelSlider("B", b);
			GUILayout.EndHorizontal();
		}

		private static void LookGUI()
		{
			Settings s = Main.Settings;
			GUILayout.Space(14f);
			GUILayout.Label("Roll animation (use the preview button above to try changes):", new GUILayoutOption[0]);
			s.RollLength = Main.SliderRow("Roll length", s.RollLength, 0.4f, 3f, "0.00", " x");
			s.SpinSpeed = Main.SliderRow("Spin speed", s.SpinSpeed, 0.4f, 2.5f, "0.00", " x");
			s.ResultPause = Main.SliderRow("Pause before the result", s.ResultPause, 0f, 2f, "0.00", " s");
			GUILayout.Space(8f);
			GUILayout.Label("Window:", new GUILayoutOption[0]);
			s.Bg3Style = GUILayout.Toggle(s.Bg3Style, " Baldur's Gate 3 style roll window (single die layout)", new GUILayoutOption[0]);
			s.DieSize = Main.SliderRow("Die size", s.DieSize, 0.6f, 1.3f, "0.00", " x");
			s.DimAmount = Main.SliderRow("Background dimming", s.DimAmount, 0f, 0.9f, "0.00", "");
			s.PulseDie = GUILayout.Toggle(s.PulseDie, " Pulse the die while waiting for the roll", new GUILayoutOption[0]);
			s.ShowClickHint = GUILayout.Toggle(s.ShowClickHint, " Also show the \"click the die\" text", new GUILayoutOption[0]);
			s.RingGlow = GUILayout.Toggle(s.RingGlow, " Glow around the die", new GUILayoutOption[0]);
			s.Sparks = GUILayout.Toggle(s.Sparks, " Sparks on success", new GUILayoutOption[0]);
			s.ShakeOnResult = GUILayout.Toggle(s.ShakeOnResult, " Shake the die when the result appears", new GUILayoutOption[0]);
			s.ShowStamp = GUILayout.Toggle(s.ShowStamp, " Wax seal stamp on the result", new GUILayoutOption[0]);
			GUILayout.Space(8f);
			Main.ColorRow("Die color:", Main.s_DiePresets, ref s.DieR, ref s.DieG, ref s.DieB);
			GUILayout.Space(4f);
			Main.ColorRow("Number color:", Main.s_DigitPresets, ref s.DigitR, ref s.DigitG, ref s.DigitB);
			GUILayout.Space(4f);
			Main.ColorRow("Tray felt color (dice tray layout only):", Main.s_FeltPresets, ref s.FeltR, ref s.FeltG, ref s.FeltB);
			GUILayout.Space(8f);
			if (GUILayout.Button("Reset look & animation to defaults", new GUILayoutOption[] { GUILayout.Width(280f) }))
			{
				s.ResetLook();
			}
		}

		private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
		{
			Main.Settings.Save(modEntry);
		}

		public static UnityModManager.ModEntry Mod;

		public static Settings Settings;

		public static bool Enabled;

		private static readonly string[][] s_Previews = new string[][]
		{
			new string[] { "shake", "Shake" },
			new string[] { "roll", "Roll" }
		};

		private static readonly string[][] s_OutcomeLabels = new string[][]
		{
			new string[] { "success", "Success" },
			new string[] { "failure", "Failure" },
			new string[] { "critical", "Natural 20" },
			new string[] { "fumble", "Natural 1" }
		};
	}
}
