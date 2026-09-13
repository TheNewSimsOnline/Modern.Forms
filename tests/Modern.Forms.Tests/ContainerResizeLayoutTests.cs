using System.Reflection;
using Xunit;

namespace Modern.Forms.Tests;

// A container that grows has to re-lay-out its own children. NewTool's macOS
// window kept every control at its design-time size because it did not.
public class ContainerResizeLayoutTests
{
    [Fact]
    public void AnchoredChildFollowsContainerResize ()
    {
        var parent = new Panel ();
        parent.SetBounds (0, 0, 1080, 720, BoundsSpecified.All);
        var child = AddAnchoredChild (parent);

        parent.SetBounds (0, 0, 1360, 860, BoundsSpecified.All);

        Assert.Equal (1270, child.Width);
        Assert.Equal (693, child.Height);
    }

    [Fact]
    public void AnchoredChildFollowsWindowAdapterResize ()
    {
        var adapterType = typeof (Control).Assembly.GetType ("Modern.Forms.ControlAdapter")!;
        var adapter = (Control)adapterType
            .GetConstructors (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0]
            .Invoke (new object?[] { null });
        adapter.SetBounds (0, 0, 1080, 720, BoundsSpecified.All);
        var child = AddAnchoredChild (adapter);

        adapter.SetBounds (0, 0, 1360, 860, BoundsSpecified.All);

        Assert.Equal (1270, child.Width);
        Assert.Equal (693, child.Height);
    }

    [Fact]
    public void DockedChildFollowsContainerResize ()
    {
        var parent = new Panel ();
        parent.SetBounds (0, 0, 400, 300, BoundsSpecified.All);
        var child = new Panel { Dock = DockStyle.Fill };
        parent.Controls.Add (child);

        parent.SetBounds (0, 0, 900, 700, BoundsSpecified.All);

        Assert.Equal (parent.DisplayRectangle.Size, child.Size);
    }

    [Fact]
    public void TabPageFollowsTabControlResize ()
    {
        var tabs = new TabControl ();
        var page = new TabPage ("Object Browser");
        tabs.TabPages.Add (page);
        tabs.SetBounds (0, 0, 400, 300, BoundsSpecified.All);

        tabs.SetBounds (0, 0, 1270, 693, BoundsSpecified.All);

        Assert.Equal (1270, page.Width);
        Assert.True (page.Height > 600, $"Expected the page to grow with the tab control, was {page.Height}.");
    }

    private static Panel AddAnchoredChild (Control parent)
    {
        var child = new Panel ();
        parent.Controls.Add (child);
        child.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        child.SetBounds (12, 27, 990, 553, BoundsSpecified.All);
        return child;
    }
}
