# Ten Minute Physics in Unity

Implementations in Unity of the YouTube channel [Ten Minute Physics](https://matthias-research.github.io/pages/tenMinutePhysics/).


## 1-2. Cannon ball

Simulate a bouncy cannon ball.

<img src="/_media/01-bouncy-ball.png" width="400">

## 3. Billiard

Simulate billiard balls with different size and mass. Watch this YouTube video for examples: 

[![Link to youtube video 1000 billiard balls](https://img.youtube.com/vi/ErsK2osLkQU/0.jpg)](https://www.youtube.com/watch?v=ErsK2osLkQU)


## 4. Pinball

Simulate a pinball game.

<img src="/_media/04-pinball.png" width="400">


## 5. Beads on wire

Simulate beads attached to a circular wire.

<img src="/_media/05-beads-on-wire.png" width="400">


## 6. Pendulums

Simulate the chaotic behavior of pendulums with as many arms as you want and where each arm can have different mass. With many arms you get a rope or hair! Watch these YouTube videos for examples: 

[![Link to youtube video butterfly effect](https://img.youtube.com/vi/GqGHz6gtakY/0.jpg)](https://www.youtube.com/watch?v=GqGHz6gtakY)

[![Link to youtube video double pendulum](https://img.youtube.com/vi/jMDiP7mhEx0/0.jpg)](https://www.youtube.com/watch?v=jMDiP7mhEx0)


## 7. 3d Vector Math for Simulation

Nothing to simulate


## 8. Interaction

Catch and throw a ball with your mouse.

<img src="/_media/08-user-interaction.png" width="400">


## 9. Getting ready to simulate the world with XPBD

Nothing to simulate


## 10. Soft body physics

Simple unbreakable soft body bunny physics. You can flatten it and throw it around with your mouse. 

<img src="/_media/10-soft-body.gif" width="400">


## 11. Find overlaps among objects

Find overlaps among thousands of objects blazing fast. Implements a version of the [Spatial Partitioning design pattern](https://github.com/Habrador/Unity-Programming-Patterns) called "Spatial Hashing" which is really useful if you have an unbounded grid. 

<img src="/_media/11-spatial-hashing.png" width="400">


## 12. Optimized soft body physics (TODO)

Is not optimizing the code from #11, but is showing how you can use a more detailed mesh and make that faster. You use two meshes: one with fewer triangles that is tetrahedralized, and one with more triangles, and then they interract during the simulation.  


## 13. Tetrahedralizer (TODO)

Implemetation of an algorithm that splits a mesh into tetrahedrons. Will be available here when finished: [Open source Computational Geometry library](https://github.com/Habrador/Computational-geometry)


## 14. Cloth simulation

Basic cloth simulation.

<img src="/_media/14-cloth-simulation.gif" width="400">


## 15. Cloth self-collisions

TODO


## 16. Simulation on the GPU

TODO


## 17. Eulerian Fluid Simulator with 200 lines of code

Spoiler: It's just the simulation part that's 200 lines of code. You need a few more lines of code to set it up, display it on screen, etc. Watch this YouTube video for examples: 

[![Link to youtube video fluid simulation](https://img.youtube.com/vi/6Jw1CsTOkDg/0.jpg)](https://www.youtube.com/watch?v=6Jw1CsTOkDg)


## 18. FLIP Water Simulator

TODO


## 19. Differential equations and calculus from scratch

Nothing to simulate

	
## 20. Height-field water simulation

Simulate a swimming pool with balls. 

<img src="/_media/20-heightfield.gif" width="400">


## 21. Fire simulator

TODO


## 22. Basic rigid body simulator

Simulating rigid bodies using XPBD. 

<img src="/_media/22-basic-rb-simulator.png" width="400">


## 23. Sweep and Prune collision detection

A fast way to check collisions between many objects by checking each object's Axis-Aligned Bounding Box (AABB). Is related to "11. Find overlaps among objects" but can handle objects with different sizes.  

<img src="/_media/23-sweep-and-prune.png" width="400">


# Bonus

Bonus implementations related to the code above.


## 3-Body Problem

Simulation of planetary orbits based on this famous unsolved problem (https://en.wikipedia.org/wiki/Three-body_problem). Watch this YouTube video for examples: 

[![Link to youtube video 3-body problem](https://img.youtube.com/vi/o5HWPeP-JS4/0.jpg)](https://www.youtube.com/watch?v=o5HWPeP-JS4)


## Calculate pi

Calculate decimals in pi by bouncing boxes with different mass and calculate collisions. Inspired by 3Blue1Bown's YouTube video: [The most unexpected answer to a counting puzzle](https://www.youtube.com/watch?v=HEfHFsfGXjs).

<img src="/_media/bonus-calculate-pi.png" width="400">