# animated_flock_perlin_terrain
This is a Unity project that generates a perlin-noise terrain with day-night cycles and an animated boid flock overhead.   

The terrain (**MyPlane.cs**) is randomly generated using a perlin noise patch (**PerlinGenerator.cs**)to determine the height of each point on a plane.  Prefabricated houses and trees have been scattered randomly on the terrain such that they do not overlap with each other and they are angled to align with the normal of the terrain below them (**MyPlane.cs**). There is a day-night cycle with a sun and moon (**DayNightCycle.cs**).

**myShader.shader**  
The terrain is coloured according to height in order to give the appearance of ocean, beach, forest, and snow-capped mountains. Sine waves are used to create a ripple effect in the ocean. The saturation of the colours of the terrain and the models depends on the distance from the camera so that things further away appear desaturated. This is accomplished by converting RGB to HSL, adjusting the S value according to the depth, and converting back from HSL to RGB.

**myFlock.cs**  
There is a flock of 50 boids (prefabricated cheese wedges) flying overhead. The boids have a leader that follows a hermite spline generated using 8 control points above the terrain and the other boids exihibit flock behaviour (alignment, separation, and cohesion). myFlock.cs uses the perlin noise patch to make sure the boids avoid colliding with the terrain. Hailstones fall at random angles. If a boid is hit by a hailstone, it gets pushed slightly in the direction of the hailstone's trajectory.

When the boolean predatorOn is true, a predator boid (a prefabricated watermelon slice) chases the nearest boid. The boids' flock behaviour changes such that they attempt to avoid the predator. When the predator reaches a boid, it "eats" it: the boid disappears and a new boid is generated elsewhere. 


