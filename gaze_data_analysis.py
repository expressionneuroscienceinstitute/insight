import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler

# Constants (you might want to make these configurable)
IPD_METERS = 0.069 # Average interpupillary distance (65 mm)
EYE_OFFSET_METERS = IPD_METERS / 2 # Distance from center of head to center of eye
DIOPTER_TO_DEGREE_CONVERSION = 0.9 # 1 diopter is roughly 1 degree of rotation

def calculate_distance(point):
    """Calculates the distance from the origin to a 3D point."""
    return np.linalg.norm(point)

def calculate_eye_diopters(eye_dir, is_left_eye):
    """Calculates the diopter change for a single eye, independent of the other eye."""
    eye_dir_normalized = eye_dir / np.linalg.norm(eye_dir)
    horizontal_angle = np.degrees(np.arctan2(eye_dir_normalized[0], eye_dir_normalized[2]))

    # Adjust horizontal angle based on eye (left/right)
    if is_left_eye:
        horizontal_angle = -horizontal_angle  # Invert for left eye

    diopters = horizontal_angle / DIOPTER_TO_DEGREE_CONVERSION
    return diopters

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


def analyze_eye_data(csv_file):
    """Analyzes eye data, calculating diopters and vergence."""
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
        left_diopters = calculate_eye_diopters(left_eye_dir, is_left_eye=True)
        right_diopters = calculate_eye_diopters(right_eye_dir, is_left_eye=False)

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

    # Outlier Filtering (std deviation)
    mean_diopter_left = df['LeftDiopters'].mean()
    std_diopter_left = df['LeftDiopters'].std()
    mean_vertical_left = df['LeftVertical'].mean()
    std_vertical_left = df['LeftVertical'].std()
    mean_diopter_right = df['RightDiopters'].mean()
    std_diopter_right = df['RightDiopters'].std()
    mean_vertical_right = df['RightVertical'].mean()
    std_vertical_right = df['RightVertical'].std()

    threshold_diopter_left = 3 * std_diopter_left
    threshold_vertical_left = 3 * std_vertical_left
    threshold_diopter_right = 3 * std_diopter_right
    threshold_vertical_right = 3 * std_vertical_right

    df_filtered = df[(np.abs(df['LeftDiopters'] - mean_diopter_left) < threshold_diopter_left) &
                     (np.abs(df['LeftVertical'] - mean_vertical_left) < threshold_vertical_left) &
                     (np.abs(df['RightDiopters'] - mean_diopter_right) < threshold_diopter_right) &
                     (np.abs(df['RightVertical'] - mean_vertical_right) < threshold_vertical_right)].copy()

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

    # Plots
    # All plots should share an initial scale in order to show 
    fig, axes = plt.subplots(3, 1, figsize=(14, 12), sharex=True)
    ax1 = axes[0]
    ax2 = axes[1]
    ax3 = axes[2]

    ax1.plot(df_filtered['Timestamp'], df_filtered['LeftDiopters'], label='Left Diopters')
    ax1.plot(df_filtered['Timestamp'], df_filtered['RightDiopters'], label='Right Diopters')
    ax1.set_ylabel('Diopters')
    ax1.set_title('Eye Movement in Diopters Over Time')
    ax1.legend()
    ax1.grid(True)

    ax2.plot(df_filtered['Timestamp'], df_filtered['LeftVertical'], label='Left Vertical')
    ax2.plot(df_filtered['Timestamp'], df_filtered['RightVertical'], label='Right Vertical')
    ax2.plot(df_filtered['Timestamp'], df_filtered['TargetVertical'], label='Target Vertical', linestyle='--')
    ax2.set_xlabel('Timestamp')
    ax2.set_ylabel('Vertical Angle (Degrees)')
    ax2.set_title('Vertical Eye Movement Over Time')
    ax2.legend()
    ax2.grid(True)

    ymin = min(df_filtered['LeftVertical'].min(), df_filtered['RightVertical'].min(), df_filtered['TargetVertical'].min())
    ymax = max(df_filtered['LeftVertical'].max(), df_filtered['RightVertical'].max(), df_filtered['TargetVertical'].max())
    ax2.set_ylim(ymin, ymax)
    
    ax3.plot(df_filtered['Timestamp'], df_filtered['TargetHorizontal'], label='Target Horizontal')
    ax3.plot(df_filtered['Timestamp'], df_filtered['TargetVertical'], label='Target Vertical')
    ax3.set_xlabel('Timestamp')
    ax3.set_ylabel('Target Angle (Degrees)')
    ax3.set_title('Target Movement Over Time')
    ax3.legend()
    ax3.grid(True)
    ax3.set_ylim(ymin, ymax)

    plt.tight_layout()
    plt.show()

    # Scatter plots
    plt.figure(figsize=(18, 6))
    plt.subplot(1, 3, 1)
    sns.scatterplot(x='LeftDiopters', y='LeftVertical', hue='Cluster', data=df_filtered)
    plt.xlabel('Diopters')
    plt.ylabel('Vertical Angle (Degrees)')
    plt.title('Left Eye Movement')
    plt.grid(True)

    plt.subplot(1, 3, 2)
    sns.scatterplot(x='RightDiopters', y='RightVertical', hue='Cluster', data=df_filtered)
    plt.xlabel('Diopters')
    plt.ylabel('Vertical Angle (Degrees)')
    plt.title('Right Eye Movement')
    plt.grid(True)
    
    plt.subplot(1, 3, 3)
    sns.scatterplot(x='TargetHorizontal', y='TargetVertical', data=df_filtered)
    plt.xlabel('Horizontal Angle (Degrees)')
    plt.ylabel('Vertical Angle (Degrees)')
    plt.title('Target Movement')
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
    base_alignment_diopter = df_filtered[['LeftDiopters', 'RightDiopters']].mean().mean()
    base_alignment_vertical = df_filtered[['LeftVertical', 'RightVertical']].mean().mean()

    print(f"\nRecommended Base Alignment:")
    print(f"  Diopters: {base_alignment_diopter:.2f}")
    print(f"  Vertical: {base_alignment_vertical:.2f}")

    #Stable Sampling
    stable_sample = df_filtered[(np.abs(df_filtered['LeftDiopters']-base_alignment_diopter) < 0.5) & (np.abs(df_filtered['RightDiopters']-base_alignment_diopter) < 0.5)]
    if (len(stable_sample) > 0):
        start_time = stable_sample['Timestamp'].iloc[0]
        end_time = stable_sample['Timestamp'].iloc[-1]
        print(f"\nStable Sample Time Range (for Fine Alignment):")
        print(f"  Start Time: {start_time:.2f}")
        print(f"  End Time: {end_time:.2f}")
    else:
        print("\nNo stable sample found within the criteria.")

if __name__ == "__main__":
    csv = input("File\n")
    file_path = f"C:\\Users\\dylan\\AppData\\LocalLow\\DefaultCompany\\binoclear_insight\\{csv}"
    analyze_eye_data(file_path)