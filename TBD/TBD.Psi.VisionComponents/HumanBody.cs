using MathNet.Spatial.Euclidean;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Psi.AzureKinect;
using System;
using System.Collections.Generic;
using System.Text;

namespace TBD.Psi.VisionComponents
{
    using System.Linq;

    public class HumanBody
    {
        List<JointId> jointList = new List<JointId>();

        public HumanBody()
        {
        }

        public HumanBody(HumanBody copy)
        {
            // copy properties.
            this.RootPose = copy.RootPose;
            this.Id = copy.Id;

            // copy the tree 
            var bonesHierarchy = AzureKinectBody.Bones;
            foreach(var pair in bonesHierarchy)
            {
                if (copy.BodyTree.Contains(pair.ParentJoint) && copy.BodyTree.Contains(pair.ChildJoint))
                {
                    this.BodyTree.UpdateTransformation(pair.ParentJoint, pair.ChildJoint, copy.BodyTree.SolveTransformation(pair.ParentJoint, pair.ChildJoint));
                }
            }
        }

        public bool FromAzureKinectBody(AzureKinectBody body)
        {
            // try to form a tree
            // step 1 find a base link, for now we will use spine chest
            var (rootPose, rootConfidence) = body.Joints[this.RootId];
            // ignore if root is unclear
            if (rootConfidence != Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium)
            {
                return false;
            }
            this.RootPose = rootPose;
            jointList.Add(this.RootId);

            // step 2 build the tree
            var bonesHierarchy = AzureKinectBody.Bones;
            // for each joint link, find the transformation between them
            foreach (var pair in bonesHierarchy)
            {
                var (childPose, childConfidence) = body.Joints[pair.ChildJoint];
                var (parentPose, parentConfidence) = body.Joints[pair.ParentJoint];
                // find the transformation between them
                var transform = childPose.TransformBy(parentPose.Invert());
                // only add them if they are both highly confident?
                if (childConfidence == Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium && parentConfidence == Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium)
                {
                    this.BodyTree.UpdateTransformation(pair.ParentJoint, pair.ChildJoint, transform);
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

        public TransformationTree<JointId> BodyTree { get; } = new TransformationTree<JointId>();
        public JointId RootId { get; } = JointId.Pelvis;
        public CoordinateSystem RootPose { get; set; }
        public uint Id { get; set; }

        public Ray3D? GazeDirection
        {
            get
            {
                // Origin Point is between the eyes
                var leftEye = this.GetJoint(JointId.EyeLeft);
                var rightEye = this.GetJoint(JointId.EyeRight);
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
                var leftClavicle = this.GetJoint(JointId.ClavicleLeft);
                var rightClavicle = this.GetJoint(JointId.ClavicleRight);
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


        public CoordinateSystem GetJoint(JointId jointId, bool InRealWorld = true)
        {
            return this.BodyTree.SolveTransformation(this.RootId, jointId)?.TransformBy(InRealWorld ? this.RootPose : new CoordinateSystem());
        }

        public static bool CompareHumanBodies(HumanBody body1, HumanBody body2, double distTol = 0.3, double rotTol = 1.5707, List<JointId> keyJoints = null)
        {
            // None of the keyjoints are passed in, we just compare neck and spine chest
            if (keyJoints == null)
            {
                keyJoints = new List<JointId>() { JointId.Neck, JointId.SpineChest };
            }

            var (matched, transDiff, rotDiff) = CalculateDifference(body1, body2, keyJoints);
            if (matched)
            {
                if (rotDiff/keyJoints.Count() < rotTol && transDiff/keyJoints.Count() < distTol)
                {
                    return true;
                }
            }
            return false;
        }

        public static (int mathces, double transDiffSum, double rotDiffSum) CalculateDifferenceSum(HumanBody body1, HumanBody body2, List<JointId> keyJoints = null)
        {
            // first get a list of joints that are both present!
            var b1Frames = body1.BodyTree.FrameIds;
            var pairedFrames = body2.BodyTree.FrameIds.Where(m => b1Frames.Contains(m));

            if (keyJoints != null)
            {
                pairedFrames = pairedFrames.Where(m => keyJoints.Contains(m));
            }

            var transDiffSum = 0.0;
            var rotDiffSum = 0.0;
            foreach (var paired in pairedFrames)
            {
                var b2Pose = body2.BodyTree.SolveTransformation(body2.RootId, paired).TransformBy(body2.RootPose);
                var b1Pose = body1.BodyTree.SolveTransformation(body1.RootId, paired).TransformBy(body1.RootPose);

                // calculate the difference
                var (transDiff, rotDiff) = Utils.CalculateDifference(b1Pose, b2Pose);

                transDiffSum += Math.Abs(transDiff);
                rotDiffSum += Math.Abs(rotDiff);
            }
            return (pairedFrames.Count(), transDiffSum, rotDiffSum);
        }

        public static (bool matched, double transDiff, double rotDiff) CalculateDifference(HumanBody body1, HumanBody body2, List<JointId> keyJoints)
        {
            // None of the keyjoints are passed in, we just compare neck and spine chest
            if (keyJoints == null)
            {
                keyJoints = new List<JointId>() { JointId.Neck, JointId.SpineChest };
            }

            // first check if the bodies have the key joints
            if (!keyJoints.All(j => body1.BodyTree.Contains(j) && body2.BodyTree.Contains(j)))
            {
                return (false, -1, -1);
            }

            var transDiffSum = 0.0;
            var rotDiffSum = 0.0;

            //compare those joints
            foreach (var key in keyJoints)
            {
                // first get the cs in both matrix
                var b1Pose = body1.BodyTree.SolveTransformation(body1.RootId, key).TransformBy(body1.RootPose);
                var b2Pose = body2.BodyTree.SolveTransformation(body2.RootId, key).TransformBy(body2.RootPose);

                // calculate the difference
                var (transDiff, rotDiff) = Utils.CalculateDifference(b1Pose, b2Pose);

                transDiffSum += Math.Abs(transDiff);
                rotDiffSum += Math.Abs(rotDiff);
            }
            return (true, transDiffSum, rotDiffSum);
        }

        public static HumanBody CombineBodies(List<HumanBody> bodies)
        {
            // create a new Human Body
            var mergedBody = new HumanBody(bodies[0]);
            if (bodies.Count > 1)
            {
                mergedBody.CombineBodies(bodies.Skip(0));
            }
            return mergedBody;
        }

        public void CombineBodies(IEnumerable<HumanBody> bodies)
        {
            // replace all the uncertain joints in the body with certain ones from the replacements
            // Note, because the bones are setup to be in the right hierarhical order, we don't have to worry about adding stuff for later
            foreach (var bone in AzureKinectBody.Bones)
            {
                // ignore if we already know the position
                if (this.BodyTree.Contains(bone.ChildJoint) && this.BodyTree.Contains(bone.ParentJoint))
                {
                    continue;
                }
                else if (this.BodyTree.Contains(bone.ParentJoint) || this.BodyTree.Contains(bone.ChildJoint))
                {
                    // we have the parent joint but not the child joint
                    // look for the child joint in the other bodies
                    var options = new List<CoordinateSystem>();
                    foreach (var b in bodies)
                    {
                        if (b.BodyTree.Contains(bone.ChildJoint) && b.BodyTree.Contains(bone.ParentJoint))
                        {
                            var transform = b.BodyTree.SolveTransformation(bone.ParentJoint, bone.ChildJoint);
                            options.Add(transform);
                        }
                    }
                    if (options.Count > 0)
                    {
                        // not sure how to merge it in a nice way, we use the first one for now.
                        this.BodyTree.UpdateTransformation(bone.ParentJoint, bone.ChildJoint, options[0]);
                    }
                }
            }

            // Change the position of the person to be the average of the current set
            // Currently, only updates the position.
            var pointArr = bodies.Select(m => m.RootPose.Origin);
            var currPoint = this.RootPose.Origin.ToVector3D();
            foreach (var point in pointArr)
            {
                currPoint += point.ToVector3D();
            }
            currPoint = currPoint.ScaleBy(1.0 / (pointArr.Count() + 1));
            this.RootPose = new CoordinateSystem(new Point3D(currPoint.X, currPoint.Y, currPoint.Z), this.RootPose.XAxis, this.RootPose.YAxis, this.RootPose.ZAxis);
        }

    }
}
