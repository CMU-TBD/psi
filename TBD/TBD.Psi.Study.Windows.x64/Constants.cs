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
        public static int JPEGEncodeQuality = 60;

        // recording constants
        public static string RecordMainDirectory = @"E:\Data\Lab-Store";
        public static string RecordFolderName = @"recordings";
        public static string RecordStoreName = @"recording";

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
        /*        public static string OperatingDirectory = @"E:\Data\Lab-Store\2021-01-13";
                public static string CalibrationRecordingPath = @"calibration-recordings\calibration-recording.0000";
                public static string CalibrationCSVPath = @"E:\Data\Cal\result-lab.csv";
                public static string TestRecordingPath = @"recordings\record-pipeline.0004";
                public static string TransformationSettingsPath = @"C:\Users\Zhi\Desktop\Dev\transformation-lab.json";
                public static Dictionary<string, string> SensorCorrespondMap = new Dictionary<string, string>()
                                {
                                    { "azure1", "topCam"},
                                    { "azure2", "mainCam"},
                                    { "k2d1", "leftCam"},
                                };*/

        // Lab 2021-01-27
        /*       public static string OperatingDirectory = @"E:\Data\Lab-Store\2021-01-27";
               public static string CalibrationRecordingPath = @"calibration-recordings\calibration-recording.0003";
               public static string CalibrationCSVPath = @"E:\Data\Cal\result-lab-2021-01-27.csv";
               public static string TestRecordingPath = @"recordings\recording.0001";
               public static string TransformationSettingsPath = @"C:\Users\Zhi\Desktop\Dev\transformation-lab-2021-01-27.json";
               public static Dictionary<string, string> SensorCorrespondMap = new Dictionary<string, string>()
                               {
                                   { "azure1", "topCam"},
                                   { "azure2", "mainCam"},
                                   { "azure3", "midCam"},
                                   { "k2d1", "leftCam"},
                               };*/

        // Lab 2021-02-10
        /*        public static string OperatingDirectory = @"E:\Data\Lab-Store\2021-02-10";
                public static string CalibrationRecordingPath = @"calibration-recordings\recording.0002";
                public static string CalibrationStoreName = "recording";
                public static string CalibrationCSVPath = @"E:\Data\Cal\result-lab-2021-02-10.csv";
                public static string TestRecordingPath = @"recordings\recording.0001";
                public static string TransformationSettingsPath = @"C:\Users\Zhi\Desktop\Dev\transformation-lab-2021-02-10.json";
                public static Dictionary<string, string> SensorCorrespondMap = new Dictionary<string, string>()
                                {
                                    { "azure1", "topCam"},
                                    { "azure2", "mainCam"},
                                    { "azure3", "midCam"},
                                    { "k2d1", "leftCam"},
                                };*/

        // Combine stats
        public static string StudyType = "lab";
        public static string OperatingDirectory = @"E:\Data\Lab-Store\";
        public static string ResourceLocations = @"E:\Data\Resources\";
        public static string TransformationSettingsDirectory = @"C:\Users\Zhi\Desktop\Dev\";
        public static string CalibrationSubDirectory = "calibration-recording";
        public static string CalibrationStoreName = "recording";
        public static Dictionary<string, string> SensorCorrespondMap = new Dictionary<string, string>()
                        {
                            { "azure1", "topCam"},
                            { "azure2", "mainCam"},
                            { "azure3", "midCam"},
                            { "k2d1", "leftCam"},
                        };

        // Lab 2021-02-17
        /* public static string PartitionIdentifier = "2021-02-17";
         public static string TestRecordingPath = @"recordings\recording.0001";
         public static string TransformationSettingsPath = @"C:\Users\Zhi\Desktop\Dev\transformation-lab-2021-02-10.json";*/


        // Lab 2021-02-4 
        // public static string PartitionIdentifier = "2021-02-24";
        // public static string TestRecordingPath = @"live-recordings\live-recording.0000";

        // Lab 2021-03-03
        /*        public static string PartitionIdentifier = "2021-03-03";
                public static string TestRecordingPath = @"live-recordings\live-recording.0000";*/

        // Lab 2021-03-18
        public static string PartitionIdentifier = "2021-03-18";
        public static string TestRecordingPath = @"live-recordings\live-recording.0000";
        public static string ReplayRecordingPath = @"live-recordings\live-recording.0000";
        public static string OperatingStore = @"post-board\board-detection.0002";

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
        public static string LiveOperatingDirectory = @"E:\Data\Lab-Store";
        public static string LiveFolderName = @"live-recordings";
        public static string LiveStoreName = @"live-recording";
        public static string RosCoreAddress = "192.168.0.201";
        public static string RosClientAddress = "192.168.0.3";
    }
}
