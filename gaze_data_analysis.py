import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler
import configparser
import argparse
import os

# --- Configuration ---
def load_config(config_file):
    """Loads configuration from a .ini file."""
    config = configparser.ConfigParser()
    config.read(config_file)
    return config['DEFAULT']

# --- Constants (Default Values) ---
DEFAULT_CONFIG = {
    'IPD_METERS': 0.069,
    'DIOPTER_TO_DEGREE_CONVERSION': 1.0,
    'OUTLIER_THRESHOLD_MULTIPLIER': 3.0,
    'STABLE_SAMPLE_THRESHOLD': 1.0,
    'VELOCITY_WINDOW_SIZE': 5,
    'ACCELERATION_WINDOW_SIZE': 5,
    'FIXATION_STABILITY_THRESHOLD': 0.5,
    'FIXATION_MIN_DURATION': 0.1
}

# --- Helper Functions ---
def calculate_distance(point):
    """Calculates the distance from the origin to a 3D point."""
    return np.linalg.norm(point)

def calculate_eye_diopters(eye_dir, target_pos, is_left_eye, config):
    """Calculates the diopter change for a single eye, independent of the other eye."""
    
    # Calculate the optimal horizontal angle for the eye to be looking at the target
    target_horizontal_angle = np.degrees(np.arctan2(target_pos[0], target_pos[2]))
    
    # Calculate the current horizontal angle of the eye
    eye_dir_normalized = eye_dir / np.linalg.norm(eye_dir)
    current_horizontal_angle = np.degrees(np.arctan2(eye_dir_normalized[0], eye_dir_normalized[2]))

    # Invert the left eye
    if is_left_eye:
        current_horizontal_angle = -current_horizontal_angle
    
    # Calculate the difference between the current and optimal horizontal angles
    angle_difference = current_horizontal_angle - target_horizontal_angle

    # Calculate the diopter change based on the angle difference
    diopter_change = angle_difference / float(config['DIOPTER_TO_DEGREE_CONVERSION'])
    
    return diopter_change

def calculate_vertical_angle(eye_dir):
    """Calculates the vertical angle in degrees."""
    eye_dir_normalized = eye_dir / np.linalg.norm(eye_dir)
    vertical_angle = np.degrees(np.arcsin(np.clip(eye_dir_normalized[1], -1.0, 1.0)))
    return vertical_angle

def calculate_target_angles(target_pos):
    """Calculates horizontal and vertical angles of the target in degrees."""
    target_pos_normalized = target_pos / np.linalg.norm(target_pos)

    horizontal_angle = np.degrees(np.arctan2(target_pos_normalized[0], target_pos_normalized[2]))
    vertical_angle = np.degrees(np.arcsin(np.clip(target_pos_normalized[1], -1.0, 1.0)))

    return horizontal_angle, vertical_angle

def calculate_velocity(data, window_size, config):
    """Calculates the velocity of eye movement."""
    velocities = np.zeros_like(data)
    for i in range(window_size, len(data)):
        velocities[i] = (data[i] - data[i - window_size]) / window_size
    return velocities

def calculate_acceleration(data, window_size, config):
    """Calculates the acceleration of eye movement."""
    accelerations = np.zeros_like(data)
    for i in range(window_size, len(data)):
        accelerations[i] = (data[i] - data[i - window_size]) / window_size
    return accelerations

def detect_fixations(diopter_data, timestamp_data, config):
    """Detects fixations based on stability and duration thresholds."""
    fixations = []
    in_fixation = False
    fixation_start = None
    stable_count = 0
    stability_threshold = float(config['FIXATION_STABILITY_THRESHOLD'])
    min_duration = float(config['FIXATION_MIN_DURATION'])

    for i in range(1, len(diopter_data)):
        if abs(diopter_data[i] - diopter_data[i-1]) < stability_threshold:
            if not in_fixation:
                in_fixation = True
                fixation_start = i - 1
            stable_count += 1
        else:
            if in_fixation:
                in_fixation = False
                fixation_end = i - 1
                duration = timestamp_data[fixation_end] - timestamp_data[fixation_start]
                if duration >= min_duration:
                    location = np.mean(diopter_data[fixation_start:fixation_end+1])
                    fixations.append({
                        'start_index': fixation_start,
                        'end_index': fixation_end,
                        'duration': duration,
                        'location': location
                    })
            stable_count = 0

    return fixations

