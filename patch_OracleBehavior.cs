using UnityEngine;
using MoreSlugcats;

public class patch_OracleBehavior
{
	public static void Patch()
	{
		On.SSOracleBehavior.Update += OracleBehavior_Update;
		On.SSOracleBehavior.NewAction += BP_NewAction;
		On.SSOracleBehavior.PebblesConversation.AddEvents += BP_AddEvents;
		On.SLOracleBehaviorHasMark.ThirdAndUpGreeting += BP_ThirdAndUpGreeting;
		
		On.SSOracleBehavior.ThrowOutBehavior.Update += BP_ThrowOut_Update;
		
		On.SSOracleBehavior.SSSleepoverBehavior.Update += BP_Sleepover_Update;
        On.SSOracleBehavior.SSSleepoverBehavior.ctor += BP_Sleepover_ctor;
        On.SSOracleBehavior.SSOracleMeetPurple.Update += BP_SSOracleMeetPurple_Update;
		//On.SSOracleBehavior.SSSleepoverBehavior.ctor += SSSleepoverBehavior_ctor;
	}


    //SS = SUPER STRUCTURE (DEFAULT 5P)
    //SL = SHORE LINE (DEFAULT MOON)
    //DM = (FUNCTIONING MOON)
    //ST = (ROTTING PEBBLES)
    //CL = (CRIPPLED 5P)

