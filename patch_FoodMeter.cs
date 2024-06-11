using RWCustom;
using UnityEngine;
using HUD;
using MonoMod.RuntimeDetour;
using System;

using System.Collections.Generic;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace RotundWorld;

public class patch_FoodMeter
{
	public static void Patch()
	{
		On.HUD.FoodMeter.ctor += FoodMeter_ctor;
        On.HUD.FoodMeter.UpdateShowCount += FoodMeter_UpdateShowCount;
		On.HUD.FoodMeter.QuarterPipShower.Draw += QuarterPipShower_Draw;
		On.HUD.FoodMeter.QuarterPipShower.ctor += QuarterPipShower_ctor;
		On.HUD.FoodMeter.MeterCircle.Draw += MeterCircle_Draw;
		On.HUD.FoodMeter.Update += FoodMeter_Update;
	}

    public static void FoodMeter_Update(On.HUD.FoodMeter.orig_Update orig, FoodMeter self)
   {
		orig.Invoke(self);
		if (BellyPlus.VisualsOnly())
			return;


		//(self.hud.owner as Player).abstractCreature.world.game.cameras[0].hud == self.hud
		

		if (arrowsOn && self.hud.owner.GetOwnerType() == HUD.HUD.OwnerType.Player && !self.IsPupFoodMeter)
        {
			//Debug.Log("---FOOD METER UPDATE! ---");
			for (int j = 0; j < (self.hud.owner as Player).abstractCreature.world.game.cameras.Length; j++)
            {
				if ((self.hud.owner as Player).abstractCreature.world.game.cameras[0].hud == self.hud)
                {
					List<AbstractCreature> players = (self.hud.owner as Player).abstractCreature.world.game.session.Players;
					FoodMeter myMeter = (self.hud.owner as Player).abstractCreature.world.game.cameras[j].hud.foodMeter;
					Vector2 myMeterPos = myMeter.circles[0].DrawPos(1f);// - new Vector2( ((self.hud.owner as Player).abstractCreature.world.game.cameras[j].sSize.x / 4f), 0f);

					//EMERGENCY CHECK! MAKE SURE WE HAVE ENOUGH ARROWS ON SCREEN TO UPDATE
					//WAIT THIS WOULD ONLY WORK IN SPLITSCREEN MODE
					if (foodArrows.Count < (players.Count * (self.hud.owner as Player).abstractCreature.world.game.cameras.Length))
					{
						Debug.Log("-NOT ENOUGH ARROWS!! HOLDUP A MINUTE..." + foodArrows.Count);
						return;
					}

					int lastCircle = self.circles.Count - 1;
					float pipAlpha = 0;
					//ALPHA SHOULD ALWAYS BE 0 UNLESS THE HUD IS VISIBLE, DUMMY
					if (self.circles[lastCircle].circles[0].sprite.isVisible)
						pipAlpha = self.circles[lastCircle].circles[0].sprite.alpha;
					for (int i = 0; i < players.Count; i++)
					{
						Player myPlayer = players[i].realizedCreature as Player;
						if (myPlayer != null)
						{
							float foodCount = Mathf.Max(patch_Player.GetPersonalFood(myPlayer) + (myPlayer.abstractCreature.GetAbsBelly().externalMass / 2) - 1, 0); // + Mathf.CeilToInt(patch_Player.GetOverstuffed(myPlayer) / 2f) - 1; //self.hud.owner.CurrentFood
							//Debug.Log("-ARROW HUD UPDATE! PLAYER" + i + " OTHER:" + (j * players.Count) + "  CAM: " + j + " DRAWPOS: " + myMeterPos.x + " ALPHA " + pipAlpha + " COLOR " + foodArrows[i + (j * players.Count)].color);
							foodArrows[i + (j * players.Count)].alpha = pipAlpha;
							if (!BellyPlus.individualFoodEnabled)
							{
                                float lineSpace = (foodCount >= self.survivalLimit ? 0.5f : 0f);
                                foodArrows[i + (j * players.Count)].x = myMeterPos.x + (self.CircleDistance(1f) * foodCount) + (self.CircleDistance(1f) * lineSpace) + (-1 + i * 3f);
                                foodArrows[i + (j * players.Count)].y = myMeterPos.y + 20f;
                            }
                            else
                            {
                                //SPECIAL CASE FOR INDIVIDUAL FOOD BAR MOD
                                float foodRow = (players.Count - i) + ((self.hud.foodMeter.pupBars != null) ? ((float)self.hud.foodMeter.pupBars.Count) : 0f);
                                if (self.hud.gourmandmeter != null)
                                    foodRow += (float)self.hud.gourmandmeter.visibleRows;

                                if (i > 0 && foodCount >= SlugcatStats.SlugcatFoodMeter(myPlayer.slugcatStats.name).x)
								{
                                    float lineSpace = (foodCount >= SlugcatStats.SlugcatFoodMeter(myPlayer.slugcatStats.name).y ? 0.72f : 0f); //self.survivalLimit
                                    float circDist = 20; //self.CircleDistance(1f)
                                    foodArrows[i + (j * players.Count)].x = myMeterPos.x + (circDist * foodCount) + (circDist * lineSpace) - 2.0f; //(-1 + i * 3f);
                                    foodArrows[i + (j * players.Count)].y = myMeterPos.y + (foodRow * 25f) + 2f; //+ 5f;
                                }
                                else //MAKE P1'S ARROW INVISIBLE, AND ANYONES BELOW FULL
                                    foodArrows[i + (j * players.Count)].alpha = 0;
                            }
						}
						else
						{	
							//REALIZED CREATURE IS NULL!
							foodArrows[i + (j * players.Count)].alpha = 0;
						}
					}
				}
				else
                {
					//Debug.Log("THIS WAS SOMEONE ELSES HUD! " + j);
				}
			}
		}
	}


