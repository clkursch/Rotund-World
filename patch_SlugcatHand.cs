using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace RotundWorld;
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
        if (self.GetBelly().tuching)
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
		
		/*
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


        //RESET BEFORE EACH CALCULATION IS RUN - MOVED THIS TO PLAYERUPDATE WHERE IT BELONGS
        // myPlayer.GetBelly().pushingOther = false;
		*/
		
		//REACH OUT TO FEED
		Player fedCrit = myPlayer.GetBelly().frFeed;
		if (fedCrit != null)
		{
			self.mode = Limb.Mode.HuntAbsolutePosition;
			self.huntSpeed = 20f;
			Vector2 tarLoc = patch_Player.ObjGetHeadPos(fedCrit);
			self.absoluteHuntPos = tarLoc - Custom.DirVec(myPlayer.bodyChunks[0].pos, tarLoc) * 3f;
			myPlayer.graphicsModule.BringSpritesToFront();
			RotateSpear(myPlayer);
		}
		
		
		Creature targetCrit = myPlayer.GetBelly().pushingCreature;
		//IF WE'RE CLOSE ENOUGH TO A PLAYER, REACH OUT TO PUSH THEM
		if (targetCrit != null)
		{
			//FOR PLAYERS
			if (targetCrit is Player myHelper)
			{
				//REACH OUT AND TOUCHHH
				self.mode = Limb.Mode.HuntAbsolutePosition;
				self.huntSpeed = 20f;

				//HANDS ON SHOULDERS FOR PUSHING/PULLING LINES
				if (patch_Player.ObjIsPushingOther(targetCrit) && myHelper.standing == true) // || patch_Player.IsPullingOther(myTarget))
					self.absoluteHuntPos = myHelper.bodyChunks[0].pos - Custom.DirVec(myPlayer.bodyChunks[0].pos, myHelper.bodyChunks[0].pos) * 8f; // - myPlayer.bodyChunks[0].vel;
				else
				{
					float reach = Mathf.InverseLerp(0, 8, patch_Player.GetSquishForce(myHelper)) * 3f;
					self.absoluteHuntPos = myHelper.bodyChunks[1].pos - Custom.DirVec(myPlayer.bodyChunks[0].pos, myHelper.bodyChunks[1].pos) * (8f - reach);
				}

				myPlayer.graphicsModule.BringSpritesToFront();
				RotateSpear(myPlayer);
				//UHH AND THEN RETURN BECAUSE THE ORIGINAL BREAKS AFTER IT RUNS. 
				return false;
			}
			
			//ALL OTHER CREATURES!
			else
			{
				self.mode = Limb.Mode.HuntAbsolutePosition;
				int reachChunk = patch_Player.ObjGetBodyChunkID(targetCrit, "rear");
				self.absoluteHuntPos = targetCrit.bodyChunks[reachChunk].pos - Custom.DirVec(myPlayer.bodyChunks[0].pos, targetCrit.bodyChunks[reachChunk].pos) * 8f;
				self.huntSpeed = 20f;
				myPlayer.graphicsModule.BringSpritesToFront();
				RotateSpear(myPlayer);
				return false;
			}
		}
		

		


		//IF WE'RE BEING PULLED BY ANOTHER PLAYER, REACH OUT TO GRAB THEIR HANDS, SORTA
		else if (patch_Player.IsStuck(myPlayer) && (myPlayer.grabbedBy.Count > 0 && myPlayer.grabbedBy[0].grabber is Player))
        {
			if (targetCrit != null)
            {
				self.mode = Limb.Mode.HuntAbsolutePosition;
				self.absoluteHuntPos = targetCrit.bodyChunks[0].pos;

				//UHH AND THEN RETURN BECAUSE THE ORIGINAL BREAKS AFTER IT RUNS. 
				return false;
			}
		}
		
		
		//ROLLING OUR PARTNER ALONG
		if (myPlayer.GetBelly().rollingOther > 0)
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
					myHelper.GetBelly().tuchShift = armDir * 2f;

                    patch_Player.ObjFeatherHeat(myPlayer, (1 + (UnityEngine.Random.value < 0.5f ? 1 : 0)) * 2, 800);
                    if (armDir != new Vector2(0, 0))
                    {
                        patch_Player.ObjFeatherHeat(myHelper, 4, 800);
                    }

                    if ((self.owner as PlayerGraphics).blink <= 0 && UnityEngine.Random.value < 0.0125)
                        myPlayer.Blink(UnityEngine.Random.Range(40, 80));

                    myPlayer.GetBelly().tuching = true;
                    //UHH AND THEN RETURN BECAUSE THE ORIGINAL BREAKS AFTER IT RUNS. 
                    return false;
                }
            }
            else
            {
                myPlayer.GetBelly().tuching = false;
            }
        }
		

		//RUN THE ORIGINAL, I GUESS
		return orig.Invoke(self);
	}
}
