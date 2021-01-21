// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.StudyComponents
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
        private Dictionary<uint, (HumanBody body, DateTime time)> currTrackingPeople = new Dictionary<uint, (HumanBody, DateTime)>();

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

            // remove stall data (bodies last seen beyond a threshold).
            this.currTrackingPeople = this.currTrackingPeople.Where(pair => (env.OriginatingTime - pair.Value.time) < this.removeThreshold)
                                                             .ToDictionary(pair => pair.Key, pair => pair.Value);
            // TODO: Better tracking
            // Currently, it does a first come first server algorithm.
            var matchedIndex = new List<uint>();

            foreach (var candidate in msg)
            {
                var combinedBody = HumanBody.CombineBodies(candidate);  // use a combined human body
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
                    if (HumanBody.CompareHumanBodies(this.currTrackingPeople[key].body, combinedBody))
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