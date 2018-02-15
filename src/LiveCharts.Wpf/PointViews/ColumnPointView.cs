﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using LiveCharts.Core.Abstractions;
using LiveCharts.Core.Coordinates;
using LiveCharts.Core.Data;
using LiveCharts.Core.ViewModels;
using LiveCharts.Wpf.Animations;
using Frame = LiveCharts.Wpf.Animations.Frame;

namespace LiveCharts.Wpf.PointViews
{
    /// <summary>
    /// The column point view.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TPoint">the type of the chart point.</typeparam>
    /// <typeparam name="TShape">the type of the shape.</typeparam>
    /// <typeparam name="TLabel">the type of the label.</typeparam>
    /// <typeparam name="TCoordinate">the type of the coordinate.</typeparam>
    /// <typeparam name="TViewModel">the type of the view model.</typeparam>
    /// <seealso cref="PointView{TModel, Point,Point2D, ColumnViewModel, TShape, TLabel}" />
    public class ColumnPointView<TModel, TPoint, TCoordinate, TViewModel, TShape, TLabel>
        : PointView<TModel, TPoint, TCoordinate, TViewModel, TShape, TLabel>
        where TPoint : Point<TModel, TCoordinate, TViewModel>, new()
        where TCoordinate : Point2D
        where TViewModel : ColumnViewModel
        where TShape : Shape, new()
        where TLabel : FrameworkElement, IDataLabelControl, new()
    {
        private TPoint _point;

        /// <inheritdoc />
        protected override void OnDraw(TPoint point, TPoint previous)
        {
            var chart = point.Chart.View;
            var vm = point.ViewModel;
            var isNew = Shape == null;

            if (isNew)
            {
                var wpfChart = (CartesianChart) chart;
                Shape = new TShape();
                wpfChart.DrawArea.Children.Add(Shape);
                Canvas.SetLeft(Shape, vm.Left);
                Canvas.SetTop(Shape, vm.Zero);
                Shape.Width = vm.Width;
                Shape.Height = 0;
            }

            var r = Shape as Rectangle;
            if (r != null)
            {
                var radius = vm.Width * .4;
                r.RadiusY = radius;
                r.RadiusX = radius;
            }

            Shape.Stroke = point.Series.Stroke.AsWpf();
            Shape.Fill = point.Series.Fill.AsWpf();

            var speed = chart.AnimationsSpeed;

            var by = isNew ? vm.Height * .3 : 0;
            var bx = isNew ? vm.Width * .1 : 0;

            Shape.Animate()
                .AtSpeed(speed)
                .Property(Canvas.LeftProperty,
                    new Frame(0.9, vm.Left - bx * .5),
                    new Frame(1, vm.Left))
                .Property(FrameworkElement.WidthProperty,
                    new Frame(0.9, vm.Width + bx),
                    new Frame(1, vm.Width))
                .Property(Canvas.TopProperty,
                    new Frame(0.8, vm.Top - by),
                    new Frame(0.9, vm.Top - by * .5),
                    new Frame(1, vm.Top))
                .Property(FrameworkElement.HeightProperty,
                    new Frame(0.8, vm.Height + by),
                    new Frame(0.9, vm.Height + by * .5),
                    new Frame(1, vm.Height))
                .Begin();
            _point = point;
        }

        /// <inheritdoc />
        protected override void OnDrawLabel(TPoint point, Core.Drawing.Point location)
        {
            var chart = point.Chart.View;
            var isNew = Label == null;

            if (isNew)
            {
                var wpfChart = (CartesianChart) chart;
                Label = new TLabel();
                Label.Measure(point.PackAll());
                Canvas.SetLeft(Shape, Canvas.GetLeft(Shape));
                Canvas.SetTop(Shape, Canvas.GetTop(Shape));
                wpfChart.DrawArea.Children.Add((UIElement) Label);
            }

            var speed = chart.AnimationsSpeed;

            ((FrameworkElement) Label).BeginAnimation(
                Canvas.LeftProperty,
                new DoubleAnimation(location.X, speed));
            ((FrameworkElement) Label).BeginAnimation(
                Canvas.TopProperty,
                new DoubleAnimation(location.Y, speed));
        }

        /// <inheritdoc />
        protected override void OnDispose(IChartView chart)
        {
            var wpfChart = (CartesianChart) chart;

            var zero = chart.Model.ScaleToUi(0, chart.Dimensions[1][_point.Series.ScalesAt[1]]);

            var animation = Shape.Animate()
                .AtSpeed(chart.AnimationsSpeed)
                .Property(Canvas.TopProperty, zero)
                .Property(FrameworkElement.HeightProperty, 0);

            animation.Then((sender, args) =>
            {
                wpfChart.DrawArea.Children.Remove(Shape);
                wpfChart.DrawArea.Children.Remove((UIElement) Label);
                animation.Dispose();
                animation = null;
            }).Begin();
        }
    }
}