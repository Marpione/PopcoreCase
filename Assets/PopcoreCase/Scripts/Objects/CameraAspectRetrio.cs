using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAspectRetrio : MonoBehaviour
{
    Camera camera;
    Camera Camera { get { return (camera == null) ? camera = GetComponent<Camera>() : camera; } }

    public float aspectRetrio = 5;

    private void Update()
    {
        float screenRatio = (float)Screen.width / (float)Screen.height;

        if (screenRatio >= aspectRetrio)
        {
            Camera.main.orthographicSize = aspectRetrio / 2;
        }
        else
        {
            float differenceInSize = aspectRetrio / screenRatio;
            Camera.orthographicSize = aspectRetrio / 2 * differenceInSize;
        }
    }
}
