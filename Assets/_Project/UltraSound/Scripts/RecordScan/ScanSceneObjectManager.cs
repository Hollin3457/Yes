using UnityEngine;

public class ScanSceneObjectManager : MonoBehaviour
{
    [Header("UI Dependency")]
    [SerializeField] private Transform Cuboid;
    
    private BoxCollider Collider;
    // Start is called before the first frame update
    void Start()
    {
        Collider = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Collider.center = Cuboid.localPosition;
        Collider.size = Cuboid.localScale;
    }
}
