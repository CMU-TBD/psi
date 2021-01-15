// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisionComponents
{
    using System.Collections.Generic;
    using Microsoft.Psi;
    using System.Linq;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Ros;
    using RosMessageTypes = TBD.Psi.Ros.RosMessageTypes;

    /// <summary>
    /// Component to Send Azure Kinect Bodies to ROS.
    /// </summary>
    public class ROSWorldSender : IConsumer<List<HumanBody>>
    {
        private const string TopicName = "/bodies";
        private const string RosMaster = "127.0.0.1";
        private const string RosSlave = "127.0.0.1";
        private const string NodeName = "/psi_pub";
        private RosPublisher.IPublisher bodyPub;
        private RosNode.Node node;
        private uint msgSeq = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ROSWorldSender"/> class.
        /// </summary>
        /// <param name="pipeline">Current pipeline.</param>
        public ROSWorldSender(Pipeline pipeline)
        {
            this.In = pipeline.CreateReceiver<List<HumanBody>>(this, this.ReceiveBodies, nameof(this.In));
            this.node = new RosNode.Node(NodeName, RosSlave, RosMaster);
            this.bodyPub = this.node.CreatePublisher(RosMessageTypes.tbd_ros_msgs.HumanBodyArray.Def, TopicName, false);
        }

        /// <summary>
        /// Gets receiver for Azure Kinect Bodies.
        /// </summary>
        public Receiver<List<HumanBody>> In { get; private set; }

        private void ReceiveBodies(List<HumanBody> msg, Envelope env)
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

                var bodyKind = new RosMessageTypes.tbd_ros_msgs.HumanBody.Kind(
                    new Microsoft.Ros.RosMessageTypes.Standard.Header.Kind(
                        this.msgSeq++,
                        env.OriginatingTime,
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
