using Microsoft.Web.WebView2.Wpf;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfControlsLibrary;

namespace WpfCSCS
{
    public class TasFunctions
    {
        CSCS_GUI Gui { get; set; }

        public void Init(CSCS_GUI gui)
        {
            Gui = gui;
            Interpreter interpreter = gui.Interpreter;

            interpreter.RegisterFunction(Constants.MAINMENU, new MAINMENUcommand());
            interpreter.RegisterFunction(Constants.WINFORM, new WINFORMcommand(true));

            interpreter.RegisterFunction(Constants.READ_XML_FILE, new ReadXmlFileFunction());
            interpreter.RegisterFunction(Constants.READ_TAGCONTENT_FROM_XMLSTRING, new ReadTagContentFromXmlStringFunction());

            interpreter.RegisterFunction(Constants.SET_FOCUS, new SetFocusFunction());
            interpreter.RegisterFunction(Constants.LAST_OBJ, new LastObjFunction());
            interpreter.RegisterFunction(Constants.LAST_OBJ_CLICKED, new LastObjClickedFunction());

            interpreter.RegisterFunction(Constants.STRINGS, new StringsFunction());

            interpreter.RegisterFunction(Constants.STATUS_BAR, new StatusBarFunction());
            interpreter.RegisterFunction(Constants.GET_FILE, new GET_FILEFunction());

            interpreter.RegisterFunction(Constants.HORIZONTAL_BAR, new HorizontalBarFunction());

            interpreter.RegisterFunction(Constants.DUAL_LIST_EXEC, new DUAL_LIST_EXECFunction());

            interpreter.RegisterFunction(Constants.GET_SELECTED_GRID_ROW, new GetSelectedGridRowFunction());

            interpreter.RegisterFunction(Constants.DLLFC, new DLLFCFunction());
            interpreter.RegisterFunction(Constants.FFILE, new FFILEFunction());
            interpreter.RegisterFunction(Constants.LIKE, new LIKEFunction());
            interpreter.RegisterFunction(Constants.PARSEFILE, new PARSEFILEFunction());
            interpreter.RegisterFunction(Constants.LCHR, new LCHRFunction());
            interpreter.RegisterFunction(Constants.ISUP, new ISUPFunction());
            interpreter.RegisterFunction(Constants.MAKE_DIR, new MAKE_DIRFunction());
            interpreter.RegisterFunction(Constants.TRIM, new TRIMFunction());
            interpreter.RegisterFunction(Constants.GFLD, new GFLDFunction());
            interpreter.RegisterFunction(Constants.SNDX, new SNDXFunction());
            interpreter.RegisterFunction(Constants.JUST, new JUSTFunction());
            interpreter.RegisterFunction(Constants.DIFF, new DIFFFunction());
            interpreter.RegisterFunction(Constants.FARRAY, new FARRAYFunction());
            interpreter.RegisterFunction(Constants.OS, new OSFunction());
            interpreter.RegisterFunction(Constants.TRAP, new TRAPFunction());
            interpreter.RegisterFunction(Constants.WebBrowse, new WebBrowseFunction());
            interpreter.RegisterFunction(Constants.ISNUM, new ISNUMFunction());
            interpreter.RegisterFunction(Constants.GET_FORM_NAME, new GET_FORM_NAMEcommand());
            interpreter.RegisterFunction(Constants.ISLO, new ISLOFunction());
            interpreter.RegisterFunction(Constants.ISAL, new ISALFunction());
            interpreter.RegisterFunction(Constants.PRINTER_NAME, new PRINTER_NAMEFunction());
            interpreter.RegisterFunction(Constants.DELF, new DELFFunction());
            interpreter.RegisterFunction(Constants.BELL, new BELLFunction());
            interpreter.RegisterFunction(Constants.ENCRYPTSTR, new ENCRYPTSTRFunction());
            interpreter.RegisterFunction(Constants.Decryptstr, new DecryptstrFunction());
            interpreter.RegisterFunction(Constants.CDOW, new CDOWFunction());
            interpreter.RegisterFunction(Constants.XPATH, new XPATHFunction());
            interpreter.RegisterFunction(Constants.PLAYWAV, new PLAYWAVFunction());
            interpreter.RegisterFunction(Constants.FILE_STORE, new FILE_STOREFunction());
            interpreter.RegisterFunction(Constants.CPATH, new CPATHFunction());
            interpreter.RegisterFunction(Constants.CHR, new CHRFunction());
            interpreter.RegisterFunction(Constants.CMNTH, new CMNTHFunction());
            interpreter.RegisterFunction(Constants.DIR_EXISTS, new DIR_EXISTSFunction());
            interpreter.RegisterFunction(Constants.DEC, new DECFunction());
            interpreter.RegisterFunction(Constants.LOC, new LOCFunction());
            interpreter.RegisterFunction(Constants.ELOC, new ELOCFunction());
            interpreter.RegisterFunction(Constants.ASC, new ASCFunction());
            interpreter.RegisterFunction(Constants.DOM, new DOMFunction());
            interpreter.RegisterFunction(Constants.DSPCE, new DSPCEFunction());
            interpreter.RegisterFunction(Constants.HEX, new HEXFunction());
            interpreter.RegisterFunction(Constants.REGEDIT, new REGEDITFunction());
            interpreter.RegisterFunction(Constants.EMAIL, new EMAILFunction());
	  interpreter.RegisterFunction(Constants.TPATH, new TPATHFunction());

            interpreter.RegisterFunction("FillOutGrid", new FillOutGridFunction());
            interpreter.RegisterFunction("FillOutGridFromDB", new FillOutGridFunction(true));
            interpreter.RegisterFunction("BindSQL", new BindSQLFunction());

            interpreter.RegisterFunction("NewBindSQL", new NewBindSQLFunction());

            interpreter.RegisterFunction("WhoAmI", new WhoAmIFunction());

            interpreter.RegisterFunction("ProgressBar", new ProgressBarFunction());
        }

    }

