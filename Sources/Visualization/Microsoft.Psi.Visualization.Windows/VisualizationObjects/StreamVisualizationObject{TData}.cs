﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Provides an abstract base class for stream visualization objects.
    /// </summary>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    public abstract class StreamVisualizationObject<TData> : VisualizationObject, IStreamVisualizationObject
    {
        /// <summary>
        /// The stream being visualized.
        /// </summary>
        private StreamBinding streamBinding;

        /// <summary>
        /// The source for the stream data, or null if the visualization object is not currently bound to a source.
        /// </summary>
        private StreamSource streamSource = null;

        /// <summary>
        /// The positive magnitude of the cursor epsilon (this value can be modified in the properties window).
        /// </summary>
        private int cursorEpsilonPosMs;

        /// <summary>
        /// The negative magnitude of the cursor epsilon (this value can be modified in the properties window).
        /// </summary>
        private int cursorEpsilonNegMs;

        /// <summary>
        /// Gets or sets the epsilon interval around the cursor used when reading data.
        /// </summary>
        private RelativeTimeInterval cursorEpsilon;

        /// <summary>
        /// The current (based on navigation cursor) value of the stream.
        /// </summary>
        private Message<TData>? currentValue;

        /// <summary>
        /// The snap to stream command.
        /// </summary>
        private RelayCommand toggleSnapToStreamCommand;

        /// <summary>
        /// The zoom to stream command.
        /// </summary>
        private RelayCommand zoomToStreamCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamVisualizationObject{TData}"/> class.
        /// </summary>
        public StreamVisualizationObject()
        {
            this.CursorEpsilonPosMs = 0;
            this.CursorEpsilonNegMs = 500;
        }

        /// <summary>
        /// Gets or sets the radius of the cursor epsilon. (This value is exposed in the Properties UI).
        /// </summary>
        [DataMember]
        [DisplayName("Cursor Epsilon Future (ms)")]
        [Description("The epsilon future duration relative to the cursor (in milliseconds) to consider when finding messages to visualize.")]
        public int CursorEpsilonPosMs
        {
            get { return this.cursorEpsilonPosMs; }

            set
            {
                this.cursorEpsilonPosMs = value;
                this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.cursorEpsilonNegMs), TimeSpan.FromMilliseconds(this.cursorEpsilonPosMs));
            }
        }

        /// <summary>
        /// Gets or sets the radius of the cursor epsilon. (This value is exposed in the Properties UI).
        /// </summary>
        [DataMember]
        [DisplayName("Cursor Epsilon Past (ms)")]
        [Description("The epsilon past duration relative to the cursor (in milliseconds) to consider when finding messages to visualize.")]
        public int CursorEpsilonNegMs
        {
            get { return this.cursorEpsilonNegMs; }

            set
            {
                this.cursorEpsilonNegMs = value;
                this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.cursorEpsilonNegMs), TimeSpan.FromMilliseconds(this.cursorEpsilonPosMs));
            }
        }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        [Browsable(true)]
        [DisplayName("Stream Name")]
        [IgnoreDataMember]
        public string StreamName => this.StreamBinding?.StreamName;

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        [Browsable(true)]
        [DisplayName("Partition Name")]
        [IgnoreDataMember]
        public string PartitionName => this.StreamBinding?.PartitionName;

        /// <summary>
        /// Gets the cursor epsilon.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelativeTimeInterval CursorEpsilon
        {
            get => this.cursorEpsilon;
            private set
            {
                this.Set(nameof(this.CursorEpsilon), ref this.cursorEpsilon, value);
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool CanSnapToStream => this.IsBound;

        /// <summary>
        /// Gets the snap to stream command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleSnapToStreamCommand
        {
            get
            {
                if (this.toggleSnapToStreamCommand == null)
                {
                    this.toggleSnapToStreamCommand = new RelayCommand(() => this.ToggleSnapToStream());
                }

                return this.toggleSnapToStreamCommand;
            }
        }

        /// <summary>
        /// Gets the zoom to stream command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToStreamCommand
        {
            get
            {
                if (this.zoomToStreamCommand == null)
                {
                    this.zoomToStreamCommand = new RelayCommand(
                        () => this.Container.Navigator.Zoom(this.StreamSource.StreamMetadata.FirstMessageOriginatingTime, this.StreamSource.StreamMetadata.LastMessageOriginatingTime),
                        () => this.StreamSource != null);
                }

                return this.zoomToStreamCommand;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the visualization object has a current value.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool HasCurrentValue => this.currentValue.HasValue;

        /// <summary>
        /// Gets the current value.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Message<TData>? CurrentValue => this.currentValue;

        /// <summary>
        /// Gets the current data.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public TData CurrentData => this.currentValue.HasValue ? this.currentValue.Value.Data : default;

        /// <summary>
        /// Gets the originating time of the current data.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public DateTime CurrentOriginatingTime => this.currentValue.HasValue ? this.currentValue.Value.OriginatingTime : default;

        /// <summary>
        /// Gets or sets the stream binding.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public StreamBinding StreamBinding
        {
            get { return this.streamBinding; }
            set { this.Set(nameof(this.StreamBinding), ref this.streamBinding, value); }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public StreamSource StreamSource
        {
            get => this.streamSource;

            private set
            {
                this.RaisePropertyChanging(nameof(this.StreamSource));
                this.RaisePropertyChanging(nameof(this.IconSource));
                this.streamSource = value;
                this.RaisePropertyChanged(nameof(this.StreamSource));
                this.RaisePropertyChanged(nameof(this.IconSource));
            }
        }

        /// <summary>
        /// Gets the adapter type.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Type StreamAdapterType => this.StreamBinding?.StreamAdapterType;

        /// <summary>
        /// Gets the adapter type name (used by property browser).
        /// </summary>
        [Browsable(true)]
        [DisplayName("Stream Adapter")]
        [Description("The stream adapter used by the visualizer.")]
        [IgnoreDataMember]
        public string StreamAdapterDisplayName => TypeSpec.Simplify(this.StreamBinding?.StreamAdapterType?.FullName);

        /// <summary>
        /// Gets the summarizer type.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Type SummarizerType => this.StreamBinding?.SummarizerType;

        /// <summary>
        /// Gets the summarizer type name (used by property browser).
        /// </summary>
        [Browsable(true)]
        [DisplayName("Summarizer")]
        [IgnoreDataMember]
        public string SummarizerTypeDisplayName => TypeSpec.Simplify(this.StreamBinding?.SummarizerType?.FullName);

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool RequiresSupplementalMetadata => false;

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsBound => this.StreamSource != null;

        /// <inheritdoc/>
        public override bool ShowZoomToStreamMenuItem => this.IsBound;

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public TimeInterval StreamExtents => this.IsBound ? new TimeInterval(this.StreamSource.StreamMetadata.FirstMessageOriginatingTime, this.StreamSource.StreamMetadata.LastMessageOriginatingTime) : TimeInterval.Empty;

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override string IconSource
        {
            get
            {
                if (!this.IsBound)
                {
                    if (this.StreamBinding.IsDerived)
                    {
                        // Stream member unbound
                        return IconSourcePath.StreamMemberUnbound;
                    }
                    else
                    {
                        // Stream unbound
                        return IconSourcePath.StreamUnbound;
                    }
                }
                else if (this.IsSnappedToStream)
                {
                    if (this.StreamBinding.IsDerived)
                    {
                        // Snap to stream member
                        return this.IsLive ? IconSourcePath.StreamMemberSnapLive : IconSourcePath.StreamMemberSnap;
                    }
                    else
                    {
                        // Snap to stream
                        return this.IsLive ? IconSourcePath.SnapToStreamLive : IconSourcePath.SnapToStream;
                    }
                }
                else if (this.StreamBinding.IsDerived)
                {
                    // Stream member
                    return this.IsLive ? IconSourcePath.StreamMemberLive : IconSourcePath.StreamMember;
                }
                else
                {
                    // Stream
                    return this.IsLive ? IconSourcePath.StreamLive : IconSourcePath.Stream;
                }
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool IsSnappedToStream => this.Container?.SnapToVisualizationObject == this;

        /// <summary>
        /// Gets the text to display in the snap to stream menu item.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public string ToggleSnapToStreamCommandText => this.IsSnappedToStream ? "Unsnap From Stream" : "Snap To Stream";

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsLive => this.StreamSource != null && this.StreamSource.IsLive;

        /// <summary>
        /// Gets the allocator to use when reading data.
        /// </summary>
        protected virtual Func<TData> Allocator { get; } = null;

        /// <summary>
        /// Gets the deallocator to use when reading data.
        /// </summary>
        protected virtual Action<TData> Deallocator { get; } =
            data =>
            {
                if (data is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };

        /// <summary>
        /// Sets the current value for the visualization object.
        /// </summary>
        /// <param name="value">The value to use as the new current value.</param>
        public void SetCurrentValue(Message<TData>? value)
        {
            if (this.currentValue != value)
            {
                this.RaisePropertyChanging(nameof(this.CurrentValue));
                this.RaisePropertyChanging(nameof(this.CurrentData));
                this.RaisePropertyChanging(nameof(this.CurrentOriginatingTime));

                // Deallocate the current value
                if (this.currentValue.HasValue)
                {
                    this.Deallocator?.Invoke(this.currentValue.Value.Data);
                }

                // If we have an incoming value
                if (value.HasValue)
                {
                    // Create a copy using the allocator
                    var data = this.Allocator != null ? this.Allocator() : default;
                    value.Value.Data.DeepClone(ref data);
                    this.currentValue = new Message<TData>(data, value.Value.OriginatingTime, value.Value.CreationTime, 0, 0);
                }
                else
                {
                    this.currentValue = null;
                }

                this.RaisePropertyChanged(nameof(this.CurrentValue));
                this.RaisePropertyChanged(nameof(this.CurrentData));
                this.RaisePropertyChanged(nameof(this.CurrentOriginatingTime));
            }
        }

        /// <inheritdoc/>
        public override void ToggleSnapToStream()
        {
            this.RaisePropertyChanging(nameof(this.IsSnappedToStream));
            this.RaisePropertyChanging(nameof(this.IconSource));
            this.RaisePropertyChanging(nameof(this.ToggleSnapToStreamCommandText));

            // If this is already the visualization object being snapped to, then
            // reset snap to stream, otherwise set it to this visualization object.
            // If another object was previously snapped, then ask it to unsnap itself
            // so that the correct property changed event gets raised.
            if (this.Container.SnapToVisualizationObject == null)
            {
                this.Container.SnapToVisualizationObject = this;
            }
            else if (this.Container.SnapToVisualizationObject == this)
            {
                this.Container.SnapToVisualizationObject = null;
            }
            else
            {
                this.Container.SnapToVisualizationObject.ToggleSnapToStream();
                this.Container.SnapToVisualizationObject = this;
            }

            this.RaisePropertyChanged(nameof(this.IsSnappedToStream));
            this.RaisePropertyChanged(nameof(this.IconSource));
            this.RaisePropertyChanged(nameof(this.ToggleSnapToStreamCommandText));
        }

        /// <inheritdoc />
        public void UpdateStreamSource(SessionViewModel sessionViewModel)
        {
            // Attempt to rebind to a source
            var newStreamSource = sessionViewModel?.CreateStreamSource(
                this.streamBinding,
                () => this.Allocator != null ? this.Allocator() : default,
                t => this.Deallocator?.Invoke(t));

            // If we're still bound to the same source, then we're done.
            if (this.StreamSource == newStreamSource)
            {
                return;
            }

            // Unhook event handlers from current stream source
            if (this.StreamSource != null)
            {
                this.StreamSource.PropertyChanging -= this.StreamSource_PropertyChanging;
                this.StreamSource.PropertyChanged -= this.StreamSource_PropertyChanged;
            }

            // Hook event handlers to new stream source
            if (newStreamSource != null)
            {
                newStreamSource.PropertyChanging += this.StreamSource_PropertyChanging;
                newStreamSource.PropertyChanged += this.StreamSource_PropertyChanged;
            }

            this.RaisePropertyChanging(nameof(this.IconSource));
            this.RaisePropertyChanging(nameof(this.CanSnapToStream));

            // Notify if we're becoming unbound from a stream source
            if (this.StreamSource != null)
            {
                this.StreamSource = null;
                this.OnStreamUnbound();
            }

            // Notify if we're now bound to a new stream source
            if (newStreamSource != null)
            {
                this.StreamSource = newStreamSource;
                this.OnStreamBound();
            }

            this.RaisePropertyChanged(nameof(this.IconSource));
            this.RaisePropertyChanged(nameof(this.CanSnapToStream));
        }

        /// <summary>
        /// Finds the index that is either exactly at currentTime, or closest to currentTime.
        /// Uses binary search to find exact match or location where match should be.
        /// </summary>
        /// <param name="currentTime">Time to search for.</param>
        /// <param name="count">Number of entries to search within.</param>
        /// <param name="timeAtIndex">Function that returns an index given a time.</param>
        /// <returns>Best matching index or -1 if no qualifying match was found.</returns>
        protected virtual int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex)
        {
            return IndexHelper.GetIndexForTime(currentTime, count, timeAtIndex);
        }

        /// <inheritdoc/>
        protected override void OnAddToPanel()
        {
            // Listen for stream read errors
            DataManager.Instance.StreamReadError += this.OnStreamReadError;

            base.OnAddToPanel();
        }

        /// <inheritdoc />
        protected override void OnRemoveFromPanel()
        {
            // Stop listening for stream read errors
            DataManager.Instance.StreamReadError -= this.OnStreamReadError;

            // Unbind the visualization object from any source
            this.UpdateStreamSource(null);

            base.OnRemoveFromPanel();
        }

        /// <summary>
        /// Called when the stream visualization object is bound to a data source.
        /// </summary>
        protected virtual void OnStreamBound()
        {
        }

        /// <summary>
        /// Called when the stream visualization object is unbound from a data source.
        /// </summary>
        protected virtual void OnStreamUnbound()
        {
            // If this is the stream currently being snapped to, disable snap to stream.
            if (this.IsSnappedToStream)
            {
                this.ToggleSnapToStream();
            }
        }

        private void StreamSource_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(this.StreamSource.IsLive))
            {
                this.RaisePropertyChanging(nameof(this.IconSource));
            }
        }

        private void StreamSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.StreamSource.IsLive))
            {
                this.RaisePropertyChanged(nameof(this.IconSource));
            }
        }

        private void OnStreamReadError(object sender, StreamReadErrorEventArgs e)
        {
            // Check if the error is related to the stream that this visualization object currently references
            if ((this.StreamSource != null) &&
                (this.StreamSource.StoreName == e.StoreName) &&
                (this.StreamSource.StorePath == e.StorePath) &&
                (this.StreamSource.StreamName == e.StreamName))
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Mark this visualization object as unbound since we can't read any messages.
                    this.UpdateStreamSource(null);

                    // Display an error message to the user.
                    string errorMessage = $"The format of the messages in the stream {e.StreamName} in store {e.StoreName} have changed and are unable to be deserialized. PsiStudio will no longer attempt to read from this stream. See detailed error information below:{Environment.NewLine}{Environment.NewLine}{e.Exception.Message}";
                    new MessageBoxWindow(Application.Current.MainWindow, "Stream Type Mismatch", errorMessage, "Close", null).ShowDialog();
                }));
            }
        }
    }
}
