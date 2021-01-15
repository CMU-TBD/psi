// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisionComponents
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
    public class BodyTracker : IConsumerProducer<List<List<HumanBody>>, List<HumanBody>>
    {
        private uint peopleIndex = 0;
        private DeliveryPolicy deliveryPolicy;
        private TimeSpan removeThreshold = TimeSpan.FromSeconds(1);
        private Pipeline pipeline;
        private Dictionary<uint, (HumanBody, DateTime)> currTrackingPeople = new Dictionary<uint, (HumanBody, DateTime)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyTracker"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        public BodyTracker(Pipeline pipeline, DeliveryPolicy defaultDeliveryPolicy = null)
        {
            this.deliveryPolicy = defaultDeliveryPolicy ?? DeliveryPolicy.Unlimited;
            this.pipeline = pipeline;
            this.In = pipeline.CreateReceiver<List<List<HumanBody>>>(this, this.BodiesCallback, nameof(this.In));
            this.Out = pipeline.CreateEmitter<List<HumanBody>>(this, nameof(this.Out));
        }

        /// <inheritdoc/>
        public Receiver<List<List<HumanBody>>> In { get; private set; }

        /// <inheritdoc/>
        public Emitter<List<HumanBody>> Out { get; private set; }

        private void BodiesCallback(List<List<HumanBody>> msg, Envelope env)
        {
            var currentBodies = new List<HumanBody>();

            // We don't know when this is called, so we going to remove stall data based on a previous threshold
            this.currTrackingPeople = this.currTrackingPeople.Where(pair => (this.pipeline.GetCurrentTime() - pair.Value.Item2) < this.removeThreshold)
                                                             .ToDictionary(pair => pair.Key, pair => pair.Value);

            // TODO: Better tracking
            // Currently, it does a first come first server algorithm.
            var matchedIndex = new List<uint>();

            foreach (var candidate in msg)
            {
                // use a combined human body
                var combinedBody = HumanBody.CombineBodies(candidate);

                var found = false;

                // see whether ther is a good match for people we are already tracking
                foreach (var key in this.currTrackingPeople.Keys)
                {
                    // ignore if used
                    if (matchedIndex.IndexOf(key) != -1)
                    {
                        continue;
                    }

                    // see if they are a good match
                    if (Utils.CompareHumanBodies(this.currTrackingPeople[key].Item1, combinedBody))
                    {
                        // Matched
                        matchedIndex.Add(key);

                        // update the bodies
                        combinedBody.Id = key;
                        this.currTrackingPeople[key] = (combinedBody, env.OriginatingTime);
                        currentBodies.Add(combinedBody);
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                // We cannot find a good match. It might be someone new
                combinedBody.Id = this.peopleIndex;
                this.currTrackingPeople[this.peopleIndex++] = (combinedBody, env.OriginatingTime);
                currentBodies.Add(combinedBody);
            }

            this.Out.Post(currentBodies, env.OriginatingTime);
        }
    }
}