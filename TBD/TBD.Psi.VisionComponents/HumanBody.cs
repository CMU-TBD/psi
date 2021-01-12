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

        public bool FromAzureKinectBody(AzureKinectBody body)
        {
            // try to form a tree
            // step 1 find a base link, for now we will use spine chest
            var (rootPose, rootConfidence) = body.Joints[this.RootId];

            int minimumConfidenceLevel = (int)JointConfidenceLevel.Low;

            // ignore if we cannot find the root pose.
            if ((int)rootConfidence < minimumConfidenceLevel)
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
                // only add them if they are both highly confident?
                if ((int)childConfidence >= minimumConfidenceLevel && (int)parentConfidence >= minimumConfidenceLevel)
                {
                    // find the transformation between them & save it 
                    var transform = childPose.TransformBy(parentPose.Invert());
                    this.BodyTree.UpdateTransformation(pair.ParentJoint, pair.ChildJoint, transform);
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

        public static bool CompareHumanBodies(HumanBody body1, HumanBody body2, double distTol = 0.5, double rotTol = 0.7, List<JointId> keyJoints = null)
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
                var b1Pose = body1.BodyTree.SolveTransformation(body1.RootId, key).TransformBy(body1.RootPose);
                var b2Pose = body2.BodyTree.SolveTransformation(body2.RootId, key).TransformBy(body2.RootPose);

                // calculate the difference
                var (transDiff, rotDiff) = Utils.CalculateDifference(b1Pose, b2Pose);

                // check if they are within reason.
                if (transDiff > distTol && rotDiff > rotTol)
                {
                    return false;
                }
            }
            return true;
        }

        public static HumanBody CombineBodies(List<HumanBody> bodies)
        {
            if (bodies.Count > 1)
            {
                bodies[0].CombineBodies(bodies.Skip(1));
            }
            return bodies[0];
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
                    continue;
                }
                else if (this.BodyTree.Contains(bone.ParentJoint) || this.BodyTree.Contains(bone.ChildJoint))
                {
                    // we have the parent joint but not the child joint
                    // look for the child joint in the bodies
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
