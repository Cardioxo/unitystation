﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NPC
{
	/// <summary>
	/// Magical dog AI brain for corgis!
	/// Used for all corgis, remember to set the name of the
	/// dog in inspector.
	/// *All logic should be server side.
	/// </summary>
	public class CorgiAI : GenericFriendlyAI
	{
		//Set this inspector. The corgi will only respond to
		//voice commands from these job types:
		public List<JobType> allowedToGiveCommands = new List<JobType>();

		//TODO: later we can make it so capt or hop can tell the dog to
		//respond to commands from others based on their names

		private ConeOfSight coneOfSight;
		private LayerMask mobMask;
		private string dogName;

		protected override void Awake()
		{
			base.Awake();
			dogName = mobName.ToLower();
			ResetBehaviours();
		}

		public override void OnEnable()
		{
			base.OnEnable();
			mobMask = LayerMask.GetMask("Walls", "NPC");
			coneOfSight = GetComponent<ConeOfSight>();
		}

		private void SingleBark(GameObject barked = null)
		{
			SoundManager.PlayNetworkedAtPos("Bark",
				gameObject.transform.position,
				Random.Range(.8F, 1.3F));

			if (barked != null)
			{
				Chat.AddActionMsgToChat(barked, $"{mobNameCap} barks at you!",
					$"{mobNameCap} barks at {barked.ExpensiveName()}");
			}
			else
			{
				Chat.AddActionMsgToChat(gameObject, $"{mobNameCap} barks!", $"{mobNameCap} barks!");
			}
		}

		IEnumerator RandomBarks(GameObject barked = null)
		{
			for (int barkAmt = Random.Range(1, 4); barkAmt > 0; barkAmt--)
			{
				SingleBark(barked);
				yield return WaitFor.Seconds(Random.Range(0.4f, 1f));
			}

			yield return WaitFor.EndOfFrame;
		}

		public override void LocalChatReceived(ChatEvent chatEvent)
		{
			ProcessLocalChat(chatEvent);
			base.LocalChatReceived(chatEvent);
		}

		void ProcessLocalChat(ChatEvent chatEvent)
		{
			var speaker = PlayerList.Instance.Get(chatEvent.speaker);

			if (speaker.Script == null) return;
			if (speaker.Script.playerNetworkActions == null) return;

			if (speaker.Job == JobType.CAPTAIN || speaker.Job == JobType.HOP)
			{
				StartCoroutine(PerformVoiceCommand(chatEvent.message.ToLower(), speaker));
			}
		}

		IEnumerator PerformVoiceCommand(string msg, ConnectedPlayer speaker)
		{
			//We want these ones to happen right away:
			if (msg.Contains($"{dogName} run") || msg.Contains($"{dogName} get out of here"))
			{
				StartFleeing(speaker.GameObject, 10f);
				yield break;
			}

			if (msg.Contains($"{dogName} stay") || msg.Contains($"{dogName} sit")
			                                    || msg.Contains($"{dogName} stop"))
			{
				ResetBehaviours();
				yield break;
			}

			//Slight delay for the others:
			yield return WaitFor.Seconds(0.5f);

			if (msg.Contains($"{dogName} come") || msg.Contains($"{dogName} follow")
			                                    || msg.Contains($"come {dogName}"))
			{
				if (Random.value > 0.8f)
				{
					yield return StartCoroutine(ChaseTail(1));
				}
				else
				{
					SingleBark();
				}

				FollowTarget(speaker.GameObject.transform);
				yield break;
			}

			if (msg.Contains($"{dogName} find food") || msg.Contains($"{dogName} explore"))
			{
				if (Random.value > 0.8f)
				{
					yield return StartCoroutine(ChaseTail(2));
				}
				else
				{
					SingleBark();
				}

				BeginExploring();
				yield break;
			}
		}

		//TODO: Do extra stuff on these events, like barking when being told to sit:
		protected override void OnExploringStopped()
		{
			StartCoroutine(RandomBarks());
		}

		protected override void OnFleeingStopped()
		{
			StartCoroutine(RandomBarks());
		}

		protected override void OnFollowStopped()
		{
			StartCoroutine(RandomBarks());
		}

		public override void OnPetted(GameObject performer)
		{
			base.OnPetted(performer);

			int randAction = Random.Range(1,6);

			switch (randAction)
			{
				case 1:
					StartCoroutine(ChaseTail(Random.Range(1,3)));
					break;
				case 2:
					RandomBarks();
					break;
				case 3:
					Chat.AddActionMsgToChat(gameObject, $"{mobNameCap} wags its tail!", $"{mobNameCap} wags its tail!");
					break;
				case 4:
					Chat.AddActionMsgToChat(
						performer,
						$"{mobNameCap} licks your hand!",
						$"{mobNameCap} licks {performer.ExpensiveName()}'s hand!");
					break;
				case 5:
					Chat.AddActionMsgToChat(
						performer,
						$"{mobNameCap} gives you its paw!",
						$"{mobNameCap} gives his paw to {performer.ExpensiveName()}");
					break;
			}
		}

		protected override void OnAttackReceived(GameObject damagedBy)
		{
			SingleBark();
			StartFleeing(damagedBy);
		}

		CatAI AnyCatsNearby()
		{
			var hits = coneOfSight.GetObjectsInSight(mobMask, dirSprites.CurrentFacingDirection, 10f, 5);
			foreach (Collider2D coll in hits)
			{
				if (coll.gameObject != gameObject && coll.gameObject.GetComponent<CatAI>() != null
				                                  && !coll.gameObject.GetComponent<HealthSystem>().IsDead)
				{
					return coll.gameObject.GetComponent<CatAI>();
				}
			}
			return null;
		}

		void BarkAtCats(CatAI cat)
		{
			float chase = Random.value;
			// RandomBarks(cat.gameObject);
			SingleBark(cat.gameObject);

			//Make the cat flee!
			cat.RunFromDog(gameObject.transform);
			FollowTarget(cat.gameObject.transform, 5f);
			RandomBarks();
		}

		protected override void DoRandomAction()
		{
			// Bark at cats!
			var possibleCat = AnyCatsNearby();
			if (possibleCat != null)
			{

				BarkAtCats(possibleCat);
				return;
			}

			int randAction = Random.Range(1, 5);
			switch (randAction)
			{
				case 1:
					StartCoroutine(ChaseTail(Random.Range(1, 5)));
					break;
				case 2:
					NudgeInDirection(GetNudgeDirFromInt(Random.Range(0, 8)));
					break;
				case 3:
					RandomBarks();
					break;
				case 4:
					Chat.AddActionMsgToChat(
						gameObject,
						$"{mobNameCap} wags its tail!",
						$"{mobNameCap} wags its tail!");
					break;
			}
		}
	}
}