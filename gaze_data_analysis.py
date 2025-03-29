import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler

def calculate_eye_angles(eye_dir):
    """Calculates horizontal and vertical angles in degrees."""
    eye_dir_normalized = eye_dir / np.linalg.norm(eye_dir)

    horizontal_angle = np.degrees(np.arctan2(eye_dir_normalized[0], eye_dir_normalized[2]))
    vertical_angle = np.degrees(np.arcsin(np.clip(eye_dir_normalized[1], -1.0, 1.0)))

    return horizontal_angle, vertical_angle

def calculate_target_angles(target_pos):
    """Calculates horizontal and vertical angles of the target in degrees."""
    target_pos_normalized = target_pos / np.linalg.norm(target_pos)

    horizontal_angle = np.degrees(np.arctan2(target_pos_normalized[0], target_pos_normalized[2]))
    vertical_angle = np.degrees(np.arcsin(np.clip(target_pos_normalized[1], -1.0, 1.0)))

    return horizontal_angle, vertical_angle

def analyze_eye_data(csv_file):
    """Filters outliers, performs clustering using standard deviation, and provides detailed analysis of a given BVD battery."""
    try:
        df = pd.read_csv(csv_file)
    except FileNotFoundError:
        print(f"Error: File '{csv_file}' not found.")
        return

    left_horizontal_list = []
    left_vertical_list = []
    right_horizontal_list = []
    right_vertical_list = []
    target_horizontal_list = []
    target_vertical_list = []

    for index, row in df.iterrows():
        left_eye_dir = np.array([row['LeftEyeDirX'], row['LeftEyeDirY'], row['LeftEyeDirZ']])
        right_eye_dir = np.array([row['RightEyeDirX'], row['RightEyeDirY'], row['RightEyeDirZ']])
        target_pos = np.array([row['TargetCenterX'], row['TargetCenterY'], row['TargetCenterZ']])

        left_horizontal, left_vertical = calculate_eye_angles(left_eye_dir)
        right_horizontal, right_vertical = calculate_eye_angles(right_eye_dir)
        target_horizontal, target_vertical = calculate_target_angles(target_pos)

        left_horizontal_list.append(left_horizontal)
        left_vertical_list.append(left_vertical)
        right_horizontal_list.append(right_horizontal)
        right_vertical_list.append(right_vertical)
        target_horizontal_list.append(target_horizontal)
        target_vertical_list.append(target_vertical)

    df['LeftHorizontal'] = left_horizontal_list
    df['LeftVertical'] = left_vertical_list
    df['RightHorizontal'] = right_horizontal_list
    df['RightVertical'] = right_vertical_list
    df['TargetHorizontal'] = target_horizontal_list
    df['TargetVertical'] = target_vertical_list

    # Outlier Filtering (std deviation)
    mean_horizontal_left = df['LeftHorizontal'].mean()
    std_horizontal_left = df['LeftHorizontal'].std()
    mean_vertical_left = df['LeftVertical'].mean()
    std_vertical_left = df['LeftVertical'].std()
    mean_horizontal_right = df['RightHorizontal'].mean()
    std_horizontal_right = df['RightHorizontal'].std()
    mean_vertical_right = df['RightVertical'].mean()
    std_vertical_right = df['RightVertical'].std()

    threshold_horizontal_left = 3 * std_horizontal_left
    threshold_vertical_left = 3 * std_vertical_left
    threshold_horizontal_right = 3 * std_horizontal_right
    threshold_vertical_right = 3 * std_vertical_right

    df_filtered = df[(np.abs(df['LeftHorizontal'] - mean_horizontal_left) < threshold_horizontal_left) &
                     (np.abs(df['LeftVertical'] - mean_vertical_left) < threshold_vertical_left) &
                     (np.abs(df['RightHorizontal'] - mean_horizontal_right) < threshold_horizontal_right) &
                     (np.abs(df['RightVertical'] - mean_vertical_right) < threshold_vertical_right)].copy()

    # Clustering (K-Means)
    features = df_filtered[['LeftHorizontal', 'LeftVertical', 'RightHorizontal', 'RightVertical']]
    scaler = StandardScaler()
    scaled_features = scaler.fit_transform(features)

    kmeans = KMeans(n_clusters=3, random_state=42)
    df_filtered.loc[:, 'Cluster'] = kmeans.fit_predict(scaled_features)

    # Angular Statistics
    print("Eye Angle Statistics:")
    print(df_filtered[['LeftHorizontal', 'LeftVertical', 'RightHorizontal', 'RightVertical']].describe())
    print("\nDataFrame Description:")
    print(df_filtered.describe())

    # Plots
    # All plots should share an initial scale in order to show 
    fig, axes = plt.subplots(3, 1, figsize=(14, 12), sharex=True)
    ax1 = axes[0]
    ax2 = axes[1]
    ax3 = axes[2]

    ax1.plot(df_filtered['Timestamp'], df_filtered['LeftHorizontal'], label='Left Horizontal')
    ax1.plot(df_filtered['Timestamp'], df_filtered['RightHorizontal'], label='Right Horizontal')
    ax1.plot(df_filtered['Timestamp'], df_filtered['TargetHorizontal'], label='Target Horizontal', linestyle='--')
    ax1.set_ylabel('Horizontal Angle (Degrees)')
    ax1.set_title('Horizontal Eye Movement Over Time')
    ax1.legend()
    ax1.grid(True)
    ax1.set_ylim(-5, 5)

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
    sns.scatterplot(x='LeftHorizontal', y='LeftVertical', hue='Cluster', data=df_filtered)
    plt.xlabel('Horizontal Angle (Degrees)')
    plt.ylabel('Vertical Angle (Degrees)')
    plt.title('Left Eye Movement')
    plt.grid(True)
    plt.xlim(-5, 5)
    plt.ylim(ymin, ymax)

    plt.subplot(1, 3, 2)
    sns.scatterplot(x='RightHorizontal', y='RightVertical', hue='Cluster', data=df_filtered)
    plt.xlabel('Horizontal Angle (Degrees)')
    plt.ylabel('Vertical Angle (Degrees)')
    plt.title('Right Eye Movement')
    plt.grid(True)
    plt.xlim(-5, 5)
    plt.ylim(ymin, ymax)
    
    plt.subplot(1, 3, 3)
    sns.scatterplot(x='TargetHorizontal', y='TargetVertical', data=df_filtered)
    plt.xlabel('Horizontal Angle (Degrees)')
    plt.ylabel('Vertical Angle (Degrees)')
    plt.title('Target Movement')
    plt.grid(True)
    plt.xlim(-5, 5)
    plt.ylim(ymin, ymax)

    plt.tight_layout()
    plt.show()

   # Binocular Fusion
    fusion_tolerance = 2.0 # Adjust as needed for testing battery
    df_filtered.loc[:, 'BinocularFusion'] = np.abs(df_filtered['LeftHorizontal'] - df_filtered['RightHorizontal']) < fusion_tolerance

    fusion_indices = df_filtered[df_filtered['BinocularFusion']].index
    fusion_times = df_filtered[df_filtered['BinocularFusion']]['Timestamp']

    plt.figure(figsize=(10, 6))
    plt.plot(df_filtered['Timestamp'], df_filtered['LeftHorizontal'], label='Left Horizontal')
    plt.plot(df_filtered['Timestamp'], df_filtered['RightHorizontal'], label='Right Horizontal')
    plt.scatter(fusion_times, df_filtered.loc[fusion_indices, 'LeftHorizontal'], color='green', label='Binocular Fusion', marker='o', s=15)
    plt.xlabel('Timestamp')
    plt.ylabel('Horizontal Angle (Degrees)')
    plt.title('Horizontal Eye Movement Over Time with Binocular Fusion')
    plt.legend()
    plt.grid(True)
    plt.show()

    if len(fusion_times) > 0:
        print("\nBinocular Fusion Detected:")
        print(f"  Number of Fusion Instances: {len(fusion_times)}")
        print(f"  Fusion Times (Timestamps): {fusion_times.tolist()}")
        print("Horizontal Angles During Fusion:")
        print(df_filtered.loc[fusion_indices, ['LeftHorizontal', 'RightHorizontal']])
    else:
        print("\nNo binocular fusion detected within the specified tolerance.")


    # Correlation Matrix
    correlation_matrix = df_filtered[['LeftHorizontal', 'LeftVertical', 'RightHorizontal', 'RightVertical', 'TargetHorizontal', 'TargetVertical']].corr()
    plt.figure(figsize=(8, 6))
    sns.heatmap(correlation_matrix, annot=True, cmap='coolwarm', vmin=-1, vmax=1)
    plt.title('Correlation Matrix of Eye and Target Movements')
    plt.show()

    # Histograms
    plt.figure(figsize=(18, 8))
    plt.subplot(2, 3, 1)
    sns.histplot(df_filtered['LeftHorizontal'], kde=True)
    plt.title('Left Horizontal Distribution')
    plt.xlabel('Horizontal Angle (Degrees)')
    plt.xlim(-5, 5)

    plt.subplot(2, 3, 2)
    sns.histplot(df_filtered['RightHorizontal'], kde=True)
    plt.title('Right Horizontal Distribution')
    plt.xlabel('Horizontal Angle (Degrees)')
    plt.xlim(-5, 5)

    plt.subplot(2, 3, 3)
    sns.histplot(df_filtered['TargetHorizontal'], kde=True)
    plt.title('Target Horizontal Distribution')
    plt.xlabel('Horizontal Angle (Degrees)')
    plt.xlim(-5, 5)

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
    base_alignment_horizontal = df_filtered[['LeftHorizontal', 'RightHorizontal']].mean().mean()
    base_alignment_vertical = df_filtered[['LeftVertical', 'RightVertical']].mean().mean()

    print(f"\nRecommended Base Alignment (Degrees):")
    print(f"  Horizontal: {base_alignment_horizontal:.2f}")
    print(f"  Vertical: {base_alignment_vertical:.2f}")

    #Stable Sampling
    stable_sample = df_filtered[(np.abs(df_filtered['LeftHorizontal']-base_alignment_horizontal) < 1) & (np.abs(df_filtered['RightHorizontal']-base_alignment_horizontal) < 1)]
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
