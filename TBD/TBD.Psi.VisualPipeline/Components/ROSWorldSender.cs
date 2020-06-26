namespace TBD.Psi.VisualPipeline.Components
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.FSharp.Collections;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Ros;
    using System.Collections.Generic;
    using TBD.Psi.Ros;
    using RosMessageTypes = Ros.RosMessageTypes;

    class ROSWorldSender : IConsumer<List<AzureKinectBody>>
    {
        private const string TopicName = "/bodies";
        private const string RosMaster = "127.0.0.1";
        private const string RosSlave = "127.0.0.1";
        private const string NodeName = "/psi_pub";
        private RosPublisher.IPublisher bodyPub;
        private RosNode.Node node;
        private uint msgSeq = 0;

        public ROSWorldSender(Pipeline pipeline)
        {
            this.In = pipeline.CreateReceiver<List<AzureKinectBody>>(this, this.receiveBodies, nameof(this.In));

 
            this.node = new RosNode.Node(NodeName, RosSlave, RosMaster);
            this.bodyPub = this.node.CreatePublisher(RosMessageTypes.tbd_ros_msgs.HumanBodyArray.Def, TopicName, false);
        }

        private void receiveBodies(List<AzureKinectBody> msg, Envelope env)
        {
            var bodyList = new List<RosMessageTypes.tbd_ros_msgs.HumanBody.Kind>();
            foreach (var body in msg)
            {
                var jointList = new List<RosMessageTypes.tbd_ros_msgs.HumanJoint.Kind>();
                foreach(var joint in body.Joints)
                {
                    var q = Utils.GetQuaternionFromCoordinateSystem(joint.Value.Pose);
                    var poseKind = new Microsoft.Ros.RosMessageTypes.Geometry.Pose.Kind(
                        new Microsoft.Ros.RosMessageTypes.Geometry.Point.Kind(joint.Value.Pose.At(0, 3), joint.Value.Pose.At(1, 3), joint.Value.Pose.At(2, 3)),
                        new Microsoft.Ros.RosMessageTypes.Geometry.Quaternion.Kind(q.X, q.Y, q.Z, q.W));

                    var jointKind = new RosMessageTypes.tbd_ros_msgs.HumanJoint.Kind(
                        (byte)joint.Key,
                        poseKind);
                    jointList.Add(jointKind);
                }
                var bodyKind = new RosMessageTypes.tbd_ros_msgs.HumanBody.Kind(
                    new Microsoft.Ros.RosMessageTypes.Standard.Header.Kind(
                        this.msgSeq++,
                        env.OriginatingTime,
                        "World"
                    ),
                    body.TrackingId,
                    jointList);
                bodyList.Add(bodyKind);
            }
            this.bodyPub.Publish(RosMessageTypes.tbd_ros_msgs.HumanBodyArray.ToMessage(
                new RosMessageTypes.tbd_ros_msgs.HumanBodyArray.Kind(bodyList)));
        }

        public Receiver<List<AzureKinectBody>> In { private set; get; }
    }
}
