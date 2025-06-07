using System;
using System.Xml;
using System.Globalization;
using UnityEngine;
using SimpleURDFImporter.Data;

namespace SimpleURDFImporter.Core
{
    public static class URDFParser
    {
        public static URDFRobot Parse(string urdfContent)
        {
            var robot = new URDFRobot();
            var doc = new XmlDocument();
            doc.LoadXml(urdfContent);
            
            var robotNode = doc.SelectSingleNode("robot");
            if (robotNode == null)
            {
                throw new Exception("Invalid URDF: No robot element found");
            }
            
            robot.name = robotNode.Attributes["name"]?.Value ?? "unnamed_robot";
            
            // Parse links
            var linkNodes = robotNode.SelectNodes("link");
            foreach (XmlNode linkNode in linkNodes)
            {
                robot.links.Add(ParseLink(linkNode));
            }
            
            // Parse joints
            var jointNodes = robotNode.SelectNodes("joint");
            foreach (XmlNode jointNode in jointNodes)
            {
                robot.joints.Add(ParseJoint(jointNode));
            }
            
            return robot;
        }
        
        private static URDFLink ParseLink(XmlNode linkNode)
        {
            var link = new URDFLink
            {
                name = linkNode.Attributes["name"]?.Value ?? "unnamed_link"
            };
            
            var visualNode = linkNode.SelectSingleNode("visual");
            if (visualNode != null)
            {
                link.visual = ParseVisual(visualNode);
            }
            
            var collisionNode = linkNode.SelectSingleNode("collision");
            if (collisionNode != null)
            {
                link.collision = ParseCollision(collisionNode);
            }
            
            var inertialNode = linkNode.SelectSingleNode("inertial");
            if (inertialNode != null)
            {
                link.inertial = ParseInertial(inertialNode);
            }
            
            return link;
        }
        
        private static URDFVisual ParseVisual(XmlNode visualNode)
        {
            var visual = new URDFVisual();
            
            var originNode = visualNode.SelectSingleNode("origin");
            if (originNode != null)
            {
                visual.origin = ParseOrigin(originNode);
            }
            else
            {
                visual.origin = new URDFOrigin();
            }
            
            var geometryNode = visualNode.SelectSingleNode("geometry");
            if (geometryNode != null)
            {
                visual.geometry = ParseGeometry(geometryNode);
            }
            
            var materialNode = visualNode.SelectSingleNode("material");
            if (materialNode != null)
            {
                visual.material = ParseMaterial(materialNode);
            }
            
            return visual;
        }
        
        private static URDFCollision ParseCollision(XmlNode collisionNode)
        {
            var collision = new URDFCollision();
            
            var originNode = collisionNode.SelectSingleNode("origin");
            if (originNode != null)
            {
                collision.origin = ParseOrigin(originNode);
            }
            else
            {
                collision.origin = new URDFOrigin();
            }
            
            var geometryNode = collisionNode.SelectSingleNode("geometry");
            if (geometryNode != null)
            {
                collision.geometry = ParseGeometry(geometryNode);
            }
            
            return collision;
        }
        
        private static URDFInertial ParseInertial(XmlNode inertialNode)
        {
            var inertial = new URDFInertial();
            
            var originNode = inertialNode.SelectSingleNode("origin");
            if (originNode != null)
            {
                inertial.origin = ParseOrigin(originNode);
            }
            else
            {
                inertial.origin = new URDFOrigin();
            }
            
            var massNode = inertialNode.SelectSingleNode("mass");
            if (massNode != null)
            {
                inertial.mass = ParseFloat(massNode.Attributes["value"]?.Value, 1f);
            }
            
            var inertiaNode = inertialNode.SelectSingleNode("inertia");
            if (inertiaNode != null)
            {
                inertial.inertia = ParseInertia(inertiaNode);
            }
            
            return inertial;
        }
        
        private static URDFOrigin ParseOrigin(XmlNode originNode)
        {
            var origin = new URDFOrigin();
            
            var xyzAttr = originNode.Attributes["xyz"]?.Value;
            if (!string.IsNullOrEmpty(xyzAttr))
            {
                var rosPosition = ParseVector3(xyzAttr);
                origin.xyz = ConvertROSToUnityPosition(rosPosition);
            }
            
            var rpyAttr = originNode.Attributes["rpy"]?.Value;
            if (!string.IsNullOrEmpty(rpyAttr))
            {
                var rosRotation = ParseVector3(rpyAttr);
                origin.rpy = ConvertROSToUnityRotation(rosRotation);
            }
            
            return origin;
        }
        
