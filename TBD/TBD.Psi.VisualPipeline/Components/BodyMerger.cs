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
    using Microsoft.Psi.Kinect;

    /// <summary>
    /// Psi Component that merges kinect bodies from multiple sources.
    /// </summary>
    public class BodyMerger : Subpipeline, IProducer<List<List<AzureKinectBody>>>
    {
        private readonly List<IProducer<List<AzureKinectBody>>> producerList = new List<IProducer<List<AzureKinectBody>>>();
        private readonly Connector<List<List<AzureKinectBody>>> outConnector;
        private IProducer<List<AzureKinectBody>> mainProducer;
        private int inputConnectorIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyMerger"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        public BodyMerger(Pipeline pipeline)
            : base(pipeline, nameof(BodyMerger))
        {
            pipeline.PipelineRun += this.PipelineStartEvent;
            this.outConnector = this.CreateOutputConnectorTo<List<List<AzureKinectBody>>>(pipeline, nameof(this.outConnector));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyMerger"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        /// <param name="mainProducer">The primary producer of AzureKinectBodies. This body will be used before others.</param>
        public BodyMerger(Pipeline pipeline, IProducer<List<AzureKinectBody>> mainProducer)
            : this(pipeline)
        {
            var inConnector = this.CreateInputConnectorFrom<List<AzureKinectBody>>(mainProducer.Out.Pipeline, $"inCon{this.inputConnectorIndex++}");
            mainProducer.PipeTo(inConnector);
            this.mainProducer = inConnector.Out;
        }

        /// <inheritdoc/>
        public Emitter<List<List<AzureKinectBody>>> Out => this.outConnector.Out;

        /// <summary>
        /// Add a Kinect2 Body Stream.
        /// </summary>
        /// <param name="producer">Kinect2 body stream producer.</param>
        /// <param name="isMain">Whether this body is a main input or not.</param>
        public void AddKinect2BodyStream(IProducer<List<KinectBody>> producer, bool isMain = false)
        {
            this.AddAzureKinectBodyStream(producer.ToAzureKinectBodies(), isMain);
        }

        /// <summary>
        /// Add a Azure Kinect Body Stream.
        /// </summary>
        /// <param name="producer">Azure kinect body stream producer.</param>
        /// <param name="isMain">Whether this body is a main input or not.</param>
        public void AddAzureKinectBodyStream(IProducer<List<AzureKinectBody>> producer, bool isMain = false)
        {
            if (isMain)
            {
                if (this.mainProducer != null)
                {
                    this.producerList.Add(this.mainProducer);
                }

                var inConnector = this.CreateInputConnectorFrom<List<AzureKinectBody>>(producer.Out.Pipeline, $"inCon{this.inputConnectorIndex++}");
                producer.PipeTo(inConnector);
                this.mainProducer = inConnector.Out;
            }
            else
            {
                var inConnector = this.CreateInputConnectorFrom<List<AzureKinectBody>>(producer.Out.Pipeline, $"inCon{this.inputConnectorIndex++}");
                producer.PipeTo(inConnector);
                this.producerList.Add(inConnector.Out);
            }
        }

        private static bool CompareAzureBodies(AzureKinectBody b1, AzureKinectBody b2)
        {
            if (b1.Joints[JointId.Neck].Confidence != JointConfidenceLevel.None && b2.Joints[JointId.Neck].Confidence != JointConfidenceLevel.None)
            {
                var neck1 = b1.Joints[JointId.Neck].Pose;
                var neck2 = b2.Joints[JointId.Neck].Pose;
                var poseDiff = neck1 - neck2;
                return Math.Sqrt((poseDiff[0, 3] * poseDiff[0, 3]) + (poseDiff[1, 3] * poseDiff[1, 3])) < 0.3;
            }

            return false;
        }

        private void PipelineStartEvent(object sender, PipelineRunEventArgs e)
        {
            var joiner = new Join<List<AzureKinectBody>, List<AzureKinectBody>, List<AzureKinectBody>, List<AzureKinectBody>>(
              this.mainProducer.Out.Pipeline,
              Reproducible.Nearest<List<AzureKinectBody>>(TimeSpan.FromMilliseconds(200)),
              (m, secondaryArray) => m.Concat(secondaryArray.SelectMany(m => m)).ToList(),
              this.producerList.Count,
              null);

            this.mainProducer.PipeTo(joiner.InPrimary, DeliveryPolicy.LatestMessage);
            for (var i = 0; i < this.producerList.Count; i++)
            {
                this.producerList[i].PipeTo(joiner.InSecondaries[i]);
            }

            joiner.Out.Select((listOfBodies, e) =>
            {

                // For now, we going to use a first-come first server algorithm.
                // TODO: In the future, some kind of Hungarian Algorithm or matchin
                // algorithm should be used.
                var mergedList = new List<List<AzureKinectBody>>();
                var usedBodiesList = new List<int>();
                for (int i = 0; i < listOfBodies.Count; i++)
                {
                    // If this body is already being used, continue
                    if (usedBodiesList.IndexOf(i) >= 0)
                    {
                        continue;
                    }

                    // create a collection with this body inside 
                    var currentCollection = new List<AzureKinectBody>()
                    {
                        listOfBodies[i],
                    };

                    // compare with all remaining bodies
                    for (int j = i + 1; j < listOfBodies.Count; j++)
                    {
                        // ignore if used
                        if (usedBodiesList.IndexOf(i) >= 0)
                        {
                            continue;
                        }

                        // check if the values are meaningful.
                        if (CompareAzureBodies(listOfBodies[i], listOfBodies[j]))
                        {
                            usedBodiesList.Add(j);
                            currentCollection.Add(listOfBodies[j]);
                        }
                    }

                    mergedList.Add(currentCollection);
                }

                return mergedList;
            }).Out.PipeTo(this.outConnector);
        }
    }
}
