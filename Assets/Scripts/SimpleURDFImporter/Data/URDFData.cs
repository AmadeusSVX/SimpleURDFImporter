using System.Collections.Generic;
using UnityEngine;

namespace SimpleURDFImporter.Data
{
    [System.Serializable]
    public class URDFRobot
    {
        public string name;
        public List<URDFLink> links = new List<URDFLink>();
        public List<URDFJoint> joints = new List<URDFJoint>();
    }

    [System.Serializable]
    public class URDFLink
    {
        public string name;
        public URDFVisual visual;
        public URDFCollision collision;
        public URDFInertial inertial;
    }

    [System.Serializable]
    public class URDFVisual
    {
        public URDFOrigin origin;
        public URDFGeometry geometry;
        public URDFMaterial material;
    }

    [System.Serializable]
    public class URDFCollision
    {
        public URDFOrigin origin;
        public URDFGeometry geometry;
    }

    [System.Serializable]
    public class URDFInertial
    {
        public URDFOrigin origin;
        public float mass;
        public URDFInertia inertia;
    }

    [System.Serializable]
    public class URDFOrigin
    {
        public Vector3 xyz = Vector3.zero;
        public Vector3 rpy = Vector3.zero; // Roll, Pitch, Yaw in radians
    }

    [System.Serializable]
    public class URDFGeometry
    {
        public GeometryType type;
        public string filename; // For mesh
        public Vector3 size; // For box
        public float radius; // For cylinder, sphere
        public float length; // For cylinder
        
        public enum GeometryType
        {
            Box,
            Cylinder,
            Sphere,
            Mesh
        }
    }

    [System.Serializable]
    public class URDFMaterial
    {
        public string name;
        public URDFColor color;
        public string texture;
    }

    [System.Serializable]
    public class URDFColor
    {
        public float r = 1f;
        public float g = 1f;
        public float b = 1f;
        public float a = 1f;
        
        public Color ToUnityColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [System.Serializable]
    public class URDFInertia
    {
        public float ixx;
        public float ixy;
        public float ixz;
        public float iyy;
        public float iyz;
        public float izz;
    }

    [System.Serializable]
    public class URDFJoint
    {
        public string name;
        public JointType type;
        public string parent;
        public string child;
        public URDFOrigin origin;
        public URDFAxis axis;
        public URDFLimit limit;
        public URDFDynamics dynamics;
        
        public enum JointType
        {
            Fixed,
            Revolute,
            Continuous,
            Prismatic,
            Floating,
            Planar
        }
    }

    [System.Serializable]
    public class URDFAxis
    {
        public Vector3 xyz = Vector3.up;
    }

    [System.Serializable]
    public class URDFLimit
    {
        public float lower;
        public float upper;
        public float effort;
        public float velocity;
    }

    [System.Serializable]
    public class URDFDynamics
    {
        public float damping;
        public float friction;
    }
}