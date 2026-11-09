using System;
using UnityEngine;

namespace DialogDice
{
	// Token: 0x02000004 RID: 4
	public struct BonusLine
	{
		// Token: 0x0600000F RID: 15 RVA: 0x00002584 File Offset: 0x00000784
		public BonusLine(string label, string description, int value, string code)
		{
			this.Label = label;
			this.Description = description;
			this.Value = value;
			this.Code = code;
			this.Icon = null;
		}

		// Token: 0x04000002 RID: 2
		public string Label;

		// Token: 0x04000003 RID: 3
		public string Description;

		// Token: 0x04000004 RID: 4
		public int Value;

		// Token: 0x04000005 RID: 5
		public Sprite Icon;

		// Token: 0x04000006 RID: 6
		public string Code;
	}
}
