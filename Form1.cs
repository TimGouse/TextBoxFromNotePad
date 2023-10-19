using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TextBoxFromNotePad
{
    public partial class Form1 : Form
    {
        // Делегат для обработчика событий клавиатуры
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Экземпляр делегата, который необходимо сохранить, чтобы предотвратить его сборку мусорщиком
        private LowLevelKeyboardProc _proc;

        // Дескриптор хука
        private static IntPtr _hookID = IntPtr.Zero;

        // Константы для идентификации типов клавиатурных событий
        private const int WM_KEYDOWN = 0x0100;
        private const int WH_KEYBOARD_LL = 13;

        public Form1()
        {
            InitializeComponent();

            // Устанавливаем хук
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        // Callback-функция, вызываемая при срабатывании хука
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Получаем состояние клавиш Shift и AltGr
                bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;
                bool altGrPressed = (Control.ModifierKeys & (Keys.Control | Keys.Alt)) != 0;

                // Получаем активное окно
                IntPtr hWnd = GetForegroundWindow();
                StringBuilder text = new StringBuilder(256);
                if (GetWindowText(hWnd, text, 256) > 0)
                {
                    // Проверяем, принадлежит ли окно Notepad
                    if (text.ToString().IndexOf("Блокнот", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Получаем текущую раскладку клавиатуры
                        IntPtr keyboardLayout = GetKeyboardLayout(0);

                        // Если да, добавляем символ к тексту
                        textBox1.Invoke(new Action(() =>
                        {
                            string chars = GetCharsFromKeys((uint)vkCode, shiftPressed, altGrPressed, keyboardLayout);
                            if (!string.IsNullOrEmpty(chars) && chars.Length == 1 && char.IsLetterOrDigit(chars[0]))
                            {
                                textBox1.Text += chars;
                            }
                            else if ((Keys)vkCode == Keys.Back) // Если была нажата клавиша Backspace
                            {
                                if (textBox1.Text.Length > 0)
                                {
                                    textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 1);
                                }
                            }
                            // Автоматически прокручиваем TextBox до конца, чтобы последний текст был видимым.
                            textBox1.SelectionStart = textBox1.Text.Length;
                            textBox1.ScrollToCaret();
                        }));
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public string GetCharsFromKeys(uint keys, bool shiftPressed, bool altGrPressed, IntPtr keyboardLayout)
        {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];
            if (shiftPressed)
                keyboardState[(int)Keys.ShiftKey] = 0xff;
            if (altGrPressed)
            {
                keyboardState[(int)Keys.ControlKey] = 0xff;
                keyboardState[(int)Keys.Menu] = 0xff;
            }
            ToUnicodeEx(keys, 0, keyboardState, buf, 256, 0, keyboardLayout);
            return buf.ToString();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        public static extern int ToUnicodeEx(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
    StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags,
            IntPtr dwhkl);
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        // Импорт необходимых Windows API функций и структур
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")]
        public static extern int ToUnicode(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
        StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags);

        public string GetCharsFromKeys(uint keys, bool shiftPressed, bool altGrPressed)
        {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];
            if (shiftPressed)
                keyboardState[(int)Keys.ShiftKey] = 0xff;
            if (altGrPressed)
            {
                keyboardState[(int)Keys.ControlKey] = 0xff;
                keyboardState[(int)Keys.Menu] = 0xff;
            }
            ToUnicode(keys, 0, keyboardState, buf, 256, 0);
            return buf.ToString();
        }

        private void Form1_Leave(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed_1(object sender, FormClosedEventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
        }
    }
}
//using System;
//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Automation;
//using System.Windows.Forms;


//namespace TextBoxFromNotePad
//{
//    public partial class Form1 : Form
//    {
//        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
//        private LowLevelKeyboardProc _proc;
//        private static IntPtr _hookID = IntPtr.Zero;

//        private const int WM_KEYDOWN = 0x0100;
//        private const int WH_KEYBOARD_LL = 13;
//        private const int WM_GETTEXT = 0x000D;
//        private const int WM_GETTEXTLENGTH = 0x000E;

//        public Form1()
//        {
//            InitializeComponent();
//            _proc = HookCallback;
//            _hookID = SetHook(_proc);
//        }

//        private static IntPtr SetHook(LowLevelKeyboardProc proc)
//        {
//            using (Process curProcess = Process.GetCurrentProcess())
//            using (ProcessModule curModule = curProcess.MainModule)
//            {
//                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
//            }
//        }
//        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
//        {
//            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
//            {
//                // Вызываем метод обновления текста из Notepad асинхронно, чтобы не блокировать UI поток
//                Task.Run(() => UpdateTextBoxFromNotepad());
//            }
//            return CallNextHookEx(_hookID, nCode, wParam, lParam);
//        }

//        private void UpdateTextBoxFromNotepad()
//        {
//            try
//            {
//                // Получаем корневой элемент Notepad
//                var notepad = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, "Notepad"));
//                if (notepad == null)
//                {
//                    Debug.WriteLine("Notepad is not running.");
//                    return;
//                }

//                // Получаем элемент TextBox в Notepad
//                var edit = notepad.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document));
//                if (edit == null)
//                {
//                    Debug.WriteLine("Cannot find TextBox component in Notepad.");
//                    return;
//                }

//                // Получаем текст из TextBox
//                var textPattern = edit.GetCurrentPattern(TextPattern.Pattern) as TextPattern;
//                if (textPattern == null)
//                {
//                    Debug.WriteLine("The text pattern is not available.");
//                    return;
//                }

//                var text = textPattern.DocumentRange.GetText(-1);

//                // Обновляем TextBox на нашей форме из потока UI
//                textBox1.Invoke(new Action(() =>
//                {
//                    textBox1.Text = text;
//                    textBox1.SelectionStart = textBox1.Text.Length;
//                    textBox1.ScrollToCaret();
//                }));
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"An error occurred: {ex.Message}");
//            }
//        }

//        private IntPtr GetNotepadHandle()
//        {
//            Process[] processes = Process.GetProcessesByName("notepad");
//            if (processes.Length > 0)
//            {
//                return processes[0].MainWindowHandle;
//            }
//            return IntPtr.Zero;
//        }
//        private void Form1_Load(object sender, EventArgs e)
//        {

//        }
//        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

//        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
//        static extern int GetWindowTextLength(IntPtr hWnd);
//        [DllImport("user32.dll", SetLastError = true)]
//        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

//        [DllImport("user32.dll", SetLastError = true)]
//        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

//        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
//        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, StringBuilder lParam);

//        [DllImport("user32.dll")]
//        static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
//        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

//        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//        [return: MarshalAs(UnmanagedType.Bool)]
//        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

//        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

//        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//        private static extern IntPtr GetModuleHandle(string lpModuleName);
//        [DllImport("user32.dll")]
//        static extern IntPtr GetForegroundWindow();
//        [DllImport("user32.dll")]
//        public static extern int ToUnicodeEx(
//    uint wVirtKey,
//    uint wScanCode,
//    byte[] lpKeyState,
//    [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
//    StringBuilder pwszBuff,
//    int cchBuff,
//    uint wFlags,
//    IntPtr dwhkl);
//        [DllImport("user32.dll")]
//        public static extern IntPtr GetKeyboardLayout(uint idThread);
//        [DllImport("user32.dll", SetLastError = true)]
//        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
//        [DllImport("user32.dll")]
//        public static extern bool GetKeyboardState(byte[] lpKeyState);
//        [DllImport("user32.dll")]
//        static extern uint MapVirtualKey(uint uCode, uint uMapType);
//        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
//        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
//        [DllImport("user32.dll", CharSet = CharSet.Auto)]
//        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);


//        //public string GetCharsFromKeys(uint keys, bool shiftPressed, bool altGrPressed, IntPtr keyboardLayout, byte[] keyboardState)
//        //{
//        //    var buf = new StringBuilder(256);

//        //    if (shiftPressed)
//        //        keyboardState[(int)Keys.ShiftKey] = 0xff;

//        //    if (altGrPressed)
//        //    {
//        //        keyboardState[(int)Keys.ControlKey] = 0xff;
//        //        keyboardState[(int)Keys.Menu] = 0xff;
//        //    }

//        //    ToUnicodeEx(keys, 0, keyboardState, buf, 256, 0, keyboardLayout);
//        //    return buf.ToString();
//        //}
//        //private string GetTextFromNotepad()
//        //{
//        //    // Находим окно по классу и заголовку (может потребоваться адаптация под вашу локализацию Windows)
//        //    IntPtr notepad = FindWindow("Notepad", null);
//        //    if (notepad != IntPtr.Zero)
//        //    {
//        //        // Находим дочернее окно, которое фактически содержит текст (обычно "Edit" для Блокнота)
//        //        IntPtr edit = FindWindowEx(notepad, IntPtr.Zero, "Edit", null);
//        //        if (edit != IntPtr.Zero)
//        //        {
//        //            int textLength = SendMessage(edit, WM_GETTEXTLENGTH, 0, 0) + 1;
//        //            StringBuilder text = new StringBuilder(textLength);
//        //            SendMessage(edit, WM_GETTEXT, textLength, text);
//        //            return text.ToString();
//        //        }
//        //    }
//        //    return null;
//        //}

//        private void Form1_Leave(object sender, EventArgs e)
//        {

//        }

//        private void Form1_FormClosed_1(object sender, FormClosedEventArgs e)
//        {
//            UnhookWindowsHookEx(_hookID);
//        }
//    }
//}
