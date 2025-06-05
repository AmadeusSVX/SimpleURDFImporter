# SimpleURDFImporter

A Unity package for importing and visualizing URDF (Unified Robot Description Format) files with physics simulation support.

## Features

- **URDF Parsing**: Reads URDF XML files and converts them to Unity GameObjects
- **3D Model Support**: Loads visual meshes from various formats (OBJ, STL, DAE)
- **Collision Detection**: Creates MeshColliders from collision geometry
- **Physics Simulation**: Sets up RigidBody components with proper mass and inertia
- **Joint System**: Supports various joint types (fixed, revolute, continuous, prismatic)
- **Material Support**: Applies colors and materials from URDF definitions

## Installation

1. Clone or download this repository
2. Copy the `Assets/Scripts/SimpleURDFImporter` folder to your Unity project
3. Import the sample URDF files from `Assets/SampleURDFs` if needed

## Usage

### Basic Usage

1. Create an empty GameObject in your scene
2. Add the `URDFImporter` component to it
3. Set the URDF file path or drag a URDF file to the inspector
4. Click "Import URDF" button or enable "Import On Start"

### Script Usage

```csharp
using SimpleURDFImporter.Components;

// Import from file
URDFImporter importer = gameObject.AddComponent<URDFImporter>();
importer.ImportURDF("path/to/your/robot.urdf");

// Import from string
string urdfContent = File.ReadAllText("robot.urdf");
importer.ImportURDFFromString(urdfContent, basePath);
```

## Component Settings

- **URDF File Path**: Path to the URDF file
- **Import On Start**: Automatically import when the scene starts
- **Use Gravity**: Enable gravity for physics simulation
- **Scale Factor**: Scale the imported model
- **Import Visual Meshes**: Import visual geometry
- **Import Collision Meshes**: Import collision geometry
- **Create Physics**: Add RigidBody components
- **Create Joints**: Create Unity joints from URDF joints

## Supported URDF Elements

### Links
- Visual geometry (box, cylinder, sphere, mesh)
- Collision geometry
- Inertial properties (mass, inertia tensor)

### Joints
- Fixed
- Revolute (with limits)
- Continuous
- Prismatic
- Floating (limited support)
- Planar (limited support)

### Materials
- Colors (RGBA)
- Named materials

## Sample URDF

A simple robot URDF is included in `Assets/SampleURDFs/simple_robot.urdf` for testing.

## Limitations

- Mesh loading requires additional libraries for full format support
- Complex joint constraints may need manual adjustment
- Texture loading is not fully implemented

## License

MIT License