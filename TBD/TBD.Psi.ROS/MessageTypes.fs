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

    module tbd_audio_msgs = 

        module Utterance = 
            let Def = { Type   = "tbd_audio_msgs/Utterance"
                        MD5    = "6bfd764a5958bbe48136d8ac1b641f78"
                        Fields = ["header", StructDef Standard.Header.Def.Fields
                                  "text", StringDef
                                  "confidence", Float32Def
                                  "end_time", TimeDef
                                  "word_list", VariableArrayDef StringDef
                                  "timing_list", VariableArrayDef UInt16Def]}

            type Kind = { Header: Standard.Header.Kind
                          Text: string
                          Confidence: single
                          EndTime: DateTime
                          WordList: string seq
                          TimingList: uint16 seq}

            let FromMessage m = m |> Seq.toList |> function ["header",          StructVal header
                                                             "text",            StringVal text
                                                             "confidence",      Float32Val confidence
                                                             "end_time",        TimeVal (sec, nsec)
                                                             "word_list",       VariableArrayVal word_list
                                                             "timing_list",     VariableArrayVal timing_list] -> { Header        = Standard.Header.FromMessage header
                                                                                                                   Text          = text
                                                                                                                   Confidence    = confidence
                                                                                                                   EndTime       = toDateTime sec nsec
                                                                                                                   WordList      = List.map (function StringVal str -> str | _ -> malformed ()) word_list
                                                                                                                   TimingList      = List.map (function UInt16Val timing -> timing | _ -> malformed ()) timing_list } | _ -> malformed ()
