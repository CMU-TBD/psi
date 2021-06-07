// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.StudyComponents
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using System.Linq;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Ros;
    using RosMessageTypes = TBD.Psi.Ros.RosMessageTypes;
    using System.Threading;

    /// <summary>
    /// Component to Send Azure Kinect Bodies to ROS.
    /// </summary>
    public class ROSWorldSender : IConsumer<List<HumanBody>>
    {
        private Pipeline pipeline;
        private const string TopicName = "/humans";
        private const string NodeName = "/psi_pub";
        private string rosCoreAddress = "";
        private string rosClientAddress = "";
        private RosPublisher.IPublisher bodyPub;
        private RosNode.Node node;
        private uint msgSeq = 0;
        private bool running = false;
        private bool useRealTime = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ROSWorldSender"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        public ROSWorldSender(Pipeline pipeline, string rosCoreAddress, string rosClientAddress, bool useRealTime = false)
        {
            this.pipeline = pipeline;
            this.rosCoreAddress = rosCoreAddress;
            this.rosClientAddress = rosClientAddress;
            this.In = pipeline.CreateReceiver<List<HumanBody>>(this, this.ReceiveBodies, nameof(this.In));
            pipeline.PipelineRun += this.PipelineStartCallback;
            this.useRealTime = useRealTime;
        }

        private void InitializeRosNode()
        {
            this.node = new RosNode.Node(NodeName, this.rosClientAddress, this.rosCoreAddress);
            this.bodyPub = this.node.CreatePublisher(RosMessageTypes.tbd_ros_msgs.HumanBodyArray.Def, TopicName, false);
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
        public Receiver<List<HumanBody>> In { get; private set; }

        private void ReceiveBodies(List<HumanBody> msg, Envelope env)
        {
            if (this.running)
            {
                var bodyList = new List<RosMessageTypes.tbd_ros_msgs.HumanBody.Kind>();
                foreach (var body in msg)
                {
                    var jointList = new List<RosMessageTypes.tbd_ros_msgs.HumanJoint.Kind>();
                    foreach (var joint in body.GetJointPoses())
                    {
                        var pose = joint.Pose;
                        var jointId = joint.Id;
                        var q = Utils.GetQuaternionFromCoordinateSystem(pose);
                        var poseKind = new Microsoft.Ros.RosMessageTypes.Geometry.Pose.Kind(
                            new Microsoft.Ros.RosMessageTypes.Geometry.Point.Kind(pose.At(0, 3), pose.At(1, 3), pose.At(2, 3)),
                            new Microsoft.Ros.RosMessageTypes.Geometry.Quaternion.Kind(q.X, q.Y, q.Z, q.W));

                        var jointKind = new RosMessageTypes.tbd_ros_msgs.HumanJoint.Kind(
                            (byte)jointId,
                            poseKind);
                        jointList.Add(jointKind);
                    }

                    // Note that the ROS module automaticallys converts UTC time to EPOCH time
                    // used in ROS.
                    var bodyKind = new RosMessageTypes.tbd_ros_msgs.HumanBody.Kind(
                        new Microsoft.Ros.RosMessageTypes.Standard.Header.Kind(
                            this.msgSeq++,
                            this.useRealTime ? DateTime.UtcNow : env.OriginatingTime,
                            "PsiWorld"
                        ),
                        body.Id,
                        jointList);
                    bodyList.Add(bodyKind);
                }

                this.bodyPub.Publish(RosMessageTypes.tbd_ros_msgs.HumanBodyArray.ToMessage(
                    new RosMessageTypes.tbd_ros_msgs.HumanBodyArray.Kind(bodyList)));
            }
        }
    }
}
