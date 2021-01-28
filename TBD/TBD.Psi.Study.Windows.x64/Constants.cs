using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Study
{
    public class Constants
    {
        // General
        public static int JPEGEncodeQuality = 75;

        // Lab 2020-12-11
        /*        public static string OperatingDirectory = @"C:\Data\Lab-Store\2020-12-11";
                public static string CalibrationRecordingPath = @"calibration-recordings\calibration-recording.0006";
                public static string CalibrationCSVPath = @"C:\Data\Cal\result-lab.csv";
                public static string TestRecordingPath = @"recordings\record-pipeline.0014";
                public static string TransformationSettingsPath = @"C:\Users\Zhi\Desktop\Dev\transformation-lab-2.json";
                public static Dictionary<string, string> SensorCorrespondMap = new Dictionary<string, string>()
                        {
                            { "azure1", "topCam"},
                            { "azure2", "mainCam"},
                        };*/

        // Lab 2021-01-13
        public static string OperatingDirectory = @"E:\Data\Lab-Store\2021-01-13";
        public static string CalibrationRecordingPath = @"calibration-recordings\calibration-recording.0000";
        public static string CalibrationCSVPath = @"E:\Data\Cal\result-lab.csv";
        public static string TestRecordingPath = @"recordings\record-pipeline.0004";
        public static string TransformationSettingsPath = @"C:\Users\Zhi\Desktop\Dev\transformation-lab.json";
        public static Dictionary<string, string> SensorCorrespondMap = new Dictionary<string, string>()
                        {
                            { "azure1", "topCam"},
                            { "azure2", "mainCam"},
                            { "k2d1", "leftCam"},
                        };


        // Hallway 2020-11-18
        /*        public static string OperatingDirectory = @"E:\Data\Hallway-Store\2020-11-18";
                public static string CalibrationRecordingPath = @"calibration-recordings\calibration-recording.0005";
                public static string CalibrationCSVPath = @"E:\Data\Cal\result-hallway.csv";
                public static string TestRecordingPath = @"recordings\record-pipeline.0005";
                public static string TransformationSettingsPath = @"C:\Users\Zhi\Desktop\Dev\transformation-hallway.json";
                public static Dictionary<string, string> SensorCorrespondMap = new Dictionary<string, string>()
                        {
                            { "azure0", "mainCam"},
                            { "azure1", "rightTopCam"},
                            { "azure3", "leftCam"},
                        };*/

        // Run Live
        public static string LiveOperatingDirectory = @"C:\Data\Inbox-Store";
        public static string LiveSavePath = @"run-live";
        public static string LiveTransformationSettingsPath = @"C:\Users\Zhi\Desktop\Dev\transformation-hallway.json";
        public static Dictionary<string, string> LiveSensorCorrespondMap = new Dictionary<string, string>()
                {
                    { "azure0", "mainCam"},
                    { "azure1", "rightTopCam"},
                    { "azure3", "leftCam"},
                };
        

    }
}
