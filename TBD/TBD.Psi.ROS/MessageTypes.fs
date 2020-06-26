namespace TBD.Psi.Ros

module RosMessageTypes = 
    
    open System
    open Microsoft.Ros.RosMessage
    open Microsoft.Ros.RosMessageTypes

    module tbd_ros_msgs = 

        module HumanJoint =
            let Def = { Type   = "tbd_ros_msgs/HumanJoint"
                        MD5    = "3c4fdc15a20513610de24bfcc00bda4d"
                        Fields = ["joint_id", UInt8Def
                                  "pose",     StructDef Geometry.Pose.Def.Fields] }

            type Kind = { JointID: uint8
                          Pose: Geometry.Pose.Kind}

            let ToMessage { JointID = jointId
                            Pose    = pose} = ["joint_id",  UInt8Val jointId
                                               "pose", StructVal ( Geometry.Pose.ToMessage pose |> Seq.toList)] |> Seq.ofList

        module HumanBody =
            let Def = { Type   = "tbd_ros_msgs/HumanBody"
                        MD5    = "849c8b609ada0f762d7b2a17b3db1ce6"
                        Fields = ["header", StructDef Standard.Header.Def.Fields
                                  "body_id", UInt32Def
                                  "joints", VariableArrayDef (StructDef HumanJoint.Def.Fields)]}

            type Kind = { Header: Standard.Header.Kind
                          BodyID: uint32
                          Joints: HumanJoint.Kind seq}

            let ToMessage { Header = header
                            BodyID = bodyID 
                            Joints = joints} = ["header", StructVal ( Standard.Header.ToMessage header |> Seq.toList)
                                                "bodyID", UInt32Val bodyID
                                                "joints", VariableArrayVal (Seq.map (HumanJoint.ToMessage >> Seq.toList >> StructVal)  joints |> Seq.toList)] |> Seq.ofList

        module HumanBodyArray =
            let Def = { Type   = "tbd_ros_msgs/HumanBodyArray"
                        MD5    = "9ad9e1f6103dbdcd3241feae8067a4bf"
                        Fields = ["bodies", VariableArrayDef (StructDef HumanBody.Def.Fields)]}

            type Kind = { Bodies : HumanBody.Kind seq}

            let ToMessage { Bodies = bodies} = ["bodies", VariableArrayVal (Seq.map (HumanBody.ToMessage >> Seq.toList >> StructVal)  bodies |> Seq.toList)] |> Seq.ofList