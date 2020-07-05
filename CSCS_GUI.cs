using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using System.Reflection;
using SplitAndMerge;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using System.Globalization;

namespace SplitAndMerge
{
    public partial class Constants
    {
        public const string DEFINE = "define";
        public const string MSG = "msg";
        public const string SET_OBJECT = "set_object";
        public const string QUIT = "quit";

        public const string CHAIN = "chain";
        public const string PARAM = "param";
        public const string WITH = "with";
        public const string NEWRUNTIME = "newruntime";
    }
}

namespace WpfCSCS
{
    public class CSCS_GUI
    {
        public static App TheApp { get; set; }
        public static Window MainWindow { get; set; }
        public static bool ChangingBoundVariable { get; set; }
        public static string RequireDEFINE { get; set; }

        public static Dictionary<string, Control> Controls { get; set; } = new Dictionary<string, Control>();
        public static Dictionary<Control, Window> Control2Window { get; set; } = new Dictionary<Control, Window>();
        //public static Action<string, string> OnWidgetClick;

        static Dictionary<string, string> s_actionHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_preActionHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_postActionHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_keyDownHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_keyUpHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_textChangedHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_selChangedHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_mouseHoverHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_dateSelectedHandlers = new Dictionary<string, string>();

        static Dictionary<string, Variable> s_boundVariables = new Dictionary<string, Variable>();
        //static Dictionary<string, TabPage> s_tabPages           = new Dictionary<string, TabPage>();
        //static TabControl s_tabControl;

        public static Dictionary<string, List<Variable>> DEFINES { get; set; } =
                  new Dictionary<string, List<Variable>>();

        public static Dictionary<string, Dictionary<string, bool>> s_varExists =
            new Dictionary<string, Dictionary<string, bool>>();

        public class SpecialObject : ScriptObject
        {
            static List<string> s_properties = new List<string> {
            "Name", "Size", "Type", "Value", "Dec", "Array", "Up", "Dup"
        };

            public SpecialObject(string name, Variable value, string type = "", int size = 0, int dec = 3, bool up = false, Variable dup = null)
            {
                Name = name;
                Value = value;
                Type = type;
                Size = size;
                Dec = dec;
                Up = up;
                Dup = dup;
            }

            public string Name { get; set; }
            public string Type { get; set; }
            public int Size { get; set; }
            public int Dec { get; set; }
            public int Array { get; set; }
            public bool Up { get; set; }
            public Variable Value { get; set; }
            public Variable Dup { get; set; }

            public virtual List<string> GetProperties()
            {
                return s_properties;
            }

            public virtual Task<Variable> GetProperty(string sPropertyName, List<Variable> args = null, ParsingScript script = null)
            {
                sPropertyName = Variable.GetActualPropertyName(sPropertyName, GetProperties());
                switch (sPropertyName)
                {
                    case "Name": return Task.FromResult(new Variable(Name));
                    case "Size": return Task.FromResult(new Variable(Size));
                    case "Type": return Task.FromResult(new Variable(Type));
                    case "Dup": return Task.FromResult(Dup);
                    case "Value": return Task.FromResult(Value);
                    case "Array": return Task.FromResult(new Variable(Array));
                    case "Dec": return Task.FromResult(new Variable(Dec));
                    case "Up": return Task.FromResult(new Variable(Up));
                    default:
                        return Task.FromResult(Variable.EmptyInstance);
                }
            }

            public virtual Task<Variable> SetProperty(string sPropertyName, Variable argValue)
            {
                sPropertyName = Variable.GetActualPropertyName(sPropertyName, GetProperties());
                switch (sPropertyName)
                {
                    case "Name":
                        Name = argValue.AsString();
                        return Task.FromResult(argValue);
                    case "Size":
                        Size = argValue.AsInt();
                        return Task.FromResult(argValue);
                    case "Type":
                        Type = argValue.AsString();
                        return Task.FromResult(argValue);
                    case "Array":
                        Array = argValue.AsInt();
                        return Task.FromResult(argValue);
                    case "Dec":
                        Dec = argValue.AsInt();
                        return Task.FromResult(argValue);
                    case "Up":
                        Up = argValue.AsBool();
                        return Task.FromResult(argValue);
                    case "Dup":
                        Dup = argValue;
                        return Task.FromResult(argValue);
                    case "Value":
                        Value = argValue;
                        return Task.FromResult(argValue);
                    default: return Task.FromResult(Variable.EmptyInstance);
                }
            }
        }

        public static void Init()
        {
            ParserFunction.RegisterFunction("#MAINMENU", new MAINMENUcommand());
            ParserFunction.RegisterFunction("#WINFORM", new WINFORMcommand(true));

            ParserFunction.RegisterFunction(Constants.MSG, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.DEFINE, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.SET_OBJECT, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.CHAIN, new ChainFunction(false));
            ParserFunction.RegisterFunction(Constants.PARAM, new ChainFunction(true));
            ParserFunction.RegisterFunction(Constants.QUIT, new QuitStatement());

            ParserFunction.RegisterFunction(Constants.WITH, new ConstantsFunction());
            ParserFunction.RegisterFunction(Constants.NEWRUNTIME, new ConstantsFunction());

            ParserFunction.RegisterFunction("OpenFile", new OpenFileFunction(false));
            ParserFunction.RegisterFunction("OpenFileContents", new OpenFileFunction(true));
            ParserFunction.RegisterFunction("SaveFile", new SaveFileFunction());

            ParserFunction.RegisterFunction("ShowWidget", new ShowHideWidgetFunction(true));
            ParserFunction.RegisterFunction("HideWidget", new ShowHideWidgetFunction(false));

            ParserFunction.RegisterFunction("GetText", new GetTextWidgetFunction());
            ParserFunction.RegisterFunction("SetText", new SetTextWidgetFunction());
            ParserFunction.RegisterFunction("AddWidgetData", new AddWidgetDataFunction());
            ParserFunction.RegisterFunction("SetWidgetOptions", new SetWidgetOptionsFunction());
            ParserFunction.RegisterFunction("GetSelected", new GetSelectedFunction());
            ParserFunction.RegisterFunction("SetBackgroundColor", new SetColorFunction(true));
            ParserFunction.RegisterFunction("SetForegroundColor", new SetColorFunction(false));

            ParserFunction.RegisterFunction("BindSQL", new BindSQLFunction());
            ParserFunction.RegisterFunction("MessageBox", new MessageBoxFunction());
            ParserFunction.RegisterFunction("SendToPrinter", new PrintFunction());

            ParserFunction.RegisterFunction("AddMenuItem", new AddMenuEntryFunction(false));
            ParserFunction.RegisterFunction("AddMenuSeparator", new AddMenuEntryFunction(true));
            ParserFunction.RegisterFunction("RemoveMenu", new RemoveMenuFunction());

            ParserFunction.RegisterFunction("RunOnMain", new RunOnMainFunction());
            ParserFunction.RegisterFunction("RunExec", new RunExecFunction());
            ParserFunction.RegisterFunction("RunScript", new RunScriptFunction());

            ParserFunction.RegisterFunction("CheckVATNumber", new CheckVATFunction());
            ParserFunction.RegisterFunction("GetVATName", new CheckVATFunction(CheckVATFunction.MODE.NAME));
            ParserFunction.RegisterFunction("GetVATAddress", new CheckVATFunction(CheckVATFunction.MODE.ADDRESS));

            ParserFunction.RegisterFunction("CreateWindow", new NewWindowFunction(NewWindowFunction.MODE.NEW));
            ParserFunction.RegisterFunction("CloseWindow", new NewWindowFunction(NewWindowFunction.MODE.DELETE));
            ParserFunction.RegisterFunction("ShowWindow", new NewWindowFunction(NewWindowFunction.MODE.SHOW));
            ParserFunction.RegisterFunction("HideWindow", new NewWindowFunction(NewWindowFunction.MODE.HIDE));
            ParserFunction.RegisterFunction("NextWindow", new NewWindowFunction(NewWindowFunction.MODE.NEXT));
            ParserFunction.RegisterFunction("ModalWindow", new NewWindowFunction(NewWindowFunction.MODE.MODAL));
            ParserFunction.RegisterFunction("SetMainWindow", new NewWindowFunction(NewWindowFunction.MODE.SET_MAIN));
            ParserFunction.RegisterFunction("UnsetMainWindow", new NewWindowFunction(NewWindowFunction.MODE.UNSET_MAIN));

            Constants.FUNCT_WITH_SPACE.Add("SetText");
            Constants.FUNCT_WITH_SPACE.Add(Constants.DEFINE);
            Constants.FUNCT_WITH_SPACE.Add(Constants.MSG);
            Constants.FUNCT_WITH_SPACE.Add(Constants.SET_OBJECT);
            Constants.FUNCT_WITH_SPACE.Add(Constants.CHAIN);
            Constants.FUNCT_WITH_SPACE.Add(Constants.PARAM);

            Interpreter.Instance.OnOutput += Print;
            ParserFunction.OnVariableChange += OnVariableChange;

            Precompiler.AddNamespace("using WpfCSCS;");
            Precompiler.AddNamespace("using System.Windows;");
            Precompiler.AddNamespace("using System.Windows.Controls;");
            Precompiler.AddNamespace("using System.Windows.Controls.Primitives;");
            Precompiler.AddNamespace("using System.Windows.Data;");
            Precompiler.AddNamespace("using System.Windows.Documents;");
            Precompiler.AddNamespace("using System.Windows.Input;");
            Precompiler.AddNamespace("using System.Windows.Media;");

            RequireDEFINE = App.GetConfiguration("Require_Define", "*");
        }

