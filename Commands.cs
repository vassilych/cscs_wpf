using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCSCS
{
    public class Commands
    {
        public void Init(CSCS_GUI gui)
        {
            Gui = gui;
            Interpreter interpreter = gui.Interpreter;

            //interpreter.RegisterFunction(Constants.OPENV, new OpenvCommand()); Constants.FUNCT_WITH_SPACE.Add(Constants.OPENV);
        }

        CSCS_GUI Gui { get; set; }

        //static void GetParameters(ParsingScript script, ref Dictionary<string, Variable> parameters)
        //{
        //    var gui = CSCS_GUI.GetInstance(script);
        //    var separator = new char[] { ';' };
        //    parameters = new Dictionary<string, Variable>();

        //    while (script.Current != Constants.END_STATEMENT && script.StillValid())
        //    {
        //        var tokens = Utils.GetTokens(script, separator);
                
        //        //var result = script.GetTempScript(tokens[2]).Execute(new char[] { '"' }, 0);//tokens[2]
                
        //        var labelName = Utils.GetToken(script, Constants.TOKEN_SEPARATION).ToLower();
        //        var value = labelName == "up" || labelName == "down" || labelName == "local" || labelName == "setup" || labelName == "close" ||
        //            labelName == "addrow" || labelName == "insertrow" || labelName == "deleterow" ?
        //            new Variable(true) :
        //                    script.Current == Constants.END_STATEMENT ? Variable.EmptyInstance :
        //                    new Variable(Utils.GetToken(script, separator));
        //                    //new Variable(Utils.GetToken(script, new char[] { ',', ';' }));
        //        if (script.Prev != '"' && !string.IsNullOrWhiteSpace(value.String))
        //        {
        //            //var existing = gui.Interpreter.GetVariableValue(value.String, script);
        //            var executed = script.GetTempScript(value.String).Execute(new char[] { ',' }, 0);
        //            value = executed == null ? value : executed;
        //        }
        //        parameters[labelName] = value;
        //        //m_lastParameter = labelName;
        //    }
        //}

        ////string GetParameter(string key, string defValue = "")
        ////{
        ////    Variable res;
        ////    if (!m_parameters.TryGetValue(key.ToLower(), out res))
        ////    {
        ////        return defValue;
        ////    }
        ////    return res.AsString();
        ////}
        ////double GetDoubleParameter(string key, double defValue = 0.0)
        ////{
        ////    Variable res;
        ////    if (!m_parameters.TryGetValue(key.ToLower(), out res))
        ////    {
        ////        return defValue;
        ////    }
        ////    return res.AsDouble();
        ////}
        ////int GetIntParameter(string key, int defValue = 0)
        ////{
        ////    Variable res;
        ////    if (!m_parameters.TryGetValue(key.ToLower(), out res))
        ////    {
        ////        return defValue;
        ////    }
        ////    return res.AsInt();
        ////}
        ////bool GetBoolParameter(string key, bool defValue = false)
        ////{
        ////    Variable res;
        ////    if (!m_parameters.TryGetValue(key.ToLower(), out res))
        ////    {
        ////        return defValue;
        ////    }
        ////    return res.AsBool();
        ////}
        ////Variable GetVariableParameter(string key, Variable defValue = null)
        ////{
        ////    Variable res;
        ////    if (!m_parameters.TryGetValue(key.ToLower(), out res))
        ////    {
        ////        return defValue;
        ////    }
        ////    return res;
        ////}

        //public class OpenvCommand : ParserFunction
        //{
        //    Dictionary<string, Variable> m_parameters;
        //    protected override Variable Evaluate(ParsingScript script)
        //    {
        //        //var firstParameterString = Utils.GetToken(script, new char[] { ' ', '}', ')', ';' });
        //        //var firstParameter = script.GetTempScript(firstParameterString).Execute(new char[] { '"' }, 0);
                
        //        //Name = Name.ToUpper();
                
        //        CSCS_GUI Gui = CSCS_GUI.GetInstance(script);


        //        //List<Variable> args = script.GetFunctionArgs();

        //        GetParameters(script, ref m_parameters);

        //        //Utils.CheckArgs(args.Count, 1, m_name);
        //        //var tableName = Utils.GetSafeString(args, 0);
        //        //var databaseName = Utils.GetSafeString(args, 1, CSCS_GUI.DefaultDB);
        //        //var lockingType = Utils.GetSafeString(args, 2, "n").ToLower();

        //        //var gui = CSCS_GUI.GetInstance(script);
        //        //return gui.BtrieveInstance.OPENV(tableName, databaseName, script, lockingType);


        //        //Utils.CheckArgs(args.Count, 3, m_name);
        //        //var name = args[0].AsString();
        //        //var lineCounter = Utils.GetSafeString(args, 1);
        //        //var actualElems = Utils.GetSafeString(args, 2);
        //        //var maxElems = Utils.GetSafeString(args, 3);
        //        //var result = DisplayArrSetup(script, name, lineCounter, actualElems, maxElems);
        //        //return result;

        //        //if (Name == Constants.DISPLAY_ARR_REFRESH)
        //        //{
        //        //    List<Variable> args = script.GetFunctionArgs();
        //        //    Utils.CheckArgs(args.Count, 1, m_name);
        //        //    var name = args[0].AsString();
        //        //    DisplayArrRefresh(Gui, name);
        //        //    return Variable.EmptyInstance;
        //        //}
        //        //GetParameters(script);

        //        //if (Name == Constants.MSG)
        //        //{
        //        //    string caption = GetParameter("caption");
        //        //    int duration = GetIntParameter("duration");
        //        //    return new Variable(objectName);
        //        //}
        //        //if (Name == Constants.DEFINE)
        //        //{
        //        //    Variable newVar = CreateVariable(script, objectName, GetVariableParameter("value"), GetVariableParameter("init"),
        //        //        GetParameter("type"), GetIntParameter("size"), GetIntParameter("dec"), GetIntParameter("array"),
        //        //        GetBoolParameter("local"), GetBoolParameter("up"), GetBoolParameter("down"), GetParameter("dup"));
        //        //    return newVar;
        //        //}
        //        //if (Name == Constants.DISPLAY_ARRAY)
        //        //{
        //        //    Variable newVar = DisplayArray(script, objectName, GetParameter("linecounter"), GetParameter("maxelements"),
        //        //        GetParameter("actualelements"), m_lastParameter);
        //        //    return newVar;
        //        //}
        //        //if (Name == Constants.DATA_GRID)
        //        //{
        //        //    Variable newVar = DataGrid(script, objectName, GetBoolParameter("addrow"), GetBoolParameter("insertrow"),
        //        //        GetBoolParameter("deleterow"), m_lastParameter);
        //        //    return newVar;
        //        //}
        //        //if (Name == Constants.ADD_COLUMN)
        //        //{
        //        //    AddGridColumn(script, objectName, GetParameter("header"), GetParameter("binding"));
        //        //    return new Variable(true);
        //        //}
        //        //if (Name == Constants.DELETE_COLUMN)
        //        //{
        //        //    DeleteGridColumn(script, objectName, GetIntParameter("num"));
        //        //    return new Variable(true);
        //        //}
        //        //if (Name == Constants.SHIFT_COLUMN)
        //        //{
        //        //    ShiftGridColumns(script, objectName, GetIntParameter("num"), GetIntParameter("to"));
        //        //    return new Variable(true);
        //        //}
        //        //if (Name == Constants.SET_OBJECT)
        //        //{
        //        //    string prop = GetParameter("property");
        //        //    bool val = GetBoolParameter("value");
        //        //    return new Variable(objectName);
        //        //}

        //        return Variable.EmptyInstance;

        //        //List<Variable> args = script.GetFunctionArgs();


        //    }


        //}
    }
}

