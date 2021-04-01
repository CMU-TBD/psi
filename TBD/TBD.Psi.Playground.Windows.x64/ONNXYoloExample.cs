using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Onnx;

namespace TBD.Psi.Playground.Windows.x64
{
    public class ONNXYoloExample
    {
        public static void Run()
        {
            using (var p = Pipeline.Create())
            {
                var store = PsiStore.Create(p, "yolo-examples", @"E:\Data\playground");
                // create YOLO
                var yolo = new TinyYoloV2OnnxModelRunner(p, @"E:\ONNX Models\tinyyolov2-8.onnx");

                yolo.Select(m =>
                {
                    var rectangleList = new List<Rectangle>();
                    foreach (var detection in m)
                    {
                        rectangleList.Add(Rectangle.Round(detection.BoundingBox));
                    }
                    return rectangleList;
                }).Write("detection", store);
                p.RunAsync();
                Console.ReadLine();

            }
        }
    }
}
