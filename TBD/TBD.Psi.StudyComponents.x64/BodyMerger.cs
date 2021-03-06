﻿// Copyright (c) Carnegie Mellon University. All rights reserved.
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
    /// Psi Component that merges kinect bodies from multiple sources.
    /// </summary>
    public class BodyMerger : Subpipeline, IProducer<List<List<HumanBody>>>
    {
        private readonly List<IProducer<List<HumanBody>>> producerList = new List<IProducer<List<HumanBody>>>();
        private readonly Connector<List<List<HumanBody>>> outConnector;
        private DeliveryPolicy deliveryPolicy;
        private IProducer<List<HumanBody>> mainProducer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyMerger"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        public BodyMerger(Pipeline pipeline, DeliveryPolicy defaultDeliveryPolicy = null)
            : base(pipeline, nameof(BodyMerger), defaultDeliveryPolicy ?? DeliveryPolicy.Unlimited)
        {
            pipeline.PipelineRun += this.PipelineStartEvent;
            this.deliveryPolicy = defaultDeliveryPolicy ?? DeliveryPolicy.Unlimited;
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
            var inConnector = this.CreateInputConnectorFrom<List<HumanBody>>(mainProducer.Out.Pipeline, $"inCon-{mainProducer.Out.Name}");
            mainProducer.PipeTo(inConnector, this.deliveryPolicy);
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

                var inConnector = this.CreateInputConnectorFrom<List<HumanBody>>(producer.Out.Pipeline, $"inCon-{producer.Out.Name}");
                producer.PipeTo(inConnector, this.deliveryPolicy);
                this.mainProducer = inConnector.Out;
            }
            else
            {
                var inConnector = this.CreateInputConnectorFrom<List<HumanBody>>(producer.Out.Pipeline, $"inCon-{producer.Out.Name}");
                producer.PipeTo(inConnector, this.deliveryPolicy);
                if (this.mainProducer == null)
                {
                    this.mainProducer = inConnector.Out;
                }
                else
                {
                    this.producerList.Add(inConnector.Out);

                }
            }
        }

        private void PipelineStartEvent(object sender, PipelineRunEventArgs e)
        {
            if (this.mainProducer != null)
            {
                var joiner = new Join<List<HumanBody>, List<HumanBody>, List<HumanBody>, List<HumanBody>>(
                  this.mainProducer.Out.Pipeline,
                  Reproducible.Nearest<List<HumanBody>>(TimeSpan.FromMilliseconds(200)),
                  (m, secondaryArray) => m.Concat(secondaryArray.SelectMany(m => m)).ToList(),
                  this.producerList.Count,
                  null);

                this.mainProducer.PipeTo(joiner.InPrimary, this.deliveryPolicy);
                for (var i = 0; i < this.producerList.Count; i++)
                {
                    this.producerList[i].PipeTo(joiner.InSecondaries[i], this.deliveryPolicy);
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
                }, this.deliveryPolicy).Out.PipeTo(this.outConnector, this.deliveryPolicy);
            }
        }
    }
}
