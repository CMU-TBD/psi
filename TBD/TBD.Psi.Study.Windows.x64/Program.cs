namespace TBD.Psi.Study
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            // For recording in post-processing
            // RecordTestStreams.Run();
            // RecordCalibrationStream.Run();

            // For Running Live
            // RunLive.Run();
            // RunRosMapCalibration.Run();

            // Post Processings
            // PostProcessCalibration.Run();
            // PostProcessDepthImageStaging.Run();
            // PostProcessBodyMergerTest.Run();
            // PostProcessBoardDetectionZipOnly.Run();

            // Replay of Lives Recordings 
            // ReplayLive.Run();

            // Sandbox.Run();

            // Post Study
            // PostStudy.DatasetOrganization.Run();
            // PostStudy.DeriveVisualize.Run();
            //PostStudy.Derive2D.Run();
            PostStudy.Derive2DRecenter.Run();

            // Calibration Stack
            // Calibration.RecordCalibrationStream.Run();
            // Calibration.DeriveBoardEstimations.Run();
            // Calibration.DeriveDepthCheck.Run();

        }
    }
}
