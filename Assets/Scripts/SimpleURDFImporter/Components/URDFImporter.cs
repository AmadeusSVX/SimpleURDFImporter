using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleURDFImporter.Core;
using SimpleURDFImporter.Data;
using SimpleURDFImporter.Utils;

namespace SimpleURDFImporter.Components
{
    public class URDFImporter : MonoBehaviour
    {
        [Header("URDF Settings")]
        [SerializeField] private string urdfFilePath;
        [SerializeField] private bool importOnStart = false;
        [SerializeField] private bool useGravity = true;
        [SerializeField] private float scaleFactor = 1f;
        
        [Header("Import Options")]
        [SerializeField] private bool importVisualMeshes = true;
        [SerializeField] private bool importCollisionMeshes = true;
        [SerializeField] private bool createPhysics = true;
        [SerializeField] private bool createJoints = true;
        
        [Header("Materials")]
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material collisionMaterial;
        
        private URDFRobot robot;
        private Dictionary<string, GameObject> linkObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, URDFLink> linkData = new Dictionary<string, URDFLink>();
        
        void Start()
        {
            if (importOnStart && !string.IsNullOrEmpty(urdfFilePath))
            {
                ImportURDF(urdfFilePath);
            }
        }
        
        public void ImportURDF(string path)
        {
            try
            {
                string urdfContent = File.ReadAllText(path);
                ImportURDFFromString(urdfContent, Path.GetDirectoryName(path));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import URDF: {e.Message}");
            }
        }
        
        public void ImportURDFFromString(string urdfContent, string basePath = "")
        {
            try
            {
                // Parse URDF
                robot = URDFParser.Parse(urdfContent);
                Debug.Log($"Parsed URDF: {robot.name} with {robot.links.Count} links and {robot.joints.Count} joints");
                
                // Clear existing objects
                ClearExistingRobot();
                
                // Create robot root
                GameObject robotRoot = new GameObject(robot.name);
                robotRoot.transform.SetParent(transform);
                robotRoot.transform.localPosition = Vector3.zero;
                robotRoot.transform.localRotation = Quaternion.identity;
                robotRoot.transform.localScale = Vector3.one * scaleFactor;
                
                // Create links
                foreach (var link in robot.links)
                {
                    CreateLink(link, robotRoot.transform, basePath);
                }
                
                // Create joints
                if (createJoints)
                {
                    foreach (var joint in robot.joints)
                    {
                        CreateJoint(joint);
                    }
                }
                
                Debug.Log($"Successfully imported URDF: {robot.name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import URDF: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private void CreateLink(URDFLink link, Transform parent, string basePath)
        {
            GameObject linkObject = new GameObject(link.name);
            linkObject.transform.SetParent(parent);
            linkObject.transform.localPosition = Vector3.zero;
            linkObject.transform.localRotation = Quaternion.identity;
            linkObject.transform.localScale = Vector3.one;
            
            linkObjects[link.name] = linkObject;
            linkData[link.name] = link;
            
            // Create visual mesh
            if (importVisualMeshes && link.visual != null)
            {
                CreateVisualMesh(link.visual, linkObject.transform, basePath);
            }
            
            // Create collision mesh
            if (importCollisionMeshes && link.collision != null)
            {
                CreateCollisionMesh(link.collision, linkObject.transform, basePath);
            }
            
            // Add physics components
            if (createPhysics && link.inertial != null)
            {
                AddPhysicsComponents(linkObject, link.inertial);
            }
        }
        
        private void CreateVisualMesh(URDFVisual visual, Transform parent, string basePath)
        {
            if (visual.geometry == null) return;
            
            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(parent);
            ApplyOrigin(visualObject.transform, visual.origin);
            
            GameObject meshObject = MeshLoader.CreatePrimitive(visual.geometry);
            if (meshObject != null)
            {
                meshObject.transform.SetParent(visualObject.transform);
                meshObject.transform.localPosition = Vector3.zero;
                meshObject.transform.localRotation = Quaternion.identity;
                
                // Apply material
                var renderer = meshObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (visual.material != null && visual.material.color != null)
                    {
                        Material mat = new Material(defaultMaterial != null ? defaultMaterial : new Material(Shader.Find("Standard")));
                        mat.color = visual.material.color.ToUnityColor();
                        renderer.material = mat;
                    }
                    else if (defaultMaterial != null)
                    {
                        renderer.material = defaultMaterial;
                    }
                }
                
                // Remove collider from visual mesh
                var collider = meshObject.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                        Destroy(collider);
                    else
                        DestroyImmediate(collider);
                }
            }
        }
        
        private void CreateCollisionMesh(URDFCollision collision, Transform parent, string basePath)
        {
            if (collision.geometry == null) return;
            
            GameObject collisionObject = new GameObject("Collision");
            collisionObject.transform.SetParent(parent);
            ApplyOrigin(collisionObject.transform, collision.origin);
            
            GameObject meshObject = MeshLoader.CreatePrimitive(collision.geometry);
            if (meshObject != null)
            {
                meshObject.transform.SetParent(collisionObject.transform);
                meshObject.transform.localPosition = Vector3.zero;
                meshObject.transform.localRotation = Quaternion.identity;
                
                // Setup collision
                var renderer = meshObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (collisionMaterial != null)
                    {
                        renderer.material = collisionMaterial;
                    }
                    renderer.enabled = false; // Hide collision mesh
                }
                
                // Ensure we have a collider
                var collider = meshObject.GetComponent<Collider>();
                if (collider == null)
                {
                    // Add mesh collider for custom meshes
                    var meshFilter = meshObject.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        var meshCollider = meshObject.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                    }
                }
            }
        }
        
