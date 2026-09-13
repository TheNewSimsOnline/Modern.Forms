using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Modern.WindowKit;
using Modern.WindowKit.Controls.Platform.Surfaces;
using Modern.WindowKit.Platform;
using Xunit;

namespace Modern.Forms.Tests;

public class WindowPaintTests
{
    [Theory]
    [InlineData(0, 64, 256, true)]
    [InlineData(64, 0, 256, true)]
    [InlineData(64, 64, 0, true)]
    [InlineData(64, 64, 256, false)]
    [InlineData(64, 64, 1, true)]
    public void UndrawableFramebufferIsSkippedAndReleased (int width, int height, int rowBytes, bool hasAddress)
    {
        var address = hasAddress ? Marshal.AllocHGlobal (16) : IntPtr.Zero;
        try {
            var framebuffer = DispatchProxy.Create (typeof (IFramebufferPlatformSurface).GetMethod ("Lock")!.ReturnType, typeof (Framebuffer));
            var state = (Framebuffer)framebuffer;
            state.Size = new PixelSize (width, height); state.RowBytes = rowBytes; state.Address = address;
            Paint (framebuffer);
            Assert.True (state.Released);
        } finally { if (address != IntPtr.Zero) Marshal.FreeHGlobal (address); }
    }

    [Fact]
    public void MissingBackingStoreIsSkipped () => Paint (null);

    private static void Paint (object? framebuffer)
    {
        var form = (Form)RuntimeHelpers.GetUninitializedObject (typeof (Form));
        var native = DispatchProxy.Create<IWindowImpl, NativeWindow> ();
        var surface = DispatchProxy.Create<IFramebufferPlatformSurface, Surface> ();
        ((Surface)(object)surface).Framebuffer = framebuffer;
        ((NativeWindow)(object)native).Surfaces = new object[] { surface };
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof (WindowBase).GetField ("window", flags)!.SetValue (form, native);
        typeof (WindowBase).GetField ("shown", flags)!.SetValue (form, true);
        typeof (WindowBase).GetMethod ("DoPaint", flags)!.Invoke (form, new object[] { new Rect (0, 0, 64, 64) });
    }

    public class NativeWindow : DispatchProxy
    {
        public object[] Surfaces { get; set; } = Array.Empty<object> ();
        protected override object? Invoke (MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == "get_Surfaces" ? Surfaces : throw new InvalidOperationException (targetMethod.Name);
    }
    public class Surface : DispatchProxy
    {
        public object? Framebuffer { get; set; }
        protected override object? Invoke (MethodInfo? targetMethod, object?[]? args) =>
            targetMethod!.Name == "Lock" ? Framebuffer : throw new InvalidOperationException (targetMethod.Name);
    }
    public class Framebuffer : DispatchProxy
    {
        public PixelSize Size { get; set; }
        public int RowBytes { get; set; }
        public IntPtr Address { get; set; }
        public bool Released { get; set; }
        protected override object? Invoke (MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod!.Name) {
                case "get_Size": return Size;
                case "get_Address": return Address;
                case "get_RowBytes": return RowBytes;
                case "get_Format": return PixelFormat.Bgra8888;
                case "Dispose": Released = true; return null;
                default: throw new InvalidOperationException (targetMethod.Name);
            }
        }
    }
}
