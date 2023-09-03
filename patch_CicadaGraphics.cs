using UnityEngine;

public class patch_CicadaGraphics
{
    public static void Patch()
    {
        //On.CicadaGraphics.DrawSprites += PG_DrawSprites; //WE ACTUALLY MIGHT NOT NEED THIS...
        On.CicadaGraphics.InitiateSprites += CicadaGraphics_InitiateSprites;
    }

	private static void CicadaGraphics_InitiateSprites(On.CicadaGraphics.orig_InitiateSprites orig, CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		orig.Invoke(self, sLeaser, rCam);
		BP_UpdateFatness(self, sLeaser);

	}

	public static void BP_UpdateFatness(CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser)
    {
        //orig.Invoke(self, sLeaser, rCam);

		//float bodySize = Custom.ClampedRandomVariation((!self.cicada.gender) ? 0.4f : 0.6f, 0.1f, 0.5f) * 2f;

		float bodySize = self.cicada.iVars.fatness;
        switch (patch_Lizard.GetChubValue(self.cicada))
		{
			case 4:
				bodySize *= 1.4f;
				break;
			case 3:
				bodySize *= 1.3f;
				break;
			case 2:
				bodySize *= 1.1f;
				break;
			case 1:
				bodySize *= 1.0f;
				break;
			case 0:
			default:
				bodySize *= 1.0f;
				break;
		}

		sLeaser.sprites[self.BodySprite].scale = bodySize; // self.iVars.fatness;
        //sLeaser.sprites[self.BodySprite].scaleY = bodySize;
		sLeaser.sprites[self.HighlightSprite].scaleX = Mathf.Lerp(5f, 3f, Mathf.Abs(bodySize - 1f) * 10f) / 20f;
        sLeaser.sprites[self.HighlightSprite].scaleY = Mathf.Lerp(12f, 8f, Mathf.Abs(bodySize - 1f) * 10f) / 20f;
    }

	/*
    public static void PG_DrawSprites(On.CicadaGraphics.orig_DrawSprites orig, CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
		
        if (self.cicada.room == null)
            return;
		
        int myLizard = BellyPlus.GetRef(self.cicada);
		
        //IF WE'RE BEING PUSHED, SQUISH THE TAIL
        Cicada liz = self.cicada as Cicada;
        float bellyBulge = (BellyPlus.inPipeStatus[myLizard] || !BellyPlus.isStuck[myLizard]) ? 0 : Mathf.InverseLerp(0, 60, BellyPlus.boostStrain[myLizard]);
		
		// WHAT IS A LIZARDS FATNESS...
		//float lizFatness = BellyPlus.myFatness[BellyPlus.GetRef(liz)];
        //self.iVars.fatness = lizFatness * (1 + bellyBulge);
    }
	*/
}