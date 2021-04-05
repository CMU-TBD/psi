

// based on ModelVisual3DVisualizationObjectEnumerable.cs

namespace TBD.Psi.Visualization.Windows
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using System.Linq;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;
    using TBD.Psi.TransformationTree;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;

    [VisualizationObject("Visualize TransformationTree")]
    public class TransformationTreeVisualizationObject : ModelVisual3DVisualizationObjectCollectionBase<CoordinateSystemVisualizationObject, TransformationTree<string>>
    {
        private string baseFrame = "";
        // The collection of child visualization objects.
        private readonly List<CoordinateSystemVisualizationObject> children = new List<CoordinateSystemVisualizationObject>();
        private List<(string name, CoordinateSystem cs)> currentCoordinateSystems = new List<(string name, CoordinateSystem cs)>();

        public TransformationTreeVisualizationObject()
        {
            this.Items = this.children;
        }

        /// <summary>
        /// Gets or sets a value indicating the base frame of the world.
        /// </summary>
        [DataMember]
        [PropertyOrder(0)]
        [Description("Sets the base frame of the world.")]
        public string BaseFrame
        {
            get { return this.baseFrame; }
            set { this.Set(nameof(this.BaseFrame), ref this.baseFrame, value); }
        }

        public override void UpdateData()
        {
            if (this.CurrentData != null)
            {
                this.UpdateCoordinateSystemValues();
                this.UpdateCoordinateSystemVisuals();
            }
            else
            {
                // No data, remove everything
                this.RemoveAll();
            }
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.BaseFrame))
            {
                this.UpdateCoordinateSystemValues();
            }
        }

        private void UpdateCoordinateSystemValues()
        {
            if (this.CurrentData != null && this.CurrentData.Contains(this.baseFrame))
            {
                this.currentCoordinateSystems = this.CurrentData.TraverseTree(this.baseFrame, new CoordinateSystem());
            }
            else
            {
                // empty list
                this.currentCoordinateSystems = new List<(string name, CoordinateSystem cs)>();
            }
        }

        private void UpdateCoordinateSystemVisuals()
        {
            // now we use the same code as the list
            int index = 0;
            foreach ((string name, CoordinateSystem cs) in this.currentCoordinateSystems)
            {
                // If we don't have enough visualization objects, create a new one
                while (index >= this.ModelView.Children.Count)
                {
                    this.AddNew();
                }

                // Get the child visualization object to update itself
                this.children[index].SetCurrentValue(this.SynthesizeMessage(cs));

                index++;
            }

            // If we have more visualization objects than data, remove the extras
            while (index < this.ModelView.Children.Count)
            {
                this.Remove(index);
            }
        }

        private void AddNew()
        {
            // Create a new child TVisObj.  It will already be
            // initialized with all the properties of the prototype.
            CoordinateSystemVisualizationObject child = this.CreateNew();

            // Add it to the collection
            this.children.Add(child);

            // Ad the new visualization object's model view as a child of our model view
            this.ModelView.Children.Add(child.ModelView);
        }

        private void Remove(int index)
        {
            // Remove the visualization object's model view from our model view
            this.ModelView.Children.RemoveAt(index);

            // remove the visualization object
            this.children.RemoveAt(index);
        }

        private void RemoveAll()
        {
            this.ModelView.Children.Clear();
            this.children.Clear();
        }
    }
}
