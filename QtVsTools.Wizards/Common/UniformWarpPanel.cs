//
// Copyright 2009,  Adam Cataldo
// https://www.codeproject.com/Articles/32629/A-better-panel-for-data-binding-to-a-WrapPanel-in
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the “Software”), to deal in the Software without
// restriction, including without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QtVsTools.Wizards.Common
{
    internal class UniformWrapPanel : WrapPanel
    {
        #region Dependency Properties

        /// <summary>
        /// Gets or sets a value indicating whether the child elements should be arranged uniformly
        /// in both rows and columns.
        /// </summary>
        public static readonly DependencyProperty IsAutoUniformProperty =
            DependencyProperty.Register(
                nameof(IsAutoUniform),
                typeof(bool),
                typeof(UniformWrapPanel),
                new FrameworkPropertyMetadata(true, OnIsAutoUniformChanged)
            );

        /// <summary>
        /// Gets or sets a value indicating whether the child elements should be arranged uniformly
        /// in both rows and columns.
        /// </summary>
        public bool IsAutoUniform
        {
            get => (bool)GetValue(IsAutoUniformProperty);
            set => SetValue(IsAutoUniformProperty, value);
        }

        private static void OnIsAutoUniformChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (sender is UniformWrapPanel panel)
                panel.InvalidateVisual();
        }

        /// <summary>
        /// Gets or sets the desired number of rows in the UniformWrapPanel.
        /// </summary>
        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(
                nameof(Rows),
                typeof(int),
                typeof(UniformWrapPanel),
                new FrameworkPropertyMetadata(0, OnRowsChanged)
            );

        /// <summary>
        /// Gets or sets the desired number of rows in the UniformWrapPanel.
        /// </summary>
        public int Rows
        {
            get => (int)GetValue(RowsProperty);
            set => SetValue(RowsProperty, Math.Max(value, 1));
        }

        private static void OnRowsChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (sender is UniformWrapPanel panel)
                panel.InvalidateVisual();
        }

        /// <summary>
        /// Gets or sets the desired number of columns in the UniformWrapPanel.
        /// </summary>
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(int),
                typeof(UniformWrapPanel),
                new FrameworkPropertyMetadata(0, OnColumnsChanged)
            );

        /// <summary>
        /// Gets or sets the desired number of columns in the UniformWrapPanel.
        /// </summary>
        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, Math.Max(value, 1));
        }

        private static void OnColumnsChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (sender is UniformWrapPanel panel)
                panel.InvalidateVisual();
        }

        #endregion

        /// <summary>
        /// Measures the child elements of the UniformWrapPanel in order to determine their size.
        /// </summary>
        /// <param name="availableSize">The available size provided by the layout system.</param>
        /// <returns>The size that this UniformWrapPanel determines it needs during layout.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // If there are no child elements, just use the base implementation
            if (Children.Count <= 0)
                return base.MeasureOverride(availableSize);

            // Convert child elements to a list for easy manipulation
            var elements = Children.Cast<UIElement>().ToList();

            if (Orientation == Orientation.Horizontal) {
                // Measure the desired widths for the elements and calculate the desired width for
                // the row
                var desiredWidths = new List<double>(Children.Count);
                if (IsAutoUniform) {
                    // If using AutoUniform, find the maximum width of the child elements
                    ItemWidth = 0.0;
                    foreach (UIElement element in Children) {
                        element.Measure(availableSize);
                        var nextSize = element.DesiredSize;
                        if (!double.IsInfinity(nextSize.Width) && !double.IsNaN(nextSize.Width))
                            ItemWidth = Math.Max(nextSize.Width, ItemWidth);
                    }

                    // Set the desired width for each element to be the maximum width
                    desiredWidths.AddRange(Enumerable.Repeat(ItemWidth, Children.Count));
                } else {
                    // If not using AutoUniform, set the desired width for each element based on
                    // Rows
                    if (Rows <= 0)
                        return base.MeasureOverride(availableSize);

                    desiredWidths.AddRange(elements.Select(element =>
                        GetDesiredWidth(element, availableSize)));
                }

                // Calculate the desired width for the entire UniformWrapPanel layout
                var desiredWidth = CalculateDesiredRowOrColumnSize(desiredWidths, Rows);
                availableSize = new Size(Math.Max(desiredWidth, availableSize.Width),
                    availableSize.Height);
            } else {
                // Measure the desired heights for the elements and calculate the desired height
                // for the column
                var desiredHeights = new List<double>(Children.Count);
                if (IsAutoUniform) {
                    // If using AutoUniform, find the maximum height of the child elements
                    ItemHeight = 0.0;
                    foreach (UIElement element in Children) {
                        element.Measure(availableSize);
                        var nextSize = element.DesiredSize;
                        if (!double.IsInfinity(nextSize.Height) && !double.IsNaN(nextSize.Height))
                            ItemHeight = Math.Max(nextSize.Height, ItemHeight);
                    }

                    // Set the desired height for each element to be the maximum height
                    desiredHeights.AddRange(Enumerable.Repeat(ItemHeight, Children.Count));
                } else {
                    // If not using AutoUniform, set the desired height for each element based on
                    // Columns
                    if (Columns <= 0)
                        return base.MeasureOverride(availableSize);

                    desiredHeights = elements
                        .Select(element => GetDesiredHeight(element, availableSize)).ToList();
                }

                // Calculate the desired height for the entire UniformWrapPanel layout
                var desiredHeight = CalculateDesiredRowOrColumnSize(desiredHeights, Columns);
                availableSize = new Size(availableSize.Width,
                    Math.Max(desiredHeight, availableSize.Height));
            }

            // Call the base implementation of MeasureOverride with the updated available size
            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// Calculates the desired size for rows or columns based on the provided list of sizes and
        /// the number of rows or columns.
        /// </summary>
        /// <param name="rowOrColumnSizes">The list of sizes for rows or columns.</param>
        /// <param name="targetRowCountOrColumnCount">The desired number of rows or
        /// columns.</param>
        /// <returns>The calculated desired size for rows or columns.</returns>
        private double CalculateDesiredRowOrColumnSize(List<double> rowOrColumnSizes,
            int targetRowCountOrColumnCount)
        {
            // Calculate the maximum size based on the provided sizes or default to 0 if no valid
            // sizes are found
            var maxSize = rowOrColumnSizes.Where(length => !double.IsNaN(length))
                .DefaultIfEmpty(0.0).Max();

            // If the maximum size is non-positive, return NaN as it indicates an invalid layout
            if (maxSize <= 0.0)
                return double.NaN;

            // If not using AutoUniform, adjust sizes by setting NaN sizes to the maximum size
            if (!IsAutoUniform) {
                rowOrColumnSizes = rowOrColumnSizes
                    .Select(length => double.IsNaN(length) ? maxSize : length).ToList();
            }

            // Calculate the total desired size and the maximum count of rows/columns to consider
            var totalDesiredSize = rowOrColumnSizes.Sum();
            var maxCount = Math.Min(targetRowCountOrColumnCount, rowOrColumnSizes.Count);

            // Calculate the suitable size for rows/columns based on total desired size and maximum
            // count
            var suitableSize = totalDesiredSize / maxCount;

            // Adjust suitableSize to ensure it doesn't exceed the maximum count of rows/columns
            while (CalculateRowCountOrColumnCountWithinLimit(rowOrColumnSizes, suitableSize,
                       out var nextLengthIncrement) > maxCount) {
                suitableSize += nextLengthIncrement;
            }

            // Return the larger of the calculated suitable size and the maximum size in the list
            return Math.Max(suitableSize, rowOrColumnSizes.Max());
        }

        /// <summary>
        /// Calculates the number of rows or columns that can fit within the specified length
        /// limit, and calculates the next length increment required for proper layout.
        /// </summary>
        /// <param name="desiredLengths">The list of desired lengths for rows or columns.</param>
        /// <param name="rowOrColumnLengthLimit">The maximum length limit for a row or
        /// column.</param>
        /// <param name="nextLengthIncrement">The calculated next length increment required for
        /// proper layout.</param>
        /// <returns>The number of rows or columns that can fit within the specified length
        /// limit.</returns>
        private static int CalculateRowCountOrColumnCountWithinLimit(List<double> desiredLengths,
            double rowOrColumnLengthLimit, out double nextLengthIncrement)
        {
            var rowCountOrColumnCount = 1;
            var currentCumulativeLength = 0.0;
            var minimalIncrement = double.MaxValue;

            // Iterate through the desired lengths to calculate the number of rows or columns that
            // can fit within the specified length limit and calculate the next length increment
            foreach (var desiredLength in desiredLengths) {
                if (currentCumulativeLength + desiredLength > rowOrColumnLengthLimit) {
                    minimalIncrement = Math.Min(minimalIncrement,
                        currentCumulativeLength + desiredLength - rowOrColumnLengthLimit);
                    currentCumulativeLength = desiredLength;
                    rowCountOrColumnCount++;
                } else {
                    currentCumulativeLength += desiredLength;
                }
            }

            // If minimalIncrement is still at its initial value, set it to 1 as the default
            // increment
            nextLengthIncrement = Math.Abs(minimalIncrement - double.MaxValue) > 0.0
                ? minimalIncrement : 1;

            return rowCountOrColumnCount;
        }

        /// <summary>
        /// Measures the desired width of the specified UI element within the given available size.
        /// </summary>
        /// <param name="element">The UI element to measure.</param>
        /// <param name="availableSize">The available size to constrain the measurement.</param>
        /// <returns>
        /// The desired width of the UI element. If the width is double.infinity or double.NaN,
        /// return double.NaN to indicate an invalid value.
        /// </returns>
        private static double GetDesiredWidth(UIElement element, Size availableSize)
        {
            element.Measure(availableSize);

            var length = element.DesiredSize.Width;
            if (double.IsInfinity(length) || double.IsNaN(length))
                return double.NaN;
            return length;
        }

        /// <summary>
        /// Measures the desired height of the specified UI element within the given available
        /// size.
        /// </summary>
        /// <param name="element">The UI element to measure.</param>
        /// <param name="availableSize">The available size to constrain the measurement.</param>
        /// <returns>
        /// The desired height of the UI element. If the height is double.infinity or double.NaN,
        /// return double.NaN to indicate an invalid value.
        /// </returns>
        private static double GetDesiredHeight(UIElement element, Size availableSize)
        {
            element.Measure(availableSize);

            var length = element.DesiredSize.Height;
            if (double.IsInfinity(length) || double.IsNaN(length))
                return double.NaN;
            return length;
        }
    }
}
