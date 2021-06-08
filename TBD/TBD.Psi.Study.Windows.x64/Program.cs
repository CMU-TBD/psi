namespace TBD.Psi.Study
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {

            // OutputApp.Run();

            // For Running Live
            Study.RunStudy.Run();
            // RunLive.Run();
            // RunRosMapCalibration.Run();

            // Replay of Lives Recordings 
            // ReplayLive.Run();

            // Post Study
            // PostStudy.DatasetOrganization.Run();
            // PostStudy.DeriveVisualize.Run();
            // PostStudy.Derive2D.Run();
            // PostStudy.Derive2DRecenter.Run();
            // PostStudy.DeriveAnalysisSetup.Run();

            // Calibration Stack
            // Calibration.RecordCalibrationStream.Run();
            // Calibration.DeriveBoardEstimations.Run();
            // Calibration.DeriveDepthCheck.Run();

        }
    }
}
