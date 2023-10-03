using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Menu.Remix;
using MoreSlugcats;
using UnityEngine;
using RWCustom;
using HUD;
using Rewired.ControllerExtensions;

namespace Expedition
{
	
	public class CycleFoodChallenge : Challenge
	{
		
		public override void UpdateDescription()
		{
			int value = this.completed ? this.target : this.score;
			this.description = ChallengeTools.IGT.Translate("Hibernate with <score_target> bonus food pips this cycle [<current_score>]").Replace("<score_target>", ValueConverter.ConvertToString<int>(this.target)).Replace("<current_score>", ValueConverter.ConvertToString<int>(value));
			if (this.cornDisqualify)
			{
				string prefix = (this.flashTimer <= 15) ? ChallengeTools.IGT.Translate(" !!!!!!!!! ") : ChallengeTools.IGT.Translate(" ______ ");
                this.description += prefix + ChallengeTools.IGT.Translate(" - Hibernating with popcorn will DISQUALIFY this challenge") + prefix;
            }
			base.UpdateDescription();
		}

		public override bool Duplicable(Challenge challenge)
		{
			return !(challenge is CycleFoodChallenge);
		}

		public override void Reset()
		{
			this.score = 0;
			base.Reset();
		}

		public override string ChallengeName()
		{
			return ChallengeTools.IGT.Translate("Cycle Food Score");
		}

		public override Challenge Generate()
		{
			int num = (int)Mathf.Lerp(5f, 15f, ExpeditionData.challengeDifficulty);
			if (ExpeditionGame.activeUnlocks.Contains("bur-rotund"))
				num -= 3;

			if (ModManager.MSC && !(ExpeditionGame.activeUnlocks.Contains("unl-foodlover") || BPOptions.foodLoverPerk.Value || (ModManager.JollyCoop && Custom.rainWorld.options.JollyPlayerCount > 1)) 
				&& (ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Spear || ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Saint))
			{
				num /= 2;
			}

			return new CycleFoodChallenge
			{
				target = num
			};
		}

		public override int Points()
		{
			float num = 1f;
			float baseTarg = this.target;
			if (ExpeditionGame.activeUnlocks.Contains("bur-rotund"))
				baseTarg += 3;
			return (int)((float)(baseTarg * 3) * num) * (int)(this.hidden ? 2f : 1f);
		}

		
		public override void Update()
		{
			base.Update();

			if (this.score != BellyPlus.bonusFood)
			{
				this.score = BellyPlus.bonusFood;
				 this.UpdateDescription();
			}

            if (this.flashTimer > 0)
                this.flashTimer--;


            if (this.game?.cameras[0].room?.shelterDoor != null)
			{
                this.UpdateDescription();
                //CHECK IF THERE'S CORN IN OUR SHELTER
                //Debug.Log("CHECKING FOR CORN");
                Room thisRoom = this.game.cameras[0].room;
				for (int j = 0; j < thisRoom.abstractRoom.entities.Count; j++)
                {
                    if (!this.hidden && (thisRoom.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject != null && (thisRoom.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject is SeedCob myCobb)
                    {
                        if (myCobb.open >= 1) //!myCobb.AbstractCob.dead && 
                        {
							this.cornDisqualify = true;
							//Debug.Log("FOUND A CORN");
						}
                    }
                }

				if (this.cornDisqualify && this.flashTimer <= 0)
				{
					//Debug.Log("TADAAA");
					this.flashTimer = 30; //DANG THIS SUCKED
					//this.hidden = true;
					//this.revealed = true;
				}


				if (!this.cornDisqualify && this.game.cameras[0].room.shelterDoor.IsClosing && this.score >= this.target)
					this.CompleteChallenge();
				return;
			}
			else
				this.cornDisqualify = false;

            
        }


		public override string ToString()
		{
			return string.Concat(new string[]
			{
				"CycleFoodChallenge",
				"~",
				ValueConverter.ConvertToString<int>(this.target),
				"><",
				this.completed ? "1" : "0",
				"><",
				this.hidden ? "1" : "0",
				"><",
				this.revealed ? "1" : "0"
			});
		}


		public override void FromString(string args)
		{
			try
			{
				string[] array = Regex.Split(args, "><");
				this.target = int.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
				this.completed = (array[1] == "1");
				this.hidden = (array[2] == "1");
				this.revealed = (array[3] == "1");
				if (!this.completed)
				{
					this.score = 0;
				}
				this.score = 0;
				this.UpdateDescription();
			}
			catch (Exception ex)
			{
				ExpLog.Log("ERROR: CycleFoodChallenge FromString() encountered an error: " + ex.Message);
			}
		}

		public int target;

		public int score;

		public int increase;

		public int[] killScores;
		
		//A FEW EXTRA
		public bool cornDisqualify;
        public int flashTimer;
    }
}
