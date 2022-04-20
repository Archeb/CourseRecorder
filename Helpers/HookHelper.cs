using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

// Reference:
// https://docs.microsoft.com/en-us/archive/blogs/toub/low-level-mouse-hook-in-c
// https://docs.microsoft.com/en-us/archive/blogs/toub/low-level-keyboard-hook-in-c
// https://stackoverflow.com/questions/44990335/is-there-any-way-to-global-hook-the-mouse-actions-like-im-hooking-the-keyboard
// http://pinvoke.net/default.aspx/user32/SetWindowsHookEx.html

namespace CourseRecorder.Helpers
{
    internal class HookHelper
    {
        public delegate void MouseCallbackFunction(int x, int y, MouseMessage message);
        public delegate void KeyboardCallbackFunction(int key);
        private MouseCallbackFunction GlobalUserMouseCallback;
        private KeyboardCallbackFunction GlobalUserKeyboardCallback;
        private static IntPtr GlobalLlMouseHook, GlobalLlKeyboardHook;
        internal delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        public void SetUpMouseHook(MouseCallbackFunction UserMouseCallback)
        {
            Debug.WriteLine("Setting up global mouse hook");

            // Create an instance of HookProc.
            HookProc GlobalLlMouseHookCallback = MouseHookCallback;
            GlobalUserMouseCallback = UserMouseCallback;
            GlobalLlMouseHook = SetWindowsHookEx(
                HookType.WH_MOUSE_LL,
                GlobalLlMouseHookCallback,
                Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
                0);

            if (GlobalLlMouseHook == IntPtr.Zero)
            {
                Debug.WriteLine("Unable to set global mouse hook");
            }
        }
        private void ClearMouseHook()
        {
            Debug.WriteLine("Deleting global mouse hook");

            if (GlobalLlMouseHook != IntPtr.Zero)
            {
                // Unhook the low-level mouse hook
                if (!UnhookWindowsHookEx(GlobalLlMouseHook))

                GlobalLlMouseHook = IntPtr.Zero;
            }
        }
        public int MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // Get the mouse WM from the wParam parameter
                var wmMouse = (MouseMessage)wParam;
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                GlobalUserMouseCallback(hookStruct.pt.x, hookStruct.pt.y, wmMouse);
            }

            // Pass the hook information to the next hook procedure in chain
            return CallNextHookEx(GlobalLlMouseHook, nCode, wParam, lParam);
        }

        public void SetUpKeyboardHook(KeyboardCallbackFunction UserKeyboardCallback)
        {
            Debug.WriteLine("Setting up global keyboard hook");

            // Create an instance of HookProc.
            HookProc GlobalLlKeyboardHookCallback = KeyboardHookCallback;
            GlobalUserKeyboardCallback = UserKeyboardCallback;
            GlobalLlKeyboardHook = SetWindowsHookEx(
                HookType.WH_KEYBOARD_LL,
                GlobalLlKeyboardHookCallback,
                Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
                0);

            if (GlobalLlKeyboardHook == IntPtr.Zero)
            {
                Debug.WriteLine("Unable to set global keyboard hook");
            }
        }
        private void ClearKeyboardHook()
        {
            Debug.WriteLine("Deleting global keyboard hook");

            if (GlobalLlKeyboardHook != IntPtr.Zero)
            {
                // Unhook the low-level keyboard hook
                if (!UnhookWindowsHookEx(GlobalLlKeyboardHook))

                    GlobalLlKeyboardHook = IntPtr.Zero;
            }
        }

        public int KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)KeyboardMessage.WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Debug.WriteLine("KeyCode: " + vkCode.ToString());
                GlobalUserKeyboardCallback(vkCode);
            }

            // Pass the hook information to the next hook procedure in chain
            return CallNextHookEx(GlobalLlMouseHook, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetWindowsHookEx(HookType hookType,
            HookProc Callback, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);
    }

    internal static class HookCodes
    {
        public const int HC_ACTION = 0;
        public const int HC_GETNEXT = 1;
        public const int HC_SKIP = 2;
        public const int HC_NOREMOVE = 3;
        public const int HC_NOREM = HC_NOREMOVE;
        public const int HC_SYSMODALON = 4;
        public const int HC_SYSMODALOFF = 5;
    }

    internal enum HookType
    {
        WH_KEYBOARD = 2,
        WH_MOUSE = 7,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class POINT
    {
        public int x;
        public int y;
    }

    ///     The MSLLHOOKSTRUCT structure contains information about a low-level keyboard
    ///     input event.
    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEHOOKSTRUCT
    {
        public POINT pt; // The x and y coordinates in screen coordinates
        public int hwnd; // Handle to the window that'll receive the mouse message
        public int wHitTestCode;
        public int dwExtraInfo;
    }

    ///     The MOUSEHOOKSTRUCT structure contains information about a mouse event passed
    ///     to a WH_MOUSE hook procedure, MouseProc.
    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        public POINT pt; // The x and y coordinates in screen coordinates. 
        public int mouseData; // The mouse wheel and button info.
        public int flags;
        public int time; // Specifies the time stamp for this message. 
        public IntPtr dwExtraInfo;
    }

    internal enum MouseMessage
    {
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,

        WM_MOUSEWHEEL = 0x020A,
        WM_MOUSEHWHEEL = 0x020E,

        WM_NCMOUSEMOVE = 0x00A0,
        WM_NCLBUTTONDOWN = 0x00A1,
        WM_NCLBUTTONUP = 0x00A2,
        WM_NCLBUTTONDBLCLK = 0x00A3,
        WM_NCRBUTTONDOWN = 0x00A4,
        WM_NCRBUTTONUP = 0x00A5,
        WM_NCRBUTTONDBLCLK = 0x00A6,
        WM_NCMBUTTONDOWN = 0x00A7,
        WM_NCMBUTTONUP = 0x00A8,
        WM_NCMBUTTONDBLCLK = 0x00A9
    }

    ///     The structure contains information about a low-level keyboard input event.
    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        public int vkCode; // Specifies a virtual-key code
        public int scanCode; // Specifies a hardware scan code for the key
        public int flags;
        public int time; // Specifies the time stamp for this message
        public int dwExtraInfo;
    }

    internal enum KeyboardMessage
    {
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_SYSKEYDOWN = 0x0104,
        WM_SYSKEYUP = 0x0105
    }
}
