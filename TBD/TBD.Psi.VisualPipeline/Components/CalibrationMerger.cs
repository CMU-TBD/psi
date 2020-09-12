using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.VisualPipeline.Components
{
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.AzureKinect;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;

    public class CalibrationMerger : Subpipeline, IProducer<(CoordinateSystem, string, CoordinateSystem, string)>
    {
        private Pipeline pipeline;
        private Merge<(CoordinateSystem, string, CoordinateSystem, string)> merger;
        private List<Emitter<CoordinateSystem>> boardPoseEmitters = new List<Emitter<CoordinateSystem>>();
        private List<string> InputNames = new List<string>();
        private Connector<(CoordinateSystem, string, CoordinateSystem, string)> outConnector;
        private int numX;
        private int numY;
        private double markerLength;
        private double markerSeperation;
        private string dictName;
        private TimeSpan timeRange = TimeSpan.FromMilliseconds(50);
        private PsiExporter store;
        private bool saveInputToStore = true;

        public CalibrationMerger(Pipeline p, PsiExporter store, int numX, int numY, double markerLength, double markerSeperation, string dictName)
            : base(p)
        {
            this.store = store;
            this.numX = numX;
            this.numY = numY;
            this.markerLength = markerLength;
            this.markerSeperation = markerSeperation;
            this.dictName = dictName;
            this.pipeline = p;
            this.pipeline.PipelineRun += this.PipelineStartEvent;
            this.merger = new Merge<(CoordinateSystem, string, CoordinateSystem, string)>(this);
            this.outConnector = this.CreateOutputConnectorTo<(CoordinateSystem, string, CoordinateSystem, string)>(p, nameof(this.outConnector));
            this.merger.Select(m => m.Data).PipeTo(this.outConnector);
        }

        public Emitter<(CoordinateSystem, string, CoordinateSystem, string)> Out => this.outConnector.Out;

        public void AddSensor(AzureKinectSensor sensor, string name)
        {
            this.AddStreams(sensor.ColorImage, sensor.DepthDeviceCalibrationInfo, name);
        }

        public void AddSavedStreams(Emitter<Shared<Image>> imgInput, Emitter<IDepthDeviceCalibrationInfo> calInput, string name)
        {
            this.AddStreams(imgInput, calInput, name);
        }

        public void AddSensor(KinectSensor sensor, string name)
        {
            this.AddStreams(sensor.ColorImage, sensor.DepthDeviceCalibrationInfo, name);
        }

        private void AddStreams(Emitter<Shared<Image>> imgInput, Emitter<IDepthDeviceCalibrationInfo> calInput, string name)
        {
            var imgReceiver = this.CreateInputConnectorFrom<Shared<Image>>(this.pipeline, $"imgFrom{name}");
            var calReceiver = this.CreateInputConnectorFrom<IDepthDeviceCalibrationInfo>(this.pipeline, $"CalFrom{name}");
            imgInput.PipeTo(imgReceiver);
            calInput.PipeTo(calReceiver);

            // create board detector
            var boardDetector = new BoardDetector(this, this.numX, this.numY, (float)this.markerLength, (float)this.markerSeperation, this.dictName);
            var greyImg = imgReceiver.ToGray();
            greyImg.PipeTo(boardDetector.ImageIn);
            calReceiver.PipeTo(boardDetector.CalibrationIn);
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
                    var joiner = this.boardPoseEmitters[i].Join(this.boardPoseEmitters[j], this.timeRange).Select(m =>
                    {
                        var (pose1, pose2) = m;
                        return (pose1, name1, pose2, name2);
                    });

                    var receiver = this.merger.AddInput($"CalibrationMerger{i}-{j}");
                    joiner.PipeTo(receiver);
                }
            }
        }
    }
}
