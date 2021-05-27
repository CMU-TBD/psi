using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.StudyComponents
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;
    using Microsoft.Ros;
    using System.Linq;
    using TBD.Psi.RosSharpBridge;
    using Microsoft.Psi.Audio;
    using MathNet.Spatial.Euclidean;
    using MathNet.Numerics.LinearAlgebra;

    public class ROSStudyListener
    {
        private Pipeline p;
        private PsiExporter store;
        private RosSharpBridge bridge;


        public ROSStudyListener(Pipeline p, PsiExporter store,  string ws_uri)
        {
            this.p = p;
            this.store = store;
            this.bridge = new RosSharpBridge(p, ws_uri);
        }

        public void AddUtteranceListener(string topicName, string storeName = "")
        {
            var emitter = this.bridge.Subscribe<TBD.Psi.RosSharpBridge.Messages.Utterance>(topicName);
            emitter.Process<TBD.Psi.RosSharpBridge.Messages.Utterance, Tuple<string, TimeSpan>>((m,e,o) =>
            {
                var startTime = DateTimeOffset.FromUnixTimeSeconds(m.header.stamp.secs).DateTime + TimeSpan.FromTicks(m.header.stamp.nsecs / 100);
                var endTime = DateTimeOffset.FromUnixTimeSeconds(m.end_time.secs).DateTime + TimeSpan.FromTicks(m.end_time.nsecs / 100);
                o.Post( new Tuple<string, TimeSpan>(m.text, endTime - startTime), startTime);
            }).Write(storeName == "" ? topicName : storeName, this.store);
        }

        public IProducer<CoordinateSystem> AddCSListener(string topicName)
        {
            var emitter = this.bridge.Subscribe<RosSharp.RosBridgeClient.MessageTypes.Geometry.PoseStamped>(topicName);
            return emitter.Process<RosSharp.RosBridgeClient.MessageTypes.Geometry.PoseStamped, CoordinateSystem>((m, e, o) =>
            {
                var mat = Matrix<double>.Build.DenseIdentity(4);
                mat[0, 3] = m.pose.position.x;
                mat[1, 3] = m.pose.position.y;
                mat[2, 3] = m.pose.position.z;

                // convert quaternion to matrix
                mat[0, 0] = 1 - 2 * m.pose.orientation.y * m.pose.orientation.y - 2 * m.pose.orientation.z * m.pose.orientation.z;
                mat[0, 1] = 2 * m.pose.orientation.x * m.pose.orientation.y - 2 * m.pose.orientation.z * m.pose.orientation.w;
                mat[0, 2] = 2 * m.pose.orientation.x * m.pose.orientation.z + 2 * m.pose.orientation.y * m.pose.orientation.w;
                mat[1, 0] = 2 * m.pose.orientation.x * m.pose.orientation.y + 2 * m.pose.orientation.z * m.pose.orientation.w;
                mat[1, 1] = 1 - 2 * m.pose.orientation.x * m.pose.orientation.x - 2 * m.pose.orientation.z * m.pose.orientation.z;
                mat[1, 2] = 2 * m.pose.orientation.y * m.pose.orientation.z - 2 * m.pose.orientation.x * m.pose.orientation.w;
                mat[2, 0] = 2 * m.pose.orientation.x * m.pose.orientation.z - 2 * m.pose.orientation.y * m.pose.orientation.w;
                mat[2, 1] = 2 * m.pose.orientation.y * m.pose.orientation.z + 2 * m.pose.orientation.x * m.pose.orientation.w;
                mat[2, 2] = 1 - 2 * m.pose.orientation.x * m.pose.orientation.x - 2 * m.pose.orientation.y * m.pose.orientation.y;

                var cs = new CoordinateSystem(mat);
                o.Post(cs, e.OriginatingTime);
            });
        }

        public void AddCSListener(string topicName, string storeName = "")
        {
            var emitter = this.bridge.Subscribe<RosSharp.RosBridgeClient.MessageTypes.Geometry.PoseStamped>(topicName);
            emitter.Process<RosSharp.RosBridgeClient.MessageTypes.Geometry.PoseStamped, CoordinateSystem>((m, e, o) =>
            {
                var mat = Matrix<double>.Build.DenseIdentity(4);
                mat[0, 3] = m.pose.position.x;
                mat[1, 3] = m.pose.position.y;
                mat[2, 3] = m.pose.position.z;
                
                // convert quaternion to matrix
                mat[0, 0] = 1 - 2 * m.pose.orientation.y * m.pose.orientation.y - 2 * m.pose.orientation.z * m.pose.orientation.z;
                mat[0, 1] = 2 * m.pose.orientation.x * m.pose.orientation.y - 2 * m.pose.orientation.z * m.pose.orientation.w;
                mat[0, 2] = 2 * m.pose.orientation.x * m.pose.orientation.z + 2 * m.pose.orientation.y * m.pose.orientation.w;
                mat[1, 0] = 2 * m.pose.orientation.x * m.pose.orientation.y + 2 * m.pose.orientation.z * m.pose.orientation.w;
                mat[1, 1] = 1 - 2 * m.pose.orientation.x * m.pose.orientation.x - 2 * m.pose.orientation.z * m.pose.orientation.z;
                mat[1, 2] = 2 * m.pose.orientation.y * m.pose.orientation.z - 2 * m.pose.orientation.x * m.pose.orientation.w;
                mat[2, 0] = 2 * m.pose.orientation.x * m.pose.orientation.z - 2 * m.pose.orientation.y * m.pose.orientation.w;
                mat[2, 1] = 2 * m.pose.orientation.y * m.pose.orientation.z + 2 * m.pose.orientation.x * m.pose.orientation.w;
                mat[2, 2] = 1 - 2 * m.pose.orientation.x * m.pose.orientation.x - 2 * m.pose.orientation.y * m.pose.orientation.y;

                var cs =  new CoordinateSystem(mat);
                o.Post(cs, e.OriginatingTime);
            }).Write(storeName == "" ? topicName : storeName, this.store);
        }

        public void AddAudio(string topicName, string storeName = "")
        {
            var emitter = this.bridge.Subscribe< TBD.Psi.RosSharpBridge.Messages.AudioData> (topicName);
            emitter.Process<TBD.Psi.RosSharpBridge.Messages.AudioData, AudioBuffer>((m, e, o) =>
            {
                var buffer = new AudioBuffer(m.data, WaveFormat.Create16kHz1Channel16BitPcm());
                o.Post(buffer, e.OriginatingTime);
            }).Write(storeName == "" ? topicName : storeName, this.store);
        }

    }
}