        private static URDFGeometry ParseGeometry(XmlNode geometryNode)
        {
            var geometry = new URDFGeometry();
            
            var boxNode = geometryNode.SelectSingleNode("box");
            if (boxNode != null)
            {
                geometry.type = URDFGeometry.GeometryType.Box;
                geometry.size = ParseVector3(boxNode.Attributes["size"]?.Value);
                return geometry;
            }
            
            var cylinderNode = geometryNode.SelectSingleNode("cylinder");
            if (cylinderNode != null)
            {
                geometry.type = URDFGeometry.GeometryType.Cylinder;
                geometry.radius = ParseFloat(cylinderNode.Attributes["radius"]?.Value, 0.5f);
                geometry.length = ParseFloat(cylinderNode.Attributes["length"]?.Value, 1f);
                return geometry;
            }
            
            var sphereNode = geometryNode.SelectSingleNode("sphere");
            if (sphereNode != null)
            {
                geometry.type = URDFGeometry.GeometryType.Sphere;
                geometry.radius = ParseFloat(sphereNode.Attributes["radius"]?.Value, 0.5f);
                return geometry;
            }
            
            var meshNode = geometryNode.SelectSingleNode("mesh");
            if (meshNode != null)
            {
                geometry.type = URDFGeometry.GeometryType.Mesh;
                geometry.filename = meshNode.Attributes["filename"]?.Value;
                var scaleAttr = meshNode.Attributes["scale"]?.Value;
                if (!string.IsNullOrEmpty(scaleAttr))
                {
                    geometry.size = ParseVector3(scaleAttr);
                }
                else
                {
                    geometry.size = Vector3.one;
                }
                return geometry;
            }
            
            return geometry;
        }
        
        private static URDFMaterial ParseMaterial(XmlNode materialNode)
        {
            var material = new URDFMaterial
            {
                name = materialNode.Attributes["name"]?.Value ?? "default_material"
            };
            
            var colorNode = materialNode.SelectSingleNode("color");
            if (colorNode != null)
            {
                var rgbaAttr = colorNode.Attributes["rgba"]?.Value;
                if (!string.IsNullOrEmpty(rgbaAttr))
                {
                    var values = rgbaAttr.Split(' ');
                    material.color = new URDFColor
                    {
                        r = ParseFloat(values[0], 1f),
                        g = values.Length > 1 ? ParseFloat(values[1], 1f) : 1f,
                        b = values.Length > 2 ? ParseFloat(values[2], 1f) : 1f,
                        a = values.Length > 3 ? ParseFloat(values[3], 1f) : 1f
                    };
                }
            }
            
            var textureNode = materialNode.SelectSingleNode("texture");
            if (textureNode != null)
            {
                material.texture = textureNode.Attributes["filename"]?.Value;
            }
            
            return material;
        }
        
        private static URDFInertia ParseInertia(XmlNode inertiaNode)
        {
            return new URDFInertia
            {
                ixx = ParseFloat(inertiaNode.Attributes["ixx"]?.Value, 0f),
                ixy = ParseFloat(inertiaNode.Attributes["ixy"]?.Value, 0f),
                ixz = ParseFloat(inertiaNode.Attributes["ixz"]?.Value, 0f),
                iyy = ParseFloat(inertiaNode.Attributes["iyy"]?.Value, 0f),
                iyz = ParseFloat(inertiaNode.Attributes["iyz"]?.Value, 0f),
                izz = ParseFloat(inertiaNode.Attributes["izz"]?.Value, 0f)
            };
        }
        
        private static URDFJoint ParseJoint(XmlNode jointNode)
        {
            var joint = new URDFJoint
            {
                name = jointNode.Attributes["name"]?.Value ?? "unnamed_joint",
                type = ParseJointType(jointNode.Attributes["type"]?.Value)
            };
            
            var parentNode = jointNode.SelectSingleNode("parent");
            if (parentNode != null)
            {
                joint.parent = parentNode.Attributes["link"]?.Value;
            }
            
            var childNode = jointNode.SelectSingleNode("child");
            if (childNode != null)
            {
                joint.child = childNode.Attributes["link"]?.Value;
            }
            
            var originNode = jointNode.SelectSingleNode("origin");
            if (originNode != null)
            {
                joint.origin = ParseOrigin(originNode);
            }
            else
            {
                joint.origin = new URDFOrigin();
            }
            
            var axisNode = jointNode.SelectSingleNode("axis");
            if (axisNode != null)
            {
                var rosAxis = ParseVector3(axisNode.Attributes["xyz"]?.Value);
                joint.axis = new URDFAxis
                {
                    xyz = ConvertROSToUnityAxis(rosAxis)
                };
            }
            else
            {
                joint.axis = new URDFAxis();
            }
            
            var limitNode = jointNode.SelectSingleNode("limit");
            if (limitNode != null)
            {
                joint.limit = ParseLimit(limitNode);
            }
            
            var dynamicsNode = jointNode.SelectSingleNode("dynamics");
            if (dynamicsNode != null)
            {
                joint.dynamics = ParseDynamics(dynamicsNode);
            }
            
            return joint;
        }
        
