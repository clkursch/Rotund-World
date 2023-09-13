using MoreSlugcats;
using RWCustom;
using UnityEngine;

public class patch_SlugcatHand
{
    public static void Patch()
    {
        On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;
        //On.Player.JollyPointUpdate += Player_JollyPointUpdate;
        On.Player.JollyPointUpdate += Player_JollyPointUpdate;
    }

    private static void Player_JollyPointUpdate(On.Player.orig_JollyPointUpdate orig, Player self)
    {
        //-- Not actually pointing, just reusing the input, so it is fair to not call orig in this case
        if (patch_Player.bellyStats[patch_Player.GetPlayerNum(self)].tuching)
            return;
        orig(self);
    }
	
	
	public static void RotateSpear(Player player)
	{
        //SO IT DOESN'T LOOK LIKE WE'RE JABBING OUR FRIENDS WHEN WE PUSH THEM
		(player.graphicsModule as PlayerGraphics).spearDir = 0;
	}

    //public static IntVector2 SlugcatHand_SlugcatFoodMeter(On.SlugcatHand.orig_SlugcatFoodMeter orig, SlugcatHand self, int slugcatNum)
    public static bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, SlugcatHand self)
    {
		if (BellyPlus.VisualsOnly())
		{
			return orig.Invoke(self);
		}

		Player myPlayer = self.owner.owner as Player;

		//OKAY NO DON'T DO THIS IF WE'RE PIGGYBACKED. THAT MAKES WEIRD THINGS HAPPEN
		if (patch_Player.IsPiggyBacked(myPlayer))
			return orig.Invoke(self);

		Player myTarget = patch_Player.FindPlayerInRange(myPlayer);
		Lizard lizHelper = patch_Player.FindLizardInRange(myPlayer, 0, 2);
		Creature critHelper = patch_LanternMouse.FindMouseInRange(myPlayer);
		if (critHelper == null)
			critHelper = patch_Cicada.FindCicadaInRange(myPlayer);
		if (critHelper == null)
			critHelper = patch_Yeek.FindYeekInRange(myPlayer);
		if (critHelper == null && myPlayer.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer)
			critHelper = patch_Scavenger.FindScavInRange(myPlayer);

		//DON'T HELP LIZARDS THAT ARE TRYING TO EAT YOUR FRIENDS
        if (lizHelper != null)
        {
            //NOT VALID IF THE LIZARD IS CARRYING OUR FRIEND
            if (lizHelper.grasps[0] != null && lizHelper.grasps[0].grabbed is Player)
                lizHelper = null;
        }


        //RESET BEFORE EACH CALCULATION IS RUN
        patch_Player.bellyStats[patch_Player.GetPlayerNum(myPlayer)].pushingOther = false;


		//IF WE'RE CLOSE ENOUGH TO A PLAYER, REACH OUT TO PUSH THEM

		if (myTarget != null
			&& (patch_Player.IsStuckOrWedged(myTarget) || patch_Player.IsPushingOther(myTarget)) //|| (patch_Player.IsPullingOther(myTarget) && !patch_Player.IsStuckOrWedged(myPlayer))
			&& !patch_Player.IsGraspingSlugcat(myPlayer)
			&& (myPlayer.corridorTurnDir == null) //SO WE DON'T DO THAT WEIRD BUG WHERE WE FLING THEM FORWARD WHILE FLIPPING
		)
		{
			Player myHelper = myTarget;
			//REACH OUT AND TOUCHHH
			//AT SOME POINT WE'LL WANT TO MAKE IT SO THAT THIS ONLY APPLIES WHEN HOLDING TOWARDS THE PLAYER (and on y axis too)

			self.mode = Limb.Mode.HuntAbsolutePosition;
			//self.absoluteHuntPos = myHelper.bodyChunks[1].pos;
			self.huntSpeed = 20f;
			//self.quickness = 5f;

			//HANDS ON SHOULDERS FOR PUSHING/PULLING LINES
			if (patch_Player.IsPushingOther(myTarget) && myHelper.standing == true) // || patch_Player.IsPullingOther(myTarget))
				self.absoluteHuntPos = myHelper.bodyChunks[0].pos - Custom.DirVec(myPlayer.bodyChunks[0].pos, myHelper.bodyChunks[0].pos) * 8f; // - myPlayer.bodyChunks[0].vel;
			else
			{
				float reach = Mathf.InverseLerp(0, 8, patch_Player.GetSquishForce(myHelper)) * 3f;
                self.absoluteHuntPos = myHelper.bodyChunks[1].pos - Custom.DirVec(myPlayer.bodyChunks[0].pos, myHelper.bodyChunks[1].pos) * (8f - reach);
            }

            //LETS TRY A DIFFERENT METHOD
            //Vector2 pos = self.connection.pos;
            //Vector2 pos2 = self.connection.pos;
            //float maximumRadiusFromAttachedPos = 150f;
            //self.FindGrip(myPlayer.room, pos, pos2, maximumRadiusFromAttachedPos, myHelper.bodyChunks[1].pos - myPlayer.bodyChunks[0].vel, -1, -1, true);
            myPlayer.graphicsModule.BringSpritesToFront();

			bool vertStuck = patch_Player.IsVerticalStuck(myHelper);
			RotateSpear(myPlayer);

			//NVMM I FOUND IT...
			if (!vertStuck && myPlayer.input[0].x == patch_Player.ObjGetXFlipDirection(myHelper) //myHelper.flipDirection
				|| (vertStuck && myPlayer.input[0].y == patch_Player.GetYFlipDirection(myHelper))
				|| myPlayer.simulateHoldJumpButton > 0 || myPlayer.isNPC)
            {
				//Debug.Log("-----PUSHING PLAYER!: " + vertStuck); //+ myPlayer.input[0].y + "_" + patch_Player.GetYFlipDirection(myHelper));
                patch_Player.PushedOn(myHelper);
				patch_Player.PushedOther(myPlayer);
            }

            //UHH AND THEN RETURN BECAUSE THE ORIGINAL BREAKS AFTER IT RUNS. 
            return false;
		}
		
		
		
		//HELP SOME MOUSEYS!
		else if (critHelper != null
			&& (patch_Player.IsStuckOrWedged(critHelper) || patch_Player.ObjIsPushingOther(critHelper))
		)
		{
			Creature myHelper = critHelper;
			self.mode = Limb.Mode.HuntAbsolutePosition;
			int reachChunk = patch_Player.ObjGetBodyChunkID(myHelper, "rear");
			self.absoluteHuntPos = myHelper.bodyChunks[reachChunk].pos - Custom.DirVec(myPlayer.bodyChunks[0].pos, myHelper.bodyChunks[reachChunk].pos) * 8f;
			self.huntSpeed = 20f;
            myPlayer.graphicsModule.BringSpritesToFront();
			bool vertStuck = patch_Lizard.IsVerticalStuck(myHelper);
			RotateSpear(myPlayer);

			if (!vertStuck && myPlayer.input[0].x == patch_Lizard.GetXFlipDirection(myHelper)
				|| (vertStuck && myPlayer.input[0].y == patch_Lizard.GetYFlipDirection(myHelper))
				|| myPlayer.simulateHoldJumpButton > 0)
            {
                //Debug.Log("-----MY PLAYER!?: " + patch_LanternMouse.findPlayerInRange(myPlayer).playerState.playerNumber);
				patch_Player.ObjPushedOn(myHelper);
				patch_Player.PushedOther(myPlayer);
            }
            return false;
		}

		//AND SOME LIZARS!
		else if (lizHelper != null
			&& (patch_Player.IsStuckOrWedged(lizHelper) || patch_Lizard.IsPushingOther(lizHelper))
		)
		{
			
			self.mode = Limb.Mode.HuntAbsolutePosition;
			self.absoluteHuntPos = lizHelper.bodyChunks[2].pos - Custom.DirVec(myPlayer.bodyChunks[0].pos, lizHelper.bodyChunks[2].pos) * 8f; // - myPlayer.bodyChunks[0].vel;
			self.huntSpeed = 20f;
			myPlayer.graphicsModule.BringSpritesToFront();
			bool vertStuck = patch_Lizard.IsVerticalStuck(lizHelper);
			RotateSpear(myPlayer);

			if (!vertStuck && myPlayer.input[0].x == patch_Lizard.GetXFlipDirection(lizHelper)
				|| (vertStuck && myPlayer.input[0].y == patch_Lizard.GetYFlipDirection(lizHelper))
				|| myPlayer.simulateHoldJumpButton > 0)
			{
				//Debug.Log("-----MY PLAYER!?: " + patch_LanternMouse.findPlayerInRange(myPlayer).playerState.playerNumber);
				patch_Lizard.PushedOn(lizHelper);
				patch_Player.PushedOther(myPlayer);
			}
			return false;
		}


		//IF WE'RE BEING PULLED BY ANOTHER PLAYER, REACH OUT TO GRAB THEIR HANDS, SORTA
		else if (patch_Player.IsStuck(myPlayer) && (myPlayer.grabbedBy.Count > 0 && myPlayer.grabbedBy[0].grabber is Player))
        {
			Player myHelper = myTarget;
			if (myHelper != null)
            {
				self.mode = Limb.Mode.HuntAbsolutePosition;
				self.absoluteHuntPos = myHelper.bodyChunks[0].pos;

				//UHH AND THEN RETURN BECAUSE THE ORIGINAL BREAKS AFTER IT RUNS. 
				return false;
			}
		}
		
		
		//ROLLING OUR PARTNER ALONG
		if (patch_Player.bellyStats[patch_Player.GetPlayerNum(myPlayer)].rollingOther > 0)
		{
			self.mode = Limb.Mode.HuntAbsolutePosition;
			self.huntSpeed = 10f;
			return false;
		}
		
		
		//RUBS
        if (myPlayer.jollyButtonDown && BPOptions.blushEnabled.Value)
        {
            Player myHelper = patch_Player.FindPlayerInRange(myPlayer);
            if (myHelper != null && patch_Player.GetChubValue(myHelper) > 2)
            {
                //-- Only rub with the appropriate hand
                var tuchingHand = (myPlayer.bodyChunks[0].pos - myHelper.bodyChunks[1].pos).x < 0 ? 1 : 0;
                if (self.limbNumber == tuchingHand)
                {
                    self.mode = Limb.Mode.HuntAbsolutePosition;
                    self.huntSpeed = 2f; //20f
                    Vector2 armDir = myPlayer.PointDir();
					self.absoluteHuntPos = myHelper.bodyChunks[1].pos - (Custom.DirVec(myPlayer.bodyChunks[0].pos, myHelper.bodyChunks[1].pos) * (myHelper.isSlugpup || myHelper.playerState.isPup ? 1.4f : 1)) + (armDir * 5f); // * (8f - reach);
					
					myPlayer.graphicsModule.BringSpritesToFront();
					patch_Player.bellyStats[patch_Player.GetPlayerNum(myHelper)].tuchShift = armDir * 2f;

                    patch_Player.ObjFeatherHeat(myPlayer, (1 + (UnityEngine.Random.value < 0.5f ? 1 : 0)) * 2, 800);
                    if (armDir != new Vector2(0, 0))
                    {
                        patch_Player.ObjFeatherHeat(myHelper, 4, 800);
                    }

                    if ((self.owner as PlayerGraphics).blink <= 0 && UnityEngine.Random.value < 0.0125)
                        myPlayer.Blink(UnityEngine.Random.Range(40, 80));

                    patch_Player.bellyStats[patch_Player.GetPlayerNum(myPlayer)].tuching = true;
                    //UHH AND THEN RETURN BECAUSE THE ORIGINAL BREAKS AFTER IT RUNS. 
                    return false;
                }
            }
            else
            {
                patch_Player.bellyStats[patch_Player.GetPlayerNum(myPlayer)].tuching = false;
            }
        }
		

		//RUN THE ORIGINAL, I GUESS
		return orig.Invoke(self);
	}
}
