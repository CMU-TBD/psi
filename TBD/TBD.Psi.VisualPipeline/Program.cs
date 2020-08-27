// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.VisualPipeline
{
    /// <summary>
    /// Main Program Entry.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main Program Entry.
        /// </summary>
        /// <param name="args">System Arguments.</param>
        public static void Main(string[] args)
        {
            RecordingPipeline.Run();
            // CollectionPipeline.Run();
            // CalibrationTool.Run();
            // ReplayPipeline.Run("test", @"C:\Data\Playback\Example2");
        }
    }
}
