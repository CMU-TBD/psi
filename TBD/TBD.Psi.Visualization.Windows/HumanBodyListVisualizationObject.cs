﻿
namespace TBD.Psi.Visualization.Windows
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using TBD.Psi.VisionComponents;

    [VisualizationObject("Human Bodies")]
    public class HumanBodyListVisualizationObject : ModelVisual3DVisualizationObjectEnumerable<HumanBodyVisualizationObject, HumanBody, List<HumanBody>>
    {
    }
}