        private static URDFJoint.JointType ParseJointType(string type)
        {
            switch (type?.ToLower())
            {
                case "fixed": return URDFJoint.JointType.Fixed;
                case "revolute": return URDFJoint.JointType.Revolute;
                case "continuous": return URDFJoint.JointType.Continuous;
                case "prismatic": return URDFJoint.JointType.Prismatic;
                case "floating": return URDFJoint.JointType.Floating;
                case "planar": return URDFJoint.JointType.Planar;
                default: return URDFJoint.JointType.Fixed;
            }
        }
        
        private static URDFLimit ParseLimit(XmlNode limitNode)
        {
            return new URDFLimit
            {
                lower = ParseFloat(limitNode.Attributes["lower"]?.Value, 0f),
                upper = ParseFloat(limitNode.Attributes["upper"]?.Value, 0f),
                effort = ParseFloat(limitNode.Attributes["effort"]?.Value, 0f),
                velocity = ParseFloat(limitNode.Attributes["velocity"]?.Value, 0f)
            };
        }
        
        private static URDFDynamics ParseDynamics(XmlNode dynamicsNode)
        {
            return new URDFDynamics
            {
                damping = ParseFloat(dynamicsNode.Attributes["damping"]?.Value, 0f),
                friction = ParseFloat(dynamicsNode.Attributes["friction"]?.Value, 0f)
            };
        }
        
        private static Vector3 ParseVector3(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Vector3.zero;
                
            var values = value.Split(' ');
            return new Vector3(
                ParseFloat(values[0], 0f),
                values.Length > 1 ? ParseFloat(values[1], 0f) : 0f,
                values.Length > 2 ? ParseFloat(values[2], 0f) : 0f
            );
        }
        
        // Convert from ROS coordinate system (Z-up, right-handed) to Unity coordinate system (Y-up, left-handed)
        private static Vector3 ConvertROSToUnityPosition(Vector3 rosPosition)
        {
            // ROS: X-forward, Y-left, Z-up
            // Unity: X-right, Y-up, Z-forward
            // Correct mapping: ROS(x,y,z) -> Unity(x,z,y) but with sign changes for handedness
            return new Vector3(rosPosition.x, rosPosition.z, rosPosition.y);
        }
        
        // Convert from ROS rotation (Roll-Pitch-Yaw) to Unity rotation
        private static Vector3 ConvertROSToUnityRotation(Vector3 rosRPY)
        {
            // ROS uses Roll-Pitch-Yaw around X-Y-Z axes respectively
            // Unity uses Euler angles in X-Y-Z order 
            // ROS to Unity axis mapping:
            // ROS Roll (X) -> Unity Roll (Z)
            // ROS Pitch (Y) -> Unity Pitch (-X) 
            // ROS Yaw (Z) -> Unity Yaw (Y)
            // Note: Return in radians, ApplyOrigin will convert to degrees
            return new Vector3(-rosRPY.y, rosRPY.z, rosRPY.x);
        }
        
        // Convert axis vector from ROS to Unity
        private static Vector3 ConvertROSToUnityAxis(Vector3 rosAxis)
        {
            // ROS (right-handed): X-forward, Y-left, Z-up
            // Unity (left-handed): X-right, Y-up, Z-forward
            // 
            // Coordinate transformation with handedness consideration:
            // ROS X (forward) → Unity Z (forward)
            // ROS Y (left) → Unity -X (right, but flipped for handedness)  
            // ROS Z (up) → Unity Y (up)
            return new Vector3(-rosAxis.y, rosAxis.z, rosAxis.x);
        }
        
        private static float ParseFloat(string value, float defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                return result;
                
            return defaultValue;
        }
    }
}