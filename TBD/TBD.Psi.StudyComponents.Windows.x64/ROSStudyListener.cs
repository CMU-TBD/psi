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
    }
}
