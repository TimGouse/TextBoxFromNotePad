using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextBoxFromNotePad
{
    public partial class Form1 : Form
    {
        private const int WH_KEYBOARD = 2;
        private const int WM_KEYDOWN = 0x0100;
        private const int WH_CALLWNDPROC = 4;
        private static IntPtr _hookID = IntPtr.Zero;
        private static IntPtr notepadHandle;
        private delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr CallWndProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static IntPtr _hookIDCallWnd = IntPtr.Zero;
        private const int WM_SETTEXT = 0x000C;

        public Form1()
        {
            InitializeComponent();

            notepadHandle = FindWindow("Notepad", null);
            if (notepadHandle == IntPtr.Zero)
            {
                MessageBox.Show("Notepad is not running");
                return;
            }

            uint notepadThreadId = GetWindowThreadProcessId(notepadHandle, IntPtr.Zero);
            _hookID = SetHook(new KeyboardProc(HookCallback), WH_KEYBOARD, notepadThreadId);
            if (_hookID == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                MessageBox.Show("Failed to set hook, error code: " + errorCode);
                return;
            }
            _hookIDCallWnd = SetHook(new CallWndProc(CallWndProcHookCallback), WH_CALLWNDPROC, notepadThreadId);
        }
        private IntPtr SetHook(Delegate proc, int hookType, uint threadId)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), threadId);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            Console.WriteLine($"HookCallback called with nCode: {nCode}, wParam: {wParam}, lParam: {lParam}");
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                char ch = (char)vkCode;
                textBox1.Text += ch;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private IntPtr CallWndProcHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                CWPSTRUCT msg = (CWPSTRUCT)Marshal.PtrToStructure(lParam, typeof(CWPSTRUCT));

                if (msg.message == WM_SETTEXT)
                {
                    string text = Marshal.PtrToStringAnsi(msg.lParam); // Получаем текст сообщения
                    textBox1.Invoke(new Action(() =>
                    {
                        textBox1.Text += text; // Добавляем текст в TextBox на форме
                    }));
                }
            }

            return CallNextHookEx(_hookIDCallWnd, nCode, wParam, lParam);
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }
        // Импорт необходимых Windows API функций и структур
        [StructLayout(LayoutKind.Sequential)]
        private struct CWPSTRUCT
        {
            public IntPtr lParam;
            public IntPtr wParam;
            public uint message;
            public IntPtr hwnd;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        private void Form1_Leave(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed_1(object sender, FormClosedEventArgs e)
        {
           
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            UnhookWindowsHookEx(_hookID);
        }
    }
}
