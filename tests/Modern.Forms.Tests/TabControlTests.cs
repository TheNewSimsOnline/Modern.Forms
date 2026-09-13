using System.Drawing;
using SkiaSharp;
using Xunit;

namespace Modern.Forms.Tests
{
    public class TabControlTests
    {
        [Fact]
        public void DesignerBounds_DoNotCoverTabStrip ()
        {
            using var root = new VisibleContainer ();
            using var tabs = new TabControl { Size = new Size (990, 553) };
            root.Controls.Add (tabs);
            var page = new TabPage {
                Text = "Resources",
                Bounds = new Rectangle (4, 22, 982, 527)
            };

            tabs.TabPages.Add (page);
            tabs.SelectedIndex = 0;
            tabs.PerformLayout ();

            Assert.Equal (new Rectangle (0, 31, 990, 522), page.Bounds);
            Assert.True (page.Visible);
        }

        [Fact]
        public void Paint_CompositesTabLabelsIntoTopStrip ()
        {
            using var root = new VisibleContainer ();
            using var tabs = new PaintableTabControl { Size = new Size (400, 200) };
            root.Controls.Add (tabs);
            tabs.TabPages.Add (new TabPage { Text = "Resources" });
            tabs.SelectedIndex = 0;
            tabs.PerformLayout ();

            using var bitmap = new SKBitmap (400, 200);
            using var canvas = new SKCanvas (bitmap);
            canvas.Clear (SKColors.White);
            tabs.Paint (new PaintEventArgs (bitmap.Info, canvas, 1));
            canvas.Flush ();

            var nonWhitePixels = 0;
            for (var y = 0; y < 31; y++)
                for (var x = 0; x < 160; x++)
                    if (bitmap.GetPixel (x, y) != SKColors.White)
                        nonWhitePixels++;

            Assert.True (nonWhitePixels > 0);
        }

        private sealed class PaintableTabControl : TabControl
        {
            public void Paint (PaintEventArgs e) => OnPaint (e);
        }

        private sealed class VisibleContainer : Control
        {
            public override bool Visible {
                get => true;
                set { }
            }
        }
    }
}
