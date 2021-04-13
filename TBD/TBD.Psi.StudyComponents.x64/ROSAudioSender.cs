// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.StudyComponents
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using System.Linq;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Ros;
    using System.Threading;

    /// <summary>
    /// Component to Send Azure Kinect Bodies to ROS.
    /// </summary>
    public class ROSAudioSender : IConsumer<AudioBuffer>
    {
        private const string TopicName = "/psi_audio";
        private const string NodeName = "/psi_audio_pub";
        private string rosCoreAddress = "";
        private string rosClientAddress = "";
        private RosPublisher.IPublisher audioPub;
        private RosNode.Node node;
        private bool running = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ROSAudioSender"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        public ROSAudioSender(Pipeline pipeline, string rosCoreAddress, string rosClientAddress)
        {
            this.rosCoreAddress = rosCoreAddress;
            this.rosClientAddress = rosClientAddress;
            this.In = pipeline.CreateReceiver<AudioBuffer>(this, this.ReceiveAudio, nameof(this.In));
            pipeline.PipelineRun += this.PipelineStartCallback;
            
        }

        private void InitializeRosNode()
        {
            this.node = new RosNode.Node(NodeName, this.rosClientAddress, this.rosCoreAddress);
            this.audioPub = this.node.CreatePublisher(RosMessageTypes.AudioCommon.AudioData.Def, TopicName, false); 
            this.running = true;
        }

        private void ReattemptRosConnection()
        {

        }

        private void PipelineStartCallback(object sender, PipelineRunEventArgs e)
        {
            if (this.rosClientAddress.Length == 0 || this.rosCoreAddress.Length == 0)
            {
                throw new ArgumentException("RosClient or RosCore Address Invalid");
            }

            try
            {
                this.InitializeRosNode();
            }
            catch (System.Net.WebException)
            {
                Console.WriteLine("Unable to connect to ROSCore...");
            }
            // TODO build some kind of restart counters
        }



        /// <summary>
        /// Gets receiver for Azure Kinect Bodies.
        /// </summary>
        public Receiver<AudioBuffer> In { get; private set; }

        private void ReceiveAudio(AudioBuffer msg, Envelope env)
        {
            if (this.running)
            {
                var audioContainer = new RosMessageTypes.AudioCommon.AudioData.Kind(msg.Data);
                this.audioPub.Publish(RosMessageTypes.AudioCommon.AudioData.ToMessage(audioContainer));
            }
        }
    }
}
