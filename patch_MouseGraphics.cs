using RWCustom;
using UnityEngine;

namespace RotundWorld;

public class patch_MouseGraphics
{
    public static void Patch()
    {
        On.MouseGraphics.DrawSprites += PG_DrawSprites;
        On.MouseGraphics.Update += MouseGraphics_Update;
    }

    static int bl = 20;

    private static void MouseGraphics_Update(On.MouseGraphics.orig_Update orig, MouseGraphics self)
    {
        orig(self);

        if (self.mouse.room == null)
            return;


        //DEAL WITH THIS IN HERE, SO IT DOESN'T RUN OFF SCREEN.
        //self.mouse.bodyChunkConnections[0].distance = 12f + (4 + patch_Lizard.GetChubValue(self.mouse) / 4); //EW WHY DID I BUILD IT LIKE THIS
        self.mouse.bodyChunkConnections[0].distance = 12f + Mathf.Max(0, patch_Lizard.GetChubValue(self.mouse));

        //STRETCH OUT BASED ON STRAIN!
        float bodyStretch = Mathf.Min(self.mouse.GetBelly().boostStrain, 15f) * 1.5f;
        if ((patch_LanternMouse.IsVerticalStuck(self.mouse) && patch_Player.GetYFlipDirection(self.mouse) > 0))
            bodyStretch *= 0.6f;
        self.mouse.bodyChunkConnections[0].distance += Mathf.Sqrt(bodyStretch);


        //RESET
        if (self.mouse.GetBelly().pushingOther > 0)
            self.mouse.GetBelly().pushingOther--;

        //STOLEN FROM SLUGCAT HANDS
        LanternMouse myHelper = patch_LanternMouse.FindMouseInRange(self.mouse);
        if (myHelper != null && !BellyPlus.VisualsOnly())
        {
            if (patch_Player.IsStuckOrWedged(myHelper) || patch_Player.ObjIsPushingOther(myHelper))
            {
                //SPRITE ROTATION HAS TO HAPPEN IN DRAWSPRITES

                //JUST PUSH IF CLOSE ENOUGH, NO NEED TO CHECK IF ANGLE LINES UP OR ANYTHING
                patch_Player.ObjPushedOn(myHelper);
                self.mouse.GetBelly().pushingOther = 3;
            }
        }
    }
    

    public static void PG_DrawSprites(On.MouseGraphics.orig_DrawSprites orig, MouseGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (self.mouse.GetBelly().pushingOther > 0 && self.mouse.room != null)
        {
            LanternMouse myHelper = patch_LanternMouse.FindMouseInRange(self.mouse);
            if (myHelper != null && (patch_Player.IsStuckOrWedged(myHelper) || patch_Player.ObjIsPushingOther(myHelper)))
            {
                //WAIT... WHAT ABOUT THIS. OK THAT'S EASIER
                for (int l = 0; l < 2; l++)
                {
                    self.limbs[0, l].pos += (myHelper.bodyChunks[1].pos - self.mouse.bodyChunks[0].pos) / 2;
                    //ISN'T THIS BACKWARDS? BUT OK
                    sLeaser.sprites[self.LimbSprite(0, l)].rotation = Custom.AimFromOneVectorToAnother(myHelper.bodyChunks[1].pos, self.mouse.bodyChunks[0].pos);
                }
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
                hipScale = 0f;
                break;
            case 2:
                hipScale = 2f;
                break;
            case 3:
                hipScale = 5f;
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
        
        if (false) //(BellyPlus.wideEyes[critNum] > 0) //|| (heatVal >= 1)
        {
			for (int num = 15; num < 19; num++) //ONLY SCALE A/B EYES, I THINK
			{
				sLeaser.sprites[num].scaleY = 1.5f; //THEY GET SCALED ON THEIR NORMAL RUN
			}
            self.mouse.bodyChunks[0].vel.y = 8f;
            self.ouchEyes = 0;
			//self.blink = 5;
        }
		else if (self.mouse.GetBelly().lungsExhausted)
		{
            //self.blink = -15; //WE'RE EXHAUSTED! EYES CLOSED...
            self.ouchEyes = 10; //THAT'S THE WRONG EXPRESSION
        }
		
        else if (((patch_LanternMouse.IsStuck(self.mouse)) || patch_Player.ObjIsPushingOther(self.mouse))) // && !self.mouse.lungsExhausted)
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