        public static string GetWidgetBindingName(Control widget)
        {
            var widgetName = widget == null || widget.DataContext == null ? "" : widget.DataContext.ToString();
            return widgetName;
        }

        static void OnVariableChange(string name, Variable newValue, bool exists)
        {
            if (ChangingBoundVariable)
            {
                return;
            }
            if (!exists && (RequireDEFINE == "*" || name.StartsWith(RequireDEFINE)))
            {
                throw new ArgumentException("Variable [" + name + "] must be defined with DEFINE function first.");
            }

            var widgetName = name.ToLower();
            if (!s_boundVariables.TryGetValue(widgetName, out _))
            {
                return;
            }

            var widget = GetWidget(widgetName);
            var text = newValue.AsString();

            SetTextWidgetFunction.SetText(widget, text);
            s_boundVariables[widgetName] = newValue;
        }

        static void UpdateVariable(Control widget, Variable newValue)
        {
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrEmpty(widgetName))
            {
                return;
            }
            ChangingBoundVariable = true;
            ParserFunction.AddGlobalOrLocalVariable(widgetName,
                                        new GetVarFunction(newValue));
            ChangingBoundVariable = false;
        }

        static void Print(object sender, OutputAvailableEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine(e.Output);
        }

        public static void AddActions(Window win, bool force = false, ParsingScript script = null)
        {
            var controls = CacheControls(win, force);
            foreach (var entry in controls)
            {
                AddWidgetActions(entry);
            }
        }

