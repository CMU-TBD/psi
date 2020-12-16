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
    /// A memory component that takes a list of human bodies and determine where they are
    /// </summary>
    public class BodyTracker : IConsumerProducer<List<List<HumanBody>>, List<HumanBody>>
    {
        private uint peopleIndex = 0;
        private TimeSpan temporalTolerance;
        private Pipeline pipeline;
        private Dictionary<uint, (HumanBody, DateTime)> currTrackingPeople = new Dictionary<uint, (HumanBody, DateTime)>();

        public BodyTracker(Pipeline p)
            : this(p, TimeSpan.FromSeconds(0.5))
        {
        }

        public BodyTracker(Pipeline p, TimeSpan temporalTolerance)
        {
            this.pipeline = p;
            this.temporalTolerance = temporalTolerance;
            this.In = pipeline.CreateReceiver<List<List<HumanBody>>>(this, this.BodiesCallback, nameof(this.In));
            this.Out = pipeline.CreateEmitter<List<HumanBody>>(this, nameof(this.Out));
        }


        /// <inheritdoc/>
        public Receiver<List<List<HumanBody>>> In { get; private set; }

        /// <inheritdoc/>
        public Emitter<List<HumanBody>> Out { get; private set; }

        private void BodiesCallback(List<List<HumanBody>> msg, Envelope env)
        {
            // House keeping:
            // remove previously tracked people outside of the temporal range.
            this.currTrackingPeople = this.currTrackingPeople.Where(pair => (env.OriginatingTime - pair.Value.Item2) < this.temporalTolerance)
                                                             .ToDictionary(pair => pair.Key, pair => pair.Value);

            // Variables:
            var currentBodies = new List<HumanBody>();


            // TODO: Better tracking
            // Currently, it does a first come first server algorithm.
            var matchedIndex = new List<uint>();

            var diffCompare = new List<(int, double, double)>();
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

                    // potential list
                    var jointList = new List<JointId>() { JointId.Pelvis, JointId.SpineChest, JointId.SpineNavel, JointId.ClavicleLeft, JointId.ClavicleRight, JointId.Neck };

                    // get the difference between bodies
                    var (matchedNum, transDiff, rotDiff) = HumanBody.CalculateDifferenceSum(this.currTrackingPeople[key].Item1, combinedBody, jointList);

                    if (matchedNum > 0)
                    {
                        // check if they are under the tolerance.
                        if ((transDiff/matchedNum) < 0.5 && (rotDiff/matchedNum) < 1.57)
                        {
                            // Matched
                            matchedIndex.Add(key);
                            // update the body id.
                            combinedBody.Id = key;
                            this.currTrackingPeople[key] = (combinedBody, env.OriginatingTime);
                            currentBodies.Add(combinedBody);
                            found = true;
                            break;
                        }
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