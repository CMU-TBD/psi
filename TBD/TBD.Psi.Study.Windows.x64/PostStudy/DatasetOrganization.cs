using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Study.PostStudy
{
    using Microsoft.Psi.Data;
    public class DatasetOrganization
    {
        public static void Run()
        {
            Dataset dataset = null;
            if (!System.IO.File.Exists(Constants.DatasetPath))
            {
                dataset = new Dataset("multi-robot-study");
                dataset.Save(Constants.DatasetPath);
            }
            else
            {
                dataset = Dataset.Load(Constants.DatasetPath);
            }
            // Now we add all the conditions
            dataset.AddSessionFromPsiStore("live-recording", @"E:\Study-Data\X01\recording\live-recording.0001", "X01.checkin");
            dataset.AddSessionFromPsiStore("live-recording", @"E:\Study-Data\X01\recording\live-recording.0002", "X01.delivery");
            dataset.AddSessionFromPsiStore("live-recording", @"E:\Study-Data\X01\recording\live-recording.0003", "X01.pickup");

            // save the dataset
            dataset.Save(Constants.DatasetPath);
        }

    }
}
