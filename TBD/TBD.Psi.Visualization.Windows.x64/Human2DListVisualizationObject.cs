
namespace TBD.Psi.Visualization.Windows
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using TBD.Psi.StudyComponents;

    [VisualizationObject("Human Bodies 2D")]
    public class Human2DListVisualizationObject : ModelVisual3DVisualizationObjectEnumerable<Human2DVisualizationObject, (uint, double[]), List<(uint, double[])>>
    {
    }
}
