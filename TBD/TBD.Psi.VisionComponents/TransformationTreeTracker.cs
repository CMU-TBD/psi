using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.VisionComponents
{
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    public class TransformationTreeTracker : TransformationTree<string>, ISourceComponent
    {
        private Pipeline p;
        private System.Threading.Timer timer;
        private TimeSpan rate;

        private void timerCallback(object state)
        {
            var frameList = new List<(string, CoordinateSystem)>();
            frameList.Add(("world", new CoordinateSystem()));
            this.traverseTree("world", new CoordinateSystem(), frameList);
            this.WorldFrameOutput.Post(frameList.Select(m => m.Item2).ToList(), this.p.GetCurrentTime());
        }

        public TransformationTreeTracker(Pipeline p, double seconds = 1)
            : base()
        {
            this.p = p;
            this.rate = TimeSpan.FromSeconds(seconds);
            this.WorldFrameOutput = p.CreateEmitter<List<CoordinateSystem>>(this, nameof(this.WorldFrameOutput));
        }

        public Emitter<List<CoordinateSystem>> WorldFrameOutput { private set; get; }


        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // start the timer that generators the output
            this.timer = new System.Threading.Timer(this.timerCallback, null, TimeSpan.Zero, this.rate);
            notifyCompletionTime.Invoke(DateTime.MaxValue);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.timer.Dispose();
            notifyCompleted();
        }
    }
}
