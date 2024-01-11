using System;
using RWCustom;
using UnityEngine;
using MonoMod.RuntimeDetour;
using Pearlcat;

public class patch_PlayerGraphics
{
    //static readonly int bl = 12;
	//CURRENTLY THE ONLY PART OF THESE GRAPHICS THAT ARE GIVEN HARD SET VALUES ARE THE FACE SPRITE.SCALEY
    //EVERYTHING ELSE IS ADDITIVE OR MULT SO SHOULD BE COMPATIBLE WITH CHANGES FROM OTHER MODS

    public struct BPgraph
    {
        public int randCycle;
        public bool staring;
		public int blSprt;
		public Color blColor;
		public int bodySprt;
        public float lastSquish;
        public float[] tailBase;
        public float checkRad; //USED TO DETECT CHANGES IN RAD
        public bool verified;
        public bool cloakRipped;
    }
    public static BPgraph[] bpGraph;
    public static int totalPlayerNum = 500;

    public static void Patch()
    {
        bpGraph = new BPgraph[totalPlayerNum];
		
		On.PlayerGraphics.PlayerObjectLooker.HowInterestingIsThisObject += POL_HowInterestingIsThisObject;
        
		//WE WANT THIS TO RUN LAST, AFTER OTHER MODS, TO CAPTURE ANYTHING THAT SETS TAIL THICKNESS
		// using (new DetourContext(-333))
		On.PlayerGraphics.ctor += BPPlayerGraphics_ctor;
		On.PlayerGraphics.DrawSprites += BP_DrawSprites;
		On.PlayerGraphics.InitiateSprites += BP_InitiateSprites;

        On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        On.PlayerGraphics.Update += PlayerGraphics_Update;
    }

    public static void BPPlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        orig.Invoke(self, ow);
        int playerNum = patch_Player.GetPlayerNum(self.player); // self.player.playerState.playerNumber;

        if (self.player.playerState.isGhost)
            return; //OH GOD DON'T LET THE GHOST SPAWNS RUN OUR TAIL INCREASE MULTIPLE TIMES

        bpGraph[playerNum] = new BPgraph
        {
            randCycle = 1,
            staring = false,
            blSprt = 12,
            bodySprt = 0,
            blColor = Color.red,
            lastSquish = 0f,
            tailBase = new float[self.tail.Length],
            verified = false,
            checkRad = 1f,
            cloakRipped = false,
        };

