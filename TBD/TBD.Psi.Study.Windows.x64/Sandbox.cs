
namespace TBD.Psi.Study
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;

    public class Sandbox
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                /*                // create input & output stores
                                var inputStore = PsiStore.Open(p, Constants.CalibrationStoreName, Path.Combine(Constants.OperatingDirectory, Constants.CalibrationSubDirectory));

                                // open an image stream
                                var color = inputStore.OpenStream<Shared<EncodedImage>>("azure1.color");
                                var index = 0;
                                color.Decode().ToGray().EncodeJpeg().Do(m =>
                                   {
                                        EncodedImage obj =  m.Resource;
                                        var stream = obj.ToStream();
                                        using (var sw = File.OpenWrite(@$"E:\Temp\img-{index++}.jpeg"))
                                        {
                                           stream.CopyTo(sw);
                                        }
                                   });

                                p.Run(ReplayDescriptor.ReplayAll);*/

                var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration(),
                });
                k4a1.Bodies.Do(m => Console.WriteLine("hello"));
                p.RunAsync();
                Console.ReadLine();
            }
        }
    }
}