    private static void BP_Sleepover_ctor(On.SSOracleBehavior.SSSleepoverBehavior.orig_ctor orig, SSOracleBehavior.SSSleepoverBehavior self, SSOracleBehavior owner)
    {
		orig.Invoke(self, owner);

		foreach (AbstractCreature abstractPlayer in self.oracle.room.game.Players)
		{
			int fats = 0;
			int thins = 0;
			if (abstractPlayer.Room == self.oracle.room.abstractRoom && abstractPlayer.realizedCreature is Player player && player != null && !player.dead) // && player != oracleBehavior.player)
			{
				if (patch_Player.GetChubValue(player) >= 3)
					fats++;
				else
					thins++;
			}

			if (fats >= 1 && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
				self.dialogBox.NewMessage(self.Translate("My, you are looking quite... Well fed. I'm glad you are doing well for yourself."), 10);
		}
	}

    public static bool repeatSelf = false;
	
	
	// public string NameForPlayer(bool capitalized)
	// {
		// string str = this.Translate("creature");
		// string text = this.Translate("little");
		// if (capitalized && InGameTranslator.LanguageID.UsesCapitals(this.oracle.room.game.rainWorld.inGameTranslator.currentLanguage))
		// {
			// text = char.ToUpper(text[0]).ToString() + text.Substring(1);
		// }
		// return text + " " + str;
	// }
	
	public static void BP_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
	{
		if (ModManager.MSC && (nextAction == MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty || nextAction == MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty))
		{
			stuckCounter = 1;
			repeatSelf = false;
		}
		
		orig.Invoke(self, nextAction);
	}
	
	
	public static void BP_ThirdAndUpGreeting(On.SLOracleBehaviorHasMark.orig_ThirdAndUpGreeting orig, SLOracleBehaviorHasMark self)
	{
		orig.Invoke(self);
		
		foreach (AbstractCreature abstractPlayer in self.oracle.room.game.Players)
		{
			int fats = 0;
			int thins = 0;
			if (abstractPlayer.Room == self.oracle.room.abstractRoom && abstractPlayer.realizedCreature is Player player && player != null && !player.dead) // && player != oracleBehavior.player)
			{
				if (patch_Player.GetChubValue(player) >= 3)
					fats++;
				else
					thins++;
			}
			
			if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
			{
				self.dialogBox.NewMessage(self.Translate("You look quite fat. I have no food to offer a glutton like yourself."), 10);
			}
			else
			{
				if (fats >= 1 && thins >= 1)
					self.dialogBox.NewMessage(self.Translate("Your friend seems very... round. Did they fit in here alright?"), 10);
				else if (fats >= 1)
				{
					self.dialogBox.NewMessage(self.Translate("My, you are looking quite... Well fed. I'm glad you are doing well for yourself."), 10);
				}
			}
		}
	}
	
	
	
	
	//EXTRA DIALOGUE
	public static void BP_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
	{
		if (patch_Player.GetChubValue(self.owner.player) > 1)
		{
			
			//self.events.Add(new Conversation.TextEvent(self, 0, ".  .  .", 0));
			//self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...is this reaching you?"), 0));
			////self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("My, you are much... Rounder than my records would have me assume."), 0));
			//self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 4));
			//if (self.id == Conversation.ID.Pebbles_White && !self.owner.playerEnteredWithMark)
			//	self.owner.playerEnteredWithMark = true; //JUST PRETEND, SO WE DON'T REPEAT THE SAME OPENER. IT'S FINE.
			//I THINK IT'S MESSING UP THE PLAYER'S GAINED KARMA, SO LETS NOT DO THAT...

			//SOME PEBBLES SPECIFIC STUFF
			if (ModManager.MSC && self.owner.oracle.room.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.owner.oracle.ID == Oracle.OracleID.SS)
			{
				Debug.Log("PEBS DIALOG " + self.owner.action);
				//THIS IS THE ONLY DIALOGUE PEBBLES SHOULD EVER GIVE SPEARMASTER
				if (self.owner.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetPurple_InspectPearl)
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Suns, your messenger is quite large..."), 0));
			}
			else if (self.owner.oracle.ID != Oracle.OracleID.SS)
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("My, what a well-fed little creature you are..."), 0));
			else
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("My, you are much... Rounder than my records would have me assume."), 0));
		}
		
		orig.Invoke(self);
	}
	
	
	public static void FindFatterPlayer(OracleBehavior oracleBehavior)
	{
		if (oracleBehavior.player == null)
			return; //LET THE GAME HANDLE THIS FIRST...

		foreach (AbstractCreature abstractPlayer in oracleBehavior.oracle.room.game.Players)
		{
			//IF THERES MORE THAN ONE...
			if (abstractPlayer.Room == oracleBehavior.oracle.room.abstractRoom && abstractPlayer.realizedCreature is Player player && player != null && player.playerState != null && !player.dead) // && player != oracleBehavior.player)
			{
				//PICK THE FATTER ONE. OR THE ONE THAT IS STILL IN THE ROOM
				if (patch_Player.GetChubValue(player) > patch_Player.GetChubValue(oracleBehavior.player) || oracleBehavior.player.room != oracleBehavior.oracle.room)
					oracleBehavior.player = player;
				// break;
			}
		}
	}
	
	

	public static void OracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
	{
		//NOT IF WE'RE MEDITATING. ANYONE CAN FALL ONTO OUR FLOOR
		if (self.getToWorking == 0)
			FindFatterPlayer(self);
		
		//IF WE'RE TRYING TO GIVE THEM MARK, TUG THEM
		if (self.action == SSOracleBehavior.Action.General_GiveMark && self.player != null)
		{
			if (self.inActionCounter > 40 && patch_Player.IsStuck(self.player))
			{
				patch_Player.ObjGainBoostStrain(self.player, 0, 3, 18);
				patch_Player.ObjGainStuckStrain(self.player, 8f);
			}
		}

		//if (self.player != null && patch_Player.ObjGetNoStuck(self.player) > 0)
		//	repeatSelf = false;
		
		orig(self, eu);
	}
	
	
	//THIS SINGLE VARIABLE SHOULD WORK FINE
	public static int stuckCounter = 0;
	
	
	public static void BP_ThrowOut_Update(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, SSOracleBehavior.ThrowOutBehavior self)
	{
		if (self.player == null || self.oracle.room == null)// || self.player.room != self.oracle.room || self.player.inShortcut)
        {
			orig(self);
			return;
		}
			
		
		
		FindFatterPlayer(self.owner);

		//RESET
		if (self.player.room != self.oracle.room)
			self.owner.throwOutCounter = 1;

		orig(self);
		
		
		//RESET TIMERS
		if (self.owner.throwOutCounter == 1)
			stuckCounter = 1;

		int firstTry = 1000;
		int secondTry = firstTry + 400;

		if (self.action == SSOracleBehavior.Action.ThrowOut_SecondThrowOut)
			secondTry = 200;

		//self.telekinThrowOut && 
		if (self.player.room == self.oracle.room && patch_Player.IsStuck(self.player) && patch_Player.PipeStatus(self.player) == false)
		{
			self.owner.throwOutCounter--;
			stuckCounter ++;
			//"
			// ""
			// " You're too fat for them.
			// "I'm tired of this. You're too fat for these corridors.
			if (patch_Player.ObjGetLoosenProg(self.player) > 0 && stuckCounter < secondTry)
				patch_Player.ObjGainLoosenProg(self.player, -1 / 2000f);
			

			//FIRST OR POLITE THROWOUT
			if (self.action != SSOracleBehavior.Action.ThrowOut_SecondThrowOut)
			{
				
				if (stuckCounter == 300)
				{
					self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
					if (self.player.isGourmand)
						self.dialogBox.Interrupt(self.Translate("... You're stuck, aren't you? I can't say I'm surprised."), 0);
					else	
						self.dialogBox.Interrupt(self.Translate("Is there a problem? Please be on your way, I have much work to do."), 0);
				}
				
				//this.SetNewDestination(this.oracle.room.MiddleOfTile(24, 17));
				if (stuckCounter == 500)
				{
					self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
					self.owner.SetNewDestination(self.oracle.room.MiddleOfTile(24, 20));
				}
				else if (stuckCounter == 700)
				{
					self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
					self.owner.SetNewDestination(self.oracle.room.MiddleOfTile(24, 20));
					if (self.player.isGourmand)
						self.dialogBox.Interrupt(self.Translate("It's more impressive that you were able to fit through here in the first place."), 0);
					else	
						self.dialogBox.Interrupt(self.Translate("... You're stuck, aren't you?"), 0);
				}
				else if (stuckCounter == firstTry)
				{
					self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
					self.dialogBox.Interrupt(self.Translate("Very well... I will help you. But please do not go wedging yourself into any more of my corridors."), 0);
				}
				else if (stuckCounter == firstTry + 300)
				{
					self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 0.35f, 1f);
					self.player.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.player.mainBodyChunk.pos, 3f, 0.6f);
					// bellyStats[myPartner.playerState.playerNumber].myHeat = 350;
					// myPartner.bodyChunks[0].vel.y += 8f; //THIS DON'T DO JACK ANYWAYS 
					Vector2 pos = self.player.bodyChunks[1].pos;
					float lifetime = 8f;
					float innerRad = 8f;
					float width = 8f;
					float length = 25f;
					int spikes = 8;
					ExplosionSpikes myPop = new ExplosionSpikes(self.oracle.room, pos, spikes, innerRad, lifetime, width, length, new Color(1f, 1f, 1f, 0.5f));
					self.oracle.room.AddObject(myPop);
					patch_Player.ObjGainBoostStrain(self.player, 0, 15, 18);
				}
				else if (stuckCounter ==  firstTry + 305)
				{
					self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
					patch_Player.ObjGainStuckStrain(self.player, 100f);
					
				}
				else if (stuckCounter == firstTry + 331)
				{
					(self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 1f;
				}
				
				
				
				else if (stuckCounter == secondTry)
				{
					self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
					self.owner.SetNewDestination(self.oracle.room.MiddleOfTile(24, 20));
					if (!repeatSelf)
					{
						if (self.player?.slugcatStats?.name?.value == "Estranged") //COLLAB UNIQUE DIALOGUE
                            self.dialogBox.Interrupt(self.Translate("It's truly horrifying how a creature so small can trap itself so easily in the cycles... And my pipes."), 0);
						else
							self.dialogBox.Interrupt(self.Translate("I may have underestimated your girth... Do brace yourself, this next one won't be so gentle."), 0);
						repeatSelf = true;
					}
					else
						self.dialogBox.Interrupt(self.Translate("Hold still now... "), 0);
				}
				
				//LOOPING
				if (stuckCounter > 1300 && stuckCounter < 1330)
				{
					patch_Player.ObjGainBoostStrain(self.player, 0, 2, 18);
					patch_Player.ObjGainStuckStrain(self.player, 1f);
					patch_Player.ObjSetFwumpFlag(self.player, 3);
					//patch_Player.ObjGainLoosenProg(self.player, 1 / 1000f);
					(self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 6f;
				}
			}
			
			//  ---SECOND THROW OUT---
			else if (self.action == SSOracleBehavior.Action.ThrowOut_SecondThrowOut)
			{
				if (stuckCounter == secondTry)
				{
					self.dialogBox.Interrupt(self.Translate("I'm tired of this. You're too fat for these corridors. Hold still."), 0);
				}
			}
			
			
			
			if (stuckCounter == secondTry + 150)
			{
				//self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
				self.oracle.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_On, 0f, 1.3f, 1f);
				self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
			}
			else if (stuckCounter == secondTry + 275)
			{
				self.owner.killFac = 0f;
				self.oracle.room.PlaySound(SoundID.Gate_Pillows_In_Place, 0f, 1f, 1f);
				self.player.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.player.mainBodyChunk.pos, 3f, 0.6f);
			}
			
			
			
			//LOOPING ACTIONS
			
			else if (stuckCounter > secondTry + 200 && stuckCounter < secondTry + 275)
			{
				self.owner.killFac += 0.015f; //THIS SHOULDN'T KILL UNLESS HE'S ACTUALLY MAD AT US
			}
			else if (stuckCounter > secondTry + 275)
			{
				patch_Player.ObjGainBoostStrain(self.player, 0, 15, 20);
				patch_Player.ObjGainStuckStrain(self.player, 8f);
				patch_Player.ObjSetFwumpFlag(self.player, 3);
				patch_Player.ObjGainLoosenProg(self.player, 1 / 1000f);
				// patch_Player.ObjSetNoStuck(self.player, 30);
				(self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 8f;
			}

			//IF THEY'RE SOMEHOW STILL STUCK, START GETTING MAD AGAIN
			if (stuckCounter > secondTry + 400)
				self.owner.throwOutCounter++;
		}
		
		//DIALOGUE CHECKPOINT IN CASE THINGS GOT INTERRUPTED
		else if (stuckCounter > (secondTry + 200) && !patch_Player.IsStuck(self.player))
			stuckCounter = secondTry - 50;
		
		// else if (self.oracle.graphicsModule != null)
		// {
			// //I THINK THIS WILL PUT IT BACK WHEN WE POP FREE
			// (self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 1f;
			// //WON'T IT REINITIALIZE? I DON'T THINK WE NEED THIS
		// }
	}
	
	
	
	
	
	
	public static void BP_Sleepover_Update(On.SSOracleBehavior.SSSleepoverBehavior.orig_Update orig, SSOracleBehavior.SSSleepoverBehavior self)
	{
		if (self.player == null || self.oracle.room == null)
        {
			stuckCounter = 1;
			return;
		}
			
		
		FindFatterPlayer(self.owner);

		orig(self);

		int firstTry = 500; //1000;
		int secondTry = firstTry + 300;

		//self.telekinThrowOut && 
		if (self.player.room == self.oracle.room && patch_Player.IsStuck(self.player)) // && patch_Player.PipeStatus(self.player) == false)
		{
			self.owner.throwOutCounter--;
			stuckCounter ++;
			if (self.panicTimer > 0) //DON'T LET MOON HAVE A STROKE MID-INTERACTION WITH SPEARMASTER
				self.panicTimer--;

			if (patch_Player.ObjGetLoosenProg(self.player) > 0 && stuckCounter < secondTry)
				patch_Player.ObjGainLoosenProg(self.player, -1 / 2000f);


			//DON'T INTERRUPT IF WE'RE MONOLOGUING
			bool artiPityConvo = self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding && self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 0 && self.inActionCounter <= 1200;
			if (artiPityConvo)
            {
				stuckCounter = firstTry + 149;

			}


			if (stuckCounter == 300)
			{
				self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
				// self.dialogBox.Interrupt(self.Translate("Is there a problem? Please be on your way, I have much work to do."), 0);
				if (self.oracle.ID == Oracle.OracleID.SS)
				{
					if (UnityEngine.Random.value <= 0.33f)
						self.dialogBox.Interrupt(self.Translate("... You're stuck, aren't you?"), 0);
					else if (UnityEngine.Random.value <= 0.5f)
						self.dialogBox.Interrupt(self.Translate("... You look like you could use some assistance."), 0);
					else
						self.dialogBox.Interrupt(self.Translate("... Again?"), 0);
				}
				else
				{
					if (self.firstMetOnThisCycle)
						self.dialogBox.Interrupt(self.Translate("Oh dear, perhaps I was a bit too generous?..."), 0);
					else if (UnityEngine.Random.value <= 0.5f)
						self.dialogBox.Interrupt(self.Translate("Oh dear, do you need help?..."), 0);
					else
						self.dialogBox.Interrupt(self.Translate("Oh my, that looks uncomfortable..."), 0);
				}
				
			}
			
			//this.SetNewDestination(this.oracle.room.MiddleOfTile(24, 17));
			if (stuckCounter == 400)
			{
				self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
				self.owner.SetNewDestination(self.oracle.room.MiddleOfTile(24, 20));
			}
			// else if (stuckCounter == 700)
			// {
				// self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
				// self.owner.SetNewDestination(self.oracle.room.MiddleOfTile(24, 20));
				// self.dialogBox.Interrupt(self.Translate("... You're stuck, aren't you?"), 0);
			// }
			else if (stuckCounter == firstTry)
			{
				self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
				self.dialogBox.Interrupt(self.Translate("Hold still now..."), 0);
			}
			else if (stuckCounter == firstTry + 150)
			{
				self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 0.35f, 1f);
				self.player.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.player.mainBodyChunk.pos, 3f, 0.6f);
				// bellyStats[myPartner.playerState.playerNumber].myHeat = 350;
				// myPartner.bodyChunks[0].vel.y += 8f; //THIS DON'T DO JACK ANYWAYS 
				Vector2 pos = self.player.bodyChunks[1].pos;
				float lifetime = 8f;
				float innerRad = 8f;
				float width = 8f;
				float length = 25f;
				int spikes = 8;
				ExplosionSpikes myPop = new ExplosionSpikes(self.oracle.room, pos, spikes, innerRad, lifetime, width, length, new Color(1f, 1f, 1f, 0.5f));
				self.oracle.room.AddObject(myPop);
				patch_Player.ObjGainBoostStrain(self.player, 0, 15, 18);
			}
			else if (stuckCounter ==  firstTry + 155)
			{
				self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
				patch_Player.ObjGainStuckStrain(self.player, 100f);

				if (self.inActionCounter <= 1000)
                {
					stuckCounter = 0;
					return;
                }
			}
			else if (stuckCounter == firstTry + 181)
			{
				(self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 1f;
			}
			
			
			
			else if (stuckCounter == secondTry)
			{
				self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
				self.owner.SetNewDestination(self.oracle.room.MiddleOfTile(24, 20));
				if (!repeatSelf)
				{
					// self.dialogBox.Interrupt(self.Translate("I may have underestimated your girth... Do brace yourself, this next one won't be so gentle."), 0);
					if (self.oracle.ID == Oracle.OracleID.SS)
					{
						if (UnityEngine.Random.value <= 0.33f)
							self.dialogBox.Interrupt(self.Translate("Have you considered going on a diet?"), 0);
						else if (UnityEngine.Random.value <= 0.5f)
							self.dialogBox.Interrupt(self.Translate("Are you familiar with the concept of fasting? I'd highly recommend it."), 0);
						else
							self.dialogBox.Interrupt(self.Translate("Surely this can't be a healthy weight for your kind."), 0);
					}
					else
					{
						if (self.firstMetOnThisCycle)
							self.dialogBox.Interrupt(self.Translate("Poor thing, you're really wedged in there. Hold still now, I will be gentle."), 0);
						else if (UnityEngine.Random.value <= 0.33f)
							self.dialogBox.Interrupt(self.Translate("Oh dear, this is quite a snug fit..."), 0);
						else if (UnityEngine.Random.value <= 0.5f)
							self.dialogBox.Interrupt(self.Translate("I apologize these corridors weren't built larger..."), 0);
						else
							self.dialogBox.Interrupt(self.Translate("You poor thing, they just didn't build these entrances wide enough."), 0);
					}
					
					repeatSelf = true;
				}
				else
					self.dialogBox.Interrupt(self.Translate("Hold still now... "), 0);
			}
			
			//LOOPING
			if (stuckCounter > (firstTry + 150) && stuckCounter < (firstTry + 180))
			{
				patch_Player.ObjGainBoostStrain(self.player, 0, 2, 18);
				patch_Player.ObjGainStuckStrain(self.player, 1f);
				patch_Player.ObjSetFwumpFlag(self.player, 3);
				patch_Player.ObjGainLoosenProg(self.player, 1 / 1000f);
				(self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 6f;
			}
		
			
			
			
			if (stuckCounter == secondTry + 150)
			{
				//self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
				self.oracle.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_On, 0f, 1.3f, 1f);
				self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
			}
			else if (stuckCounter == secondTry + 275)
			{
				self.owner.killFac = 0f;
				self.oracle.room.PlaySound(SoundID.Gate_Pillows_In_Place, 0f, 1f, 1f);
				self.player.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.player.mainBodyChunk.pos, 3f, 0.6f);
			}
			
			
			
			//LOOPING ACTIONS
			
			else if (stuckCounter > secondTry + 200 && stuckCounter < secondTry + 275)
			{
				self.owner.killFac += 0.015f; //THIS SHOULDN'T KILL UNLESS HE'S ACTUALLY MAD AT US
			}
			else if (stuckCounter > secondTry + 275)
			{
				patch_Player.ObjGainBoostStrain(self.player, 0, 15, 20);
				patch_Player.ObjGainStuckStrain(self.player, 8f);
				patch_Player.ObjSetFwumpFlag(self.player, 3);
				patch_Player.ObjGainLoosenProg(self.player, 1 / 1000f);
				// patch_Player.ObjSetNoStuck(self.player, 30);
				(self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 8f;
			}

			//IF THEY'RE SOMEHOW STILL STUCK, START GETTING MAD AGAIN
			if (stuckCounter > secondTry + 400)
				self.owner.throwOutCounter++;
		}
		
		//DIALOGUE CHECKPOINT IN CASE THINGS GOT INTERRUPTED
		else if (stuckCounter > (secondTry + 200) && !patch_Player.IsStuck(self.player))
			stuckCounter = secondTry - 50;
	}



    //SPECIFIC TO SPEARMASTER 5P DIALOGUE WITH MARK
    private static void BP_SSOracleMeetPurple_Update(On.SSOracleBehavior.SSOracleMeetPurple.orig_Update orig, SSOracleBehavior.SSOracleMeetPurple self)
    {
        if (self.player == null || self.oracle.room == null)
        {
            stuckCounter = 1;
            return;
        }

        FindFatterPlayer(self.owner);

        orig(self);

		int secondTry = -100; //RIGHT TO THE SECOND PHASE

        if (self.player.room == self.oracle.room && patch_Player.IsStuck(self.player))
        {
            self.owner.inActionCounter--;
            stuckCounter++;

            if (stuckCounter == secondTry + 150)
            {
                //self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
                self.oracle.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_On, 0f, 1.3f, 1f);
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
            }
            else if (stuckCounter == secondTry + 275)
            {
                self.owner.killFac = 0f;
                self.oracle.room.PlaySound(SoundID.Gate_Pillows_In_Place, 0f, 1f, 1f);
                self.player.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.player.mainBodyChunk.pos, 3f, 0.6f);
            }



            //LOOPING ACTIONS

            else if (stuckCounter > secondTry + 200 && stuckCounter < secondTry + 275)
            {
                self.owner.killFac += 0.015f; //THIS SHOULDN'T KILL UNLESS HE'S ACTUALLY MAD AT US
            }
            else if (stuckCounter > secondTry + 275)
            {
                patch_Player.ObjGainBoostStrain(self.player, 0, 15, 20);
                patch_Player.ObjGainStuckStrain(self.player, 8f);
                patch_Player.ObjSetFwumpFlag(self.player, 3);
                patch_Player.ObjGainLoosenProg(self.player, 1 / 1000f);
                // patch_Player.ObjSetNoStuck(self.player, 30);
                (self.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 8f;
            }

            //IF THEY'RE SOMEHOW STILL STUCK, START GETTING MAD AGAIN
            if (stuckCounter > secondTry + 400)
                self.owner.inActionCounter++;
        }
    }
}