        for (int i = 0; i < self.tail.Length; i++)
		{
			//bpGraph[playerNum].tailBase[i] = self.bodyParts[i].rad;
			bpGraph[playerNum].tailBase[i] = self.tail[i].rad;
            if (i == 0)
                bpGraph[playerNum].checkRad = self.tail[0].rad; //REMEMBER THE SIZE OF OUR FIRST SEGMENT AS A CHECK
        }
    }
	
	//FOR MODDERS TO SET THEIR CUSTOM SLUGCAT'S COLOR
    public static void SetBloodColor(Player player, Color color) 
    {
        bpGraph[patch_Player.GetPlayerNum(player)].blColor = color;
    }

    public static void GetDMSBloodColor(Player player)
    {
        int playerNum = patch_Player.GetPlayerNum(player);
        if (DressMySlugcat.Customization.For(player).CustomSprites.Count > 0)
        {
            string SpriteSheetName = DressMySlugcat.Customization.For(player).CustomSprites[0].SpriteSheetID;
            if (SpriteSheetName == "dressmyslugcat.crypt" || SpriteSheetName == "dressmyslugcat.cryptid" || SpriteSheetName == "snowbean" || SpriteSheetName == "snowberry")
                bpGraph[playerNum].blColor = new Color(120f / 225f, 0 / 225f, 255 / 225f);
        }
        
    }


    public static float POL_HowInterestingIsThisObject(On.PlayerGraphics.PlayerObjectLooker.orig_HowInterestingIsThisObject orig, object self, PhysicalObject obj)
    {
        float interestValue = orig.Invoke((PlayerGraphics.PlayerObjectLooker)self, obj); //FIRST GRAB THE ORIGINAL
        if (obj is Creature && patch_Player.ObjIsStuckable(obj as Creature) && patch_Player.ObjIsStuck(obj as Creature))
        {
            interestValue += 0.5f;
        }
        return interestValue;
    }


    public static void BP_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
		int playerNum = patch_Player.GetPlayerNum(self.player);

        orig.Invoke(self, sLeaser, rCam);

        //DOUBLE CHECK OUR TAIL SPRITES WERENT TINKERED WITH
        if (self.tail.Length != bpGraph[playerNum].tailBase.Length)
        {
            TailBaseRefresh(self);
        }

		UpdateTailThickness(self); //UPDATE OUR TAIL THICKNESS AS WE SPAWN.

        //LET'S KEEP TRACK OF OUR BLUSH SPRITE INT. IT COULD CHANGE OR BE DIFFERENT BETWEEN SLUGCATS!
        if (BPOptions.blushEnabled.Value && !BellyPlus.VisualsOnly())
            PB_InitiateExtraFx(self, sLeaser, rCam);

        
    }


    private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        int playerNum = patch_Player.GetPlayerNum(self.player);
        int bls = bpGraph[playerNum].blSprt;

        orig(self, sLeaser, rCam, newContatiner);

        if (BPOptions.blushEnabled.Value && !BellyPlus.VisualsOnly() && sLeaser.sprites.Length > bls)
        {
            //MOVING THESE HERE BECAUSE VIGARO SAID SO
            //WE'LL ADD IT TO OUR OWN CONTAINER SO IT ISN'T IN THE FOREGROUND
            //-- Has to be in the foreground for the shader to work properly
            newContatiner = rCam.ReturnFContainer("Foreground");
            newContatiner.AddChild(sLeaser.sprites[bls]);
            //Debug.Log("BLS SPRITE");
            //sLeaser.sprites[bls].MoveToFront(); //MOVE THE FACE OVER FRONT, PLS?
            //-- Can't move over the face since we're in the wrong container
            //sLeaser.sprites[bls].MoveBehindOtherNode(sLeaser.sprites[9]);
        }
    }

    public static void PB_InitiateExtraFx(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) //, bool stealMark)
    {
        int playerNum = patch_Player.GetPlayerNum(self.player);

        Array.Resize<FSprite>(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
        int bls = sLeaser.sprites.Length - 1;
        sLeaser.sprites[bls] = new FSprite("Futile_White", true);

        bpGraph[playerNum].blSprt = bls;
        sLeaser.sprites[bls].shader = rCam.game.rainWorld.Shaders["FlatLightNoisy"];
		
		if (self.player?.slugcatStats?.name?.value == "Guide") //GUIDE GETS A UNIQUE COLOR
            bpGraph[playerNum].blColor = new Color(0.0f, 0.78f, 1f);

        if (BellyPlus.dressMySlugcatEnabled) //FOR DMS SPECIFIC ONES
            GetDMSBloodColor(self.player);

        self.AddToContainer(sLeaser, rCam, null);
    }

	//RECALCULATE OUR BASE TAIL SIZE AS IF WE WERE JUST INITIALIZING
	public static void TailBaseRefresh(PlayerGraphics self)
    {
        if (self.player.playerState.isGhost)
            return; //OH GOD DON'T LET THE GHOST SPAWNS RUN OUR TAIL INCREASE MULTIPLE TIMES

        int playerNum = patch_Player.GetPlayerNum(self.player);
        if (BPOptions.debugLogs.Value)
            Debug.Log("OUR TAIL LENGTH WAS TINKERED WITH! RECALCULATE IT ");
		bpGraph[playerNum].tailBase = new float[self.tail.Length];
		//STORE THE TAIL THICKNESS INFO!
		for (int i = 0; i < self.tail.Length; i++)
		{
			bpGraph[playerNum].tailBase[i] = self.tail[i].rad;
        }
    }
	

    public static float GetTailBonus(PlayerGraphics self)
    {
        float bonusChubVal = patch_Player.GetOverstuffed(self.player);
        if (BellyPlus.VisualsOnly())
            bonusChubVal = Math.Min(bonusChubVal, 10);

        if (bonusChubVal > 25) //STUFFING PAST THIS WILL ONLY COUNT AS A FRACTION OF IT'S NORMAL SIZE
            bonusChubVal = 25 + Mathf.Sqrt(bonusChubVal - 25); //AFTER THIS WE ADD THE SQRRT OF REMAINING
        bonusChubVal /= 10;
        return bonusChubVal;
    }


    //TAIL THICKNESS CHART BASED ON CHUB
    static readonly float[] tailThickChartA = { 1f, 1.1f, 1.2f, 1.3f, 1.4f };
    static readonly float[] tailThickChartB = { 1f, 1.35f, 1.6f, 1.7f, 1.8f }; //THIS ONE IS FOR THE GUIDE (the other applecat)
    static readonly float[] tailThickChartC = { 1f, 1.05f, 1.1f, 1.15f, 1.2f }; //REDUCED SIZE (for forager)

    public static void UpdateTailThickness(PlayerGraphics self) //, float tailThick)
    {
        if (self.player.playerState.isGhost)
            return; //OH GOD DON'T LET THE GHOST SPAWNS RUN OUR TAIL INCREASE MULTIPLE TIMES

        int playerNum = patch_Player.GetPlayerNum(self.player); // self.player.playerState.playerNumber;

        //ACTUALLY, NO NO... WE COULD TOTALLY MAKE THIS A STATIC ADDITION INSTEAD OF MULT. WE WOULDN'T WANT TO MULT REALLY FAT TAILS
        //UM, WELL... EXCEPT THEN THE TAIL TIP WILL BE ALL FUNKY... RIGHT?
        //OKAY, HOW ABOUT ON INITIALIZE, WE SET A "PREVIOUS CHUB" VALUE EQUAL TO OUR CHUB (MAYBE FLOOR 0?)
        //AND ON UPDATING TAIL SIZE, WE FIRST USE OUR PREVIOUS CHUB VALUE TO UNDO OUR PREVIOUS TAIL THICKNESS BACK TO IT'S NORMAL SIZE
        //AND THEN IMMEDIATELY UPDATE IT TO IT'S NEW SIZE GIVEN OUR NEW CHUB VALUE
        //AND THEN UPDATE "PREV CHUB VALUE" FOR NEXT TIME. YE. NICE
        //OKAY NEVERMIND I AM REALLY BAD AT MATH. WE'LL JUST GET THE TAIL SIZE ON INITIALIZING AND USE THAT AS A BASE EACH TIME.

        //BEECAT BREAKS OUR TAIL! AND HAS THEIR OWN CHUBBY TAIL THAT GROWS ANYWAYS
        if (self.player?.slugcatStats?.name?.value == "bee")
            return;
		
		
		//WE SHOULD HAVE ADDED THIS SAFTEY NET LONG AGO! MAKE SURE WE'RE NOT ABOUT TO UPDATE NON EXISTING TAIL SIZES
		if (self.tail.Length != bpGraph[playerNum].tailBase.Length)
			TailBaseRefresh(self);

        //(0-4) DON'T LET THIS BE NEGATIVE. WE DON'T HAVE NEGATIVE ARRAY VALUES
        int newChubVal = Math.Max(patch_Player.GetChubValue(self.player), 0);
        float bonusChubVal = GetTailBonus(self);// Mathf.Min(GetTailBonus(self), 0.1f);

        // Debug.Log("TAIL CHONK!): " + tailThickChart[newChubVal] + " BONUS: " + bonusChubVal);
        for (int i = 0; i < self.tail.Length; i++)
        {
			//Debug.Log("TAIL THICC!: " + self.tail.Length + " i: " + i);
            //OKAY WE HAVE A BETTER WAY TO DO THAT NOW      -    IT'S NOT APPLECAT. IT'S FOR THE GUIDE
            float tailThickMult = (self.player?.slugcatStats?.name?.value == "Guide") ? tailThickChartB[newChubVal] : tailThickChartA[newChubVal];
            if (self.player?.slugcatStats?.name?.value == "Cloudtail")
                tailThickMult = tailThickChartC[newChubVal];
            self.tail[i].rad = bpGraph[playerNum].tailBase[i] * tailThickMult + bonusChubVal;
            if (i == 0)
                bpGraph[playerNum].checkRad = self.tail[0].rad; //REMEMBER THE SIZE OF OUR FIRST SEGMENT AS A CHECK
        }
    }


    public static void CloakCheck(PlayerGraphics self)
    {
        if (Pearlcat.ModOptions.DisableCosmetics.Value)
            return;
        int playerNum = patch_Player.GetPlayerNum(self.player);
        if (self.owner.bodyChunks[1].mass > 0.55f)
        {
            if (!bpGraph[playerNum].cloakRipped && self.player.room != null)
            {
                self.owner.room.PlaySound(SoundID.Tentacle_Plant_Grab_Other, self.owner.firstChunk.pos, 1.0f, 0.5f);
                self.owner.room.PlaySound(SoundID.Seed_Cob_Open, self.owner.firstChunk.pos, 1.6f, 2.0f);
                /*
                AbstractConsumable abstractConsumable = new AbstractConsumable(self.player.room.world, AbstractPhysicalObject.AbstractObjectType.SlimeMold, null, self.player.abstractCreature.pos, self.player.room.game.GetNewID(), -1, -1, null);
                abstractConsumable.destroyOnAbstraction = true;
                self.player.room.abstractRoom.AddEntity(abstractConsumable);
                abstractConsumable.RealizeInRoom();
                SlimeMold floorCloak = (abstractConsumable.realizedObject as SlimeMold);
                floorCloak.JellyfishMode = true;
                floorCloak.firstChunk.pos += Custom.RNV() * UnityEngine.Random.value * 85f;
                floorCloak.firstChunk.vel = new Vector2(0, 2f);
                floorCloak.gravity = 0.1f;
                floorCloak.CollideWithObjects = false;
                //if (self.player.TryGetPearlcatModule(out var playerModule))
                //    floorCloak.color = playerModule.CloakColor;
                */
                Color cloakColor = default;
                if (self.player.TryGetPearlcatModule(out var playerModule))
                    cloakColor = playerModule.CloakColor;

                int num = 18;
                for (int j = 0; j < num; j++)
                {
                    self.player.room.AddObject(new PuffBallSkin(self.player.firstChunk.pos, Custom.RNV() * Mathf.Lerp(2f, 10f, UnityEngine.Random.value), cloakColor, cloakColor));
                }
                
            }

            bpGraph[playerNum].cloakRipped = true;
        }
    }

    public static void CloakDraw(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
    {
        if (Pearlcat.ModOptions.DisableCosmetics.Value)
            return;

        if (!self.player.TryGetPearlcatModule(out var playerModule)) return;

        var cloakSprite = sLeaser.sprites[playerModule.CloakSprite];
        var scarfSprite = sLeaser.sprites[playerModule.ScarfSprite];
        var sleeveLSprite = sLeaser.sprites[playerModule.SleeveLSprite];
        var sleeveRSprite = sLeaser.sprites[playerModule.SleeveRSprite];

        int playerNum = patch_Player.GetPlayerNum(self.player);
        if (bpGraph[playerNum].cloakRipped)
        {
            sLeaser.sprites[0].alpha = 1.0f;
            cloakSprite.isVisible = false;
            sleeveLSprite.isVisible = false;
            sleeveRSprite.isVisible = false;
            scarfSprite.isVisible = false;
        }
    }


    public static void BP_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        int playerNum = patch_Player.GetPlayerNum(self.player);

        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        //HOPEFULLY TO HELP COMBAT ERROR LOG ISSUES!
        if (self.player.room == null)
            return;

        //MAKE SURE TAIL SIZES HAVE NOT BEEN TINKERED WITH ALL OF A SUDDEN!
        if (self.tail.Length > 0 && bpGraph[playerNum].checkRad != self.tail[0].rad)
        {
            TailBaseRefresh(self); //RE-CALCULATE OUR BASE VALUES
            UpdateTailThickness(self); //RE-APPLY THE WEIGHT
        }
            

        //TORSO SPRITE ARRAY POSITION CAN VARY
        int ct = bpGraph[playerNum].bodySprt; //CENTER TORSO (?)
		int hp = ct + 1;	//HIP

        float hipScale = 0;
        float torsoScale = 0;
		//float limbScale = 0;
		switch (patch_Player.GetChubFloatValue(self.player))
        {
            case 0f:
            default:
                torsoScale = 0f;
				hipScale = 0f;
                break;
            case 1f:
                torsoScale = 0f;
				hipScale = 2f;
                break;
            case 2f:
                torsoScale = 1f;
				hipScale = 5f;
				break;
            // case 2.5f:
                // torsoScale = 1.5f;
                // hipScale = 5.5f;
                // break;
            case 3f:
				torsoScale = 2f;
				hipScale = 7f;
				break;
			case 3.5f:
				torsoScale = 2.5f;
				hipScale = 8.5f;
				break;
			case 4f:
                torsoScale = 3f;
				hipScale = 10f;
				//limbScale = 10f;
                break;
        }
		
		//EXTRA CHUB IN ARENA MODE - OR IN EVERY MODE!
		int stuffing = patch_Player.GetOverstuffed(self.player);
        if (BellyPlus.VisualsOnly())
            stuffing = Math.Min(stuffing, 10);
        //Debug.Log("STUFFING!): " + stuffing + " CHUB" + patch_Player.GetChubValue(self.player));
        bool inPipe = patch_Player.PipeStatus(self.player);

        if (stuffing > 0f) //HOPEFULLY THIS WILL STOP IT
        {
            if (stuffing > 10) //STUFFING PAST 10 WILL ONLY COUNT AS A THIRD OF IT'S NORMAL SIZE
                stuffing = 10 + ((stuffing - 10) / 2);

            if (patch_Player.IsCramped(self.player))
            {
                hipScale += stuffing / 2f; //3
                torsoScale += Mathf.Min(stuffing, 15) / 6f;
            }
			else
			{
				hipScale += stuffing / 1.5f;
                torsoScale += Mathf.Min(stuffing, 15) / 3f;
			}
            //sLeaser.sprites[hp].scaleY += 0.05f * Mathf.Min(stuffing, 20);
            float heighBonus = stuffing;
            if (heighBonus > 15f) //STUFFING PAST 10 WILL ONLY COUNT AS A THIRD OF IT'S NORMAL SIZE
                heighBonus = 15f + ((heighBonus - 15f) / 3f);

            if (self.player.playerState.isPup && !self.player.isGourmand)
                heighBonus *= 0.8f; //THE PUPS ARE TOO TAAALLL

            sLeaser.sprites[hp].scaleY += 0.05f * heighBonus;
        }
        //}
		
		
		//OKAY WIDE CHARACTERS LOOK TOO WONKY. WE CAN TRY AND RESHAPE IT AS WE INCREASE IN SIZE
		float extraLoad = sLeaser.sprites[hp].scaleX - 1.1f;
        float baseWidth = (self.player.playerState.isPup && self.player.isGourmand) ? 0f : 10f;
        if (extraLoad > 0.1f && stuffing > baseWidth)
		{
			sLeaser.sprites[hp].scaleX -= extraLoad * Mathf.Lerp(0f, 1f, (stuffing - baseWidth) /10f);
		}
		

        //OVERSIZED CHARACTERS LOOK TOO WIDE. ADD SOME OF THAT WIDTH TO THEIR HEIGHT TO BALANCE OUT
        bool tubbyBuild = sLeaser.sprites[hp].scaleX > 1.15f;  //TORSO SCALE ALWAYS RESETS EACH TICK SO WE CAN RELIABLY MEASURE IT
        if (tubbyBuild && stuffing < 10)
			sLeaser.sprites[hp].scaleY += 0.05f * (hipScale / 2f);


        //THE POACHER IS A FUNNY SHAPE!
        if (self.player.slugcatStats.name.value == "FriendDragonslayer" && hipScale > 10)
        {
            float redirect = Mathf.Min(hipScale - 10, 6);
            sLeaser.sprites[hp].scaleX -= 0.05f * (redirect);
            sLeaser.sprites[hp].scaleY += 0.05f * (redirect / 2);
            sLeaser.sprites[hp].y -= redirect;
        }

        if (inPipe)
            hipScale = Mathf.Min(hipScale, 20); //DON'T LET OUR FAT CLIP THROUGH WALLS IN CORRIDORS

        //BODY SQUASH AND STRETCH!
        if (patch_Player.IsStuck(self.player) && hipScale > 8)
        {
            float stuckPerc = patch_Player.GetProgress(self.player) / Mathf.Max(((patch_Player.bellyStats[playerNum].tileTightnessMod) / 30) - 1, 0.1f);
            stuckPerc = 1f - Mathf.Max(0, stuckPerc);
            hipScale = Mathf.Lerp(7f, hipScale, stuckPerc);
        }
		
		
		sLeaser.sprites[ct].scaleX += 0.05f * torsoScale;
        sLeaser.sprites[hp].scaleX += 0.05f * hipScale;

        //3 * 2.25
        //1 * 0.75
        //(stuffing / 60)

        //MORE STRETCH AND SQUISHING
        float frc = patch_Player.GetSquishForce(self.player);
        if (patch_Player.ObjBeingPushed(self.player) > 0 && patch_Player.IsStuck(self.player))
		{
            //ROTATE OUR TORSO FOR A BETTER LOOKING SQUISH
            if (!patch_Player.IsVerticalStuck(self.player))
            {
                float stuckFace = patch_Player.ObjGetStuckVector(self.player).x;
                sLeaser.sprites[hp].rotation = -90 * stuckFace;
            }
            
            if (frc > 5)
                frc += ((frc - 5) / 2f);
            float boostPerc = Mathf.InverseLerp(0, 8, frc);

            if (bpGraph[playerNum].lastSquish == 0) //SINGLE FRAME OF TWEEN SO IT'S LESS JARRING
                boostPerc *= 0.75f;
				
			//5-10-23 WE WAN'T THE SQUISH TO SCALE A BIT MORE AT LOWER SIZES. IT'S HARDLY VISIBLE
			float sqScl = 0.20f;
			// sqScl = Custom.LerpMap(hipScale, 10f, 35f, 0.60f, 0.20f);
			sqScl = Custom.LerpMap(sLeaser.sprites[hp].scaleX, 1.2f, 3.5f, 0.35f, 0.20f); //1.6 scaleX IS GOURMAND DEFAULT HIP SIZE
			
			if (!inPipe) //IT LOOKS FUNKY IF WE'RE IN A PIPE
				sLeaser.sprites[hp].ScaleAroundPointRelative(new Vector2(0, 8), Mathf.Lerp(1f, 1f + sqScl, boostPerc), Mathf.Lerp(1f, 1f - sqScl, boostPerc));
            self.tail[0].pos += patch_Player.ObjGetStuckVector(self.player) * (3f + (0.05f * hipScale)) * (boostPerc) * (sqScl * 5f);
        }
        bpGraph[playerNum].lastSquish = frc;



        //IF WE'RE DEAD, CUT THE REST. IT'S ALL EXPRESSION STUFF
        if (self.player.dead)
			return;

        bool exhaustionFxToggle = BPOptions.blushEnabled.Value && !(self.player.isNPC && self.player.isSlugpup) && !BellyPlus.VisualsOnly();
        float heatVal = 0;
        
        if (exhaustionFxToggle && rCam.cameraNumber == 0 && sLeaser.sprites.Length > bpGraph[playerNum].blSprt)
        {
            int bl = bpGraph[playerNum].blSprt;

            sLeaser.sprites[bl].scale = 1.8f;
            sLeaser.sprites[bl].scaleY = 0.8f;
            sLeaser.sprites[bl].rotation = sLeaser.sprites[9].rotation;
            sLeaser.sprites[bl].color = bpGraph[playerNum].blColor; //Color.red;

            sLeaser.sprites[bl].x = sLeaser.sprites[9].x; // + Custom.DirVec(self.drawPositions[0, 0], self.objectLooker.mostInterestingLookPoint).x;
            sLeaser.sprites[bl].y = sLeaser.sprites[9].y -= 0.5f; // + Custom.DirVec(self.drawPositions[0, 0], self.objectLooker.mostInterestingLookPoint).y;
            //Debug.Log("-----RENDER LAYERS!: " + sLeaser.sprites[10]._renderLayer + "-" + sLeaser.sprites[3]._renderLayer);
            float breathNum = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(self.lastBreath, self.breath, timeStacker) * 3.1415927f * 2f);
            float heatFloor = 400; //250
            float heatCeil = 1000; //750
            heatVal = Mathf.Min(Mathf.Max(patch_Player.GetHeat(self.player) - heatFloor, 0), heatCeil) / heatCeil;
            //Debug.Log("----BREATH!: " + breathNum + "  -  " + self.player.aerobicLevel + "  -  " + timeStacker + "-" + heatVal);

            if (patch_Player.GetBoostStrain(self.player) >= 10)
                heatVal *= 1.1f; //PUMP UP THE HEAD JUST A LITTLE BIT IF WE'RE BOOSTING HARD

            sLeaser.sprites[bl].alpha = heatVal * 0.4f;
            if (self.player.shortcutDelay > 1) // || patch_Player.IsGrabbedByPlayer(self.player))
                sLeaser.sprites[bl].alpha = 0f;

            //CHECK THIS FIRST BECAUSE WE MIGHT BE EXHAUSTTED
            if (self.player.isGourmand)
                self.player.ClassMechanicsGourmand();

            if (!self.player.lungsExhausted && !self.player.gourmandExhausted)
                self.player.aerobicLevel = Mathf.Min(0.85f, Mathf.Max(self.player.aerobicLevel, heatVal));

            //RELAXED EYES
            if (heatVal >= 0.7f && bpGraph[playerNum].randCycle < 3)
            {
                //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((!self.player.dead) ? "B" : "Dead"));
                self.player.Blink(5);
            }
            


            if (patch_Player.GetPant(self.player) && breathNum > 0.9f)
            {
                patch_Player.SetPant(self.player, false);
            }
            else if (!patch_Player.GetPant(self.player) && breathNum < 0.1f)
            {
                patch_Player.SetPant(self.player, true);
                float huffVol = heatVal * 0.45f * (self.player.lungsExhausted ? 0.6f : 1f);
                self.player.room.PlaySound(SoundID.Slugcat_Rocket_Jump, self.player.mainBodyChunk.pos, huffVol, 1.6f - (heatVal/2f) + Mathf.Lerp(-.2f, .2f, UnityEngine.Random.value));

                //ROLL THE DICE
                bpGraph[playerNum].randCycle = Mathf.FloorToInt(Mathf.Lerp(0f, 4f, UnityEngine.Random.value));


                //CHECK CHANCE
                if (UnityEngine.Random.value < heatVal * Mathf.Max(1f - (self.player.aerobicLevel / 3f), 0f)) // (self.player.lungsExhausted ? 0.5f : 0f)
                {
                    Vector2 pos = self.head.pos + new Vector2(self.player.flipDirection * (10f + Mathf.Lerp(-5f, 5f, UnityEngine.Random.value)), -5f + Mathf.Lerp(-5f, 5f, UnityEngine.Random.value));
                    //self.player.room.PlaySound(SoundID.Lizard_Voice_Black_B, pos, 0.1f, 1f);
                    float lifetime = 25f;
                    float innerRad = 2f;
                    float width = 8f;
                    float length = 8f;
                    int spikes = 6;
                    ExplosionSpikes myPop = new ExplosionSpikes(self.player.room, pos, spikes, innerRad, lifetime, width, length, new Color(1f, 1f, 1f, 0.5f));
                    self.player.room.AddObject(myPop);
                }
            }
        }






        if (patch_Player.GetWideEyes(self.player) > 0)
        {
            sLeaser.sprites[9].scaleY = 1.5f;
            float shift = patch_Player.IsStuck(self.player) ? 3f : 0f;
            sLeaser.sprites[9].y += shift;
            self.blink = 0;
            
			if (exhaustionFxToggle && rCam.cameraNumber == 0)
            {
                int bl = bpGraph[playerNum].blSprt;
                sLeaser.sprites[bl].y += shift;
                sLeaser.sprites[bl].scaleY += 0.4f;
                float alphaBoost = (Mathf.Min(patch_Player.GetWideEyes(self.player), 25f) / 25f) * 0.3f;
                sLeaser.sprites[bl].alpha += alphaBoost;
            }

            if (patch_Player.IsStuck(self.player))
                self.player.bodyChunks[0].vel.y = 8f;
        }
        else if (((patch_Player.IsStuckOrWedged(self.player) && patch_Player.IsPlayerSqueezing(self.player)) || patch_Player.IsPushingOther(self.player) || patch_Player.IsPullingOther(self.player)) && !self.player.lungsExhausted)
        {
            sLeaser.sprites[9].scaleY = 1f;
            //THIS MAKES THE > < FACE
            sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + "Stunned");
			//DMS NEEDS SOMETHING A LITTLE EXTRA
			if (BellyPlus.dressMySlugcatEnabled)
				ReplaceDMSSprite(self, 9, "FaceStunned");
            
            if (!patch_Player.IsPushingOther(self.player) && !patch_Player.IsPullingOther(self.player))
            {
                if (!patch_Player.IsVerticalStuck(self.player))
                {
                    //sLeaser.sprites[9].x += (5.0f * self.player.flipDirection);
                    sLeaser.sprites[9].y -= 1f; //patch_Player.GetYFlipDirection(self.player)
                    //if (blushToggle)
                        //sLeaser.sprites[10].x -= (1f * self.player.flipDirection);
					
					//IF WE DON'T SHRINK THE UPPER TORSO TO NORMAL SIZE HERE, IT LOOKS LIKE WE GOT WEIRD SHOULDERS.
					if (inPipe)
						sLeaser.sprites[ct].scaleX -= 0.05f * torsoScale;
                }

                if (self.player.input[0].IntVec != new IntVector2(0, 0))
                {
                    self.objectLooker.LookAtPoint(self.player.bodyChunks[0].pos + self.player.input[0].IntVec.ToVector2() * 50, 1f);
                    bpGraph[playerNum].staring = true; //SO WE CAN TURN IT OFF LATER
                }   
            }

            //Debug.Log("-----SQUISHING!: " + 1f + (patch_Player.GetStuckStrain(self.player) / 20f));
            //sLeaser.sprites[hp].scaleX *= 1f + (patch_Player.GetStuckStrain(self.player) / 200f);
            //sLeaser.sprites[hp].scaleY *= 1f - (patch_Player.GetStuckStrain(self.player) / 200f);
        }
        else
        {
            //IF WE'RE PANTING AND OUR EYES ARE CLOSED, MAKE EM EXTRA BIG!
            if (heatVal >= 0.7f && !self.player.lungsExhausted)
            {
                if (bpGraph[playerNum].randCycle < 3f)
                {
                    sLeaser.sprites[9].scaleY = 1.7f;
                    self.player.Blink(5);
                }
                else
                {
                    sLeaser.sprites[9].scaleY = 0.8f;
                }
            }   
            else
                sLeaser.sprites[9].scaleY = 1f;
        }




        //EVERGREEN CHANGES!
        /*
		if (patch_Player.ChunkyEvergreen(self.player))
		{
            Debug.Log("-----CHUNKY EVERGREEN!!: ");
            if (self.player.animation == Player.AnimationIndex.Roll && sLeaser.sprites[0].element.name == "EvergreenBody")
			{
				//sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName("EvergreenSpikedBody");
				//sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("EvergreenSpikedHips");
                ReplaceDMSSprite(self, 0, "EvergreenSpikedBody");
                ReplaceDMSSprite(self, 1, "EvergreenSpikedHips");
            }
			else if (self.player.stopRollingCounter > 0 && sLeaser.sprites[0].element.name == "EvergreenSpikedBody")
			{
				//sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName("EvergreenBody");
				//sLeaser.sprites[1].element = Futile.atlasManager.GetElementWithName("EvergreenHips");
                ReplaceDMSSprite(self, 0, "EvergreenBody");
                ReplaceDMSSprite(self, 1, "EvergreenHips");
            }
		}
		*/
        //Debug.Log("SPRITE" + sLeaser.sprites[3].element.name); //HMM NOPE 
        //Debug.Log("SPRITE" + DressMySlugcat.Customization.For(self.player).CustomSprites[0].SpriteSheetID);
        

        //QUIT STARING OFF INTO SPACE!
        //if (self.player.bodyMode == Player.BodyModeIndex.CorridorClimb && !(patch_Player.ObjIsWedged(self.player) && self.player.input[0].IntVec != new IntVector2(0,0)))
        if (bpGraph[playerNum].staring && !patch_Player.IsStuckOrWedged(self.player) || self.player.input[0].IntVec == new IntVector2(0, 0))
        {
            if (self.objectLooker.lookAtPoint != null && UnityEngine.Random.value < 0.01f)
                self.objectLooker.lookAtPoint = null;
        }
		
		
		//FOR THOSE PESKY GRAPHICS MODS...
		bpGraph[playerNum].verified = true;

        
        bool debugBar = false;
        if (debugBar)
        {
            sLeaser.sprites[11].alpha = 1f;
            sLeaser.sprites[11].element = Futile.atlasManager.GetElementWithName("pixel");
            sLeaser.sprites[11].scale = 5f;
            //float barLen = patch_Player.GetExhaustionMod(self.player, 0f);

            float barLen = patch_Player.GetStuckPercent(self.player);
            sLeaser.sprites[11].scaleX = (8f * (1f - barLen) * 5f);

            //float barLen = patch_Player.GetProgress(self.player) / 1f;
            //sLeaser.sprites[11].scaleX = (8f * (barLen) * 5f);

            //float testAlpha = patch_Player.GetBoostStrain(self.player);
            //sLeaser.sprites[bl].alpha += testAlpha/8f;
            //sLeaser.sprites[bl].alpha += 0.5f;
        }
    }


    //THIS IS THE SAME DRAWSPRITES HOOK EXCEPT APPLIED LATER SO IT HAS LATER PRIORITY
    public static void LatePriorityDrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (self.player?.slugcatStats?.name?.value == "Pearlcat")
            CloakDraw(self, sLeaser);
    }


    public static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);
        //TUMMY RUBS
        self.drawPositions[1, 0] += patch_Player.bellyStats[patch_Player.GetPlayerNum(self.player)].tuchShift;

        if (self.player?.slugcatStats?.name?.value == "Pearlcat")
            CloakCheck(self);
    }

    public static bool InspectGraphics(PlayerGraphics self)
    {
        int playerNum = patch_Player.GetPlayerNum(self.player);
		return bpGraph[playerNum].verified;
    }
	
	
	//A HOOK FOR A DMS HOOK!
	public static void ReplaceDMSSprite(PlayerGraphics self, int sprt, string name)
    {
        if (DressMySlugcat.Hooks.PlayerGraphicsHooks.PlayerGraphicsData.TryGetValue(self, out var data))
		{
			data.SpriteNames[sprt] = name;
		}
    }
}