﻿using MathNet.Spatial.Euclidean;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi.AzureKinect;
using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.StudyComponents
{
    using Microsoft.Psi;
    using System.Linq;

    public class HumanBody
    {
        List<JointId> jointList = new List<JointId>();
        Dictionary<JointId, JointConfidenceLevel> jointConfidenceLevels = new Dictionary<JointId, JointConfidenceLevel>();

        public HumanBody()
        {
        }

        public bool FromAzureKinectBody(AzureKinectBody body)
        {
            // try to form a tree
            // step 1 find a base link, for now we will use spine chest
            var (rootPose, rootConfidence) = body.Joints[this.RootId];

            int minimumConfidenceLevel = (int)Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Low;

            // ignore if we cannot find the root pose.
            if ((int)rootConfidence < minimumConfidenceLevel)
            {
                return false;
            }
            this.RootPose = rootPose;
            jointList.Add(this.RootId);
            this.jointConfidenceLevels[this.RootId] = rootConfidence;

            // step 2 build the tree
            var bonesHierarchy = AzureKinectBody.Bones;
            // for each joint link, find the transformation between them
            foreach (var pair in bonesHierarchy)
            {
                var (childPose, childConfidence) = body.Joints[pair.ChildJoint];
                var (parentPose, parentConfidence) = body.Joints[pair.ParentJoint];
                // only add them if they are both highly confident?
                if ((int)childConfidence >= minimumConfidenceLevel && (int)parentConfidence >= minimumConfidenceLevel)
                {
                    // find the transformation between them & save it 
                    var transform = childPose.TransformBy(parentPose.Invert());
                    this.BodyTree.UpdateTransformation(pair.ParentJoint, pair.ChildJoint, transform);
                    this.jointConfidenceLevels[pair.ChildJoint] = childConfidence;
                }
                else if ((int)childConfidence >= minimumConfidenceLevel)
                {
/*                    // only the child joint is found, but the parent didn't.
                    // recursively see if the parent's parent is available.
                    var parentJointId = pair.ParentJoint;
                    var ancestorJointId = pair.ParentJoint;
                    while (ancestorJointId != this.RootId)
                    {
                        ancestorJointId = bonesHierarchy.Where(m => m.ChildJoint == parentJointId).First().ParentJoint;
                        // see if ancestorjoint is available
                        var (ancestorPose, ancestorConfidence) = body.Joints[ancestorJointId];
                        if (ancestorConfidence >= Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Low)
                        {
                            // find the transformation between them & save it 
                            var transform = childPose.TransformBy(ancestorPose.Invert());
                            this.BodyTree.UpdateTransformation(ancestorJointId, pair.ChildJoint, transform);
                            break;
                        }
                    }       */             
                }
            }

            // set the ID of the body
            this.Id = body.TrackingId;

            return true;
        }

        public List<(JointId Id, CoordinateSystem Pose)> GetJointPoses()
        {
            // traverse the tree and find them
            var poseList = this.BodyTree.TraverseTree(this.RootId, this.RootPose);
            // Add the root pose to it.
            poseList.Add((this.RootId, this.RootPose));
            return poseList;
        }

        public JointConfidenceLevel getJointConfidenceLevel(JointId Id)
        {
            if (this.jointConfidenceLevels.ContainsKey(Id))
            {
                return this.jointConfidenceLevels[Id];
            }
            return JointConfidenceLevel.None;
        }

        public TransformationTree<JointId> BodyTree { get; } = new TransformationTree<JointId>();
        public JointId RootId { get; } = JointId.Pelvis;
        public CoordinateSystem RootPose { get; set; }
        public uint Id { get; set; }

        public Ray3D? GazeDirection
        {
            get
            {
                // Origin Point is between the eyes
                var leftEye = this.GetJoint(JointId.EyeLeft, JointConfidenceLevel.Medium);
                var rightEye = this.GetJoint(JointId.EyeRight, JointConfidenceLevel.Medium);
                if (leftEye != null && rightEye != null)
                {
                    // the eyes start in the middle
                    var origin = (leftEye.Origin.ToVector3D() + rightEye.Origin.ToVector3D()) / 2;
                    var direction = leftEye.ZAxis;
                    return new Ray3D(origin.ToPoint3D(), direction);
                }
                return null;
            }
        }

        public Ray3D? TorsoDirection
        {
            get
            {
                // Origin Point is between the eyes
                var leftClavicle = this.GetJoint(JointId.ClavicleLeft, JointConfidenceLevel.Medium);
                var rightClavicle = this.GetJoint(JointId.ClavicleRight, JointConfidenceLevel.Medium);
                if (leftClavicle != null && rightClavicle != null)
                {
                    // the eyes start in the middle
                    var origin = (leftClavicle.Origin.ToVector3D() + rightClavicle.Origin.ToVector3D()) / 2;
                    var direction = (leftClavicle.XAxis + -1 * rightClavicle.XAxis) / 2;
                    return new Ray3D(origin.ToPoint3D(), direction);
                }
                return null;
            }
        }


        public CoordinateSystem GetJoint(JointId jointId, JointConfidenceLevel minimumConfidenceLevel = JointConfidenceLevel.Low, bool InRealWorld = true)
        {
            if ( !this.jointConfidenceLevels.ContainsKey(jointId) || (int)this.jointConfidenceLevels[jointId] < (int)minimumConfidenceLevel)
            {
                return null;
            }
            return this.BodyTree.SolveTransformation(this.RootId, jointId)?.TransformBy(InRealWorld ? this.RootPose : new CoordinateSystem());
        }

        public static bool CompareHumanBodies(HumanBody body1, HumanBody body2, double distTol = 0.4, double rotTol = 0.7, List<JointId> keyJoints = null)
        {
            // Set the key joints if none is passed in
            if (keyJoints == null)
            {
                keyJoints = new List<JointId>() { JointId.Neck, JointId.SpineChest };
            }

            // make sure the bodies has the key joints
            if(!body1.BodyTree.Contains(keyJoints) || !body2.BodyTree.Contains(keyJoints))
            {
                return false;
            }

            //compare those joints
            foreach (var key in keyJoints)
            {
                // first get the cs in both matrix
                var b1Pose = body1.BodyTree.SolveTransformation(body1.RootId, key)?.TransformBy(body1.RootPose);
                var b2Pose = body2.BodyTree.SolveTransformation(body2.RootId, key)?.TransformBy(body2.RootPose);

                if (b1Pose == null || b2Pose == null)
                {
                    return false;
                }

                // calculate the difference
                var (transDiff, rotDiff) = Utils.CalculateDifference(b1Pose, b2Pose);

                // check if they are within reason.
                // The old code check if the rotation difference is out a certain tolerance. It was too sensitive to bad body
                // rotations especially estimated eyes or body orientation.
                if (transDiff > distTol)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Combine the bodies in the given list in a new body. 
        /// </summary>
        /// <param name="bodies">List of human bodies to combine.</param>
        /// <returns>New combined human body.</returns>
        public static HumanBody CombineBodies(List<HumanBody> bodies)
        {
            var newBody = bodies[0].DeepClone();
            if (bodies.Count > 1)
            {
                newBody.CombineBodies(bodies.Skip(1));
            }
            return newBody;
        }

        public void CombineBodies(IEnumerable<HumanBody> bodies)
        {

            // replace all the uncertain joints in the body with certain ones from the replacements
            // Note, the bones are setup to be in the right hierarhical order, we don't have to worry about adding stuff for later
            foreach (var bone in AzureKinectBody.Bones)
            {
                // ignore if we already know the position
                if (this.BodyTree.Contains(bone.ChildJoint) && this.BodyTree.Contains(bone.ParentJoint))
                {
                    // use the other bodies if they are more certain then us about the child
                    // TODO: Right now we just pick the first
                    var betterCandidate = bodies.Where(m => (int)m.getJointConfidenceLevel(bone.ChildJoint) > (int)this.getJointConfidenceLevel(bone.ChildJoint)).FirstOrDefault();

                    if (betterCandidate != null)
                    {
                        var transform = betterCandidate.BodyTree.SolveTransformation(bone.ParentJoint, bone.ChildJoint);
                        this.BodyTree.UpdateTransformation(bone.ParentJoint, bone.ChildJoint, transform);
                        this.jointConfidenceLevels[bone.ChildJoint] = betterCandidate.getJointConfidenceLevel(bone.ChildJoint);
                    }
                    continue;
                }
                else if (this.BodyTree.Contains(bone.ParentJoint))
                {
                    // we have the parent joint but not the child joint
                    // look for the child joint in the bodies
                    // TODO alternatives to just pick the first one
                    var alternative = bodies.Where(b => b.BodyTree.Contains(bone.ChildJoint) && b.BodyTree.Contains(bone.ParentJoint)).FirstOrDefault();
                    if (alternative != null)
                    {
                        var transform = alternative.BodyTree.SolveTransformation(bone.ParentJoint, bone.ChildJoint);
                        this.BodyTree.UpdateTransformation(bone.ParentJoint, bone.ChildJoint, transform);
                        this.jointConfidenceLevels[bone.ChildJoint] = alternative.getJointConfidenceLevel(bone.ChildJoint);
                    }
                }
            }

            // Change the position of the person to be the average of the current set
            var pointArr = bodies.Select(m => m.RootPose.Origin);
            var currPoint = this.RootPose.Origin.ToVector3D();
            foreach (var point in pointArr)
            {
                currPoint = currPoint + point.ToVector3D();
            }
            currPoint = currPoint.ScaleBy(1.0 / (pointArr.Count() + 1));
            this.RootPose = new CoordinateSystem(new Point3D(currPoint.X, currPoint.Y, currPoint.Z), this.RootPose.XAxis, this.RootPose.YAxis, this.RootPose.ZAxis);
        }

    }
}