        private void AddPhysicsComponents(GameObject linkObject, URDFInertial inertial)
        {
            // Add Rigidbody
            var rb = linkObject.AddComponent<Rigidbody>();
            rb.mass = inertial.mass;
            rb.useGravity = useGravity;
            
            // Apply center of mass
            if (inertial.origin != null)
            {
                rb.centerOfMass = inertial.origin.xyz;
            }
            
            // Apply inertia tensor
            if (inertial.inertia != null)
            {
                // Unity uses the inertia tensor diagonal
                rb.inertiaTensor = new Vector3(
                    inertial.inertia.ixx,
                    inertial.inertia.iyy,
                    inertial.inertia.izz
                );
                
                // Calculate inertia tensor rotation from off-diagonal elements
                // This is a simplified approach
                if (Mathf.Abs(inertial.inertia.ixy) > 0.001f ||
                    Mathf.Abs(inertial.inertia.ixz) > 0.001f ||
                    Mathf.Abs(inertial.inertia.iyz) > 0.001f)
                {
                    Debug.LogWarning($"Off-diagonal inertia elements detected for {linkObject.name}. Using simplified approximation.");
                }
            }
        }
        
        private void CreateJoint(URDFJoint joint)
        {
            if (!linkObjects.ContainsKey(joint.parent) || !linkObjects.ContainsKey(joint.child))
            {
                Debug.LogWarning($"Cannot create joint {joint.name}: parent or child link not found");
                return;
            }
            
            GameObject parentObject = linkObjects[joint.parent];
            GameObject childObject = linkObjects[joint.child];
            
            // Set parent-child relationship
            childObject.transform.SetParent(parentObject.transform);
            
            // Apply joint origin to child
            ApplyOrigin(childObject.transform, joint.origin);
            
            // Create Unity joint based on URDF joint type
            switch (joint.type)
            {
                case URDFJoint.JointType.Fixed:
                    CreateFixedJoint(parentObject, childObject, joint);
                    break;
                    
                case URDFJoint.JointType.Revolute:
                case URDFJoint.JointType.Continuous:
                    CreateHingeJoint(parentObject, childObject, joint);
                    break;
                    
                case URDFJoint.JointType.Prismatic:
                    CreatePrismaticJoint(parentObject, childObject, joint);
                    break;
                    
                case URDFJoint.JointType.Floating:
                case URDFJoint.JointType.Planar:
                    Debug.LogWarning($"Joint type {joint.type} not fully supported yet: {joint.name}");
                    break;
            }
        }
        