    class MAINMENUcommand : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            return Variable.EmptyInstance;
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
            Gui = CSCS_GUI.GetInstance(script);
            if (m_paramMode)
            {
                var NameOrPathOfXamlForm = Utils.GetBodyBetween(script, '\0', '\0', Constants.END_STATEMENT);
                if (NameOrPathOfXamlForm.EndsWith(".xaml") == false)
                {
                    NameOrPathOfXamlForm = NameOrPathOfXamlForm + ".xaml";
                }
                NameOrPathOfXamlForm = script.GetFilePath(NameOrPathOfXamlForm);
                if (File.Exists(NameOrPathOfXamlForm))
                {
                    var parentWin = Gui.GetParentWindow(script);
                    SpecialWindow modalwin;
                    if (parentWin != null && script.ParentScript != null &&
                        !script.ParentScript.OriginalScript.Contains(Constants.MAINMENU))
                    {
                        var winMode = SpecialWindow.MODE.SPECIAL_MODAL;
                        modalwin = CreateNew(NameOrPathOfXamlForm, parentWin, winMode, script);
                    }
                    else
                    {
                        var winMode = SpecialWindow.MODE.NORMAL;
                        modalwin = CreateNew(NameOrPathOfXamlForm, parentWin, winMode, script);
                    }


                    return new Variable(modalwin.Instance.Tag.ToString());
                }
                else
                {
                    MessageBox.Show($"The file {NameOrPathOfXamlForm} does not exist! Closing program.");
                    Environment.Exit(0);
                    return null;
                }
            }
            else return null;
        }
    }
    class GET_FORM_NAMEcommand : NewWindowFunction
    {
        bool m_paramMode;

        public GET_FORM_NAMEcommand(bool paramMode = false)
        {
            m_paramMode = paramMode;
        }

        protected override Variable Evaluate(ParsingScript script)
        {
            Gui = CSCS_GUI.GetInstance(script);
            var gui = script.Context as CSCS_GUI;
            Window win;
            if (CSCS_GUI.File2Window.TryGetValue(script.Filename, out win))
            {
                //var ib = new InputBinding(new KeyCommand((object arg1) => { return true; }, (object obj) => { gui.Interpreter.Run(toRun.String, null, null, null, script); }), new KeyGesture(keyPressed));
                //win.InputBindings.Add(ib);
               return new Variable(win.Name);
            }
            return null;
        }
    }

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

    class SetFocusFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var gui = CSCS_GUI.GetInstance(script);
            var widgetName = Utils.GetSafeString(args, 0);
            var widget = gui.GetWidget(widgetName);
            if (widget == null || !(widget is Control))
            {
                return Variable.EmptyInstance;
            }
            //CSCS_GUI.skipPostEvent = true;
            widget.Focus();

            return Variable.EmptyInstance;
        }
    }

    class LastObjFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name);

            var gui = CSCS_GUI.GetInstance(script);
            if (string.IsNullOrEmpty(gui.LastObjWidgetName))
            {
                return Variable.EmptyInstance;
            }
            else
            {
                return new Variable(gui.LastObjWidgetName);
            }
        }
    }

    class LastObjClickedFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name);

            var gui = CSCS_GUI.GetInstance(script);
            if (string.IsNullOrEmpty(gui.LastObjClickedWidgetName))
            {
                return Variable.EmptyInstance;
            }
            else
            {
                return new Variable(gui.LastObjClickedWidgetName);
            }
        }
    }

    class StringsFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var gui = CSCS_GUI.GetInstance(script);

            var widgetName = Utils.GetSafeString(args, 0);
            var listName = Utils.GetSafeString(args, 1);
            var option = Utils.GetSafeString(args, 2);
            var argStr = Utils.GetSafeString(args, 3);
            var argNum = Utils.GetSafeInt(args, 4);
            //var arg3 = Utils.GetSafeString(args, 4);

            var widget = gui.GetWidget(widgetName);
            if (widget is ASMemoBox)
            {
                var mtb = widget as ASMemoBox;
                List<string> lines = mtb.Text.Split('\n').ToList();

                if (option == "stCount")
                    return new Variable(lines.Count);
                else if (option == "stGetLine")
                {
                    return new Variable(lines[argNum]);
                }
                else if (option == "stGetText")
                {
                    return new Variable(mtb.Text);
                }
                else if (option == "stSetLine")
                {
                    lines[argNum] = argStr;
                    mtb.Text = string.Join("\n", lines);
                    return Variable.EmptyInstance;
                }
                else if (option == "stSetText")
                {
                    mtb.Text = argStr;
                    return Variable.EmptyInstance;
                }
                else if (option == "stClear")
                {
                    mtb.Text = "";
                    return Variable.EmptyInstance;
                }
                else if (option == "stAddLine")
                {
                    lines.Add(argStr);
                    mtb.Text = string.Join("\n", lines);
                    return Variable.EmptyInstance;
                }
                else if (option == "stDelLine")
                {
                    lines.RemoveAt(argNum);
                    mtb.Text = string.Join("\n", lines);
                    return Variable.EmptyInstance;
                }
                else if (option == "stInsLine")
                {
                    lines.Insert(argNum, argStr);
                    mtb.Text = string.Join("\n", lines);
                    return Variable.EmptyInstance;
                }
                else if (option == "stLoad")
                {
                    string text = File.ReadAllText(argStr);
                    mtb.Text = text;
                    return Variable.EmptyInstance;
                }
                else if (option == "stSave")
                {
                    File.WriteAllText(argStr, mtb.Text);
                    return Variable.EmptyInstance;
                }
                else if (option == "stSort")
                {
                    lines.Sort();
                    mtb.Text = string.Join("\n", lines);
                    return Variable.EmptyInstance;
                }
                else if (option == "stFind" || option == "stLocate")
                {
                    var index = Int32.MaxValue;
                    index = lines.FindIndex(p => p == argStr);
                    return new Variable(index);
                }
            }
            else if (widget is ASDualListDialogHelper)
            {
                var dldh = widget as ASDualListDialogHelper;
                List<string> leftLines = dldh.List1;
                List<string> rightLines = dldh.List2;

                if (option == "stAddLine")
                {
                    leftLines.Add(argStr);
                    return Variable.EmptyInstance;
                }
                else if (option == "stGetLine")
                {
                    return new Variable(rightLines[argNum]);
                }
                else if (option == "stCount")
                    return new Variable(rightLines.Count);
                //else if (option == "stCount")
                //    return new Variable(lines.Count);
                //else if (option == "stGetLine")
                //{
                //    return new Variable(lines[argNum]);
                //}
                //else if (option == "stGetText")
                //{
                //    return new Variable(mtb.Text);
                //}
                //else if (option == "stSetLine")
                //{
                //    lines[argNum] = argStr;
                //    mtb.Text = string.Join("\n", lines);
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stSetText")
                //{
                //    mtb.Text = argStr;
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stClear")
                //{
                //    mtb.Text = "";
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stAddLine")
                //{
                //    lines.Add(argStr);
                //    mtb.Text = string.Join("\n", lines);
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stDelLine")
                //{
                //    lines.RemoveAt(argNum);
                //    mtb.Text = string.Join("\n", lines);
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stInsLine")
                //{
                //    lines.Insert(argNum, argStr);
                //    mtb.Text = string.Join("\n", lines);
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stLoad")
                //{
                //    string text = File.ReadAllText(argStr);
                //    mtb.Text = text;
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stSave")
                //{
                //    File.WriteAllText(argStr, mtb.Text);
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stSort")
                //{
                //    lines.Sort();
                //    mtb.Text = string.Join("\n", lines);
                //    return Variable.EmptyInstance;
                //}
                //else if (option == "stFind" || option == "stLocate")
                //{
                //    var index = Int32.MaxValue;
                //    index = lines.FindIndex(p => p == argStr);
                //    return new Variable(index);
                //}
            }
            else if (widget is ListBox)
            {
                var dldh = widget as ListBox;
                var lines = dldh.Items;

                if (option == "stAddLine")
                {
                    lines.Add(new ListBoxItem() { Content = argStr});
                    return Variable.EmptyInstance;
                }
                else if (option == "stGetLine")
                {
                    return new Variable(dldh.Items[argNum]);
                }
                else if (option == "stGetText")
                {
                    string text = null;
                    foreach (var item in lines)
                    {
                        text = text + (item as ListBoxItem).Content + System.Environment.NewLine;
                    }
                    return new Variable(text);
                }
                else if (option == "stCount")
                    return new Variable(dldh.Items.Count);  
                else if (option.ToLower() == "stclear")
                {
                    dldh.Items.Clear();// = new ItemCollection();
                    return new Variable(true);
                }
             
            }
            return Variable.EmptyInstance;
        }
    }
    class GET_FILEFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var gui = CSCS_GUI.GetInstance(script);

            var extension = Utils.GetSafeString(args, 0);
            var startPath = Utils.GetSafeString(args, 1);
            var dialogTitle = Utils.GetSafeString(args, 2);
            var filter = Utils.GetSafeString(args, 3);
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = startPath,
                Title = dialogTitle,

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = extension,
                Filter = filter,
                FilterIndex = 2,
                RestoreDirectory = true,

                ReadOnlyChecked = true,
                ShowReadOnly = true
            };

            if ((bool)openFileDialog1.ShowDialog())
            {
                return new Variable(openFileDialog1.FileName);
            }
            return Variable.EmptyInstance;
        }
    }

    class StatusBarFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var gui = CSCS_GUI.GetInstance(script);

            var widgetName = Utils.GetSafeString(args, 0);
            var position = Utils.GetSafeInt(args, 1);
            var newText = Utils.GetSafeString(args, 2);

            var widget = gui.GetWidget(widgetName);
            if (widget is StatusBar)
            {
                var statusbar = widget as StatusBar;

                var statusbarItems = statusbar.Items;

                int x = 0;
                for (int i = 0; i < statusbarItems.Count; i++)
                {
                    if (statusbarItems[i] is Separator)
                        continue;

                    if (position == x)
                    {
                        if (statusbar.Items[i] is StatusBarItem)
                        {
                            if ((statusbar.Items[i] as StatusBarItem).Content is TextBlock)
                            {
                                if (!string.IsNullOrEmpty(newText))
                                    ((statusbar.Items[i] as StatusBarItem).Content as TextBlock).Text = newText;
                                else
                                    return new Variable(((statusbar.Items[i] as StatusBarItem).Content as TextBlock).Text);
                            }
                        }
                        break;
                    }
                    else
                    {
                        x++;
                    }
                }
            }

            return Variable.EmptyInstance;
        }
    }

    class HorizontalBarFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var gui = CSCS_GUI.GetInstance(script);

            var widgetName = Utils.GetSafeString(args, 0);
            var option = Utils.GetSafeString(args, 1).ToLower();
            //var value = Utils.GetSafeVariable(args, 2);

            var widget = gui.GetWidget(widgetName);
            if (widget is ASHorizontalBar)
            {
                var ashb = widget as ASHorizontalBar;
                if (option == "fontsize")
                {
                    ashb.FontSize = Utils.GetSafeInt(args, 2);
                }
                else if (option == "barwidth")
                {
                    ashb.BarWidth = Utils.GetSafeInt(args, 2);
                }
                else if (option == "text")
                {
                    ashb.Text = Utils.GetSafeString(args, 2);
                }
                else if (option == "color")
                {
                    var rgbArray = Utils.GetSafeVariable(args, 2).Tuple;

                    ashb.BarColor = new SolidColorBrush(Color.FromRgb((byte)rgbArray[0].Value, (byte)rgbArray[1].Value, (byte)rgbArray[2].Value));
                }
            }

            return Variable.EmptyInstance;
        }
    }

    class DUAL_LIST_EXECFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var gui = CSCS_GUI.GetInstance(script);

            var widgetName = Utils.GetSafeString(args, 0);
            var widget = gui.GetWidget(widgetName);

            if (widget is ASDualListDialogHelper)
            {
                var dldh = widget as ASDualListDialogHelper;
                dldh.List2 = new List<string>();

                ASDualListDialog dualListDialog = new ASDualListDialog();
                dualListDialog.Title = dldh.Title;
                dualListDialog.OkButtonCaption = dldh.OkButtonCaption;
                dualListDialog.CancelButtonCaption = dldh.CancelButtonCaption;
                dualListDialog.HelpButtonCaption = dldh.HelpButtonCaption;
                dualListDialog.Label1Caption = dldh.Label1Caption;
                dualListDialog.Label2Caption = dldh.Label2Caption;

                dualListDialog.Sorted = dldh.Sorted;
                dualListDialog.ShowHelp = dldh.ShowHelp;

                dualListDialog.LeftList = new ObservableCollection<string>(dldh.List1);
                dualListDialog.RightList = new ObservableCollection<string>(dldh.List2);

                dualListDialog.ShowDialog();

                dldh.List2 = dualListDialog.RightList.ToList();
            }


            return Variable.EmptyInstance;

            //var widget = gui.GetWidget(widgetName);
            //if (widget is ASHorizontalBar)
            //{
            //    var ashb = widget as ASHorizontalBar;
            //    if(option == "fontsize")
            //    {
            //        ashb.FontSize = Utils.GetSafeInt(args, 2);
            //    }
            //    else if (option == "barwidth")
            //    {
            //        ashb.BarWidth = Utils.GetSafeInt(args, 2);
            //    }
            //    else if (option == "text")
            //    {
            //        ashb.Text = Utils.GetSafeString(args, 2);
            //    }
            //    else if (option == "color")
            //    {
            //        var rgbArray = Utils.GetSafeVariable(args, 2).Tuple;

            //        ashb.BarColor = new SolidColorBrush(Color.FromRgb((byte)rgbArray[0].Value, (byte)rgbArray[1].Value, (byte)rgbArray[2].Value));
            //    }
            //}

            return Variable.EmptyInstance;
        }
    }

    class GetSelectedGridRowFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var gui = CSCS_GUI.GetInstance(script);

            var widgetName = Utils.GetSafeString(args, 0);
            var widget = gui.GetWidget(widgetName);

            if (widget is DataGrid)
            {
                Variable resultArray = new Variable();
                resultArray.Type = Variable.VarType.ARRAY;
                resultArray.Tuple = new List<Variable>();
                foreach (var item in gui.gridsSelectedRow[widgetName.ToLower()])
                {
                    resultArray.Tuple.Add(new Variable(item));
                }

                return resultArray;
            }

            return Variable.EmptyInstance;
        }
    }
    class DLLFCFunction : ParserFunction
    {
        public T CallFunction<T>(string dllPath , string function, string parameters)  where T:struct
        {
            try
            {
               
                var DLL = Assembly.LoadFile(dllPath);
                //var assembly = Assembly.ReflectionOnlyLoadFrom(dllPath);

                foreach (var type in DLL.GetTypes())
                {
                    Debug.WriteLine(type.Name);
                    var logics = type.GetMethods();
                    foreach (var item in logics)
                    {
                        if (item.Name.ToLower() == function.ToLower())
                        {
                            var c = Activator.CreateInstance(type);
                            var method = type.GetMethod((item.Name));
                            var xy = (T)method.Invoke(c, new object[] { parameters });
                            return xy;
                        }
                    }
                }
                //var theType = DLL.GetType("LoginUtil.Util.PopuniAppInfoModelTas");

               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return (T) (object) false;
        }
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            Utils.CheckArgs(args.Count, 1, m_name);
            var dllPath = Utils.GetSafeString(args, 0);
            var function = Utils.GetSafeString(args, 1);
            var returnType = Utils.GetSafeString(args, 2);
            var parameters = Utils.GetSafeString(args, 3);

           if (File.Exists(dllPath))
            {
                if (returnType.ToLower() == "i")
                {
                    var xy = CallFunction<int>(dllPath, function, parameters);
                    return new Variable(xy);
                }
                if (returnType.ToLower() == "r")
                {
                    var xy = CallFunction<decimal>(dllPath, function, parameters);
                    return new Variable(xy);
                }
            }

            return Variable.EmptyInstance;
        }
    }

    class FFILEFunction : ParserFunction
    {
        List<string> fileEntries = null;
        int index = 0;
        string pattern = null;
        string directory = null;

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var temp = Utils.GetSafeString(args, 0);


            if (temp != pattern)
            {
                pattern = temp;
                index = 0;
                directory = null;
                if ((pattern.Contains("\\") || pattern.Contains("/")) && pattern.ToArray()[1] == ':')
                {
                    var dir = pattern.Substring(0, pattern.Length - pattern.LastIndexOf("\\") - 1);
                    pattern = pattern.Substring(pattern.LastIndexOf("\\") + 1);
                    directory = dir;

                }
                else if ((pattern.Contains("\\") || pattern.Contains("/")) && pattern.ToArray()[1] != ':')
                {
                    var dir = pattern.Substring(0, pattern.Length - pattern.LastIndexOf("\\"));
                    pattern = pattern.Substring(pattern.LastIndexOf("\\") + 1);
                    directory = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, dir);
                    if (directory.StartsWith("\\"))
                    {
                        directory.Remove(1, 1);
                    }
                }
                else
                    directory = System.AppDomain.CurrentDomain.BaseDirectory;

                if (Directory.Exists(directory))
                    fileEntries = Directory.GetFiles(directory, pattern).ToList();
                else
                    return Variable.EmptyInstance;
            }
            var secondArg = Utils.GetSafeString(args, 1).ToLower();
            if (fileEntries.Count < 1)
            {
                return Variable.EmptyInstance;
            }
            if (secondArg == "f")
            {
                return new Variable(Path.GetFileName(fileEntries[0]));
            }
            else if (secondArg == "n")
            {
                index++;
                if (fileEntries.Count <= index)
                    return Variable.EmptyInstance;
                return new Variable(Path.GetFileName(fileEntries[index]));
            }
            else
                return Variable.EmptyInstance;
        }
    }

    class PARSEFILEFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var path = Utils.GetSafeString(args, 0);
            var naredba = Utils.GetSafeString(args, 1);
            switch (naredba.ToLower())
            {
                case "pfpath":
                    path = Path.GetDirectoryName(path) + "\\";
                    return new Variable(path);
                case "pfname":
                    return new Variable(Path.GetFileNameWithoutExtension(path));
                case "pfext":
                    path = path.Remove(0, 1);
                    return new Variable(Path.GetExtension(path));
                default:
                    return Variable.EmptyInstance;
            }
        }
    }

    class PRINTER_NAMEFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            PrinterSettings settings = new PrinterSettings();
            return new Variable(settings.PrinterName);
        }
    }

    class DELFFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var path = Utils.GetSafeString(args, 0);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return Variable.EmptyInstance;
        }
    }

    class ENCRYPTSTRFunction : ParserFunction
    {
        public string TwoFishEncryption(string plain, string key)
        {

            BCEngine bcEngine = new BCEngine(new TwofishEngine(), Encoding.ASCII);
            bcEngine.SetPadding(new Pkcs7Padding());
            return bcEngine.Encrypt(plain, key);
        }
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);
            var typeEnc = Utils.GetSafeString(args, 0);
            var stringToEncrypt = Utils.GetSafeString(args,1);
            var phrase = Utils.GetSafeString(args, 2);
           if (typeEnc.ToLower() == "setwofish")
            {
               var enc = TwoFishEncryption(stringToEncrypt, phrase);
                return new Variable(enc);
            }
            return Variable.EmptyInstance;
        }
    }
    class DecryptstrFunction : ParserFunction
    {
        public string BlowFishDecryption(string cipher, string key)
        {
            BCEngine bcEngine = new BCEngine(new TwofishEngine(), Encoding.ASCII);
            bcEngine.SetPadding(new Pkcs7Padding());
            return bcEngine.Decrypt(cipher, key);
        }
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);
            var typeEnc = Utils.GetSafeString(args, 0);
            var stringToDecrypt = Utils.GetSafeString(args, 1);
            var phrase = Utils.GetSafeString(args, 2);
            if (typeEnc.ToLower() == "setwofish")
            {
                var enc = BlowFishDecryption(stringToDecrypt, phrase);
                return new Variable(enc);
            }
            return Variable.EmptyInstance;
        }
    }
    class BELLFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            System.Media.SystemSounds.Beep.Play();
            return Variable.EmptyInstance;
        }
    }

    class CDOWFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var dateVariable = Utils.GetSafeVariable(args, 0);

            return new Variable(dateVariable.DateTime.DayOfWeek.ToString());
        }
    }

    class CHRFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var numberVariable = Utils.GetSafeDouble(args, 0);

            return new Variable(((char)numberVariable).ToString());
        }
    }

    class CMNTHFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var dateVariable = Utils.GetSafeVariable(args, 0);

            return new Variable(dateVariable.DateTime.ToString("MMMM"));
        }
    }
    
    class DIR_EXISTSFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var dir = Utils.GetSafeString(args, 0);
            if (Directory.Exists(dir))
                return new Variable(true);
            else
                return new Variable(false);
        }
    }
    
    class DECFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var num = Utils.GetSafeDouble(args, 0);
            return new Variable(--num);
        }
    }

    class HEXFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var num = Utils.GetSafeInt(args, 0);
            //byte[] bytes = BitConverter.he(num);
            //return new Variable(BitConverter.ToString(bytes));
            //decimal aaa= (decimal)num;
            //decimal aaa= Convert.ToInt64(num, 16)
            return new Variable(num.ToString("X"));
        }
    }

 
    
    class ISALFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var rijec = Utils.GetSafeString(args, 0);
            return new Variable(Char.IsLetter(rijec, 0));
        }
    }

    class ISLOFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var rijec = Utils.GetSafeString(args, 0);
            return new Variable(Char.IsLower(rijec, 0));
        }
    }
    
    class ISNUMFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var rijec = Utils.GetSafeString(args, 0);
            return new Variable(Char.IsNumber(rijec, 0));
        }
    }
    
    class LCHRFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var rijec = Utils.GetSafeString(args, 0);
            var param = Utils.GetSafeString(args, 1);
            if (string.IsNullOrEmpty(param) || param.ToUpper() == "C")
            {
                var temp = rijec.TrimEnd();
                var x = temp[temp.Length - 1];
                return new Variable(x.ToString());
            }
            return new Variable(rijec.TrimEnd().Count());
        }
    }
    
    class LIKEFunction : ParserFunction
    {
        bool IsMatchRegex(string str, string pat, char singleWildcard, char multipleWildcard)
        {

            string escapedSingle = Regex.Escape(new string(singleWildcard, 1));
            string escapedMultiple = Regex.Escape(new string(multipleWildcard, 1));

            pat = Regex.Escape(pat);
            pat = pat.Replace(escapedSingle, ".");
            pat = "^" + pat.Replace(escapedMultiple, ".*") + "$";

            Regex reg = new Regex(pat);

            return reg.IsMatch(str);

        }
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var rijec1 = Utils.GetSafeString(args, 0);
            var rijec2 = Utils.GetSafeString(args, 1);

            return new Variable(IsMatchRegex(rijec2, rijec1, '?', '*'));
        }
    }
    
    class ISUPFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var rijec = Utils.GetSafeString(args, 0);
            return new Variable(Char.IsUpper(rijec, 0));
        }
    }
    class MAKE_DIRFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var dir = Utils.GetSafeString(args, 0);
            Directory.CreateDirectory(dir);
            return new Variable(true);
        }
    }
    class FARRAYFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var arrayVar = Utils.GetSafeVariable(args, 0);
            return new Variable(arrayVar.Tuple.Count);
        }
    }
    class DIFFFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var text1 = Utils.GetSafeString(args, 0);
            var text2 = Utils.GetSafeString(args, 1);
            return new Variable(Difference(text1,text2));
        }
        public string Soundex(string data)
        {
            StringBuilder result = new StringBuilder();

            if (data != null && data.Length > 0)
            {
                string previousCode, currentCode;
                result.Append(Char.ToUpper(data[0]));
                previousCode = string.Empty;

                for (int i = 1; i < data.Length; i++)
                {
                    currentCode = EncodeChar(data[i]);

                    if (currentCode != previousCode)
                        result.Append(currentCode);

                    if (result.Length == 4) break;

                    if (!currentCode.Equals(string.Empty))
                        previousCode = currentCode;
                }
            }

            if (result.Length < 4)
                result.Append(new String('0', 4 - result.Length));

            return result.ToString();
        }
        private string EncodeChar(char c)
        {

            switch (Char.ToLower(c))
            {
                case 'b':
                case 'f':
                case 'p':
                case 'v':
                    return "1";
                case 'c':
                case 'g':
                case 'j':
                case 'k':
                case 'q':
                case 's':
                case 'x':
                case 'z':
                    return "2";
                case 'd':
                case 't':
                    return "3";
                case 'l':
                    return "4";
                case 'm':
                case 'n':
                    return "5";
                case 'r':
                    return "6";
                default:
                    return string.Empty;
            }
        }

        public int Difference(string data1, string data2)
        {
            int result = 0;

            if (data1.Equals(string.Empty) || data2.Equals(string.Empty))
                return 0;

            string soundex1 = Soundex(data1);
            string soundex2 = Soundex(data2);

            if (soundex1.Equals(soundex2))
                result = 4;
            else
            {
                if (soundex1[0] == soundex2[0])
                    result = 1;

                string sub1 = soundex1.Substring(1, 3); //characters 2, 3, 4

                if (soundex2.IndexOf(sub1) > -1)
                {
                    result += 3;
                    return result;
                }

                string sub2 = soundex1.Substring(2, 2); //characters 3, 4

                if (soundex2.IndexOf(sub2) > -1)
                {
                    result += 2;
                    return result;
                }

                string sub3 = soundex1.Substring(1, 2); //characters 2, 3

                if (soundex2.IndexOf(sub3) > -1)
                {
                    result += 2;
                    return result;
                }

                char sub4 = soundex1[1];
                if (soundex2.IndexOf(sub4) > -1)
                    result++;

                char sub5 = soundex1[2];
                if (soundex2.IndexOf(sub5) > -1)
                    result++;

                char sub6 = soundex1[3];
                if (soundex2.IndexOf(sub6) > -1)
                    result++;

            }

            return result;
        }
    }
    class TRIMFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var text = Utils.GetSafeString(args, 0);
            var start_end = Utils.GetSafeString(args, 1);
            if (start_end == null || start_end.ToLower() == "t")
            {
                return new Variable(text.TrimEnd());
            }
            return new Variable(text.TrimStart());
        }
    }
    class GFLDFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var fieldNum = Utils.GetSafeInt(args, 0);
            var handle = Utils.GetSafeString(args, 1);
            var delimiter = Utils.GetSafeString(args, 2);
            var text = Utils.GetSafeString(args, 3);
            return new Variable(text.Split(delimiter.ToCharArray().First())[fieldNum-1]);
        }
    }
    class SNDXFunction : ParserFunction
    {
        public string For(string word)
        {
            const int MaxSoundexCodeLength = 4;

            var soundexCode = new StringBuilder();
            var previousWasHOrW = false;

            // Upper case all letters in word and remove any punctuation
            word = Regex.Replace(
                word == null ? string.Empty : word.ToUpper(),
                    @"[^\w\s]",
                        string.Empty);

            if (string.IsNullOrEmpty(word))
                return string.Empty.PadRight(MaxSoundexCodeLength, '0');

            // Retain the first letter
            soundexCode.Append(word.First());

            for (var i = 1; i < word.Length; i++)
            {
                var numberCharForCurrentLetter = GetCharNumberForLetter(word[i]);

                // Skip this number if it matches the number for the first character
                if (i == 1 &&
                        numberCharForCurrentLetter == GetCharNumberForLetter(soundexCode[0]))
                    continue;

                // Skip this number if the previous letter was a 'H' or a 'W' 
                // and this number matches the number assigned before that
                if (soundexCode.Length > 2 && previousWasHOrW &&
                        numberCharForCurrentLetter == soundexCode[soundexCode.Length - 2])
                    continue;

                // Skip this number if it was the last added
                if (soundexCode.Length > 0 &&
                        numberCharForCurrentLetter == soundexCode[soundexCode.Length - 1])
                    continue;

                soundexCode.Append(numberCharForCurrentLetter);

                previousWasHOrW = "HW".Contains(word[i]);
            }

            return soundexCode
                    .Replace("0", string.Empty)
                        .ToString()
                            .PadRight(MaxSoundexCodeLength, '0')
                                .Substring(0, MaxSoundexCodeLength);
        }

        /// <summary>
        /// Retrieves the soundex number for a given letter.
        /// </summary>
        /// <param name="letter">Letter to get the soundex number for.</param>
        /// <returns>Soundex number (as a character) for letter.</returns>
        private char GetCharNumberForLetter(char letter)
        {
            if ("BFPV".Contains(letter)) return '1';
            if ("CGJKQSXZ".Contains(letter)) return '2';
            if ("DT".Contains(letter)) return '3';
            if ('L' == letter) return '4';
            if ("MN".Contains(letter)) return '5';
            if ('R' == letter) return '6';

            return '0'; // i.e. letter is [AEIOUWYH]
        }
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var text = Utils.GetSafeString(args, 0);
            return new Variable(For(text));
        }
    }
    class JUSTFunction : ParserFunction
    {
        private int End(string text)
        {
            int num = 0;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] == ' ')
                {
                    num++;
                }
                else
                    break;
            }
            foreach (var item in text.ToCharArray())
            {
                if (item == ' ')
                {
                    num++;
                }
                else
                    break;
            }
            return num;
        }
        private int Start(string text)
        {
            int num = 0;
            foreach (var item in text.ToCharArray())
            {
                if (item == ' ')
                {
                    num++;
                }
                else
                    break;
            }
            return num;
        }
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var text = Utils.GetSafeString(args, 0);
            var just = Utils.GetSafeString(args, 1);
            string realText = text.Trim();
            int numberOfStartSpaces = Start(text);
            int numberOfEndSpaces = End(text);
            int numberOfSpaces = numberOfEndSpaces + numberOfStartSpaces;
            //int textLengst = text.Length - numberOfStartSpaces - numberOfEndSpaces;
            if (just.ToLower() == "r")
            {
                string EmptSp = null;
                for (int i = 0; i < numberOfSpaces; i++)
                {
                    EmptSp = EmptSp + " ";
                }
                return new Variable(EmptSp + realText);
            }
            else if (just.ToLower() == "l")
            {
                string EmptSp = null;
                for (int i = 0; i < numberOfSpaces; i++)
                {
                    EmptSp = EmptSp + " ";
                }
                return new Variable(text.TrimEnd() + EmptSp);
            }
            else if (just.ToLower() == "c")
            {
                int numEmptySpaces = numberOfStartSpaces + numberOfEndSpaces;
                if (numEmptySpaces % 2 == 0)
                {
                    for (int i = 0; i < numEmptySpaces / 2; i++)
                    {
                        realText = " " + realText + " ";
                    }
                }
                else
                {
                    for (int i = 0; i < (numEmptySpaces - 1) / 2; i++)
                    {
                        realText = " " + realText + " ";
                    }
                    realText = realText + " ";
                }
                return new Variable(realText);
            }
            return  Variable.EmptyInstance;
        }
    }
    class OSFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().OfType<ManagementObject>()
                        select x.GetPropertyValue("Caption")).FirstOrDefault();
            return new Variable(name != null ? name.ToString() : "Unknown");
        }
    }
    class TRAPFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var key = Utils.GetSafeString(args, 0);
            var toRun = args.FirstOrDefault(p => p.CurrentAssign.ToLower() == "gosub");
            var gui = script.Context as CSCS_GUI;
            Window win;
            Key keyPressed =new ReturnKeyUtil().ReturnKey(key);
          

            if (CSCS_GUI.File2Window.TryGetValue(script.Filename, out win))
            {
                var ib = new InputBinding(new KeyCommand((object arg1) => { return true; }, (object obj) => { gui.Interpreter.Run(toRun.String, null, null, null, script); }), new KeyGesture(keyPressed));
                win.InputBindings.Add(ib);
            }

            return new Variable(true);
        }
    }
    class WebBrowseFunction : ParserFunction
    {
        protected override  Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var browser = Utils.GetSafeString(args, 0);
            var url = Utils.GetSafeString(args, 0);
            var gui = script.Context as CSCS_GUI;
            Window win;

            WebView2 WebView1 = (WebView2)gui.GetWidget(browser);
            WebView1.EnsureCoreWebView2Async().GetAwaiter();
            if (WebView1 != null && WebView1.CoreWebView2 != null)
            {
                WebView1.CoreWebView2.Navigate(url);
            }

            //if (CSCS_GUI.File2Window.TryGetValue(script.Filename, out win))
            //{
            //    var ib = new InputBinding(new KeyCommand((object arg1) => { return true; }, (object obj) => { gui.Interpreter.Run(toRun.String, null, null, null, script); }), new KeyGesture(keyPressed));
            //    win.InputBindings.Add(ib);
            //}

            return new Variable(true);
        }
    }
    class ASCFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var chr = Utils.GetSafeString(args, 0);
            if (chr.Length != 1)
            {
                return Variable.EmptyInstance;
            }
            else
            {
                return new Variable((int)chr.First());
            }
        }
    }

    class ELOCFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var fieldToCheckFor = Utils.GetSafeString(args, 0);
            var fieldToCheckWithin = Utils.GetSafeString(args, 1);
            var startPos = Utils.GetSafeInt(args, 2);
            var numChars = Utils.GetSafeInt(args, 3);
            var ignoreCase = Utils.GetSafeString(args, 4);
            if (ignoreCase.ToLower() == "t")
            {
                ignoreCase = "true";
            }
            if (ignoreCase.ToLower() == "f")
            {
                ignoreCase = "false";
            }
            if (string.IsNullOrEmpty(fieldToCheckFor) || string.IsNullOrEmpty(fieldToCheckWithin))
                return new Variable(0);

            if (startPos < 0 || startPos > fieldToCheckWithin.Length)
                startPos = fieldToCheckWithin.Length;
            if (startPos > 0)
                startPos++;
            if (numChars < 0 || numChars > fieldToCheckWithin.Length - startPos)
                numChars = fieldToCheckWithin.Length - startPos;

            string substringToFind = fieldToCheckFor;
            string substringToSearchWithin = "";
            if (numChars == 0)
            {
                substringToSearchWithin = fieldToCheckWithin.Substring(0, fieldToCheckWithin.Length - startPos);
            }
            else
            {
                substringToSearchWithin = fieldToCheckWithin.Substring(fieldToCheckWithin.Length - startPos - numChars, numChars);
            }
            bool ign = false;
            Boolean.TryParse(ignoreCase, out ign);
            if (ign)
            {
                substringToFind = substringToFind.ToLower();
                substringToSearchWithin = substringToSearchWithin.ToLower();
            }

            int index = substringToSearchWithin.LastIndexOf(substringToFind);

            if (index >= 0)
                return new Variable(index + 1);

            return new Variable(0);
        }
    }
    
    class LOCFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);
            var fieldToCheckFor = Utils.GetSafeString(args, 0);
            var fieldToCheckWithin = Utils.GetSafeString(args, 1);
            var startPos = Utils.GetSafeInt(args, 2);
            var numChars = Utils.GetSafeInt(args, 3);
            var ignoreCase = Utils.GetSafeString(args, 4);
            if (ignoreCase.ToLower() == "t")
            {
                ignoreCase = "true";
            }
            if (ignoreCase.ToLower() == "f")
            {
                ignoreCase = "false";
            }
            if (string.IsNullOrEmpty(fieldToCheckFor) || string.IsNullOrEmpty(fieldToCheckWithin))
                return new Variable(0);

            if (startPos < 0 || startPos > fieldToCheckWithin.Length)
                startPos = 0;
            if (startPos > 0)
                startPos--;
            if (numChars < 0 || numChars > fieldToCheckWithin.Length - startPos)
                numChars = fieldToCheckWithin.Length - startPos;

            string substringToFind = fieldToCheckFor;
            string substringToSearchWithin = "";
            if (numChars == 0)
            {
                substringToSearchWithin = fieldToCheckWithin.Substring(startPos, fieldToCheckWithin.Length - startPos);
            }
            else
            {
                substringToSearchWithin = fieldToCheckWithin.Substring(startPos, numChars);
            }
            bool ign = false;
            Boolean.TryParse(ignoreCase, out ign);
            if (ign)
            {
                substringToFind = substringToFind.ToLower();
                substringToSearchWithin = substringToSearchWithin.ToLower();
            }

            int index = substringToSearchWithin.IndexOf(substringToFind);

            if (index >= 0)
                return new Variable(index + 1);

            return new Variable(0);
        }
    }
    
    class DSPCEFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                var tmp = System.AppDomain.CurrentDomain.BaseDirectory.Substring(0, 3);
                if (drive.IsReady && drive.Name.ToLower() == tmp.ToLower())
                {
                    return new Variable(drive.TotalFreeSpace); ;
                }
            }
            return Variable.EmptyInstance;
        }
    }
    
    class CPATHFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name);
            return new Variable(System.AppDomain.CurrentDomain.BaseDirectory);
        }
    }

    class XPATHFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name);
            return new Variable(System.AppDomain.CurrentDomain.BaseDirectory);
        }
    }

    class PLAYWAVFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);
            var wav = Utils.GetSafeString(args, 0);
            if (File.Exists(wav))
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(wav);
                player.Play();
            }

            return Variable.EmptyInstance;
        }
    }
    
    class FILE_STOREFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var gui = script.Context as CSCS_GUI;

            var destinationVarName = Utils.GetSafeString(args, 0);
            var sourceVarName = Utils.GetSafeString(args, 1);
            var operation = Utils.GetSafeString(args, 2);
            if (string.IsNullOrEmpty(operation))
            {
                operation = "fsimport";
            }
            switch (operation.ToLower())
            {
                case "fsimport":
                    if (!File.Exists(sourceVarName))
                    {
                        return new Variable(false);
                    }
                    destinationVarName = File.ReadAllText(sourceVarName);
                    if (gui.DEFINES.TryGetValue(sourceVarName.ToLower(), out DefineVariable defVarSource))
                    {
                        if (gui.DEFINES.TryGetValue(destinationVarName.ToLower(), out DefineVariable defVarDest))
                        {
                            defVarDest.InitVariable(new Variable(defVarSource.String), gui);
                        }
                    }
                    break;
                case "fsemport":
                    // code block
                    break;
                case "fscopy":
                    // code block
                    break;
                default:
                    // code block
                    break;
            }

            return new Variable(System.AppDomain.CurrentDomain.BaseDirectory);
        }
    }
    
    class REGEDITFunction : ParserFunction, INumericFunction
    {
        private bool canStart = false;
        private string path = "";
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name, false);
            var va0 = Utils.GetSafeString(args, 0);
            var section_path = Utils.GetSafeString(args, 1);
            var field = Utils.GetSafeString(args, 2);
            var value = Utils.GetSafeString(args, 3);


            switch (va0.ToLower())
            {
                case "regopen":
                    path = section_path;
                    if (field.ToLower() == "rtFile".ToLower() && !File.Exists(section_path))
                    {
                        return new Variable(false);
                    }
                    using (var fs = File.Open(section_path, FileMode.Open))
                    {
                        canStart = fs.CanWrite;
                        return new Variable(canStart);
                    }
                case "regreadint":
                    if (!canStart)
                        return new Variable(false);
                    var tmp = new IniFileUtil(path).Read(field, section_path);
                    return new Variable(int.Parse(tmp));
                case "regreadstr":
                    if (!canStart)
                        return new Variable(false);
                    var tmp1 = new IniFileUtil(path).Read(field, section_path);
                    return new Variable(tmp1);
                case "regreadbool":
                    if (!canStart)
                        return new Variable(false);
                    var tmp2 = new IniFileUtil(path).Read(field, section_path);
                    return new Variable(bool.Parse(tmp2));
                case "regwriteint":
                    if (!canStart)
                        return new Variable(false);
                    new IniFileUtil(path).Write(field, value.ToString(), section_path);
                    return new Variable(true);
                case "regwritestr":
                    if (!canStart)
                        return new Variable(false);
                    new IniFileUtil(path).Write(field, value.ToString(), section_path);
                    return new Variable(true);
                case "regwritebool":
                    if (!canStart)
                        return new Variable(false);
                    new IniFileUtil(path).Write(field, value.ToString(), section_path);
                    return new Variable(true);
                case "regdelete":
                    new IniFileUtil(path).DeleteSection(section_path);
                    return new Variable(true);
                case "regclose":
                    return new Variable(true);
                default:
                    return new Variable(false);
            }
        }
        public override string Description()
        {
            return "Read or write entries to the .INI file.";
        }
    }

    class EMAILFunction : ParserFunction
    {
        private bool SetupMail(string podaci, string outgoingServer, string password, string username, string senderMail, string senderUsername)
        {
            bool ok = true;
            var to = GetTo(podaci);
            var subject = GetSubject(podaci);
            var body = GetiBody(podaci);
            var cc = GetCC(podaci);
            var bcc = GetBCC(podaci);
            var atch = GetATCH(podaci);
            
            foreach (var item in to)
            {
                if(ok)
                ok =  Send(item, senderMail, subject + DateTime.Now.ToString("ddMMYY HHmmss"), body, outgoingServer, username, password, cc, bcc, atch);
            }
            return ok;
        }
        public bool Send(string addressTo, string addressFrom, string subject, string body, string outgoingServer , string username, string password, List<string> cc, List<string> bcc, List<string> atchs)
        {
            MailAddress to = new MailAddress(addressTo);
            MailAddress from = new MailAddress(addressFrom);

            MailMessage email = new MailMessage(from, to);
            if (cc != null)
            {
                foreach (var item in cc)
                {
                    if(!string.IsNullOrEmpty(item))
                        email.CC.Add(item);
                }
            }
            if (atchs != null)
            {
                foreach (var item in atchs)
                {
                    if(!string.IsNullOrEmpty(item) && File.Exists(item))
                        email.Attachments.Add(new Attachment(item));
                }
            }

            if (bcc != null)
            {
                foreach (var item in bcc)
                {
                    if (!string.IsNullOrEmpty(item))
                        email.Bcc.Add(item);
                }
            }
            email.Subject = subject;
            email.Body = body;

            SmtpClient smtp = new SmtpClient();
            smtp.Host = outgoingServer;
            smtp.Port = 25;
            smtp.Credentials = new NetworkCredential(username, password);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.EnableSsl = true;

            try
            {
                smtp.Send(email);
                return true;
            }
            catch (SmtpException ex)
            {
                return false;
            }
        }
        private List<string> GetTo(string podaci)
        {
            bool start = false;
            string address = null;
            using (StringReader reader = new StringReader(podaci))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.ToLower().StartsWith(@"\") )
                    {
                        start = false;
                    }
                    if (start)
                    {
                        address += line;
                    }
                    if (line.ToLower().StartsWith(@"\to"))
                    {
                        start = true;
                    }
                  
                }
            }
           return address.Split(',').ToList();

        }
        private string GetSubject(string podaci)
        {
            bool start = false;
            string subject = null;
            using (StringReader reader = new StringReader(podaci))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.ToLower().StartsWith(@"\"))
                    {
                        start = false;
                    }
                    if (start)
                    {
                        subject += line;
                    }
                    if (line.ToLower().StartsWith(@"\subject"))
                    {
                        start = true;
                    }

                }
            }
            return subject;

        }
        private string GetiBody(string podaci)
        {
            bool start = false;
            string body = null;
            using (StringReader reader = new StringReader(podaci))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.TrimStart().ToLower().StartsWith(@"\"))
                    {
                        start = false;
                    }
                    if (start)
                    {
                        body += line + System.Environment.NewLine;
                    }
                    if (line.TrimStart().ToLower().StartsWith(@"\body"))
                    {
                        start = true;
                    }

                }
            }
            return body;

        }
        private List<string> GetCC(string podaci)
        {
            bool start = false;
            string address = null;
            using (StringReader reader = new StringReader(podaci))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.ToLower().StartsWith(@"\"))
                    {
                        start = false;
                    }
                    if (start)
                    {
                        address += line;
                    }
                    if (line.ToLower().StartsWith(@"\cc"))
                    {
                        start = true;
                    }

                }
            }
            return address.Split(',').ToList();

        }
        private List<string> GetBCC(string podaci)
        {
            bool start = false;
            string address = null;
            using (StringReader reader = new StringReader(podaci))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.ToLower().StartsWith(@"\"))
                    {
                        start = false;
                    }
                    if (start)
                    {
                        address += line;
                    }
                    if (line.ToLower().StartsWith(@"\bcc"))
                    {
                        start = true;
                    }

                }
            }
            return address.Split(',').ToList();

        }
        private List<string> GetATCH(string podaci)
        {
            bool start = false;
            string address = null;
            using (StringReader reader = new StringReader(podaci))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.ToLower().StartsWith(@"\"))
                    {
                        start = false;
                    }
                    if (start)
                    {
                        address += line;
                    }
                    if (line.ToLower().StartsWith(@"\attachments"))
                    {
                        start = true;
                    }

                }
            }
            if (address == null)
                return null;
            return address.Split(',').ToList();

        }
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var gui = CSCS_GUI.GetInstance(script);

            var option = Utils.GetSafeString(args, 0);
            var podaci = Utils.GetSafeString(args, 1);
            var widgetName = Utils.GetSafeString(args, 2);
            var saveAttachsFolder = Utils.GetSafeString(args,3);
            var outgoingServer = Utils.GetSafeString(args,4);
            var password = Utils.GetSafeString(args,5);
            var username = Utils.GetSafeString(args,6);
            var senderMail = Utils.GetSafeString(args,7);
            var senderUsername = Utils.GetSafeString(args,8);
            switch (option.ToLower())
            {
                case "emlsendmsg":
                    var widget1 = gui.GetWidget(podaci);
                    if (widget1 is ListBox)
                    {
                        string text = null;
                        var asmemobox = widget1 as ListBox;
                        foreach (var item in asmemobox.Items)
                        {
                            text = text +((ListBoxItem) item).Content + System.Environment.NewLine;
                        }
                        return new Variable(SetupMail(text, outgoingServer, password, username, senderMail, senderUsername));
                    }
                    return new Variable(false);
                case "test":
                    var widget = gui.GetWidget(widgetName);
                    if (widget is ASMemoBox)
                    {
                        var asmemobox = widget as ASMemoBox;
                        asmemobox.Text =
        @"
                    \TO
                    nebojsa@aurasoft.hr, matija@aurasoft.hr
,sanja@aurasoft.hr
\CC
                    lidija@aurasoft.hr
,lorena@aurasoft.hr
  \BCC
                    enzo@aurasoft.hr, boris@aurasoft.hr
                    \SUBJECT
                    Nebki naslov
                    \BODY
ovo je testni mail!!
                    neki text u body-u
                    fsfvs fsdv skjsvsdv
                    sdvsvs
                    dvsdv ssdvs sd svf wsdv
\Attachments
d:\temp\aaa.txt, d:\temp\ggg.txt, 
                    ";
                        var text = asmemobox.Text;
                    }
                    break;
                default:
                    // code block
                    break;
            }
           
            return Variable.EmptyInstance;
        }
    }

	class TPATHFunction : ParserFunction
	{
		protected override Variable Evaluate(ParsingScript script)
		{
			return new Variable(App.GetConfiguration("ScriptsPath", ""));
		}
	}
    
    class FillOutGridFunction : ParserFunction
    {
        bool m_fromDB;
        public FillOutGridFunction(bool fromDB = false)
        {
            m_fromDB = fromDB;
        }
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var gui = CSCS_GUI.GetInstance(script);
            var gridVar = VariableArgsFunction.GetDatagridData(gui, widgetName, out CSCS_GUI.WidgetData wd);
            DataGrid dg = gridVar != null ? gridVar.Object as DataGrid : gui.GetWidget(widgetName) as DataGrid;
            if (dg == null)
            {
                return Variable.EmptyInstance;
            }

            int maxElems = -1;
            if (wd != null && !string.IsNullOrWhiteSpace(wd.maxElemsName))
            {
                var maxVar = gui.Interpreter.GetVariableValue(wd.maxElemsName);
                maxElems = maxVar == null ? -1 : maxVar.AsInt();
            }

            ObservableCollection<Row> list = new ObservableCollection<Row>();
            if (m_fromDB)
            {
                var tableName = Utils.GetSafeString(args, 1);
                FillOutFromDB(dg, tableName, maxElems, list);
            }
            else
            {
                var firstCol = Utils.GetSafeVariable(args, 1);
                var rows = firstCol.Tuple.Count;
                for (int i = 0; i < rows && (maxElems < 0 || i < maxElems); i++)
                {
                    var row = new Row(list.Count);
                    for (int j = 1; j < args.Count; j++)
                    {
                        var current = args[j].Tuple[i];
                        if (current.Type == Variable.VarType.STRING)
                        {
                            row.AddCol(current.String);
                        }
                        else if (current.Type == Variable.VarType.NUMBER)
                        {
                            row.AddCol(current.AsBool());
                        }

                    }
                    list.Add(row);
                }
            }
            dg.ItemsSource = list;

            if (wd != null)
            {
                wd.actualElems = dg.Items.Count;
                wd.actualElemsVar = new Variable(wd.actualElems);
                if (!string.IsNullOrWhiteSpace(wd.actualElemsName))
                {
                    gui.Interpreter.AddGlobal(wd.actualElemsName, new GetVarFunction(wd.actualElemsVar), false);
                }
            }

            return new Variable(dg.Items.Count);
        }
        protected ObservableCollection<Row> FillOutFromDB(DataGrid dg, string tableName, int maxElems,
            ObservableCollection<Row> list)
        {
            var query = "select * from " + tableName;
            var sqlResult = SQLQueryFunction.GetData(query, tableName);

            for (int i = 1; i < sqlResult.Tuple.Count && (maxElems < 0 || i <= maxElems); i++)
            {
                var data = sqlResult.Tuple[i];
                var row = new Row(list.Count);
                for (int j = 0; j < dg.Columns.Count; j++)
                {
                    //var column = dg.Columns[j] as DataGridTemplateColumn;
                    var elem = data.Tuple[j];
                    if (elem.Original == Variable.OriginalType.BOOL)
                    {
                        row.AddCol(elem.AsBool());
                    }
                    else
                    {
                        row.AddCol(elem.AsString());
                    }
                }
                list.Add(row);
            }
            return list;
        }

        public class Row
        {
            int strIndex = 0;
            int boolIndex = 0;
            public int RowNumber { get; set; }

            public Row(int rowNumber)
            {
                RowNumber = rowNumber;
            }

            public void AddCol(string str)
            {
                switch (strIndex)
                {
                    case 0: S1 = str; break;
                    case 1: S2 = str; break;
                    case 2: S3 = str; break;
                    case 3: S4 = str; break;
                    case 4: S5 = str; break;
                    case 5: S6 = str; break;
                    case 6: S7 = str; break;
                    case 7: S8 = str; break;
                    case 8: S9 = str; break;
                    case 9: S10 = str; break;
                }
                strIndex++;
            }
            public void AddCol(bool b)
            {
                switch (boolIndex)
                {
                    case 0: B1 = b; break;
                    case 1: B2 = b; break;
                    case 2: B3 = b; break;
                    case 3: B4 = b; break;
                    case 4: B5 = b; break;
                }
                boolIndex++;
            }
            public string S1 { get; set; }
            public string S2 { get; set; }
            public string S3 { get; set; }
            public string S4 { get; set; }
            public string S5 { get; set; }
            public string S6 { get; set; }
            public string S7 { get; set; }
            public string S8 { get; set; }
            public string S9 { get; set; }
            public string S10 { get; set; }
            public bool B1 { get; set; }
            public bool B2 { get; set; }
            public bool B3 { get; set; }
            public bool B4 { get; set; }
            public bool B5 { get; set; }

        }
    }

    class BindSQLFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var widgetName = Utils.GetSafeString(args, 0);
            var gui = CSCS_GUI.GetInstance(script);
            var widget = gui.GetWidget(widgetName);
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

    class NewBindSQLFunction : ParserFunction
    {
        public static Dictionary<string, List<string>> gridsHeaders = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<string>> gridsTags = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<Variable.VarType>> gridsTypes = new Dictionary<string, List<Variable.VarType>>();

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var gui = CSCS_GUI.GetInstance(script);
            var widgetName = Utils.GetSafeString(args, 0);
            var widget = gui.GetWidget(widgetName);
            if (widget == null)
            {
                return Variable.EmptyInstance;
            }
            var queryString = Utils.GetSafeString(args, 1);

            if (widget is DataGrid)
            {
                var dg = widget as DataGrid;

                //--------------------

                if (!gridsHeaders.ContainsKey(widgetName.ToLower()))
                    gridsHeaders[widgetName.ToLower()] = new List<string>();

                if (!gridsTags.ContainsKey(widgetName.ToLower()))
                    gridsTags[widgetName.ToLower()] = new List<string>();

                if (!gridsTypes.ContainsKey(widgetName.ToLower()))
                    gridsTypes[widgetName.ToLower()] = new List<Variable.VarType>();

                var dgColumns = dg.Columns;
                for (int i = 0; i < dgColumns.Count; i++)
                {
                    var column = dg.Columns.ElementAt(i);

                    if (column is DataGridTemplateColumn)
                    {
                        var dgtc = column as DataGridTemplateColumn;

                        var cell = dgtc.CellTemplate.LoadContent();

                        gridsHeaders[widgetName.ToLower()].Add(dgtc.Header.ToString());

                        if (cell is ASTimeEditer)
                        {
                            var te = cell as ASTimeEditer;
                            if (te.Tag != null)
                            {
                                gridsTags[widgetName.ToLower()].Add(te.Tag.ToString());
                                //tagsAndTypes.Add(te.Tag.ToString(), typeof(TimeSpan));
                                //tagsAndHeaders.Add(te.Tag.ToString(), dgtc.Header.ToString());
                                //timeAndDateEditerTagsAndSizes[te.Tag.ToString()] = te.DisplaySize;
                            }
                        }
                        else if (cell is ASDateEditer)
                        {
                            var de = cell as ASDateEditer;
                            if (de.Tag != null)
                            {
                                gridsTags[widgetName.ToLower()].Add(de.Tag.ToString());
                                //tagsAndTypes.Add(de.Tag.ToString(), typeof(DateTime));
                                //tagsAndHeaders.Add(de.Tag.ToString(), dgtc.Header.ToString());
                                //timeAndDateEditerTagsAndSizes[de.Tag.ToString()] = de.DisplaySize;
                            }
                        }
                        else if (cell is CheckBox)
                        {
                            var cb = cell as CheckBox;
                            if (cb.Tag != null)
                            {
                                gridsTags[widgetName.ToLower()].Add(cb.Tag.ToString());
                                //tagsAndTypes.Add(cb.Tag.ToString(), typeof(bool));
                                //tagsAndHeaders.Add(cb.Tag.ToString(), dgtc.Header.ToString());
                            }
                        }
                        else if (cell is TextBox)
                        {
                            var tb = cell as TextBox;
                            if (tb.Tag != null)
                            {
                                gridsTags[widgetName.ToLower()].Add(tb.Tag.ToString());
                                //tagsAndTypes.Add(tb.Tag.ToString(), typeof(string));
                                //tagsAndHeaders.Add(tb.Tag.ToString(), dgtc.Header.ToString());
                            }
                        }
                    }
                }

                //------------------

                dg.Items.Clear();
                dg.Columns.Clear();

                //var query = "select * from " + tableName;
                var sqlResult = SQLQueryFunction.GetData(queryString/*, tableName*/);

                Variable columns = sqlResult.Tuple[0];
                columns.Tuple.RemoveAll(p => p.String.ToLower() == "id");
                for (int i = 0; i < columns.Tuple.Count; i += 1)
                {
                    string label = columns.Tuple[i].AsString();
                    if (label.ToLower() == "id")
                    {
                        continue;
                    }
                    DataGridTextColumn column = new DataGridTextColumn();
                    //column.Header = label;
                    column.Header = gridsHeaders[widgetName.ToLower()][i];
                    column.Binding = new Binding(label.Replace(' ', '_'));

                    dg.Columns.Add(column);
                }

                for (int i = 1; i < sqlResult.Tuple.Count; i++)
                {
                    var data = sqlResult.Tuple[i];
                    //data.Tuple.RemoveAt(0);
                    dynamic row = new ExpandoObject();
                    for (int j = 0; j < dg.Columns.Count; j++)
                    {
                        gridsTypes[widgetName.ToLower()].Add(data.Tuple[j].Type);
                        //var column = dg.Columns[j].Header.ToString();
                        var column = gridsTags[widgetName.ToLower()][j];
                        string val = "";
                        if (data.Tuple[j].Type == Variable.VarType.DATETIME)
                        {
                            val = data.Tuple.Count > j ? data.Tuple[j].DateTime.ToString("dd/MM/yyyy") : "";
                        }
                        else
                        {
                            val = data.Tuple.Count > j ? data.Tuple[j].AsString() : "";
                        }
                        ((IDictionary<String, Object>)row)[column.Replace(' ', '_')] = val;
                    }
                    dg.Items.Add(row);
                    Console.WriteLine(i);
                }

                return new Variable(sqlResult.Tuple.Count);
            }

            return Variable.EmptyInstance;
        }
    }

    class WhoAmIFunction : ParserFunction
    {

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name, true);

            var gui = CSCS_GUI.GetInstance(script);

            return new Variable(Path.GetFileNameWithoutExtension(script.Filename));
        }
    }
    
    class ProgressBarFunction : ParserFunction
    {

        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name, true);

            var gui = CSCS_GUI.GetInstance(script);
            var widgetName = Utils.GetSafeString(args, 0);
            var newValue = Utils.GetSafeInt(args, 1);
            var widget = gui.GetWidget(widgetName);
            if (widget == null)
            {
                return Variable.EmptyInstance;
            }

            if(widget is ProgressBar)
            {
                var pb = widget as ProgressBar;

                //var t = new Thread(() => {
                //    Action action = () => { pb.Value = newValue; };

                //    pb.Dispatcher.BeginInvoke(action);
                //});
                //t.Start();

                ////---------------------
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;
                worker.DoWork += (object sender, DoWorkEventArgs e) => { (sender as BackgroundWorker).ReportProgress(newValue); };
                worker.ProgressChanged += (object sender, ProgressChangedEventArgs e) => { pb.Value = e.ProgressPercentage; };

                worker.RunWorkerAsync();
            }

            return Variable.EmptyInstance;
        }
    }

}
