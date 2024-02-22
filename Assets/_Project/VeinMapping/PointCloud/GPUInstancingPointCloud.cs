using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Uses GPU instancing for mesh rendering, and a compute shader for billboarding.
/// </summary>
public class GPUInstancingPointCloud : MonoBehaviour
{
    private const int DefaultBufferSize = 256 * 256;
    private const int ResizePadding = 100;
    private static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        matricesId = Shader.PropertyToID("_Matrices"),
        positionCountId = Shader.PropertyToID("_PositionCount"),
        cameraPositionId = Shader.PropertyToID("_CameraPosition"),
        pointColorId = Shader.PropertyToID("_Color"),
        pointSizeId = Shader.PropertyToID("_PointSize");

    [SerializeField]
    private ComputeShader computeShader;
    [SerializeField]
    private Material material;
    [SerializeField]
    private Mesh mesh;
    [SerializeField, Range(0.001f, 1f)]
    private float pointSize = 0.001f;
    [SerializeField] 
    private BoolEvent onVisibilityToggled;
    
    private ComputeBuffer _positionsBuffer;
    private ComputeBuffer _matricesBuffer;
    private int _positionCount;
    private int _threadGroupsX;
    private uint _threadGroupSizeX;
    private Transform _camTransform;
    private Bounds _bounds;
    private bool _visible = true;

    private void OnEnable()
    {
        _positionsBuffer = new ComputeBuffer(DefaultBufferSize, sizeof(float) * 4);
        _matricesBuffer = new ComputeBuffer(DefaultBufferSize, sizeof(float) * 16);

        computeShader.SetBuffer(0, positionsId, _positionsBuffer);
        computeShader.SetBuffer(0, matricesId, _matricesBuffer);
        computeShader.SetFloat(pointSizeId, pointSize);
        computeShader.GetKernelThreadGroupSizes(0, out _threadGroupSizeX, out _, out _);

        material.SetBuffer(positionsId, _positionsBuffer);
        material.SetBuffer(matricesId, _matricesBuffer);

        _camTransform = Camera.main.transform;
        
        onVisibilityToggled.Register(SetPointCloudVisibility);
    }

    private void OnDisable()
    {
        _positionsBuffer.Release();
        _positionsBuffer = null;
        _matricesBuffer.Release();
        _matricesBuffer = null;
        
        onVisibilityToggled.Unregister(SetPointCloudVisibility);
    }

    private void Update()
    {
        RenderPoints();
    }

    /// <summary>
    /// Updates the list of points that will be rendered.
    /// </summary>
    /// <param name="positions">
    /// The first three elements of each Vector4 represent the position in world space and the last
    /// element is a value between zero and one which is used as the alpha for that particular point.
    /// </param>
    /// <param name="bounds">
    /// The world space bounding volume of the point cloud.
    /// This is used for camera frustum culling.
    /// </param>
    public void UpdatePoints(List<Vector4> positions, Bounds bounds)
    {
        _positionCount = positions.Count;
        AdaptBufferSize(_positionCount);
        _bounds = bounds;
        _positionsBuffer.SetData(positions);
        _threadGroupsX = Mathf.CeilToInt((float)_positionCount / _threadGroupSizeX);
        computeShader.SetInt(positionCountId, _positionCount);
    }

    /// <summary>
    /// Sets the color of the point cloud. Alpha component is ignored because that's
    /// controlled by the fourth component of each point.
    /// </summary>
    /// <param name="color">The color of the point cloud.</param>
    public void SetColor(Color color)
    {
        material.SetColor(pointColorId, color);
    }

    private void RenderPoints()
    {
        if (_positionCount == 0 || !_visible)
        {
            return;
        }

        computeShader.SetVector(cameraPositionId, _camTransform.position);
        computeShader.Dispatch(0, _threadGroupsX, 1, 1);

        Graphics.DrawMeshInstancedProcedural(
            mesh,
            submeshIndex: 0,
            material,
            _bounds,
            _positionCount,
            castShadows: ShadowCastingMode.Off,
            receiveShadows: false);
    }

    private void AdaptBufferSize(int newSize)
    {
        if (newSize > _positionsBuffer.count)
        {
            Debug.Log($"[GPUInstancingPointCloud] Resizing buffers: {_positionsBuffer.count} -> {newSize}");
            _positionsBuffer.Release();
            _matricesBuffer.Release();
            var newCount = newSize + ResizePadding;
            _positionsBuffer = new ComputeBuffer(newCount, sizeof(float) * 4);
            _matricesBuffer = new ComputeBuffer(newCount, sizeof(float) * 16);
        }
    }

    private void SetPointCloudVisibility(bool visible)
    {
        _visible = visible;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_bounds.center, _bounds.size);
    }
#endif
}
