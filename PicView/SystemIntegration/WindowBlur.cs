using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PicView.SystemIntegration
{
    internal static partial class WindowBlur
    {
        internal enum AccentState
        {
            Disabled = 0,
            EnableGradient = 1,
            EnableTransparentGradient = 2,
            EnableBlurBehind = 3,
            Invalid = 4
        }

        #region IEquatable<T>
        private struct AccentPolicy : IEquatable<AccentPolicy>
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;

            public bool Equals(AccentPolicy other)
            {
                return AnimationId == other.AnimationId;
            }

            public static bool operator ==(AccentPolicy left, AccentPolicy right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(AccentPolicy left, AccentPolicy right)
            {
                return !(left == right);
            }

            public override bool Equals(object? obj)
            {
                return obj is AccentPolicy && Equals((AccentPolicy)obj);
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }

        private struct WindowCompositionAttributeData : IEquatable<WindowCompositionAttributeData>
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;

            public bool Equals(WindowCompositionAttributeData other)
            {
                return Attribute == other.Attribute &&  
                       Data.Equals(other.Data) &&
                       SizeOfData == other.SizeOfData;
            }

            public override bool Equals(object? obj)
            {
                return obj is WindowCompositionAttributeData && Equals((WindowCompositionAttributeData)obj);
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }
        #endregion
        private enum WindowCompositionAttribute
        {
            AccentPolicy = 19
        }

        [LibraryImport("user32.dll")]
        private static partial int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        internal static void EnableBlur(Window window, AccentState accentState = AccentState.EnableBlurBehind)
        {
            var windowHelper = new WindowInteropHelper(window);
            var accent = new AccentPolicy
            {
                AccentState = accentState
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.AccentPolicy,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            int result = SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);

            if (result == 0)
            {
                throw new Exception("Failed to enable blur on window.");
            }
        }

    }
}
