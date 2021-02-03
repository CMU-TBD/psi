namespace TBD.Psi.Study
{
    using System;
    class Program
    {
        static void Main(string[] args)
        {
            // For recording in post-processing
            // RecordTestStreams.Run();

            // For Running Live
            // RunLive.Run();

            // Post Processing
            // PostProcessBoardDetection.Run();
            // PostProcessDepthImageStaging.Run();
            PostProcessBodyMergerTest.Run();
        }
    }
}
