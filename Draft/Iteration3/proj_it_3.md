# Project Iteration 3

## Goal
Add Asteroids that interact with the spaceship.

For the third iteration, our goal was to put the final touches to our haptically enabled game.

Ken was tasked to add a hotkey to turn the haptics on and off and along with Anchit they decided to add arrows and text to show the acting forces on the ship and what keys to press for slingshot and thrustor mode.

Ancgit was tasked add extra haptic feedback, and a tutorial scene.
Punit decided to add an HUD that included a Timer, Fuel Remaining etc.

Finally, I was tasked with fixing the simulation step for rendering the gravitational forces on the haply, adding the logic for a crossfade slider between the force of the thrustors and gravitational forces being felt through the Haply. Finally I also decided to add asteroids in to give the player something to dodge. Each time an asteroid hits  the ship, some fuel is lost. 

## Achievements

To view others results view included iteration posts in appendix.

* Simulation step for rendering graviational forces which means
	* 	creating a selector to audition the gravity of each planet using the left and right arrow keys when in free movement mode and
	* adding rendering the forces of the planets on the spaceship when in released mode, the mode that comes after being sent flying from the slingshot.
	* Finally I added a crossfader equation between the force of the planets gravity and the thrusters when in released mode.

* Addition of asteroids:
	* Added the code to randomly generate asteroids based on a prefab asteroid and some scripts see code for more information.
	* Added collision layers so that the space ship in the slingshot scenario does not come in collision with the asteroids until on after it  is realeased.
	* Made it so that they are destroyed if they hit a planet.
	* Adjusted size and texture of the 'roids
	* Added an asteroid sprite to them.

## Discussion

Unfortunatey, most of my work was done away from my hapley, and Montreal was hit with a horrible freezing rain storm that cripled all the island execpt for downtown as the powerlines are underground instead of being on telephone poles. So fortunately I was able to go to my lab to do most of the work. On that point, fixing the gravity was crucial in having the game perform as we would more of less want since the goal was to simulate gravity. Since the generation of gravity uses loops to iterate through the planets to add together their accelerations, it was not suited to have that be done in the simulation steps of the haply and instead be done in it's own process. As such, to add the calculated gravitational forces into the end-effectors net force variable, I simply needed to store the gavity value in a discoverable spot to be accessed during the Haplys' simulation step. I would comment that it is not the best implementation in terms of security. 

When it comes to the asteroids, they add a bit of a bullet dodge mechanic to the game and having the player loose fuel for every collision will give them a sense of urgency to reach the end. This of course is only implemented for the released mode. When the player is in free movement, they would most likely want to simply observe how the planets move and interact with everything. Therefore, turning off collisions with the asteroids is probably best in this situation. Now, in order to avoid the asteroids from colliding with the ship when it is stationary in the slingshot, I added collision layers where when static, the ship is on a layer that does not interact with the asteroids but when in release mode switches to the layer that allows for interactions. 

Another challenge was with how the planets interacted with the asteroids. Since the planets physics are not being handeled by a Rigidbody object and instead being directly computed within the script, the asteroids were passing over them without collisions. So a simple solution was to a __sperical collider__ to the planets gameObject to serve as a collision trigger. Like that, when the asteroids collide with the planets, they are destroyed. 

## Future Steps

Now that the last additions are being made, the only things I believe that are needed to be done is the writing of the final report and tuning of the games variables like the gravitational force as it is always being modified. 

