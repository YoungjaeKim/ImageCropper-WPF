using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageCropper
{
    /// <summary>
    /// Provides a Canvas where a rectangle will be drawn
    /// that matches the selection area that the user drew
    /// on the canvas using the mouse
    /// </summary>
    public partial class SelectionCanvas : Canvas
    {
        #region Instance fields
        private Point mouseLeftDownPoint;
        private Style cropperStyle;
        public Shape rubberBand = null;
        public readonly RoutedEvent CropImageEvent;
        #endregion

        #region Events
        /// <summary>
        /// Raised when the user has drawn a selection area
        /// </summary>
        public event RoutedEventHandler CropImage
        {
            add { AddHandler(this.CropImageEvent, value); }
            remove { RemoveHandler(this.CropImageEvent, value); }
        }
        #endregion

        #region Ctor
        /// <summary>
        /// Constructs a new SelectionCanvas, and registers the 
        /// CropImage event
        /// </summary>
        public SelectionCanvas()
        {
            this.CropImageEvent = EventManager.RegisterRoutedEvent("CropImage",
                RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectionCanvas));
        }
        #endregion

        #region Public Properties
        public Style  CropperStyle
        {
            get { return cropperStyle; }
            set { cropperStyle = value; }
        }
        #endregion

        #region Overrides

        /// <summary>
        /// Captures the mouse
        /// </summary>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!this.IsMouseCaptured)
            {
                mouseLeftDownPoint = e.GetPosition(this);
                this.CaptureMouse();
            }
        }

        /// <summary>
        /// Releases the mouse, and raises the CropImage Event
        /// </summary>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (this.IsMouseCaptured && rubberBand != null)
            {
                this.ReleaseMouseCapture();

                RaiseEvent(new RoutedEventArgs(this.CropImageEvent, this));
            }
        }

        /// <summary>
        /// Creates a child control <see cref="System.Windows.Shapes.Rectangle">Rectangle</see>
        /// and adds it to this conrtols children collection at the co-ordinates the user
        /// drew with the mouse
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (this.IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(this);

                if (rubberBand == null)
                {
                    rubberBand = new Rectangle();
                    if (cropperStyle != null)
                        rubberBand.Style = cropperStyle;
                      //rubberBand.Stroke = new SolidColorBrush(Colors.LightGray);
                    //rubberBand.Fill = Brushes.Yellow;
                    //rubberBand.Opacity = 0.20;
                    this.Children.Add(rubberBand);
                }

                double width = Math.Abs(mouseLeftDownPoint.X - currentPoint.X);
                double height = Math.Abs(mouseLeftDownPoint.Y - currentPoint.Y);
                double left = Math.Min(mouseLeftDownPoint.X, currentPoint.X);
                double top = Math.Min(mouseLeftDownPoint.Y, currentPoint.Y);

                rubberBand.Width = width;
                rubberBand.Height = height;
                Canvas.SetLeft(rubberBand, left);
                Canvas.SetTop(rubberBand, top);
            }
        }
        #endregion
    }
}