# Unity VR Template Project

A clean, minimal Unity VR template project for stationary 3D VR experiences with accurate depth perception.

## Project Overview

This template provides a foundation for developing stationary VR experiences with proper stereoscopic rendering and accurate depth perception. It has been cleaned from a complex vision therapy application to provide only the essential VR functionality.

## Project Setup

- **Unity Version**: 2022.3.61f1
- **Platform**: Meta Quest (OpenXR compatible)
- **VR SDKs**: Meta XR SDK 76.0.0, XR Interaction Toolkit 2.6.4
- **Rendering**: Built-in Render Pipeline with VR optimizations

## Key Features

✅ **Accurate Depth Perception**: Proper stereoscopic setup with OVRCameraRig  
✅ **Stationary VR Experience**: No locomotion, perfect for seated/standing experiences  
✅ **Gaze-Based Interaction**: Simple pointer-based interaction system  
✅ **Clean Codebase**: Minimal, well-documented scripts  
✅ **VR-Optimized**: Configured for Meta Quest with OpenXR  

## Getting Started

1. **Open Project**: Load in Unity 2022.3.61f1 or newer
2. **VR Setup**: Ensure Meta Quest is connected and drivers installed
3. **Scene**: Open `Assets/Scenes/VRTemplate.unity`
4. **Test**: Press Play to test in editor or build to device

## Scene Structure

The VRTemplate scene includes:
- **OVRCameraRig**: Stereo camera setup with accurate IPD
- **UI Canvas**: World-space canvas for VR interaction
- **Lighting**: Basic directional light for depth perception
- **Skybox**: Simple gradient skybox

## Scripts Overview

| Script | Purpose |
|--------|---------|
| `VRApplicationManager` | Simple singleton for app lifecycle management |
| `SimpleVRMenu` | Basic VR menu with gaze interaction |
| `GazePointerController` | Handles gaze-based UI interaction |
| `InteractableMenuItem` | Makes UI elements interactable via gaze |
| `FollowHMD` | Makes objects follow VR headset position |

## VR Configuration

### Optimized For:
- **Tracking**: 3DOF/6DOF head tracking (stationary experience)
- **Input**: Gaze-based interaction + controller support
- **Rendering**: Stereo rendering with proper IPD settings
- **Performance**: Optimized for mobile VR (Quest)

### Depth Perception Settings:
- Stereo separation configured for natural depth
- Near/far clip planes optimized for room-scale
- Eye anchor positioning for accurate stereoscopy

## Customization Guide

### Adding Content:
1. Place 3D objects in the scene
2. Ensure proper scale (1 Unity unit = 1 meter)
3. Add colliders for gaze interaction
4. Test depth perception with objects at varying distances

### Modifying Interaction:
- Edit `GazePointerController` for custom pointer behavior
- Modify `InteractableMenuItem` for different interaction types
- Add audio feedback in `SimpleVRMenu`

### UI Customization:
- Modify world-space canvas in the scene
- Update `SimpleVRMenu` for different menu layouts
- Adjust text and button styling

## Essential Packages

- **Meta XR Core SDK**: Core VR functionality
- **XR Interaction Toolkit**: Interaction system
- **OpenXR Plugin**: Cross-platform VR
- **TextMeshPro**: UI text rendering

## Building for Quest

1. Switch platform to Android
2. Configure XR settings for OpenXR
3. Set appropriate quality settings
4. Build and deploy to device

## Performance Considerations

- **Target**: 72Hz for Quest, 90Hz for Quest 2/Pro
- **Rendering**: Single-pass stereo enabled
- **Shadows**: Optimized shadow settings
- **Post-processing**: Minimal for mobile VR

## Depth Perception Validation

To verify accurate depth perception:
1. Place objects at 0.5m, 2m, and 5m distances
2. Test with different IPD settings
3. Verify no eye strain during extended use
4. Check stereo convergence at various depths

## Support

This template is designed for:
- Educational VR projects
- Prototyping stationary VR experiences  
- Research applications requiring accurate depth
- Simple VR applications without complex locomotion

## License

Open source template for VR development and research.