using HarfBuzzSharp;
using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml;

namespace WpfCSCS
{
    public partial class SpecialWindow
    {
        static Dictionary<Window, SpecialWindow> s_windowCache = new Dictionary<Window, SpecialWindow>();

        public bool? DialogResult { get; set; }
        public enum MODE { NORMAL, MODAL, SPECIAL_MODAL };

        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


        private const uint MF_BYCOMMAND = 0x00000000;
        private const int WS_MAXIMIZEBOX = 0x10000; //maximize button
        private const int WS_MINIMIZEBOX = 0x20000; //minimize button
        private const int WS_SYSMENU = 0x80000;

        private const uint MF_GRAYED = 0x00000001;
        private const uint MF_ENABLED = 0x00000000;
        private const uint SC_CLOSE = 0xF060;
        private const uint SC_MINIMIZE = 0xF020;
        private const uint SC_MAXIMIZE = 0xF030;

        private const int GWL_STYLE = -16;

        private Window Owner { get; set; }

        private SpecialWindow SpecOwner { get; set; }

        private WindowStyle OwnerStyle { get; set; }
        private int OwnerWindowlong { get; set; }
        private bool IsDisabled { get; set; }

        public Action<bool?> ClosedCallBack;
        private bool? ModalDialogResult = null;

        public Window Instance { get; set; }

        public MODE Mode { get; set; }

        public CSCS_GUI Gui { get; set; }

        public string ID { get; set; }

        public bool IsMain {
            get
            {
                return CSCS_GUI.MainWindow == Instance;
            }
            set
            {
                if (value)
                {
                    CSCS_GUI.MainWindow = Instance;
                }
                else if (CSCS_GUI.MainWindow == Instance)
                {
                    CSCS_GUI.MainWindow = null;
                }
            }
        }

        private void showProperties(object obj)
        {
            //IInputElement focusedControl = FocusManager.GetFocusedElement();
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
            
            string toDisplay = "";
            if (elementWithFocus is FrameworkElement)
            {
                //Name
                //?
                toDisplay += "Widget name: " + "(not implemented)"; //(elementWithFocus as FrameworkElement).Name + "(?)";

                var varName = (elementWithFocus as FrameworkElement).DataContext?.ToString();

                //DataContext
                toDisplay += "\nBound variable name: " + varName;

                if(Gui.DEFINES.TryGetValue(varName.ToLower(), out DefineVariable defVar))
                {
                    //VarType and VarSize
                    toDisplay += "\nBound variable type: " + defVar.DefType.ToLower();
                    toDisplay += "\nBound variable size: " + defVar.Size;
                }
                else
                {
                    toDisplay += "(not DEFINEd)";
                }
                
            }

            if (toDisplay.Length > 0)
                MessageBox.Show(toDisplay);
        }

        public SpecialWindow(CSCS_GUI gui, string filename, MODE mode = MODE.NORMAL, Window owner = null)
        {
            Mode = mode;
            Owner = owner;
            SpecOwner = GetInstance(owner);
            OwnerStyle = owner == null ? WindowStyle.None : owner.WindowStyle;
            Gui = gui;
            Instance = CreateWindow(filename);

            
            var ib = new InputBinding(
                new KeyCommand((object arg1) => { return true; }, showProperties),
                new KeyGesture(Key.F9, ModifierKeys.Shift));
            Instance.InputBindings.Add(ib);


            IsMain = CSCS_GUI.MainWindow == null;
            if (!IsMain && mode != MODE.NORMAL)
            {
                Instance.Owner = owner != null ? owner : CSCS_GUI.MainWindow;
            }
            s_windowCache[Instance] = this;

            Random rnd = new Random();
            var inst = filename + "_" + rnd.Next(10000);
            Instance.Tag = ID = inst;

            Instance.SourceInitialized += Win_SourceInitialized;
            Instance.Activated += Win_Activated;
            Instance.Loaded += Win_Loaded;
            Instance.Unloaded += Win_Unloaded;
            Instance.ContentRendered += Win_ContentRendered;

            Instance.Closing += Win_Closing;
            Instance.Deactivated += Win_Deactivated;
            Instance.Closed += Win_Closed;

            Instance.StateChanged += Window_StateChanged;

            if (Mode == MODE.SPECIAL_MODAL && Owner != null)
            {
                bool? ModalDialogResult = null;
                IntPtr handleOwner = (new System.Windows.Interop.WindowInteropHelper(Owner)).Handle;
                ClosedCallBack += new Action<bool?>(p => { ModalDialogResult = p; });
                WindowInteropHelper helper = new WindowInteropHelper(Instance);
                EnableWindow(handleOwner, false);

                IntPtr handle = (new System.Windows.Interop.WindowInteropHelper(Instance)).Handle;
                EnableWindow(handle, true);
                SetForegroundWindow(handle);
            }

            Win_Opened(Instance, EventArgs.Empty);
        }

        public static SpecialWindow GetInstance(Window win)
        {
            if (win == null || !s_windowCache.TryGetValue(win, out SpecialWindow spec))
            {
                return null;
            }
            return spec;
        }

        public static bool RemoveInstance(Window win)
        {
            var result = s_windowCache.Remove(win);
            return result;
        }

