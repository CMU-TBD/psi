
namespace TBD.Psi.StudyComponents
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    public class StreamValidator : Subpipeline, IProducer<DateTime>
    {
        private Pipeline parent;
        private Connector<DateTime> outConnector;
        private List<IProducer<DateTime>> streamSources = new List<IProducer<DateTime>>();
        public StreamValidator(Pipeline pipeline)
            : base(pipeline)
        {
            this.parent = pipeline;
            this.outConnector = this.CreateOutputConnectorTo<DateTime>(pipeline, nameof(this.outConnector));
            pipeline.PipelineRun += this.pipelineStarted;
            
        }

        private void pipelineStarted(object sender, PipelineRunEventArgs e)
        {
            if (this.streamSources.Count > 1)
            {
                // link up all the sources
                var fuser = new Fuse<DateTime, DateTime, DateTime, DateTime>(
                    this,
                    Available.AllFirst<DateTime>(TimeSpan.MaxValue),
                    (m1, m2) =>
                    {
                        var list = new List<DateTime>(m2);
                        list.Add(m1);
                        list.Sort();
                        return list.LastOrDefault();
                    },
                    this.streamSources.Count-1);
                this.streamSources[0].PipeTo(fuser.InPrimary);
                for (var i = 0; i < this.streamSources.Count-1; i++)
                {
                    this.streamSources[i+1].PipeTo(fuser.InSecondaries[i]);
                }
                fuser.Out.Name = "zhi-test";
                fuser.Out.PipeTo(this.outConnector);
            }
            else if (this.streamSources.Count == 1)
            {
                this.streamSources[0].PipeTo(this.outConnector);
            }
        }

        public void AddStream<T>(IProducer<T> incomingStream, string name = "")
        {
            // create an input connector
            var inputConnector = this.CreateInputConnectorFrom<T>(this.parent, $"In-{incomingStream.Out.Name}");
            incomingStream.PipeTo(inputConnector);
            streamSources.Add(inputConnector.First().Select((m, e) => {
                Console.WriteLine($"Receiving Msg from {((name.Length == 0) ? incomingStream.Out.Name : name)}"); 
                return e.OriginatingTime;
                }));
        }

        public Emitter<DateTime> Out => this.outConnector.Out;
    }
}
