using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;



public class patch_Vulture
{
	public static void Patch()
	{
		On.Vulture.ctor += BP_VulturePatch;
        // On.Vulture.Update += BPVulture_Update;
        //On.Vulture.VultureThruster.Update += BPThruster_Update;

        On.VultureGraphics.DrawSprites += BPVultureGraphics_DrawSprites;
        On.VultureGraphics.InitiateSprites += BPVultureGraphics_InitiateSprites;
        On.VultureTentacle.TentacleContour += BPVultureTentacle_TentacleContour;
    }

    public static Dictionary<int, Vulture> vultureBook = new Dictionary<int, Vulture>(0);

    private static void BP_VulturePatch(On.Vulture.orig_ctor orig, Vulture self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        int critNum = self.abstractCreature.ID.RandomSeed;
        bool mouseExists = false;
        try
        {
            patch_Vulture.vultureBook.Add(critNum, self); //ADD OURSELVES TO THE GUESTBOOK
        }
        catch (ArgumentException)
        {
            mouseExists = true;
        }

        if (mouseExists)
        {
            //Debug.Log("CREATURE ALREADY EXISTS! CANCELING: " + critNum);
            patch_Vulture.vultureBook[critNum] = self;
            UpdateBellySize(self);
            return;
        }

        BellyPlus.InitializeCreature(critNum);

        //NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
        int seed = UnityEngine.Random.seed;
        UnityEngine.Random.seed = critNum;

        int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));
		if (patch_DLL.CheckFattable(self) == false)
			critChub = 0;
		
        BellyPlus.myFoodInStomach[critNum] = critChub;
		if (BPOptions.debugLogs.Value)
			Debug.Log("CREATURE SPAWNED! CHUB SIZE: " + critChub);

        UpdateBellySize(self);

        if (BellyPlus.parasiticEnabled)
            BellyPlus.InitPSFoodValues(abstractCreature);
    }


    public static int GetRef(Vulture self)
    {
        return self.abstractCreature.ID.RandomSeed;
    }





    public static void UpdateBellySize(Vulture self)
    {
        float baseWeight = self.IsMiros ? 1.8f : (1.2f * (self.IsKing ? 1.4f : 1f));
        float baseRad = 9.5f;
        float baseGrav = 0.9f;
        int currentFood = BellyPlus.myFoodInStomach[GetRef(self)];

        switch (Math.Min(currentFood, 8))
        {
            case 8:
                baseWeight *= 1.2f;
                baseRad *= 1.3f;
                baseGrav *= 1.3f;
                break;
            case 7:
                baseWeight *= 1.1f;
                baseRad *= 1.2f;
                baseGrav *= 1.2f;
                break;
            case 6:
                baseWeight *= 1f;
                baseRad *= 1.1f;
                baseGrav *= 1.05f;
                break;
            case 5:
                baseWeight *= 1f;
                baseRad *= 1.0f;
                break;
            case 4:
            default:
                baseWeight *= 1f;
                baseRad *= 1f;
                break;
        }
		
		if (!BellyPlus.VisualsOnly())
		{
			self.SetLocalGravity(baseGrav);

			for (int i = 0; i < 4; i++)
			{
				self.bodyChunks[i].mass = baseWeight;
				self.bodyChunks[i].rad = baseRad;
			}
		}
        
        //NECK FATNESS
        if (currentFood >= 7)
        {
            float extraNeck = (currentFood == 7) ? 1.5f : 2.1f;
            float baseNeckRad = self.IsKing ? 6f : 5f;
            for (int k = 0; k < self.neck.tChunks.Length; k++)
            {
                //self.neck.tChunks[k] = new Tentacle.TentacleChunk(self.neck, k, (float)(k + 1) / (float)self.neck.tChunks.Length, self.IsKing ? 6f : 5f);
                self.neck.tChunks[k].rad = baseNeckRad * extraNeck;
            }
        }

        patch_Lizard.UpdateChubValue(self);
    }



    private static float BPVultureTentacle_TentacleContour(On.VultureTentacle.orig_TentacleContour orig, VultureTentacle self, float x)
    {
		float extra = 0f;
		if (patch_Lizard.GetChubValue(self.vulture) >= 4)
		{
			extra = 3f;
		}
		return orig(self, x) + extra; 
    }

    private static void BPVultureGraphics_InitiateSprites(On.VultureGraphics.orig_InitiateSprites orig, VultureGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);

        //EXTRA ROTUND SPRITE
        float myChub = patch_Lizard.GetChubValue(self.vulture);
		if (myChub >= 4) 
		{
            sLeaser.sprites[self.BodySprite].element = Futile.atlasManager.GetElementWithName("Cicada8body");
			//sLeaser.sprites[self.BodySprite].scale = (self.IsKing ? 2.6f : 2f);
			float extra = 1f + patch_Lizard.GetOverstuffed(self.vulture) / 12f; //8f
            sLeaser.sprites[self.BodySprite].scaleX = (self.IsKing ? 6 : 5) * extra;
            sLeaser.sprites[self.BodySprite].scaleY = (self.IsKing ? 2.6f : 2.2f) * extra;
            //Debug.Log("VULTURE GRAPHICS INITIATE! ");
        }
    }

    
	

    private static void BPVultureGraphics_DrawSprites(On.VultureGraphics.orig_DrawSprites orig, VultureGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
		
		float myChub = patch_Lizard.GetChubValue(self.vulture);
        if (myChub == 3)
		{
            sLeaser.sprites[self.HeadSprite].scaleX *= 1.3f;
            sLeaser.sprites[self.HeadSprite].scaleY *= 1.05f;
        }
		else if (myChub >= 4)
		{
            sLeaser.sprites[self.HeadSprite].scaleX *= 1.65f;
            sLeaser.sprites[self.HeadSprite].scaleY *= 1.1f;
        }
    }




	/*
    public static void BPVulture_Update(On.Vulture.orig_Update orig, Vulture self, bool eu)
    {
		orig.Invoke(self, eu);

        float myChub = patch_Lizard.GetChubValue(self);
		if (myChub > 1)
        {
			if (myChub == 2f)
				myChub = 0.2f;
			else if (myChub == 3f)
				myChub = 0.3f;
			else
				myChub = 0.5f;

			for (int i = 0; i < 4; i++)
			{
				//JUST THE INVERSE OF THE NORMAL 
				//self.vulture.bodyChunks[i].vel += ((self.ThrustVector * (self.vulture.IsKing ? 1.2f : 0.8f) * self.Force) / 5f) * Math.Max(patch_Lizard.GetChubValue(self.vulture), 0);
				if (self.bodyChunks[i].vel.y > 4f)
					self.bodyChunks[i].vel.y -= myChub;
			}
		}
		
	}


    public static void BPThruster_Update(On.Vulture.VultureThruster.orig_Update orig, Vulture.VultureThruster self, bool eu)
	{
		//int critNum = self.abstractCreature.ID.RandomSeed;
		orig.Invoke(self, eu);
		
		if (self.Active)
		{
			for (int i = 0; i < 4; i++)
			{
				//JUST THE INVERSE OF THE NORMAL 
				//self.vulture.bodyChunks[i].vel += ((self.ThrustVector * (self.vulture.IsKing ? 1.2f : 0.8f) * self.Force) / 5f) * Math.Max(patch_Lizard.GetChubValue(self.vulture), 0);
				if (patch_Lizard.GetChubValue(self.vulture) > 1 && self.vulture.bodyChunks[i].vel.y > 1f)
					self.vulture.bodyChunks[i].vel /= patch_Lizard.GetChubValue(self.vulture);
			}
		}
	}
	*/
}