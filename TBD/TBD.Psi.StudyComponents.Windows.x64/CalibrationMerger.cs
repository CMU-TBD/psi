﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.StudyComponents
{
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.AzureKinect;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;
    using TBD.Psi.OpenCV;

    public class CalibrationMerger : Subpipeline, IProducer<(CoordinateSystem, string, CoordinateSystem, string)>
    {
        private Pipeline pipeline;
        private Zip<(CoordinateSystem, string, CoordinateSystem, string)> zipper;
        private List<Emitter<CoordinateSystem>> boardPoseEmitters = new List<Emitter<CoordinateSystem>>();
        private List<string> InputNames = new List<string>();
        private Connector<(CoordinateSystem, string, CoordinateSystem, string)> outConnector;
        private int numX;
        private int numY;
        private int firstMarker;
        private double markerLength;
        private double markerSeperation;
        private DeliveryPolicy deliveryPolicy;
        private ArucoDictionary dictName;
        private TimeSpan timeRange = TimeSpan.FromMilliseconds(15);
        private Exporter store = null;
        private bool saveInputToStore = false;

        public CalibrationMerger(Pipeline p, Exporter store, int numX, int numY, double markerLength, double markerSeperation, ArucoDictionary dictName, DeliveryPolicy deliveryPolicy = null, int firstMarker = 0)
            : this(p, numX, numY, markerLength, markerSeperation, dictName, firstMarker)
        {
            this.store = store;
            this.saveInputToStore = true;
            this.deliveryPolicy = deliveryPolicy ?? DeliveryPolicy.Unlimited;
        }

        public CalibrationMerger(Pipeline p, int numX, int numY, double markerLength, double markerSeperation, ArucoDictionary dictName, int firstMarker = 0)
            : base(p)
        {
            this.numX = numX;
            this.numY = numY;
            this.markerLength = markerLength;
            this.markerSeperation = markerSeperation;
            this.firstMarker = firstMarker;
            this.dictName = dictName;
            this.pipeline = p;
            this.pipeline.PipelineRun += this.PipelineStartEvent;
            this.zipper = new Zip<(CoordinateSystem, string, CoordinateSystem, string)>(this);

            this.outConnector = this.CreateOutputConnectorTo<(CoordinateSystem, string, CoordinateSystem, string)>(p, nameof(this.outConnector));
            this.zipper.Select(m => m.First(), this.deliveryPolicy).PipeTo(this.outConnector, this.deliveryPolicy);
        }


        public Emitter<(CoordinateSystem, string, CoordinateSystem, string)> Out => this.outConnector.Out;

        public void AddSensor(AzureKinectSensor sensor, string name)
        {
            this.AddStreams(sensor.ColorImage, sensor.DepthDeviceCalibrationInfo, name);
        }

        public void AddSavedStreams(Emitter<Shared<Image>> imgInput, Emitter<IDepthDeviceCalibrationInfo> calInput, string name)
        {
            this.AddStreams(imgInput, calInput, $"{name}-{this.firstMarker}");
        }

        public void AddSensor(KinectSensor sensor, string name)
        {
            this.AddStreams(sensor.ColorImage, sensor.DepthDeviceCalibrationInfo, name);
        }

        private void AddStreams(Emitter<Shared<Image>> imgInput, Emitter<IDepthDeviceCalibrationInfo> calInput, string name)
        {
            var imgReceiver = this.CreateInputConnectorFrom<Shared<Image>>(this.pipeline, $"imgFrom{name}");
            var calReceiver = this.CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(this.pipeline, $"CalFrom{name}");
            imgInput.PipeTo(imgReceiver, this.deliveryPolicy);
            calInput.PipeTo(calReceiver);

            // create board detector
            var boardDetector = new BoardDetector(this, this.numX, this.numY, (float)this.markerLength, (float)this.markerSeperation, this.dictName, this.firstMarker);
            var greyImg = imgReceiver.ToGray(deliveryPolicy);
            greyImg.PipeTo(boardDetector.ImageIn, deliveryPolicy);
            calReceiver.PipeTo(boardDetector.CalibrationIn);
            if (this.saveInputToStore)
            {
                greyImg.EncodeJpeg(quality: 80, deliveryPolicy).Write($"{name}.img", this.store);
                boardDetector.Out.Write($"{name}.pose", this.store);
                boardDetector.DebugImageOut.EncodeJpeg().Write($"{name}.debug", this.store);
            }

            // save the outgoing emitter
            this.boardPoseEmitters.Add(boardDetector.Out);
            this.InputNames.Add(name);
        }

        public void AddStream(Emitter<Shared<Image>> imgInput, IDepthDeviceCalibrationInfo info, string name)
        {
            var imgReceiver = this.CreateInputConnectorFrom<Shared<Image>>(this.pipeline, $"imgFrom{name}");
            imgInput.PipeTo(imgReceiver);

            // create board detector
            var boardDetector = new BoardDetector(this, this.numX, this.numY, (float)this.markerLength, (float)this.markerSeperation, this.dictName);
            boardDetector.AddCalibrationInfo(info);
            var greyImg = imgReceiver.ToGray();
            greyImg.PipeTo(boardDetector.ImageIn);
            if (this.saveInputToStore)
            {
                greyImg.EncodeJpeg(quality: 80).Write($"{name}.img", this.store);
                boardDetector.Out.Write($"{name}.pose", this.store);
            }

            // save the outgoing emitter
            this.boardPoseEmitters.Add(boardDetector.Out);
            this.InputNames.Add(name);
        }

        private void PipelineStartEvent(object sender, PipelineRunEventArgs e)
        {
            for (var i = 0; i < this.boardPoseEmitters.Count; i++)
            {
                for (var j = i + 1; j < this.boardPoseEmitters.Count; j++)
                {
                    var name1 = this.InputNames[i];
                    var name2 = this.InputNames[j];
                    // join and send to merger
                    var joiner = this.boardPoseEmitters[i].Join(this.boardPoseEmitters[j], this.timeRange, this.deliveryPolicy, this.deliveryPolicy).Select(m =>
                    {
                        var (pose1, pose2) = m;
                        return (pose1, name1, pose2, name2);
                    }, this.deliveryPolicy);
                    var receiver = this.zipper.AddInput($"CalibrationMerger{i}-{j}");
                    joiner.PipeTo(receiver, this.deliveryPolicy);
                }
            }
        }
    }
}
