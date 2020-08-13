// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisualPipeline
{
    using System;
    using System.Collections.Generic;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using TBD.Psi.VisualPipeline.Components;

    /// <summary>
    /// Data Collection Pipeline
    /// </summary>
    public class ReplayPipeline
    {

        /// <summary>
        /// Entry point to the pipeline.
        /// </summary>
        /// <param name="storeName">Name of Store.</param>
        /// <param name="rootPath">Path to Store.</param>
        public static void Run(string storeName, string rootPath)
        {
            using (var p = Pipeline.Create(true))
            {
                // output store
                var input = Store.Open(p, storeName, rootPath);
                var store = Store.Create(p, "replayPipeline", @"C:\Data\Stores");

                var body1 = input.OpenStream<List<AzureKinectBody>>("azure1.bodies");
                var body2 = input.OpenStream<List<AzureKinectBody>>("azure2.bodies");

                // copy the frames to the other store
                input.OpenStream<List<CoordinateSystem>>("frames").Write("frames", store);

                // input.OpenStream<Shared<EncodedImage>>("azure1.color").Write("azure1.color", store);
                // input.OpenStream<Shared<EncodedImage>>("azure2.color").Write("azure2.color", store);

                // Merge the who streams
                var merger = new BodyMerger(p, body1);
                merger.AddAzureKinectBodyStream(body2);

                // Tracking of Bodies across time.
                var tracker = new BodyTracker(p);
                merger.PipeTo(tracker);
                tracker.Out.Write("TrackedBodies", store);

                // Write diagnostics + start
                p.Diagnostics.Write("diagnostics", store);
                p.Run(new ReplayDescriptor(TimeInterval.Infinite, replaySpeed: 0.5f));

                Console.WriteLine("Done!");

                // Console.ReadLine();
            }
        }
    }
}
