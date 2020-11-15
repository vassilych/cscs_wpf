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
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SplitAndMerge
{
    public partial class Constants
    {
        public const string READ_XML_FILE = "readXmlFile";
        public const string READ_TAGCONTENT_FROM_XMLSTRING = "readTagContentFromXmlString";

        public const string DEFINE = "DEFINE";
        public const string DISPLAY_ARRAY = "DISPLAYARR";
        public const string DATA_GRID = "DATA_GRID";
        public const string ADD_COLUMN = "NEWCOLUMN";
        public const string DELETE_COLUMN = "DELETECOLUMN";
        public const string SHIFT_COLUMN = "SHIFTCOLUMN";
        public const string MSG = "MSG";
        public const string SET_OBJECT = "SET_OBJECT";

        public const string SET_TEXT = "SetText";
        public const string QUIT = "quit";

        public const string CHAIN = "chain";
        public const string PARAM = "param";
        public const string WITH = "with";
        public const string NEWRUNTIME = "newruntime";
    }
}

namespace WpfCSCS
{
    class ReadXmlFileFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var xmlPath = args[0];

            string lala = xmlPath.AsString();

            string xmlString = File.ReadAllText(lala);

            return new Variable(xmlString);
        }
    }
    class ReadTagContentFromXmlStringFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var xmlString = args[0];
            var xmlTag = args[1];

            return new Variable(ExtractTag(xmlString.AsString(), xmlTag.AsString()));
        }

        private string ExtractTag(string xml, string tag)
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
            return result.Trim();
        }

    }

    public class CSCS_GUI
    {
        public static Dispatcher Dispatcher { get; set; }

        public class WidgetData
        {
            public enum COL_TYPE { STRING, NUMBER };

            public WidgetData(FrameworkElement w)
            {
                widget = w;
            }
            public FrameworkElement widget;

            public List<string> headerNames = new List<string>();
            public List<string> headerBindings = new List<string>();
            public List<COL_TYPE> colTypes = new List<COL_TYPE>();
            public Dictionary<string, Variable> headers = new Dictionary<string, Variable>();

            public string lineCounterName;
            public int lineCounter;
            public Variable lineCounterVar;

            public string actualElemsName;
            public int actualElems;
            public Variable actualElemsVar;

            public string maxElemsName;
            public int maxElems;
            public Variable maxElemsVar;

            public bool needsReset;
        }

        public static App TheApp { get; set; }
        public static Window MainWindow { get; set; }
        public static bool ChangingBoundVariable { get; set; }
        public static string RequireDEFINE { get; set; }

        public static Dictionary<string, FrameworkElement> Controls { get; set; } = new Dictionary<string, FrameworkElement>();
        public static Dictionary<FrameworkElement, Window> Control2Window { get; set; } = new Dictionary<FrameworkElement, Window>();
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

        public static Dictionary<string, DefineVariable> DEFINES { get; set; } =
            new Dictionary<string, DefineVariable>();
        public static Dictionary<string, WidgetData> WIDGETS { get; set; } =
            new Dictionary<string, WidgetData>();

        //public static Dictionary<string, List<Variable>> DEFINES { get; set; } =
        //          new Dictionary<string, List<Variable>>();

        public static Dictionary<string, Dictionary<string, bool>> s_varExists =
            new Dictionary<string, Dictionary<string, bool>>();

        public static void Init()
        {
            Interpreter.Instance.OnOutput += Print;
            ParserFunction.OnVariableChange += OnVariableChange;

            ParserFunction.RegisterFunction("#MAINMENU", new MAINMENUcommand());
            ParserFunction.RegisterFunction("#WINFORM", new WINFORMcommand(true));

            ParserFunction.RegisterFunction(Constants.READ_XML_FILE, new ReadXmlFileFunction());
            ParserFunction.RegisterFunction(Constants.READ_TAGCONTENT_FROM_XMLSTRING,
                new ReadTagContentFromXmlStringFunction());

            ParserFunction.RegisterFunction(Constants.MSG, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.DEFINE, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.SET_OBJECT, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.DISPLAY_ARRAY, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.DATA_GRID, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.ADD_COLUMN, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.DELETE_COLUMN, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.SHIFT_COLUMN, new VariableArgsFunction(true));

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
            ParserFunction.RegisterFunction("SetImage", new SetImageFunction());

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
            ParserFunction.RegisterFunction("FillWidget", new FillWidgetFunction());

            ParserFunction.RegisterFunction("AsyncCall", new AsyncCallFunction());

            ParserFunction.AddAction(Constants.ASSIGNMENT, new MyAssignFunction());
            ParserFunction.AddAction(Constants.POINTER, new MyPointerFunction());

            Constants.FUNCT_WITH_SPACE.Add(Constants.DEFINE);
            Constants.FUNCT_WITH_SPACE.Add(Constants.DISPLAY_ARRAY);
            Constants.FUNCT_WITH_SPACE.Add(Constants.DATA_GRID);
            Constants.FUNCT_WITH_SPACE.Add(Constants.ADD_COLUMN);
            Constants.FUNCT_WITH_SPACE.Add(Constants.DELETE_COLUMN);
            Constants.FUNCT_WITH_SPACE.Add(Constants.SHIFT_COLUMN);
            Constants.FUNCT_WITH_SPACE.Add(Constants.MSG);
            Constants.FUNCT_WITH_SPACE.Add(Constants.SET_OBJECT);
            Constants.FUNCT_WITH_SPACE.Add(Constants.SET_TEXT);

            Constants.FUNCT_WITH_SPACE.Add(Constants.CHAIN);
            Constants.FUNCT_WITH_SPACE.Add(Constants.PARAM);

            Precompiler.AddNamespace("using WpfCSCS;");
            Precompiler.AddNamespace("using System.Windows;");
            Precompiler.AddNamespace("using System.Windows.Controls;");
            Precompiler.AddNamespace("using System.Windows.Controls.Primitives;");
            Precompiler.AddNamespace("using System.Windows.Data;");
            Precompiler.AddNamespace("using System.Windows.Documents;");
            Precompiler.AddNamespace("using System.Windows.Input;");
            Precompiler.AddNamespace("using System.Windows.Media;");
            Precompiler.AsyncMode = false;

            RequireDEFINE = App.GetConfiguration("Require_Define", "false");
        }

        public static string GetWidgetBindingName(FrameworkElement widget)
        {
            var widgetName = widget == null || widget.DataContext == null ? "" : widget.DataContext.ToString();
            return widgetName;
        }

        static void OnVariableChange(string name, Variable newValue, bool exists = true)
        {
            if (ChangingBoundVariable)
            {
                return;
            }
            if (!exists && RequireDEFINE != "false" && (RequireDEFINE == "*" || name.StartsWith(RequireDEFINE)))
            {
                throw new ArgumentException("Variable [" + name + "] must be defined with DEFINE function first.");
            }

            var widgetName = name.ToLower();
            if (!s_boundVariables.TryGetValue(widgetName, out Variable bounded))
            {
                return;
            }

            var widget = GetWidget(widgetName);
            if (widget == null)
            {
                var obj = bounded.Object;
            }
            var text = newValue.AsString();

            SetTextWidgetFunction.SetText(widget, text);
            s_boundVariables[widgetName] = newValue;
        }

        static void UpdateVariable(FrameworkElement widget, Variable newValue)
        {
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrWhiteSpace(widgetName))
            {
                return;
            }

            if (CSCS_GUI.DEFINES.TryGetValue(widgetName, out DefineVariable defVar))
            {
                defVar.InitVariable(newValue);
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

        public static bool AddBinding(string name, FrameworkElement widget)
        {
            var text = GetTextWidgetFunction.GetText(widget);
            Variable baseValue = new Variable(text);
            ParserFunction.AddGlobal(name, new GetVarFunction(baseValue), false /* not native */);

            var current = new Variable(widget);
            if (widget is DataGrid)
            {
                var dg = widget as DataGrid;
                WidgetData wd = new WidgetData(dg);
                DEFINES[name] = new DefineVariable(name, "datagrid", dg);
                for (int i = 0; i < dg.Columns.Count; i++)
                {
                    var textCol = dg.Columns[i] as DataGridTextColumn;
                    var header = textCol.Header as string;
                    if (textCol.Binding == null)
                    {
                        textCol.Binding = new Binding(header);
                    }
                    Binding binding = textCol.Binding as Binding;
                    wd.headerBindings.Add(binding.Path.Path);

                    if (!string.IsNullOrWhiteSpace(header))
                    {
                        var headerStr = binding.Path.Path.ToLower();
                        var headerVar = new DefineVariable(headerStr, "datagrid", dg, i);
                        if (DEFINES.TryGetValue(headerStr, out DefineVariable existing) && existing.Tuple != null)
                        {
                            headerVar.Tuple = existing.Tuple;
                            headerVar.Size = existing.Size;
                        }
                        DEFINES[headerStr] = headerVar;
                        wd.headers[headerStr] = headerVar;
                        wd.headerNames.Add(headerStr);
                        wd.colTypes.Add(WidgetData.COL_TYPE.STRING);

                        //var array = new DefineVariable(new List<Variable>());
                        ParserFunction.AddGlobal(headerStr, new GetVarFunction(headerVar), false /* not native */);
                    }
                }
                WIDGETS[name] = wd;
                dg.Sorting += Dg_Sorting;
                //dg.ItemsSource = new 
            }

            name = name.ToLower();
            s_boundVariables[name] = current;
            if (DEFINES.TryGetValue(name, out DefineVariable defVar))
            {
                OnVariableChange(name, defVar);
            }
            return true;
        }

        private static void Dg_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var dg = sender as DataGrid;
            string dgName = dg.DataContext as string;
            var funcName = dgName + "@Header";
            CSCS_GUI.RunScript(funcName, dg.Parent as Window, new Variable(dgName), new Variable(e.Column.DisplayIndex));

            Dispatcher.BeginInvoke((Action)delegate ()
            {
                IEnumerable<ExpandoObject> sortedCast = null;
                try
                {
                    var casted = dg.Items.Cast<ExpandoObject>();
                    sortedCast = casted.ToList();
                }
                catch(Exception exc)
                {
                    Console.WriteLine(exc);
                    var sorted = dg.Items.SourceCollection;
                    sortedCast = sorted.Cast<ExpandoObject>();
                }

                FillWidgetFunction.ResetArrays(sender as FrameworkElement, sortedCast);
            }, null);

            //Console.WriteLine(e.Handled);
        }

        public static bool OnAddingRow(DataGrid dg)
        {
            string dgName = dg.DataContext as string;
            var funcName = dgName + "@Add";
            var rowList = dg.ItemsSource as List<ExpandoObject>;
            var currentSize = rowList == null ? 0 : rowList.Count; 
            var res = CSCS_GUI.RunScript(funcName, dg.Parent as Window, new Variable(dgName), new Variable(currentSize));
            bool canAddRow = res == Variable.EmptyInstance || res.AsDouble() != 0;

            return canAddRow;
        }

        public static bool AddActionHandler(string name, string action, FrameworkElement widget)
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

        public static bool AddPreActionHandler(string name, string action, FrameworkElement widget)
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
        public static bool AddPostActionHandler(string name, string action, FrameworkElement widget)
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

        public static bool AddKeyDownHandler(string name, string action, FrameworkElement widget)
        {
            s_keyDownHandlers[name] = action;
            widget.KeyDown += new KeyEventHandler(Widget_KeyDown);
            return true;
        }
        public static bool AddKeyUpHandler(string name, string action, FrameworkElement widget)
        {
            s_keyUpHandlers[name] = action;
            widget.KeyUp += new KeyEventHandler(Widget_KeyUp);
            return true;
        }
        public static bool AddTextChangedHandler(string name, string action, FrameworkElement widget)
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
        public static bool AddSelectionChangedHandler(string name, string action, FrameworkElement widget)
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
        public static bool AddDateChangedHandler(string name, string action, FrameworkElement widget)
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

        private static void ValueUpdated(string funcName, string widgetName, FrameworkElement widget, Variable newValue)
        {
            UpdateVariable(widget, newValue);
            Control2Window.TryGetValue(widget, out Window win);
            RunScript(funcName, win, new Variable(widgetName), newValue);
        }

        private static void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var widget = sender as FrameworkElement;
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

        public static bool AddMouseHoverHandler(string name, string action, FrameworkElement widget)
        {
            s_mouseHoverHandlers[name] = action;
            widget.MouseEnter += new MouseEventHandler(Widget_Hover);
            return true;
        }

        private static void Widget_Click(object sender, RoutedEventArgs e)
        {
            var widget = sender as FrameworkElement;
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
            var widget = sender as FrameworkElement;
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
            var widget = sender as FrameworkElement;
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
            var widget = sender as FrameworkElement;
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
            var widget = sender as FrameworkElement;
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
            var widget = sender as Selector;
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
            var widget = sender as FrameworkElement;
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

        public static FrameworkElement GetWidget(string name)
        {
            CacheControls(MainWindow);
            if (Controls.TryGetValue(name.ToLower(), out FrameworkElement control))
            {
                return control;
            }
            return null;
        }

        public static List<FrameworkElement> CacheControls(Window win, bool force = false)
        {
            List<FrameworkElement> controls = new List<FrameworkElement>();

            if ((!force && Controls.Count > 0) || win == null)
            {
                return controls;
            }

            var content = win.Content;
            List<UIElement> children = null;

            if (content is Grid)
            {
                var grid = content as Grid;
                if (grid.Children.Count > 0 && grid.Children[0] is StackPanel)
                {
                    var stack = grid.Children[0] as StackPanel;
                    children = stack.Children.Cast<UIElement>().ToList();
                }
                else
                {
                    children = grid.Children.Cast<UIElement>().ToList();
                }
            }
            else if (content is Panel)
            {
                var panel = content as Panel;
                children = (content as Panel).Children.Cast<UIElement>().ToList();
            }
            else if (content is StackPanel)
            {
                var stack = content as StackPanel;
                children = stack.Children.Cast<UIElement>().ToList();
            }

            CacheChildren(children, controls, win);
            return controls;
        }

        static void CacheChildren(List<UIElement> children, List<FrameworkElement> controls, Window win)
        {
            if (children == null || children.Count == 0)
            {
                return;
            }
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
                                                CacheControl(child2 as FrameworkElement, win, controls);
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
                    CacheControl(child as FrameworkElement, win, controls);
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

        public static void CacheControl(FrameworkElement widget, Window win = null, List<FrameworkElement> controls = null)
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
        public static void RemoveControl(FrameworkElement widget)
        {
            widget.Visibility = Visibility.Hidden;
            Controls.Remove(widget.DataContext.ToString().ToLower());
        }

        public static void AddWidgetActions(FrameworkElement widget)
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
            /*Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                result = callbackFunction.Run(args);
            }));*/

            CSCS_GUI.Dispatcher.Invoke((Action)delegate ()
            {
                result = callbackFunction.Run(args);
            }, null);

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
                printDlg.PrintVisual(widget as FrameworkElement, "Window Printing.");
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

        public static Variable GetText(FrameworkElement widget)
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

        public static bool SetText(FrameworkElement widget, string text, int index = -1)
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
                    dispatcher.Invoke(new Action(() =>
                    {
                        combo.SelectedIndex = index;
                    }));
                }
            }
            else if (widget is CheckBox)
            {
                var checkBox = widget as CheckBox;
                dispatcher.Invoke(new Action(() =>
                {
                    checkBox.IsChecked = text == "1" || text.ToLower() == "true";
                }));
            }
            else if (widget is ContentControl)
            {
                var contentable = widget as ContentControl;
                dispatcher.Invoke(new Action(() =>
                {
                    contentable.Content = text;
                }));
            }
            else if (widget is TextBox)
            {
                var textBox = widget as TextBox;
                dispatcher.Invoke(new Action(() =>
                {
                    textBox.Text = text;
                }));
            }
            else if (widget is RichTextBox)
            {
                var richTextBox = widget as RichTextBox;
                dispatcher.Invoke(new Action(() =>
                {
                    richTextBox.Document.Blocks.Clear();
                    richTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
                }));
            }
            else if (widget is DatePicker && !string.IsNullOrWhiteSpace(text))
            {
                var datePicker = widget as DatePicker;
                var format = text.Length == 10 ? "yyyy/MM/dd" : text.Length == 8 ? "hh:mm:ss" :
                             text.Length == 12 ? "hh:mm:ss.fff" : "yyyy/MM/dd hh:mm:ss";
                dispatcher.Invoke(new Action(() =>
                {
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
                    ClearWidget(widgetName, dg);
                }
            }

            return new Variable(true);
        }

        public static bool ClearWidget(string widgetName, FrameworkElement widget = null)
        {
            widget = widget == null ? CSCS_GUI.GetWidget(widgetName) as DataGrid : widget;
            if (widget is DataGrid)
            {
                if (CSCS_GUI.WIDGETS.TryGetValue(widgetName, out CSCS_GUI.WidgetData wd))
                {
                    foreach (var entry in wd.headers)
                    {
                        entry.Value.Tuple.Clear();
                        ParserFunction.AddGlobal(entry.Key, new GetVarFunction(entry.Value), false);
                    }
                }

                var dg = widget as DataGrid;
                var rowList = dg.ItemsSource as List<ExpandoObject>;
                FillWidgetFunction.ResetArrays(dg);
                rowList?.Clear();
                dg.ItemsSource = null;
                dg.Items.Refresh();
                dg.UpdateLayout();
            }

            return widget is DataGrid;
        }

        static bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (element == null || !element.IsVisible)
                return false;
            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        public static void RedisplayWidget(DataGrid dg, string option = "current", int row = -1)
        {
            var rowList = dg.ItemsSource as List<ExpandoObject>;

            if (dg.Items == null || rowList.Count == 0)
            {
                return;
            }
            row = row >= 0 ? row : option.EndsWith("top") ? 0 : option.EndsWith("end") || option.EndsWith("bottom") ? rowList.Count - 1 : dg.SelectedIndex;
            row = row < 0 ? 0 : row;
            //var visibles = dg.Scr;
            dg.ScrollIntoView(rowList[row]);

            if (row != 0 && row != rowList.Count - 1)
            {
                DataGridRow rowElem = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(dg.SelectedIndex);
                rowElem?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                int visible = rowElem != null ? (int)(dg.ActualHeight / rowElem.ActualHeight) - 2 : 0;

                var adjusted = row - visible >= 0 ? row + visible : 0;
                if (adjusted != row)
                {
                    dg.ScrollIntoView(rowList[adjusted]);
                }
            }
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
            if (widget == null || !(widget is Control))
            {
                return Variable.EmptyInstance;
            }

            var color = SetWidgetOptionsFunction.StringToColor(colorName);
            SolidColorBrush brush = new SolidColorBrush(color);

            if (m_bgColor)
            {
                ((Control)widget).Background = brush;
            }
            else
            {
                ((Control)widget).Foreground = brush;
            }
            return new Variable(true);
        }
    }

    class SetImageFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var imageName = Utils.GetSafeString(args, 1);
            var widget = CSCS_GUI.GetWidget(widgetName);
            if (widget == null)
            {
                return Variable.EmptyInstance;
            }

            bool found = false;
            if (widget is ContentControl)
            {
                var control = widget as ContentControl;
                control.Content = new Image
                {
                    Source = new BitmapImage(new Uri(imageName, UriKind.RelativeOrAbsolute)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Stretch.None
                };
                found = true;
            }
            else if (widget is System.Windows.Controls.Image)
            {
                var img = widget as Image;
                img.Source = new BitmapImage(new Uri(imageName, UriKind.RelativeOrAbsolute));
                found = true;
            }

            return new Variable(found);
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
                CSCS_GUI.RemoveControl(item as FrameworkElement);
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

        public static ParsingScript GetScript(FrameworkElement widget)
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
            var separator = new char[] { ',' };
            List<Variable> parameters;
            if (m_paramMode)
            {
                var argsStr = Utils.GetBodyBetween(script, '\0', '\0', Constants.END_STATEMENT);
                string[] argsArray = argsStr.Split(separator);
                //string msg = "CmdArgs:";
                if (!s_parameters.TryGetValue(script.Filename, out parameters))
                {
                    parameters = new List<Variable>();
                    string[] cmdArgs = Environment.GetCommandLineArgs();
                    var cmdArgsArr = cmdArgs.Length > 1 ? cmdArgs[1].Split(separator) : new string[0];
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
        string m_lastParameter;

        public VariableArgsFunction(bool processFirst = true)
        {
            m_processFirstToken = processFirst;
        }

        void GetParameters(ParsingScript script)
        {
            var separator = new char[] { ' ', ';' };
            m_parameters = new Dictionary<string, Variable>();

            while (script.Current != Constants.END_STATEMENT && script.StillValid())
            {
                var labelName = Utils.GetToken(script, Constants.TOKEN_SEPARATION).ToLower();
                var value = labelName == "up" || labelName == "local" || labelName == "setup" || labelName == "close" ||
                    labelName == "addrow" || labelName == "insertrow" || labelName == "deleterow" ?
                    new Variable(true) :
                            script.Current == Constants.END_STATEMENT ? Variable.EmptyInstance :
                            new Variable(Utils.GetToken(script, separator));
                if (script.Prev != '"' && !string.IsNullOrWhiteSpace(value.String))
                {
                    var existing = ParserFunction.GetVariableValue(value.String, script);
                    value = existing == null ? value : existing;
                }
                m_parameters[labelName] = value;
                m_lastParameter = labelName;
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
            var objectName = m_processFirstToken ? Utils.GetToken(script, new char[] { ' ', '}', ')', ';' }) : "";
            Name = Name.ToUpper();
            GetParameters(script);

            if (Name == Constants.MSG)
            {
                string caption = GetParameter("caption");
                int duration = GetIntParameter("duration");
                return new Variable(objectName);
            }
            if (Name == Constants.DEFINE)
            {
                Variable newVar = CreateVariable(script, objectName, GetVariableParameter("value"), GetVariableParameter("init"),
                    GetParameter("type"), GetIntParameter("size"), GetIntParameter("dec"), GetIntParameter("array"),
                    GetBoolParameter("local"), GetBoolParameter("up"), GetParameter("dup"));
                return newVar;
            }
            if (Name == Constants.DISPLAY_ARRAY)
            {
                Variable newVar = DisplayArray(script, objectName, GetParameter("linecounter"), GetParameter("maxelements"),
                    GetParameter("actualelements"), m_lastParameter);
                return newVar;
            }
            if (Name == Constants.DATA_GRID)
            {
                Variable newVar = DataGrid(script, objectName, GetBoolParameter("addrow"), GetBoolParameter("insertrow"),
                    GetBoolParameter("deleterow"), m_lastParameter);
                return newVar;
            }
            if (Name == Constants.ADD_COLUMN)
            {
                AddGridColumn(script, objectName, GetParameter("header"), GetParameter("binding"));
                return new Variable(true);
            }
            if (Name == Constants.DELETE_COLUMN)
            {
                DeleteGridColumn(script, objectName, GetIntParameter("num"));
                return new Variable(true);
            }
            if (Name == Constants.SHIFT_COLUMN)
            {
                ShiftGridColumns(script, objectName, GetIntParameter("num"), GetIntParameter("to"));
                return new Variable(true);
            }
            if (Name == Constants.SET_OBJECT)
            {
                string prop = GetParameter("property");
                bool val = GetBoolParameter("value");
                return new Variable(objectName);
            }

            return new Variable(objectName);
        }

        static Variable CreateVariable(ParsingScript script, string name, Variable value, Variable init,
            string type = "", int size = 0, int dec = 3, int array = 0, bool local = false, bool up = false, string dup = null)
        {
            DefineVariable dupVar = null;
            if (!string.IsNullOrWhiteSpace(dup) && !CSCS_GUI.DEFINES.TryGetValue(dup, out dupVar))
            {
                throw new ArgumentException("Couldn't find variable [" + dup + "]");
            }

            var valueStr = value == null ? "" : value.AsString();
            init = init == null ? Variable.EmptyInstance : init;

            DefineVariable newVar = null;
            var parts = name.Split(new char[] { ',' });
            foreach (var objName in parts)
            {
                newVar = dupVar != null ? new DefineVariable(objName, dupVar, local) :
                                          new DefineVariable(objName, valueStr, type, size, dec, array, local, up);
                newVar.InitVariable(dupVar != null ? dupVar.Init : init, script);
            }

            return newVar;
        }

        static DefineVariable DataGrid(ParsingScript script, string name, bool addrow, bool insertrow, bool deleterow, string action)
        {
            if (!CSCS_GUI.DEFINES.TryGetValue(name, out DefineVariable gridVar))
            {
                throw new ArgumentException("Couldn't find variable [" + name + "]");
            }
            if (gridVar.DefType != "datagrid")
            {
                throw new ArgumentException("Variable of wrong type: [" + gridVar.DefType + "]");
            }
            if (!CSCS_GUI.WIDGETS.TryGetValue(name, out CSCS_GUI.WidgetData wd))
            {
                throw new ArgumentException("Couldn't find widget data for widget: [" + name + "]");
            }

            DataGrid dg = gridVar.Object as DataGrid;
            wd.lineCounter = dg.SelectedIndex;
            var where = wd.lineCounter >= 0 ? wd.lineCounter : 0;
            var rowList = dg.ItemsSource as List<ExpandoObject>;

            if ((addrow || where >= rowList.Count) && !dg.IsReadOnly && CSCS_GUI.OnAddingRow(dg))
            {
                var expando = MyAssignFunction.GetNewRow(dg, wd);
                rowList.Add(expando);
            }
            else if (insertrow && !dg.IsReadOnly && CSCS_GUI.OnAddingRow(dg))
            {
                var expando = MyAssignFunction.GetNewRow(dg, wd);
                rowList.Insert(where, expando);
            }
            else if (deleterow && !dg.IsReadOnly)
            {
                rowList.RemoveAt(where);
            }

            if (action == "goedit")
            {
                dg.IsReadOnly = false;
            }
            else if (action == "gorowselect")
            {
                dg.IsReadOnly = true;
            }

            if ((insertrow || addrow) && !dg.IsReadOnly)
            {
                FillWidgetFunction.ResetArrays(dg);
            }

            dg.Items.Refresh();
            dg.UpdateLayout();
            return gridVar;
        }

        //DISPLAYARR ‘DataGridName’ LINECOUNTER cntr1 MAXELEMENTS cntr2 ACTUALELEMENTS cntr3 SETUP
        static DefineVariable DisplayArray(ParsingScript script, string name, string
            lineCounter, string maxElems, string actualElems, string action)
        {
            if (!CSCS_GUI.DEFINES.TryGetValue(name, out DefineVariable gridVar))
            {
                throw new ArgumentException("Couldn't find variable [" + name + "]");
            }
            if (gridVar.DefType != "datagrid")
            {
                throw new ArgumentException("Variable of wrong type: [" + gridVar.DefType + "]");
            }
            if (!CSCS_GUI.WIDGETS.TryGetValue(name, out CSCS_GUI.WidgetData wd))
            {
                throw new ArgumentException("Couldn't find widget data for widget: [" + name + "]");
            }

            DataGrid dg = gridVar.Object as DataGrid;
            gridVar.Active = action != "close" && (gridVar.Active || action == "setup");
            if (action == "close")
            {
                SetWidgetOptionsFunction.ClearWidget(name, dg);
                return gridVar;
            }
            else if (action != "setup")
            {
                SetWidgetOptionsFunction.RedisplayWidget(dg, action);
                return gridVar;
            }

            var rowList = dg.ItemsSource as List<ExpandoObject>;
            FillWidgetFunction.ResetArrays(dg);

            CSCS_GUI.DEFINES[lineCounter] = new DefineVariable(name, "lineCounter", gridVar.Object, true);
            CSCS_GUI.DEFINES[maxElems] = new DefineVariable(name, "maxElems", gridVar.Object, true);
            CSCS_GUI.DEFINES[actualElems] = new DefineVariable(name, "actualElems", gridVar.Object, true);

            var max = ParserFunction.GetVariableValue(maxElems, script);
            int maxValue = max == null ? 0 : max.AsInt();

            if (Utils.CheckLegalName(lineCounter, script, false))
            {
                ParserFunction.AddGlobal(lineCounter, new GetVarFunction(new Variable(dg.SelectedIndex)), false);
            }
            if (Utils.CheckLegalName(actualElems, script, false))
            {
                ParserFunction.AddGlobal(actualElems, new GetVarFunction(new Variable(rowList == null ? 0 : rowList.Count)), false);
            }
            if (Utils.CheckLegalName(maxElems, script, false))
            {
                ParserFunction.AddGlobal(maxElems, new GetVarFunction(new Variable(maxValue)), false);
            }

            dg.SelectionChanged += (s, e) =>
            {
                ParserFunction.AddGlobal(lineCounter, new GetVarFunction(new Variable(dg.SelectedIndex)), false);
                wd.lineCounter = dg.SelectedIndex;
                var funcName = name + "@Move";
                CSCS_GUI.RunScript(funcName, s as Window, new Variable(name), new Variable(dg.SelectedIndex));
            };

            dg.MouseDoubleClick += (s, e) =>
            {
                var funcName = name + "@Select";
                CSCS_GUI.RunScript(funcName, s as Window, new Variable(name), new Variable(dg.SelectedIndex));
            };

            dg.AddingNewItem += (s, e) =>
            {
                max = ParserFunction.GetVariableValue(maxElems, script);
                if (max != null)
                {
                }
            };
            /*dg.CellEditEnding += (s, e) =>
            {
                DataGridRow row = e.Row;
                DataGridColumn col = e.Column;
                int row_index = dg.ItemContainerGenerator.IndexFromContainer(row);
                int col_index = col.DisplayIndex;

                var item = e.Row.Item as ExpandoObject;
                var newVal = (e.EditingElement as TextBox).Text;
                if (item != null)
                {
                    var p = item as IDictionary<String, object>;
                    for (int i = 0; i < dg.Columns.Count; i++)
                    {
                        var x = p[wd.headerNames[i]];
                        var z = 0;
                    }

                }
            };*/

            dg.RowEditEnding += (s, e) =>
            {
            };

            wd.lineCounterName = lineCounter;
            wd.lineCounter = dg.SelectedIndex;
            wd.actualElemsName = actualElems;
            wd.actualElems = rowList == null ? 0 : rowList.Count;
            wd.maxElemsName = maxElems;
            wd.maxElems = maxValue;

            foreach (var headerName in wd.headerNames)
            {
                FillWidgetFunction.AddGridData(name, headerName);
            }

            return gridVar;
        }

        static void AddGridColumn(ParsingScript script, string name, string header, string binding)
        {
            if (!CSCS_GUI.DEFINES.TryGetValue(name, out DefineVariable gridVar))
            {
                throw new ArgumentException("Couldn't find variable [" + name + "]");
            }
            if (gridVar.DefType != "datagrid")
            {
                throw new ArgumentException("Variable of wrong type: [" + gridVar.DefType + "]");
            }
            if (!CSCS_GUI.WIDGETS.TryGetValue(name, out CSCS_GUI.WidgetData wd))
            {
                throw new ArgumentException("Couldn't find widget data for widget: [" + name + "]");
            }

            DataGrid dg = gridVar.Object as DataGrid;

            DataGridTextColumn textColumn = new DataGridTextColumn();
            textColumn.Header = header;
            textColumn.Binding = new Binding(binding);
            dg.Columns.Add(textColumn);

            wd.headerBindings.Add(binding);
            var headerStr = binding.ToLower();
            var headerVar = new DefineVariable(headerStr, "datagrid", dg, dg.Columns.Count - 1);
            headerVar.Active = true;
            CSCS_GUI.DEFINES[headerStr] = headerVar;
            wd.headers[headerStr] = headerVar;
            wd.headerNames.Add(headerStr);
            wd.colTypes.Add(CSCS_GUI.WidgetData.COL_TYPE.STRING);
            ParserFunction.AddGlobal(headerStr, new GetVarFunction(headerVar), false);

            var rowList = dg.ItemsSource as List<ExpandoObject>;
            for (int i = 0; i < rowList.Count; i++)
            {
                var p = rowList[i] as IDictionary<String, object>;
                p[headerStr] = "";
            }
            FillWidgetFunction.ResetArrays(dg);

            dg.Items.Refresh();
            dg.UpdateLayout();
        }

        static void ShiftGridColumns(ParsingScript script, string name, int from, int to)
        {
            if (!CSCS_GUI.DEFINES.TryGetValue(name, out DefineVariable gridVar))
            {
                throw new ArgumentException("Couldn't find variable [" + name + "]");
            }
            if (gridVar.DefType != "datagrid")
            {
                throw new ArgumentException("Variable of wrong type: [" + gridVar.DefType + "]");
            }
            if (!CSCS_GUI.WIDGETS.TryGetValue(name, out CSCS_GUI.WidgetData wd))
            {
                throw new ArgumentException("Couldn't find widget data for widget: [" + name + "]");
            }

            DataGrid dg = gridVar.Object as DataGrid;

            var cols = dg.Columns;
            cols[from].DisplayIndex = to;
            cols[to].DisplayIndex = from;

            var binding1  = wd.headerNames[from];
            var binding2  = wd.headerNames[to];
            var colType1 = wd.colTypes[from];
            var colType2 = wd.colTypes[to];

            wd.headerNames[from] = binding2;
            wd.headerNames[to]   = binding1;
            wd.colTypes[from]    = colType2;
            wd.colTypes[to]      = colType1;

            var array1 = ParserFunction.GetVariableValue(binding1);
            var array2 = ParserFunction.GetVariableValue(binding2);
            var max = array1 == null || array1 == null || array1.Tuple == null || array2.Tuple == null ? 0 :
                Math.Min(array1.Tuple.Count, array2.Tuple.Count);

            /*for (int rowNb = 0; rowNb < max; rowNb++)
            {
                var tmp = array1.Tuple[rowNb];
                array1.Tuple[rowNb] = array2.Tuple[rowNb];
                array2.Tuple[rowNb] = tmp;
            }*/

            dg.Items.Refresh();
            dg.UpdateLayout();
        }

        static void DeleteGridColumn(ParsingScript script, string name, int colId)
        {
            if (!CSCS_GUI.DEFINES.TryGetValue(name, out DefineVariable gridVar))
            {
                throw new ArgumentException("Couldn't find variable [" + name + "]");
            }
            if (gridVar.DefType != "datagrid")
            {
                throw new ArgumentException("Variable of wrong type: [" + gridVar.DefType + "]");
            }
            if (!CSCS_GUI.WIDGETS.TryGetValue(name, out CSCS_GUI.WidgetData wd))
            {
                throw new ArgumentException("Couldn't find widget data for widget: [" + name + "]");
            }

            DataGrid dg = gridVar.Object as DataGrid;

            dg.Columns.RemoveAt(colId);
            var cols = dg.Columns;
            for (int i = colId; i < cols.Count - 1; i++)
            {
                wd.headerNames[i] = wd.headerNames[i + 1];
                wd.colTypes[i]    = wd.colTypes[i + 1];
            }

            dg.Items.Refresh();
            dg.UpdateLayout();
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

    class FillWidgetFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var widgetName = Utils.GetSafeString(args, 0);
            var varName = Utils.GetSafeString(args, 1);

            int count = AddGridData(widgetName, varName);
            return new Variable(count);
        }

        public static int AddGridData(string widgetName, string headerName)
        {
            if (!CSCS_GUI.WIDGETS.TryGetValue(widgetName, out CSCS_GUI.WidgetData wd) ||
                !(wd.widget is DataGrid))
            {
                return 0;
            }
            DataGrid dg = wd.widget as DataGrid;

            var data = ParserFunction.GetVariableValue(headerName);
            if (!CSCS_GUI.DEFINES.TryGetValue(headerName, out DefineVariable defVar) || data == null)
            {
                return 0;
            }

            if (CSCS_GUI.DEFINES.TryGetValue(dg.DataContext as string, out DefineVariable headerDef) &&
                dg.Columns.Count > defVar.Index && headerDef.Tuple.Count > defVar.Index)
            {
                var textCol = dg.Columns[defVar.Index] as DataGridTextColumn;
                textCol.Header = headerDef.Tuple[defVar.Index].AsString();
            }

            var entries = data.Tuple;
            defVar.Active = true;
            int count = wd.maxElems <= 0 ? entries.Count : Math.Min(entries.Count, wd.maxElems);
            for (int i = 0; i < count; i++)
            {
                MyAssignFunction.AddCell(dg, i, defVar.Index, entries[i]);
            }

            //var rowList = dg.ItemsSource as List<ExpandoObject>;
            if (CSCS_GUI.DEFINES.TryGetValue(wd.actualElemsName, out DefineVariable actualElems))
            {
                actualElems.Value = dg.Items.Count;
                //actualElems.Value = rowList.Count;
                ParserFunction.AddGlobal(wd.actualElemsName, new GetVarFunction(actualElems), false);
            }

            wd.headers[headerName] = data;
            dg.Items.Refresh();
            dg.UpdateLayout();

            return count;
        }

        static int Compare(double num1, double num2)
        {
            return num1 < num2 ? -1 : num2 > num1 ? 1 : 0;
        }

        public static void ResetArrays(FrameworkElement widget, IEnumerable<ExpandoObject> newCollection = null)
        {
            DataGrid dg = widget as DataGrid;
            if (dg == null)
            {
                return;
            }
            var name = dg.DataContext as string;
            if (string.IsNullOrWhiteSpace(name) ||
                !CSCS_GUI.WIDGETS.TryGetValue(name, out CSCS_GUI.WidgetData wd))
            {
                return;
            }

            var rowList = dg.ItemsSource as List<ExpandoObject>;
            if (rowList == null)
            {
                return;
            }

            bool sorting = newCollection != null;
            if (!sorting)
            {
                newCollection = rowList;
            }
            var correctList = newCollection.ToList();

            if (sorting)
            {
                rowList.Clear();
            }
            else
            {
                dg.ItemsSource = null;
            }

            for (int rowNb = 0; rowNb < correctList.Count; rowNb++)
            {
                var row = correctList[rowNb] as IDictionary<String, object>; ;
                for (int colNb = 0; colNb < dg.Columns.Count; colNb++)
                {
                    var colStr = wd.headerNames[colNb];
                    var v = row[colStr];
                    Variable cellValue = wd.colTypes[colNb] == CSCS_GUI.WidgetData.COL_TYPE.STRING ||
                        v is string ? new Variable(v.ToString()) : new Variable((double)v);

                    var headerData = ParserFunction.GetVariableValue(colStr);
                    headerData.SetAsArray();
                    while (headerData.Tuple.Count <= rowNb)
                    {
                        headerData.Tuple.Add(Variable.EmptyInstance);
                    }
                    headerData.Tuple[rowNb] = cellValue;
                    MyAssignFunction.AddCell(dg, rowNb, colNb, cellValue);
                }
            }
            dg.Items.Refresh();
            dg.UpdateLayout();
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

    public class DefineVariable : Variable
    {
        string DATE_FORMAT
        {
            get;
            set;
        } = " ";
        string TIME_FORMAT { get; set; } = "";

        public string Name { get; set; }
        public string DefValue { get; set; }
        public string DefType { get; set; } = "";

        public int Size { get; set; } = 0;
        public int Current { get; set; } = 0;
        public int MaxElements { get; set; } = -1;
        public int Dec { get; set; } = 0;
        public int Array { get; set; } = 0;
        public bool Local { get; set; } = false;
        public bool Up { get; set; } = false;
        public bool Active { get; set; } = false;
        public DefineVariable Dup { get; set; }
        public Variable Init { get; set; }

        public string AssignedString { get; set; }
        public double AssignedNumber { get; set; }

        public bool LocalAssign { get; set; }

        public static new DefineVariable EmptyInstance = new DefineVariable();

        public override Variable Default()
        {
            return EmptyInstance;
        }

        public override double Value
        {
            get
            {
                if (!string.IsNullOrEmpty(DefValue))
                {
                    m_value = Interpreter.Instance.Process(DefValue).AsDouble();
                }
                m_value = Math.Round(m_value, Dec);
                return m_value;
            }
            set
            {
                if (!string.IsNullOrEmpty(DefValue))
                {
                    return;
                }
                if (!LocalAssign)
                {
                    Size = 0;
                }
                AssignedNumber = value;
                m_value = value;
                Type = VarType.NUMBER;
            }
        }

        public override string String
        {
            get
            {
                if (!string.IsNullOrEmpty(DefValue))
                {
                    m_string = Interpreter.Instance.Process(DefValue).AsString();
                }
                return m_string;
            }
            set
            {
                if (!string.IsNullOrEmpty(DefValue))
                {
                    return;
                }
                AssignedString = value;
                if (!LocalAssign)
                {
                    Size = 0;
                }
                m_string = value;
                Type = VarType.STRING;
            }
        }

        public DefineVariable()
        {
        }

        public DefineVariable(string name, string type, Object obj, int index = -1)
        {
            Name = name.ToLower();
            Tuple = new List<Variable>();
            DefType = type.ToLower();
            Index = index;
            Object = obj;
            Type = VarType.ARRAY;
            Active = false;
        }

        public DefineVariable(string name, string type, Object obj, bool active)
        {
            Name = name.ToLower();
            DefType = type.ToLower();
            Object = obj;
            Active = active;
        }

        public DefineVariable(string name, string value,
            string type = "", int size = 0, int dec = 3, int array = 0, bool local = false, bool up = false)
        {
            Name = name.ToLower();
            DefValue = value;
            DefType = type.ToLower();
            Size = size;
            Dec = dec;
            Local = local;
            Up = up;
            Array = array;
            Active = true;
        }

        public DefineVariable(string name, DefineVariable dup, bool local = false)
        {
            Name = name.ToLower();
            Local = local;
            DefValue = dup.DefValue;
            DefType = dup.DefType.ToLower();
            Size = dup.Size;
            Dec = dup.Dec;
            Up = dup.Up;
            Array = dup.Array;
            Dup = dup;
            Active = dup.Active;
        }

        public DefineVariable(List<Variable> a)
        {
            Tuple = a;
        }

        static double CheckValue(string type, int size, Variable varValue)
        {
            double val = varValue.AsDouble();
            switch (type)
            {
                case "b":
                    if (val < Byte.MinValue || val > Byte.MaxValue)
                    {
                        return 0;
                    }
                    break;
                case "i":
                    if (val < short.MinValue || val > short.MaxValue)
                    {
                        return 0;
                    }
                    break;
                case "r":
                    if (val < Int32.MinValue || val > Int32.MaxValue)
                    {
                        return 0;
                    }
                    break;
            }
            if (size > 0)
            {
                var strValue = val.ToString();
                if (strValue.Length > size)
                {
                    return 0;
                }
            }
            return val;
        }

        public void InitVariable(Variable init, ParsingScript script = null, bool update = true, int arrayIndex = -1)
        {
            /*
I  - signed small int (2 bytes), from -32,768 to 32.767
R  - signed int (4 bytes), from -2,147,483,648 to 2,147,483,647
B – unsigned tinyInt (1 byte), from 0 to 255
N – standard FLOAT type (8 bytes)
Sign 	Max Value 	Minimum Value 
Negative	– 1.79E+308	-2.23E-308
Positive	1.79E+308	2.23E-308
 
When using maths, internal precision is max possible for the type. SIZE parameter in DEFINE means – ‘display’ size, that can be returned to the GUI or report.
 
L – logic/boolean (1 byte), internaly represented as 0 or 1, as constant as true  false  .true.  .false.  .t.  .f.  ‘Y’  ‘N’
             * */
            Init = init;
            LocalAssign = true;
            switch (DefType)
            {
                case "a":
                    String = init.AsString();
                    Type = VarType.STRING;
                    if (Size > 0 && m_string.Length > Size)
                    {
                        m_string = m_string.Substring(0, Size);
                    }
                    if (Up)
                    {
                        m_string = m_string.ToUpper();
                    }

                    break;

                case "p":
                case "f":
                    Pointer = init.AsString();
                    Type = VarType.POINTER;
                    break;
                case "d":
                case "t":
                    DateTime = ToDateTime(init.AsString());
                    Type = VarType.DATETIME;
                    break;
                case "l": // "logic" (boolean)
                    Value = ToBool(init.AsString()) ? 1 : 0;
                    Type = VarType.NUMBER;
                    break;
                case "b": // byte
                case "i": // integer
                case "n": // number
                case "r": // small int
                default:
                    Value = CheckValue(DefType, Size, init);
                    Type = VarType.NUMBER;
                    break;
            }

            if (Array > 0)
            {
                DefineVariable item = this.DeepClone() as DefineVariable;
                item.Array = 0;
                if (Tuple == null || arrayIndex < 0)
                {
                    Tuple = new List<Variable>(Array);
                }
                Tuple.AddRange(System.Linq.Enumerable.Repeat(item, Array));
                Type = VarType.ARRAY;
            }

            CSCS_GUI.ChangingBoundVariable = true;
            if (update)
            {
                if (Local)
                {
                    ParserFunction.AddLocalVariable(new GetVarFunction(this), Name);
                }
                else
                {
                    ParserFunction.AddGlobalOrLocalVariable(Name, new GetVarFunction(this), script);
                }
                if (CSCS_GUI.DEFINES.TryGetValue(Name, out DefineVariable existing) && existing.Object != null)
                {
                    this.Object = existing.Object;
                    this.DefType = existing.DefType;
                    this.Index = existing.Index;
                }
                CSCS_GUI.DEFINES[Name] = this;
            }
            CSCS_GUI.ChangingBoundVariable = false;

            LocalAssign = false;
        }

        public string GetDateFormat()
        {
            if (!string.IsNullOrWhiteSpace(DATE_FORMAT))
            {
                //return DATE_FORMAT;
            }
            string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var usDate = !sysFormat.StartsWith("dd") && !sysFormat.EndsWith("dd");
            DATE_FORMAT = "dd/MM/yyyy";
            switch (Size)
            {
                case 5:
                    DATE_FORMAT = usDate ? "MM/dd" : "dd/MM";
                    break;
                case 7:
                    DATE_FORMAT = usDate ? "MM/yyyy" : "MM/yyyy";
                    break;
                case 8:
                    DATE_FORMAT = usDate ? "MM/dd/yy" : "dd/MM/yy";
                    break;
                case 10:
                    DATE_FORMAT = usDate ? "MM/dd/yyyy" : "dd/MM/yyyy";
                    break;
            }
            return DATE_FORMAT;
        }

        public string GetTimeFormat()
        {
            if (!string.IsNullOrWhiteSpace(TIME_FORMAT))
            {
                return TIME_FORMAT;
            }
            TIME_FORMAT = "dd/MM/yyyy";
            switch (Size)
            {
                case 3:
                    TIME_FORMAT = "fff";
                    break;
                case 5:
                    TIME_FORMAT = "HH:mm";
                    break;
                case 6:
                    TIME_FORMAT = "ss.fff";
                    break;
                case 8:
                    TIME_FORMAT = "HH:mm:ss";
                    break;
                case 11:
                    TIME_FORMAT = "HH:mm:ss.ff";
                    break;
                case 12:
                    TIME_FORMAT = "HH:mm:ss.fff";
                    break;
            }
            return TIME_FORMAT;
        }

        public bool ToBool(string strValue)
        {
            char ch = string.IsNullOrWhiteSpace(strValue) ? '0' : strValue.ToLower()[0];
            return ch == 't' || ch == 'y' || ch == '1';
        }

        public DateTime ToDateTime(string strValue)
        {
            DateTime dt = DateTime.MinValue;
            if (DefType == "d")
            {
                if (!string.IsNullOrWhiteSpace(strValue) &&
                    !DateTime.TryParseExact(strValue, GetDateFormat(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    throw new ArgumentException("Error: Couldn't parse [" + strValue + "] with format [" + GetDateFormat() + "]");
                }
            }
            if (DefType == "t")
            {
                if (!string.IsNullOrWhiteSpace(strValue) &&
                    !DateTime.TryParseExact(strValue, GetTimeFormat(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    throw new ArgumentException("Error: Couldn't parse [" + strValue + "] with format [" + GetTimeFormat() + "]");
                }
                dt = dt.Subtract(new TimeSpan(dt.Date.Ticks));
            }

            return dt;
        }

        public override DateTime AsDateTime()
        {
            return m_datetime;
        }

        public override string AsString(bool isList = true,
                               bool sameLine = true,
                               int maxCount = -1)
        {
            if (DefType == "d")
            {
                return DateTime.ToString(GetDateFormat());
            }
            if (DefType == "t")
            {
                return DateTime.ToString(GetTimeFormat());
            }
            if (DefType == "l")
            {
                return AsDouble() == 0 ? "false" : "true";
            }
            if (Size > 0 && (DefType == "n" || DefType == "i" || DefType == "b" || DefType == "r"))
            {
                var strValue = Value.ToString();
                if (strValue.Length > Size)
                {
                    return "0";
                }
                return strValue;
            }
            if (Size > 0 && DefType == "a" && m_string.Length > Size)
            {
                m_string = m_string.Substring(0, Size);
                return m_string;
            }

            try
            {
                return base.AsString(isList, sameLine, maxCount);
            }
            catch(Exception exc)
            {
                Console.WriteLine(exc);
                return "";
            }
        }

        public override double AsDouble()
        {
            return base.AsDouble();
        }

        public override bool Preprocess()
        {
            /*if (DefType == "datagrid" && Type == VarType.ARRAY)
            {
                var dg = Object as DataGrid;
                if (dg != null && dg.DataContext is string &&
                    CSCS_GUI.WIDGETS.TryGetValue(dg.DataContext as string, out CSCS_GUI.WidgetData wd) &&
                    wd.needsReset && wd.headers.ContainsKey(Name))
                {
                    FillWidgetFunction.ResetArrays(dg);
                    wd.needsReset = false;
                }
            }*/
            return base.Preprocess();
        }

        public override void AddToDate(Variable valueB, int sign)
        {
            if (valueB.Type == Variable.VarType.NUMBER)
            {
                var dt = AsDateTime();
                var delta = valueB.Value * sign;
                if (DefType == "t")
                {
                    DateTime = dt.AddSeconds(delta);
                }
                else
                {
                    DateTime = dt.AddDays(delta);
                }
            }
            else
            {
                base.AddToDate(valueB, sign);
            }
        }
    }

    class MyPointerFunction : PointerFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            var pointer = script.Pointer;
            List<string> args = Utils.GetTokens(script);
            Utils.CheckArgs(args.Count, 1, m_name);

            DefineVariable defVar;
            if (!CSCS_GUI.DEFINES.TryGetValue(m_name, out defVar))
            {
                script.Pointer = pointer;
                return base.Evaluate(script);
            }

            defVar.Pointer = args[0];
            return defVar;
        }
    }

    class MyAssignFunction : AssignFunction
    {
        bool m_pointerAssign;
        string m_originalName;
        int m_arrayIndex = -1;

        protected override Variable Evaluate(ParsingScript script)
        {
            DefineVariable defVar = IsDefinedVariable(script);
            if (defVar != null)
            {
                return DoAssign(script, m_name, defVar);
            }
            var res = Assign(script, m_originalName);
            return ResetNotDefined(res);
        }

        protected override async Task<Variable> EvaluateAsync(ParsingScript script)
        {
            DefineVariable defVar = IsDefinedVariable(script);
            if (defVar != null)
            {
                return DoAssign(script, m_name, defVar);
            }
            var res = await AssignAsync(script, m_originalName);
            return ResetNotDefined(res);
        }

        Variable ResetNotDefined(Variable result)
        {
            DefineVariable defVar = result as DefineVariable;
            if (defVar != null && defVar.Active)
            {
                defVar.DefType = "";
            }
            return result;
        }

        protected DefineVariable IsDefinedVariable(ParsingScript script)
        {
            m_originalName = m_name;
            m_pointerAssign = m_name.StartsWith("&");
            if (m_pointerAssign)
            {
                m_name = m_name.Substring(1);
            }

            int argStart = m_name.IndexOf(Constants.START_ARRAY);
            if (argStart > 0)
            {
                m_name = m_name.Substring(0, argStart);
            }

            if (!CSCS_GUI.DEFINES.TryGetValue(m_name, out DefineVariable defVar))
            {
                return null;
            }

            if (argStart > 0)
            {
                int argEnd = m_originalName.IndexOf(Constants.END_ARRAY, argStart + 1);
                var index = m_originalName.Substring(argStart + 1, argEnd - argStart - 1);
                m_arrayIndex = Interpreter.Instance.Process(index).AsInt();
                if (defVar.DefType != "datagrid" && defVar.Tuple != null && m_arrayIndex >= defVar.Tuple.Count - 1)
                {
                    defVar = defVar.Tuple.ElementAt(m_arrayIndex) as DefineVariable;
                }
            }
            return defVar;
        }

        public Variable DoAssign(ParsingScript script, string varName, DefineVariable defVar, bool localIfPossible = false)
        {
            m_name = Constants.GetRealName(varName);
            script.CurrentAssign = m_name;

            Variable varValue = Utils.GetItem(script);
            if (m_pointerAssign)
            {
                if (CSCS_GUI.DEFINES.TryGetValue(defVar.Pointer, out DefineVariable refValue))
                {
                    refValue.InitVariable(varValue, script, false);
                    ParserFunction.AddGlobalOrLocalVariable(m_originalName,
                            new GetVarFunction(refValue));
                    return refValue;
                }
            }
            else
            {
                if (defVar.DefType == "datagrid")
                {
                    var dg = defVar.Object as DataGrid;
                    CSCS_GUI.WidgetData wd;
                    if (defVar.Index < 0 && CSCS_GUI.WIDGETS.TryGetValue(m_name, out wd))
                    {
                        if (defVar.Active)
                        {
                            var column = dg.Columns[m_arrayIndex] as DataGridTextColumn;
                            column.Header = varValue.AsString();
                        }
                        if (CSCS_GUI.DEFINES.TryGetValue(m_name, out DefineVariable headerDef))
                        {
                            headerDef.SetAsArray();
                            while (headerDef.Tuple.Count < dg.Columns.Count)
                            {
                                headerDef.Tuple.Add(Variable.EmptyInstance);
                            }
                            headerDef.Tuple[m_arrayIndex] = varValue;
                        }
                    }
                    else
                    {
                        var rowList = dg.ItemsSource as List<ExpandoObject>;
                        if (!CSCS_GUI.WIDGETS.TryGetValue(dg.DataContext as string, out wd) ||
                            !CSCS_GUI.DEFINES.TryGetValue(wd.actualElemsName, out DefineVariable actualElems))
                        {
                            return Variable.EmptyInstance;
                        }
                        if (wd.maxElems <= m_arrayIndex)
                        {
                            throw new ArgumentException("Requested element is too big: " + m_arrayIndex + ". Max=" + wd.maxElems);
                        }
                        while (defVar.Tuple.Count < m_arrayIndex + 1)
                        {
                            defVar.Tuple.Add(Variable.EmptyInstance);
                        }
                        if (defVar.Active)
                        { // Changing value of an existing cell
                            AddCell(dg, m_arrayIndex, defVar.Index, varValue);

                            FillWidgetFunction.ResetArrays(dg);
                            actualElems.Value = rowList.Count;

                            dg.Items.Refresh();
                            dg.UpdateLayout();
                        }
                    }
                }
                else if (defVar.DefType == "linecounter")
                {
                    var dg = defVar.Object as DataGrid;
                    dg.SelectedIndex = varValue.AsInt();
                    var rowList = dg.ItemsSource as List<ExpandoObject>;
                    object item = rowList[dg.SelectedIndex];
                    dg.SelectedItem = item;
                    dg.ScrollIntoView(item);
                    DataGridRow row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(dg.SelectedIndex);
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
                else if (defVar.DefType == "maxelems")
                {
                    var dg = defVar.Object as DataGrid;
                    var wd = CSCS_GUI.WIDGETS[dg.DataContext as string];
                    wd.maxElems = varValue.AsInt();
                    if (wd.actualElems <= 0 || wd.actualElems > wd.maxElems)
                    {
                        wd.actualElems = wd.maxElems;
                        if (!string.IsNullOrWhiteSpace(wd.actualElemsName) &&
                            CSCS_GUI.DEFINES.TryGetValue(wd.actualElemsName, out DefineVariable actualElems))
                        {
                            actualElems.Value = wd.maxElems;
                            ParserFunction.AddGlobal(wd.actualElemsName, new GetVarFunction(actualElems), false);
                        }
                    }
                    var rowList = dg.ItemsSource == null ? new List<ExpandoObject>() : 
                                  dg.ItemsSource as List<ExpandoObject>;
                    while (rowList.Count > wd.maxElems)
                    {
                        rowList.RemoveAt(rowList.Count - 1);
                    }
                    dg.ItemsSource = rowList;
                    dg.Items.Refresh();
                    dg.UpdateLayout();
                }
                else
                {
                    defVar.InitVariable(varValue, script, false, m_arrayIndex);
                    OnVariableChange(m_name, defVar, true);
                }
            }

            ParserFunction.AddGlobalOrLocalVariable(m_name, new GetVarFunction(varValue));
            return defVar;
        }

        public static void AddCell(DataGrid dg, int rowNb, int colNb, Variable varValue)
        {
            var name = dg.DataContext as string;
            if (string.IsNullOrWhiteSpace(name) || !CSCS_GUI.WIDGETS.TryGetValue(name, out CSCS_GUI.WidgetData wd))
            {
                return;
            }
            if (dg.ItemsSource == null)
            {
                dg.ItemsSource = new List<ExpandoObject>();
                //dg.ItemsSource = new ObservableCollection<ExpandoObject>();
            }

            if (varValue.Type == Variable.VarType.NUMBER)
            {
                AddCell(dg, rowNb, colNb, wd, varValue.AsDouble());
                wd.colTypes[colNb] = CSCS_GUI.WidgetData.COL_TYPE.NUMBER;
            }
            else
            {
                AddCell(dg, rowNb, colNb, wd, varValue.AsString());
            }
        }

        static void AddCell<T>(DataGrid dg, int rowNb, int colNb, CSCS_GUI.WidgetData wd, T value)
        {
            var rowList = dg.ItemsSource as List<ExpandoObject>;

            while (rowList.Count < rowNb + 1)
            {
                var expando = GetNewRow(dg, wd);
                rowList.Add(expando);
            }

            var pp = rowList[rowNb] as IDictionary<String, object>;
            pp[wd.headerNames[colNb]] = value;
        }

        public static ExpandoObject GetNewRow(DataGrid dg, CSCS_GUI.WidgetData wd)
        {
            dynamic expando = new ExpandoObject();
            var p = expando as IDictionary<String, object>;
            for (int i = 0; i < dg.Columns.Count; i++)
            {
                p[wd.headerNames[i]] = "";
            }
            return expando;
        }

        override public ParserFunction NewInstance()
        {
            return new MyAssignFunction();
        }
    }

    class AsyncCallFunction : ParserFunction, INumericFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            string funcName = Utils.GetToken(script, Constants.TOKEN_SEPARATION);
            script.MoveForwardIf(',');
            string callback = Utils.GetToken(script, Constants.TOKEN_SEPARATION);
            script.MoveForwardIf(',');

            List<Variable> args = script.GetFunctionArgs();

            CustomFunction newThreadFunction = ParserFunction.GetFunction(funcName, null) as CustomFunction;
            if (newThreadFunction == null)
            {
                throw new ArgumentException("Error: Couldn't find function [" + funcName + "]");
            }
            CustomFunction callbackFunction = ParserFunction.GetFunction(callback, null) as CustomFunction;
            if (callbackFunction == null)
            {
                throw new ArgumentException("Error: Couldn't find function [" + callback + "]");
            }

            ThreadPool.QueueUserWorkItem(unused => ThreadProc(newThreadFunction, callbackFunction, args));
            return Variable.EmptyInstance;
        }

        static void ThreadProc(CustomFunction newThreadFunction, CustomFunction callbackFunction, List<Variable> args)
        {
            Variable result = Interpreter.Run(newThreadFunction, args);

            var resultArgs = new List<Variable>() {
                new Variable(newThreadFunction.Name), result
            };

            RunOnMainFunction.RunOnMainThread(callbackFunction, resultArgs);
        }
    }
}