    public static void FoodMeter_UpdateShowCount(On.HUD.FoodMeter.orig_UpdateShowCount orig, FoodMeter self)
	{
		if (self.showCount > self.circles.Count)
			return; //THAT WOULD HAVE CRASHED US!

        orig.Invoke(self);
	}



	public static int bonusFoodMemory = 0; //REMEMBERS WHAT IT WAS FOR THE REST OF THE SESSION
	public static List<FSprite> foodArrows;
	public static bool arrowsOn;
	
	//THIS PART IS USED SPECIFICALLY FOR THE SLEEP SCREEN, SINCE WE HANDLE THAT DIFFERENTLY THAN THE PLAYER VERSION
	public static void MeterCircle_Draw(On.HUD.FoodMeter.MeterCircle.orig_Draw orig, FoodMeter.MeterCircle self, float timeStacker)
    {
		orig.Invoke(self, timeStacker);
		if (BellyPlus.VisualsOnly())
			return;
		
		if (self.meter.hud?.owner?.GetOwnerType() == HUD.HUD.OwnerType.SleepScreen || self.meter.hud?.owner?.GetOwnerType() == HUD.HUD.OwnerType.CharacterSelect)
        {

			if (self.meter.hud.owner.GetOwnerType() == HUD.HUD.OwnerType.CharacterSelect)
				bonusFoodMemory = self.meter.sleepScreenPhase; //CHEATING LOL THIS IS DUMB

            //if (self.number >= (self.meter.hud.owner as Player).slugcatStats.maxFood)
            if (self.number >= (self.meter.maxFood - bonusFoodMemory)) //(self.meter.hud.owner.CurrentFood - self.meter.survivalLimit)))//
            {
				self.circles[0].visible = false;
				self.circles[0].sprite.alpha = 0f;
			}
		}
    }

