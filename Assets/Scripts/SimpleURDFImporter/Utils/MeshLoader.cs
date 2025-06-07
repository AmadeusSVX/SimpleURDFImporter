using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SimpleURDFImporter.Utils
{
    public static class MeshLoader
    {
        public static GameObject LoadMesh(string filename, string basePath = "")
        {
            if (string.IsNullOrEmpty(filename))
                return null;
                
            // Handle package:// URLs commonly used in ROS
            if (filename.StartsWith("package://"))
            {
                filename = filename.Replace("package://", "");
                var parts = filename.Split('/');
                if (parts.Length > 1)
                {
                    // Skip package name and reconstruct path
                    filename = string.Join("/", parts, 1, parts.Length - 1);
                }
            }
            
            // Try to find the mesh file
            string[] searchPaths = {
                Path.Combine(basePath, filename),
                Path.Combine(Application.dataPath, "SampleURDFs", filename),
                Path.Combine(Application.dataPath, filename),
                filename
            };
            
            foreach (var searchPath in searchPaths)
            {
                if (File.Exists(searchPath))
                {
                    return LoadMeshFromPath(searchPath);
                }
            }
            
            Debug.LogWarning($"Mesh file not found: {filename}");
            return null;
        }
        
        private static GameObject LoadMeshFromPath(string path)
        {
            var extension = Path.GetExtension(path).ToLower();
            
            switch (extension)
            {
                case ".obj":
                    return LoadOBJ(path);
                case ".stl":
                    return LoadSTL(path);
                case ".dae":
                    return LoadDAE(path);
                default:
                    Debug.LogWarning($"Unsupported mesh format: {extension}");
                    return null;
            }
        }
        
        private static GameObject LoadOBJ(string path)
        {
            // Simple OBJ loader implementation
            // In a real implementation, you would use a proper OBJ loader library
            #if UNITY_EDITOR
            // In editor, try to use Unity's built-in importer
            var assetPath = ConvertToAssetPath(path);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (obj != null)
                    return GameObject.Instantiate(obj);
            }
            #endif
            
            // Fallback to basic implementation
            Debug.LogWarning($"OBJ loading not fully implemented. Using placeholder for: {path}");
            return CreatePlaceholderMesh();
        }
        
        private static GameObject LoadSTL(string path)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                Mesh mesh = ParseSTL(fileData);
                
                if (mesh != null)
                {
                    GameObject go = new GameObject(Path.GetFileNameWithoutExtension(path));
                    MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
                    
                    meshFilter.mesh = mesh;
                    meshRenderer.material = new Material(Shader.Find("Standard"));
                    
                    return go;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load STL file: {path}. Error: {e.Message}");
            }
            
            return CreatePlaceholderMesh();
        }
        
        private static GameObject LoadDAE(string path)
        {
            // COLLADA loader implementation
            // In a real implementation, you would parse the DAE file
            #if UNITY_EDITOR
            var assetPath = ConvertToAssetPath(path);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (obj != null)
                    return GameObject.Instantiate(obj);
            }
            #endif
            
            Debug.LogWarning($"DAE loading not fully implemented. Using placeholder for: {path}");
            return CreatePlaceholderMesh();
        }
        
        private static GameObject CreatePlaceholderMesh()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "PlaceholderMesh";
            return go;
        }
        
        #if UNITY_EDITOR
        private static string ConvertToAssetPath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            return null;
        }
        #endif
        
        public static GameObject CreatePrimitive(SimpleURDFImporter.Data.URDFGeometry geometry, string basePath = "")
        {
            GameObject go = null;
            
            switch (geometry.type)
            {
                case SimpleURDFImporter.Data.URDFGeometry.GeometryType.Box:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = geometry.size;
                    break;
                    
                case SimpleURDFImporter.Data.URDFGeometry.GeometryType.Cylinder:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(
                        geometry.radius * 2f,
                        geometry.length * 0.5f,
                        geometry.radius * 2f
                    );
                    break;
                    
                case SimpleURDFImporter.Data.URDFGeometry.GeometryType.Sphere:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = Vector3.one * (geometry.radius * 2f);
                    break;
                    
                case SimpleURDFImporter.Data.URDFGeometry.GeometryType.Mesh:
                    go = LoadMesh(geometry.filename, basePath);
                    if (go != null && geometry.size != Vector3.zero)
                    {
                        go.transform.localScale = geometry.size;
                    }
                    break;
            }
            
            return go;
        }
        
        private static Mesh ParseSTL(byte[] fileData)
        {
            if (fileData.Length < 84)
            {
                Debug.LogError("STL file is too small");
                return null;
            }
            
            // Check if it's ASCII or binary STL
            string header = Encoding.ASCII.GetString(fileData, 0, Math.Min(fileData.Length, 80)).ToLower();
            if (header.StartsWith("solid") && !IsBinarySTL(fileData))
            {
                return ParseASCIISTL(Encoding.ASCII.GetString(fileData));
            }
            else
            {
                return ParseBinarySTL(fileData);
            }
        }
        
        private static bool IsBinarySTL(byte[] fileData)
        {
            // Binary STL has 80-byte header + 4-byte triangle count
            if (fileData.Length < 84) return false;
            
            uint triangleCount = BitConverter.ToUInt32(fileData, 80);
            int expectedSize = 84 + (int)(triangleCount * 50); // 50 bytes per triangle
            
            return fileData.Length == expectedSize;
        }
        
        private static Mesh ParseBinarySTL(byte[] fileData)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            
            // Skip 80-byte header
            int offset = 80;
            
            // Read triangle count
            uint triangleCount = BitConverter.ToUInt32(fileData, offset);
            offset += 4;
            
            // Read triangles
            for (int i = 0; i < triangleCount; i++)
            {
                // Read normal (12 bytes)
                Vector3 normal = new Vector3(
                    BitConverter.ToSingle(fileData, offset),
                    BitConverter.ToSingle(fileData, offset + 4),
                    BitConverter.ToSingle(fileData, offset + 8)
                );
                offset += 12;
                
                // Read 3 vertices (36 bytes)
                Vector3[] triVerts = new Vector3[3];
                for (int j = 0; j < 3; j++)
                {
                    triVerts[j] = new Vector3(
                        BitConverter.ToSingle(fileData, offset),
                        BitConverter.ToSingle(fileData, offset + 4),
                        BitConverter.ToSingle(fileData, offset + 8)
                    );
                    offset += 12;
                }
                
                // Convert from ROS (X-forward, Y-left, Z-up) to Unity (X-right, Y-up, Z-forward)
                // ROS->Unity: (x,y,z) -> (x,z,y)
                // Also reverse winding order for left-handed coordinate system
                for (int j = 2; j >= 0; j--) // Reverse order: 2, 1, 0
                {
                    Vector3 vertex = triVerts[j];
                    vertices.Add(new Vector3(vertex.x, vertex.z, vertex.y));
                    normals.Add(new Vector3(normal.x, normal.z, normal.y));
                    triangles.Add(vertices.Count - 1);
                }
                
                // Skip attribute byte count (2 bytes)
                offset += 2;
            }
            
            // Assign to mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            
            // Calculate bounds
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private static Mesh ParseASCIISTL(string fileContent)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            
            string[] lines = fileContent.Split('\n');
            Vector3 currentNormal = Vector3.zero;
            List<Vector3> currentTriangle = new List<Vector3>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                if (line.StartsWith("facet normal"))
                {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        float nx = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                        float ny = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                        float nz = float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);
                        // Convert from ROS to Unity coordinates
                        currentNormal = new Vector3(nx, nz, ny);
                    }
                    currentTriangle.Clear();
                }
                else if (line.StartsWith("vertex"))
                {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                        float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                        float z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                        
                        currentTriangle.Add(new Vector3(x, y, z));
                        
                        // When we have 3 vertices, add the triangle with reversed winding
                        if (currentTriangle.Count == 3)
                        {
                            // Reverse winding order for left-handed coordinate system
                            for (int j = 2; j >= 0; j--)
                            {
                                Vector3 v = currentTriangle[j];
                                // Convert from ROS to Unity coordinates
                                vertices.Add(new Vector3(v.x, v.z, v.y));
                                normals.Add(currentNormal);
                                triangles.Add(vertices.Count - 1);
                            }
                        }
                    }
                }
            }
            
            // Assign to mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            
            // Calculate bounds
            mesh.RecalculateBounds();
            
            return mesh;
        }
    }
}