using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Menu.Remix;
using MoreSlugcats;
using UnityEngine;

namespace Expedition
{
	public class FoodScoreChallenge : Challenge
	{
		public override void UpdateDescription()
		{
			this.description = ChallengeTools.IGT.Translate("Eat <score_target> points worth of food [<current_score>/<score_target>]").Replace("<score_target>", ValueConverter.ConvertToString<int>(this.target)).Replace("<current_score>", ValueConverter.ConvertToString<int>(this.score));
			base.UpdateDescription();
		}


		public override bool Duplicable(Challenge challenge)
		{
			return !(challenge is FoodScoreChallenge);
		}


		public override string ChallengeName()
		{
			// return ChallengeTools.IGT.Translate("Overall Score");
			return ChallengeTools.IGT.Translate("Food Score");
		}


		public override Challenge Generate()
		{
			// int num = Mathf.RoundToInt(Mathf.Lerp(65f, 135f, ExpeditionData.challengeDifficulty) / 10f) * 10;
			int num = Mathf.RoundToInt(Mathf.Lerp(75f, 200f, ExpeditionData.challengeDifficulty) / 10f) * 10;
			return new FoodScoreChallenge
			{
				target = num
			};
		}


		public override void Reset()
		{
			this.score = 0;
			this.increase = 0;
			base.Reset();
		}


		public override int Points()
		{
			float num = 1f;
			if (ModManager.MSC && (ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Saint
			|| ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Spear))
			{
				num = 1.35f;
			}
			return (int)((float)(this.target / 2) * num) * (int)(this.hidden ? 2f : 1f);
		}



		public override void Update()
		{
			base.Update();
			//if (this.game != null && this.game.rainWorld.progression.currentSaveState != null)
			//int num = this.game.GetStorySession.saveState.totFood;
			
			if (this.score != this.game.GetStorySession.saveState.totFood)
			{
				this.score = this.game.GetStorySession.saveState.totFood;
				this.UpdateDescription();
			}
			
			if (this.score >= this.target)
			{
				this.CompleteChallenge();
			}
		}
		
		
		
		
		public void FoodEated(int add, int playerNumber)
		{
			if (this.completed || this.game == null || add == 0)
			{
				return;
			}
			
			
			//int points = ChallengeTools.expeditionCreatures.Find((ChallengeTools.ExpeditionCreature f) => f.creature == type).points;
			int points = add;
			this.score += points;
			ExpLog.Log(string.Concat(new string[]
			{
				"Player ",
				(playerNumber + 1).ToString(),
				" eated ",
				add.ToString(),
				" | +",
				points.ToString()
			}));
			
			this.UpdateDescription();
			if (this.score >= this.target)
			{
				this.score = this.target;
				this.CompleteChallenge();
			}
		}
		

		public override string ToString()
		{
			return string.Concat(new string[]
			{
				"FoodScoreChallenge",
				"~",
				ValueConverter.ConvertToString<int>(this.score),
				"><",
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
				this.score = int.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
				this.target = int.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
				this.completed = (array[2] == "1");
				this.hidden = (array[3] == "1");
				this.revealed = (array[4] == "1");
				this.UpdateDescription();
			}
			catch (Exception ex)
			{
				ExpLog.Log("ERROR: FoodScoreChallenge FromString() encountered an error: " + ex.Message);
			}
		}
		
		public int target;
		
		public int score;
		
		public int increase;
		
		public int[] killScores;
	}
}
