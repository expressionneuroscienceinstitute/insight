# Unity VR Template Project

A clean, minimal Unity VR template project for stationary 3D VR experiences with accurate depth perception.

## Project Setup

- **Unity Version**: 2022.3.61f1
- **Platform**: Meta Quest (OpenXR)
- **VR SDK**: Meta XR SDK, XR Interaction Toolkit

## Features

- Basic VR scene with proper depth perception
- Simple gaze-based interaction system
- Basic VR menu system
- Stationary VR experience (no locomotion)
- Clean, minimal codebase

## Getting Started

1. Open the project in Unity 2022.3.61f1 or newer
2. Open the `VRTemplate` scene in Assets/Scenes/
3. Ensure your VR headset is connected and set up
4. Press Play to test the VR experience

## Scripts Overview

- **VRApplicationManager**: Simple singleton for basic VR app management
- **SimpleVRMenu**: Basic VR menu with gaze interaction
- **GazePointerController**: Handles gaze-based interaction
- **InteractableMenuItem**: Makes UI elements interactable via gaze
- **FollowHMD**: Makes objects follow the VR headset

## VR Setup

The project is configured for:
- OpenXR with Meta Quest support
- Accurate depth perception using proper IPD settings
- Stationary tracking (no room-scale movement)
- Hand tracking and controller support

## Customization

This template provides a minimal foundation that you can build upon:
- Add your own 3D content to the scene
- Customize the menu system
- Add additional interaction methods
- Implement your specific VR experience logic

## Package Dependencies

Essential VR packages included:
- Meta XR SDK Core
- XR Interaction Toolkit
- OpenXR Plugin
- Meta OpenXR Plugin

## License

This project is provided as a template for VR development.