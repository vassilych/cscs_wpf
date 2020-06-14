using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Xml;

namespace WpfCSCS
{
    public partial class SpecialModalWindow
    {
        public enum MODE { NORMAL, MODAL, SPECIAL_MODAL };

        [DllImport("user32.dll")]
        static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public Action<bool?> ClosedCallBack;
        private bool? ModalDialogResult = null;

        public Window Instance { get; set; }

        public Window Owner { get; set; }

        public MODE Mode { get; set; }

        public SpecialModalWindow(string filename, MODE mode = MODE.NORMAL, Window owner = null)
        {
            Mode = mode;
            Owner = owner;
            Instance = CreateWindow(filename);

            Random rnd = new Random();
            var inst = filename + "_" + rnd.Next(1000);
            Instance.Tag = inst;

            var controls = CSCS_GUI.CacheControls(Instance, true);
            foreach (var entry in controls)
            {
                CSCS_GUI.AddActions(entry);
            }

            Instance.SourceInitialized += Win_SourceInitialized;
            Instance.Activated += Win_Activated;
            Instance.Loaded += Win_Loaded;
            Instance.ContentRendered += Win_ContentRendered;

            Instance.Closing += Win_Closing;
            Instance.Deactivated += Win_Deactivated;
            Instance.Closed += Win_Closed;

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

        public void Show()
        {
            if (Mode == MODE.NORMAL)
            {
                Instance.Show();
            }
            else
            {
                Instance.ShowDialog();
            }
        }

        private void Win_SourceInitialized(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnInit";
            Interpreter.Run(funcName, new Variable(win.Tag), Variable.EmptyInstance, Variable.EmptyInstance, ChainFunction.GetScript(win));
            Instance.SourceInitialized -= Win_SourceInitialized;
        }

        private void Win_Activated(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnActivated";
            Interpreter.Run(funcName, new Variable(win.Tag), Variable.EmptyInstance, Variable.EmptyInstance, ChainFunction.GetScript(win));
            Instance.Activated -= Win_Activated;
        }

        public void Win_Opened(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnOpen";
            Interpreter.Run(funcName, new Variable(win.Tag), Variable.EmptyInstance, Variable.EmptyInstance, ChainFunction.GetScript(win));
        }

        private void Win_Loaded(object sender, RoutedEventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnStart";
            CSCS_GUI.RunScript(funcName, win, new Variable(win.Tag));
            Instance.Loaded -= Win_Loaded;
        }

        private void Win_ContentRendered(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnDisplay";
            CSCS_GUI.RunScript(funcName, win, new Variable(win.Tag));
            Instance.ContentRendered -= Win_ContentRendered;
            if (Owner != null && Mode != MODE.NORMAL)
            {
                win.Owner = Owner;
            }
        }

        private void Win_Deactivated(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnDeactivated";
            CSCS_GUI.RunScript(funcName, win, new Variable(win.Tag));
            Instance.Deactivated -= Win_Deactivated;
        }

        private void Win_Closed(object sender, EventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnClose";
            CSCS_GUI.RunScript(funcName, win, new Variable(win.Tag));

            Instance.Closed -= Win_Closed;
            Instance.Close();
            Instance = null;
        }

        private void Win_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Window win = sender as Window;
            var funcName = Path.GetFileNameWithoutExtension(win.Tag.ToString()) + "_OnClosing";
            var result = CSCS_GUI.RunScript(funcName, win, new Variable(win.Tag));
            e.Cancel = result != null && result.AsBool();
            if (e.Cancel)
            {
                return;
            }

            NewWindowFunction.RemoveWindow(win);
            Instance.Closing -= Win_Closing;
            if (Mode == MODE.SPECIAL_MODAL && Instance.Owner != null)
            {
                IntPtr handle = (new System.Windows.Interop.WindowInteropHelper(Instance)).Handle;
                IntPtr ownerhandle = (new System.Windows.Interop.WindowInteropHelper(Instance.Owner)).Handle;
                EnableWindow(handle, false);
                EnableWindow(ownerhandle, true);
                ClosedCallBack(ModalDialogResult);
            }
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
}
