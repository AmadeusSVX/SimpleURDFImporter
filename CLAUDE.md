# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SimpleURDFImporter is a Unity package for importing and visualizing URDF (Unified Robot Description Format) files with physics simulation support. It bridges ROS robotics standards with Unity's game engine.

## Development Environment

- Unity Version: 2022.3.52f1
- Project Type: Unity 3D project with custom assembly definition
- No traditional build commands - use Unity Editor for development and testing

## Architecture

### Core Components

1. **URDFData.cs** - Data structures mirroring URDF XML schema
2. **URDFParser.cs** - XML parsing with ROS→Unity coordinate conversion
3. **URDFImporter.cs** - Unity GameObject creation and physics setup
4. **MeshLoader.cs** - 3D file loading (STL, OBJ, DAE)

### Coordinate System Conversion

Critical transformation from ROS (Z-up, right-handed) to Unity (Y-up, left-handed):
- Position: ROS(x,y,z) → Unity(x,z,y)
- Rotation: Special handling for Roll-Pitch-Yaw conversion
- Scale: ROS(x,y,z) → Unity(x,z,y)

## Key Development Tasks

### Testing URDF Import
1. Open project in Unity Editor
2. Create empty GameObject
3. Add URDFImporter component
4. Set URDF file path and mesh root path
5. Click "Import URDF" in Inspector

### Adding New Features
- Joint types are handled in `CreateJoint()` method
- Geometry types are processed in `CreateVisualMesh()` and `CreateCollisionMesh()`
- Material handling is in `CreateMaterial()` method

### Common Code Locations
- URDF parsing logic: `URDFParser.ParseURDF()`
- Mesh loading: `MeshLoader.LoadMesh()`
- Physics setup: `URDFImporter.CreateLink()`
- Joint creation: `URDFImporter.CreateJoint()`

## Important Considerations

1. Always maintain ROS→Unity coordinate conversion consistency
2. STL files can be ASCII or binary - both must be supported
3. Joint limits and dynamics are applied to Unity's ArticulationBody
4. Collision layers can be customized via collisionLayer setting
5. Sample URDF files in Assets/SampleURDFs/ for testing