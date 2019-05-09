# animated_flock_perlin_terrain
This is a Unity project that generates a perlin-noise terrain with day-night cycles and an animated boid flock overhead. 

The terrain is randomly generated using a perlin noise patch to determine the height of each point on a plane. The terrain is coloured according to height in order to give the appearance of ocean, beach, forest, and snow-capped mountains. The ocean ripples. Prefabricated models have been scattered randomly on the terrain such that they do not overlap. The models are angled to align with the normal of the terrain below them. The saturation of the colours of the terrain and the models depends on the distance from the camera so that things further away appear desaturated. There is a day-night cycle with a sun, moon, and clouds.

There is a flock of boids (prefabricated cheese wedges) flying overhead. The pointy end of the cheese wedge aligns with their direction of motion. The boids' leader follows a hermite spline generated using 8 control points above the terrain. The boids avoid crashing into the terrain. Hailstones fall at random angles and hit the boids, slightly pushing them along the trajectory of the hailstones. A predator boid (a prefabricated watermelon slice) chases the nearest boid until it eats it. When a boid is eaten, a new boid is generated elsewhere to replace it.


