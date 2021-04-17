using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.StudyComponents
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    public class HumanSituatedFilter : IConsumerProducer<List<HumanBody>, List<HumanBody>>
    {
        public HumanSituatedFilter(Pipeline p)
        {
            this.In = p.CreateReceiver<List<HumanBody>>(this, this.humanReceiver, nameof(this.In));
            this.Out = p.CreateEmitter<List<HumanBody>>(this, nameof(this.Out));
        }

        private void humanReceiver(List<HumanBody> msg, Envelope env)
        {
            var newList = new List<HumanBody>();
            foreach (var humanBody in msg)
            {
                // a bunch of heutristic rules about whether a human body is valid
                if (humanBody.EstimatedHeight >= 1)
                {
                    newList.Add(humanBody);
                }
            }
            this.Out.Post(newList, env.OriginatingTime);
        }

        public Receiver<List<HumanBody>> In { get; private set; }
        public Emitter<List<HumanBody>> Out { get; private set; }
    }
}