        private void CreateFixedJoint(GameObject parent, GameObject child, URDFJoint joint)
        {
            var fixedJoint = child.AddComponent<FixedJoint>();
            fixedJoint.connectedBody = parent.GetComponent<Rigidbody>();
        }
        
        private void CreateHingeJoint(GameObject parent, GameObject child, URDFJoint joint)
        {
            var hingeJoint = child.AddComponent<HingeJoint>();
            hingeJoint.connectedBody = parent.GetComponent<Rigidbody>();
            hingeJoint.axis = joint.axis.xyz;
            
            if (joint.limit != null && joint.type == URDFJoint.JointType.Revolute)
            {
                hingeJoint.useLimits = true;
                hingeJoint.limits = new JointLimits
                {
                    min = joint.limit.lower * Mathf.Rad2Deg,
                    max = joint.limit.upper * Mathf.Rad2Deg
                };
            }
            
            if (joint.dynamics != null)
            {
                hingeJoint.useSpring = true;
                hingeJoint.spring = new JointSpring
                {
                    damper = joint.dynamics.damping,
                    spring = 0f,
                    targetPosition = 0f
                };
            }
        }
        
        private void CreatePrismaticJoint(GameObject parent, GameObject child, URDFJoint joint)
        {
            var configurableJoint = child.AddComponent<ConfigurableJoint>();
            configurableJoint.connectedBody = parent.GetComponent<Rigidbody>();
            
            // Lock all rotations
            configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
            configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
            configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;
            
            // Configure linear motion based on axis
            Vector3 axis = joint.axis.xyz.normalized;
            if (Mathf.Abs(axis.x) > 0.9f)
            {
                configurableJoint.xMotion = ConfigurableJointMotion.Limited;
                configurableJoint.yMotion = ConfigurableJointMotion.Locked;
                configurableJoint.zMotion = ConfigurableJointMotion.Locked;
            }
            else if (Mathf.Abs(axis.y) > 0.9f)
            {
                configurableJoint.xMotion = ConfigurableJointMotion.Locked;
                configurableJoint.yMotion = ConfigurableJointMotion.Limited;
                configurableJoint.zMotion = ConfigurableJointMotion.Locked;
            }
            else if (Mathf.Abs(axis.z) > 0.9f)
            {
                configurableJoint.xMotion = ConfigurableJointMotion.Locked;
                configurableJoint.yMotion = ConfigurableJointMotion.Locked;
                configurableJoint.zMotion = ConfigurableJointMotion.Limited;
            }
            
            if (joint.limit != null)
            {
                configurableJoint.linearLimit = new SoftJointLimit
                {
                    limit = joint.limit.upper
                };
            }
        }
        
        private void ApplyOrigin(Transform transform, URDFOrigin origin)
        {
            if (origin == null) return;
            
            transform.localPosition = origin.xyz;
            transform.localRotation = Quaternion.Euler(
                origin.rpy.x * Mathf.Rad2Deg,
                origin.rpy.y * Mathf.Rad2Deg,
                origin.rpy.z * Mathf.Rad2Deg
            );
        }
        
        private void ClearExistingRobot()
        {
            linkObjects.Clear();
            linkData.Clear();
            
            // Remove all child objects
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
        
        [ContextMenu("Import URDF from File")]
        public void ImportFromFile()
        {
            if (!string.IsNullOrEmpty(urdfFilePath))
            {
                ImportURDF(urdfFilePath);
            }
            else
            {
                Debug.LogError("URDF file path is not set");
            }
        }
    }
}