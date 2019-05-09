using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assignment03
{
    public class PerlinGenerator
    {

        //f(t) = 6t^5-15t^4+10t^3
        float interpolation_function(float t)
        {
            float t_cubic = t * t * t;
            float t_square = t * t;

            return 6 * t_cubic * t_square - 15 * t_square * t_square + 10 * t_cubic;
        }

        public float[,] Perlin2DNoise(int width, int height, int frequency)
        {
            Vector2[,] gradients = new Vector2[frequency + 1, frequency + 1];

            Vector2[] edge_centers = new Vector2[4];
            edge_centers[0] = (new Vector2(0, 1)).normalized;
            edge_centers[1] = (new Vector2(0, -1)).normalized;
            edge_centers[2] = (new Vector2(1, 0)).normalized;
            edge_centers[3] = (new Vector2(-1, 0)).normalized;

            for (int i = 0; i < frequency + 1; ++i)
            {
                for (int j = 0; j < frequency + 1; ++j)
                {
                    float roll = Random.value;
                    if (roll < 0.25f) gradients[i,j] = edge_centers[0];
                    else if (roll < 0.5f) gradients[i,j] = edge_centers[1];
                    else if (roll < 0.75f) gradients[i,j] = edge_centers[2];
                    else gradients[i,j] = edge_centers[3];
                }
            }

            //generate 4 dot products and blend them together
            float[,] noise = new float[width + 1, height + 1];
            float period = 1.0f / frequency;
            float step_i = 1.0f / width;
            float step_j = 1.0f / height;

            for (int i = 0; i < width; ++i)
            {
                for(int j= 0; j < height; ++j)
                {
                    float location_period_i = step_i * i / period;
                    float location_period_j = step_j * j / period;

                    int cell_i = Mathf.FloorToInt(location_period_i);
                    int cell_j = Mathf.FloorToInt(location_period_j);

                    float in_cell_location_i = location_period_i - cell_i;
                    float in_cell_location_j = location_period_j - cell_j;

                    float dot_bottom_left = Vector2.Dot(gradients[cell_i,cell_j], new Vector2(in_cell_location_i, in_cell_location_j));
                    float dot_bottom_right = Vector2.Dot(gradients[cell_i + 1,cell_j], new Vector2(in_cell_location_i - 1, in_cell_location_j));
                    float dot_top_left = Vector2.Dot(gradients[cell_i,cell_j + 1], new Vector2(in_cell_location_i, in_cell_location_j - 1));
                    float dot_top_right = Vector2.Dot(gradients[cell_i + 1,cell_j + 1], new Vector2(in_cell_location_i - 1, in_cell_location_j - 1));

                    float weight_i = interpolation_function(in_cell_location_i);
                    float weight_j = interpolation_function(in_cell_location_j);

                    //perform interpolation for each corner
                    float bottom_left = (1 - weight_i) * (1 - weight_j) * dot_bottom_left;
                    float bottom_right = weight_i * (1 - weight_j) * dot_bottom_right;
                    float top_left = (1 - weight_i) * weight_j * dot_top_left;
                    float top_right =  weight_i * weight_j * dot_top_right;

                    noise[i,j] = bottom_left + bottom_right + top_left + top_right;
                }
            }

            return noise;
        }

    }
}
