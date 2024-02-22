using NUHS.Common;
using NUHS.UltraSound.Recording;
using UnityEngine;

public class MirrorTesting : MonoBehaviour
{
    public Transform cube;
    //public Transform localObj;
    public MirrorCube mirror;
    public Vector2 scannerSize = new Vector2(0.001f, 0.001f);

    private Vector3 hitPoint;

    // Update is called once per frame
    void Update()
    {
        hitPoint = Input.mousePosition;
        hitPoint.z = -Camera.main.transform.position.z;
        hitPoint = Camera.main.ScreenToWorldPoint(hitPoint);

        if (!Input.GetMouseButton(0))
        {
            return;
        }
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            var worldToLocalMatrix = Matrix4x4.TRS(cube.position, cube.rotation, Vector3.one).inverse;
            var cuboidLocalPos = worldToLocalMatrix.MultiplyPoint3x4(hit.point) + cube.localScale * 0.5f;
            //Debug.Log($"{a.x}, {a.y}, {a.z}");

            //Debug.DrawLine(ray.origin, hit.point, Color.green, 0.5f);
            //var cuboidLocalPos = localObj.InverseTransformPoint(hit.point); // + cube.localScale * 0.5f;
            //var matrix = Matrix4x4.TRS(cube.position, cube.rotation, cube.localScale);
            //var cuboidLocalPos = matrix.MultiplyPoint3x4(hit.point) + cube.localScale * 0.5f;
            var normalizedPos = new Vector2(cuboidLocalPos.x / cube.localScale.x, cuboidLocalPos.y / cube.localScale.y);
            //Debug.Log($"{hit.point} -> {cuboidLocalPos.x}, {cuboidLocalPos.y}, {cuboidLocalPos} -> {normalizedPos}");
            //return;
            var realScannerSize = Size2D.FromMeters(scannerSize);
            mirror.Fill(normalizedPos, realScannerSize, cube.localScale);
        }
    }

    public void GenerateGrid()
    {
        mirror.GenerateGrid(cube.localScale);
    }

    void OnDrawGizmos()
    {
        if (hitPoint == null)
        {
            return;
        }
        Gizmos.matrix = Matrix4x4.TRS(hitPoint, cube.localRotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, scannerSize);
    }
}
