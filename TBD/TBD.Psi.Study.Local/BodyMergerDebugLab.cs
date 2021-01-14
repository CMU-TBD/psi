namespace TBD.Psi.Study.Local
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;

    public class BodyMergerDebugLab
    {
        public static void Run()
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var input = PsiStore.Open(p, "calibration-recording", @"C:\Data\Broken\broken-recording");
                var calibration = input.OpenStream<IDepthDeviceCalibrationInfo>("k2d1.depth-calibration");
                //var calibration2 = input.OpenStream<IDepthDeviceCalibrationInfo>("azure1.depth-calibration");
                calibration.Do((e,m) => Console.WriteLine("Hello"));
                //calibration2.Do(m => Console.WriteLine("Hello222"));
                p.Run();
            }
        }
    }
}
