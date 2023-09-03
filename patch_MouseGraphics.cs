using RWCustom;
using UnityEngine;

public class patch_MouseGraphics
{
    public static void Patch()
    {
        On.MouseGraphics.DrawSprites += PG_DrawSprites;
        //On.MouseGraphics.InitiateSprites += PG_InitiateSprites;
    }

    static int bl = 20;

    public static void PG_DrawSprites(On.MouseGraphics.orig_DrawSprites orig, MouseGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (self.mouse.room == null)
            return;

        int myMouse = patch_LanternMouse.GetRef(self.mouse);

        //DEAL WITH THIS IN HERE, SO IT DOESN'T RUN OFF SCREEN.
        self.mouse.bodyChunkConnections[0].distance = 12f + (4 + patch_Lizard.GetChubValue(self.mouse) / 4);

        //STRETCH OUT BASED ON STRAIN!
		float bodyStretch = Mathf.Min(BellyPlus.boostStrain[myMouse], 15f) * 0.7f;
		if (BellyPlus.beingPushed[myMouse] > 0 || (patch_LanternMouse.IsVerticalStuck(self.mouse) && patch_Lizard.GetYFlipDirection(self.mouse) > 0))
			bodyStretch *= 0.6f;
		self.mouse.bodyChunkConnections[0].distance += Mathf.Sqrt(bodyStretch);
		

        //RESET
        BellyPlus.pushingOther[myMouse] = false;
		
		//STOLEN FROM SLUGCAT HANDS
		LanternMouse myHelper = patch_LanternMouse.FindMouseInRange(self.mouse);
		if (myHelper != null && !BellyPlus.VisualsOnly())
			if (patch_Player.IsStuckOrWedged(myHelper) || patch_Player.ObjIsPushingOther(myHelper))
			{
                //WAIT... WHAT ABOUT THIS. OK THAT'S EASIER
                for (int l = 0; l < 2; l++)
                {
                    //self.limbs[0, l].pos = patch_Player.GetCreatureVector(self.mouse).ToVector2(); //BAD
                    self.limbs[0, l].pos += (myHelper.bodyChunks[1].pos - self.mouse.bodyChunks[0].pos) / 2;
                    //ISN'T THIS BACKWARDS? BUT OK
                    sLeaser.sprites[self.LimbSprite(0, l)].rotation = Custom.AimFromOneVectorToAnother( myHelper.bodyChunks[1].pos, self.mouse.bodyChunks[0].pos);
                }

                bool vertStuck = patch_LanternMouse.IsVerticalStuck(myHelper);
				if (!vertStuck && patch_LanternMouse.GetMouseAngle(self.mouse).x == patch_LanternMouse.GetMouseAngle(myHelper).x
					|| (vertStuck && patch_LanternMouse.GetMouseAngle(self.mouse).y == patch_LanternMouse.GetMouseAngle(myHelper).y))
				{
					//Debug.Log("-----MY PLAYER!?: " + findPlayerInRange(self.mouse).abstractCreature.ID.RandomSeed);
					patch_Player.ObjPushedOn(myHelper);
					patch_Lizard.PushedOther(self.mouse);
				}
			}
		
		
		//IF THEY'RE STUCK, MAYBE DON'T SHOW THEIR FORELEGS?
		if (patch_LanternMouse.IsStuck(self.mouse))
		{
			// sLeaser.sprites[this.LimbSprite(l, m)] //L:0 = FRONT LEGS. L:1 = HIND LEGS
			//sLeaser.sprites[self.LimbSprite(0, 0)].isVisible = false; 
			//sLeaser.sprites[self.LimbSprite(0, 1)].isVisible = false;
		}

        float hipScale = 0;
        switch (patch_Lizard.GetChubValue(self.mouse))
        {
            case 0:
                hipScale = 0f;
                break;
            case 1:
                hipScale = 0f; //2
                break;
            case 2:
                hipScale = 2f; //5
                break;
            case 3:
                hipScale = 5f; //8
                break;
            case 4:
                hipScale = 10f;
                break;
        }

        //OK, WE MIGHT NEED TO CHEAT A BIT WITH THE MICE...
        float stuckBonus = 1;
        if (patch_LanternMouse.IsStuck(self.mouse))
        {
            stuckBonus = 1.8f;
        }

        sLeaser.sprites[self.BodySprite(0)].scaleX = 1 + 0.03f * stuckBonus * hipScale;
        // sLeaser.sprites[self.BodySprite(1)].scaleX = 1 + 0.05f * stuckBonus * hipScale;
        // sLeaser.sprites[self.BodySprite(1)].scaleY = 1 + 0.02f  * hipScale;
        // sLeaser.sprites[self.HeadSprite].scaleX = 1 + 0.01f * hipScale; 
		//THESE REFRESH EVERY FRAME, SO JUST ADD TO THEM
		sLeaser.sprites[self.BodySprite(1)].scaleX += 0.065f * stuckBonus * hipScale;
        sLeaser.sprites[self.BodySprite(1)].scaleY += 0.04f  * hipScale;
		sLeaser.sprites[self.HeadSprite].scaleX += 0.015f * hipScale; 
        //Debug.Log("----SQUEAK!: " + sLeaser.sprites[self.BodySprite(1)].scaleX);


        float heatVal = 0;
        float breathNum = 0;
        
        if (BellyPlus.wideEyes[myMouse] > 0) //|| (heatVal >= 1)
        {
			for (int num = 15; num < 19; num++) //ONLY SCALE A/B EYES, I THINK
			{
				sLeaser.sprites[num].scaleY = 1.5f; //THEY GET SCALED ON THEIR NORMAL RUN
			}
            self.mouse.bodyChunks[0].vel.y = 8f;
            self.ouchEyes = 0;
			//self.blink = 5;
        }
		else if (BellyPlus.lungsExhausted[myMouse])
		{
            //self.blink = -15; //WE'RE EXHAUSTED! EYES CLOSED...
            self.ouchEyes = 10; //THAT'S THE WRONG EXPRESSION
        }
		
        else if (((patch_LanternMouse.IsStuck(self.mouse)) || patch_Player.ObjIsPushingOther(self.mouse) || patch_Player.ObjIsPullingOther(self.mouse))) // && !self.mouse.lungsExhausted)
        {
            //THIS IS ALL WE NEED
            self.ouchEyes = 10;
            if (!patch_LanternMouse.IsVerticalStuck(self.mouse))
            {
                for (int num = 15; num < 19; num++)
                {
                    sLeaser.sprites[num].rotation += 180; //FLIP THE EYES!
                }
            }
        }

    }

}