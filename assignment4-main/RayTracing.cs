using System.Collections.Generic; 
using UnityEngine;

public class RayTracing : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public Light DirectionalLight;

    [Header("Spheres")]
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;

    private Camera _camera;
    private float _lastFieldOfView;
    private RenderTexture _target;
    private Material _addMaterial;
    private uint _currentSample = 0;
    private ComputeBuffer _sphereBuffer;
    private List<Transform> _transformsToWatch = new List<Transform>();

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        _transformsToWatch.Add(transform);
        _transformsToWatch.Add(DirectionalLight.transform);
    }

    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    private void Update()
    {
        if (_camera.fieldOfView != _lastFieldOfView)
        {
            _currentSample = 0;
            _lastFieldOfView = _camera.fieldOfView;
        }

        foreach (Transform t in _transformsToWatch)
        {
            if (t.hasChanged)
            {
                _currentSample = 0;
                t.hasChanged = false;
            }
        }
    }

    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();

        //add number of random spheres
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();

            //position and radius
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            //reject spheres intersecting others
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }

            //albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector4.zero : new Vector4(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector4(color.r, color.g, color.b) : new Vector4(0.04f, 0.04f, 0.04f);

            //add sphere list
            spheres.Add(sphere);

            SkipSphere:
            continue;
        }

        //assign to compute buffer
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
        if (spheres.Count > 0)
        {
            _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
            _sphereBuffer.SetData(spheres);
        }
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));

        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));

        if (_sphereBuffer != null)
            RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            //release render texture if possible
            if (_target != null)
                _target.Release();

            //get render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();

            //reset sampling
            _currentSample = 0;
        }
    }

    private void Render(RenderTexture destination)
    {
        //check current render target
        InitRenderTexture();

        //set target and dispatch compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        //blit result texture to screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, destination, _addMaterial);
        _currentSample++;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }
}
