namespace TBD.Psi.VisualPipeline.Components
{
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Components;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class BodyTracker : IConsumerProducer<List<List<AzureKinectBody>>, List<AzureKinectBody>>
    {
        private uint peopleIndex = 0;
        private TimeSpan removeThreshold = TimeSpan.FromSeconds(1);
        private Pipeline pipeline;
        private Dictionary<uint, (AzureKinectBody, DateTime)> currTrackingPeople = new Dictionary<uint, (AzureKinectBody, DateTime)>();

        public BodyTracker(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.In = pipeline.CreateReceiver<List<List<AzureKinectBody>>>(this, this.BodiesCallback, nameof(this.In));
            this.Out = pipeline.CreateEmitter<List<AzureKinectBody>>(this, nameof(this.Out));
        }

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
                foreach(var key in currTrackingPeople.Keys)
                {
                    // ignore if used
                    if (matchedIndex.IndexOf(key) != -1)
                    {
                        continue;
                    }

                    //try to see if they are a good match
                    if (this.matchingBodies(currTrackingPeople[key].Item1, candidate))
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

        private static bool compareAzureBodies(AzureKinectBody b1, AzureKinectBody b2)
        {
            if (b1.Joints[JointId.Neck].Confidence != JointConfidenceLevel.None && b2.Joints[JointId.Neck].Confidence != JointConfidenceLevel.None)
            {
                var neck1 = b1.Joints[JointId.Neck].Pose;
                var neck2 = b2.Joints[JointId.Neck].Pose;
                var poseDiff = neck1 - neck2;
                return (Math.Sqrt(poseDiff[0, 3] * poseDiff[0, 3] + poseDiff[1, 3] * poseDiff[1, 3]) < 0.5);
            }
            return false;
        }

        private bool matchingBodies(AzureKinectBody tracked, List<AzureKinectBody> candidate)
        {
            foreach(var body in candidate)
            {
                if (compareAzureBodies(tracked, body))
                {
                    return true;
                }
            }
            return false;
        }

        public Receiver<List<List<AzureKinectBody>>> In { private set; get; }

        public Emitter<List<AzureKinectBody>> Out { private set; get; }
    }
}