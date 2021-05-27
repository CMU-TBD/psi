using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Study
{
    using Microsoft.Azure.Kinect.Sensor;

    public class Constants
    {
        // General
        public static int JPEGEncodeQuality = 60;

        // recording constants
        public static string RecordMainDirectory = @"E:\Data\Lab-Store";
        public static string RecordFolderName = @"recordings";
        public static string RecordStoreName = @"recording";

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
                            { "azure3", "leftCam"},
                        };
        public static Dictionary<int, WiredSyncMode> SensorSyncMode = new Dictionary<int, WiredSyncMode>()
                        {
                            { 1, WiredSyncMode.Subordinate},
                            { 2, WiredSyncMode.Master},
                            { 3, WiredSyncMode.Standalone},
                        };

        public static string PartitionIdentifier = "2021-05-25";
        public static string TestRecordingPath = @"live-recordings\live-recording.0000";
        public static string OperatingStore = @"post-board\board-detection.0000";
        public static string OperatingStoreSubPath = @"phantom-body-test\videodata3";
        // public static string OperatingStoreSubPath = @"live-recordings\live-recording.0000";
        public static string OperatingStoreName = @"Cropped";


        public static string LocalRosCoreAddress = "127.0.0.1";
        public static string LocalRosClientAddress = "127.0.0.1";

        // Run Live
        public static string LiveOperatingDirectory = @"E:\Data\Lab-Store";
        public static string LiveFolderName = @"live-recordings";
        public static string LiveStoreName = @"live-recording";
        public static string RosCoreAddress = "192.168.0.201";
        public static string RosClientAddress = "192.168.0.157";

        // Replay Live
        public static string ReplayLiveStore = @"live-recording.0000";
    }
}
