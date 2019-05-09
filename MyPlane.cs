using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assignment03
{
    public class MyPlane : MonoBehaviour
    {

        public Material simpleMaterial;
        public GameObject[] models; //the little houses and trees
        public int currModelIndex;
        public bool roomLeft;
        public ArrayList availableTriangles; //so every model doesn't have to recalculate which triangles are taken
        public GameObject[] clouds;
        public Vector3[] cloudDirections; //clouds move across the sky, each in a different random direction
        public float cloudHeight;
        public float[,] noise;


        void Start()
        {

            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            renderer.material = simpleMaterial;
            GenerateMyPlane();

            Mesh PlaneMesh = gameObject.GetComponent<MeshFilter>().mesh;
            int[] triangles = PlaneMesh.triangles;

            int[] availableTrianglesArray = new int[triangles.Length];
            Array.Copy(triangles, 0, availableTrianglesArray, 0, triangles.Length);
            availableTriangles = new ArrayList(availableTrianglesArray);

            //scatter models randomly and ensure they don't overlap
            models = new GameObject[10]; //5 trees and 5 houses
            currModelIndex = 0;
            roomLeft = true;//indicates whether there is space to add more models
            AddTrees(5); 
            AddHouses(5);

            //add clouds
            cloudHeight = 3; //clouds are a fixed height above the terrain
            string[] cloudNames = { "RG_Cloud-01", "RG_Cloud-02", "RG_Cloud-03", "RG_Cloud-04", "RG_Cloud-05"};
            AddClouds(cloudNames);

        }

        // Update is called once per frame
        void Update()
        {
            //clouds move
            UpdateClouds();
        }


        void UpdateClouds()
        {
            float terrainWidth = gameObject.GetComponent<Renderer>().bounds.extents.x;
            float terrainDepth = gameObject.GetComponent<Renderer>().bounds.extents.z;
            Vector3 terrainCenter = gameObject.GetComponent<Renderer>().bounds.center;

            for (int i =0; i< clouds.Length; ++i)
            {
                Vector3 prevPosition = clouds[i].GetComponent<Renderer>().bounds.center;

                Vector3 diff = prevPosition - terrainCenter;
                if (Mathf.Abs(diff.x) > terrainWidth) cloudDirections[i].x *= -1; //if we've reached an x edge, need to turn around
                if (Mathf.Abs(diff.z) > terrainDepth) cloudDirections[i].z *= -1; //if we've reached a z edge, need to turn around

                Vector3 newPosition = prevPosition + (0.2f * Time.deltaTime * cloudDirections[i]);
                newPosition.y = terrainCenter.y + cloudHeight; //don't want floating point rounding to make the cloud fall out of the sky. Keep clouds at same height
                clouds[i].transform.position = newPosition;
            }
        }

        void AddClouds(string[] cloudNames)
        {
            int numClouds = cloudNames.Length;
            cloudDirections = new Vector3[numClouds];
            clouds = new GameObject[numClouds];

            System.Random random = new System.Random();//will give the clouds random directions to move in

            float terrainWidth = gameObject.GetComponent<Renderer>().bounds.extents.x;
            float terrainDepth = gameObject.GetComponent<Renderer>().bounds.extents.z;
            Vector3 terrainCenter = gameObject.GetComponent<Renderer>().bounds.center;

            for (int i = 0; i < numClouds; ++i)
            {
                //using prefab clouds from https://assetstore.unity.com/packages/3d/environments/awesome-low-poly-fantasy-clouds-97654
                GameObject myCloud = Instantiate(Resources.Load("LowPolyFantasyClouds/Models/"+cloudNames[i])) as GameObject;

                //choose random initial starting points -- it's fine if clouds overlap
                float xInitial = ((float)random.NextDouble() * 2 * terrainWidth) + terrainCenter.x - terrainWidth; //random x-value within terrain width of terrain center
                float zInitial = ((float)random.NextDouble() * 2 * terrainDepth) + terrainCenter.z - terrainDepth; //random z-value within terrain depth of terrain center

                myCloud.transform.position = new Vector3(xInitial,terrainCenter.y+cloudHeight,zInitial);
                myCloud.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);//prefab cloud models are too big

                //the clouds will stay at same height but move around the scene on x and z axises
                cloudDirections[i] = new Vector3((float)random.NextDouble(), 0, (float)random.NextDouble());
                clouds[i] = myCloud;
            }

        }

        void AddTrees(int numTrees)
        {
            //using prefab trees from https://assetstore.unity.com/packages/3d/vegetation/trees/free-trees-103208
            string[] trees = { "Fir_Tree", "Poplar_Tree", "Oak_Tree", "Palm_Tree" };

            for (int i = 0; i < numTrees; ++i)//10 trees
            {
                //scale to 0.2 cause prefab trees are too big
                AddTree(trees[i % trees.Length], 0.2f); //there are only 4 unique trees
            }
        }

        void AddHouses(int numHouses)
        {
            for(int i =0; i< numHouses; ++i)
            {
                if (roomLeft == false) return;
                GameObject myHouse = Instantiate(Resources.Load("Simple_Houses_Lite/Prefab/house_3")) as GameObject; //use prefab house recommended by prof
                //scale to 0.1 cause prefabs are too big
                AddModel(myHouse, 0.1f, 0.5f, 0.1f); //between 0.1 and 0.5 so that that the house appears on land (can be on vegetation or snow
            }
        }

        void AddTree(string treeName, float scaling)
        {
            if (roomLeft == false) return;
            GameObject myTree = Instantiate(Resources.Load("Darth_Artisan/Free_Trees/Prefabs/"+treeName)) as GameObject;
            AddModel(myTree, 0.1f, 0.3f, scaling); //between 0.1 and 0.3 so that that the tree appears on the vegetation level
        }

        bool DoModelsOverlap(Vector3 newModelPosition, GameObject newModel)
        {
            float newModelRadiusX = newModel.GetComponent<Renderer>().bounds.extents.x;
            float newModelRadiusZ = newModel.GetComponent<Renderer>().bounds.extents.z;

            for (int i = 0; i< currModelIndex; ++i)
            {
                float modelRadiusX = models[i].GetComponent<Renderer>().bounds.extents.x;
                float modelRadiusZ = models[i].GetComponent<Renderer>().bounds.extents.z;
                Vector3 modelPosition = transform.InverseTransformPoint(models[i].transform.position);

                Vector3 diff = newModelPosition - modelPosition;


                if (Math.Abs(diff.x) < (modelRadiusX + newModelRadiusX)) return true;
                if (Math.Abs(diff.z) < (modelRadiusZ + newModelRadiusZ)) return true;

            }

            return false;
        }

        void AddModel(GameObject myModel, float minHeight, float maxHeight, float scaling)
        {
            myModel.transform.localScale = new Vector3(scaling, scaling, scaling);

            Mesh PlaneMesh = gameObject.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = PlaneMesh.vertices;
            int[] triangles = PlaneMesh.triangles;

            //pick a random triangle to place the model 
            int triangleIndex = UnityEngine.Random.Range(0, triangles.Length);
            int triangle = triangles[triangleIndex];

            Vector3[] triangleVertices = { vertices[triangle], vertices[triangle + 1], vertices[triangle + 2] };

            //we will place the model at centre of triangle
            Vector3 triangleCentre = (triangleVertices[0] + triangleVertices[1] + triangleVertices[2]) / 3.0f;
            float height = triangleCentre.y;

            //Want the model to only be placed within the specified level (for example not the ocean)
            //Also want to make sure no models overlap
            while (height < minHeight || height > maxHeight || DoModelsOverlap(triangleCentre, myModel) == true)
            {
            
                if(availableTriangles.Count == 0) //no space to place more models
                {
                    Destroy(myModel);
                    roomLeft = false;
                    return;
                }
                //keep trying triangles until one with its centre at the right height and that doesn't overlap is found
                triangleIndex = UnityEngine.Random.Range(0, availableTriangles.Count);
                triangle = (int)availableTriangles[triangleIndex];

                //we will not try this triangle again
                availableTriangles.RemoveAt(triangleIndex);

                if(vertices.Length < triangle + 3) //to be safe for the next few lines
                {
                    continue;
                }
                triangleVertices[0] = vertices[triangle];
                triangleVertices[1] = vertices[triangle + 1]; 
                triangleVertices[2] = vertices[triangle + 2];

                triangleCentre = (triangleVertices[0] + triangleVertices[1] + triangleVertices[2]) / 3.0f;
                height = triangleCentre.y;
            }

            //place the model at the centre of the triangle
            Vector3 worldSpacePosition = transform.TransformPoint(triangleCentre); // convert back to worldspace
            myModel.transform.position = worldSpacePosition;

            //rotate model so it is upright on its local surface
            Vector3[] normals = PlaneMesh.normals;
            Vector3 normal = normals[triangle];
            myModel.transform.rotation = Quaternion.FromToRotation(new Vector3(0,1,0), normal); //makes y axis of model go along normal of triangle at model centre

            //keep track of models
            models[currModelIndex] = myModel;
            currModelIndex++;
        }

        void GenerateMyPlane()
        {
           
            Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;
            Vector2[] uvs = mesh.uv;

            mesh.Clear();

            //subdivision = how many squares per row/col
            int subdivision = 250;
            int stride = subdivision + 1;
            int num_vert = stride * stride;
            int num_tri = subdivision * subdivision * 2;

            indices = new int[num_tri * 3];
            int index_ptr = 0;
            for (int i = 0; i < subdivision; i++)
            {
                for (int j = 0; j < subdivision; j++)
                {
                    int quad_corner = i * stride + j;
                    indices[index_ptr] = quad_corner;
                    indices[index_ptr + 1] = quad_corner + stride;
                    indices[index_ptr + 2] = quad_corner + stride + 1;
                    indices[index_ptr + 3] = quad_corner;
                    indices[index_ptr + 4] = quad_corner + stride + 1;
                    indices[index_ptr + 5] = quad_corner + 1;
                    index_ptr += 6;
                }
            }

            Debug.Assert(index_ptr == indices.Length);

            PerlinGenerator myNoiseGenerator = new PerlinGenerator();

            //resolution is 250x250, frequency is 10
            noise = myNoiseGenerator.Perlin2DNoise(250, 250, 10);

            const float xz_start = -5;
            const float xz_end = 5;
            float step = (xz_end - xz_start) / (float)(subdivision);
            vertices = new Vector3[num_vert];
            uvs = new Vector2[num_vert];
            int uv_index = 0;
            for (int i = 0; i < stride; i++)
            {
                for (int j = 0; j < stride; j++)
                {

                    float cur_x = xz_start + j * step;
                    float cur_z = xz_start + i * step;

                    float cur_y = noise[i,j];

                    //The ocean should be flat
                    if(cur_y < 0)
                    {
                        cur_y = 0;
                    }

                    vertices[i * stride + j] = new Vector3(cur_x, cur_y, cur_z);

                    //tile the texture (the textures are things like grass and sand so tiling is what we want)
                    uvs[uv_index] = new Vector2(cur_x, cur_z);
                    uv_index++;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
        }
    }
}
