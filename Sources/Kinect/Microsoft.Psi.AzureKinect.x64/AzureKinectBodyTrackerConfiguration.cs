// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Represents the Azure Kinect body tracker configuration.
    /// </summary>
    public class AzureKinectBodyTrackerConfiguration
    {
        /// <summary>
        /// Gets or sets the temporal smoothing to use across frames for the body tracker.
        /// </summary>
        /// <remarks>
        /// Set between 0 (no smoothing) and 1 (full smoothing). Less smoothing will increase
        /// the responsiveness of the detected skeletons but will cause more positional and
        /// orientational jitters.
        /// </remarks>
        public float TemporalSmoothing { get; set; } = 0f;

        /// <summary>
        /// Gets or sets a value indicating whether to use the lite network.
        /// </summary>
        /// <remarks>
        /// The lite network is faster but comes at a 5% decrease in performance.
        /// </remarks>
        public bool LiteNetwork { get; set; } = false;

        /// <summary>
        /// Gets or sets a the body tracker's processing mode.
        /// </summary>
        /// <remarks>
        /// Defaults to Cuda. Alternatives are CPU, TensorRT, and DirectML.
        /// </remarks>
        public TrackerProcessingMode ProcessingMode { get; set; } = TrackerProcessingMode.Cuda;

        /// <summary>
        /// Gets or sets the sensor orientation used by body tracking.
        /// </summary>
        public SensorOrientation SensorOrientation { get; set; } = SensorOrientation.Default;
    }
}
