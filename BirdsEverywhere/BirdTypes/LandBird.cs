using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;

namespace BirdsEverywhere.BirdTypes
{
	class LandBird : CustomBirdType
	{
		public override string birdTypeName { get; } = "LandBird";
		public float flightOffset { get; set; }
		public int walkTimer { get; set; }

		
		public LandBird() { }

		public LandBird(int tileX, int tileY, string birdName, BehaviorStatus state = BehaviorStatus.Pecking)
			: base(0, tileX, tileY, birdName)
		{
			flip = random.NextDouble() < 0.5;
			position.X += 32f;
			position.Y += 32f;
			startingPosition = position;
			flightOffset = (float)random.NextDouble() - 0.5f;

			this.state = state;
		}


		// ##############################
		// # End of Animation Behaviors #
		// ##############################

		public void hop(Farmer who)
		{
			gravityAffectedDY = -2f;
		}

		private void donePecking(Farmer who)
		{
			state = ((!(random.NextDouble() < 0.5)) ? BehaviorStatus.Stopped : BehaviorStatus.Pecking);
		}

		private void playFlap(Farmer who)
		{
			if (Utility.isOnScreen(position, 64))
			{
				Game1.playSound("batFlap");
			}
		}

		private void playPeck(Farmer who)
		{
			if (Utility.isOnScreen(position, 64))
			{
				Game1.playSound("shiny4");
			}
		}

		// ########################
		// # Behavior / Animation #
		// ########################

		public override bool update(GameTime time, GameLocation environment)
		{
			if (yJumpOffset < 0f && state != BehaviorStatus.FlyingAway)
			{
				if (!flip && !environment.isCollidingPosition(getBoundingBox(-2, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true))
				{
					position.X -= 2f;
				}
				else if (!environment.isCollidingPosition(getBoundingBox(2, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true))
				{
					position.X += 2f;
				}
			}
			characterCheckTimer -= time.ElapsedGameTime.Milliseconds;
			if (characterCheckTimer < 0)
			{
				Character f = Utility.isThereAFarmerOrCharacterWithinDistance(position / 64f, 4, environment);
				characterCheckTimer = 200;
				if (f != null && state != BehaviorStatus.FlyingAway)
				{
					if (random.NextDouble() < 0.85)
					{
						Game1.playSound("SpringBirds");
					}
					state = BehaviorStatus.FlyingAway;
					addBirdObservation(this.birdName, environment.Name);

					if (f.Position.X > position.X)
					{
						flip = false;
					}
					else
					{
						flip = true;
					}
					sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame((short)(baseFrame + 6), 70),
						new FarmerSprite.AnimationFrame((short)(baseFrame + 7), 60, secondaryArm: false, flip, playFlap),
						new FarmerSprite.AnimationFrame((short)(baseFrame + 8), 70),
						new FarmerSprite.AnimationFrame((short)(baseFrame + 7), 60)
					});
					sprite.loop = true;
				}
			}
			switch (state)
			{
				case BehaviorStatus.Pecking:
					if (sprite.CurrentAnimation == null)
					{
						List<FarmerSprite.AnimationFrame> peckAnim = new List<FarmerSprite.AnimationFrame>();
						peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 2), 480));
						peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 170, secondaryArm: false, flip));
						peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 4), 170, secondaryArm: false, flip));
						int pecks = random.Next(1, 5);
						for (int i = 0; i < pecks; i++)
						{
							peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 70));
							peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 4), 100, secondaryArm: false, flip, playPeck));
						}
						peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 100));
						peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 2), 70, secondaryArm: false, flip));
						peckAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + 1), 70, secondaryArm: false, flip));
						peckAnim.Add(new FarmerSprite.AnimationFrame((short)baseFrame, 500, secondaryArm: false, flip, donePecking));
						sprite.loop = false;
						sprite.setCurrentAnimation(peckAnim);
					}
					break;
				case BehaviorStatus.FlyingAway:
					if (!flip)
					{
						position.X -= 6f;
					}
					else
					{
						position.X += 6f;
					}
					yOffset -= 2f + flightOffset;
					break;
				case BehaviorStatus.Sleeping:
					if (sprite.CurrentAnimation == null)
					{
						sprite.currentFrame = baseFrame + 5;
					}
					if (random.NextDouble() < 0.003 && sprite.CurrentAnimation == null)
					{
						state = BehaviorStatus.Stopped;
					}
					break;
				case BehaviorStatus.Walking:
					if (!flip && !environment.isCollidingPosition(getBoundingBox(-1, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true))
					{
						position.X -= 1f;
					}
					else if (flip && !environment.isCollidingPosition(getBoundingBox(1, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true))
					{
						position.X += 1f;
					}
					walkTimer -= time.ElapsedGameTime.Milliseconds;
					if (walkTimer < 0)
					{
						state = BehaviorStatus.Stopped;
						sprite.loop = false;
						sprite.CurrentAnimation = null;
						sprite.currentFrame = baseFrame;
					}
					break;
				case BehaviorStatus.Stopped:
					if (random.NextDouble() < 0.008 && sprite.CurrentAnimation == null && yJumpOffset >= 0f)
					{
						switch (random.Next(6))
						{
							case 0:
								state = BehaviorStatus.Sleeping;
								break;
							case 1:
								state = BehaviorStatus.Pecking;
								break;
							case 2:
								hop(null);
								break;
							case 3:
								flip = !flip;
								hop(null);
								break;
							case 4:
							case 5:
								state = BehaviorStatus.Walking;
								sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
						{
							new FarmerSprite.AnimationFrame((short)baseFrame, 100),
							new FarmerSprite.AnimationFrame((short)(baseFrame + 1), 100)
						});
								sprite.loop = true;
								if (position.X >= startingPosition.X)
								{
									flip = false;
								}
								else
								{
									flip = true;
								}
								walkTimer = random.Next(5, 15) * 100;
								break;
						}
					}
					else if (sprite.CurrentAnimation == null)
					{
						sprite.currentFrame = baseFrame;
					}
					break;
			}
			return base.update(time, environment);
		}
	}
}
