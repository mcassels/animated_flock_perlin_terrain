using UnityEngine;
using System.Collections;

namespace Assignment03
{
    //I read the following tutorial for reference: http://twiik.net/articles/simplest-possible-day-night-cycle-in-unity-5
    public class DayNightCycle : MonoBehaviour
    {

        public Light sun;
        public Light moon;
        public float defaultSunIntensity;
        public float defaultMoonIntensity;
        public float currentTime; //fraction of day
        public int dayLength = 120; //number of seconds in day

        void Start()
        {
            defaultSunIntensity = sun.intensity;
            defaultMoonIntensity = 0.2f; //needs to be less cause it's moonlight
            currentTime = 0.2f;//start at sunrise
        }

        void Update()
        {
            currentTime = (currentTime + (Time.deltaTime / (float)dayLength)) % 1; //%1 so that currentTime will go back to zero at the end of a day and process restarts

            //rotate the sun around (over and under) the terrain
            sun.transform.localRotation = Quaternion.Euler((currentTime * 360f) - 90, 170, 0);

            //rotate the moon differently depending on if night is starting or ending 
            //(we don't actually see the moon rise up over the horizon, we just get some moonlight moving across)
            if (currentTime > 0.8)
            {
                moon.transform.localRotation = Quaternion.Euler((currentTime * 360f) + 20, 170, 0); //getting more direct/shining more on water
            }
            else
            {
                moon.transform.localRotation = Quaternion.Euler(-(currentTime * 360f) + 20, 170, 0); //getting less direct, less reflection on water
            }

            //light intensities should change based on current time

            float sunIntensityLevel = 0f; //nighttime
            float moonIntensityLevel = 0f;

            if (currentTime >= 0.88 && currentTime <= 0.9) //moon rise
            {
                moonIntensityLevel = Mathf.Clamp01((currentTime - 0.8f) * 50);
            }
            else if (currentTime > 0.9 || currentTime <= 0.1) //moon is out
            {
                moonIntensityLevel = 0.5f;
            }
            else if(currentTime > 0.1 && currentTime <= 0.2) //moon set
            {
                moonIntensityLevel = Mathf.Clamp01(1 - ((currentTime - 0.1f) * 50));
            }
            else if (currentTime >= 0.2 && currentTime <= 0.3) //dawn
            {
                sunIntensityLevel = Mathf.Clamp01((currentTime - 0.2f) * 50); //gets more intense, clamp so it's never less than 0 or more than 1

            } else if(currentTime > 0.3f && currentTime < 0.75f) //daytime
            {
                sunIntensityLevel = 1;

            } else if (currentTime >= 0.75f && currentTime < 0.8) //dusk
            {
                sunIntensityLevel = Mathf.Clamp01(1 - ((currentTime - 0.75f) * 50)); //gets less intense, clamp so it's never less than 0 or more than 1
            }

            sun.intensity = defaultSunIntensity * sunIntensityLevel;
            moon.intensity = defaultMoonIntensity * moonIntensityLevel;
        }
    }
}
