using UnityEngine;
using UnityEngine.Assertions;

namespace NUHS.UltraSound.Recording
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class MeshVisualizationManager : MonoBehaviour
    {
        [SerializeField] private Material[] mats;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private int currentMatIndex = 0;
        
        void Awake()
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            Assert.IsTrue(mats.Length > 1);
        }

        public void LoadObj(string objString)
        {
            if (meshFilter == null)
            {
                Awake();
            }

            meshFilter.mesh = null;
            Mesh mesh = MeshParser.Parse(objString);
            meshFilter.mesh = mesh;
            currentMatIndex = 0;
            SetMaterial();
        }

        public void SwitchMaterial()
        {
            currentMatIndex++;
            if (currentMatIndex >= mats.Length)
            {
                currentMatIndex = 0;
            }

            SetMaterial();
        }

        public void ResetMesh()
        {
            if (meshFilter != null)
            {
                meshFilter.mesh = null;
            }
        }

        private void SetMaterial()
        {
            meshRenderer.material = mats[currentMatIndex];
        }
    }
}
