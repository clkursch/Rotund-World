

public class BPEnums
{
	public static void Patch()
	{
		//On.CicadaGraphics.DrawSprites += PG_DrawSprites; //WE ACTUALLY MIGHT NOT NEED THIS...
		//On.CicadaGraphics.InitiateSprites += CicadaGraphics_InitiateSprites;
		//On.SoundLoader.SoundData.
	}

	//public static readonly SoundID.Lizard_Voice_Pink_A = new SoundID("Lizard_Voice_Pink_A", true);
	//public static readonly SoundID.Lizard_Voice_Pink_B = new SoundID("Lizard_Voice_Pink_B", true);
	//public static readonly SoundID.Lizard_Voice_Pink_C = new SoundID("Lizard_Voice_Pink_C", true);
	//public static readonly SoundID.Lizard_Voice_Pink_D = new SoundID("Lizard_Voice_Pink_D", true);
	//public static readonly SoundID.Lizard_Voice_Pink_E = new SoundID("Lizard_Voice_Pink_E", true);


	//public static void RegisterAllEnumExtensions()
	//{
	//	BPEnums.BPSoundID.RegisterValues();
	//}

	//public static void UnregisterAllEnumExtensions()
	//{
	//	BPEnums.BPSoundID.UnregisterValues();
	//}




	public class BPSoundID
	{
		// Token: 0x06002F4C RID: 12108 RVA: 0x0035C740 File Offset: 0x0035A940
		public static void RegisterValues()
		{
			BPEnums.BPSoundID.SqueezeLoop = new SoundID("SqueezeLoop", true);
			BPEnums.BPSoundID.Pop1 = new SoundID("Pop1", true);
			BPEnums.BPSoundID.Fwump1 = new SoundID("Fwump1", true);
			BPEnums.BPSoundID.Squinch1 = new SoundID("Squinch1", true);
			BPEnums.BPSoundID.Fwump2 = new SoundID("Fwump2", true);
			
		}

		// Token: 0x06002F4D RID: 12109 RVA: 0x0035C980 File Offset: 0x0035AB80
		public static void UnregisterValues()
		{
			SoundID squeezeLoop = BPEnums.BPSoundID.SqueezeLoop;
			if (squeezeLoop != null)
			{
				squeezeLoop.Unregister();
			}
			BPEnums.BPSoundID.SqueezeLoop = null;
			SoundID pop1 = BPEnums.BPSoundID.Pop1;
			if (pop1 != null)
			{
				pop1.Unregister();
			}
			BPEnums.BPSoundID.Pop1 = null;
			SoundID fwump1 = BPEnums.BPSoundID.Fwump1;
			if (fwump1 != null)
			{
				fwump1.Unregister();
			}
			BPEnums.BPSoundID.Fwump1 = null;
			SoundID squinch1 = BPEnums.BPSoundID.Squinch1;
			if (squinch1 != null)
			{
				squinch1.Unregister();
			}
			BPEnums.BPSoundID.Squinch1 = null;
			SoundID fwump2 = BPEnums.BPSoundID.Fwump2;
			if (fwump2 != null)
			{
				fwump2.Unregister();
			}
			BPEnums.BPSoundID.Fwump2 = null;
			
		}

		// Token: 0x04003522 RID: 13602
		public static SoundID SqueezeLoop;

		// Token: 0x04003523 RID: 13603
		public static SoundID Pop1;

		// Token: 0x04003524 RID: 13604
		public static SoundID Fwump1;

		// Token: 0x04003525 RID: 13605
		public static SoundID Squinch1;

		// Token: 0x04003526 RID: 13606
		public static SoundID Fwump2;

	}


}