        public static Variable RunScript(string funcName, Window win, Variable arg1, Variable arg2 = null)
        {
            CustomFunction customFunction = ParserFunction.GetFunction(funcName, null) as CustomFunction;
            if (customFunction != null)
            {
                List<Variable> args = new List<Variable>();
                args.Add(arg1);
                args.Add(arg2 != null ? arg2 : Variable.EmptyInstance);

                var script = ChainFunction.GetScript(win);
                if (script != null && script.StackLevel != null)
                {
                    foreach (var item in script.StackLevel.Variables)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Key))
                        {
                            var func = item.Value as GetVarFunction;
                            func.Value.ParamName = item.Key;
                            args.Add(func.Value);
                        }
                    }
                }
                return Interpreter.Run(customFunction, args, script);
            }
            return Variable.EmptyInstance;
        }

        public static bool AddBinding(string name, Control widget)
        {
            var text = GetTextWidgetFunction.GetText(widget);
            Variable baseValue = new Variable(text);
            ParserFunction.AddGlobal(name, new GetVarFunction(baseValue), false /* not native */);

            s_boundVariables[name.ToLower()] = Variable.EmptyInstance;
            return true;
        }

        public static bool AddActionHandler(string name, string action, Control widget)
        {
            var clickable = widget as ButtonBase;
            if (clickable == null)
            {
                return false;
            }
            s_actionHandlers[name] = action;
            clickable.Click += new RoutedEventHandler(Widget_Click);
            return true;
        }
        public static bool AddPreActionHandler(string name, string action, Control widget)
        {
            s_preActionHandlers[name] = action;
            if (widget is ComboBox)
            {
                var combo = widget as ComboBox;
                combo.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(Widget_PreClick);
                return true;
            }
            widget.MouseDown += new MouseButtonEventHandler(Widget_PreClick);
            return true;
        }
        public static bool AddPostActionHandler(string name, string action, Control widget)
        {
            s_postActionHandlers[name] = action;
            if (widget is ComboBox)
            {
                var combo = widget as ComboBox;
                combo.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(Widget_PostClick);
                return true;
            }
            widget.MouseUp += new MouseButtonEventHandler(Widget_PostClick);
            return true;
        }

        public static bool AddKeyDownHandler(string name, string action, Control widget)
        {
            s_keyDownHandlers[name] = action;
            widget.KeyDown += new KeyEventHandler(Widget_KeyDown);
            return true;
        }
        public static bool AddKeyUpHandler(string name, string action, Control widget)
        {
            s_keyUpHandlers[name] = action;
            widget.KeyUp += new KeyEventHandler(Widget_KeyUp);
            return true;
        }
        public static bool AddTextChangedHandler(string name, string action, Control widget)
        {
            var textable = widget as TextBoxBase;
            if (textable == null)
            {
                return false;
            }
            s_textChangedHandlers[name] = action;
            textable.TextChanged += new TextChangedEventHandler(Widget_TextChanged);

            return true;
        }
        public static bool AddSelectionChangedHandler(string name, string action, Control widget)
        {
            var sel = widget as Selector;
            if (sel == null)
            {
                return false;
            }
            s_selChangedHandlers[name] = action;
            sel.SelectionChanged += new SelectionChangedEventHandler(Widget_SelectionChanged);
            return true;
        }
        public static bool AddDateChangedHandler(string name, string action, Control widget)
        {
            var datePicker = widget as DatePicker;
            if (datePicker == null)
            {
                return false;
            }
            s_dateSelectedHandlers[name] = action;
            datePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;

            return true;
        }

        private static void ValueUpdated(string funcName, string widgetName, Control widget, Variable newValue)
        {
            UpdateVariable(widget, newValue);
            Control2Window.TryGetValue(widget, out Window win);
            RunScript(funcName, win, new Variable(widgetName), newValue);
        }

        private static void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            Control widget = sender as Control;
            var widgetName = GetWidgetBindingName(widget);
            var picker = sender as DatePicker;
            DateTime? date = picker?.SelectedDate;
            if (string.IsNullOrWhiteSpace(widgetName) || date == null ||
               !s_dateSelectedHandlers.TryGetValue(widgetName, out string funcName))
            {
                return;
            }

            ValueUpdated(funcName, widgetName, widget, new Variable(date.Value.ToString("yyyy/MM/dd")));
        }

        public static bool AddMouseHoverHandler(string name, string action, Control widget)
        {
            s_mouseHoverHandlers[name] = action;
            widget.MouseEnter += new MouseEventHandler(Widget_Hover);
            return true;
        }

        private static void Widget_Click(object sender, RoutedEventArgs e)
        {
            Control widget = sender as Control;
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrEmpty(widgetName))
            {
                return;
            }

            string funcName;
            if (!s_actionHandlers.TryGetValue(widgetName, out funcName))
            {
                return;
            }

            Variable result = null;
            if (widget is CheckBox)
            {
                var checkBox = widget as CheckBox;
                var val = checkBox.IsChecked == true ? true : false;
                result = new Variable(val);
            }
            else
            {
                result = new Variable(widgetName);
            }

            ValueUpdated(funcName, widgetName, widget, result);
        }

        private static void Widget_PreClick(object sender, MouseButtonEventArgs e)
        {
            Control widget = sender as Control;
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrWhiteSpace(widgetName) || e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            string funcName;
            if (s_preActionHandlers.TryGetValue(widgetName, out funcName))
            {
                var arg = GetTextWidgetFunction.GetText(widget);
                Control2Window.TryGetValue(widget, out Window win);
                Interpreter.Run(funcName, new Variable(widgetName), new Variable(arg), Variable.EmptyInstance, ChainFunction.GetScript(win));
            }
        }

        private static void Widget_PostClick(object sender, MouseButtonEventArgs e)
        {
            Control widget = sender as Control;
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrWhiteSpace(widgetName) || e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            string funcName;
            if (s_postActionHandlers.TryGetValue(widgetName, out funcName))
            {
                var arg = GetTextWidgetFunction.GetText(widget);
                Control2Window.TryGetValue(widget, out Window win);
                Interpreter.Run(funcName, new Variable(widgetName), new Variable(arg),
                    Variable.EmptyInstance, ChainFunction.GetScript(win));
            }
        }

        private static void Widget_KeyDown(object sender, KeyEventArgs e)
        {
            Control widget = sender as Control;
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrWhiteSpace(widgetName))
            {
                return;
            }

            string funcName;
            if (s_keyDownHandlers.TryGetValue(widgetName, out funcName))
            {
                Control2Window.TryGetValue(widget, out Window win);
                Interpreter.Run(funcName, new Variable(widgetName),
                    new Variable(((char)e.Key).ToString()),
                    Variable.EmptyInstance, ChainFunction.GetScript(win));
            }
        }
        private static void Widget_KeyUp(object sender, KeyEventArgs e)
        {
            Control widget = sender as Control;
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrWhiteSpace(widgetName))
            {
                return;
            }

            string funcName;
            if (s_keyUpHandlers.TryGetValue(widgetName, out funcName))
            {
                Control2Window.TryGetValue(widget, out Window win);
                Interpreter.Run(funcName, new Variable(widgetName),
                    new Variable(((char)e.Key).ToString()),
                    Variable.EmptyInstance, ChainFunction.GetScript(win));
            }
        }

        private static void Widget_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxBase widget = sender as TextBoxBase;
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrWhiteSpace(widgetName))
            {
                return;
            }

            var text = GetTextWidgetFunction.GetText(widget);
            UpdateVariable(widget, text);

            string funcName;
            if (s_textChangedHandlers.TryGetValue(widgetName, out funcName))
            {
                Control2Window.TryGetValue(widget, out Window win);
                Interpreter.Run(funcName, new Variable(widgetName), text,
                    Variable.EmptyInstance, ChainFunction.GetScript(win));
            }
        }

        private static void Widget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Control widget = sender as Selector;
            var widgetName = GetWidgetBindingName(widget);
            if (s_selChangedHandlers.TryGetValue(widgetName, out string funcName))
            {
                var item = e.AddedItems.Count > 0 ? e.AddedItems[0].ToString() : e.RemovedItems.Count > 0 ? e.RemovedItems[0].ToString() : "";
                Control2Window.TryGetValue(widget, out Window win);
                Interpreter.Run(funcName, new Variable(widgetName), new Variable(item),
                    Variable.EmptyInstance, ChainFunction.GetScript(win));
            }
        }

        private static void Widget_Hover(object sender, MouseEventArgs e)
        {
            Control widget = sender as Control;
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrWhiteSpace(widgetName))
            {
                return;
            }

            if (s_mouseHoverHandlers.TryGetValue(widgetName, out string funcName))
            {
                Control2Window.TryGetValue(widget, out Window win);
                Interpreter.Run(funcName, new Variable(widgetName), new Variable(e.ToString()),
                    Variable.EmptyInstance, ChainFunction.GetScript(win));
            }
        }

        public static Control GetWidget(string name)
        {
            CacheControls(MainWindow);
            Control control;
            if (Controls.TryGetValue(name.ToLower(), out control))
            {
                return control;
            }
            return null;
        }

        public static List<Control> CacheControls(Window win, bool force = false)
        {
            List<Control> controls = new List<Control>();

            if ((!force && Controls.Count > 0) || win == null)
            {
                return controls;
            }

            var content = win.Content as Panel;
            if (content != null)
            {
                CacheChildren(content.Children.Cast<UIElement>().ToList(), controls, win);
            }
            return controls;
        }

        static void CacheChildren(List<UIElement> children, List<Control> controls, Window win)
        {
            foreach (var child in children)
            {
                if (child is Grid)
                {
                    var gridControl = child as Grid;
                    CacheChildren(gridControl.Children.Cast<UIElement>().ToList(), controls, win);
                }
                else if (child is TabControl)
                {
                    var tabControl = child as TabControl;
                    var count = VisualTreeHelper.GetChildrenCount(tabControl);
                    for (int i = 0; i < count; i++)
                    {
                        DependencyObject item = VisualTreeHelper.GetChild(tabControl, i);
                        if (item is Grid)
                        {
                            var tabGrid = item as Grid;
                            var count2 = VisualTreeHelper.GetChildrenCount(tabGrid);
                            for (int j = 0; j < count2; j++)
                            {
                                DependencyObject item2 = VisualTreeHelper.GetChild(tabGrid, j);
                                if (item2 is TabPanel)
                                {
                                    var tabPanel = item2 as TabPanel;
                                    var count3 = VisualTreeHelper.GetChildrenCount(tabPanel);
                                    for (int k = 0; k < count3; k++)
                                    {
                                        DependencyObject item3 = VisualTreeHelper.GetChild(tabPanel, k);
                                        if (item3 is TabItem)
                                        {
                                            var tabItem = item3 as TabItem;
                                            var content2 = tabItem.Content as Grid;
                                            foreach (var child2 in content2.Children)
                                            {
                                                CacheControl(child2 as Control, win, controls);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    CacheControl(child as Control, win, controls);
                    if (child is ItemsControl)
                    {
                        var parent = child as ItemsControl;
                        var items = parent.Items;
                        if (items != null && items.Count > 0)
                        {
                            CacheChildren(items.Cast<UIElement>().ToList(), controls, win);
                        }
                    }
                }
            }
        }

        public static void CacheControl(Control widget, Window win = null, List<Control> controls = null)
        {
            if (widget != null && widget.DataContext != null)
            {
                Controls[widget.DataContext.ToString().ToLower()] = widget;
                controls?.Add(widget);
                if (win != null)
                {
                    Control2Window[widget] = win;
                }
            }
        }
        public static void RemoveControl(Control widget)
        {
            widget.Visibility = Visibility.Hidden;
            Controls.Remove(widget.DataContext.ToString().ToLower());
        }

        public static void AddWidgetActions(Control widget)
        {
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrWhiteSpace(widgetName))
            {
                return;
            }

            string clickAction = widgetName + "@Clicked";
            string preClickAction = widgetName + "@PreClicked";
            string postClickAction = widgetName + "@PostClicked";
            string keyDownAction = widgetName + "@KeyDown";
            string keyUpAction = widgetName + "@KeyUp";
            string textChangeAction = widgetName + "@TextChange";
            string mouseHoverAction = widgetName + "@MouseHover";
            string selectionChangedAction = widgetName + "@SelectionChanged";
            string dateChangedAction = widgetName + "@DateChanged";

            AddActionHandler(widgetName, clickAction, widget);
            AddPreActionHandler(widgetName, preClickAction, widget);
            AddPostActionHandler(widgetName, postClickAction, widget);
            AddKeyDownHandler(widgetName, keyDownAction, widget);
            AddKeyUpHandler(widgetName, keyUpAction, widget);
            AddTextChangedHandler(widgetName, textChangeAction, widget);
            AddSelectionChangedHandler(widgetName, selectionChangedAction, widget);
            AddMouseHoverHandler(widgetName, mouseHoverAction, widget);
            AddDateChangedHandler(widgetName, dateChangedAction, widget);
            AddBinding(widgetName, widget);
        }

        public static string Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static Variable RunScript(string fileName, bool encode = false)
        {
            Init();

            if (encode)
            {
                EncodeFileFunction.EncodeDecode(fileName, false);
            }
            string script = Utils.GetFileContents(fileName);
            if (encode)
            {
                EncodeFileFunction.EncodeDecode(fileName, true);
            }

            Variable result = null;
            try
            {
                result = Interpreter.Instance.Process(script, fileName);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Exception: " + exc.Message);
                Console.WriteLine(exc.StackTrace);
                ParserFunction.InvalidateStacksAfterLevel(0);
                var onException = CustomFunction.Run(Constants.ON_EXCEPTION, new Variable("Global Scope"),
                                  new Variable(exc.Message), Variable.EmptyInstance);
                if (onException == null)
                {
                    throw;
                }
            }

            return result;
        }
    }

    class GetSelectedFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var widget = CSCS_GUI.GetWidget(widgetName);
            if (widget == null)
            {
                return Variable.EmptyInstance;
            }

            if (widget is DataGrid)
            {
                Variable selectedItems = new Variable(Variable.VarType.ARRAY);
                var dg = widget as DataGrid;
                var sel = dg.SelectedItems;
                int total = sel.Count;
                for (int i = 0; i < total; i++)
                {
                    var item = sel[i] as ExpandoObject;
                    var itemList = item.ToList();
                    selectedItems.AddVariable(new Variable(itemList[0].Value.ToString()));
                }
                return selectedItems;
            }

            return GetTextWidgetFunction.GetText(widget);
        }
    }

    class BindSQLFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var widget = CSCS_GUI.GetWidget(widgetName);
            if (widget == null)
            {
                return Variable.EmptyInstance;
            }
            var tableName = Utils.GetSafeString(args, 1);

            if (widget is DataGrid)
            {
                var dg = widget as DataGrid;
                dg.Items.Clear();
                dg.Columns.Clear();
                Variable columns = SQLColumnsFunction.GetColsData(tableName);
                for (int i = 0; i < columns.Tuple.Count; i += 2)
                {
                    string label = columns.Tuple[i].AsString();
                    DataGridTextColumn column = new DataGridTextColumn();
                    column.Header = label;
                    column.Binding = new Binding(label.Replace(' ', '_'));

                    dg.Columns.Add(column);
                }

                var query = "select * from " + tableName;
                var sqlResult = SQLQueryFunction.GetData(query, tableName);

                for (int i = 1; i < sqlResult.Tuple.Count; i++)
                {
                    var data = sqlResult.Tuple[i];
                    dynamic row = new ExpandoObject();
                    for (int j = 0; j < dg.Columns.Count; j++)
                    {
                        var column = dg.Columns[j].Header.ToString();
                        var val = data.Tuple.Count > j ? data.Tuple[j].AsString() : "";
                        ((IDictionary<String, Object>)row)[column.Replace(' ', '_')] = val;
                    }
                    dg.Items.Add(row);
                }
                return new Variable(sqlResult.Tuple.Count);
            }

            return Variable.EmptyInstance;
        }
    }

    public class RunExecFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            string execName = Utils.GetItem(script).AsString();
            var argsStr = Utils.GetBodyBetween(script, '\0', ')', Constants.END_STATEMENT);
            var args = argsStr.Replace(',', ' ');
            var result = RunExec(execName, args);
            return result;
        }

        public static Variable RunExec(string filename, string args)
        {
            var proc = System.Diagnostics.Process.Start(filename, args);
            return new Variable(proc.Id);
        }
    }

    public class RunOnMainFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            string funcName = Utils.GetToken(script, Constants.NEXT_OR_END_ARRAY);

            ParserFunction func = ParserFunction.GetFunction(funcName, script);
            Utils.CheckNotNull(funcName, func, script);

            Variable result = Variable.EmptyInstance;
            if (func is CustomFunction)
            {
                List<Variable> args = script.GetFunctionArgs();
                result = RunOnMainThread(func as CustomFunction, args);
            }
            else
            {
                var argsStr = Utils.GetBodyBetween(script, '\0', ')', Constants.END_STATEMENT);
                result = RunOnMainThread(func, argsStr);
            }
            return result;
        }

        public static object RunOnMainThread(Action action)
        {
            return Application.Current.Dispatcher.Invoke(action, null);
        }

        public static Variable RunOnMainThread(CustomFunction callbackFunction, List<Variable> args)
        {
            Variable result = Variable.EmptyInstance;
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                result = callbackFunction.Run(args);
            }));
            return result;
        }
        public static Variable RunOnMainThread(ParserFunction func, string argsStr)
        {
            Variable result = Variable.EmptyInstance;
            ParsingScript tempScript = new ParsingScript(argsStr);
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                result = func.GetValue(tempScript);
            }));
            return result;
        }
    }

    class PrintFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var text = Utils.GetSafeString(args, 0);
            var widget = CSCS_GUI.GetWidget(text);

            PrintDialog printDlg = new PrintDialog();
            if (printDlg.ShowDialog() != true)
            { // user cancelled printing.
                return new Variable("");
            }

            if (widget == null)
            {
                FlowDocument doc = new FlowDocument(new Paragraph(new Run(text)));
                IDocumentPaginatorSource idpSource = doc;
                printDlg.PrintDocument(idpSource.DocumentPaginator, "CSCS Printing.");

            }
            else
            {
                printDlg.PrintVisual(widget as Control, "Window Printing.");
            }

            return new Variable(printDlg.PrintQueue.FullName);
        }
    }

    class GetTextWidgetFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var widget = CSCS_GUI.GetWidget(widgetName);
            return GetText(widget);
        }

        public static Variable GetText(Control widget)
        {
            string result = "";
            if (widget is ContentControl)
            {
                var contentable = widget as ContentControl;
                result = contentable.Content.ToString();
            }
            else if (widget is CheckBox)
            {
                var checkBox = widget as CheckBox;
                result = checkBox.IsChecked.HasValue && checkBox.IsChecked.Value ? "true" : "false";
            }
            else if (widget is TextBox)
            {
                var textBox = widget as TextBox;
                result = textBox.Text;
            }
            else if (widget is RichTextBox)
            {
                var richTextBox = widget as RichTextBox;
                result = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
            }
            else if (widget is ComboBox)
            {
                var comboBox = widget as ComboBox;
                result = comboBox.Text;
            }
            else if (widget is DatePicker)
            {
                var datePicker = widget as DatePicker;
                result = datePicker.Text;
            }

            return new Variable(result);
        }
    }

    public class SetTextWidgetFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            var rest = script.Rest;
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var text = Utils.GetSafeString(args, 1);

            var widget = CSCS_GUI.GetWidget(widgetName);
            if (widget == null)
            {
                return Variable.EmptyInstance;
            }

            int index = widget is ComboBox && args[0].Type == Variable.VarType.NUMBER ? (int)args[0].Value : -1;
            var set = SetText(widget, text, index);

            return new Variable(set);
        }

        public static bool SetText(Control widget, string text, int index = -1)
        {
            var dispatcher = Application.Current.Dispatcher;
            if (widget is ComboBox)
            {
                var combo = widget as ComboBox;
                if (index < 0)
                {
                    index = 0;
                    foreach (var item in combo.Items)
                    {
                        if (item.ToString() == text)
                        {
                            break;
                        }
                        index++;
                    }
                }
                if (index >= 0 && index < combo.Items.Count)
                {
                    dispatcher.Invoke(new Action(() => {
                        combo.SelectedIndex = index;
                    }));
                }
            }
            else if (widget is CheckBox)
            {
                var checkBox = widget as CheckBox;
                dispatcher.Invoke(new Action(() => {
                    checkBox.IsChecked = text == "1" || text.ToLower() == "true";
                }));
            }
            else if (widget is ContentControl)
            {
                var contentable = widget as ContentControl;
                dispatcher.Invoke(new Action(() => {
                    contentable.Content = text;
                }));
            }
            else if (widget is TextBox)
            {
                var textBox = widget as TextBox;
                dispatcher.Invoke(new Action(() => {
                    textBox.Text = text;
                }));
            }
            else if (widget is RichTextBox)
            {
                var richTextBox = widget as RichTextBox;
                dispatcher.Invoke(new Action(() => {
                    richTextBox.Document.Blocks.Clear();
                    richTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
                }));
            }
            else if (widget is DatePicker && !string.IsNullOrWhiteSpace(text))
            {
                var datePicker = widget as DatePicker;
                var format = text.Length == 10 ? "yyyy/MM/dd" : text.Length == 8 ? "hh:mm:ss" :
                             text.Length == 12 ? "hh:mm:ss.fff" : "yyyy/MM/dd hh:mm:ss";
                dispatcher.Invoke(new Action(() => {
                    datePicker.SelectedDate = DateTime.ParseExact(text, format, CultureInfo.InvariantCulture);
                }));
            }
            else
            {
                return false;
            }
            return true;
        }
    }

    class MessageBoxFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var message = Utils.GetSafeString(args, 0);
            var caption = Utils.GetSafeString(args, 1, "Info");
            var answerType = Utils.GetSafeString(args, 2, "ok").ToLower();
            var messageType = Utils.GetSafeString(args, 3, "info").ToLower();

            MessageBoxButton buttons =
                answerType == "ok" ? MessageBoxButton.OK :
                answerType == "okcancel" ? MessageBoxButton.OKCancel :
                answerType == "yesno" ? MessageBoxButton.YesNo :
                answerType == "yesnocancel" ? MessageBoxButton.YesNoCancel : MessageBoxButton.OK;

            MessageBoxImage icon =
                messageType == "question" ? MessageBoxImage.Question :
                messageType == "info" ? MessageBoxImage.Information :
                messageType == "warning" ? MessageBoxImage.Warning :
                messageType == "error" ? MessageBoxImage.Error :
                messageType == "exclamation" ? MessageBoxImage.Exclamation :
                messageType == "stop" ? MessageBoxImage.Stop :
                messageType == "hand" ? MessageBoxImage.Hand :
                messageType == "asterisk" ? MessageBoxImage.Asterisk :
                                              MessageBoxImage.None;
            var result = MessageBox.Show(message, caption,
                                         buttons, icon);

            var ret = result == MessageBoxResult.OK ? "OK" :
                      result == MessageBoxResult.Cancel ? "Cancel" :
                      result == MessageBoxResult.Yes ? "Yes" :
                      result == MessageBoxResult.No ? "No" : "None";

            return new Variable(ret);
        }
    }
    class AddWidgetDataFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            string widgetName = Utils.GetToken(script, Constants.TOKEN_SEPARATION);
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var data = args[0];

            var widget = CSCS_GUI.GetWidget(widgetName);
            var itemsAdded = 0;
            if (widget is ComboBox)
            {
                var combo = widget as ComboBox;
                if (data.Type == Variable.VarType.ARRAY)
                {
                    foreach (var item in data.Tuple)
                    {
                        combo.Items.Add(item.AsString());
                    }
                    itemsAdded = data.Tuple.Count;
                }
                else
                {
                    combo.Items.Add(data.AsString());
                    itemsAdded = 1;
                }
            }
            else if (widget is DataGrid)
            {
                List<string> source = new List<string>();
                DataGrid dg = widget as DataGrid;
                if (data.Type == Variable.VarType.ARRAY && data.Tuple.Count > 0)
                {
                    dynamic row = new ExpandoObject();

                    for (int i = 0; i < dg.Columns.Count; i++)
                    {
                        var column = dg.Columns[i].Header.ToString();
                        var val = data.Tuple.Count > i ? data.Tuple[i].AsString() : "";
                        ((IDictionary<String, Object>)row)[column.Replace(' ', '_')] = val;
                    }

                    dg.Items.Add(row);
                }
                else
                {
                    var dataItems = data.AsString().Split(',');
                    for (int i = 0; i < dataItems.Length; i++)
                    {
                        dg.Items.Add(dataItems[i]);
                    }
                    itemsAdded = dataItems.Length;
                }
            }

            else if (widget is ListView)
            {
                List<string> source = new List<string>();
                ListView listView = widget as ListView;
                if (data.Type == Variable.VarType.ARRAY && data.Tuple.Count > 0)
                {
                    StringBuilder viewItem = new StringBuilder();
                    for (int i = 0; i < data.Tuple.Count; i++)
                    {
                        viewItem.Append(data.Tuple[i].AsString());
                        source.Add(data.Tuple[i].AsString());
                    }
                }
                else
                {
                    var dataItems = data.AsString().Split(',');
                    for (int i = 0; i < dataItems.Length; i++)
                    {
                        listView.Items.Add(dataItems[i]);
                    }
                    itemsAdded = dataItems.Length;
                }
            }

            return new Variable(itemsAdded);
        }
    }

    class SetWidgetOptionsFunction : ParserFunction
    {
        Dictionary<string, Color> m_bgcolors = new Dictionary<string, Color>();
        Dictionary<string, Color> m_fgcolors = new Dictionary<string, Color>();

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var widgetName = Utils.GetSafeString(args, 0).ToLower();
            var option = Utils.GetSafeString(args, 1).ToLower();

            var widget = CSCS_GUI.GetWidget(widgetName);
            if (widget is DataGrid)
            {
                DataGrid dg = widget as DataGrid;
                if (option == "colors")
                {
                    var bgColor = Utils.GetSafeString(args, 2).ToLower();
                    var fgColor = Utils.GetSafeString(args, 3, "black").ToLower();
                    m_bgcolors[widgetName] = StringToColor(bgColor);
                    m_fgcolors[widgetName] = StringToColor(fgColor);

                    dg.LoadingRow += new EventHandler<DataGridRowEventArgs>(DataGrid_LoadingRow);
                }
                else if (option == "columns")
                {
                    var colNames = args[2];
                    if (colNames.Type != Variable.VarType.ARRAY)
                    {
                        string label = colNames.AsString();
                        DataGridTextColumn column = new DataGridTextColumn();
                        column.Header = label;
                        column.Binding = new Binding(label.Replace(' ', '_'));

                        dg.Columns.Add(column);
                    }
                    else
                    {
                        foreach (var item in colNames.Tuple)
                        {
                            string label = item.ToString();
                            DataGridTextColumn column = new DataGridTextColumn();
                            column.Header = label;
                            column.Binding = new Binding(label.Replace(' ', '_'));

                            dg.Columns.Add(column);
                        }
                    }
                }
                else if (option == "clear")
                {
                    dg.Items.Clear();
                }
            }

            return new Variable(true);
        }

        void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            string widgetName = CSCS_GUI.GetWidgetBindingName(dg);
            Color bgcolor, fgcolor;
            if (m_bgcolors.TryGetValue(widgetName, out bgcolor))
            {
                e.Row.Background = new SolidColorBrush(bgcolor);
            }
            if (m_fgcolors.TryGetValue(widgetName, out fgcolor))
            {
                e.Row.Foreground = new SolidColorBrush(fgcolor);
            }
        }

        public static Color StringToColor(string strColor)
        {
            switch (strColor.ToLower())
            {
                case "black": return Colors.Black;
                case "white": return Colors.White;
                case "green": return Colors.Green;
                case "red": return Colors.Red;
                case "blue": return Colors.Blue;
                case "brown": return Colors.Brown;
                case "yellow": return Colors.Yellow;
                case "rose": return Colors.MistyRose;
                case "purple": return Colors.Purple;
                case "orange": return Colors.Orange;
                case "magenta": return Colors.Magenta;
                case "maroon": return Colors.Maroon;
                case "aqua": return Colors.Aqua;
                case "aquamarine": return Colors.Aquamarine;
                case "azure": return Colors.Azure;
                case "beige": return Colors.Beige;
                case "chocolate": return Colors.Chocolate;
                case "coral": return Colors.Coral;
                case "cyan": return Colors.Cyan;
                case "darkblue": return Colors.DarkBlue;
                case "darkcyan": return Colors.DarkCyan;
                case "darkgray": return Colors.DarkGray;
                case "darkgreen": return Colors.DarkGreen;
                case "darkkhaki": return Colors.DarkKhaki;
                case "darkorange": return Colors.DarkOrange;
                case "darkred": return Colors.DarkRed;
                case "darkturquoise": return Colors.DarkTurquoise;
                case "deeppink": return Colors.DeepPink;
                case "deepskyblue": return Colors.DeepSkyBlue;
                case "dimgray": return Colors.DimGray;
                case "gray": return Colors.Gray;
                case "gold": return Colors.Gold;
                case "greenyellow": return Colors.GreenYellow;
                case "hotpink": return Colors.HotPink;
                case "indigo": return Colors.Indigo;
                case "khaki": return Colors.Khaki;
                case "lightblue": return Colors.LightBlue;
                case "lightcyan": return Colors.LightCyan;
                case "lightgray": return Colors.LightGray;
                case "lightgreen": return Colors.LightGreen;
                case "lightpink": return Colors.LightPink;
                case "lightskyblue": return Colors.LightSkyBlue;
                case "lime": return Colors.Lime;
                case "limegreen": return Colors.LimeGreen;
                case "navy": return Colors.Navy;
                case "olive": return Colors.Olive;
                case "salmon": return Colors.Salmon;
                case "silver": return Colors.Silver;
                case "skyblue": return Colors.SkyBlue;
                case "snow": return Colors.Snow;
                case "violet": return Colors.Violet;
            }

            var color = (Color)ColorConverter.ConvertFromString(strColor);
            return color;
        }
    }


    class OpenFileFunction : ParserFunction
    {
        bool m_getFileContents;

        public OpenFileFunction(bool getContents)
        {
            m_getFileContents = getContents;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            /*List<Variable> args =*/
            script.GetFunctionArgs();
            return OpenFile(m_getFileContents);
        }
        public static Variable OpenFile(bool getContents = false)
        {
            Microsoft.Win32.OpenFileDialog openFile = new Microsoft.Win32.OpenFileDialog();
            if (openFile.ShowDialog() != true)
            {
                return Variable.EmptyInstance;
            }

            var fileName = openFile.FileName;
            if (!getContents)
            {
                return new Variable(fileName);
            }
            string contents = Utils.GetFileContents(fileName);
            contents = contents.Replace("\n", Environment.NewLine);
            return new Variable(contents);
        }
    }
    class SaveFileFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            string text = Utils.GetSafeString(args, 0);

            return SaveFile(text);
        }
        public static Variable SaveFile(string text)
        {
            Microsoft.Win32.SaveFileDialog saveFile = new Microsoft.Win32.SaveFileDialog();
            if (saveFile.ShowDialog() != true)
            {
                return Variable.EmptyInstance;
            }

            var fileName = saveFile.FileName;
            File.WriteAllText(fileName, text);
            return new Variable(fileName);
        }
    }

    class SetColorFunction : ParserFunction
    {
        bool m_bgColor;

        public SetColorFunction(bool bgcolor)
        {
            m_bgColor = bgcolor;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var colorName = Utils.GetSafeString(args, 1);
            var widget = CSCS_GUI.GetWidget(widgetName);
            if (widget == null)
            {
                return Variable.EmptyInstance;
            }

            var color = SetWidgetOptionsFunction.StringToColor(colorName);
            SolidColorBrush brush = new SolidColorBrush(color);

            if (m_bgColor)
            {
                widget.Background = brush;
            }
            else
            {
                widget.Foreground = brush;
            }
            return new Variable(true);
        }
    }

    class AddMenuEntryFunction : ParserFunction
    {
        bool m_separator;

        public AddMenuEntryFunction(bool separator = false)
        {
            m_separator = separator;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var parentName = Utils.GetSafeString(args, 0);
            ItemsControl parent = CSCS_GUI.GetWidget(parentName) as ItemsControl;
            if (parent == null)
            {
                return Variable.EmptyInstance;
            }

            CSCS_GUI.Control2Window.TryGetValue(parent, out Window win);

            if (m_separator)
            {
                parent.Items.Add(new Separator());
                return new Variable(parentName);
            }

            Utils.CheckArgs(args.Count, 2, m_name);
            var menuName = Utils.GetSafeString(args, 1);
            var menuLabel = Utils.GetSafeString(args, 2, menuName);
            var menuAction = Utils.GetSafeString(args, 3);

            MenuItem newMenuItem = new MenuItem();
            newMenuItem.Header = menuLabel;
            newMenuItem.DataContext = menuName;

            if (!string.IsNullOrWhiteSpace(menuAction))
            {
                newMenuItem.Click += (sender, eventArgs) =>
                {
                    Interpreter.Run(menuAction, new Variable(menuName), new Variable(eventArgs.Source.ToString()));
                };
            }

            parent.Items.Add(newMenuItem);
            CSCS_GUI.CacheControl(newMenuItem, win);

            return new Variable(menuName);
        }
    }

    class RemoveMenuFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var parentName = Utils.GetSafeString(args, 0);
            ItemsControl parent = CSCS_GUI.GetWidget(parentName) as ItemsControl;
            if (parent == null || parent.Items == null)
            {
                return Variable.EmptyInstance;
            }

            RemoveMenu(parent);
            return new Variable(true);
        }

        static void RemoveMenu(ItemsControl parent)
        {
            if (parent == null || parent.Items == null)
            {
                return;
            }

            foreach (var item in parent.Items)
            {
                CSCS_GUI.RemoveControl(item as Control);
                RemoveMenu(item as ItemsControl);
            }
            parent.Items.Clear();
        }
    }

    class ShowHideWidgetFunction : ParserFunction
    {
        bool m_showWidget;

        public ShowHideWidgetFunction(bool showWidget)
        {
            m_showWidget = showWidget;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var widget = CSCS_GUI.GetWidget(widgetName);
            if (widget == null)
            {
                return Variable.EmptyInstance;
            }

            widget.Visibility = m_showWidget ? Visibility.Visible : Visibility.Hidden;

            return new Variable(true);
        }
    }
    class ChainFunction : ParserFunction
    {
        bool m_paramMode;
        static Dictionary<string, List<Variable>> s_parameters = new Dictionary<string, List<Variable>>();
        static Dictionary<string, ParsingScript> s_chains = new Dictionary<string, ParsingScript>();
        static Dictionary<Window, string> s_window2File = new Dictionary<Window, string>();
        static Dictionary<string, Window> s_file2Window = new Dictionary<string, Window>();
        static Dictionary<string, Window> s_tag2Parent = new Dictionary<string, Window>();

        public ChainFunction(bool paramMode = false)
        {
            m_paramMode = paramMode;
        }

        public static ParsingScript GetScript(Control widget)
        {
            if (!CSCS_GUI.Control2Window.TryGetValue(widget, out Window win))
            {
                return null;
            }
            return GetScript(win);
        }

        public static ParsingScript GetScript(Window window)
        {
            if (window == null || !s_window2File.TryGetValue(window, out string filename))
            {
                return null;
            }
            if (!s_chains.TryGetValue(filename, out ParsingScript result))
            {
                return null;
            }
            return result;
        }

        public static void CacheWindow(Window window, string filename)
        {
            if (!string.IsNullOrWhiteSpace(filename))
            {
                s_window2File[window] = filename;
                s_file2Window[filename] = window;
            }
        }

        public static void CacheParentWindow(string tag, Window parent)
        {
            s_tag2Parent[tag] = parent;
        }

        public static void CloseAllWindows()
        {
            foreach (var win in s_window2File.Keys)
            {
                win.Close();
            }
        }

        public static Window GetParentWindow(string filename)
        {
            if (!s_tag2Parent.TryGetValue(filename, out Window win))
            {
                return null;
            }
            return win;
        }

        public static Window GetParentWindow(ParsingScript script)
        {
            if (script.ParentScript != null &&
                s_file2Window.TryGetValue(script.ParentScript.Filename, out Window win))
            {
                return win;
            }
            return CSCS_GUI.MainWindow;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> parameters;
            if (m_paramMode)
            {
                var argsStr = Utils.GetBodyBetween(script, '\0', '\0', Constants.END_STATEMENT);
                string[] argsArray = argsStr.Split(new char[] { ',' });
                //string msg = "CmdArgs:";
                if (!s_parameters.TryGetValue(script.Filename, out parameters))
                {
                    parameters = new List<Variable>();
                    string[] cmdArgs = Environment.GetCommandLineArgs();
                    var cmdArgsArr = cmdArgs.Length > 1 ? cmdArgs[1].Split(new char[] { ',' }) : new string[0];
                    for (int i = 1; i < cmdArgsArr.Length; i++)
                    {
                        parameters.Add(new Variable(cmdArgsArr[i]));
                        //msg += "[" + cmdArgsArr[i] + "]";
                    }
                }

                for (int i = 0; i < argsArray.Length && i < parameters.Count; i++)
                {
                    var func = new GetVarFunction(parameters[i]);
                    func.Name = argsArray[i];
                    //ParserFunction.AddGlobalOrLocalVariable(argsArray[i], func, script, true);
                    script.StackLevel.Variables[argsArray[i]] = func;

                    //msg += func.Name + "=[" + parameters[i].AsString() + "] ";
                }
                //MessageBox.Show(msg, parameters.Count + " args", MessageBoxButton.OK, MessageBoxImage.Hand);

                s_chains[script.Filename] = script;
                return Variable.EmptyInstance;
            }

            int currentScriptPos = script.Pointer;
            string argsExpr = Utils.ReplaceSpaces(script);
            var tempScript = script.GetTempScript(argsExpr);
            tempScript.ScriptOffset = script.ScriptOffset + currentScriptPos;
            List<Variable> args = tempScript.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            string chainName = args[0].AsString();
            string binName = chainName;
            parameters = new List<Variable>();
            string paramsStr = "\"";
            bool canAdd = false;
            bool newRuntime = false;
            for (int i = 1; i < args.Count; i++)
            {
                if (canAdd)
                {
                    if (newRuntime)
                    {
                        paramsStr += args[i].AsString() + ",";
                    }
                    else
                    {
                        parameters.Add(args[i]);
                    }
                    continue;
                }
                if (string.Equals(args[i].AsString(), Constants.NEWRUNTIME, StringComparison.OrdinalIgnoreCase))
                {
                    newRuntime = true;
                    continue;
                }
                canAdd = args[i].AsString().ToLower() == Constants.WITH;
                if (!canAdd)
                {
                    var parami = chainName = args[i].AsString();
                    paramsStr += parami + " ";
                }
            }

            if (newRuntime)
            {
                paramsStr = paramsStr.Substring(0, paramsStr.Length - 1) + '"';
                var execDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var exec = Path.Combine(execDir, binName);
                RunExecFunction.RunExec(exec, paramsStr);
                //var t = Task.Run(() => RunTask(chainScript));
                //t.Wait();
                //var result = t.Result;
                //return result;
                return Variable.EmptyInstance;
            }

            ParsingScript chainScript = tempScript.GetIncludeFileScript(chainName);
            chainScript.StackLevel = ParserFunction.AddStackLevel(chainScript.Filename);
            chainScript.CurrentModule = chainName;
            chainScript.ParentScript = script;

            s_parameters[chainScript.Filename] = parameters;

            return RunTask(chainScript);
        }

        static Variable RunTask(ParsingScript chainScript)
        {
            Variable result = Variable.EmptyInstance;
            //Application.Current.Dispatcher.Invoke(new Action(() => {
            while (chainScript.StillValid())
            {
                result = chainScript.Execute();
                chainScript.GoToNextStatement();
            }
            //}));

            if (!string.IsNullOrWhiteSpace(chainScript.CurrentModule))
            {
                //throw new ArgumentException("Chained script finished without Quit statement.");
            }

            return result;
        }
    }

    class WINFORMcommand : NewWindowFunction
    {
        bool m_paramMode;

        public WINFORMcommand(bool paramMode = false)
        {
            m_paramMode = paramMode;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            if (m_paramMode)
            {
                var NazivIliPutanjaFormeIzgleda = Utils.GetBodyBetween(script, '\0', '\0', Constants.END_STATEMENT);
                if (NazivIliPutanjaFormeIzgleda.EndsWith(".xaml") == false)
                {
                    NazivIliPutanjaFormeIzgleda = NazivIliPutanjaFormeIzgleda + ".xaml";
                }
                if (File.Exists(NazivIliPutanjaFormeIzgleda))
                {
                    var parentWin = ChainFunction.GetParentWindow(script);
                    SpecialWindow modalwin;
                    if (parentWin != null && !script.ParentScript.OriginalScript.Contains("#MAINMENU"))
                    {
                        //parentWin.IsEnabled = false;
                        //parentWin.

                        var winMode = SpecialWindow.MODE.SPECIAL_MODAL;
                        modalwin = CreateNew(NazivIliPutanjaFormeIzgleda, parentWin, winMode, script.Filename);
                    }
                    else
                    {
                        var winMode = SpecialWindow.MODE.NORMAL;
                        modalwin = CreateNew(NazivIliPutanjaFormeIzgleda, parentWin, winMode, script.Filename);
                    }


                    return new Variable(modalwin.Instance.Tag.ToString());
                }
                else
                {
                    MessageBox.Show($"Ne postoji datoteka {NazivIliPutanjaFormeIzgleda}! Gasim program.");
                    Environment.Exit(0);
                    return null;
                }
            }
            else return null;
        }
    }

    class MAINMENUcommand : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            return null;
        }
    }


    class VariableArgsFunction : ParserFunction
    {
        bool m_processFirstToken = true;
        Dictionary<string, Variable> m_parameters;

        public VariableArgsFunction(bool processFirst = true)
        {
            m_processFirstToken = processFirst;
        }

        void GetParameters(ParsingScript script)
        {
            m_parameters = new Dictionary<string, Variable>();
            while (script.Current != Constants.END_STATEMENT)
            {
                var labelName = Utils.GetToken(script, Constants.TOKEN_SEPARATION);
                var value = script.Current == Constants.END_STATEMENT ? Variable.EmptyInstance :
                                                                       new Variable(Utils.GetToken(script, Constants.TOKEN_SEPARATION));
                m_parameters[labelName.ToLower()] = value;
            }
        }

        string GetParameter(string key, string defValue = "")
        {
            Variable res;
            if (!m_parameters.TryGetValue(key.ToLower(), out res))
            {
                return defValue;
            }
            return res.AsString();
        }
        double GetDoubleParameter(string key, double defValue = 0.0)
        {
            Variable res;
            if (!m_parameters.TryGetValue(key.ToLower(), out res))
            {
                return defValue;
            }
            return res.AsDouble();
        }
        int GetIntParameter(string key, int defValue = 0)
        {
            Variable res;
            if (!m_parameters.TryGetValue(key.ToLower(), out res))
            {
                return defValue;
            }
            return res.AsInt();
        }
        bool GetBoolParameter(string key, bool defValue = false)
        {
            Variable res;
            if (!m_parameters.TryGetValue(key.ToLower(), out res))
            {
                return defValue;
            }
            return res.AsBool();
        }
        Variable GetVariableParameter(string key, Variable defValue = null)
        {
            Variable res;
            if (!m_parameters.TryGetValue(key.ToLower(), out res))
            {
                return defValue;
            }
            return res;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            var objectName = m_processFirstToken ? Utils.GetToken(script, Constants.NEXT_OR_END_ARRAY) : "";
            GetParameters(script);

            if (Name.ToUpper() == "MSG")
            {
                string caption = GetParameter("caption");
                int duration = GetIntParameter("duration");
                return new Variable(objectName);
            }
            if (Name.ToUpper() == "DEFINE")
            {
                string name = GetParameter("name");
                CSCS_GUI.SpecialObject sp = new CSCS_GUI.SpecialObject(objectName, GetVariableParameter("value"), GetParameter("type"),
                                                                       GetIntParameter("size"), GetIntParameter("dec"),
                                                                       GetBoolParameter("up"), GetVariableParameter("dup"));
                Variable newVar = new Variable(sp);
                CSCS_GUI.ChangingBoundVariable = true;
                AddGlobalOrLocalVariable(objectName, new GetVarFunction(newVar), script);
                CSCS_GUI.ChangingBoundVariable = false;

                List<Variable> moduleVars;
                if (!CSCS_GUI.DEFINES.TryGetValue(script.Filename, out moduleVars))
                {
                    moduleVars = new List<Variable>();
                }
                moduleVars.Add(newVar);
                CSCS_GUI.DEFINES[script.Filename] = moduleVars;

                return newVar;
            }
            if (Name.ToUpper() == "SET_OBJECT")
            {
                string prop = GetParameter("property");
                bool val = GetBoolParameter("value");
                return new Variable(objectName);
            }

            return new Variable(objectName);
        }
    }

    class ReturnStatement : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            if (script.ProcessReturn())
            {
                return Variable.EmptyInstance;
            }

            script.MoveForwardIf(Constants.SPACE);
            if (!script.FromPrev(Constants.RETURN.Length).Contains(Constants.RETURN))
            {
                script.Backward();
            }
            Variable result = Utils.GetItem(script);

            if (string.IsNullOrWhiteSpace(script.CurrentModule))
            {
                script.SetDone();
                result.IsReturn = true;
            }

            return result;
        }
    }

    class RunScriptFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileName = args[0].AsString();
            var encode = Utils.GetSafeInt(args, 1) > 0;
            var result = CSCS_GUI.RunScript(fileName, encode);

            return result;
        }
    }

    class QuitStatement : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            script.CurrentModule = "";
            if (script.StackLevel != null)
            {
                ParserFunction.PopLocalVariables(script.StackLevel.Id);
                script.StackLevel = null;
            }

            return Variable.EmptyInstance;
        }
    }

    class NewWindowFunction : ParserFunction
    {
        static string ns = "WpfCSCS.";

        static Dictionary<string, Window> s_windows = new Dictionary<string, Window>();
        static Dictionary<string, string> s_windowType = new Dictionary<string, string>();

        static int s_currentWindow = -1;

        internal enum MODE { NEW, SHOW, HIDE, DELETE, NEXT, MODAL, SET_MAIN, UNSET_MAIN };
        MODE m_mode;

        internal NewWindowFunction(MODE mode = MODE.NEW)
        {
            m_mode = mode;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();

            if (m_mode == MODE.NEXT)
            {
                HideAll();
                var windows = s_windows.Values.ToArray();
                if (windows.Length > 0)
                {
                    if (++s_currentWindow > windows.Length - 1)
                    {
                        s_currentWindow = 0;
                    }
                    windows[s_currentWindow].Show();
                }
                return new Variable(s_currentWindow);
            }

            Utils.CheckArgs(args.Count, 1, m_name);
            string instanceName = args[0].AsString();
            // ../../scripts/Window4.xaml

            Window wind = null;
            if (m_mode == MODE.NEW || m_mode == MODE.MODAL)
            {
                var parentWin = ChainFunction.GetParentWindow(script);
                var winMode = m_mode == MODE.NEW ? SpecialWindow.MODE.NORMAL : //SpecialWindow.MODE.SPECIAL_MODAL;
                    parentWin == CSCS_GUI.MainWindow ? SpecialWindow.MODE.MODAL : SpecialWindow.MODE.SPECIAL_MODAL;
                SpecialWindow modalwin = CreateNew(instanceName, parentWin, winMode, script.Filename);
                return new Variable(modalwin.Instance.Tag.ToString());
            }

            if (!s_windows.TryGetValue(instanceName, out wind))
            {
                if (!s_windowType.TryGetValue(instanceName, out string windName) ||
                    !s_windows.TryGetValue(windName, out wind))
                {
                    throw new ArgumentException("Couldn't find window [" + instanceName + "]");
                }
                instanceName = windName;
            }

            if (m_mode == MODE.HIDE)
            {
                wind.Hide();
            }
            else if (m_mode == MODE.SHOW)
            {
                wind.Show();
            }
            else if (m_mode == MODE.SET_MAIN || m_mode == MODE.UNSET_MAIN)
            {
                var special = SpecialWindow.GetInstance(wind);
                if (special != null)
                {
                    special.IsMain = m_mode == MODE.SET_MAIN;
                }
            }
            else if (m_mode == MODE.DELETE)
            {
                wind.Close();
                RemoveWindow(wind);
            }

            return new Variable(instanceName);
        }

        public static SpecialWindow CreateNew(string instanceName, Window parentWin = null,
            SpecialWindow.MODE winMode = SpecialWindow.MODE.NORMAL, string cscsFilename = "")
        {
            SpecialWindow modalwin = new SpecialWindow(instanceName, winMode,
                winMode != SpecialWindow.MODE.NORMAL ? parentWin : null);
            var wind = modalwin.Instance;

            var tag = wind.Tag.ToString();
            s_windows[tag] = wind;
            s_windowType[instanceName] = tag;
            s_currentWindow = 0;

            ChainFunction.CacheWindow(wind, cscsFilename);
            ChainFunction.CacheParentWindow(tag, parentWin);

            wind.Show();
            return modalwin;
        }

        public static void RemoveWindow(Window wind)
        {
            s_windows.Remove(wind.Tag.ToString());
        }

        static void HideAll()
        {
            foreach (var item in s_windows)
            {
                item.Value.Hide();
            }
        }
        static void ShowAll()
        {
            foreach (var item in s_windows)
            {
                item.Value.Show();
            }
        }

        static object GetInstance(string strFullyQualifiedName)
        {
            Type t = Type.GetType(strFullyQualifiedName);
            if (t == null)
            {
                return null;
            }
            return Activator.CreateInstance(t);
        }
    }

    internal class CheckVATFunction : ParserFunction
    {
        internal enum MODE { CHECK, NAME, ADDRESS };

        static string s_request = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
                                                      xmlns:urn=""urn:ec.europa.eu:taxud:vies:services:checkVat:types"">
                          <soapenv:Header/><soapenv:Body><urn:checkVat>
                            <urn:countryCode>COUNTRY</urn:countryCode>
                            <urn:vatNumber>VATNUMBER</urn:vatNumber>
                          </urn:checkVat></soapenv:Body></soapenv:Envelope>";
        static Dictionary<string, string> s_cache = new Dictionary<string, string>();
        MODE m_mode;

        internal CheckVATFunction(MODE mode = MODE.CHECK)
        {
            m_mode = mode;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            string vat = args[0].AsString(); // 26389058739
            string country = Utils.GetSafeString(args, 1, "HR");
            /*string callBack = Utils.GetSafeString(args, 2);

            CustomFunction callbackFunction = ParserFunction.GetFunction(callBack, null) as CustomFunction;
            if (callbackFunction == null)
            {
                throw new ArgumentException("Error: Couldn't find function [" + callBack + "]");
            }*/

            CacheVAT(vat, country);
            switch (m_mode)
            {
                case MODE.CHECK:
                    return new Variable(s_cache[vat + "valid"] == "true");
                case MODE.ADDRESS:
                    return new Variable(s_cache[vat + "address"]);
                case MODE.NAME:
                    return new Variable(s_cache[vat + "name"]);
            }

            return Variable.EmptyInstance; ;
        }

        static string ExtractTag(string xml, string tag)
        {
            var start = xml.IndexOf("<" + tag + ">");
            if (start < 0)
            {
                return "";
            }
            var wordStart = start + tag.Length + 2;
            var end = xml.IndexOf("</" + tag + ">", wordStart);
            if (end < 0)
            {
                return "";
            }
            var result = xml.Substring(wordStart, end - wordStart);
            return result;
        }

        internal static void CacheVAT(string vat, string country)
        {
            if (s_cache.TryGetValue(vat + "name", out string name) && !string.IsNullOrEmpty(name))
            {
                return;
            }

            s_cache[vat + "valid"] = "false";
            s_cache[vat + "name"] = "";
            s_cache[vat + "address"] = "";

            var wc = new WebClient();
            var request = s_request.Replace("COUNTRY", country).Replace("VATNUMBER", vat);

            string response = "";
            try
            {
                response = wc.UploadString("http://ec.europa.eu/taxation_customs/vies/services/checkVatService", request);
            }
            catch (Exception exc)
            {
                s_cache[vat + "name"] = exc.Message;
                return;
            }

            //<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><checkVatResponse xmlns="urn:ec.europa.eu:taxud:vies:services:checkVat:types"><countryCode>HR</countryCode><vatNumber>26389058739</vatNumber><requestDate>2020-05-01+02:00</requestDate><valid>true</valid>
            //<name>AURA SOFT D.O.O.</name><address>KAPETANA LAZARIÄ†A 1 D, PAZIN, 52000 PAZIN</address></checkVatResponse></soap:Body></soap:Envelope>
            var validTag = ExtractTag(response, "valid");
            if (validTag == "true")
            {
                s_cache[vat + "valid"] = validTag;
                s_cache[vat + "name"] = ExtractTag(response, "name");
                s_cache[vat + "address"] = ExtractTag(response, "address");
            }
        }
    }
}
