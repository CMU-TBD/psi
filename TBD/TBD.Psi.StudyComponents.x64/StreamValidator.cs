
namespace TBD.Psi.StudyComponents
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    public class StreamValidator : IProducer<DateTime>
    {
        private Pipeline pipeline;
        private int incomingStreamTotal = 0;
        private int incomingStreamCount = 0;
        public StreamValidator(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.Out = pipeline.CreateEmitter<DateTime>(this, nameof(this.Out));
        }

        public void AddStream<T>(IProducer<T> incomingStream, string name = "")
        {
            this.incomingStreamTotal++;
            var receiverName = (name.Length == 0) ? $"receiver-{this.incomingStreamTotal}" : name;
            var receiver = this.pipeline.CreateReceiver<T>(this, (m, e) =>
            {
                Console.WriteLine($"Receiving Msg from {receiverName}");
                this.incomingStreamCount++;
                if (this.incomingStreamCount == this.incomingStreamTotal)
                {
                    this.Out.Post(e.OriginatingTime, e.OriginatingTime);
                }
            }, receiverName);
            incomingStream.First().PipeTo(receiver);
        }

        public Emitter<DateTime> Out { get; private set; }
    }
}
