using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAroundDemo : MonoBehaviour
{
    [SerializeField] private Vector3 origin;
    [SerializeField] private float speed;

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(origin,Vector3.up, speed* Time.deltaTime);
    }
}
