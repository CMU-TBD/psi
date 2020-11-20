using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.VisionComponents
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    public class TransformationTree : ISourceComponent
    {
        private Pipeline p;
        private bool running;
        private System.Threading.Timer timer;
        private TimeSpan rate;
        private Dictionary<string, Dictionary<string, CoordinateSystem>> tree = new Dictionary<string, Dictionary<string, CoordinateSystem>>();

        private void traverseTree(string parent, CoordinateSystem transform, List<CoordinateSystem> frameList)
        {
            foreach(var child in this.tree[parent].Keys)
            {
                var childTransfrom = transform.TransformBy(this.tree[parent][child]);
                frameList.Add(childTransfrom);
                if (this.tree.ContainsKey(child))
                {
                    this.traverseTree(child, childTransfrom, frameList);
                }
            }
        }

        private void timerCallback(object state)
        {
            var frameList = new List<CoordinateSystem>();
            frameList.Add(new CoordinateSystem());
            this.traverseTree("world", new CoordinateSystem(), frameList);
            this.WorldFrameOutput.Post(frameList, this.p.GetCurrentTime());
        }

        public TransformationTree(Pipeline p, double seconds = 1)
        {
            this.p = p;
            this.rate = TimeSpan.FromSeconds(seconds);
            this.WorldFrameOutput = p.CreateEmitter<List<CoordinateSystem>>(this, nameof(this.WorldFrameOutput));
        }

        public Emitter<List<CoordinateSystem>> WorldFrameOutput { private set; get; }


        public bool UpdateTransformation(string frameA, string frameB, double[,] mat)
        {
            return this.UpdateTransformation(frameA, frameB, new CoordinateSystem(Matrix<double>.Build.DenseOfArray(mat)));
        }

        public bool UpdateTransformation(string frameA, string frameB, CoordinateSystem transform)
        {

            if (this.tree.ContainsKey(frameA) || this.tree.ContainsKey(frameB))
            {
                // if this exist
                if (this.tree.ContainsKey(frameA) && !this.tree.ContainsKey(frameB))
                {
                    this.tree[frameA][frameB] = transform;
                    this.tree[frameB] = new Dictionary<string, CoordinateSystem>();
                }
                else if (!this.tree.ContainsKey(frameA) && this.tree.ContainsKey(frameB))
                {
                    this.tree[frameB][frameA] = transform.Invert();
                    this.tree[frameA] = new Dictionary<string, CoordinateSystem>();
                }
                else
                {
                    // update the existing graph to prevent loop
                    if (this.tree[frameA].ContainsKey(frameB))
                    {
                        this.tree[frameA][frameB] = transform;
                    }
                    else if (this.tree[frameB].ContainsKey(frameA))
                    {
                        this.tree[frameB][frameA] = transform.Invert();
                    }
                }
            }
            else
            {
                // for all of them.
                this.tree[frameA] = new Dictionary<string, CoordinateSystem>();
                this.tree[frameB] = new Dictionary<string, CoordinateSystem>();
                this.tree[frameA][frameB] = transform;

            }
            return true;
        }

        private CoordinateSystem recursiveSearchNode(string parent, string target)
        {
            // check end condition
            if (this.tree[parent].ContainsKey(target))
            {
                return this.tree[parent][target];
            }
            
            foreach(var child in this.tree[parent].Keys)
            {
                if(this.tree.ContainsKey(child))
                {
                    // search the children
                    var transform = this.recursiveSearchNode(child, target);
                    if (transform != null)
                    {
                        return transform.TransformBy(this.tree[parent][child]);
                    }
                }
            }
            return null;
        }

        public CoordinateSystem SolveTransformation(string frameA, string frameB)
        {
            // check if both frames are in the tree
            if (!this.tree.ContainsKey(frameA) || !this.tree.ContainsKey(frameB))
            {
                return null;
            }

            // start from A
            var transform = this.recursiveSearchNode(frameA, frameB);
            if(transform == null)
            {
                // search the otherway in case they are above it
                transform = this.recursiveSearchNode(frameB, frameA);
                if (transform == null)
                {
                    return null;
                }
                return transform.Invert();
            }
            return transform;
        }

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
