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
            RunRosMapCalibration.Run();

            // Post Processings
            // PostProcessCalibration.Run();
            // PostProcessDepthImageStaging.Run();
            // PostProcessBodyMergerTest.Run();
            // PostProcessBoardDetectionZipOnly.Run();

            // Replay of Lives Recordings 
            // ReplayLive.Run();

            // Sandbox.Run();
        }
    }
}
