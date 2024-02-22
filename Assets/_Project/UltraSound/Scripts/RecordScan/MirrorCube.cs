using NUHS.Common;
using UnityEngine;

namespace NUHS.UltraSound.Recording
{
    public sealed class MirrorCube : MonoBehaviour
    {
        [SerializeField] private Renderer tilePrefab;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material fillMaterial;
        [SerializeField] private float maxLength;
        [SerializeField] private int resolution;

        private Renderer[] tiles;
        private float segmentLength;
        private int cols;
        private int rows;
        private Bounds bounds;

        [ContextMenu("GenerateGrid")]
        public void GenerateGrid(Vector2 scale)
        {
            rows = cols = resolution;
            if (scale.x > scale.y)
            {
                rows = Mathf.RoundToInt(rows * (scale.y / scale.x));
            }
            else
            {
                cols = Mathf.RoundToInt(cols * (scale.x / scale.y));
            }
            segmentLength = maxLength / resolution;
            tiles = new Renderer[rows * cols];
            var segmentHalfLength = segmentLength * 0.5f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var i = y * cols + x;
                    var localPos = new Vector3(x * segmentLength + segmentHalfLength, y * segmentLength + segmentHalfLength);
                    tiles[i] = Instantiate(tilePrefab, transform);
                    tiles[i].transform.localScale = new Vector2(segmentLength, segmentLength);
                    tiles[i].transform.localPosition = localPos;
                }
            }
        }

        public void DestroyGrid()
        {
            if (tiles == null)
            {
                return;
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                Destroy(tiles[i].gameObject);
            }
            tiles = null;
        }

        [ContextMenu("Fill")]
        public void Fill(Vector2 normalizedLocalPosInCuboid, Size2D scannerHeadSize, Vector2 cuboidScale)
        {
            if (tiles == null) return;

            var width = segmentLength * cols;
            var height = segmentLength * rows;

            var ratioX = width / cuboidScale.x;
            var ratioY = height / cuboidScale.y;
            var scaledScannerSize = new Vector3(
                scannerHeadSize.X.Meters * ratioX,
                scannerHeadSize.Y.Meters * ratioY,
                0f);

            var mirrorLocalX = normalizedLocalPosInCuboid.x * width;
            var mirrorLocalY = normalizedLocalPosInCuboid.y * height;
            bounds = new Bounds(new Vector2(mirrorLocalX, mirrorLocalY), scaledScannerSize);

            var segmentSize = new Vector3(segmentLength, segmentLength);
            for (int i = 0; i < tiles.Length; i++)
            {
                if (bounds.Intersects(new Bounds(tiles[i].transform.localPosition, segmentSize)))
                {
                    tiles[i].sharedMaterial = fillMaterial;
                }
            }
        }

        public void ResetFill()
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].sharedMaterial = defaultMaterial;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(bounds.center + transform.position, bounds.size);
        }
    }
}
