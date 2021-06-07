using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.StudyComponents
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    public class StateTracker : IConsumerProducer<string, Tuple<string, TimeSpan>>, ISourceComponent
    {
        private bool completed = false;
        private DateTime lastStateTime = DateTime.MinValue;
        private string lastStateName = "";

        public StateTracker(Pipeline p, string startName = "started")
        {
            this.In = p.CreateReceiver<string>(this, this.callback, nameof(this.In));
            this.Out = p.CreateEmitter<Tuple<string, TimeSpan>>(this, nameof(this.Out));
            this.lastStateName = startName;
        }

        private void onPipelineClose(object sender, PipelineCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void callback(string stateName, Envelope env)
        {
            if (!this.completed)
            {
                if (this.lastStateName != "")
                {
                    this.postState(env.OriginatingTime);
                }
                this.lastStateTime = env.OriginatingTime;
                this.lastStateName = stateName;
            }
        }

        private void postState(DateTime currTime)
        {
            this.Out.Post(new Tuple<string, TimeSpan>(lastStateName, currTime - lastStateTime), currTime);
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime.Invoke(DateTime.MaxValue);
            this.lastStateTime = this.Out.Pipeline.GetCurrentTime();
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            // Post the closing message.
            this.completed = true;
            this.postState(finalOriginatingTime);
            notifyCompleted.Invoke();
        }

        public Emitter<Tuple<string, TimeSpan>> Out { get; private set; }

        public Receiver<string> In { get; private set; }
    }
}
