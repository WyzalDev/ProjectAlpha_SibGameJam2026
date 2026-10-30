using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Game.Scripts.Core.Features.Enemies.Scanner
{
    /// <summary>
    /// Generates the spherical-sector mesh used as the visible radar/vision volume.
    /// The GameObject should be parented to the head bone and aligned so local +Z is forward.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RobotScannerView : MonoBehaviour
    {
        [SerializeField] private RobotScannerData _data;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Light _scannerLight;
        [SerializeField] private RobotScannerVisualState _editorPreviewState = RobotScannerVisualState.Idle;
        [SerializeField] private bool _showPreviewInEditor = true;

        private MeshFilter _meshFilter;
        private Mesh _generatedMesh;
        private MaterialPropertyBlock _propertyBlock;
        private RobotScannerVisualState _currentState = RobotScannerVisualState.Idle;
        private bool _isScanning = true;

        public RobotScannerData Data => _data;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        
        private bool _rebuildRequested;

        private void OnEnable()
        {
            RobotScannerData.OnDataChanged -= HandleDataChanged;
            RobotScannerData.OnDataChanged += HandleDataChanged;
            
            CacheComponents();
            RebuildMesh();
            ApplyColor(_currentState);
            ApplyVisibility();
        }

        private void OnDisable()
        {
            RobotScannerData.OnDataChanged -= HandleDataChanged;
        }

        private void Awake()
        {
            CacheComponents();
            RebuildMesh();
            ApplyColor(_currentState);
        }

        private void Update()
        {
            if (!_rebuildRequested)
                return;

            _rebuildRequested = false;

            RebuildMesh();

            ApplyColor(Application.isPlaying
                ? _currentState
                : _editorPreviewState);
        }
        
        private void HandleDataChanged(RobotScannerData changedData)
        {
            if (_data != changedData)
                return;

            _rebuildRequested = true;
        }

        private void OnValidate()
        {
            CacheComponents();
            RebuildMesh();
            ApplyColor(Application.isPlaying ? _currentState : _editorPreviewState);
            ApplyVisibility();
        }

        private void OnDestroy()
        {
            DestroyGeneratedMesh();
        }

        private void CacheComponents()
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
        }

        public void SetVisualState(RobotScannerVisualState state)
        {
            _currentState = state;
            ApplyColor(state);
        }

        public void SetVisible(bool visible)
        {
            _isScanning = visible;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (_meshRenderer != null)
                _meshRenderer.enabled = Application.isPlaying ? _isScanning : _showPreviewInEditor;

            if (_scannerLight != null)
                _scannerLight.enabled = Application.isPlaying ? _isScanning : _showPreviewInEditor;
        }

        private void ApplyColor(RobotScannerVisualState state)
        {
            if (_data == null)
                return;

            Color color = _data.GetColor(state);
            color.a = _data.VisualAlpha;

            if (_meshRenderer != null)
            {
                if (_propertyBlock == null)
                    _propertyBlock = new MaterialPropertyBlock();

                _meshRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                _propertyBlock.SetColor(EmissionColorId, new Color(color.r, color.g, color.b, 1f));
                _meshRenderer.SetPropertyBlock(_propertyBlock);
            }

            if (_scannerLight != null)
                _scannerLight.color = color;
        }

        private void RebuildMesh()
        {
            if (_meshFilter == null)
                return;

            DestroyGeneratedMesh();

            if (_data == null)
            {
                _meshFilter.sharedMesh = null;
                return;
            }

            _generatedMesh = CreateSectorMesh(_data);
            _generatedMesh.name = $"{name}_ScannerSector";
            _meshFilter.sharedMesh = _generatedMesh;
        }

        private void DestroyGeneratedMesh()
        {
            if (_generatedMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(_generatedMesh);
            else
                DestroyImmediate(_generatedMesh);

            _generatedMesh = null;
        }

        private static Mesh CreateSectorMesh(RobotScannerData data)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            int hSegments = Mathf.Max(4, data.HorizontalSegments);
            int vSegments = Mathf.Max(2, data.VerticalSegments);
            int rSegments = Mathf.Max(1, data.RadialSegments);

            float halfHorizontal = data.HorizontalAngle * 0.5f;
            float halfVertical = data.VerticalAngle * 0.5f;
            float range = data.Range;

            // Curved spherical cap at the scanner's maximum range.
            int capStart = vertices.Count;
            for (int v = 0; v <= vSegments; v++)
            {
                float vertical = Mathf.Lerp(-halfVertical, halfVertical, v / (float)vSegments);
                for (int h = 0; h <= hSegments; h++)
                {
                    float horizontal = Mathf.Lerp(-halfHorizontal, halfHorizontal, h / (float)hSegments);
                    vertices.Add(Direction(horizontal, vertical) * range);
                }
            }

            for (int v = 0; v < vSegments; v++)
            {
                for (int h = 0; h < hSegments; h++)
                {
                    int a = capStart + v * (hSegments + 1) + h;
                    int b = a + hSegments + 1;
                    int c = a + 1;
                    int d = b + 1;

                    // Winding faces outward around +Z. The material should be double-sided too.
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                    triangles.Add(b);
                }
            }

            // Four radial boundary walls close the spherical sector.
            AddHorizontalBoundary(vertices, triangles, -halfHorizontal, halfVertical, rSegments, vSegments, range, true);
            AddHorizontalBoundary(vertices, triangles, halfHorizontal, halfVertical, rSegments, vSegments, range, false);
            AddVerticalBoundary(vertices, triangles, -halfVertical, halfHorizontal, rSegments, hSegments, range, false);
            AddVerticalBoundary(vertices, triangles, halfVertical, halfHorizontal, rSegments, hSegments, range, true);

            var mesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 Direction(float horizontalDegrees, float verticalDegrees)
        {
            float horizontal = horizontalDegrees * Mathf.Deg2Rad;
            float vertical = verticalDegrees * Mathf.Deg2Rad;
            float cosVertical = Mathf.Cos(vertical);

            return new Vector3(
                cosVertical * Mathf.Sin(horizontal),
                Mathf.Sin(vertical),
                cosVertical * Mathf.Cos(horizontal));
        }

        private static void AddHorizontalBoundary(
            List<Vector3> vertices,
            List<int> triangles,
            float horizontal,
            float halfVertical,
            int radialSegments,
            int verticalSegments,
            float range,
            bool reverse)
        {
            int start = vertices.Count;
            for (int r = 0; r <= radialSegments; r++)
            {
                float radius = range * r / radialSegments;
                for (int v = 0; v <= verticalSegments; v++)
                {
                    float vertical = Mathf.Lerp(-halfVertical, halfVertical, v / (float)verticalSegments);
                    vertices.Add(Direction(horizontal, vertical) * radius);
                }
            }

            AddGridTriangles(triangles, start, radialSegments, verticalSegments, reverse);
        }

        private static void AddVerticalBoundary(
            List<Vector3> vertices,
            List<int> triangles,
            float vertical,
            float halfHorizontal,
            int radialSegments,
            int horizontalSegments,
            float range,
            bool reverse)
        {
            int start = vertices.Count;
            for (int r = 0; r <= radialSegments; r++)
            {
                float radius = range * r / radialSegments;
                for (int h = 0; h <= horizontalSegments; h++)
                {
                    float horizontal = Mathf.Lerp(-halfHorizontal, halfHorizontal, h / (float)horizontalSegments);
                    vertices.Add(Direction(horizontal, vertical) * radius);
                }
            }

            AddGridTriangles(triangles, start, radialSegments, horizontalSegments, reverse);
        }

        private static void AddGridTriangles(
            List<int> triangles,
            int start,
            int rowSegments,
            int columnSegments,
            bool reverse)
        {
            int rowLength = columnSegments + 1;
            for (int row = 0; row < rowSegments; row++)
            {
                for (int column = 0; column < columnSegments; column++)
                {
                    int a = start + row * rowLength + column;
                    int b = a + rowLength;
                    int c = a + 1;
                    int d = b + 1;

                    if (!reverse)
                    {
                        triangles.Add(a); triangles.Add(c); triangles.Add(b);
                        triangles.Add(c); triangles.Add(d); triangles.Add(b);
                    }
                    else
                    {
                        triangles.Add(a); triangles.Add(b); triangles.Add(c);
                        triangles.Add(c); triangles.Add(b); triangles.Add(d);
                    }
                }
            }
        }
    }
}
