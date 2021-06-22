using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBD.Psi.StudyComponents;

namespace TBD.Psi.Study
{
    public static class OutputApp
    {
        public static void Run()
        {
            var transformationSettingPath = Path.Combine(Constants.SettingsLocation, $"transformations-{Constants.StudyType}-{Constants.PartitionIdentifier}.json");
            var tree = TransformationTree.TransformationTreeJSONParser.ParseJSONFile(transformationSettingPath);
            var cs = tree.QueryTransformation("world", "baxterBase");
            var q = Utils.GetQuaternionFromCoordinateSystem(cs);
            Console.WriteLine($"position - X:{cs.Origin.X} Y:{cs.Origin.Y} Z:{cs.Origin.Z}");
            Console.WriteLine($"rotation - X:{q.X} Y:{q.Y} Z:{q.Z} W:{q.W}");
        }
    }
}