        public static void ShowHideButtons(Window win, bool show = true)
        {
            IntPtr hwnd = new WindowInteropHelper(win).Handle;
            IntPtr hMenu = GetSystemMenu(hwnd, false);
            if (hMenu != IntPtr.Zero)
            {
                EnableMenuItem(hMenu, SC_CLOSE, show ? MF_BYCOMMAND : MF_BYCOMMAND | MF_GRAYED);
                win.ResizeMode = !show ? ResizeMode.NoResize : ResizeMode.CanResize;
            }

            var windowlong = GetWindowLong(hwnd, GWL_STYLE);
            //SetWindowLong(hwnd, GWL_STYLE, show ? windowlong | WS_SYSMENU : windowlong & ~WS_SYSMENU);
            SetWindowLong(hwnd, GWL_STYLE, show ? windowlong | WS_SYSMENU | WS_MAXIMIZEBOX | WS_MINIMIZEBOX :
                windowlong & ~WS_SYSMENU & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        bool m_minimizing;
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (m_minimizing)
            {
                return;
            }
            Window win = sender as Window;
            SpecialWindow inst = GetInstance(win);
            if (inst == null || !inst.IsMain || win.WindowState != WindowState.Minimized)
            {
                return;
            }
            m_minimizing = true;
            foreach (var entry in s_windowCache)
            {
                if (entry.Key == sender)
                {
                    continue;
                }
                entry.Key.WindowState = WindowState.Minimized;
            }
            m_minimizing = false;
        }

        private void Win_SourceInitialized(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnInit";
            Gui.Interpreter.Run(funcName, new Variable(win.Tag), Variable.EmptyInstance, Variable.EmptyInstance,
                Gui.GetScript(win));
            Instance.SourceInitialized -= Win_SourceInitialized;
        }

        private void Win_Activated(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnActivated";
            Gui.Interpreter.Run(funcName, new Variable(win.Tag), Variable.EmptyInstance, Variable.EmptyInstance,
                Gui.GetScript(win));
            Instance.Activated -= Win_Activated;
        }

        public void Win_Opened(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnOpen";
            Gui.Interpreter.Run(funcName, new Variable(win.Tag), Variable.EmptyInstance, Variable.EmptyInstance,
                Gui.GetScript(win));
        }

        private void Win_Loaded(object sender, RoutedEventArgs e)
        {
            Window win = sender as Window;
            Gui.AddActions(win, true);

            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnStart";
            Gui.RunScript(funcName, win, new Variable(win.Tag));
            Instance.Loaded -= Win_Loaded;
            
            if (Mode == MODE.MODAL && Owner != null)
            {
                Owner.IsEnabled = false;
                Owner.WindowStyle = WindowStyle.SingleBorderWindow;
                ShowHideButtons(Owner, false);
                if (SpecOwner != null)
                {
                    SpecOwner.IsDisabled = true;
                }
                //Owner.Hide();
            }
        }

        private void Win_Unloaded(object sender, RoutedEventArgs e)
        {
            Window win = sender as Window;
            Gui.AddActions(win, true);

            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnUnload";
            Gui.RunScript(funcName, win, new Variable(win.Tag));

            if (Mode == MODE.MODAL && Owner != null)
            {
                Owner.IsEnabled = true;
                Owner.WindowStyle = OwnerStyle;
                //Owner.Show();
                ShowHideButtons(Owner, true);
                if (SpecOwner != null)
                {
                    SpecOwner.IsDisabled = false;
                }
                Owner.Focus();
            }
        }

        private void Win_ContentRendered(object sender, EventArgs e)
        {
            Window win = sender as Window;
            if (win == null || Instance == null)
            {
                return;
            }
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnDisplay";
            Gui.RunScript(funcName, win, new Variable(win.Tag));
            Instance.ContentRendered -= Win_ContentRendered;
            if (Owner != null && Mode != MODE.NORMAL)
            {
                try
                {
                    win.Owner = Owner;
                }
                catch { }
            }
        }

        private void Win_Deactivated(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnDeactivated";
            Gui.RunScript(funcName, win, new Variable(win.Tag));
            Instance.Deactivated -= Win_Deactivated;
        }

        private void Win_Closed(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnClose";
            Gui.RunScript(funcName, win, new Variable(win.Tag));

            if (IsMain)
            {
                Environment.Exit(0);
            }

            Instance.Closed -= Win_Closed;
            Instance.Close();
            Instance = null;

            var parent = Gui.GetParentWindow(win.Tag.ToString());
            parent?.Focus();

            NewWindowFunction.RemoveWindow(win);
            //CSCS_GUI.Window2File.Remove(win);
            //CSCS_GUI.File2Window.Remove(Gui.Script.Filename);
        }

        private void Win_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Window win = sender as Window;
            if (IsDisabled)
            {
                e.Cancel = true;
                return;
            }

            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnClosing";
            var result = Gui.RunScript(funcName, win, new Variable(win.Tag));
            e.Cancel = result != null && result.AsBool();
            if (e.Cancel)
            {
                return;
            }

            Instance.Closing -= Win_Closing;
            if (Mode == MODE.SPECIAL_MODAL && Instance.Owner != null)
            {
                IntPtr handle = (new System.Windows.Interop.WindowInteropHelper(Instance)).Handle;
                IntPtr ownerhandle = (new System.Windows.Interop.WindowInteropHelper(Instance.Owner)).Handle;
                EnableWindow(handle, false);
                EnableWindow(ownerhandle, true);
                ClosedCallBack(ModalDialogResult);
            }
            Owner?.Focus();
        }

        public static Window CreateWindow(string filename)
        {
            var text = File.ReadAllText(filename);
            XmlReader xmlReader = XmlReader.Create(new StringReader(text));
            var newInstance = System.Windows.Markup.XamlReader.Load(xmlReader) as Window;
            if (newInstance == null)
            {
                throw new ArgumentException("Couldn't create window [" + filename + "]");
            }
            return newInstance;
        }
    }

    public class KeyCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        public KeyCommand(Predicate<object> canExecute, Action<object> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

}
