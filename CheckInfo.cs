using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;

namespace DialogDice
{
	// Token: 0x02000005 RID: 5
	public class CheckInfo
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000010 RID: 16 RVA: 0x000025AA File Offset: 0x000007AA
		public int Needed
		{
			get
			{
				return this.DC - this.Bonus;
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000011 RID: 17 RVA: 0x000025B9 File Offset: 0x000007B9
		public int ChancePercent
		{
			get
			{
				return Math.Max(0, Math.Min(20, 21 - this.Needed)) * 5;
			}
		}

		// Token: 0x04000007 RID: 7
		public StatType Stat;

		// Token: 0x04000008 RID: 8
		public UnitEntityData Unit;

		// Token: 0x04000009 RID: 9
		public bool IsParty;

		// Token: 0x0400000A RID: 10
		public int DC;

		// Token: 0x0400000B RID: 11
		public int DCAdjustment;

		// Token: 0x0400000C RID: 12
		public int Bonus;

		// Token: 0x0400000D RID: 13
		public List<BonusLine> Lines;

		// Token: 0x0400000E RID: 14
		public string Context;

		// Token: 0x0400000F RID: 15
		public bool HideDC;

		// Token: 0x04000010 RID: 16
		public bool HasRoll;

		// Token: 0x04000011 RID: 17
		public int D20;

		// Token: 0x04000012 RID: 18
		public int Total;

		// Token: 0x04000013 RID: 19
		public bool Passed;
	}
}
