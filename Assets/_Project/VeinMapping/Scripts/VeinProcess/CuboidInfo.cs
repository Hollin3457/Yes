using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace NUHS.VeinMapping
{
    public struct CuboidPoints
    {
        public Vector3 origin;
        public Vector3 corner1;
        public Vector3 corner2;
        public Vector3 corner3;
    }

    public class CuboidInfo : MonoBehaviour 
    {
        [SerializeField] private GameObject[] corners;

        private void Awake()
        {
            Assert.IsTrue(corners.Length == 4);
        }

        public CuboidPoints GetCuboidInfo()
        {
            return new CuboidPoints()
            {
                origin = corners[0].transform.position,
                corner1 = corners[1].transform.position,
                corner2 = corners[2].transform.position,
                corner3 = corners[3].transform.position
            };
        }
    }
}