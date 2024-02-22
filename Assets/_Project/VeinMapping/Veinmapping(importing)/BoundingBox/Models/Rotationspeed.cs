using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Rotationspeed : MonoBehaviour
{

    void Update()
    {
        // Rotate the object around its local X axis at 1 degree per second
        transform.Rotate(0,Time.deltaTime * 500.0f, 0);

        
    }
}