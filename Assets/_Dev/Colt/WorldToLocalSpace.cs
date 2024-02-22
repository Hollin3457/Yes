using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldToLocalSpace : MonoBehaviour
{
    public bool convertRotation;
    public Transform localObj;
    public Vector3 worldPos;

    [ContextMenu("DoConversion")]
    void DoConversion()
    {
        if (convertRotation)
        {
            //Debug.Log(localObj.InverseTransformDirection(transform.position));
            var rot = Quaternion.Inverse(localObj.rotation) * transform.rotation;
            Debug.Log($"{rot.eulerAngles.x}, {rot.eulerAngles.y}, {rot.eulerAngles.z}");
        }
        else
        {

            var pos = localObj.InverseTransformPoint(worldPos);
            Debug.Log($"{pos.x}, {pos.y}, {pos.z}");
            var worldToLocalMatrix = Matrix4x4.TRS(localObj.position, localObj.rotation, Vector3.one).inverse;
            pos = worldToLocalMatrix.MultiplyPoint3x4(worldPos);
            Debug.Log($"{pos.x}, {pos.y}, {pos.z}");

            //var pos = localObj.InverseTransformPoint(transform.position);
            //Debug.Log($"{pos.x}, {pos.y}, {pos.z}");
            //var worldToLocalMatrix = Matrix4x4.TRS(localObj.position, localObj.rotation, localObj.localScale).inverse;
            //pos = worldToLocalMatrix.MultiplyPoint3x4(transform.position);
            //Debug.Log($"{pos.x}, {pos.y}, {pos.z}");
        }
    }

    public IEnumerator RecordFrame()
    {
        yield return new WaitForSeconds(3);
        Debug.Log("halsdhfj");
    }
}
