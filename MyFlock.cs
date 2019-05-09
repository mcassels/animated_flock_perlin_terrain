using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assignment03
{
    public class MyFlock : MonoBehaviour
    {
        public Vector3[] knotPositions;
        public Vector3[] knotTangents;
        public List<Vector3> pointsOnSpline;
        public List<Vector3> evenlySpacedPointsOnSpline;
        public int numPointsOnSpline;
        public int leaderPosition;
        public GameObject leader;
        public GameObject[] flockBoids;
        public GameObject predator;
        public Vector3[] flockBoidVelocities;
        int numBoids;
        float visionSize; //how far each boid can see in every direction
        float availableThrust; //how much thrust for each boid
        float collisionWeight, alignmentWeight, cohesionWeight, escapePredatorWeight;
        Vector3 predatorVelocity;
        int indexOfCurrentPrey;
        Boolean predatorOn;
        Boolean hailOn;
        public List<GameObject> hailstones;
        public List<Vector3> hailstoneDirections;

        //terrain dimensions
        float width;
        float height;
        float depth;

        float[] Xs;
        float[] Zs;
        Dictionary<Vector2, float> heights;

        void Start() {
            width = 10f;
            height = 0.6f;
            depth = 10f;

            List<Vector3> unevenPointsOnSpline = MakeSpline();
            pointsOnSpline = getEvenlySpacedPointsOnSpline(unevenPointsOnSpline);
            numPointsOnSpline = pointsOnSpline.Count;

            leader = Instantiate(Resources.Load("FREE_Food_Pack/Prefabs/Cheese_02")) as GameObject;
            leader.transform.localScale = new Vector3(10,10,10);//prefab cheese is too small

            leaderPosition = 0;
            leader.transform.position = pointsOnSpline[leaderPosition];

            numBoids = 50;
            visionSize = 20f;
            availableThrust = 0.3f;
            collisionWeight = 1f;
            alignmentWeight = 1f;
            cohesionWeight = 1f;
            escapePredatorWeight = 1f;
            flockBoids = AddFlockBoids(numBoids);

            CalculateTerrainHeights(); //for avoidance with perlin noise terrain


            predatorOn = false;
            if(predatorOn) {
                AddPredator();
            }

            hailOn = true;

        }

        void Update() {

            //update leader
            leaderPosition = (Mathf.CeilToInt(leaderPosition + 0.1f)) % numPointsOnSpline;
            leader.transform.position = pointsOnSpline[leaderPosition];
            Vector3 leaderDirection = (pointsOnSpline[(leaderPosition + 1) % pointsOnSpline.Count] - pointsOnSpline[leaderPosition]).normalized;
            leader.transform.rotation = Quaternion.FromToRotation(new Vector3(1, 0, 0), leaderDirection); //makes x axis of model go along direction of motion

            //update flock
            for (int i =0; i< numBoids; i++) {
                GameObject boid = flockBoids[i];
                Vector3 currentPosition = boid.transform.position;
                Vector3 currentVelocity = flockBoidVelocities[i];

                List<int> visibleBoids = CalculateVisibleBoids(boid, i); //indices of flockBoids that are visible to current boid
                //Boolean leaderVisible = isLeaderVisible(boid);
                Boolean leaderVisible = true;

                Vector3 collisionAvoidance = CalculateAvoidance(i, visibleBoids, currentVelocity, leaderVisible);
                Vector3 alignment = CalculateVelocityMatching(boid, visibleBoids, leaderVisible);
                Vector3 cohesion = CalculateFlockCentering(boid, visibleBoids, leaderVisible);

                Vector3 escapePredator = new Vector3(0, 0, 0);
                if(predatorOn) {
                    escapePredator = CalculateEscapePredator(boid);
                }

                Vector3 totalDemand = collisionAvoidance * collisionWeight + alignment * alignmentWeight + cohesion * cohesionWeight + escapePredator * escapePredatorWeight;
                Vector3 newVelocity = (currentVelocity + totalDemand * availableThrust).normalized * 0.1f;

                Boolean goingToHitPerlinNoiseTerrain = CalculateCollisionWithPerlinTerrain(currentPosition, newVelocity); //FOR AVOID PERLIN NOISE TERRAIN
                if (goingToHitPerlinNoiseTerrain)
                {
                    newVelocity.y = 1; //go up
                }

                boid.transform.position = currentPosition + newVelocity.normalized*0.1f;
                boid.transform.rotation = Quaternion.FromToRotation(new Vector3(1, 0, 0), newVelocity); //makes x axis of model go along direction of motion
                flockBoidVelocities[i] = newVelocity;
            }

            //update predator
            if (predatorOn) {
                eatBoidIfPossible();
                indexOfCurrentPrey = findNearestBoidToPredator();
                GameObject nearestBoid = flockBoids[indexOfCurrentPrey];
                predatorVelocity = (nearestBoid.transform.position - predator.transform.position).normalized;
                predator.transform.rotation = Quaternion.FromToRotation(new Vector3(1, 0, 0), predatorVelocity); //makes x axis of model go along direction of motion
                predator.transform.position += predatorVelocity * 0.1f;
            }

            //update hail
            if(hailOn) {
                AddHailstone(); //add a new hailstone (so that they're all falling at different times)
                AddHailstone();

                List<int> indicesToRemove = new List<int>(); //hailstones to destroy

                for(int i=0;i<hailstones.Count;i++) { //update each existing hailstone
                    hailstones[i].transform.position += hailstoneDirections[i]*0.1f;
                    if(hailstones[i].transform.position.y < 0) { //underground so we want to destroy it
                        indicesToRemove.Add(i);
                    }
                }

                for(int i=0;i<hailstones.Count;i++) {
                    for(int j=0;j<flockBoids.Length;j++) {
                        float distance = Vector3.Distance(flockBoids[j].transform.position, hailstones[i].transform.position);
                        if(distance < 0.1f) {
                            flockBoidVelocities[j] = hailstoneDirections[i]; //move in direction of hail
                            flockBoids[j].transform.position += hailstoneDirections[i]; //immediately get pushed a bit;
                            Debug.Log("hit");
                            indicesToRemove.Add(i); //hailstone is destroyed after hitting a boid
                        }
                    }

                }
                for (int i = 0; i < indicesToRemove.Count; i++)
                {
                    GameObject hailstone = hailstones[indicesToRemove[i]];
                    if(hailstones.Contains(hailstone)) {
                        hailstones.Remove(hailstone);
                        Destroy(hailstone);
                        hailstoneDirections.RemoveAt(i);
                    }
                }


            }

        }

        void AddHailstone() {
            float minX = -1 * (width / 2);
            float maxX = width / 2;
            float minZ = -1 * (depth / 2) - 2;
            float maxZ = depth / 2 - 2;

            //randomly place the hailstone above the terrain
            float X = UnityEngine.Random.Range(minX, maxX);
            float Z = UnityEngine.Random.Range(minZ, maxZ);

            GameObject hailstone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hailstone.transform.position = new Vector3(X, 15f, Z);
            hailstone.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            hailstones.Add(hailstone);

            Vector3 hailstoneDirection = (new Vector3(UnityEngine.Random.Range(-1f, 1f), -1f, UnityEngine.Random.Range(-1f, 1f))).normalized;
            hailstoneDirections.Add(hailstoneDirection);
        }


        Boolean CalculateCollisionWithPerlinTerrain(Vector3 boidPosition, Vector3 newVelocity) {
            Vector3 projectedPosition = boidPosition + newVelocity;

            Boolean goingToHitTerrain = false;
            float terrainHeight = GetTerrainHeightUnderBoid(boidPosition);
            if (projectedPosition.y < terrainHeight+0.5f)
                goingToHitTerrain = true;

            return goingToHitTerrain;
        }



        Vector3 CalculateEscapePredator(GameObject boid) {
            Vector3 projectedPredatorPosition = predator.transform.position + predatorVelocity;
            float distance = Vector3.Distance(boid.transform.position, predator.transform.position);
            float weight = 1 / (distance*distance);

            return (projectedPredatorPosition - boid.transform.position).normalized * weight * -1;
        }

        void eatBoidIfPossible()
        {
            GameObject nearestBoid = flockBoids[indexOfCurrentPrey];
            float distance = Vector3.Distance(nearestBoid.transform.position, predator.transform.position);
            if (distance < 0.5f)
            {
                nearestBoid.transform.position = nearestBoid.transform.position + Vector3.left*10; // "kill" boid and respawn in the middle
                indexOfCurrentPrey = findNearestBoidToPredator(); //choose new prey
            }
        }

        int findNearestBoidToPredator() {
            int nearestBoidIndex = 0;
            float minDistance = Vector3.Distance(flockBoids[nearestBoidIndex].transform.position, predator.transform.position);

            for (int i=1;i<numBoids;i++) {
                float distance = Vector3.Distance(flockBoids[i].transform.position, predator.transform.position);
                if(distance<minDistance) {
                    nearestBoidIndex = i;
                    minDistance = distance;
                }
            }
            return nearestBoidIndex;
        }


        void AddPredator() {
            predator = Instantiate(Resources.Load("FREE_Food_Pack/Prefabs/Watermelon")) as GameObject;
            predator.transform.localScale = new Vector3(10, 10, 10);//prefab cheese is too small
            predatorVelocity = Vector3.right;
            predator.transform.position = new Vector3(0, 2, 0);
            indexOfCurrentPrey = findNearestBoidToPredator();
        }


        List<Vector3> getEvenlySpacedPointsOnSpline(List<Vector3> pointsOnSpline) {
            int numPointsOnOriginalSpline = pointsOnSpline.Count;
            float[] arcLengthToEachPointOnSpline = new float[numPointsOnOriginalSpline];
            arcLengthToEachPointOnSpline[0] = 0f;

            for(int i =1;i<numPointsOnOriginalSpline;i++) {
                Vector3 tangent = pointsOnSpline[(i + 1) % numPointsOnOriginalSpline] - pointsOnSpline[i];
                float deltaT = 1f;
                float arcLengthSinceLastPoint = tangent.magnitude*deltaT;
                arcLengthToEachPointOnSpline[i] = arcLengthToEachPointOnSpline[i-1] + arcLengthSinceLastPoint;
            }

            List<Vector3> evenlySpacedPoints = new List<Vector3>();

            float numEvenlySpacedPoints = 400;
            float totalArcLength = arcLengthToEachPointOnSpline[numPointsOnOriginalSpline-1];
            float arcLengthBetweenTwoPoints = totalArcLength / numEvenlySpacedPoints;

            evenlySpacedPoints.Add(pointsOnSpline[0]);

            float currentArcLength = 0;

            for(int i=1;i<numEvenlySpacedPoints;i++) {
                float newArcLength = currentArcLength + i * arcLengthBetweenTwoPoints;
                int t = i - 1; 

                while (arcLengthToEachPointOnSpline[t] < newArcLength) { t++; }

                Vector3 newPoint = pointsOnSpline[t];
                evenlySpacedPoints.Add(newPoint);

            }
            return evenlySpacedPoints;

        }

        Boolean isLeaderVisible(GameObject boid) {
            float distanceToLeader = Vector3.Distance(boid.transform.position, leader.transform.position);
            if (distanceToLeader <= visionSize) return true;
            return false;
        }

        Vector3 CalculateFlockCentering(GameObject thisBoid, List<int> visibleBoids, Boolean leaderVisible) {
            List<Vector3> projectedPositions = new List<Vector3>();

            for(int i=0;i<visibleBoids.Count;i++) {
                int boidIndex = visibleBoids[i];
                Vector3 boidPosition = flockBoids[boidIndex].transform.position;
                Vector3 projectedPosition = boidPosition + flockBoidVelocities[boidIndex];
                projectedPositions.Add(projectedPosition);
            }

            Vector3 sumProjectedPositions = new Vector3(0, 0, 0);
            for(int i=0;i<projectedPositions.Count;i++){
                sumProjectedPositions += projectedPositions[i];
            }
            Vector3 averageProjectedPosition = sumProjectedPositions / projectedPositions.Count;

            Vector3 directionToFlockCentre = (averageProjectedPosition - thisBoid.transform.position).normalized;

            Vector3 directionToLeader = (pointsOnSpline[leaderPosition] - thisBoid.transform.position).normalized;

            return ((directionToFlockCentre + directionToLeader)/2).normalized;

        }


        Vector3 CalculateVelocityMatching(GameObject boid, List<int> visibleBoids, Boolean leaderVisible) {
            Vector3 boidPosition = boid.transform.position;

            Vector3 weightedVelocity = new Vector3(0, 0, 0);

            for (int i =0;i < visibleBoids.Count; i++) {
                int otherBoidIndex = visibleBoids[i];
                Vector3 otherBoidPosition = flockBoids[otherBoidIndex].transform.position;
                Vector3 otherBoidVelocity = flockBoidVelocities[otherBoidIndex].normalized;

                float distance = Vector3.Distance(boidPosition, otherBoidPosition);
                weightedVelocity += otherBoidVelocity / distance;
            }

            if(leaderVisible) {
                Vector3 leaderVelocity = (pointsOnSpline[(leaderPosition + 1) % pointsOnSpline.Count] - pointsOnSpline[leaderPosition]).normalized;

                float distance = Vector3.Distance(boidPosition, leader.transform.position);
                weightedVelocity += leaderVelocity/ distance;
            }
            return weightedVelocity.normalized;

        }

        void CalculateTerrainHeights() {
            MyPlane plane = gameObject.GetComponent<MyPlane>();
            float[,] noise = plane.noise;

            int subdivision = 250;
            int stride = subdivision + 1;
            const float xz_start = -5;
            const float xz_end = 5;
            float step = (xz_end - xz_start) / (float)(subdivision);

            Xs = new float[stride];
            Zs = new float[stride];
            heights = new Dictionary<Vector2, float>();
            for (int i = 0; i < stride; i++)
            {
                for (int j = 0; j < stride; j++)
                {
                    float cur_x = xz_start + j * step;
                    float cur_z = xz_start + i * step;
                    Xs[j] = cur_x;
                    Zs[i] = cur_z;

                    float cur_y = noise[i, j];

                    //The ocean should be flat
                    if (cur_y < 0)
                    {
                        cur_y = 0.04f; //the amplitude of the waves is 0.04
                    }
                    heights.Add(new Vector2(cur_x, cur_z), cur_y);
                }
            }
        }

        float findNearValue(float thisValue, float[] Values) {
            for (int i = 0; i < Values.Length; i++)
            {
                if (Math.Abs(thisValue - Values[i]) < 0.1f)
                {
                    return Values[i];
                }
            }
            return 100f; //no near value found
        }


        float GetTerrainHeightUnderBoid(Vector3 boidPosition) {
            float this_x = boidPosition.x;
            float x_for_dict = findNearValue(this_x, Xs);

            float this_z = boidPosition.z;
            float z_for_dict = findNearValue(this_z, Zs);

            if (x_for_dict == 100f || z_for_dict == 100f) return 0f;


            return heights[new Vector2(x_for_dict, z_for_dict)];
        }



        Vector3 CalculateAvoidance(int boidIndex, List<int> visibleBoids, Vector3 velocity, Boolean leaderVisible) {
            Vector3 boidPosition = flockBoids[boidIndex].transform.position;
            Vector3 projectedPosition = boidPosition + velocity;

            Vector3 dontFlyTooHigh = new Vector3(0,0,0);

            if (projectedPosition.y > 10)
            { 
                dontFlyTooHigh = Vector3.down;
            }

            Vector3 avoidOtherBoids = new Vector3(0, 0, 0);
            for(int i= 0;i<visibleBoids.Count;i++) {
                int otherBoidIndex = visibleBoids[i];
                Vector3 otherBoidPosition = flockBoids[otherBoidIndex].transform.position;

                float distance = Vector3.Distance(projectedPosition, otherBoidPosition);
                Vector3 directionToOtherBoid = otherBoidPosition - projectedPosition;
                avoidOtherBoids -= directionToOtherBoid.normalized;
            }

            Vector3 avoidLeader = new Vector3(0, 0, 0);
            if(leaderVisible) {
                float distance = Vector3.Distance(projectedPosition, leader.transform.position);
                if(distance < 1) {
                    Vector3 directionToLeader = leader.transform.position - projectedPosition;
                    avoidLeader -= directionToLeader.normalized;
                }
            }

            return (dontFlyTooHigh + avoidOtherBoids.normalized + avoidLeader.normalized).normalized;
        }


        List<int> CalculateVisibleBoids(GameObject boid, int boidIndex) {
            List<int> visibleBoids = new List<int>();

            for(int i = 0;i<numBoids; i++) {
                if (i == boidIndex) continue; //don't want to calculate itself

                GameObject otherBoid = flockBoids[i];

                float distance = Vector3.Distance(boid.transform.position, otherBoid.transform.position);
                if(distance <= visionSize) {
                    visibleBoids.Add(i);
                }
            }
            return visibleBoids;
        }


        GameObject[] AddFlockBoids(int numBoids) {
            GameObject[] boids = new GameObject[numBoids];
            flockBoidVelocities = new Vector3[numBoids];

            for(int i = 0; i< numBoids; i++) {
                GameObject boid = Instantiate(Resources.Load("FREE_Food_Pack/Prefabs/Cheese_02")) as GameObject;
                boid.transform.localScale = new Vector3(3, 3, 3);//prefab cheese is too small
                boids[i] = boid;

                if(i < 10) {
                    boids[i].transform.position = pointsOnSpline[leaderPosition] + Vector3.right * i*0.1f;
                }
                else if (i < 20)
                {
                    boids[i].transform.position = pointsOnSpline[leaderPosition] + Vector3.left * i*0.1f;
                }
                else if (i < 30)
                {
                    boids[i].transform.position = pointsOnSpline[leaderPosition] + Vector3.forward * i*0.1f;
                }
                else if (i < 40)
                {
                    boids[i].transform.position = pointsOnSpline[leaderPosition] + Vector3.back * i*0.1f;
                }
                else
                {
                    boids[i].transform.position = pointsOnSpline[leaderPosition] + Vector3.up * i*0.1f;
                }


                flockBoidVelocities[i] = new Vector3(0,0,0);
            }

            return boids;

        }



        List<Vector3> MakeSpline() {
            Vector3 back_right_corner = Vector3.right * (width / 2) + Vector3.forward * (depth / 2) + Vector3.up * (height + 1.5f);
            Vector3 back_left_corner = Vector3.left * (width / 2) + Vector3.forward * (depth / 2) + Vector3.up * (height + 0.5f);

            Vector3 front_right_corner = Vector3.right * (width / 2) + Vector3.back * (depth / 2) + Vector3.up * (height + 1.5f);
            Vector3 front_left_corner = Vector3.left * (width / 2) + Vector3.back * (depth / 2) + Vector3.up * (height + 0.5f);

            Vector3 front_centre = Vector3.back * ((depth / 2) - 1) + Vector3.up * (height + 1f);
            Vector3 back_centre = Vector3.forward * ((depth / 2) - 1) + Vector3.up * (height + 1f);
            Vector3 right_centre = Vector3.right * ((width / 2) - 1) + Vector3.up * (height + 3f);
            Vector3 left_centre = Vector3.left * ((width / 2) - 1) + Vector3.up * (height + 0.1f);


            knotPositions = new Vector3[] { back_left_corner, back_centre, back_right_corner, right_centre, front_right_corner, front_centre, front_left_corner, left_centre };

            Vector3 tan1 = ((back_left_corner - left_centre) + (back_centre - back_left_corner)) / 2;
            Vector3 tan2 = ((back_centre - back_left_corner) + (back_right_corner - back_centre)) / 2;
            Vector3 tan3 = ((back_right_corner - back_centre) + (right_centre - back_right_corner)) / 2;
            Vector3 tan4 = ((right_centre - back_right_corner) + (front_right_corner-right_centre)) / 2;
            Vector3 tan5 = ((front_right_corner - right_centre) + (front_centre - front_right_corner)) / 2;
            Vector3 tan6 = ((front_centre - front_right_corner) + (front_left_corner - front_centre)) / 2;
            Vector3 tan7 = ((front_left_corner - front_centre) + (left_centre - front_left_corner)) / 2;
            Vector3 tan8 = ((left_centre - front_left_corner) + (back_left_corner-left_centre)) / 2;

            knotTangents = new Vector3[] {tan1,tan2,tan3,tan4,tan5,tan6,tan7,tan8};
            int numKnots = knotPositions.Length;

            //connect the loop
            List<Vector3> pointsOnSpline = GetPositionsBetweenTwoKnots(knotPositions[numKnots - 1], knotTangents[numKnots - 1], knotPositions[0], knotTangents[0]);

            for (int i = 0; i < numKnots - 1; i++)
            {
                List<Vector3> newPoints = GetPositionsBetweenTwoKnots(knotPositions[i], knotTangents[i], knotPositions[i + 1], knotTangents[i + 1]);
                pointsOnSpline.AddRange(newPoints);

            }


            //int lengthOfLineRenderer = pointsOnSpline.Count;

            //LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
            //lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            //lineRenderer.widthMultiplier = 0.1f;
            //lineRenderer.positionCount = lengthOfLineRenderer;
            //lineRenderer.material.color = Color.red;

            //var points = new Vector3[lengthOfLineRenderer];

            //for (int i = 0; i < lengthOfLineRenderer; i++)
            //{
            //    points[i] = pointsOnSpline[i];
            //}

            //lineRenderer.SetPositions(points);
            return pointsOnSpline;
        }

        List<Vector3> GetPositionsBetweenTwoKnots(Vector3 knot1_position, Vector3 knot1_tangent, Vector3 knot2_position, Vector3 knot2_tangent)
        {
        
            float t;
            Vector3 point;
            List<Vector3> points = new List<Vector3>();
            int numPoints = 1000;
            float alpha = 3f;

            for (int i = 0; i < numPoints; i++)
            {
                t = i / (numPoints - 1.0f);
                float t2 = Mathf.Pow(t, 2f);
                float t3 = Mathf.Pow(t, 3f);

                //from "curves" handout on connex
                point = (1f - 3.0f * t2 + 2f * t3) * knot1_position
                    + (3f * t2 - 2f * t3) * knot2_position
                    + (t - 2f * t2 + t3) * alpha * knot1_tangent
                    + (-t2 + t3) * alpha * knot2_tangent;

                points.Add(point);
            }
            return points;
        }
    }
}

