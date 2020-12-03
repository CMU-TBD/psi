﻿using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.VisionComponents
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    public class TransformationTree<T>
    {

        private Dictionary<T, Dictionary<T, CoordinateSystem>> tree = new Dictionary<T, Dictionary<T, CoordinateSystem>>();

        protected void traverseTree(T parent, CoordinateSystem transform, List<(T id, CoordinateSystem Pose)> frameList)
        {
            foreach(var child in this.tree[parent].Keys)
            {
                var childTransfrom = this.tree[parent][child].TransformBy(transform);
                frameList.Add((child, childTransfrom));
                if (this.tree.ContainsKey(child))
                {
                    this.traverseTree(child, childTransfrom, frameList);
                }
            }
        }


        public TransformationTree()
        {
        }

        public List<(T, CoordinateSystem)> TraverseTree(T root, CoordinateSystem rootPose)
        {
            var poseList = new List<(T Id, CoordinateSystem Pose)>();
            this.traverseTree(root, rootPose, poseList);
            return poseList;
        }

        public bool UpdateTransformation(T frameA, T frameB, double[,] mat)
        {
            return this.UpdateTransformation(frameA, frameB, new CoordinateSystem(Matrix<double>.Build.DenseOfArray(mat)));
        }

        public bool UpdateTransformation(T frameA, T frameB, CoordinateSystem transform)
        {

            if (this.tree.ContainsKey(frameA) || this.tree.ContainsKey(frameB))
            {
                // if this exist
                if (this.tree.ContainsKey(frameA) && !this.tree.ContainsKey(frameB))
                {
                    this.tree[frameA][frameB] = transform;
                    this.tree[frameB] = new Dictionary<T, CoordinateSystem>();
                }
                else if (!this.tree.ContainsKey(frameA) && this.tree.ContainsKey(frameB))
                {
                    this.tree[frameB][frameA] = transform.Invert();
                    this.tree[frameA] = new Dictionary<T, CoordinateSystem>();
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
                this.tree[frameA] = new Dictionary<T, CoordinateSystem>();
                this.tree[frameB] = new Dictionary<T, CoordinateSystem>();
                this.tree[frameA][frameB] = transform;

            }
            return true;
        }

        protected CoordinateSystem recursiveSearchNode(T parent, T target)
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

        public CoordinateSystem SolveTransformation(T frameA, T frameB)
        {
            // check if both frames are in the tree
            if (!this.tree.ContainsKey(frameA) || !this.tree.ContainsKey(frameB))
            {
                return null;
            }

            // if they are the same, return identity
            if (EqualityComparer<T>.Default.Equals(frameA, frameB))
            {
                return new CoordinateSystem();
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
    }
}
