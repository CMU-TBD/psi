// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisualPipeline.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Psi Componet that given a a merged bodies, track the user overtime.
    /// </summary>
    public class BodyTracker : IConsumerProducer<List<List<AzureKinectBody>>, List<AzureKinectBody>>
    {
        private uint peopleIndex = 0;
        private TimeSpan removeThreshold = TimeSpan.FromSeconds(1);
        private Pipeline pipeline;
        private Dictionary<uint, (AzureKinectBody, DateTime)> currTrackingPeople = new Dictionary<uint, (AzureKinectBody, DateTime)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyTracker"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        public BodyTracker(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.In = pipeline.CreateReceiver<List<List<AzureKinectBody>>>(this, this.BodiesCallback, nameof(this.In));
            this.Out = pipeline.CreateEmitter<List<AzureKinectBody>>(this, nameof(this.Out));
        }

        /// <inheritdoc/>
        public Receiver<List<List<AzureKinectBody>>> In { get; private set; }

        /// <inheritdoc/>
        public Emitter<List<AzureKinectBody>> Out { get; private set; }

        private void BodiesCallback(List<List<AzureKinectBody>> msg, Envelope env)
        {
            var currentBodies = new List<AzureKinectBody>();

            // We don't know when this is called, so we going to remove stall data based on a previous threshold
            this.currTrackingPeople = this.currTrackingPeople.Where(pair => (this.pipeline.GetCurrentTime() - pair.Value.Item2) < this.removeThreshold)
                                                             .ToDictionary(pair => pair.Key, pair => pair.Value);

            // TODO: Better tracking
            // Currently, it does a first come first server algorithm.
            var matchedIndex = new List<uint>();

            foreach (var candidate in msg)
            {
                var found = false;

                // see whether ther is a good match
                foreach (var key in this.currTrackingPeople.Keys)
                {
                    // ignore if used
                    if (matchedIndex.IndexOf(key) != -1)
                    {
                        continue;
                    }

                    // try to see if they are a good match
                    if (this.MatchingBodies(this.currTrackingPeople[key].Item1, candidate))
                    {
                        // MATCH
                        matchedIndex.Add(key);

                        // update the bodies with the first body
                        this.currTrackingPeople[key] = (candidate[0], env.Time);
                        var body = candidate[0];
                        body.TrackingId = key;
                        currentBodies.Add(body);
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                // We cannot find a good match. It might be someone new
                candidate[0].TrackingId = this.peopleIndex;
                this.currTrackingPeople[this.peopleIndex++] = (candidate[0], env.Time);
                currentBodies.Add(candidate[0]);
            }

            this.Out.Post(currentBodies, env.OriginatingTime);
        }

        private bool MatchingBodies(AzureKinectBody tracked, List<AzureKinectBody> candidate)
        {
            foreach (var body in candidate)
            {
                if (Utils.CompareAzureBodies(tracked, body))
                {
                    return true;
                }
            }

            return false;
        }
    }
}