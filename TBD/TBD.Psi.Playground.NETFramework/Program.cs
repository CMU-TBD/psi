namespace TBD.Psi.Playground
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using Microsoft.Psi.Speech;
    using TBD.Psi.Playground.NETFramework;

    class Program
    {
        static void Main(string[] args)
        {
            //DoubleKinect.Run(args);
            ProgramDeepSpeech.Run();
            //OpenCVTest.Run(args);

            /*     using (var p = Pipeline.Create(true))
                 {
                     var store = Store.Create(p, "test", @"C:\Data\Stores");

                     var k4a1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                     {
                         OutputColor = true,
                         DeviceIndex = 1
                     });
                     var arucoDetector = new OpenCVArucoDetector(p);
                     k4a1.ColorImage.PipeTo(arucoDetector.ImageIn);
                     arucoDetector.IdsOut.Do(m =>
                     {
                         Console.WriteLine(m.Count);
                     });
                     k4a1.ColorImage.Write("img", store);
                     arucoDetector.IdsOut.Select(m => m.Count).Write("num", store);
                     p.RunAsync();
                     Console.ReadLine();
                 }*/
        }
    }
}
