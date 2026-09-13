using System.Reflection;
using System.Runtime.CompilerServices;
using Modern.WindowKit;
using Modern.WindowKit.Platform;
using Xunit;

namespace Modern.Forms.Tests;

public class FormSizeLimitsTests
{
    [Fact]
    public void MinimumSizeDoesNotCreateAZeroSizedNativeMaximum ()
    {
        var form = MakeForm (out var native);
        form.MinimumSize = new System.Drawing.Size (1120, 760);
        Assert.Equal (new Size (1120, 760), native.Minimum);
        Assert.Equal (double.PositiveInfinity, native.Maximum.Width);
        Assert.Equal (double.PositiveInfinity, native.Maximum.Height);
        Assert.Equal (new System.Drawing.Size (1120, 760), form.Size);
    }

    [Fact]
    public void ClearingMaximumRestoresUnboundedNativeLimits ()
    {
        var form = MakeForm (out var native);
        form.MaximumSize = new System.Drawing.Size (1600, 1000);
        Assert.Equal (new Size (1600, 1000), native.Maximum);
        form.MaximumSize = System.Drawing.Size.Empty;
        Assert.Equal (double.PositiveInfinity, native.Maximum.Width);
        Assert.Equal (double.PositiveInfinity, native.Maximum.Height);
    }

    [Fact]
    public void ExplicitMaximumRemainsEnforcedWhenMinimumChanges ()
    {
        var form = MakeForm (out var native);
        form.MaximumSize = new System.Drawing.Size (1600, 1000);
        form.MinimumSize = new System.Drawing.Size (860, 650);
        Assert.Equal (new Size (1600, 1000), native.Maximum);
    }

    private static Form MakeForm (out NativeWindow native)
    {
        // Exercise the actual setters without opening a native test window.
        var form = (Form)RuntimeHelpers.GetUninitializedObject (typeof (Form));
        var proxy = DispatchProxy.Create<IWindowImpl, NativeWindow> ();
        native = (NativeWindow)(object)proxy;
        typeof (WindowBase).GetField ("window", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue (form, proxy);
        return form;
    }

    public class NativeWindow : DispatchProxy
    {
        public Size Minimum { get; set; }
        public Size Maximum { get; set; }
        public Size Client { get; set; } = new Size (800, 600);
        protected override object? Invoke (MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod!.Name) {
                case "SetMinMaxSize": Minimum = (Size)args![0]!; Maximum = (Size)args[1]!; return null;
                case "get_ClientSize": return Client;
                case "Resize": Client = (Size)args![0]!; return null;
                default: return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance (targetMethod.ReturnType) : null;
            }
        }
    }
}
