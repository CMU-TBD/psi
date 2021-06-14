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
        // settings
        public static int JPEGEncodeQuality = 60;
        public static string RosCoreAddress = "192.168.0.201";
        public static string RosClientAddress = "192.168.0.157";
        public static string RosBridgeServerAddress = "ws://192.168.0.152:9090";
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

        // Dataset path & identifier

        public static string ResourceLocations = @"E:\Data\Resources\";
        public static string RootPath = @"E:\Study-Data";
        public static string StudyType = "lab";
        public static string PartitionIdentifier = $"{DateTime.Today:yyyy-MM-dd}";
        // Derived
        public static string CalibrationDatasetIdentifier = $"{PartitionIdentifier}-calibration";

        // post study dataset path 
        public static string ParticipantToAnalyze = "P1";

    }
}
