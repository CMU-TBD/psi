namespace TBD.Psi.Study
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {

            //OutputApp.Run();

            // For Running Live
            // Study.RunStudy.Run();
            // RunRosMapCalibration.Run();

            // Replay of Lives Recordings 
            // Replay.ReplayLive.Run();

            // Post Study
            // PostStudy.DatasetOrganization.Run();
            // PostStudy.DeriveVisualize.Run();
            // PostStudy.Derive2D.Run();

            PostStudy.ReevaluateTracking.Run();
            // PostStudy.DeriveStudyInfo.Run();
            // PostStudy.Derive2DRecenter.Run();
            // PostStudy.DeriveAnalysisSetup.Run();

            // Calibration Stack
            // Calibration.RecordCalibrationStream.Run();
            // Calibration.DeriveCharucoEstimations.Run();
            // Calibration.DeriveBoardEstimations.Run();
            // Calibration.DeriveDepthCheck.Run();
        }
    }
}
