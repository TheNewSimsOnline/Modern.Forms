using Modern.Forms.Renderers;

namespace Modern.Forms
{
    /// <summary>
    /// Represents a TabControl control.
    /// </summary>
    public class TabControl : Control
    {
        private readonly TabStrip tab_strip;

        /// <summary>
        /// Initializes a new instance of the TabControl class.
        /// </summary>
        public TabControl ()
        {
            tab_strip = Controls.AddImplicitControl (new TabStrip {
                Dock = DockStyle.Top
            });

            tab_strip.SelectedTabChanged += TabStrip_SelectedTabChanged;

            TabPages = new TabPageCollection (this, tab_strip);
        }

        /// <summary>
        /// Gets the collection of tabs contained by this TabControl.
        /// </summary>
        public TabPageCollection TabPages { get; }

        private TabPage? GetPageFromTab (TabStripItem? item) => TabPages.FirstOrDefault (p => p.TabStripItem == item);

        /// <summary>
        /// Raises the SelectedIndexChanged event.
        /// </summary>
        protected virtual void OnSelectedIndexChanged (EventArgs e) => SelectedIndexChanged?.Invoke (this, e);

        /// <inheritdoc/>
        protected override void OnPaint (PaintEventArgs e)
        {
            base.OnPaint (e);

            RenderManager.Render (this, e);

            // The tab strip is an implicit child. Avalonia/AppKit can omit that
            // child's backing buffer while still laying out the selected page,
            // leaving a correctly-sized but blank strip. Composite the strip as
            // part of the TabControl's own paint so tab labels are guaranteed to
            // be visible on every backend.
            tab_strip.RenderOntoParent (e);
        }

        /// <summary>
        /// Gets or sets the index of the currently selected tab page. This value will be -1 if there is not a selected tab page;
        /// </summary>
        public int SelectedIndex {
            get => tab_strip.SelectedIndex;
            set {
                if (tab_strip.SelectedIndex == value) {
                    // A designer commonly assigns SelectedIndex = 0 after the
                    // first page is inserted. The strip already defaults to 0,
                    // so no change event is raised unless we synchronize the
                    // page visibility explicitly.
                    TabStrip_SelectedTabChanged (tab_strip, EventArgs.Empty);
                } else {
                    tab_strip.SelectedIndex = value;
                }
            }
        }

        /// <summary>
        /// Raised when the value of the SelectedIndex property changes.
        /// </summary>
        public event EventHandler? SelectedIndexChanged;

        /// <summary>
        /// Gets or sets the currently selected tab page.
        /// </summary>
        public TabPage? SelectedTabPage {
            get => GetPageFromTab (tab_strip.SelectedTab);
            set {
                if (value is null) {
                    tab_strip.SelectedTab = null;
                    return;
                }

                var index = TabPages.IndexOf (value);

                if (index == -1)
                    throw new ArgumentException ("TabPage is not part of this TabControl");

                tab_strip.SelectedIndex = index;
            }
        }

        // Handles changes of the TabStrip's selected tab.
        private void TabStrip_SelectedTabChanged (object? sender, EventArgs e)
        {
            var old_selected = Controls.OfType<TabPage> ().FirstOrDefault (tp => tp.Visible);
            var new_selected = GetPageFromTab (tab_strip.SelectedTab);

            if (old_selected == new_selected)
                return;

            if (old_selected != null)
                old_selected.Visible = false;

            if (new_selected != null)
                new_selected.Visible = true;

            OnSelectedIndexChanged (EventArgs.Empty);
        }

        /// <inheritdoc/>
        protected override void OnLayout (LayoutEventArgs e)
        {
            base.OnLayout (e);

            // Keep the implicit strip and the selected page in deterministic,
            // non-overlapping bounds. Designer-created pages carry WinForms' old
            // (4, 22) design-time bounds, and on some backends those bounds could
            // survive the first layout and paint over the implicit tab strip.
            var strip_height = tab_strip.Height > 0 ? tab_strip.Height : 31;
            tab_strip.SetBounds (0, 0, Width, strip_height, BoundsSpecified.All);

            var page_height = Math.Max (0, Height - strip_height);
            foreach (var page in TabPages)
                page.SetBounds (0, strip_height, Width, page_height, BoundsSpecified.All);
        }
    }
}
