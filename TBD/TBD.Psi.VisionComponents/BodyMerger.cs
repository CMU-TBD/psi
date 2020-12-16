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
    /// A component that merges the body streams from multiple sources, and combine the bodies into a list, based on
    /// how likely they are the same.
    /// </summary>
    public class BodyMerger : Subpipeline, IProducer<List<List<HumanBody>>>
    {
        private readonly List<IProducer<List<HumanBody>>> producerList = new List<IProducer<List<HumanBody>>>();
        private readonly Connector<List<List<HumanBody>>> outConnector;
        private IProducer<List<HumanBody>> mainProducer;
        private int inputConnectorIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyMerger"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        public BodyMerger(Pipeline pipeline)
            : base(pipeline, nameof(BodyMerger))
        {
            pipeline.PipelineRun += this.PipelineStartEvent;
            this.outConnector = this.CreateOutputConnectorTo<List<List<HumanBody>>>(pipeline, nameof(this.outConnector));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyMerger"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        /// <param name="mainProducer">The primary producer of AzureKinectBodies. This body will be used before others.</param>
        public BodyMerger(Pipeline pipeline, IProducer<List<HumanBody>> mainProducer)
            : this(pipeline)
        {
            var inConnector = this.CreateInputConnectorFrom<List<HumanBody>>(mainProducer.Out.Pipeline, $"inCon{this.inputConnectorIndex++}");
            mainProducer.PipeTo(inConnector);
            this.mainProducer = inConnector.Out;
        }

        /// <inheritdoc/>
        public Emitter<List<List<HumanBody>>> Out => this.outConnector.Out;

        /// <summary>
        /// Add a Body Stream.
        /// </summary>
        /// <param name="producer">Azure kinect body stream producer.</param>
        /// <param name="isMain">Whether this body is a main input or not.</param>
        public void AddHumanBodyStream(IProducer<List<HumanBody>> producer, bool isMain = false)
        {
            if (isMain)
            {
                if (this.mainProducer != null)
                {
                    this.producerList.Add(this.mainProducer);
                }

                var inConnector = this.CreateInputConnectorFrom<List<HumanBody>>(producer.Out.Pipeline, $"inCon{this.inputConnectorIndex++}");
                producer.PipeTo(inConnector);
                this.mainProducer = inConnector.Out;
            }
            else
            {
                var inConnector = this.CreateInputConnectorFrom<List<HumanBody>>(producer.Out.Pipeline, $"inCon{this.inputConnectorIndex++}");
                producer.PipeTo(inConnector);
                this.producerList.Add(inConnector.Out);
            }
        }

        private void PipelineStartEvent(object sender, PipelineRunEventArgs e)
        {
            var joiner = new Join<List<HumanBody>, List<HumanBody>, List<HumanBody>, List<HumanBody>>(
              this.mainProducer.Out.Pipeline,
              Reproducible.Nearest<List<HumanBody>>(TimeSpan.FromMilliseconds(200)),
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
                var mergedList = new List<List<HumanBody>>();
                var usedBodiesList = new List<int>();
                for (int i = 0; i < listOfBodies.Count; i++)
                {
                    // If this body is already being used, continue
                    if (usedBodiesList.IndexOf(i) >= 0)
                    {
                        continue;
                    }

                    // create a collection with this body inside 
                    var currentCollection = new List<HumanBody>()
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

                        // check if the values are meaningful
                        if (HumanBody.CompareHumanBodies(listOfBodies[i], listOfBodies[j]))
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