# --- Main Analysis Function ---
def analyze_eye_data(csv_file, config):
    """Analyzes eye data, calculating diopters, velocity, and acceleration."""
    try:
        df = pd.read_csv(csv_file)
    except FileNotFoundError:
        print(f"Error: File '{csv_file}' not found.")
        return

    left_diopter_list = []
    right_diopter_list = []
    left_vertical_list = []
    right_vertical_list = []
    target_horizontal_list = []
    target_vertical_list = []

    for index, row in df.iterrows():
        left_eye_dir = np.array([row['LeftEyeDirX'], row['LeftEyeDirY'], row['LeftEyeDirZ']])
        right_eye_dir = np.array([row['RightEyeDirX'], row['RightEyeDirY'], row['RightEyeDirZ']])
        target_pos = np.array([row['TargetCenterX'], row['TargetCenterY'], row['TargetCenterZ']])

        # Calculate diopters for each eye
        left_diopters = calculate_eye_diopters(left_eye_dir, target_pos, is_left_eye=True, config=config)
        right_diopters = calculate_eye_diopters(right_eye_dir, target_pos, is_left_eye=False, config=config)

        # Calculate vertical angles
        left_vertical = calculate_vertical_angle(left_eye_dir)
        right_vertical = calculate_vertical_angle(right_eye_dir)
        target_horizontal, target_vertical = calculate_target_angles(target_pos)

        left_diopter_list.append(left_diopters)
        right_diopter_list.append(right_diopters)
        left_vertical_list.append(left_vertical)
        right_vertical_list.append(right_vertical)
        target_horizontal_list.append(target_horizontal)
        target_vertical_list.append(target_vertical)

    df['LeftDiopters'] = left_diopter_list
    df['RightDiopters'] = right_diopter_list
    df['LeftVertical'] = left_vertical_list
    df['RightVertical'] = right_vertical_list
    df['TargetHorizontal'] = target_horizontal_list
    df['TargetVertical'] = target_vertical_list

    # Calculate Velocity and Acceleration
    df['LeftVelocity'] = calculate_velocity(df['LeftDiopters'].values, int(config['VELOCITY_WINDOW_SIZE']), config)
    df['RightVelocity'] = calculate_velocity(df['RightDiopters'].values, int(config['VELOCITY_WINDOW_SIZE']), config)
    df['LeftAcceleration'] = calculate_acceleration(df['LeftVelocity'].values, int(config['ACCELERATION_WINDOW_SIZE']), config)
    df['RightAcceleration'] = calculate_acceleration(df['RightVelocity'].values, int(config['ACCELERATION_WINDOW_SIZE']), config)

    # Detect Fixations
    df['LeftFixations'] = [detect_fixations(df['LeftDiopters'].values, df['Timestamp'].values, config)] * len(df)
    df['RightFixations'] = [detect_fixations(df['RightDiopters'].values, df['Timestamp'].values, config)] * len(df)

    # Print Fixation Information
    print(f"\nNumber of Left Fixations: {len(df['LeftFixations'].iloc[0])}")
    for fixation in df['LeftFixations'].iloc[0]:
        print(f"  Left Fixation: Duration: {fixation['duration']:.4f}s, Location: {fixation['location']:.2f} diopters")
    print(f"Number of Right Fixations: {len(df['RightFixations'].iloc[0])}")
    for fixation in df['RightFixations'].iloc[0]:
        print(f"  Right Fixation: Duration: {fixation['duration']:.4f}s, Location: {fixation['location']:.2f} diopters")

    # Outlier Filtering (std deviation)
    outlier_threshold_multiplier = float(config['OUTLIER_THRESHOLD_MULTIPLIER'])
    window_size = 10 # Adjust as needed

    df_filtered = df.copy()
    for i in range(window_size, len(df)):
        window = df.iloc[i-window_size:i]

        mean_diopter_left = window['LeftDiopters'].mean()
        std_diopter_left = window['LeftDiopters'].std()
        mean_vertical_left = window['LeftVertical'].mean()
        std_vertical_left = window['LeftVertical'].std()
        mean_diopter_right = window['RightDiopters'].mean()
        std_diopter_right = window['RightDiopters'].std()
        mean_vertical_right = window['RightVertical'].mean()
        std_vertical_right = window['RightVertical'].std()

        threshold_diopter_left = outlier_threshold_multiplier * std_diopter_left
        threshold_vertical_left = outlier_threshold_multiplier * std_vertical_left
        threshold_diopter_right = outlier_threshold_multiplier * std_diopter_right
        threshold_vertical_right = outlier_threshold_multiplier * std_vertical_right

        if (np.abs(df['LeftDiopters'].iloc[i] - mean_diopter_left) > threshold_diopter_left) or \
           (np.abs(df['LeftVertical'].iloc[i] - mean_vertical_left) > threshold_vertical_left) or \
           (np.abs(df['RightDiopters'].iloc[i] - mean_diopter_right) > threshold_diopter_right) or \
           (np.abs(df['RightVertical'].iloc[i] - mean_vertical_right) > threshold_vertical_right):
            df_filtered = df_filtered.drop(i)

    # Clustering (K-Means)
    features = df_filtered[['LeftDiopters', 'LeftVertical', 'RightDiopters', 'RightVertical']]
    scaler = StandardScaler()
    scaled_features = scaler.fit_transform(features)

    kmeans = KMeans(n_clusters=3, random_state=42)
    df_filtered.loc[:, 'Cluster'] = kmeans.fit_predict(scaled_features)

    # Angular Statistics
    print("Eye Diopter Statistics:")
    print(df_filtered[['LeftDiopters', 'RightDiopters']].describe())
    print("\nDataFrame Description:")
    print(df_filtered.describe())

    # --- Plots ---
    # Plot 1: Eye Movement in Diopters and Vertical Angles
    fig1, axes1 = plt.subplots(2, 1, figsize=(14, 15), sharex=True, sharey=True)
    ax1 = axes1[0]
    ax2 = axes1[1]

    ax1.plot(df_filtered['Timestamp'], df_filtered['LeftDiopters'], label='Left Diopters')
    ax1.plot(df_filtered['Timestamp'], df_filtered['RightDiopters'], label='Right Diopters')
    ax1.set_ylabel('Diopters')
    ax1.set_title('Eye Movement in Diopters Over Time')
    ax1.legend()
    ax1.grid(True)

    ax2.plot(df_filtered['Timestamp'], df_filtered['LeftVertical'], label='Left Vertical')
    ax2.plot(df_filtered['Timestamp'], df_filtered['RightVertical'], label='Right Vertical')
    ax2.set_xlabel('Timestamp')
    ax2.set_ylabel('Vertical Angle (Degrees)')
    ax2.set_title('Vertical Eye Movement Over Time')
    ax2.legend()
    ax2.grid(True)

    ymin = min(df_filtered['LeftVertical'].min(), df_filtered['RightVertical'].min())
    ymax = max(df_filtered['LeftVertical'].max(), df_filtered['RightVertical'].max())
    ax2.set_ylim(ymin, ymax)

    plt.tight_layout()
    plt.show()

    # Plot 2: Eye Acceleration
    fig2, axes4 = plt.subplots(2, 1, figsize=(14, 5))
    ax3 = axes4[0]
    ax4 = axes4[1]

    ax3.plot(df_filtered['Timestamp'], df_filtered['LeftAcceleration'], label='Left Acceleration')
    ax3.plot(df_filtered['Timestamp'], df_filtered['RightAcceleration'], label='Right Acceleration')
    ax3.set_xlabel('Timestamp')
    ax3.set_ylabel('Acceleration (Diopters/s^2)')
    ax3.set_title('Eye Acceleration Over Time')
    ax3.legend()
    ax3.grid(True)

    ax4.plot(df_filtered['Timestamp'], df_filtered['LeftVelocity'], label='Left Velocity')
    ax4.plot(df_filtered['Timestamp'], df_filtered['RightVelocity'], label='Right Velocity')
    ax4.set_xlabel('Timestamp')
    ax4.set_ylabel('Velocity (Diopters/s)')
    ax4.set_title('Eye Velocity Over Time')
    ax4.legend()
    ax4.grid(True)

    plt.tight_layout()
    plt.show()

    # Scatter plots
    plt.figure(figsize=(18, 6))
    plt.subplot(1, 2, 1)
    sns.scatterplot(x='LeftDiopters', y='LeftVertical', hue='Cluster', data=df_filtered)
    plt.xlabel('Diopters')
    plt.ylabel('Vertical Angle (Degrees)')
    plt.title('Left Eye Movement')
    plt.grid(True)

    plt.subplot(1, 2, 2)
    sns.scatterplot(x='RightDiopters', y='RightVertical', hue='Cluster', data=df_filtered)
    plt.xlabel('Diopters')
    plt.ylabel('Vertical Angle (Degrees)')
    plt.title('Right Eye Movement')
    plt.grid(True)

    plt.tight_layout()
    plt.show()

    # Correlation Matrix
    correlation_matrix = df_filtered[['LeftDiopters', 'LeftVertical', 'RightDiopters', 'RightVertical', 'TargetHorizontal', 'TargetVertical']].corr()
    plt.figure(figsize=(8, 6))
    sns.heatmap(correlation_matrix, annot=True, cmap='coolwarm', vmin=-1, vmax=1)
    plt.title('Correlation Matrix of Eye and Target Movements')
    plt.show()

    # Histograms
    plt.figure(figsize=(18, 8))
    plt.subplot(2, 3, 1)
    sns.histplot(df_filtered['LeftDiopters'], kde=True)
    plt.title('Left Diopter Distribution')
    plt.xlabel('Diopters')

    plt.subplot(2, 3, 2)
    sns.histplot(df_filtered['RightDiopters'], kde=True)
    plt.title('Right Diopter Distribution')
    plt.xlabel('Diopters')

    plt.subplot(2, 3, 3)
    sns.histplot(df_filtered['TargetHorizontal'], kde=True)
    plt.title('Target Horizontal Distribution')
    plt.xlabel('Horizontal Angle (Degrees)')

    plt.subplot(2, 3, 4)
    sns.histplot(df_filtered['LeftVertical'], kde=True)
    plt.title('Left Vertical Distribution')
    plt.xlabel('Vertical Angle (Degrees)')

    plt.subplot(2, 3, 5)
    sns.histplot(df_filtered['RightVertical'], kde=True)
    plt.title('Right Vertical Distribution')
    plt.xlabel('Vertical Angle (Degrees)')
    
    plt.subplot(2, 3, 6)
    sns.histplot(df_filtered['TargetVertical'], kde=True)
    plt.title('Target Vertical Distribution')
    plt.xlabel('Vertical Angle (Degrees)')

    plt.tight_layout()
    plt.show()

    # Base Alignment
    left_base_alignment_diopter = df_filtered[['LeftDiopters']].mean().mean()
    left_base_alignment_vertical = df_filtered[['LeftVertical']].mean().mean()

    print(f"\nRecommended Base Alignment (Left Eye):")
    print(f"  Diopters: {left_base_alignment_diopter:.2f}")
    print(f"  Vertical: {left_base_alignment_vertical:.2f}")

    right_base_alignment_diopter = df_filtered[['RightDiopters']].mean().mean()
    right_base_alignment_vertical = df_filtered[['RightVertical']].mean().mean()

    print(f"\nRecommended Base Alignment (Right Eye):")
    print(f"  Diopters: {right_base_alignment_diopter:.2f}")
    print(f"  Vertical: {right_base_alignment_vertical:.2f}")

    #Stable Sampling
    stable_sample_threshold = float(config['STABLE_SAMPLE_THRESHOLD'])
    stable_sample = df_filtered[(np.abs(df_filtered['LeftDiopters']-left_base_alignment_diopter) < stable_sample_threshold) & (np.abs(df_filtered['RightDiopters']-left_base_alignment_diopter) < stable_sample_threshold)]
    if (len(stable_sample) > 0):
        start_time = stable_sample['Timestamp'].iloc[0]
        end_time = stable_sample['Timestamp'].iloc[-1]
        print(f"\nStable Sample Time Range (for Fine Alignment):")
        print(f"  Start Time: {start_time:.2f}")
        print(f"  End Time: {end_time:.2f}")
    else:
        print("\nNo stable sample found within the criteria.")

# --- Main Execution ---
if __name__ == "__main__":
    # Parse command-line arguments
    parser = argparse.ArgumentParser(description="Analyze eye-tracking data from the Insight Diagnostic Program.")
    parser.add_argument("csv_file", help="Path to the CSV file containing eye-tracking data.")
    parser.add_argument("-c", "--config", help="Path to the configuration file.", default="config.ini")
    args = parser.parse_args()

    # Load configuration
    if os.path.exists(args.config):
        config = load_config(args.config)
    else:
        print(f"Warning: Configuration file '{args.config}' not found. Using default values.")
        config = DEFAULT_CONFIG

    # Analyze data
    analyze_eye_data(args.csv_file, config)
