# VR Main Menu System Setup Guide
## Diagnostic Vision Therapy Platform

This guide explains how to set up and use the enhanced VR main menu system for the Meta Quest Pro diagnostic vision therapy application.

## Overview

The new VR main menu system provides:
- **Full VR UX Hub**: 3D world-space menu with gaze-based interaction
- **Persistent GameManager**: DontDestroyOnLoad singleton with calibration status tracking
- **Async Scene Transitions**: Unity Addressables support with fallback to SceneManager
- **Integration**: Works with existing CalibrationManager and InteractableMenuItem systems

## New Components

### 1. Enhanced GameManager (`GameManager.cs`)
- **Singleton Pattern**: Persists across scenes with DontDestroyOnLoad
- **Calibration Status**: Tracks calibration state and validity
- **Scene Routing**: Manages navigation between scenes
- **Addressables Support**: Async scene loading with proper error handling
- **Events**: Provides state change and calibration status events

### 2. VR Main Menu (`VRMainMenu.cs`)
- **VR Integration**: Works with existing InteractableMenuItem system
- **Dynamic UI**: Updates button states based on calibration status
- **Audio Feedback**: Supports button click and hover sounds
- **Event System**: Integrates with GameManager for proper navigation

### 3. VR Menu Prefab Manager (`VRMenuPrefabManager.cs`)
- **Runtime Creation**: Programmatically creates 3D VR menu interface
- **World Space Canvas**: Properly positioned for VR interaction
- **Button Generation**: Creates all required menu buttons with proper components
- **Automatic Setup**: Configures VRMainMenu component references

### 4. VR Scene Transition Manager (`VRSceneTransitionManager.cs`)
- **Loading Screens**: Provides VR-appropriate loading feedback
- **Addressables Integration**: Handles async scene loading with error handling
- **Progress Tracking**: Visual progress indicators for scene transitions
- **Error Management**: Graceful fallback and error display

## Setup Instructions

### Step 1: Scene Setup
1. Open the `MainMenu.unity` scene
2. Add a new empty GameObject named "VR_Systems"
3. Add the following components to VR_Systems:
   - `GameManager`
   - `VRMenuPrefabManager`
   - `VRSceneTransitionManager`

### Step 2: GameManager Configuration
In the GameManager component:
1. **Scene References**: 
   - Assign Addressable references for each scene (if using Addressables)
   - Leave empty to use fallback SceneManager loading
2. **UI Panels**: These will be automatically configured by VRMenuPrefabManager

### Step 3: VR Menu Prefab Manager Configuration
In the VRMenuPrefabManager component:
1. **Menu Positioning**:
   - `Menu Distance`: 2.0m (distance from player)
   - `Menu Height`: 0.0m (vertical offset)
   - `Menu Size`: (3, 2, 0.1) (width, height, depth)
2. **Button Configuration**:
   - `Button Size`: (0.8, 0.2) (width, height)
   - Configure colors for normal, hover, and disabled states
3. **References**: Will auto-detect player camera if not assigned

### Step 4: VR Scene Transition Manager Configuration
In the VRSceneTransitionManager component:
1. **Loading Screen UI**: Will be created at runtime if not assigned
2. **VR Loading Feedback**: Configure rotation speed for loading indicator
3. **Error Handling**: Set error display duration

### Step 5: Addressables Setup (Optional)
If using Addressables for scene management:
1. Open **Window > Asset Management > Addressables > Groups**
2. Create addressable entries for each scene:
   - `MainMenu.unity` → Address: "MainMenu"
   - `CalibrationAlignment.unity` → Address: "CalibrationScene"
   - `DeveloperEnviroment.unity` → Address: "DiagnosticScene"
   - `RoomEnviroment.unity` → Address: "ResultsScene"
3. Build Addressables: **Build > New Build > Default Build Script**

### Step 6: XR Integration
Ensure your VR setup includes:
1. **XR Rig**: Properly configured for Meta Quest Pro
2. **Eye Tracking**: OVREyeGaze components for calibration
3. **GazePointerController**: For VR menu interaction
4. **Layers**: UI layer configured for VR interaction

## Usage

### Menu Buttons
The VR main menu includes five buttons:

1. **Run Calibration**
   - Loads the calibration scene
   - Initiates eye calibration process
   - Available regardless of calibration status

2. **Start Diagnostic**
   - Only enabled when calibration is complete
   - Loads the diagnostic scene
   - Shows warning if calibration required

3. **Review Results**
   - Loads the results viewing scene
   - Available anytime

4. **Settings**
   - Opens settings panel (future implementation)
   - Changes state to Settings

5. **Quit**
   - Exits the application
   - Works in editor and build

### Calibration Status
The menu displays real-time calibration status:
- **✓ Complete**: Green indicator, diagnostic enabled
- **⚠ Required**: Red/yellow indicator, diagnostic disabled

### Scene Transitions
All scene transitions include:
- Loading screen with progress bar
- Status messages
- Error handling with fallback
- Proper cleanup of previous scenes

## Integration with Existing Systems

### CalibrationManager Integration
The GameManager automatically:
- Subscribes to calibration completion events
- Updates UI when calibration status changes
- Prevents diagnostic access without calibration

### InteractableMenuItem Integration
The VR menu uses your existing:
- Gaze-based interaction system
- Dwell progress indicators
- Audio feedback support

### Backward Compatibility
Legacy methods are preserved:
- `StartGame()` → `OnStartDiagnosticClicked()`
- `ExitGame()` → `OnQuitClicked()`
- `HideMenu()` / `ShowMenu()` still work

## Customization

### Visual Appearance
Modify `VRMenuPrefabManager`:
- Button colors and sizes
- Menu positioning and orientation
- Material assignments
- Text styling

### Interaction
Customize `VRMainMenu`:
- Button event handlers
- Audio feedback
- Status text formatting
- Error handling

### Scene Flow
Adjust `GameManager`:
- Add new game states
- Modify scene routing logic
- Implement additional validation

## Troubleshooting

### Common Issues

**Menu not appearing:**
- Check VRMenuPrefabManager is enabled
- Verify player camera reference
- Ensure menu distance is appropriate

**Buttons not responding:**
- Verify GazePointerController is active
- Check InteractableMenuItem components
- Confirm UI layer settings

**Scene transitions failing:**
- Check Addressable references
- Verify scene names in fallback
- Review console for error messages

**Calibration status not updating:**
- Ensure EyeCalibrationManager.Instance exists
- Check event subscription in GameManager
- Verify calibration completion event firing

### Debug Information
Enable debug logging by checking:
- GameManager state changes
- Scene transition status
- Calibration event firing
- Menu button interactions

## Performance Considerations

- Menu updates only when necessary
- Efficient world-space canvas rendering
- Proper event cleanup on scene transitions
- Addressables memory management

## Future Enhancements

Planned improvements:
- Settings panel implementation
- Advanced loading animations
- Haptic feedback integration
- Voice command support
- Accessibility features

---

**Note**: This system is designed specifically for Meta Quest Pro with Unity 2022.3 LTS and requires the Meta OpenXR packages for full functionality.