    //GIVE EVERYONE THE QUARTERPIP METER. NOT JUST RED
    public static void FoodMeter_ctor(On.HUD.FoodMeter.orig_ctor orig, HUD.FoodMeter self, HUD.HUD hud, int maxFood, int survivalLimit, Player pup, int pupNumber)
	{
        if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, hud, maxFood, survivalLimit, pup, pupNumber); 
			return;
		}

        //if (hud.owner.GetOwnerType() == HUD.HUD.OwnerType.CharacterSelect || hud.owner.GetOwnerType() == HUD.HUD.OwnerType.ArenaSession)
			//BellyPlus.tomorrowsBonusFood = 0;

		//JUST REAL QUICK
		if (self.IsPupFoodMeter)
        {
			//(hud.owner as Player).slugcatStats.maxFood += 2;
			//maxFood += 2;
		}
		
		//REAL QUICK SINCE THIS DOESN'T EXIST YET
		if (hud.owner.GetOwnerType() == HUD.HUD.OwnerType.CharacterSelect)
			BellyPlus.bonusFood = Math.Max(0, hud.owner.CurrentFood - survivalLimit);
			

		//1-18-23 SHOW THE CORRECT FOOD AMOUNT, EVEN ON THE SLEEP SCREEN
		if (hud.owner.GetOwnerType() == HUD.HUD.OwnerType.SleepScreen || hud.owner.GetOwnerType() == HUD.HUD.OwnerType.CharacterSelect) //HUD.OwnerType.SleepScreen
		{
			//RUN THE ORIGINAL, BUT WITH ONE EXTRA FOOD PIP IF WE EARNED THE BONUS PIP
			orig.Invoke(self, hud, maxFood + BellyPlus.bonusFood, survivalLimit, pup, pupNumber);
			//self.lastCount += BellyPlus.bonusFood;
			//self.showCount += BellyPlus.bonusFood;
			//self.NewShowCount(self.lastCount);
			// this.eatCircles = survivalLimit;
			//BellyPlus.tomorrowsBonusFood = Mathf.Max(BellyPlus.bonusFood - survivalLimit, 0); //WE MIGHT NOT NEED THIS SINCE FOOD IS SAVED CORRECTLY SOMEHOW
		}
		else
			orig.Invoke(self, hud, maxFood, survivalLimit, pup, pupNumber); // -BellyPlus.bonusFood

		//JUST REAL QUICK
		//if (self.IsPupFoodMeter)
		//	(hud.owner as Player).slugcatStats.maxFood -= 2;

		Debug.Log("-BP FOOD METER DEBUG! BONUS FRUIT!" + hud.owner.GetOwnerType() + " CURRENT FOOD:" + hud.owner.CurrentFood + " BONUS:" + BellyPlus.bonusFood + " " + self.eatCircles);


		//if (hud.owner.GetOwnerType() == HUD.HUD.OwnerType.Player && self.IsPupFoodMeter)
		//{
		//	for (int i = 0; i < 2; i++)
		//	{
		//		self.circles.Add(new FoodMeter.MeterCircle(self, i));
		//		self.circles[i].AddGradient();
		//		self.circles[self.circles.Count - 1].AddCircles();
		//	}
		//}


		if (hud.owner.GetOwnerType() == HUD.HUD.OwnerType.Player && !self.IsPupFoodMeter)
		{
			
			List<AbstractCreature> players = (self.hud.owner as Player).abstractCreature.world.game.session.Players;
			if (players.Count < 2)
            {
				arrowsOn = false;
				//return;
			}
			else
            {
				arrowsOn = true;
				int myPlyrNum = (self.hud.owner as Player).playerState.playerNumber;

                //OKAY THAT'S NOT WORKING AFTER THE UPDATE.. WE HAVE TO DO THIS DUMB THING TO CHECK WHICH PLAYER NUMBER WE ACTUALLY ARE
                for (int j = 0; j < (self.hud.owner as Player).abstractCreature.world.game.cameras.Length; j++)
				{
					if ((self.hud.owner as Player).abstractCreature.world.game.cameras[j].hud == self.hud)
						myPlyrNum = j;

                }
                    
				Debug.Log("CREATING ARROW HUD! PNUM:" + myPlyrNum);

				//ONLY P1 SHOULD RUN THIS (WHAT A MESS...)
				if (myPlyrNum == 0)
					foodArrows = new List<FSprite>();
				
				for (int j = 0; j < (self.hud.owner as Player).abstractCreature.world.game.cameras.Length; j++)
				{
					// if ((self.hud.owner as Player).abstractCreature.world.game.cameras[j].hud == self.hud)
					if ((self.hud.owner as Player).abstractCreature.world.game.cameras[j].hud == self.hud)
                    {
						Debug.Log(" CURRENT CAM:" + j);

						for (int i = 0; i < players.Count; i++)
						{
							Debug.Log("CREATING ARROW HUD PLOOP " + i + " J " + j + " TOTAL " + (i + (j * players.Count)));
							//Color color = PlayerGraphics.SlugcatColor((players[i].state as PlayerState).slugcatCharacter);
							//JollyMeter.PlayerIcon value = new FSprite("Multiplayer_Arrow", true); //JollyMeter.PlayerIcon(this, players[i], color);
							//this.playerIcons.Add(i, value);
							try
							{
								foodArrows.Add(new FSprite("Multiplayer_Arrow", true));
								foodArrows[i + (j * players.Count)].color = PlayerGraphics.SlugcatColor((players[i].state as PlayerState).slugcatCharacter);
								//foodArrows[i].scale = 0.75f;
								//self.fContainer.AddChild(foodArrows[i + (j * players.Count)]);
							}
							catch (Exception e)
                            {
								Debug.Log("CATCH! ARROW ERROR" + e);
							}


							try
							{
								self.fContainer.AddChild(foodArrows[i + (j * players.Count)]);
							}
                            catch (Exception e)
                            {
								Debug.Log("CATCH! ARROW ERROR PT 2" + e);
							}

                            //catch (Exception e)
                            //      {
                            //	Debug.Log("CATCH " + e);
                            //}
                        }
                    }
				}
			}
		}

		//HOLDUP!! DON'T PUT ANYTHING AFTER THIS! THIS WILL OFTEN NEVER GET HERE BC IN SINGLE PLAYER WE RETURN EARLY
		//just kidding we fixed that

		//WE WANT TO BE ABLE TO STORE MORE THAN OUR MAX
		//self.eatCircles -= BellyPlus.bonusFood;//I THOUGHT THIS WORKED. BUT IT DIDN'T -BUT DIDN'T IT WORK? I THOUGHT IT WORKED...
		//1-18-23 DISABLING

		if (!self.IsPupFoodMeter)
        {
			//SET THIS BACK TO 0 FOR NEXT TIME
			bonusFoodMemory = BellyPlus.bonusFood;
			BellyPlus.bonusFood = 0; // Math.Max (BellyPlus.bonusFood - self.eatCircles, 0);
			//4-7-23 OKAY THIS PART IS GOING AWAY
			//BellyPlus.bonusHudPip = BellyPlus.tomorrowsBonusFood * 4; //;

			if (hud.owner.GetOwnerType() == HUD.HUD.OwnerType.CharacterSelect)
			{
				self.sleepScreenPhase = bonusFoodMemory;
				bonusFoodMemory = 0;
            }


			Debug.Log("-BP FOOD METER DEBUG! PT 2- BONUS FRUIT!" + hud.owner.GetOwnerType() + " CURRENT FOOD:" + hud.owner.CurrentFood + " BONUS:" + BellyPlus.bonusFood + " " + self.eatCircles + " MAX " + maxFood);

		}





	}

	public static int GetPrePlayerNum(FoodMeter self)
	{
        //OKAY THAT'S NOT WORKING AFTER THE UPDATE.. WE HAVE TO DO THIS DUMB THING TO CHECK WHICH PLAYER NUMBER WE ACTUALLY ARE
        try
        {
            for (int j = 0; j < (self.hud.owner as Player).abstractCreature.world.game.cameras.Length; j++)
            {
                if ((self.hud.owner as Player).abstractCreature.world.game.cameras[j].hud == self.hud)
                    return j;
            }
        }
        catch (Exception e)
        {
            Debug.Log("CATCH! QUARTER PIP SETUP ERROR" + e);
        }
        
		return -1; //LAME. I WANT TO CRASH THE GAME
    }

	
	// public static List<FoodMeter.MeterCircle> circles;
	public static List<FSprite> bonusCircles;
	public static List<FSprite> bonusCircles2;
	//public static List<FSprite> pupBonusCircles;
	//public static List<FSprite> pupBonusCircles2;
	//AND THEN, SINCE I'M A LAZY BINCH
	public static List<FSprite> bonusCirclesB;
	public static List<FSprite> bonusCircles2B;
	
	public const int maxPupPips = 10; 

    public static void QuarterPipShower_ctor(On.HUD.FoodMeter.QuarterPipShower.orig_ctor orig, HUD.FoodMeter.QuarterPipShower self, FoodMeter owner)
    {
        orig.Invoke(self, owner);
		if (BellyPlus.VisualsOnly())
			return;

        self.owner = owner;

        //(self.owner.hud.owner as Player).playerState.playerNumber
        int myPlyrNum = GetPrePlayerNum(self.owner);
        //Debug.Log("BAR COUNT " + myPlyrNum);

        if (!self.owner.IsPupFoodMeter)
        {
			int pupCircles = 0; // self.owner.pupBars.Count * 3;
			// if (self.owner.pupBars != null)
				// pupCircles = self.owner.pupBars.Count * 3;
			// Debug.Log("PUP BAR COUNT " + pupCircles);
			// OUR PUP METERS HAVEN'T INITIALIZED YET SO THIS WOULD ALWAYS BE 0

			//ONLY P1 SHOULD RUN THIS (WHAT A MESS...)
			if (myPlyrNum == 0)
			{
				bonusCircles = new List<FSprite>();
				bonusCircles2 = new List<FSprite>();
				for (int i = 0; i < (BellyPlus.MaxBonusPips + pupCircles); i++)
				{
					bonusCircles.Add(new FSprite("pixel", true));
					owner.fContainer.AddChild(bonusCircles[i]);
					bonusCircles2.Add(new FSprite("pixel", true));
					owner.fContainer.AddChild(bonusCircles2[i]);
				}
			}

			//...ALRIGHT FINE, AND ONE FOR PLAYER 2
			else if (myPlyrNum == 1)
			{
				bonusCirclesB = new List<FSprite>();
				bonusCircles2B = new List<FSprite>();
				for (int i = 0; i < (BellyPlus.MaxBonusPips + pupCircles); i++)
				{
					bonusCirclesB.Add(new FSprite("pixel", true));
					owner.fContainer.AddChild(bonusCirclesB[i]);
					bonusCircles2B.Add(new FSprite("pixel", true));
					owner.fContainer.AddChild(bonusCircles2B[i]);
				}
			}
		}
		else
        {
			if (myPlyrNum == 0)
			{
				Debug.Log("ADDING PUP BONUS CIRCLES ");
				int startI = BellyPlus.MaxBonusPips + (self.owner.pupNumber - 1) * maxPupPips;
				for (int i = startI; i < (startI + maxPupPips); i++)
				{
					bonusCircles.Add(new FSprite("pixel", true));
					owner.fContainer.AddChild(bonusCircles[i]);
					bonusCircles2.Add(new FSprite("pixel", true));
					owner.fContainer.AddChild(bonusCircles2[i]);
				}
			}
			//AND THEN THE P2 VERSION
			else if (myPlyrNum == 1)
			{
				Debug.Log("ADDING PUP BONUS CIRCLES (FOR THE OTHER HUD)");
				int startI = BellyPlus.MaxBonusPips + (self.owner.pupNumber - 1) * maxPupPips;
				for (int i = startI; i < (startI + maxPupPips); i++)
				{
					bonusCirclesB.Add(new FSprite("pixel", true));
					owner.fContainer.AddChild(bonusCirclesB[i]);
					bonusCircles2B.Add(new FSprite("pixel", true));
					owner.fContainer.AddChild(bonusCircles2B[i]);
				}
			}
		}

		


	}

	
	
	
	public static void BonusPipVisibility(HUD.FoodMeter.QuarterPipShower self, float timeStacker, FSprite mySprite, int circleCount, bool flipped, float alpha, int pipShow)
    {
        mySprite.isVisible = true; //BONUS PIP
		int lastCircle = self.owner.circles.Count - 1;// + circleCount;
		int pipCap = pipShow < (4 * (circleCount + 1)) ? 3 : 2; //IF WE'VE GOT BOTH PIPS ACTIVE, CUT THIS FIRST PIP DOWN TO A HALF PIP SO THEY DONT OVERLAP
		int displayPip = Mathf.Min(pipShow - (circleCount * 4), pipCap);
        //Debug.Log("PIP INFO!" + displayPip + " CAP:" + pipCap + " CIRCLE:" + circleCount + " BONUSPIPS: " + BellyPlus.bonusHudPip);
		mySprite.element = Futile.atlasManager.GetElementWithName("QuarterPips" + displayPip); //OHHH, THESE ARE MANUALLY NAMED VISUAL ASSETS. AND THEY ONLY EXIST FOR THE FIRST 3
		
		// mySprite.alpha = Mathf.Lerp(self.owner.circles[lastCircle].circles[0].lastFade, self.owner.circles[lastCircle].circles[0].fade, timeStacker) * Mathf.Lerp(Mathf.Lerp(0.2f, 0.5f, Mathf.Pow(Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(self.lastQuarterPipSin, self.quarterPipSin, timeStacker) / 20f)), 0.4f)), 0.6f, Mathf.Pow(Mathf.Lerp(self.lastLightUp, self.lightUp, timeStacker), 2f));
		mySprite.scale = 1.0f * (Mathf.Lerp(self.owner.circles[lastCircle].circles[0].lastRad, self.owner.circles[lastCircle].circles[0].rad, timeStacker) / self.owner.circles[lastCircle].circles[0].snapRad);
		mySprite.x = self.owner.circles[lastCircle].DrawPos(timeStacker).x + self.owner.CircleDistance(timeStacker) * (circleCount + 1);
		mySprite.y = self.owner.circles[lastCircle].DrawPos(timeStacker).y;
		mySprite.color = Custom.FadableVectorCircleColors[self.owner.circles[lastCircle].circles[0].color];
		mySprite.alpha = alpha / (pipShow >= (4 * (circleCount + 1)) ? 1 : 2);

		//PUP PIPS
		if (self.owner.IsPupFoodMeter)
        {
			mySprite.scale /= 2f;
			mySprite.color = Color.Lerp(Color.Lerp(mySprite.color, Custom.HSL2RGB(self.owner.pup.npcStats.H, Mathf.Lerp(self.owner.pup.npcStats.S, 1f, 0.8f), self.owner.pup.npcStats.Dark ? 0.3f : 0.7f), 0.5f - (float)self.owner.circles[0].circles[0].color * 0.5f), new Color(0.6f, 0.6f, 0.6f), self.owner.deathFade);
			//Debug.Log("PIP INFO!" + displayPip + " CAP:" + pipCap + " CIRCLE:" + circleCount + " X: " + mySprite.x + " ALPHA: " + mySprite.alpha);
		}
			

		if (flipped)
			mySprite.rotation = 180f; //ROTATE BANANA
	}



    public static void QuarterPipShower_Draw(On.HUD.FoodMeter.QuarterPipShower.orig_Draw orig, HUD.FoodMeter.QuarterPipShower self, float timeStacker)
	{
        //MOVING THIS CHECK UP HERE BECAUSE IT SHOULD ALWAYS CHECK IN CASE OUR PREVIOUS TICK HAD VISIBLE FOOD PIPS!
        //EVERY ONCE IN A WHILE, FLIP ALL OF OUR PIPS BACK OFF IN CASE SOME WEIRDNESS WITH THE HP-BAR MOD LEFT US WITH FLOATING PIPS PAST OUR ACTUAL METER
        if (self.owner.visibleCounter == 1 && bonusCircles != null)
        {
            Debug.Log("RESET ALL PIPS! ");
            //for (int j = 0; j < BellyPlus.MaxBonusPips; j++)
            for (int j = 0; j < bonusCircles.Count; j++) //WHY NOT JUST DO THIS? IDIOT
            {
                bonusCircles[j].isVisible = false;
                bonusCircles2[j].isVisible = false;
            }
        }

        //RUN THIS PART IF FULL. (JUST A DUPLICATE OF THE EXISTING ONE, WITH A FEW TWEAKS)
        if (self.owner.showCount >= self.owner.maxFood && !BellyPlus.VisualsOnly()) //MAXFOOD SHOULD BE COMING FROM SLUGCATSTATS
		{
			int lastCircle = self.owner.circles.Count - 1;
			
			
			//ALRIGHT ALRIGHT, LETS DO THINGS THE RIGHT WAY NOW
			float pipAlpha = 0;
			//ALPHA SHOULD ALWAYS BE 0 UNLESS THE HUD IS VISIBLE, DUMMY
			if (self.owner.circles[lastCircle].circles[1].sprite.isVisible)
				pipAlpha = self.owner.circles[lastCircle].circles[1].sprite.alpha;
			
			bool firstHud = ((self.owner.hud.owner as Player).abstractCreature.world.game.cameras[0].hud == self.owner.hud);
			//bool primeHud = ((self.owner.hud.owner as Player).abstractCreature.world.game.cameras[0].hud == self.owner.hud);

			if (self.owner.IsPupFoodMeter)
            {
				int foodCount = ( self.owner.pup.CurrentFood - self.owner.pup.MaxFoodInStomach) * 2;
				//Debug.Log("IM A PUP! HERES MY HUD " + self.owner.pupNumber + "FOOD" + self.owner.pup.CurrentFood + " (bonus)foodCount: " + foodCount);
				int startJ = BellyPlus.MaxBonusPips + (self.owner.pupNumber - 1) * maxPupPips;
				for (int j = startJ; j < (startJ + maxPupPips); j++)
                {
					int pipCnt = j - startJ ;
					if (firstHud)
					{
						bonusCircles[j].isVisible = false;
						bonusCircles2[j].isVisible = false;
						//Debug.Log("IM A PUP! HERES MY JS " + j + " START " + startJ + " pipCnt: " + pipCnt);
						if (foodCount >= 1 + (4 * pipCnt))
							BonusPipVisibility(self, timeStacker, bonusCircles[j], pipCnt + 0, false, pipAlpha, foodCount);
						if (foodCount >= 4 + (4 * pipCnt))
							BonusPipVisibility(self, timeStacker, bonusCircles2[j], pipCnt + 0, true, pipAlpha, foodCount);
						else
							break;
					}
					else
					{
						bonusCirclesB[j].isVisible = false;
						bonusCircles2B[j].isVisible = false;
						//Debug.Log("IM A PUP! HERES MY JS " + j + " START " + startJ + " pipCnt: " + pipCnt);
						if (foodCount >= 1 + (4 * pipCnt))
							BonusPipVisibility(self, timeStacker, bonusCirclesB[j], pipCnt + 0, false, pipAlpha, foodCount);
						if (foodCount >= 4 + (4 * pipCnt))
							BonusPipVisibility(self, timeStacker, bonusCircles2B[j], pipCnt + 0, true, pipAlpha, foodCount);
						else
							break;
					}
				}

			}

			else
            {
				//OKAY MAYBE OUR HUD ONLY SHOWS UP TO 20 BUT WE KEEP TRACKING MORE THAN THAT
				int showBonusHudPips = Math.Min(BellyPlus.bonusHudPip, BellyPlus.MaxBonusPips * 4);
				
				
				for (int j = 0; j < BellyPlus.MaxBonusPips; j++) //bonusCircles.Count
				{
					// bool firstHud = ((self.owner.hud.owner as Player).abstractCreature.world.game.cameras[0].hud == self.owner.hud);
					// Debug.Log("FIRST HUD? " + firstHud + " MYHUD: " + self.owner.hud);
					if (firstHud)
					{
						bonusCircles[j].isVisible = false;
						bonusCircles2[j].isVisible = false;

						if (showBonusHudPips >= 1 + (4 * j))
							BonusPipVisibility(self, timeStacker, bonusCircles[j], j, false, pipAlpha, showBonusHudPips);
						if (showBonusHudPips >= 4 + (4 * j))
							BonusPipVisibility(self, timeStacker, bonusCircles2[j], j, true, pipAlpha, showBonusHudPips);
						else
							break; //SAVE RESOURCES I GUESS
								   //Debug.Log("PIP INFO! J: " + j + " BONUSPIPS: " + showBonusHudPips);
					}

					//THE LAZY DUPLICATE WAY FOR CAMERA 2 AND UP (OR REALLY JUST THE LAST CAMERA ATM...
					else
					{
						bonusCirclesB[j].isVisible = false;
						bonusCircles2B[j].isVisible = false;

						if (showBonusHudPips >= 1 + (4 * j))
							BonusPipVisibility(self, timeStacker, bonusCirclesB[j], j, false, pipAlpha, showBonusHudPips);
						if (showBonusHudPips >= 4 + (4 * j))
							BonusPipVisibility(self, timeStacker, bonusCircles2B[j], j, true, pipAlpha, showBonusHudPips);
						else
							break; //SAVE RESOURCES I GUESS
					}


				}
			}
			
			
			
			//THEN DO THE NORMAL QUARTERPIP THING. EVEN THOUGH THIS SHOULD DO NOTHING AT THIS POINT ANYWAYS
			orig.Invoke(self, timeStacker);
		}
		else
		{
			orig.Invoke(self, timeStacker);
		}
	}
}