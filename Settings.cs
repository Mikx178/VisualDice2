using System;
using UnityEngine;
using UnityModManagerNet;

namespace DialogDice
{
	public class Settings : UnityModManager.ModSettings
	{
		public string ChoiceFor(string outcome)
		{
			if (outcome != null)
			{
				if (outcome == "success")
				{
					return this.SoundSuccess;
				}
				if (outcome == "failure")
				{
					return this.SoundFailure;
				}
				if (outcome == "critical")
				{
					return this.SoundCritical;
				}
				if (outcome == "fumble")
				{
					return this.SoundFumble;
				}
			}
			return "";
		}

		public void SetChoice(string outcome, string value)
		{
			if (outcome != null)
			{
				if (outcome == "success")
				{
					this.SoundSuccess = value;
					return;
				}
				if (outcome == "failure")
				{
					this.SoundFailure = value;
					return;
				}
				if (outcome == "critical")
				{
					this.SoundCritical = value;
					return;
				}
				if (!(outcome == "fumble"))
				{
					return;
				}
				this.SoundFumble = value;
			}
		}

		public override void Save(UnityModManager.ModEntry modEntry)
		{
			UnityModManager.ModSettings.Save<Settings>(this, modEntry);
		}

		public bool RollYourself = true;

		public bool ShowHiddenChecks;

		public bool Sounds = true;

		public float Volume = 0.8f;

		public bool UseTray;

		public string SoundSuccess = "";

		public string SoundFailure = "";

		public string SoundCritical = "";

		public string SoundFumble = "";

		public bool InteractiveMapChecks = true;

		public bool InteractiveLocks = true;

		// ---- look & feel
		public float RollLength = 1f;
		public float SpinSpeed = 1f;
		public float ResultPause = 0.8f;
		public float DieSize = 1f;
		public float DimAmount = 0.55f;
		public bool Bg3Style = true;
		public float DieR = 0.50f;
		public float DieG = 0.52f;
		public float DieB = 0.58f;
		public float DigitR = 0.97f;
		public float DigitG = 0.95f;
		public float DigitB = 0.90f;
		public float FeltR = 0.09f;
		public float FeltG = 0.22f;
		public float FeltB = 0.16f;
		public bool PulseDie = true;
		public bool ShowClickHint;
		public bool Sparks = true;
		public bool ShakeOnResult = true;
		public bool RingGlow = true;
		public bool ShowStamp = true;

		public Color GetDieColor()
		{
			return new Color(this.DieR, this.DieG, this.DieB, 1f);
		}

		public Color GetDigitColor()
		{
			return new Color(this.DigitR, this.DigitG, this.DigitB, 1f);
		}

		public Color GetFeltColor()
		{
			return new Color(this.FeltR, this.FeltG, this.FeltB, 1f);
		}

		public void ResetLook()
		{
			Settings d = new Settings();
			this.RollLength = d.RollLength;
			this.SpinSpeed = d.SpinSpeed;
			this.ResultPause = d.ResultPause;
			this.DieSize = d.DieSize;
			this.DimAmount = d.DimAmount;
			this.DieR = d.DieR;
			this.DieG = d.DieG;
			this.DieB = d.DieB;
			this.DigitR = d.DigitR;
			this.DigitG = d.DigitG;
			this.DigitB = d.DigitB;
			this.FeltR = d.FeltR;
			this.FeltG = d.FeltG;
			this.FeltB = d.FeltB;
			this.Bg3Style = d.Bg3Style;
			this.PulseDie = d.PulseDie;
			this.ShowClickHint = d.ShowClickHint;
			this.Sparks = d.Sparks;
			this.ShakeOnResult = d.ShakeOnResult;
			this.RingGlow = d.RingGlow;
			this.ShowStamp = d.ShowStamp;
		}
	}
}
