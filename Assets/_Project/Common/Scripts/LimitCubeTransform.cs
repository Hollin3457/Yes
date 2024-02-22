using UnityEngine;

public class LimitCubeTransform : MonoBehaviour
{
    [SerializeField] private Transform ReferenceObject;
    [SerializeField] private Vector3 InitialRelativePosition;
    [SerializeField] private Vector3 MinRelativePosition;
    [SerializeField] private Vector3 MaxRelativePosition;
    
    //[SerializeField] private float ClampRadius;

    private Vector3 RelativePosition;

    void Start()
    {
        transform.position = ReferenceObject.InverseTransformPoint(InitialRelativePosition);
    }

    // Update is called once per frame
    void Update()
    {
        // clamp cube position so it does not move out of user's view
        RelativePosition = ReferenceObject.InverseTransformPoint(transform.position);
        transform.position = ReferenceObject.TransformPoint(new Vector3(
            Mathf.Clamp(RelativePosition[0], MinRelativePosition[0], MaxRelativePosition[0]),
            Mathf.Clamp(RelativePosition[1], MinRelativePosition[1], MaxRelativePosition[1]),
            Mathf.Clamp(RelativePosition[2], MinRelativePosition[2], MaxRelativePosition[2])
            ));

        // clamp Z rotation
        transform.localEulerAngles = Vector3.zero;
    }
}
