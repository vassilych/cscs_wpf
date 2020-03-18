using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Dynamic;
using System.Reflection;
using SplitAndMerge;
using System.Security.Cryptography;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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
        public static MainWindow MainWindow { get; set; }

        public static Dictionary<string, Control> Controls { get; set; } = new Dictionary<string, Control>();
        //public static Action<string, string> OnWidgetClick;

        static Dictionary<string, string> s_actionHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_preActionHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_postActionHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_keyDownHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_keyUpHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_textChangedHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_selChangedHandlers = new Dictionary<string, string>();
        static Dictionary<string, string> s_mouseHoverHandlers = new Dictionary<string, string>();

        static Dictionary<string, Variable> s_boundVariables = new Dictionary<string, Variable>();
        //static Dictionary<string, TabPage> s_tabPages           = new Dictionary<string, TabPage>();
        //static TabControl s_tabControl;

        static bool s_changingBoundVariable;

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
            ParserFunction.RegisterFunction(Constants.MSG, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.DEFINE, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.SET_OBJECT, new VariableArgsFunction(true));
            ParserFunction.RegisterFunction(Constants.CHAIN, new ChainFunction(false));
            ParserFunction.RegisterFunction(Constants.PARAM, new ChainFunction(true));
            ParserFunction.RegisterFunction(Constants.QUIT, new QuitStatement());

            ParserFunction.RegisterFunction(Constants.WITH, new ConstantsFunction());
            ParserFunction.RegisterFunction(Constants.NEWRUNTIME, new ConstantsFunction());

            ParserFunction.RegisterFunction("OpenFile", new OpenFileFunction());
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

            ParserFunction.RegisterFunction("RunOnMain", new RunOnMainFunction());
            ParserFunction.RegisterFunction("RunExec", new RunExecFunction());

            Constants.FUNCT_WITH_SPACE.Add("SetText");
            Constants.FUNCT_WITH_SPACE.Add(Constants.DEFINE);
            Constants.FUNCT_WITH_SPACE.Add(Constants.MSG);
            Constants.FUNCT_WITH_SPACE.Add(Constants.SET_OBJECT);
            Constants.FUNCT_WITH_SPACE.Add(Constants.CHAIN);
            Constants.FUNCT_WITH_SPACE.Add(Constants.PARAM);

            //ParserFunction.RegisterFunction("funcName", new MyFunction());

            Interpreter.Instance.OnOutput += Print;
            ParserFunction.OnVariableChange += OnVariableChange;
            AddActions();

            Precompiler.AddNamespace("using WpfCSCS;");
            Precompiler.AddNamespace("using System.Windows;");
            Precompiler.AddNamespace("using System.Windows.Controls;");
            Precompiler.AddNamespace("using System.Windows.Controls.Primitives;");
            Precompiler.AddNamespace("using System.Windows.Data;");
            Precompiler.AddNamespace("using System.Windows.Documents;");
            Precompiler.AddNamespace("using System.Windows.Input;");
            Precompiler.AddNamespace("using System.Windows.Media;");
        }

        public static string GetWidgetBindingName(Control widget)
        {
            var widgetName = widget == null || widget.DataContext == null ? "" : widget.DataContext.ToString();
            return widgetName;
        }

        static void CacheControl(Control widget)
        {
            if (widget != null && widget.DataContext != null)
            {
                Controls[widget.DataContext.ToString().ToLower()] = widget;
            }
        }

        static bool OnVariableChange(string name, Variable newValue, bool isGlobal)
        {
            if (s_changingBoundVariable)
            {
                return true;
            }
            var widgetName = name.ToLower();
            if (!s_boundVariables.TryGetValue(widgetName, out _))
            {
                return false;
            }

            var widget = GetWidget(widgetName);
            var text = newValue.AsString();

            SetTextWidgetFunction.SetText(widget, text);
            s_boundVariables[widgetName] = newValue;
            return true;
        }

        static void UpdateVariable(Control widget, string text)
        {
            var widgetName = GetWidgetBindingName(widget);
            if (string.IsNullOrEmpty(widgetName))
            {
                return;
            }
            s_changingBoundVariable = true;
            ParserFunction.AddGlobalOrLocalVariable(widgetName,
                                        new GetVarFunction(new Variable(text)));
            s_changingBoundVariable = false;
        }

        static void Print(object sender, OutputAvailableEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine(e.Output);
        }

        public static void AddActions()
        {
            CacheControls();
            foreach (KeyValuePair<string, Control> entry in Controls)
            {
                AddActions(entry.Value);
            }
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
            CustomFunction.Run(funcName, new Variable(widgetName), result);
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
                CustomFunction.Run(funcName, new Variable(widgetName), new Variable(arg));
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
                CustomFunction.Run(funcName, new Variable(widgetName), new Variable(arg));
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
                CustomFunction.Run(funcName, new Variable(widgetName),
                    new Variable(((char)e.Key).ToString()));
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
                CustomFunction.Run(funcName, new Variable(widgetName),
                    new Variable(((char)e.Key).ToString()));
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
            UpdateVariable(widget, text.AsString());

            string funcName;
            if (s_textChangedHandlers.TryGetValue(widgetName, out funcName))
            {
                CustomFunction.Run(funcName, new Variable(widgetName), text);
            }
        }

        private static void Widget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Control widget = sender as Selector;
            var widgetName = GetWidgetBindingName(widget);
            if (s_selChangedHandlers.TryGetValue(widgetName, out string funcName))
            {
                var item = e.AddedItems.Count > 0 ? e.AddedItems[0].ToString() : e.RemovedItems.Count > 0 ? e.RemovedItems[0].ToString() : "";
                CustomFunction.Run(funcName, new Variable(widgetName), new Variable(item));
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
                CustomFunction.Run(funcName, new Variable(widgetName), new Variable(e.ToString()));
            }
        }

        public static Control GetWidget(string name)
        {
            CacheControls();
            Control control;
            if (Controls.TryGetValue(name.ToLower(), out control))
            {
                return control;
            }
            return null;
        }

        public static void CacheControls(bool force = false)
        {
            if ((!force && Controls.Count > 0) || MainWindow == null)
            {
                return;
            }

            Grid content = MainWindow.Content as Grid;
            var children = content.Children;
            foreach (var child in children)
            {
                if (child is TabControl)
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
                                                CacheControl(child2 as Control);
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
                    CacheControl(child as Control);
                }
            }
        }

        public static void AddActions(Control widget)
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

            AddActionHandler(widgetName, clickAction, widget);
            AddPreActionHandler(widgetName, preClickAction, widget);
            AddPostActionHandler(widgetName, postClickAction, widget);
            AddKeyDownHandler(widgetName, keyDownAction, widget);
            AddKeyUpHandler(widgetName, keyUpAction, widget);
            AddTextChangedHandler(widgetName, textChangeAction, widget);
            AddSelectionChangedHandler(widgetName, selectionChangedAction, widget);
            AddMouseHoverHandler(widgetName, mouseHoverAction, widget);
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

        public static void RunScript(string fileName)
        {
            Init();

            EncodeFileFunction.EncodeDecode(fileName, false);
            string script = Utils.GetFileContents(fileName);
            EncodeFileFunction.EncodeDecode(fileName, true);

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
                throw;
            }
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
                    combo.SelectedIndex = index;
                }
            }
            else if (widget is CheckBox)
            {
                var checkBox = widget as CheckBox;
                checkBox.IsChecked = text == "1" || text.ToLower() == "true";
            }
            else if (widget is ContentControl)
            {
                var contentable = widget as ContentControl;
                contentable.Content = text;
            }
            else if (widget is TextBox)
            {
                var textBox = widget as TextBox;
                textBox.Text = text;
            }
            else if (widget is RichTextBox)
            {
                var richTextBox = widget as RichTextBox;
                richTextBox.Document.Blocks.Clear();
                richTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
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
            var caption = Utils.GetSafeString(args, 1, "Question");
            var answerType = Utils.GetSafeString(args, 2, "ok").ToLower();
            var messageType = Utils.GetSafeString(args, 3, "ok").ToLower();

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
        protected override Variable Evaluate(ParsingScript script)
        {
            /*List<Variable> args =*/
            script.GetFunctionArgs();
            return OpenFile();
        }
        public static Variable OpenFile()
        {
            Microsoft.Win32.OpenFileDialog openFile = new Microsoft.Win32.OpenFileDialog();
            if (openFile.ShowDialog() != true)
            {
                return Variable.EmptyInstance;
            }

            var fileName = openFile.FileName;
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

        public ChainFunction(bool paramMode = false)
        {
            m_paramMode = paramMode;
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
                    var cmdArgsArr = cmdArgs[1].Split(new char[] { ',' });
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
                    ParserFunction.AddLocalVariable(func);
                    //msg += func.Name + "=[" + parameters[i].AsString() + "] ";
                }
                //MessageBox.Show(msg, parameters.Count + " args", MessageBoxButton.OK, MessageBoxImage.Hand);
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

            ParsingScript chainScript = IncludeFile.GetIncludeFileScript(tempScript, chainName);
            chainScript.StackLevel = ParserFunction.AddStackLevel(chainScript.Filename);
            chainScript.CurrentModule = chainName;

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
                throw new ArgumentException("Chained script finished without Quit statement.");
            }

            return result;
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
                                                                        Utils.GetItem(script, false);
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
                //RegisterFunction(objectName, new GetVarFunction(newVar), true);
                AddGlobalOrLocalVariable(objectName, new GetVarFunction(newVar), script);
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
}
