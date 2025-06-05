using System;
using System.IO;
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
            // Simple STL loader implementation
            // In a real implementation, you would parse the STL file
            Debug.LogWarning($"STL loading not fully implemented. Using placeholder for: {path}");
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
        
        public static GameObject CreatePrimitive(SimpleURDFImporter.Data.URDFGeometry geometry)
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
                    go = LoadMesh(geometry.filename);
                    if (go != null && geometry.size != Vector3.zero)
                    {
                        go.transform.localScale = geometry.size;
                    }
                    break;
            }
            
            return go;
        }
    }
}