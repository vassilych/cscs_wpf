using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
    public enum FindvOption
    {
        First, Last, Next, Previous, MatchExact, Generic /* exact or greater */
    }

    public class CacheLine
    {
        public int id;
        public Dictionary<string, DefineVariable> Line = new Dictionary<string, DefineVariable>(); // one line of the table, string = fieldName, DefineVariable = value
    }

    public class CachingClass
    {
        public int numOfRowsToGet;
        public string KeyName; // for comparison with another key while "findv next"
        public List<CacheLine> CachedLines; // index = 1, 2, 3 to 300 
                                            //public static int (in) CSCS_GUI.MaxCacheSize, from .exe.config

        public CachingClass()
        {
            numOfRowsToGet = 3;
            KeyName = "";
            CachedLines = new List<CacheLine>();
        }
    }

    public class KeyClass
    {
        public int KeyNum;
        public string KeyName;
        public Dictionary<string, string> KeyColumns = new Dictionary<string, string>();

        public bool Ascending;
        public bool Unique;
    }

    public class OpenvTable
    {
        public int currentRow; // ID column
        public string tableName;

        public KeyClass CurrentKey; // last used index/key
        public List<KeyClass> Keys; // indexes/keys

        public List<string> FieldNames; // table columns

        public string databaseName; // optional parameter for another databse
        public string lockingType; //  N, R, F, X // not implemented
        public string lastAscDescOption;

        public int currentCacheListIndex;
        public CachingClass Cache = new CachingClass();
    }

    class FlerrFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name);
            var lastFnum = Utils.GetSafeInt(args, 0);

            int flerrInt = GetFlerr(lastFnum);

            return new Variable((double)flerrInt);
        }

        private int GetFlerr(int lastFnum)
        {
            if (lastFnum == 0)
            {
                return Btrieve.LastFlerrInt;
            }
            else
            {
                if (Btrieve.LastFlerrsOfFnums.TryGetValue(lastFnum, out int lastFlerr))
                {
                    return lastFlerr;
                }
                else
                {
                    throw new Exception("non-existing fnum!");
                }
            }
        }
    }

    class ScanStatement : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            return ProcessScan(script);
        }
        Variable ProcessScan(ParsingScript script)
        {
            string forString = Utils.GetBodyBetween(script, Constants.START_ARG, Constants.END_ARG);
            script.Forward();

            ProcessScan(script, forString);

            return Variable.EmptyInstance;
        }
        void ProcessScanNew(ParsingScript script, string forString)
        {
            var gui = script.Context as CSCS_GUI;
            int MAX_LOOPS = Interpreter.LastInstance.ReadConfig("maxLoops", 256000);

            string[] forTokens = forString.Split(Constants.END_STATEMENT);
            if (forTokens.Length != 7)
            {
                throw new ArgumentException("Expecting: scan(hndlNum; \"keyName\"; \"start|start2\"; \"whileString\"; \"forString\"; \"scope(a)\"; \"lock(t / f)\")");
            }

            int startForCondition = script.Pointer;


            int tableHndlNum = (int)script.GetTempScript(forTokens[0]).Execute(null, 0).Value;
            var keyName = script.GetTempScript(forTokens[1]).Execute(null, 0);
            var startString = script.GetTempScript(forTokens[2]).Execute(null, 0).AsString();
            var whileString = script.GetTempScript(forTokens[3]);
            var forExpression = script.GetTempScript(forTokens[4]).Execute(null, 0).AsString();
            var scope = script.GetTempScript(forTokens[5]).Execute(null, 0);
            var nlock = script.GetTempScript(forTokens[6]).Execute(null, 0);

            KeyClass KeyClass;

            int cycles = 0;
            bool stillValid = true;

            //new Btrieve.FINDVClass((int)tableHndlNum.Value, "g", keyName.String, startString.AsString(), forExpression.String).FINDV();

            var scopeString = scope.String.ToLower();
            bool limited = false;
            int selectLimit = 0;
            int selectLimitCounter = 0;
            if (scopeString.StartsWith("n"))
            {
                var selectLimitString = scopeString.TrimStart('n').Replace(" ", "");
                if (int.TryParse(selectLimitString, out selectLimit))
                {
                    limited = true;
                    selectLimitCounter = selectLimit;
                }
                else
                {
                    Btrieve.SetFlerr(12, tableHndlNum);
                    return;
                }
            }

            //--------------------------------------
            
            var thisOpenv = Btrieve.OPENVs[tableHndlNum];


            //get KeyClass
            if (keyName.AsString().StartsWith("@") && int.TryParse(keyName.AsString().TrimStart('@'), out int keyNum))
            {
                if (keyNum > 0)
                {
                    // for optimization
                    var kljuceviTable = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_SCHEMA == CSCS_GUI.Adictionary.SY_TABLESList.First(r => r.SYCT_NAME.ToUpper() == thisOpenv.tableName.ToUpper()).SYCT_SCHEMA).OrderBy(s => s.SYKI_KEYNUM).ToArray();

                    KeyClass = thisOpenv.Keys.First(p => p.KeyName == kljuceviTable.Where(r => r.SYKI_KEYNUM == keyNum).First().SYKI_KEYNAME);
                }
                else
                {
                    KeyClass = new KeyClass() { KeyName = "ID", Ascending = true, Unique = true, KeyNum = 0, KeyColumns = new Dictionary<string, string>() { { "ID", "" } } };
                }
            }
            else if (!thisOpenv.Keys.Any(p => p.KeyName.ToUpper() == keyName.AsString().ToUpper()))
            {
                // "Key does not exist for this table!"
                Btrieve.SetFlerr(4, tableHndlNum);
                return;
            }
            else
            {
                KeyClass = thisOpenv.Keys.First(p => p.KeyName.ToUpper() == keyName.AsString().ToUpper());
            }

            //start string
            List<string> keySegmentsOrdered = new List<string>();

            var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).ToList();
            keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();

            if (!KeyClass.Unique)
            {
                keySegmentsOrdered.Add("ID");
            }

            if (KeyClass.KeyNum != 0 && string.IsNullOrEmpty(startString))
            {
                StringBuilder matchExactStringBuilder = new StringBuilder();

                for (int i = 0; i < keySegmentsOrdered.Count; i++)
                {
                    matchExactStringBuilder.Append(CSCS_GUI.DEFINES[keySegmentsOrdered[i].ToLower()]);
                    matchExactStringBuilder.Append("|");
                }
                matchExactStringBuilder.Remove(matchExactStringBuilder.Length - 1, 1); // remove last "|"
                startString = matchExactStringBuilder.ToString();
            }

            //OderBySB
            StringBuilder orderBySB = new StringBuilder();

            string ascDescOption = "";

            ascDescOption = "asc";
            
            foreach (var segmentName in KeyClass.KeyColumns.Keys)
            {
                orderBySB.Append($" {segmentName} {ascDescOption},");
            }

            if (!KeyClass.Unique)
            {
                orderBySB.Append(" ID ");
                orderBySB.Append("asc");
            }
            else
            {
                orderBySB.Remove(orderBySB.Length - 1, 1);
            }


            //sql where -- start string & 
            StringBuilder sqlStartStringSB = new StringBuilder();


            var matchExactValues = startString.Split('|');
            if (matchExactValues.Count() != KeyClass.KeyColumns.Count())
            {
                Btrieve.SetFlerr(99, tableHndlNum);
                return;
            }


            //string sqlWhereString = "(";
            //for (int i = 0; i < keySegmentsOrdered.Count; i++)
            //{
            //    sqlWhereString += keySegmentsOrdered[i] + " >=" + matchExactValues[i] + " AND ";
            //}
            //sqlWhereString = sqlWhereString.Substring(0, sqlWhereString.Length - 5);
            //sqlWhereString += ")";

            //if (!string.IsNullOrEmpty(forExpression))
            //{
            //    sqlWhereString += " AND (" + forExpression + ")";
            //}
            
            // startString and forSqlExpression
            string sqlWhereString = "((";
            for (int j = 0; j < keySegmentsOrdered.Count; j++)
            {
                for (int i = 0; i < keySegmentsOrdered.Count - j; i++)
                {
                    string compareSign = "";
                    if (i < keySegmentsOrdered.Count - j - 1)
                        compareSign = " = ";
                    else if (i == keySegmentsOrdered.Count - j - 1)
                    {
                        if (j == 0)
                            compareSign = " >= ";
                        else
                            compareSign = " > ";
                    }
                        sqlWhereString += keySegmentsOrdered[i] + compareSign + matchExactValues[i] + " AND ";
                }
                sqlWhereString = sqlWhereString.Substring(0, sqlWhereString.Length - 5);
                sqlWhereString += ") OR (";    
            }
            sqlWhereString = sqlWhereString.Substring(0, sqlWhereString.Length - 5);
            sqlWhereString += ")";


            if (!string.IsNullOrEmpty(forExpression))
            {
                sqlWhereString += " AND (" + forExpression + ")";
            }



            string query =
    $@"EXECUTE sp_executesql N'
SELECT {(limited? "TOP " + selectLimit : "")}
*
FROM {Btrieve.Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}

with (nolock) 

{(!string.IsNullOrEmpty(sqlWhereString) ? " WHERE (" + sqlWhereString + ")" : "")} 

ORDER BY {orderBySB}
'";


            int currentSqlId = 0;

            using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Btrieve.SetFlerr(3, tableHndlNum);
                        return;
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            currentSqlId = (int)reader["ID"];
                            int currentFieldNum = 1;
                            while (currentFieldNum < reader.FieldCount)
                            {
                                var currentColumnName = reader.GetName(currentFieldNum);
                                if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                {
                                    if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                    {
                                        KeyClass.KeyColumns[currentColumnName] = ((DateTime)reader[currentColumnName]).ToString("yyyy-MM-dd");
                                    }
                                    else
                                    {
                                        KeyClass.KeyColumns[currentColumnName] = reader[currentColumnName].ToString();
                                    }
                                }

                                var loweredCurrentColumnName = currentColumnName.ToLower();
                                if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                {
                                    return;
                                }
                                else
                                {
                                    if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                    {
                                        DateTime fieldValue = (DateTime)reader[currentColumnName];
                                        var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                        var newVar = new Variable(fieldValue.ToString(dateFormat));
                                        CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(newVar);
                                        gui.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                    }
                                    else
                                    {
                                        string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                        CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue).Clone());
                                        gui.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                    }
                                }
                                currentFieldNum++;
                            }


                            if (stillValid)
                            {
                                if (limited && selectLimitCounter == 0)
                                {
                                    Btrieve.SetFlerr(0, tableHndlNum);
                                    break;
                                }

                                var condResult = whileString.Execute(null, 0);

                                stillValid = Convert.ToBoolean(condResult.Value);
                                if (!stillValid)
                                {
                                    break;
                                }

                                if (MAX_LOOPS > 0 && ++cycles >= MAX_LOOPS)
                                {
                                    throw new ArgumentException("Looks like an infinite loop after " +
                                                                  cycles + " cycles.");
                                }

                                script.Pointer = startForCondition;
                                Variable result = Interpreter.LastInstance.ProcessBlock(script);

                                if (limited)
                                {
                                    selectLimitCounter--;
                                    if (selectLimitCounter == 0)
                                    {
                                        Btrieve.SetFlerr(0, tableHndlNum);
                                        break;
                                    }
                                }

                                if (result.IsReturn || result.Type == Variable.VarType.BREAK)
                                {
                                    break;
                                }

                                //new Btrieve.FINDVClass(tableHndlNum, "n", keyName.String).FINDV();
                                if (Btrieve.LastFlerrsOfFnums[tableHndlNum] == 3)
                                {
                                    Btrieve.SetFlerr(0, tableHndlNum);
                                    break;
                                }

                            }

                            Btrieve.SetFlerr(0, tableHndlNum);

                            script.Pointer = startForCondition;
                            Interpreter.LastInstance.SkipBlock(script);
                        }

                        
                    }
                    thisOpenv.CurrentKey = KeyClass;
                    thisOpenv.currentRow = currentSqlId;
                    Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                }
            }

            Btrieve.SetFlerr(0, tableHndlNum);

            
        }
        void ProcessScan(ParsingScript script, string forString)
        {
            int MAX_LOOPS = Interpreter.LastInstance.ReadConfig("maxLoops", 256000);

            string[] forTokens = forString.Split(Constants.END_STATEMENT);
            if (forTokens.Length != 7)
            {
                throw new ArgumentException("Expecting: scan(hndlNum; \"keyName\"; \"start|start2\"; \"whileString\"; \"forString\"; \"scope(a)\"; \"lock(t / f)\")");
            }

            int startForCondition = script.Pointer;


            var tableHndlNum = script.GetTempScript(forTokens[0]).Execute(null, 0);
            var keyName = script.GetTempScript(forTokens[1]).Execute(null, 0);
            var startString = script.GetTempScript(forTokens[2]).Execute(null, 0);
            var whileString = script.GetTempScript(forTokens[3]);
            var forExpression = script.GetTempScript(forTokens[4]).Execute(null, 0);
            var scope = script.GetTempScript(forTokens[5]).Execute(null, 0);
            var nlock = script.GetTempScript(forTokens[6]).Execute(null, 0);


            int cycles = 0;
            bool stillValid = true;

            new Btrieve.FINDVClass((int)tableHndlNum.Value, "g", keyName.String, startString.AsString(), forExpression.String, "", script).FINDV();

            var scopeString = scope.String.ToLower();
            bool limited = false;
            int selectLimit = 0;
            int selectLimitCounter = 0;
            if (scopeString.StartsWith("n"))
            {
                var selectLimitString = scopeString.TrimStart('n').Replace(" ", "");
                if (int.TryParse(selectLimitString, out selectLimit))
                {
                    limited = true;
                    selectLimitCounter = selectLimit;
                }
                else
                {
                    Btrieve.SetFlerr(12, (int)tableHndlNum.Value);
                    return;
                }
            }

            while (stillValid)
            {
                if (limited && selectLimitCounter == 0)
                {
                    Btrieve.SetFlerr(0, (int)tableHndlNum.Value);
                    break;
                }

                var condResult = whileString.Execute(null, 0);

                stillValid = Convert.ToBoolean(condResult.Value);
                if (!stillValid)
                {
                    break;
                }

                if (MAX_LOOPS > 0 && ++cycles >= MAX_LOOPS)
                {
                    throw new ArgumentException("Looks like an infinite loop after " +
                                                  cycles + " cycles.");
                }

                script.Pointer = startForCondition;
                Variable result = Interpreter.LastInstance.ProcessBlock(script);

                if (limited)
                {
                    selectLimitCounter--;
                    if (selectLimitCounter == 0)
                    {
                        Btrieve.SetFlerr(0, (int)tableHndlNum.Value);
                        break;
                    }
                }

                if (result.IsReturn || result.Type == Variable.VarType.BREAK)
                {
                    break;
                }

                new Btrieve.FINDVClass((int)tableHndlNum.Value, "n", keyName.String, "", "", "", script).FINDV();
                if (Btrieve.LastFlerrsOfFnums[(int)tableHndlNum.Value] == 3)
                {
                    Btrieve.SetFlerr(0, (int)tableHndlNum.Value);
                    break;
                }

            }

            Btrieve.SetFlerr(0, (int)tableHndlNum.Value);

            script.Pointer = startForCondition;
            Interpreter.LastInstance.SkipBlock(script);
        }
    }



    public class Btrieve
    {
        public static void Init()
        {
            Interpreter.LastInstance.RegisterFunction(Constants.OPENV, new OpenvFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.FINDV, new FindvFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.CLOSEV, new ClosevFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.REPL, new ReplFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.CLR, new ClrFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.RCNGET, new RcnGetFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.RCNSET, new RcnSetFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.ACTIVE, new ActiveFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.DEL, new DelFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.SAVE, new SaveFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.RDA, new RDAFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.WRTA, new WRTAFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.TRC, new TRCFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.FLERR, new FlerrFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.SCAN, new ScanStatement());

            Interpreter.LastInstance.RegisterFunction(Constants.DISPLAY_TABLE_SETUP, new DisplayTableSetupFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.DISPLAY_ARRAY_SETUP, new DisplayArraySetupFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.DISPLAY_ARRAY_REFRESH, new DisplayArrayRefreshFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.DISPLAYARRAY, new DisplayArrayFunction());
            Interpreter.LastInstance.RegisterFunction(Constants.DISPLAYTABLE, new DisplayTableFunction());

            Interpreter.LastInstance.RegisterFunction(Constants.DATAGRID, new DataGridFunction());

            //Interpreter.LastInstance.RegisterFunction(Constants.SCAN, new ScanStatement());
        }

        public static Dictionary<string, string> Databases { get; set; } = new Dictionary<string, string>(); // <SYCD_USERCODE, SYCD_DBASENAME>

        public static Dictionary<int, OpenvTable> OPENVs { get; set; } =
            new Dictionary<int, OpenvTable>();

        public static int LastFlerrInt;
        public static Dictionary<int, int> LastFlerrsOfFnums = new Dictionary<int, int>();
        public static void SetFlerr(int errNum, int fnum = 0)
        {
            LastFlerrInt = errNum;
            if (fnum != 0)
                LastFlerrsOfFnums[fnum] = errNum;
        }

        public static Variable CLOSEV(int tableHndlNum)
        {
            if (CSCS_SQL.SqlServerConnection.State != System.Data.ConnectionState.Closed)
            {
                CSCS_SQL.SqlServerConnection.Close();
            }

            return Variable.EmptyInstance;
        }

        public static Variable OPENV(string tableName, string databaseName, ParsingScript script, string lockingType)
        {
            if (CSCS_SQL.SqlServerConnection.State != System.Data.ConnectionState.Open)
            {
                CSCS_SQL.SqlServerConnection.Open();
            }

            //
            var table = CSCS_GUI.Adictionary.SY_TABLESList.FirstOrDefault(p => p.SYCT_NAME.ToUpper() == tableName.ToUpper() && p.SYCT_USERCODE == databaseName.ToUpper());
            if (table == null)
            {
                // err: "There's no table with name {tableName.ToUpper()} in database {databaseName.ToUpper()}!"

                SetFlerr(7);
                return new Variable((long)0); // tableHndl fill with 0 -> error
            }

            var listOfFields = CSCS_GUI.Adictionary.SY_FIELDSList.Where(p => p.SYTD_SCHEMA == table.SYCT_SCHEMA).ToList();

            foreach (var field in listOfFields)
            {
                if (!CSCS_GUI.DEFINES.ContainsKey(field.SYTD_FIELD))
                {
                    DefineVariable newVar = new DefineVariable(field.SYTD_FIELD, null, field.SYTD_TYPE, field.SYTD_SIZE, field.SYTD_DEC, field.SYTD_ARRAYNUM/*, local, up*/);
                    newVar.InitVariable(Variable.EmptyInstance, script);
                }
            }

            List<KeyClass> listOfKeys = new List<KeyClass>();

            var listOfKeySegments = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_SCHEMA == table.SYCT_SCHEMA).OrderBy(p => p.SYKI_KEYNUM).ThenBy(p => p.SYKI_SEGNUM).ToList();

            for (int i = 1; i <= listOfKeySegments.Max(p => p.SYKI_KEYNUM); i++)
            {
                var thisKey = listOfKeySegments.FirstOrDefault(p => p.SYKI_KEYNUM == i);

                Dictionary<string, string> listOfSegments = new Dictionary<string, string>();
                foreach (var keySegment in listOfKeySegments.Where(p => p.SYKI_KEYNUM == i).OrderBy(p => p.SYKI_SEGNUM))
                {
                    listOfSegments[keySegment.SYKI_FIELD] = "";
                }

                listOfKeys.Add(new KeyClass()
                {
                    KeyNum = thisKey.SYKI_KEYNUM,
                    KeyName = thisKey.SYKI_KEYNAME,
                    KeyColumns = listOfSegments,
                    Ascending = thisKey.SYKI_ASCDESC == "A" ? true : false,
                    Unique = thisKey.SYKI_UNIQUE == "Y" ? true : false
                });
            }

            var thisFnum = Btrieve.OPENVs.Keys.Count > 0 ? Btrieve.OPENVs.Keys.Max() + 1 : 1;
            Btrieve.OPENVs.Add(thisFnum, new OpenvTable()
            {
                tableName = tableName.ToLower(),
                databaseName = databaseName.ToLower(),
                FieldNames = listOfFields.Select(p => p.SYTD_FIELD).ToList(),
                Keys = listOfKeys,
                Cache = new CachingClass(),
                lockingType = lockingType
            });

            SetFlerr(0, thisFnum);

            return new Variable((long)thisFnum);
        }

        public class FINDVClass
        {
            public FINDVClass(int _tableHndlNum, string _operationType, string _tableKey = "", string _matchExactValue = "", string _forString = "", string _columnsToSelect = "", ParsingScript _script = null, string _keyo = "")
            {
                tableHndlNum = _tableHndlNum;
                operationType = _operationType;
                tableKey = _tableKey;
                matchExactString = _matchExactValue;
                forString = _forString;
                columnsToSelect = _columnsToSelect;
                script = _script;
                keyo = _keyo;
                Gui = script.Context as CSCS_GUI;
            }

            ParsingScript script;
            CSCS_GUI Gui;

            int tableHndlNum;
            string operationType;
            string tableKey;
            OpenvTable thisOpenv;
            string matchExactString;
            string forString;
            string columnsToSelect;
            string keyo;

            public static Dictionary<int, string> nextPrevCachedWhereStrings = new Dictionary<int, string>(); // <tableHndlNum, nextPrevCachedWhereString>
            public static Dictionary<int, string> cachedSqlForString = new Dictionary<int, string>(); // <tableHndlNum, cachedSqlForString>
            public static Dictionary<int, string> cachedColumnsToSelect = new Dictionary<int, string>(); // <tableHndlNum, cachedColumnsToSelect>

            static Dictionary<int, FindvOption> lastUsedPreviousOrNext = new Dictionary<int, FindvOption>();

            static KeyClass KeyClass;

            public Variable FINDV()
            {
                bool keyNeeded = false; // for Next and Previous the last key is used
                if (operationType == "f"
                    || operationType == "l"
                    || operationType == "m"
                    || operationType == "g")
                {
                    keyNeeded = true;
                }

                thisOpenv = OPENVs[tableHndlNum];

                if (keyNeeded)
                {
                    if (tableKey.StartsWith("@") && int.TryParse(tableKey.TrimStart('@'), out int keyNum))
                    {
                        if (keyNum > 0)
                        {
                            var kljuceviTable = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_SCHEMA == CSCS_GUI.Adictionary.SY_TABLESList.First(r => r.SYCT_NAME.ToUpper() == thisOpenv.tableName.ToUpper()).SYCT_SCHEMA).OrderBy(s => s.SYKI_KEYNUM);

                            // new @ num
                            KeyClass = thisOpenv.Keys.FirstOrDefault(p => p.KeyNum == keyNum);
                        }
                        else
                        {
                            KeyClass = new KeyClass() { KeyName = "ID", Ascending = true, Unique = true, KeyNum = 0, KeyColumns = new Dictionary<string, string>() { { "ID", "" } } };
                        }
                    }
                    else if (!thisOpenv.Keys.Any(p => p.KeyName.ToUpper() == tableKey.ToUpper()))
                    {
                        // "Key does not exist for this table!"
                        SetFlerr(4, tableHndlNum);
                        return Variable.EmptyInstance;
                    }
                    else
                    {
                        KeyClass = thisOpenv.Keys.First(p => p.KeyName.ToUpper() == tableKey.ToUpper());
                    }
                }
                else
                {
                    KeyClass = thisOpenv.CurrentKey;
                }

                if (keyNeeded && KeyClass.KeyNum != 0 && string.IsNullOrEmpty(matchExactString))
                {
                    StringBuilder matchExactStringBuilder = new StringBuilder();

                    var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).ToList();

                    var keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();

                    for (int i = 0; i < keySegmentsOrdered.Count; i++)
                    {
                        matchExactStringBuilder.Append(CSCS_GUI.DEFINES[keySegmentsOrdered[i].ToLower()]);
                        matchExactStringBuilder.Append("|");
                    }
                    matchExactStringBuilder.Remove(matchExactStringBuilder.Length - 1, 1); // remove last "|"
                    matchExactString = matchExactStringBuilder.ToString();
                }

                switch (operationType)
                {
                    case "f":
                        return findFirstOrLast(FindvOption.First);
                    case "l":
                        return findFirstOrLast(FindvOption.Last);
                    case "n":
                        if (thisOpenv.lockingType == "a") 
                        {
                            return findNextOrPrevious(FindvOption.Next);
                        }
                        else
                        {
                            return findNextOrPreviousWithoutCacheing(FindvOption.Next);
                        }
                    case "p":
                        if (thisOpenv.lockingType == "a")
                        {
                            return findNextOrPrevious(FindvOption.Previous);
                        }
                        else
                        {
                            return findNextOrPreviousWithoutCacheing(FindvOption.Previous);
                        }
                    case "m":
                        return findMatchExact(FindvOption.MatchExact);
                    case "g":
                        return findGeneric(FindvOption.Generic);
                    default:
                        SetFlerr(1, tableHndlNum);
                        return Variable.EmptyInstance;
                }
            }

            public static string GetOrderByString(FindvOption option, OpenvTable thisOpenv, KeyClass keyClass)
            {
                StringBuilder orderBySB = new StringBuilder();

                string ascDescOption = "";

                if (option == FindvOption.First)
                {
                    ascDescOption = "asc";
                    thisOpenv.lastAscDescOption = "asc";
                }
                else if (option == FindvOption.Last)
                {
                    ascDescOption = "desc";
                    thisOpenv.lastAscDescOption = "desc";
                }
                else if (option == FindvOption.Next)
                {
                    //ascDescOption = thisOpenv.lastAscDescOption;
                    ascDescOption = "asc";
                }
                else if (option == FindvOption.Previous)
                {
                    ascDescOption = "desc";
                }
                else if (option == FindvOption.MatchExact || option == FindvOption.Generic)
                {
                    ascDescOption = "asc";
                }

                foreach (var segmentName in keyClass.KeyColumns.Keys)
                {
                    orderBySB.Append($" {segmentName} {ascDescOption},");
                }

                if (!keyClass.Unique)
                {
                    orderBySB.Append(" ID ");
                    orderBySB.Append(option != FindvOption.Next ? ascDescOption : "asc");
                }
                else
                {
                    orderBySB.Remove(orderBySB.Length - 1, 1);
                }

                return orderBySB.ToString();
            }

            private Variable findFirstOrLast(FindvOption option)
            {
                nextPrevCachedWhereStrings[tableHndlNum] = null;
                cachedSqlForString[tableHndlNum] = null;
                cachedColumnsToSelect[tableHndlNum] = null;


                int currentSqlId = 0;

                string columns = "*";
                if (!string.IsNullOrEmpty(columnsToSelect))
                {
                    columns = "ID, " + columnsToSelect;

                    var splittedColumns = columnsToSelect.Replace(" ", "").Split(',');
                    foreach (var segment in KeyClass.KeyColumns.Keys)
                    {
                        if (!splittedColumns.Any(p => p == segment))
                        {
                            columns += ", " + segment;
                        }
                    }
                    clearBuffer(thisOpenv);

                    cachedColumnsToSelect[tableHndlNum] = columns;
                }

                string sqlForString = "";
                if (!string.IsNullOrEmpty(forString))
                {
                    sqlForString = GetForString(forString);
                    cachedSqlForString[tableHndlNum] = sqlForString;
                }

                string orderByString = GetOrderByString(option, thisOpenv, KeyClass);

                string query =
    $@"EXECUTE sp_executesql N'
select top 1
{columns}
from {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}

with (nolock) 

{(!string.IsNullOrEmpty(sqlForString) ? " WHERE (" + sqlForString + ")" : "")} 

order by {orderByString}
'";

                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            SetFlerr(3, tableHndlNum);
                            return new Variable((long)3);
                        }
                        else
                        {
                            if (keyo == "keyo")
                            {
                                SetFlerr(0, tableHndlNum);
                                return Variable.EmptyInstance;
                            }

                            while (reader.Read())
                            {
                                currentSqlId = (int)reader["ID"];
                                int currentFieldNum = 1;
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);
                                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = ((DateTime)reader[currentColumnName]).ToString("yyyy-MM-dd");
                                        }
                                        else
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = reader[currentColumnName].ToString();
                                        }
                                    }

                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    {
                                        return new Variable((long)4);
                                    }
                                    else
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            //DateTime fieldValue;
                                            //var toString = reader[currentColumnName].ToString();
                                            //if (DateTime.TryParse(toString, out fieldValue)){}
                                            
                                            DateTime fieldValue = (DateTime)reader[currentColumnName];
                                            var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                            var newVar = new Variable(fieldValue.ToString(dateFormat));
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(newVar);
                                            Gui.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue), script);
                                            Gui.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                        }
                                    }
                                    currentFieldNum++;
                                }

                            }

                            //OPENVs
                            thisOpenv.CurrentKey = KeyClass;
                            thisOpenv.currentRow = currentSqlId;
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                            Btrieve.OPENVs[tableHndlNum].Cache = new CachingClass() { KeyName = KeyClass.KeyName };
                            //Btrieve.OPENVs[tableHndlNum].currentCacheListIndex = 1; // ??
                        }
                    }
                }

                SetFlerr(0, tableHndlNum); // 0 means OK
                return Variable.EmptyInstance;
            }

            private string GetCompareSign(FindvOption option)
            {
                string compareSign = "";

                if (option == FindvOption.Next)
                {
                    compareSign = ">";
                }

                else if (option == FindvOption.Previous)
                {
                    compareSign = "<";
                }

                return compareSign;
            }

            private string GetNextOrPreviousWhereStringWithParams(FindvOption option, string forString = null)
            {
                List<string> keySegmentsOrdered = new List<string>();

                if (KeyClass.KeyNum != 0)
                {
                    var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == thisOpenv.CurrentKey.KeyName).ToList();

                    keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();

                    if (keyUsed.First().SYKI_UNIQUE == "N")
                    {
                        keySegmentsOrdered.Add("ID");
                    }
                }
                else
                {
                    keySegmentsOrdered.Add("ID");
                }

                StringBuilder wStringBuilder = new StringBuilder();

                string compareSign = GetCompareSign(option);

                for (int j = keySegmentsOrdered.Count; j > 0; j--)
                {
                    wStringBuilder.Append("(");
                    for (int i = 0; i < j; i++)
                    {
                        wStringBuilder.Append(keySegmentsOrdered[i] + $" {(i + 1 == j ? " " + compareSign + " " : " = ")} " + $"@{i} AND ");
                    }
                    wStringBuilder.Remove(wStringBuilder.Length - 5, 5); // remove last " AND "
                    wStringBuilder.Append(")");

                    wStringBuilder.Append(" OR ");
                }
                wStringBuilder.Remove(wStringBuilder.Length - 4, 4); // remove last " OR "

                return wStringBuilder.ToString();
            }

            private string getFieldType(string fieldName)
            {
                var fieldType = CSCS_GUI.Adictionary.SY_FIELDSList.Where(p => p.SYTD_FIELD == fieldName).Select(p => p.SYTD_TYPE).First();

                switch (fieldType)
                {
                    case "O":// ??
                        return "";// it's not beeing used
                    case "T":// time
                        return "time";
                    case "N":// numeric(with decimal)
                        return "float";
                    case "A":// alphanumeric
                        return "varchar(max)";
                    case "D":// date
                        return "date";
                    case "L":// logic
                        return "bit";
                    case "R":// record
                        return "int";
                    case "M":// memo
                        return "";// it's not beeing used
                    case "B":// byte
                        return "int";
                    case "I":// int
                        return "int";
                    case "V":// overlay
                        return "";// it's not beeing used
                    default:
                        break;
                }

                return fieldType;
            }

            private string GetParametersDeclaration(KeyClass KeyClass)
            {
                List<string> keySegmentsOrdered = new List<string>();

                int numOfParams = 0;

                if (KeyClass.KeyNum != 0)
                {
                    var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).ToList();

                    numOfParams = keyUsed.Count() + (keyUsed.First().SYKI_UNIQUE == "N" ? 1 : 0);

                    keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();

                    if (keyUsed.First().SYKI_UNIQUE == "N")
                    {
                        keySegmentsOrdered.Add("ID");
                    }
                }
                else
                {
                    keySegmentsOrdered.Add("ID");
                    numOfParams = 1;
                }

                StringBuilder pdStringBuilder = new StringBuilder();

                for (int i = 0; i < numOfParams; i++)
                {
                    if (keySegmentsOrdered[i] == "ID")
                    {
                        pdStringBuilder.Append($"@{i} int, ");
                    }
                    else
                    {
                        pdStringBuilder.Append($"@{i} {getFieldType(keySegmentsOrdered[i])}, ");
                    }
                }

                pdStringBuilder.Remove(pdStringBuilder.Length - 2, 2); // remove last ", "

                return pdStringBuilder.ToString();
            }

            private string GetGenericParametersDeclaration()
            {
                List<string> keySegmentsOrdered = new List<string>();

                int numOfParams = 0;

                if (KeyClass.KeyNum != 0)
                {
                    var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == thisOpenv.CurrentKey.KeyName).ToList();

                    numOfParams = keyUsed.Count() + (keyUsed.First().SYKI_UNIQUE == "N" ? 1 : 0);

                    keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();

                    if (keyUsed.First().SYKI_UNIQUE == "N")
                    {
                        keySegmentsOrdered.Add("ID");
                    }
                }
                else
                {
                    keySegmentsOrdered.Add("ID");
                    numOfParams = 1;
                }

                StringBuilder pdStringBuilder = new StringBuilder();

                for (int i = 0; i < numOfParams; i++)
                {
                    if (keySegmentsOrdered[i] == "ID")
                    {
                        pdStringBuilder.Append($"@{i} int, ");
                    }
                    else
                    {
                        pdStringBuilder.Append($"@{i} {getFieldType(keySegmentsOrdered[i])}, ");
                    }
                }

                pdStringBuilder.Remove(pdStringBuilder.Length - 2, 2); // remove last ", "

                return pdStringBuilder.ToString();
            }

            private string GetParametersValues()
            {
                StringBuilder pvStringBuilder = new StringBuilder();

                List<string> keySegmentsOrdered = new List<string>();

                if (KeyClass.KeyNum != 0)
                {
                    var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == thisOpenv.CurrentKey.KeyName).ToList();

                    keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();
                }


                for (int i = 0; i < keySegmentsOrdered.Count; i++)
                {
                    pvStringBuilder.Append("\'" + thisOpenv.CurrentKey.KeyColumns[keySegmentsOrdered[i]] + "\', ");
                }
                if (KeyClass.Unique == false || KeyClass.KeyNum == 0)
                {
                    pvStringBuilder.Append("\'" + thisOpenv.currentRow + "\', ");
                }
                pvStringBuilder.Remove(pvStringBuilder.Length - 2, 2); // remove last ", "

                // segments oredered + ID if not unique
                return pvStringBuilder.ToString();
            }

            private string GetGenericParametersValues(KeyClass KeyClass, string[] matchExactValues)
            {
                StringBuilder pvStringBuilder = new StringBuilder();

                List<string> keySegmentsOrdered = new List<string>();

                if (KeyClass.KeyNum != 0)
                {
                    var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).ToList();

                    keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();
                }


                for (int i = 0; i < keySegmentsOrdered.Count; i++)
                {
                    pvStringBuilder.Append("\'" + matchExactValues[i] + "\', ");
                }
                if (KeyClass.Unique == false || KeyClass.KeyNum == 0)
                {
                    pvStringBuilder.Append("\'" + "0" + "\', ");
                }
                pvStringBuilder.Remove(pvStringBuilder.Length - 2, 2); // remove last ", "

                // segments oredered + ID if not unique
                return pvStringBuilder.ToString();
            }


            private string GetMatchExactWhereString(string[] matchExactValues = null, string forString = null)
            {
                StringBuilder mStringBuilder = new StringBuilder();

                mStringBuilder.Append("(");

                string[] tempKey;
                if (KeyClass.KeyNum != 0)
                    tempKey = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToArray();
                else
                    tempKey = new string[] { "ID" };

                //match exact values
                for (int i = 0; i < KeyClass.KeyColumns.Count(); i++)
                {
                    mStringBuilder.Append(tempKey[i]);
                    mStringBuilder.Append(" = ");
                    mStringBuilder.Append($"''{matchExactValues[i]}''");
                    mStringBuilder.Append(" and ");
                }
                mStringBuilder.Remove(mStringBuilder.Length - 5, 5);

                mStringBuilder.Append(")");

                return mStringBuilder.ToString();

            }

            private string GetGenericWhereStringWithParams(KeyClass KeyClass)
            {
                List<string> keySegmentsOrdered = new List<string>();

                if (KeyClass.KeyNum != 0)
                {
                    var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).ToList();

                    keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();

                    if (keyUsed.First().SYKI_UNIQUE == "N")
                    {
                        keySegmentsOrdered.Add("ID");
                    }
                }
                else
                {
                    keySegmentsOrdered.Add("ID");
                }

                StringBuilder gStringBuilder = new StringBuilder();

                gStringBuilder.Append("(");
                

                for (int i = 0; i < keySegmentsOrdered.Count; i++)
                {
                    gStringBuilder.Append(keySegmentsOrdered[i] + $" = " + $"@{i} AND ");

                }
                gStringBuilder.Remove(gStringBuilder.Length - 5, 5); // remove last " AND "
                

                gStringBuilder.Append(")");

                gStringBuilder.Append(" OR ");

                for (int j = keySegmentsOrdered.Count; j > 0; j--)
                {
                    gStringBuilder.Append("(");
                    for (int i = 0; i < j; i++)
                    {
                        gStringBuilder.Append(keySegmentsOrdered[i] + $" {(i +1 == j ? " > " : " = ")} " + $"@{i} AND ");

                    }
                    gStringBuilder.Remove(gStringBuilder.Length - 5, 5); // remove last " AND "
                    gStringBuilder.Append(")");

                    gStringBuilder.Append(" OR ");
                }
                gStringBuilder.Remove(gStringBuilder.Length - 4, 4); // remove last " OR "

                return gStringBuilder.ToString();
            }

            private string GetGenericWhereString(string[] matchExactValues = null)
            {
                StringBuilder gStringBuilder = new StringBuilder();

                List<string> keySegmentsOrdered = new List<string>();
                if (KeyClass.KeyNum != 0)
                {
                    var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).ToList();

                    keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();
                }
                else
                {
                    keySegmentsOrdered.Add("ID");
                }

                gStringBuilder.Append("(");
                for (int i = 0; i < keySegmentsOrdered.Count; i++)
                {
                    gStringBuilder.Append(keySegmentsOrdered[i] + " = " + $"\'\'{matchExactValues[i].Trim('\'')}\'\' AND ");
                }
                gStringBuilder.Remove(gStringBuilder.Length - 5, 5);// remove last " AND "

                gStringBuilder.Append(") OR ");

                for (int j = keySegmentsOrdered.Count; j > 0; j--)
                {
                    gStringBuilder.Append("(");
                    for (int i = 0; i < j; i++)
                    {
                        gStringBuilder.Append(keySegmentsOrdered[i] + $" {(i + 1 == j ? " > " : " = ")} " + $"\'\'{matchExactValues[i].Trim('\'')}\'\' AND ");
                    }
                    gStringBuilder.Remove(gStringBuilder.Length - 5, 5); // remove last " AND "
                    gStringBuilder.Append(")");

                    gStringBuilder.Append(" OR ");
                }

                gStringBuilder.Remove(gStringBuilder.Length - 4, 4); // remove last " OR "

                return gStringBuilder.ToString();

            }

            public static string GetForString(string forString)
            {
                // lacks implementation for date with "."(12.12.1995.)

                // '31/12/94' -> '1994-12-31'
                Regex rgx = new Regex(@"'\d{2}/\d{2}/\d{2}'");
                MatchCollection matColl = rgx.Matches(forString);

                foreach (var item in matColl)
                {
                    Match match = (Match)item;
                    var date = match.ToString().Trim('\'');
                    var parts = date.Split('/');
                    var newDate = $"{parts[2]}-{parts[1]}-{parts[0]}";

                    string sqlDate = "";
                    if (int.Parse(parts[2]) < 40) // fixed
                    {
                        sqlDate += "20";
                    }
                    else
                    {
                        sqlDate += "19";
                    }
                    sqlDate += newDate;
                    forString = forString.Replace(match.ToString(), $"'{sqlDate}'");
                }

                // '31/12/1994' -> '1994-12-31'
                Regex rgx2 = new Regex(@"'\d{2}/\d{2}/\d{4}'");
                MatchCollection matColl2 = rgx2.Matches(forString);

                foreach (var item in matColl2)
                {
                    Match match = (Match)item;
                    var date = match.ToString().Trim('\'');
                    var parts = date.Split('/');
                    var sqlDate = $"{parts[2]}-{parts[1]}-{parts[0]}";

                    forString = forString.Replace(match.ToString(), $"'{sqlDate}'");
                }

                // ' -> '' (because of sp_executesql)
                forString = forString.Replace("\'", "\'\'");

                return forString;
            }

            private void fillBufferFromCacheLine(CacheLine cacheLine)
            {
                thisOpenv.currentRow = cacheLine.id;

                foreach (var lineItem in cacheLine.Line)
                {
                    var currentColumnName = lineItem.Key;
                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                    {
                        
                        if (CSCS_GUI.DEFINES.TryGetValue(currentColumnName.ToLower(), out DefineVariable thisVar))
                        {
                            if(thisVar.DefType == "d")
                            {
                                KeyClass.KeyColumns[currentColumnName] = lineItem.Value.DateTime.ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                KeyClass.KeyColumns[currentColumnName] = lineItem.Value.ToString();
                            }
                        }
                    }

                    string fieldValue = lineItem.Value.ToString();

                    CSCS_GUI.DEFINES[currentColumnName.ToLower()].InitVariable(new Variable(fieldValue).Clone());
                    Gui.OnVariableChange(currentColumnName.ToLower(), new Variable(fieldValue), true);
                }
            }

            private Variable findNextOrPrevious(FindvOption option)
            {
                int currentSqlId = thisOpenv.currentRow;

                if(lastUsedPreviousOrNext.TryGetValue(tableHndlNum, out FindvOption lastOption) && lastOption == option)
                {
                    //if something is in Cache
                    if (thisOpenv.Cache.CachedLines.Count > 0)
                    {
                        var firstCachedLine = thisOpenv.Cache.CachedLines.First();

                        fillBufferFromCacheLine(firstCachedLine);

                        thisOpenv.Cache.CachedLines.Remove(firstCachedLine);

                        lastUsedPreviousOrNext[tableHndlNum] = option;

                        SetFlerr(0, tableHndlNum); // 0 means OK
                        return Variable.EmptyInstance;
                    }
                }
                else
                {
                    // option != lastOption ... direction changed
                    thisOpenv.Cache.numOfRowsToGet = 1;
                    thisOpenv.Cache.CachedLines = new List<CacheLine>();
                }
                

                int numOfRowsToSelect = thisOpenv.Cache.numOfRowsToGet;

                thisOpenv.Cache.numOfRowsToGet *= 3;
                if (thisOpenv.Cache.numOfRowsToGet > CSCS_GUI.MaxCacheSize)
                    thisOpenv.Cache.numOfRowsToGet = CSCS_GUI.MaxCacheSize;


                string query = "";

                if (false && lastUsedPreviousOrNext.TryGetValue(tableHndlNum, out FindvOption lastOption2) && lastOption2 == option && !string.IsNullOrEmpty(nextPrevCachedWhereStrings[tableHndlNum]))
                {
                    query = nextPrevCachedWhereStrings[tableHndlNum];
                }
                else
                {
                    string columns = "*";
                    if (!string.IsNullOrEmpty(cachedColumnsToSelect[tableHndlNum]))
                    {
                        columns = cachedColumnsToSelect[tableHndlNum];
                    }

                    string whereString = GetNextOrPreviousWhereStringWithParams(option);

                    string sqlForString = "";
                    if (!string.IsNullOrEmpty(cachedSqlForString[tableHndlNum]))
                    {
                        sqlForString = cachedSqlForString[tableHndlNum];
                    }

                    string orderByString = GetOrderByString(option, thisOpenv, KeyClass);

                    string paramsDeclaration = GetParametersDeclaration(KeyClass);

                    query =
        $@"EXECUTE sp_executesql N'
select top {numOfRowsToSelect}
{columns}
from {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName} 
with (nolock) 
where (
{whereString}
)
{(!string.IsNullOrEmpty(cachedSqlForString[tableHndlNum]) ? "AND (" + sqlForString + ")" : "")} 

order by {orderByString}
',
N'{paramsDeclaration}', ";

                    nextPrevCachedWhereStrings[tableHndlNum] = query;
                }


                string paramsValues = GetParametersValues();

                query += paramsValues;

                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            SetFlerr(3, tableHndlNum);
                            lastUsedPreviousOrNext[tableHndlNum] = option;
                            return Variable.EmptyInstance;
                        }
                        else
                        {
                            if (keyo == "keyo")
                            {
                                SetFlerr(0, tableHndlNum);
                                lastUsedPreviousOrNext[tableHndlNum] = option;
                                return Variable.EmptyInstance;
                            }

                            bool firstPass = true; // for buffer, first row outside cache
                                                        
                            while (reader.Read())
                            {
                                //if (firstPass)

                                Dictionary<string, DefineVariable> cacheLine = new Dictionary<string, DefineVariable>();

                                currentSqlId = (int)reader["ID"];

                                //cacheLine.Add("id", new DefineVariable());

                                int currentFieldNum = 1;
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);
                                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = ((DateTime)reader[currentColumnName]).ToString("yyyy-MM-dd");
                                        }
                                        else
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = reader[currentColumnName].ToString();
                                        }
                                    }

                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    //if (!cacheLine.ContainsKey(loweredCurrentColumnName))
                                    {
                                        lastUsedPreviousOrNext[tableHndlNum] = option;
                                        return new Variable((long)4);
                                    }
                                    else
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            DateTime fieldValue = (DateTime)reader[currentColumnName];
                                            //var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                            //var newVar = new Variable(fieldValue.ToString(dateFormat));

                                            cacheLine.Add(currentColumnName, CSCS_GUI.DEFINES[loweredCurrentColumnName]);
                                            cacheLine[currentColumnName].DateTime = fieldValue;
                                            //cacheLine[currentColumnName].InitVariable(newVar);
                                            
                                            //CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(newVar);
                                            //CSCS_GUI.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();

                                            cacheLine.Add(currentColumnName, CSCS_GUI.DEFINES[loweredCurrentColumnName]);
                                            cacheLine[currentColumnName].InitVariable(new Variable(fieldValue).Clone());

                                            //CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue).Clone());
                                            //CSCS_GUI.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                        }
                                    }

                                    currentFieldNum++;
                                }
                                thisOpenv.Cache.CachedLines.Add(new CacheLine() { id = currentSqlId, Line = CloneCacheLineDictionary(cacheLine) });

                                //firstPass = false;
                            }

                            //thisOpenv.currentRow = currentSqlId;
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                        }

                    }
                }

                lastUsedPreviousOrNext[tableHndlNum] = option;
                
                findNextOrPrevious(option);

                SetFlerr(0, tableHndlNum); // 0 means OK
                return Variable.EmptyInstance;
            }

            public static Dictionary<string, DefineVariable> CloneCacheLineDictionary(Dictionary<string, DefineVariable> original)
            {
                Dictionary<string, DefineVariable> dictCopy = new Dictionary<string, DefineVariable>();
                foreach (var item in original)
                {
                    dictCopy.Add((string)item.Key.Clone(), (DefineVariable)item.Value.Clone());
                }
                return dictCopy;
            }

            private Variable findNextOrPreviousWithoutCacheing(FindvOption option)
            {
                int currentSqlId = thisOpenv.currentRow;

                int numOfRowsToSelect = 1;

                string query = "";

                if (lastUsedPreviousOrNext.TryGetValue(tableHndlNum, out FindvOption lastOption) && lastOption == option && !string.IsNullOrEmpty(nextPrevCachedWhereStrings[tableHndlNum]))
                {
                    query = nextPrevCachedWhereStrings[tableHndlNum];
                }
                else
                {
                    string columns = "*";
                    if (!string.IsNullOrEmpty(cachedColumnsToSelect[tableHndlNum]))
                    {
                        columns = cachedColumnsToSelect[tableHndlNum];
                    }

                    string whereString = GetNextOrPreviousWhereStringWithParams(option);

                    string sqlForString = "";
                    if (!string.IsNullOrEmpty(cachedSqlForString[tableHndlNum]))
                    {
                        sqlForString = cachedSqlForString[tableHndlNum];
                    }

                    string orderByString = GetOrderByString(option, thisOpenv, KeyClass);

                    string paramsDeclaration = GetParametersDeclaration(KeyClass);

                    query =
        $@"EXECUTE sp_executesql N'
select top {numOfRowsToSelect}
{columns}
from {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName} 
with (nolock) 
where (
{whereString}
)
{(!string.IsNullOrEmpty(cachedSqlForString[tableHndlNum]) ? "AND (" + sqlForString + ")" : "")} 

order by {orderByString}
',
N'{paramsDeclaration}', ";

                    nextPrevCachedWhereStrings[tableHndlNum] = query;
                }


                string paramsValues = GetParametersValues();

                query += paramsValues;

                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            SetFlerr(3, tableHndlNum);
                            lastUsedPreviousOrNext[tableHndlNum] = option;
                            return Variable.EmptyInstance;
                        }
                        else
                        {
                            if (keyo == "keyo")
                            {
                                SetFlerr(0, tableHndlNum);
                                lastUsedPreviousOrNext[tableHndlNum] = option;
                                return Variable.EmptyInstance;
                            }

                            bool firstPass = true; // for buffer, first row outside cache

                            while (reader.Read())
                            {
                                Dictionary<string, DefineVariable> cacheLine = new Dictionary<string, DefineVariable>();

                                if (firstPass)
                                    currentSqlId = (int)reader["ID"];

                                int currentFieldNum = 1;
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);
                                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = ((DateTime)reader[currentColumnName]).ToString("yyyy-MM-dd");
                                        }
                                        else
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = reader[currentColumnName].ToString();
                                        }
                                    }

                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    {
                                        lastUsedPreviousOrNext[tableHndlNum] = option;
                                        return new Variable((long)4);
                                    }
                                    else
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            DateTime fieldValue = (DateTime)reader[currentColumnName];
                                            var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                            var newVar = new Variable(fieldValue.ToString(dateFormat));
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(newVar);
                                            Gui.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue).Clone());
                                            Gui.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                        }
                                    }

                                    currentFieldNum++;
                                }

                                firstPass = false;
                            }

                            thisOpenv.currentRow = currentSqlId;
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                        }

                    }
                }

                lastUsedPreviousOrNext[tableHndlNum] = option;

                SetFlerr(0, tableHndlNum); // 0 means OK
                return Variable.EmptyInstance;
            }

            private Variable findMatchExact(FindvOption option)
            {
                nextPrevCachedWhereStrings[tableHndlNum] = null;
                cachedSqlForString[tableHndlNum] = null;
                cachedColumnsToSelect[tableHndlNum] = null;

                var matchExactValues = matchExactString.Split('|');
                if (matchExactValues.Count() != KeyClass.KeyColumns.Count())
                {
                    SetFlerr(99, tableHndlNum); // unequal number of key segments and key values
                    return Variable.EmptyInstance;
                }

                string columns = "*";
                if (!string.IsNullOrEmpty(columnsToSelect))
                {
                    columns = "ID, " + columnsToSelect;

                    var splittedColumns = columnsToSelect.Replace(" ", "").Split(',');
                    foreach (var segment in KeyClass.KeyColumns.Keys)
                    {
                        if (!splittedColumns.Any(p => p == segment))
                        {
                            columns += ", " + segment;
                        }
                    }
                    clearBuffer(thisOpenv);

                    cachedColumnsToSelect[tableHndlNum] = columns;
                }

                string sqlForString = "";
                if (!string.IsNullOrEmpty(forString))
                {
                    sqlForString = GetForString(forString);
                    cachedSqlForString[tableHndlNum] = sqlForString;
                }

                int currentSqlId = thisOpenv.currentRow;

                string orderByString = GetOrderByString(option, thisOpenv, KeyClass);

                string whereString = GetMatchExactWhereString(matchExactValues);

                var query =
$@"EXECUTE sp_executesql N'
select top 1
{columns}
from {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
with (nolock)
where (
{whereString}
)
{(!string.IsNullOrEmpty(sqlForString) ? "AND (" + sqlForString + ")" : "")} 
order by {orderByString}
'";
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            SetFlerr(3, tableHndlNum);
                            return Variable.EmptyInstance;
                        }
                        else
                        {
                            if (keyo == "keyo")
                            {
                                SetFlerr(0, tableHndlNum);
                                return Variable.EmptyInstance;
                            }

                            while (reader.Read())
                            {
                                currentSqlId = (int)reader["ID"];
                                int currentFieldNum = 1;
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);
                                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = ((DateTime)reader[currentColumnName]).ToString("yyyy-MM-dd");
                                        }
                                        else
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = reader[currentColumnName].ToString();
                                        }
                                    }

                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    {
                                        return new Variable((long)4);
                                    }
                                    else
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            DateTime fieldValue = (DateTime)reader[currentColumnName];
                                            var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                            var newVar = new Variable(fieldValue.ToString(dateFormat));
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(newVar);
                                            Gui.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue));
                                            Gui.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                        }
                                    }
                                    currentFieldNum++;
                                }

                            }

                            thisOpenv.CurrentKey = KeyClass;
                            thisOpenv.currentRow = currentSqlId;
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                            Btrieve.OPENVs[tableHndlNum].Cache = new CachingClass() { KeyName = KeyClass.KeyName };
                        }

                    }
                }

                SetFlerr(0, tableHndlNum);
                return Variable.EmptyInstance;
            }

            public Variable clearBuffer(OpenvTable thisOpenv)
            {
                foreach (var bufferField in thisOpenv.FieldNames)
                {
                    var field = bufferField.ToLower();
                    if (CSCS_GUI.DEFINES.ContainsKey(field))
                    {
                        CSCS_GUI.DEFINES[field].InitVariable(Variable.EmptyInstance);
                        Gui.OnVariableChange(field, Variable.EmptyInstance, true);
                    }
                }

                return Variable.EmptyInstance;
            }
            private Variable findGeneric(FindvOption option)
            {
                nextPrevCachedWhereStrings[tableHndlNum] = null;
                cachedSqlForString[tableHndlNum] = null;
                cachedColumnsToSelect[tableHndlNum] = null;

                var matchExactValues = matchExactString.Split('|');
                if (matchExactValues.Count() != KeyClass.KeyColumns.Count())
                {
                    SetFlerr(99, tableHndlNum);
                    return Variable.EmptyInstance;
                }

                string columns = "*";
                if (!string.IsNullOrEmpty(columnsToSelect))
                {
                    columns = "ID, " + columnsToSelect;

                    var splittedColumns = columnsToSelect.Replace(" ", "").Split(',');
                    foreach (var segment in KeyClass.KeyColumns.Keys)
                    {
                        if (!splittedColumns.Any(p => p == segment))
                        {
                            columns += ", " + segment;
                        }
                    }
                    clearBuffer(thisOpenv);

                    cachedColumnsToSelect[tableHndlNum] = columns;
                }

                string sqlForString = "";
                if (!string.IsNullOrEmpty(forString))
                {
                    sqlForString = GetForString(forString);
                    cachedSqlForString[tableHndlNum] = sqlForString;
                }

                int currentSqlId = thisOpenv.currentRow;

                string whereString = GetGenericWhereStringWithParams(KeyClass);

                string orderByString = GetOrderByString(option, thisOpenv, KeyClass);

                string paramsDeclaration = GetParametersDeclaration(KeyClass);

                var query =
$@"EXECUTE sp_executesql N'
select top 1
{columns}
from {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
with (nolock)
where (
{whereString}
)
{(!string.IsNullOrEmpty(sqlForString) ? "AND (" + sqlForString + ")" : "")} 
order by {orderByString}
',
N'{paramsDeclaration}', ";

                string paramsValues = GetGenericParametersValues(KeyClass, matchExactValues);

                query += paramsValues;

                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            SetFlerr(3, tableHndlNum);
                            return Variable.EmptyInstance;
                        }
                        else
                        {
                            if (keyo == "keyo")
                            {
                                SetFlerr(0, tableHndlNum);
                                return Variable.EmptyInstance;
                            }

                            while (reader.Read())
                            {
                                currentSqlId = (int)reader["ID"];
                                int currentFieldNum = 1;
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);
                                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = ((DateTime)reader[currentColumnName]).ToString("yyyy-MM-dd");
                                        }
                                        else
                                        {
                                            KeyClass.KeyColumns[currentColumnName] = reader[currentColumnName].ToString();
                                        }
                                    }

                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    {
                                        return new Variable((long)4);
                                    }
                                    else
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            DateTime fieldValue = (DateTime)reader[currentColumnName];
                                            var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                            var newVar = new Variable(fieldValue.ToString(dateFormat));
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(newVar);
                                            Gui.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue).Clone());
                                            Gui.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                        }
                                    }
                                    currentFieldNum++;
                                }

                            }

                            thisOpenv.CurrentKey = KeyClass;
                            thisOpenv.currentRow = currentSqlId;
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                            Btrieve.OPENVs[tableHndlNum].Cache = new CachingClass() { KeyName = KeyClass.KeyName };
                        }

                    }
                }

                SetFlerr(0, tableHndlNum);
                return Variable.EmptyInstance;
            }
        }


        public class OpenvFunction : ParserFunction
        {
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 1, m_name);
                var tableName = Utils.GetSafeString(args, 0);
                var databaseName = Utils.GetSafeString(args, 1, CSCS_GUI.DefaultDB);
                var lockingType = Utils.GetSafeString(args, 2, "n").ToLower();

                return Btrieve.OPENV(tableName, databaseName, script, lockingType);
            }
        }

        public class ClosevFunction : ParserFunction
        {
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 1, m_name);
                var tableHndlNum = Utils.GetSafeInt(args, 0);

                return Btrieve.CLOSEV(tableHndlNum);
            }
        }

        public class FindvFunction : ParserFunction
        {
            int tableHndlNum;
            string operationType;
            string tableKey;
            OpenvTable thisOpenv;
            string matchExactValue;
            string forString;
            string columnsToSelect;
            string keyo;

            static KeyClass KeyClass;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);
                tableHndlNum = Utils.GetSafeInt(args, 0);
                operationType = Utils.GetSafeString(args, 1).ToLower(); // f/m/n/p/m/g 
                tableKey = Utils.GetSafeString(args, 2).ToLower(); // e.g. VEZM_RNALOGLIN
                matchExactValue = Utils.GetSafeString(args, 3); // e.g. 900000|2
                forString = Utils.GetSafeString(args, 4); // e.g. CUST_CODE = 12345
                columnsToSelect = Utils.GetSafeString(args, 5); // e.g. "VEZM_VEZNIBROJ, VEZM_RNALOG"
                keyo = Utils.GetSafeString(args, 6); // e.g. "VEZM_VEZNIBROJ, VEZM_RNALOG"

                new Btrieve.FINDVClass(tableHndlNum, operationType, tableKey, matchExactValue, forString, columnsToSelect, script, keyo).FINDV();

                return Variable.EmptyInstance;
            }
        }

        public class ClrFunction : ParserFunction
        {
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);
                int tableHndlNum = Utils.GetSafeInt(args, 0);
                string operationType = Utils.GetSafeString(args, 1).ToLower(); // buff -> buffer and recordNumber(ID)(thisOpenv.currentRow) / rec -> ONLY recordNumber

                Clear(tableHndlNum, operationType, script);

                return Variable.EmptyInstance;
            }

            public void Clear(int tableHndlNum, string operationType, ParsingScript script)
            {
                OpenvTable thisOpenv = Btrieve.OPENVs[tableHndlNum];

                if (operationType == "buff")
                {
                    //clear buffer
                    new Btrieve.FINDVClass(tableHndlNum, "x", "", "", "", "", script).clearBuffer(thisOpenv);
                }

                if (operationType == "buff" || operationType == "rec")
                {
                    //clear ID
                    thisOpenv.currentRow = 0;
                    Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                }
            }
        }

        public class RcnGetFunction : ParserFunction
        {
            int tableHndlNum;

            OpenvTable thisOpenv;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 1, m_name);
                tableHndlNum = Utils.GetSafeInt(args, 0);

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                return RcnGet(thisOpenv);
            }

            public Variable RcnGet(OpenvTable thisOpenv)
            {
                return new Variable((double)thisOpenv.currentRow);
            }
        }

        public class RcnSetFunction : ParserFunction
        {
            int tableHndlNum;
            int idNum;

            OpenvTable thisOpenv;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);
                tableHndlNum = Utils.GetSafeInt(args, 0);
                idNum = Utils.GetSafeInt(args, 1); // ID of record to be filled into buffer
                var gui = script.Context as CSCS_GUI;
                RcnSet(gui, tableHndlNum, idNum);

                return Variable.EmptyInstance;
            }

            public Variable RcnSet(CSCS_GUI gui, int tableHndlNum, int idNum)
            {
                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                int currentSqlId = 0;

                string query =
    $@"EXECUTE sp_executesql N'
Select top 1 * 
from {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}

WHERE ID = {idNum}
'";
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            //err: Record not found
                            SetFlerr(3, tableHndlNum);
                            //return new Variable((long)3);
                            return Variable.EmptyInstance;
                        }
                        else
                        {
                            while (reader.Read())
                            {
                                currentSqlId = (int)reader["ID"];
                                int currentFieldNum = 1;
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);

                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    {
                                        // err: this column is not opened in buffer via Openv
                                        return new Variable((long)4);
                                    }
                                    else
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            DateTime fieldValue = (DateTime)reader[currentColumnName];
                                            var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue.ToString(dateFormat)));
                                            gui.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue.ToString(dateFormat)), true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue));
                                            gui.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                        }
                                    }
                                    currentFieldNum++;
                                }
                            }

                            thisOpenv.currentRow = currentSqlId;
                            thisOpenv.CurrentKey = new KeyClass() { KeyName = "ID", Ascending = true, Unique = true, KeyNum = 0, KeyColumns = new Dictionary<string, string>() { { "ID", "" } } };
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;

                            Btrieve.FINDVClass.cachedColumnsToSelect[tableHndlNum] = null;
                            Btrieve.FINDVClass.cachedSqlForString[tableHndlNum] = null;
                            Btrieve.FINDVClass.nextPrevCachedWhereStrings[tableHndlNum] = null;
                        }
                    }
                }

                SetFlerr(0, tableHndlNum); // 0 means OK
                return Variable.EmptyInstance;
            }
        }

        public class ActiveFunction : ParserFunction
        {
            int tableHndlNum;
            OpenvTable thisOpenv;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 1, m_name);
                tableHndlNum = Utils.GetSafeInt(args, 0);


                thisOpenv = Btrieve.OPENVs[tableHndlNum];
                if (thisOpenv.currentRow > 0)
                    return new Variable(true);
                else
                {
                    return new Variable(false);
                }
            }
        }

        public class DelFunction : ParserFunction
        {
            int tableHndlNum;
            bool noPrompt;

            OpenvTable thisOpenv;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 1, m_name);
                tableHndlNum = Utils.GetSafeInt(args, 0);
                noPrompt = Utils.GetSafeVariable(args, 1, new Variable(false)).AsBool(); // true -> noPrompt, false -> prompt("are you sure...?")

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                if (!noPrompt)
                {
                    if (MessageBoxResult.No == MessageBox.Show("Are you sure you want to delete the current record?", "Caution", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    {
                        return Variable.EmptyInstance;
                    }
                }

                if (DeleteCurrentRecord())
                {
                    //clear buffer
                    new Btrieve.FINDVClass(tableHndlNum, "x", "", "", "", "", script).clearBuffer(thisOpenv);

                    //clear ID
                    thisOpenv.currentRow = 0;
                    Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                }

                return Variable.EmptyInstance;
            }

            private bool DeleteCurrentRecord()
            {
                string query =
    $@"EXECUTE sp_executesql N'
Delete from {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
WHERE ID = {thisOpenv.currentRow}
'";
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    var rez = cmd.ExecuteNonQuery();
                    if (rez == 1)
                    {
                        SetFlerr(0, tableHndlNum); // 0 means OK
                        return true;
                    }
                    else
                    {
                        SetFlerr(6); // err: not deleted
                        return false;
                    }
                }
            }
        }

        public class SaveFunction : ParserFunction
        {
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 1, m_name);
                int tableHndlNum = Utils.GetSafeInt(args, 0);
                bool noPrompt = Utils.GetSafeVariable(args, 1, new Variable(false)).AsBool(); // true -> noPrompt, false -> prompt("are you sure...?")
                bool noClr = Utils.GetSafeVariable(args, 2, new Variable(false)).AsBool(); //

                OpenvTable thisOpenv = Btrieve.OPENVs[tableHndlNum];

                Save(noPrompt, thisOpenv, tableHndlNum, noClr, script);

                return Variable.EmptyInstance;
            }

            public void Save(bool noPrompt, OpenvTable thisOpenv, int tableHndlNum, bool noClr, ParsingScript script)
            {
                bool clear = false;

                if (!noPrompt)
                {
                    if (MessageBoxResult.No == MessageBox.Show("Are you sure you want to save the current record?", "Caution", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    {
                        return;
                    }
                }
                if (thisOpenv.currentRow > 0)
                {
                    if (UpdateCurrentRecord(thisOpenv, tableHndlNum))
                        clear = true;
                }
                else
                {
                    if (InsertNewRecord(thisOpenv, tableHndlNum))
                        clear = true;
                }

                if (clear && !noClr)
                {
                    //clear buffer
                    new Btrieve.FINDVClass(tableHndlNum, "x", "", "", "", "", script).clearBuffer(thisOpenv);

                    //clear ID
                    thisOpenv.currentRow = 0;
                    Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                }
            }

            private bool InsertNewRecord(OpenvTable thisOpenv, int tableHndlNum)
            {
                StringBuilder valuesStringBuilder = new StringBuilder();

                foreach (var field in thisOpenv.FieldNames)
                {
                    valuesStringBuilder.AppendLine("");

                    var bufferVar = CSCS_GUI.DEFINES[field.ToLower()];
                    if (bufferVar.DefType == "a" || bufferVar.DefType == "t" || bufferVar.DefType == "d")
                    {
                        var bufferVarAsString = bufferVar.AsString();

                        if (bufferVar.DefType == "d")
                        {
                            bufferVarAsString = bufferVar.DateTime.ToString("yyyy-MM-dd");
                        }

                        valuesStringBuilder.Append("\'\'" + bufferVarAsString + "\'\'");
                    }
                    else if (bufferVar.DefType == "n")
                    {
                        valuesStringBuilder.Append(bufferVar.AsString().Replace(',', '.'));
                    }
                    else if (bufferVar.DefType == "l")
                    {
                        valuesStringBuilder.Append(bufferVar.AsDouble().ToString());
                    }
                    else
                    {
                        valuesStringBuilder.Append(bufferVar.AsString());
                    }

                    valuesStringBuilder.Append(", ");
                }
                valuesStringBuilder.Remove(valuesStringBuilder.Length - 2, 2); // remove last ", "

                string query =
        $@"EXECUTE sp_executesql N'
INSERT INTO {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
VALUES ({valuesStringBuilder})
'";
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    var rez = cmd.ExecuteNonQuery();
                    if (rez == 1)
                    {
                        SetFlerr(0, tableHndlNum); // 0 means OK
                        return true;
                    }
                    else
                    {
                        SetFlerr(6); // err: not inserted
                        return false;
                    }
                }
            }

            private bool UpdateCurrentRecord(OpenvTable thisOpenv, int tableHndlNum)
            {
                StringBuilder setStringBuilder = new StringBuilder();

                foreach (var field in thisOpenv.FieldNames)
                {
                    setStringBuilder.AppendLine("");
                    setStringBuilder.Append(field + " = ");

                    var bufferVar = CSCS_GUI.DEFINES[field.ToLower()];
                    if (bufferVar.DefType == "a" || bufferVar.DefType == "t" || bufferVar.DefType == "d")
                    {
                        var bufferVarAsString = bufferVar.AsString();

                        if (bufferVar.DefType == "d")
                        {
                            bufferVarAsString = bufferVar.DateTime.ToString("yyyy-MM-dd");
                        }

                        setStringBuilder.Append("\'\'" + bufferVarAsString + "\'\'");
                    }
                    else if (bufferVar.DefType == "n")
                    {
                        setStringBuilder.Append(bufferVar.AsString().Replace(',', '.'));
                    }
                    else if (bufferVar.DefType == "l")
                    {
                        setStringBuilder.Append(bufferVar.AsDouble().ToString());
                    }
                    else
                    {
                        setStringBuilder.Append(bufferVar.AsString());
                    }

                    setStringBuilder.Append(", ");
                }
                setStringBuilder.Remove(setStringBuilder.Length - 2, 2); // remove last ", "

                string query =
        $@"EXECUTE sp_executesql N'
Update {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
SET {setStringBuilder}
WHERE ID = {thisOpenv.currentRow}
'";
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    var rez = cmd.ExecuteNonQuery();
                    if (rez == 1)
                    {
                        SetFlerr(0, tableHndlNum); // 0 means OK
                        return true;
                    }
                    else
                    {
                        SetFlerr(6); // not updated
                        return false;
                    }
                }

            }
        }

        public class RDAFunction : ParserFunction
        {

            ParsingScript from;
            string to;
            int tableHndlNum;

            string tableKey;


            string startString;
            ParsingScript whileString;

            ParsingScript forString;

            string scopeString;
            string cntrNameString;

            OpenvTable thisOpenv;
            KeyClass KeyClass;

            ParsingScript Script;

            double rowsAffected = 0;

            int rowNumber = 0;

            protected override Variable Evaluate(ParsingScript script)
            {
                Script = script;
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 6, m_name);

                from = script.GetTempScript(args[0].ToString()); // cols from DB
                to = Utils.GetSafeString(args, 1); // arrays to fill

                tableHndlNum = Utils.GetSafeInt(args, 2);

                tableKey = Utils.GetSafeString(args, 3).ToLower();

                startString = Utils.GetSafeString(args, 4);
                whileString = script.GetTempScript(args[5].ToString());

                forString = script.GetTempScript(args[6].ToString());

                scopeString = Utils.GetSafeString(args, 7).ToLower();
                cntrNameString = Utils.GetSafeString(args, 8).ToLower();

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                if (!string.IsNullOrEmpty(tableKey))
                {
                    if (tableKey.StartsWith("@") && int.TryParse(tableKey.TrimStart('@'), out int keyNum))
                    {
                        if (keyNum > 0)
                        {
                            var kljuceviTable = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_SCHEMA == CSCS_GUI.Adictionary.SY_TABLESList.First(r => r.SYCT_NAME == thisOpenv.tableName.ToUpper()).SYCT_SCHEMA).OrderBy(s => s.SYKI_KEYNUM).ToArray();

                            KeyClass = thisOpenv.Keys.First(p => p.KeyName == kljuceviTable.Where(r => r.SYKI_KEYNUM == keyNum).First().SYKI_KEYNAME);
                        }
                        else
                        {
                            KeyClass = new KeyClass() { KeyName = "ID", Ascending = true, Unique = true, KeyNum = 0, KeyColumns = new Dictionary<string, string>() { { "ID", "" } } };
                        }
                    }
                    else if (!thisOpenv.Keys.Any(p => p.KeyName.ToUpper() == tableKey.ToUpper()) /* or the key with this number does not exist */)
                    {
                        // "Key does not exist for this table!"
                        SetFlerr(4, tableHndlNum);
                        return Variable.EmptyInstance;
                    }
                    else
                    {
                        KeyClass = thisOpenv.Keys.First(p => p.KeyName.ToUpper() == tableKey.ToUpper());
                    }
                }
                else
                {
                    KeyClass = thisOpenv.CurrentKey;
                }


                if (!string.IsNullOrEmpty(startString))
                {
                    // if has start string
                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, startString, "", "", script).FINDV();
                }
                else
                {
                    // doesn't have start string
                    string currentStart = "";

                    if (KeyClass.KeyNum != 0)
                    {
                        var segmentsOrdered = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToArray();

                        for (int i = 0; i < segmentsOrdered.Count(); i++)
                        {
                            if (CSCS_GUI.DEFINES.TryGetValue(segmentsOrdered[i].ToLower(), out DefineVariable bufferVar))
                            {
                                currentStart += bufferVar.AsString();
                                currentStart += "|";
                            }
                        }
                        currentStart = currentStart.TrimEnd('|');
                    }
                    else
                    {
                        // key @0
                        currentStart = thisOpenv.currentRow.ToString();
                    }

                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, currentStart, "", "", script).FINDV();
                }

                bool limited = false;
                int selectLimit = 0;
                int selectLimitCounter = 0;
                if (scopeString.StartsWith("n"))
                {
                    var selectLimitString = scopeString.TrimStart('n').Replace(" ", "");
                    if (int.TryParse(selectLimitString, out selectLimit))
                    {
                        limited = true;
                        selectLimitCounter = selectLimit;
                    }
                    else
                    {
                        SetFlerr(12, tableHndlNum);
                        return Variable.EmptyInstance;
                    }
                }

                bool whileIsSet = false;
                if (!string.IsNullOrEmpty(whileString.String))
                    whileIsSet = true;

                bool forIsSet = false;
                if (!string.IsNullOrEmpty(forString.String))
                    forIsSet = true;

                while (!whileIsSet || script.GetTempScript(whileString.String).Execute(new char[] { '"' }, 0).AsBool())
                {
                    if (forIsSet && !script.GetTempScript(forString.String).Execute(new char[] { '"' }, 0).AsBool())
                    {
                        new Btrieve.FINDVClass(tableHndlNum, "n", "", "", "", "", script).FINDV();
                        continue;
                    }


                    if (limited && selectLimitCounter == 0)
                    {
                        SetFlerr(0, tableHndlNum);
                        break;
                    }


                    if (RDA())
                    {
                        rowsAffected++;
                        rowNumber++;
                    }

                    if (limited)
                    {
                        selectLimitCounter--;
                        if (selectLimitCounter == 0)
                        {
                            SetFlerr(0, tableHndlNum);
                            break;
                        }
                    }

                    new Btrieve.FINDVClass(tableHndlNum, "n", "", "", "", "", script).FINDV();
                    if (LastFlerrsOfFnums[tableHndlNum] == 3)
                    {
                        SetFlerr(0, tableHndlNum);
                        break;
                    }
                }

                var current = CSCS_GUI.DEFINES[cntrNameString];
                current.InitVariable(new Variable(rowsAffected));

                return Variable.EmptyInstance;
            }

            private bool RDA()
            {
                var fromSplitted = from.String.Split('|');

                List<Variable> executed = new List<Variable>();

                foreach (var item in fromSplitted)
                {
                    executed.Add(Script.GetTempScript(item).Execute(null, 0));
                }

                to = to.Replace(" ", "");
                var toSplitted = to.Split(',');

                for (int i = 0; i < toSplitted.Length; i++)
                {
                    var arrayName = toSplitted[i].ToLower();
                    if (CSCS_GUI.DEFINES[arrayName].Array > 1)
                    {
                        CSCS_GUI.DEFINES[arrayName].Tuple[rowNumber] = executed.ElementAt(i).Clone();
                    }
                }


                return true;
            }
        }

        //not fully implemented
        public class WRTAFunction : ParserFunction
        {
            Variable from;
            //string from;
            //string to;
            Variable to;
            int tableHndlNum;

            string tableKey;

            ParsingScript forString;

            string cntrNameString;

            int arrayIndex = 0;

            OpenvTable thisOpenv;
            KeyClass KeyClass;

            ParsingScript Script;

            double rowsAffected = 0; //

            int rowNumber = 0; //

            string recaArrayName;

            private bool areTypesCompatible(string TASType, Variable.VarType CSCSType)
            {
                bool incompatible = false;

                switch (CSCSType)
                {
                    case Variable.VarType.NUMBER:
                        if (TASType != "N" && TASType != "I" && TASType != "R" && TASType != "B")
                            incompatible = true;
                        break;

                    case Variable.VarType.STRING:
                        if (TASType != "A")
                            incompatible = true;
                        break;

                    case Variable.VarType.DATETIME:
                        if (TASType != "D" && TASType != "T")
                            incompatible = true;
                        break;

                    default:
                        return false;
                }

                if (incompatible)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            private bool checkCompatibility()
            {
                var adictDB = CSCS_GUI.Adictionary.SY_DATABASESList.Where(p => p.SYCD_USERCODE.ToLower() == thisOpenv.databaseName).FirstOrDefault();
                if(adictDB != null)
                {
                    var adictTable = CSCS_GUI.Adictionary.SY_TABLESList.Where(p => p.SYCT_USERCODE.ToLower() == adictDB.SYCD_USERCODE.ToLower() && thisOpenv.tableName == p.SYCT_NAME.ToLower()).FirstOrDefault();
                    if(adictTable != null)
                    {
                        var adictFields = CSCS_GUI.Adictionary.SY_FIELDSList.Where(p=>p.SYTD_SCHEMA == adictTable.SYCT_SCHEMA).ToList();

                        bool incompatible = false;
                        for (int i = 0; i < to.Tuple.Count; i++) 
                        {
                            string currColumnType = null;
                            Variable.VarType currArrayType;

                            var currColumnName = to.Tuple[i].AsString();
                            var adictField = adictFields.Where(p=>p.SYTD_FIELD.ToLower() == currColumnName).FirstOrDefault();
                            if(adictField != null)
                            {
                                currColumnType = adictField.SYTD_TYPE;
                            }

                            

                            string fromTypeData = Utils.ConvertToScript(InterpreterInstance, from.Tuple[i].AsString(), out _);
                            var fromTmpScript = Script.GetTempScript(fromTypeData);
                            var fromExecuted = fromTmpScript.Execute();
                            currArrayType = fromExecuted.Type;

                            switch (currArrayType)
                            {
                                
                                case Variable.VarType.NUMBER:
                                case Variable.VarType.STRING:
                                case Variable.VarType.DATETIME:
                                    if(!areTypesCompatible(currColumnType, currArrayType))
                                    {
                                        return false;
                                    }
                                    break;
                                case Variable.VarType.ARRAY:
                                    if (!areTypesCompatible(currColumnType, fromExecuted.Tuple[0].Type))
                                    {
                                        return false;
                                    }
                                    break;
                                default:
                                    return false;
                            }
                        }

                        if (incompatible)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                        
                    }
                }
                return false;

            }

            protected override Variable Evaluate(ParsingScript script)
            {
                Script = script;
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 6, m_name);

                from = script.GetTempScript(args[0].ToString()).Execute(); // array names

                //string data2 = Utils.ConvertToScript(InterpreterInstance, args[0].ToString(), out _);
                //var fromTmpScript2 = script.GetTempScript(data2);
                //var from2 = fromTmpScript2.Execute();

                //from = Utils.GetSafeString(args, 0); // array names
                to = script.GetTempScript(args[1].ToString()).Execute(); // DB table columns to fill

                tableHndlNum = Utils.GetSafeInt(args, 2);

                recaArrayName = Utils.GetSafeString(args, 3);
                var maxa = Utils.GetSafeInt(args, 4);

                forString = script.GetTempScript(args[5].ToString()); // 
                cntrNameString = Utils.GetSafeString(args, 6).ToLower(); //

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                //check from and to array count, must be the same
                if(from.Tuple.Count != to.Tuple.Count)
                {
                    MessageBox.Show("Error: Array count and table column count is not the same.");
                    return Variable.EmptyInstance;
                }

                //check from and to data types compatibility
                if (!checkCompatibility())
                {
                    MessageBox.Show("Error: \"from\" arrays\' and \"to\" columns\' doesn\'t match data types.");
                    return Variable.EmptyInstance;
                }

                bool limited = false;
                int selectLimit = 0;
                int selectLimitCounter = 0;
                if (maxa != 0)
                {
                    selectLimit = maxa;
                    limited = true;
                    selectLimitCounter = selectLimit;
                }

                ParsingScript tmpScript = null;
                bool forIsSet = false;
                if (!string.IsNullOrEmpty(forString.String))
                {
                    forIsSet = true;
                    string data = Utils.ConvertToScript(InterpreterInstance, forString.String, out _);
                    tmpScript = script.GetTempScript(data);
                }

                while (true)
                {
                    if (limited && selectLimitCounter == 0)
                    {
                        SetFlerr(0, tableHndlNum);
                        break;
                    }


                    if (forIsSet /*&& !script.GetTempScript(forString.String).Execute(new char[] { '"' }, 0).AsBool()*/)
                    {
                        var res = tmpScript.Execute(new char[] { '"' }, 0);
                        var boolRes = res.AsBool();
                        if (!boolRes) 
                        {
                            arrayIndex++;

                            rowsAffected++;
                            rowNumber++;

                            var current1 = CSCS_GUI.DEFINES[cntrNameString];
                            current1.InitVariable(new Variable(rowsAffected));

                            if (limited)
                            {
                                selectLimitCounter--;
                                if (selectLimitCounter == 0)
                                {
                                    SetFlerr(0, tableHndlNum);
                                    break;
                                }
                            }

                            continue;
                        }
                    }

                    if (WRTA(script))
                    {
                        rowsAffected++;
                        rowNumber++;
                    }

                    if (limited)
                    {
                        selectLimitCounter--;
                        if (selectLimitCounter == 0)
                        {
                            SetFlerr(0, tableHndlNum);
                            break;
                        }
                    }

                    arrayIndex++;

                    var current = CSCS_GUI.DEFINES[cntrNameString];
                    current.InitVariable(new Variable(rowsAffected));
                }

                

                return Variable.EmptyInstance;
            }

            private bool WRTA(ParsingScript script)
            {
                bool shouldInsert = false;
                bool shouldUpdate = false;

                DefineVariable recaArrayDefVar;

                string currentId = null;

                if (CSCS_GUI.DEFINES.TryGetValue(recaArrayName.ToLower(), out recaArrayDefVar))
                {
                    currentId = recaArrayDefVar.Tuple[arrayIndex].AsString();
                    var query =
$@"EXECUTE sp_executesql N'
SELECT COUNT(*) FROM {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
WHERE id = {currentId}
'";

                    using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                    {
                        var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            if((int)reader[0] > 0)
                            {
                                //update -> because id EXISTS
                                //RECA IS set --> UPDATE
                                shouldUpdate = true;
                                reader.Close();
                            }
                            else
                            {
                                //insert -> id DOESN'T exist
                                shouldInsert = true;
                                reader.Close();
                            }
                        }
                    }
                }
                if (shouldUpdate)
                {
                    List<string> columns = new List<string>();
                    List<string> values = new List<string>();

                    foreach (var fromItem in from.Tuple)
                    {
                        if (fromItem.Type == Variable.VarType.ARRAY)
                        {
                            bool notANumber = fromItem.Tuple[arrayIndex].Type != Variable.VarType.NUMBER;

                            var valueString = "";
                            valueString += notANumber ? "\'\'" : "";
                            valueString += fromItem.Tuple[arrayIndex];
                            valueString += notANumber ? "\'\'" : "";
                            values.Add(valueString);
                        }
                        else if (fromItem.Type == Variable.VarType.STRING)
                        {
                            var evaluated = fromItem.AsString();

                            string data = Utils.ConvertToScript(InterpreterInstance, evaluated, out _);
                            var tmpScript = script.GetTempScript(data);
                            var res = tmpScript.Execute();

                            var valueString = "";
                            valueString += res.Type == Variable.VarType.NUMBER ? res.AsString() : "\'\'" + res.AsString() + "\'\'";
                            values.Add(valueString);
                        }
                        else if (fromItem.Type == Variable.VarType.NUMBER)
                        {
                            var evaluated = fromItem.AsString();

                            var valueString = "";
                            valueString += fromItem.Value;
                            values.Add(valueString);
                        }
                    }

                    foreach (var columnName in to.Tuple)
                    {
                        string columnString = columnName.AsString();
                        columns.Add(columnString);
                    }

                    string columnsEqualsValuesString = "";

                    for (int i = 0; i < columns.Count; i++)
                    {
                        columnsEqualsValuesString += columns[i] + " = " + values[i] + ", ";
                    }
                    columnsEqualsValuesString = columnsEqualsValuesString.Substring(0, columnsEqualsValuesString.Length - 2);


                    var query =
$@"EXECUTE sp_executesql N'
UPDATE {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
SET {columnsEqualsValuesString}
WHERE id = {currentId}
'";

                    using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                    {
                        try
                        {
                            var reader = cmd.ExecuteNonQuery();
                            
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message + "\n" + "ID = " + currentId);
                        }
                        return true;
                    }
                }

                if (string.IsNullOrEmpty(recaArrayName) || shouldInsert)
                {//RECA ISN'T set -> INSERT all ... OR id from RECAS array DOESN't EXIST in database table

                    string columnsString = "";
                    string valuesString = "";

                    foreach (var fromItem in from.Tuple)
                    {
                        if(fromItem.Type == Variable.VarType.ARRAY)
                        {
                            bool notANumber = fromItem.Tuple[arrayIndex].Type != Variable.VarType.NUMBER;

                            valuesString += notANumber ? "\'\'" : "";
                            valuesString += fromItem.Tuple[arrayIndex];
                            valuesString += notANumber ? "\'\'" : "";
                            valuesString += ", ";
                        }
                        else if (fromItem.Type == Variable.VarType.STRING)
                        {
                            var evaluated = fromItem.AsString();

                            string data = Utils.ConvertToScript(InterpreterInstance, evaluated, out _);
                            var tmpScript = script.GetTempScript(data);
                            var res = tmpScript.Execute();

                            valuesString += res.Type == Variable.VarType.NUMBER ? res.AsString() : "\'\'" + res.AsString() + "\'\'";
                            valuesString += ", ";
                        }
                        else if (fromItem.Type == Variable.VarType.NUMBER)
                        {
                            var evaluated = fromItem.AsString();

                            valuesString += fromItem.Value;
                            valuesString += ", ";
                        }
                    }
                    valuesString = valuesString.Substring(0, valuesString.Length - 2);

                    foreach (var columnName in to.Tuple)
                    {
                        columnsString += columnName.AsString();
                        columnsString += ", ";
                    }
                    columnsString = columnsString.Substring(0, columnsString.Length - 2);


                    var query =
$@"EXECUTE sp_executesql N'
INSERT INTO {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
({columnsString})
VALUES
({valuesString})
'";

                    using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                    {
                        try
                        {
                            var reader = cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        return true;
                    }
                }
                    
                return true;
            }
        }

        public class TRCFunction : ParserFunction
        {
            OpenvTable thisOpenv;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 1, m_name);

                int tableHndlNum = Utils.GetSafeInt(args, 0);

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                int numOfRecords;
                try
                {
                    var query =
$@"EXECUTE sp_executesql N'
SELECT COUNT(*) as numOfRows FROM {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
'";

                    using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                return new Variable((int)reader["numOfRows"]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }

                return Variable.EmptyInstance;
            }
        }

        public class ReplFunction : ParserFunction
        {
            int tableHndlNum;
            string columnsString;
            string withString;
            string tableKey;


            string startString;
            ParsingScript whileString;

            ParsingScript forString;

            string scopeString;
            string cntrNameString;

            OpenvTable thisOpenv;
            KeyClass KeyClass;

            double rowsAffected = 0;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 4, m_name);

                var gui = script.Context as CSCS_GUI;
                tableHndlNum = Utils.GetSafeInt(args, 0);
                columnsString = Utils.GetSafeString(args, 1); // columns to replace
                withString = Utils.GetSafeString(args, 2); // replace values
                tableKey = Utils.GetSafeString(args, 3).ToLower();

                startString = Utils.GetSafeString(args, 4);
                whileString = script.GetTempScript(args[5].ToString());

                forString = script.GetTempScript(args[6].ToString());

                scopeString = Utils.GetSafeString(args, 7).ToLower();
                cntrNameString = Utils.GetSafeString(args, 8).ToLower();

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                if (!string.IsNullOrEmpty(tableKey))
                {
                    if (tableKey.StartsWith("@") && int.TryParse(tableKey.TrimStart('@'), out int keyNum))
                    {
                        if (keyNum > 0)
                        {
                            var kljuceviTable = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_SCHEMA == CSCS_GUI.Adictionary.SY_TABLESList.First(r => r.SYCT_NAME == thisOpenv.tableName.ToUpper()).SYCT_SCHEMA).OrderBy(s => s.SYKI_KEYNUM).ToArray();

                            KeyClass = thisOpenv.Keys.First(p => p.KeyName == kljuceviTable.Where(r => r.SYKI_KEYNUM == keyNum).First().SYKI_KEYNAME);
                        }
                        else
                        {
                            KeyClass = new KeyClass() { KeyName = "ID", Ascending = true, Unique = true, KeyNum = 0, KeyColumns = new Dictionary<string, string>() { { "ID", "" } } };
                        }
                    }
                    else if (!thisOpenv.Keys.Any(p => p.KeyName == tableKey.ToUpper()))
                    {
                        // "Key does not exist for this table!"
                        SetFlerr(4, tableHndlNum);
                        return Variable.EmptyInstance;
                    }
                    else
                    {
                        KeyClass = thisOpenv.Keys.First(p => p.KeyName == tableKey.ToUpper());
                    }
                }
                else
                {
                    KeyClass = thisOpenv.CurrentKey;
                }

                if (!string.IsNullOrEmpty(startString))
                {
                    //if has a start string
                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, startString/*, forString.String*/, "", "", script).FINDV();
                }
                else
                {
                    //doesn't nave a start string

                    string currentStart = "";

                    if (KeyClass.KeyNum != 0)
                    {
                        var segmentsOrdered = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToArray();

                        for (int i = 0; i < segmentsOrdered.Count(); i++)
                        {
                            if (CSCS_GUI.DEFINES.TryGetValue(segmentsOrdered[i].ToLower(), out DefineVariable bufferVar))
                            {
                                currentStart += bufferVar.AsString();
                                currentStart += "|";
                            }
                        }
                        currentStart = currentStart.TrimEnd('|');
                    }
                    else
                    {
                        // key @0
                        currentStart = thisOpenv.currentRow.ToString();
                    }


                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, currentStart, "", "", script).FINDV();
                }

                bool limited = false;
                int selectLimit = 0;
                int selectLimitCounter = 0;
                if (scopeString.StartsWith("n"))
                {
                    var selectLimitString = scopeString.TrimStart('n').Replace(" ", "");
                    if (int.TryParse(selectLimitString, out selectLimit))
                    {
                        limited = true;
                        selectLimitCounter = selectLimit;
                    }
                    else
                    {
                        SetFlerr(12, tableHndlNum);
                        return Variable.EmptyInstance;
                    }
                }

                bool whileIsSet = false;
                if (!string.IsNullOrEmpty(whileString.String))
                    whileIsSet = true;

                bool forIsSet = false;
                if (!string.IsNullOrEmpty(forString.String))
                    forIsSet = true;

                var whileQoutsReplaced = whileString.String;

                while (!whileIsSet || script.GetTempScript(whileQoutsReplaced).Execute(new char[] { '"' }, 0).AsBool())
                {
                    if (forIsSet && !script.GetTempScript(forString.String).Execute(new char[] { '"' }, 0).AsBool())
                    {
                        new Btrieve.FINDVClass(tableHndlNum, "n", "", "", "", "", script).FINDV();
                        if (LastFlerrsOfFnums[tableHndlNum] == 3)
                        {
                            SetFlerr(0, tableHndlNum);
                            break;
                        }
                        continue;
                    }

                    if (limited && selectLimitCounter == 0)
                    {
                        SetFlerr(0, tableHndlNum);
                        break;
                    }

                    REPL();

                    if (limited)
                    {
                        selectLimitCounter--;
                        if (selectLimitCounter == 0)
                        {
                            SetFlerr(0, tableHndlNum);
                            break;
                        }
                    }

                    new Btrieve.FINDVClass(tableHndlNum, "n", "", "", "", "", script).FINDV();
                    if (LastFlerrsOfFnums[tableHndlNum] == 3)
                    {
                        SetFlerr(0, tableHndlNum);
                        break;
                    }
                }

                if (CSCS_GUI.DEFINES.TryGetValue(cntrNameString, out DefineVariable currentDefVar))
                {
                    currentDefVar.InitVariable(new Variable(rowsAffected));
                    gui.OnVariableChange(cntrNameString, new Variable(rowsAffected), true);
                }

                return Variable.EmptyInstance;
            }

            private Variable REPL()
            {
                var columnsParts = columnsString.Replace(" ", "").Split(',');
                var withParts = withString.Replace(" ", "").Split(',');

                if (columnsParts.Length != withParts.Length)
                {
                    SetFlerr(78, tableHndlNum);
                    return Variable.EmptyInstance;
                }

                StringBuilder setStringBuilder = new StringBuilder();
                for (int i = 0; i < columnsParts.Length; i++)
                {
                    setStringBuilder.Append(columnsParts[i] + " = " + withParts[i].Replace("\'", "\'\'"));
                    setStringBuilder.Append(", ");
                }
                setStringBuilder.Remove(setStringBuilder.Length - 2, 2); // remove last ", "

                string query =
        $@"EXECUTE sp_executesql N'
Update {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
SET {setStringBuilder}
WHERE ID = {thisOpenv.currentRow}
'";
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    int rows = cmd.ExecuteNonQuery();
                    rowsAffected += rows;
                }

                SetFlerr(0, tableHndlNum); // 0 means OK
                return Variable.EmptyInstance;
            }
        }


        static Dictionary<string, DataTable> gridsDataTables = new Dictionary<string, DataTable>(); // <gridName, DataTable>
        static Dictionary<string, OpenvTable> gridsOpenvs = new Dictionary<string, OpenvTable>(); // <gridName, OpenvTable>

        public class DisplayTableSetupFunction : ParserFunction
        {
            string gridName;

            int tableHndlNum;

            string tableKey;

            //string startString;
            //string whileString;

            //string forString;

            OpenvTable thisOpenv;
            KeyClass KeyClass;

            ParsingScript Script;
            CSCS_GUI Gui;

            //DataTable gridSource;
            Dictionary<string, string> tagsAndHeaders;
            Dictionary<string, Type> tagsAndTypes;
            Dictionary<string, Type> newTagsAndTypes;
            Dictionary<string, int> timeAndDateEditerTagsAndSizes = new Dictionary<string, int>();

            DataGrid dg;
            protected override Variable Evaluate(ParsingScript script)
            {
                Script = script;
                Gui = script.Context as CSCS_GUI;
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 3, m_name);

                gridName = Utils.GetSafeString(args, 0).ToLower();
                tableHndlNum = Utils.GetSafeInt(args, 1);
                tableKey = Utils.GetSafeString(args, 2);

                //startString = Utils.GetSafeString(args, 3);
                //whileString = Utils.GetSafeString(args, 4).ToLower();
                //forString = Utils.GetSafeString(args, 5).ToLower();

                gridsTableClass[gridName] = new DisplayTableClass();
                gridsTableClass[gridName].startString = script.GetTempScript(args[3].AsString()); //Utils.GetSafeString(args, 3);
                gridsTableClass[gridName].whileString = script.GetTempScript(args[4].AsString()); //Utils.GetSafeString(args, 4).ToLower();
                gridsTableClass[gridName].forString = Utils.GetSafeString(args, 5).ToLower();

                //------------------------------------------------------------------------

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                gridsOpenvs[gridName.ToLower()] = thisOpenv;

                if (!string.IsNullOrEmpty(tableKey))
                {
                    if (tableKey.StartsWith("@") && int.TryParse(tableKey.TrimStart('@'), out int keyNum))
                    {
                        if (keyNum > 0)
                        {
                            var kljuceviTable = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_SCHEMA == CSCS_GUI.Adictionary.SY_TABLESList.First(r => r.SYCT_NAME.ToUpper() == thisOpenv.tableName.ToUpper()).SYCT_SCHEMA).OrderBy(s => s.SYKI_KEYNUM).ToArray();

                            KeyClass = thisOpenv.Keys.First(p => p.KeyName == kljuceviTable.Where(r => r.SYKI_KEYNUM == keyNum).First().SYKI_KEYNAME);
                        }
                        else
                        {
                            KeyClass = new KeyClass() { KeyName = "ID", Ascending = true, Unique = true, KeyNum = 0, KeyColumns = new Dictionary<string, string>() { { "ID", "" } } };
                        }
                    }
                    else if (!thisOpenv.Keys.Any(p => p.KeyName.ToUpper() == tableKey.ToUpper()))
                    {
                        // "Key does not exist for this table!"
                        SetFlerr(4, tableHndlNum);
                        return Variable.EmptyInstance;
                    }
                    else
                    {
                        KeyClass = thisOpenv.Keys.First(p => p.KeyName.ToUpper() == tableKey.ToUpper());
                    }
                }
                else
                {
                    KeyClass = thisOpenv.CurrentKey;
                }

                //------------------------------------------------------

                dg = Gui.GetWidget(gridName) as DataGrid;
                if (dg == null)
                {
                    return Variable.EmptyInstance;
                }

                //tagsAndTypes = new Dictionary<string, Type>();
                //tagsAndHeaders = new Dictionary<string, string>();

                if (tagsAndTypes == null)
                    tagsAndTypes = new Dictionary<string, Type>();
                if (tagsAndHeaders == null)
                    tagsAndHeaders = new Dictionary<string, string>();

                var columns = dg.Columns;
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = dg.Columns.ElementAt(i);

                    if (column is DataGridTemplateColumn)
                    {
                        var dgtc = column as DataGridTemplateColumn;

                        var cell = dgtc.CellTemplate.LoadContent();
                        if (cell is TimeEditer)
                        {
                            var te = cell as TimeEditer;
                            if (te.Tag != null)
                            {
                                tagsAndTypes.Add(te.Tag.ToString(), typeof(TimeSpan));
                                tagsAndHeaders.Add(te.Tag.ToString(), dgtc.Header.ToString());
                                timeAndDateEditerTagsAndSizes[te.Tag.ToString()] = te.DisplaySize;
                            }
                        }
                        else if (cell is DateEditer)
                        {
                            var de = cell as DateEditer;
                            if (de.Tag != null)
                            {
                                tagsAndTypes.Add(de.Tag.ToString(), typeof(DateTime));
                                tagsAndHeaders.Add(de.Tag.ToString(), dgtc.Header.ToString());
                                timeAndDateEditerTagsAndSizes[de.Tag.ToString()] = de.DisplaySize;
                            }
                        }
                        else if (cell is CheckBox)
                        {
                            var cb = cell as CheckBox;
                            if (cb.Tag != null)
                            {
                                tagsAndTypes.Add(cb.Tag.ToString(), typeof(bool));
                                tagsAndHeaders.Add(cb.Tag.ToString(), dgtc.Header.ToString());
                            }
                        }
                        else if (cell is TextBox)
                        {

                            var tb = cell as TextBox;
                            if (tb.Tag != null)
                            {
                                tagsAndTypes.Add(tb.Tag.ToString(), typeof(string));
                                tagsAndHeaders.Add(tb.Tag.ToString(), dgtc.Header.ToString());
                            }
                        }
                    }
                }

                gridsDataTables[gridName] = new DataTable();
                var idColumn = new DataColumn();
                idColumn.DataType = System.Type.GetType("System.Int32");
                idColumn.ColumnName = "ID";
                idColumn.Caption = "ID";
                idColumn.ReadOnly = true;
                gridsDataTables[gridName].Columns.Add(idColumn);

                newTagsAndTypes = new Dictionary<string, Type>();
                foreach (var item in tagsAndTypes)
                {

                    var newColumn = new DataColumn();
                    newColumn.ColumnName = item.Key;
                    newColumn.DataType = item.Value;
                    var field = CSCS_GUI.Adictionary.SY_FIELDSList.Where(p => p.SYTD_SCHEMA.ToLower() == thisOpenv.tableName.ToLower() && p.SYTD_FIELD == item.Key).FirstOrDefault();
                    if (field != null)
                    {
                        switch (field.SYTD_TYPE)
                        {
                            case "B":
                            case "I":
                            case "R":
                                newColumn.DataType = typeof(int);
                                //tagsAndTypes[item.Key] = typeof(int);
                                newTagsAndTypes[item.Key] = typeof(int);
                                break;
                            case "N":
                                newColumn.DataType = typeof(double);
                                //tagsAndTypes[item.Key] = typeof(double);
                                newTagsAndTypes[item.Key] = typeof(double);
                                break;
                            case "A":
                                newColumn.DataType = typeof(string);
                                //tagsAndTypes[item.Key] = typeof(string);
                                newTagsAndTypes[item.Key] = typeof(string);
                                break;
                            case "L":
                                newColumn.DataType = typeof(bool);
                                //tagsAndTypes[item.Key] = typeof(bool);
                                newTagsAndTypes[item.Key] = typeof(bool);
                                break;
                            case "D":
                                newColumn.DataType = typeof(DateTime);
                                //tagsAndTypes[item.Key] = typeof(DateTime);
                                newTagsAndTypes[item.Key] = typeof(DateTime);
                                break;
                            case "T":
                                newColumn.DataType = typeof(TimeSpan);
                                //tagsAndTypes[item.Key] = typeof(TimeSpan);
                                newTagsAndTypes[item.Key] = typeof(TimeSpan);
                                break;
                            default:
                                break;
                        }
                    }


                    newColumn.Caption = tagsAndHeaders[item.Key];
                    gridsDataTables[gridName].Columns.Add(newColumn);
                }

                //StringBuilder selectSb = new StringBuilder();
                //selectSb.Append("ID, ");

                //foreach (var column in tagsAndTypes.Keys)
                //{
                //    selectSb.Append(column + ", ");
                //}
                //selectSb.Remove(selectSb.Length - 2, 2);

                ////---------------------------------------------------------------------------

                ////fillDataTable();

                ////---------------------------------------------------------------------------

                //dg.Items.Clear();
                dg.Columns.Clear();

                dg.AutoGenerateColumns = true;
                dg.AutoGeneratingColumn += DataGrid_OnAutoGeneratingColumn;

                dg.SelectionMode = DataGridSelectionMode.Single;
                dg.SelectionUnit = DataGridSelectionUnit.FullRow;

                dg.ItemsSource = gridsDataTables[gridName].AsDataView();
                //dg.ItemsSource = gridSource.DefaultView;

                //dg.SelectionChanged += Dg_SelectionChanged;
                dg.CellEditEnding += Dg_CellEditEnding;
                //dg.PreparingCellForEdit += Dg_PreparingCellForEdit;

                dg.RowEditEnding += Dg_RowEditEnding;

                dg.SelectedCellsChanged += Dg_SelectedCellsChanged;
                //-------------------------------------------------------------------------------

                //grids[gridName] = new DisplayArrayClass()
                //{
                //    dg = dg
                //};
                gridsTableClass[gridName].dg = dg;
                gridsTableClass[gridName].tableOrArray = "table";
                gridsTableClass[gridName].tags = tagsAndHeaders.Keys.ToList();
                gridsTableClass[gridName].tableHndlNum = tableHndlNum;
                gridsTableClass[gridName].tableKey = tableKey;
                gridsTableClass[gridName].tagsAndTypes = tagsAndTypes;
                gridsTableClass[gridName].KeyClass = KeyClass;
                gridsTableClass[gridName].Script = Script;

                fillDataTable(gridName);

                return Variable.EmptyInstance;
            }

            public void fillDataTable(string gridName)
            {
                gridsDataTables[gridName].Rows.Clear();

                var startString = gridsTableClass[gridName].startString;
                var tableHndlNum = gridsTableClass[gridName].tableHndlNum;
                var tableKey = gridsTableClass[gridName].tableKey;
                var KeyClass = gridsTableClass[gridName].KeyClass;
                var thisOpenv = OPENVs[gridsTableClass[gridName].tableHndlNum];

                var whileString = gridsTableClass[gridName].whileString;
                var forString = gridsTableClass[gridName].forString;

                var Script = gridsTableClass[gridName].Script;

                var tagsAndTypes = gridsTableClass[gridName].tagsAndTypes;

                if (!string.IsNullOrEmpty(startString.String))
                {
                    // if has start string
                    var startParts = startString.String.Split('|');
                    var startStringResult = "";
                    foreach (var startPart in startParts)
                    {
                        var part = Script.GetTempScript(startPart).Execute(null, 0).AsString();
                        startStringResult += part;
                        startStringResult += "|";
                    }
                    startStringResult = startStringResult.Remove(startStringResult.Length - 1, 1);

                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, startStringResult, "", "", Script).FINDV();
                }
                else
                {
                    // doesn't have start string
                    string currentStart = "";

                    if (KeyClass.KeyNum != 0)
                    {
                        var segmentsOrdered = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == KeyClass.KeyName).OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToArray();

                        for (int i = 0; i < segmentsOrdered.Count(); i++)
                        {
                            if (CSCS_GUI.DEFINES.TryGetValue(segmentsOrdered[i].ToLower(), out DefineVariable bufferVar))
                            {
                                currentStart += bufferVar.AsString();
                                currentStart += "|";
                            }
                        }
                        currentStart = currentStart.TrimEnd('|');
                    }
                    else
                    {
                        // key @0
                        currentStart = thisOpenv.currentRow.ToString();
                    }

                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, currentStart, "", "", Script).FINDV();
                }

                //---------------------------------------------------------------------------

                gridsDataTables[gridName].Rows.Clear();

                bool whileIsSet = false;
                if (!string.IsNullOrEmpty(whileString.String))
                    whileIsSet = true;

                bool forIsSet = false;
                if (!string.IsNullOrEmpty(forString))
                    forIsSet = true;

                while (!whileIsSet || Script.GetTempScript(whileString.String).Execute(new char[] { '"' }, 0).AsBool())
                {
                    if (forIsSet && !Script.GetTempScript(forString).Execute(new char[] { '"' }, 0).AsBool())
                    {
                        new Btrieve.FINDVClass(tableHndlNum, "n", "", "", "", "", Script).FINDV();
                        continue;
                    }

                    var currentRow = gridsDataTables[gridName].NewRow();

                    currentRow["ID"] = thisOpenv.currentRow;

                    foreach (var column in tagsAndTypes)
                    {
                        if (CSCS_GUI.DEFINES.TryGetValue(column.Key.ToLower(), out DefineVariable defVar))
                        {
                            currentRow[column.Key] = defVar.AsString();
                        }
                    }
                    gridsDataTables[gridName].Rows.Add(currentRow);


                    new Btrieve.FINDVClass(tableHndlNum, "n", "", "", "", "", Script).FINDV();
                    if (LastFlerrsOfFnums[tableHndlNum] == 3)
                    {
                        SetFlerr(0, tableHndlNum);
                        break;
                    }
                }
            }

            //            private void fillDataTableQuery()
            //            {
            //                int startId = 1;

            //                if (!string.IsNullOrEmpty(startString))
            //                {
            //                    // if has start string
            //                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, startString).FINDV();
            //                    startId = new RcnGetFunction().RcnGet(thisOpenv).AsInt();
            //                }

            //                StringBuilder columnsSB = new StringBuilder();
            //                columnsSB.Append("ID, ");
            //                foreach (var columnName in tagsAndTypes.Keys)
            //                {
            //                    columnsSB.Append(columnName + ", ");
            //                }
            //                columnsSB.Remove(columnsSB.Length - 2, 2); // removes last ", "

            //                StringBuilder whereSB = new StringBuilder();
            //                if (!string.IsNullOrEmpty(whileString) || !string.IsNullOrEmpty(forString))
            //                {
            //                    whereSB.Append("WHERE (");
            //                }
            //                if (!string.IsNullOrEmpty(whileString))
            //                {
            //                    whereSB.Append(whileString);
            //                }
            //                if (!string.IsNullOrEmpty(whileString) && !string.IsNullOrEmpty(forString))
            //                {
            //                    whereSB.Append(") AND (");
            //                }
            //                if (!string.IsNullOrEmpty(forString))
            //                {
            //                    whereSB.Append(forString);
            //                }
            //                if (!string.IsNullOrEmpty(whileString) || !string.IsNullOrEmpty(whileString))
            //                {
            //                    whereSB.Append(")");
            //                }
            //                if (startId > 1)
            //                {
            //                    if (string.IsNullOrEmpty(whereSB.ToString()))
            //                    {
            //                        whereSB.Append("WHERE (");
            //                    }
            //                    else
            //                    {
            //                        whereSB.Append("AND (");
            //                    }
            //                    whereSB.Append("ID >= " + startId);
            //                    whereSB.Append(")");
            //                }

            //                StringBuilder orderBySB = new StringBuilder();
            //                foreach (var keyColumn in KeyClass.KeyColumns)
            //                {
            //                    orderBySB.Append(keyColumn.Key + ", ");
            //                }
            //                if (!KeyClass.Unique)
            //                {
            //                    orderBySB.Append("ID, ");
            //                }
            //                orderBySB.Remove(orderBySB.Length - 2, 2); // removes last ", "


            //                var query =
            //$@"EXECUTE sp_executesql N'
            //select 
            //{columnsSB}
            //from {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
            //with (nolock)
            //{whereSB}
            //order by {orderBySB}
            //'";

            //                using (SqlConnection con = new SqlConnection(CSCS_SQL.ConnectionString))
            //                {
            //                    using (SqlCommand cmd = new SqlCommand(query, con))
            //                    {
            //                        con.Open();
            //                        using (SqlDataReader reader = cmd.ExecuteReader())
            //                        {
            //                            if (!reader.HasRows)
            //                            {
            //                                SetFlerr(3, tableHndlNum);
            //                                return;
            //                            }
            //                            else
            //                            {
            //                                gridsDataTables[gridName].Rows.Clear();
            //                                gridsDataTables[gridName].Load(reader);
            //                            }
            //                        }
            //                    }
            //                }
            //            }


            private void DataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
            {
                if (e.PropertyName == "ID")
                {
                    e.Column.Visibility = Visibility.Collapsed;
                }
                else
                {
                    var realHeader = tagsAndHeaders[e.Column.Header.ToString()];

                    if (e.PropertyType == typeof(DateTime))
                    {
                        // Create The Column
                        DataGridTemplateColumn dateColumn = new DataGridTemplateColumn();

                        //Binding bind = new Binding(e.Column.Header.ToString());
                        //bind.Mode = BindingMode.TwoWay;
                        //bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                        //bind.StringFormat = "dd/MM/yy";
                        //if(timeAndDateEditerTagsAndSizes[e.Column.Header.ToString()] == 10)
                        //{
                        //    bind.Converter = new DateEditerConverter();
                        //    bind.StringFormat = "dd/MM/yyyy";
                        //}

                        Binding bind = new Binding(e.Column.Header.ToString());
                        bind.Mode = BindingMode.TwoWay;
                        bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                        if (timeAndDateEditerTagsAndSizes[e.Column.Header.ToString()] == 8)
                        {
                            CultureInfo ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
                            ci.DateTimeFormat.ShortDatePattern = "dd/MM/yy";
                            //ci.DateTimeFormat.LongDatePattern = "dd/MM/yyyy HH:mm:ss";
                            Thread.CurrentThread.CurrentCulture = ci;

                            bind.Converter = new DateEditerConverter();
                            bind.ConverterParameter = 8; //size
                            bind.StringFormat = "dd/MM/yy";
                        }
                        else if (timeAndDateEditerTagsAndSizes[e.Column.Header.ToString()] == 10)
                        {
                            CultureInfo ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
                            ci.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
                            //ci.DateTimeFormat.LongDatePattern = "dd/MM/yyyy HH:mm:ss";
                            Thread.CurrentThread.CurrentCulture = ci;

                            bind.Converter = new DateEditerConverter();
                            bind.ConverterParameter = 10; //size
                            bind.StringFormat = "dd/MM/yyyy";
                        }

                        // Create the TextBlock
                        FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                        textBlockFactory.SetBinding(TextBlock.TextProperty, bind);

                        DataTemplate textBlockTemplate = new DataTemplate();
                        textBlockTemplate.VisualTree = textBlockFactory;

                        // Create the DatePicker
                        FrameworkElementFactory datePickerFactory = new FrameworkElementFactory(typeof(DateEditer));
                        datePickerFactory.SetBinding(DateEditer.TextProperty, bind);
                        datePickerFactory.SetValue(DateEditer.DisplaySizeProperty, timeAndDateEditerTagsAndSizes[e.Column.Header.ToString()]);


                        DataTemplate datePickerTemplate = new DataTemplate();
                        datePickerTemplate.VisualTree = datePickerFactory;

                        // Set the Templates to the Column
                        dateColumn.CellTemplate = textBlockTemplate;
                        dateColumn.CellEditingTemplate = datePickerTemplate;

                        e.Column = dateColumn;
                    }
                    else if (e.PropertyType == typeof(TimeSpan))
                    {
                        // Create The Column
                        DataGridTemplateColumn timeColumn = new DataGridTemplateColumn();

                        Binding bind = new Binding(e.Column.Header.ToString());
                        bind.Mode = BindingMode.TwoWay;
                        bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                        if (timeAndDateEditerTagsAndSizes[e.Column.Header.ToString()] == 5)
                        {
                            bind.Converter = new TimeEditerConverter();
                            bind.ConverterParameter = 5;
                        }
                        else if (timeAndDateEditerTagsAndSizes[e.Column.Header.ToString()] == 8)
                        {
                            bind.Converter = new TimeEditerConverter();
                            bind.ConverterParameter = 8;
                        }

                        // Create the TextBlock
                        FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                        textBlockFactory.SetBinding(TextBlock.TextProperty, bind);
                        //textBlockFactory.AddHandler(TextBlock.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(textBlockGotKeyboardFocus));

                        DataTemplate textBlockTemplate = new DataTemplate();
                        textBlockTemplate.VisualTree = textBlockFactory;

                        // Create the TimeEditer
                        FrameworkElementFactory timeEditerFactory = new FrameworkElementFactory(typeof(TimeEditer));
                        timeEditerFactory.SetBinding(TimeEditer.TextProperty, bind);
                        timeEditerFactory.SetValue(TimeEditer.DisplaySizeProperty, timeAndDateEditerTagsAndSizes[e.Column.Header.ToString()]);

                        DataTemplate timeEditerTemplate = new DataTemplate();
                        timeEditerTemplate.VisualTree = timeEditerFactory;

                        // Set the Templates to the Column
                        timeColumn.CellTemplate = textBlockTemplate;
                        timeColumn.CellEditingTemplate = timeEditerTemplate;

                        e.Column = timeColumn;
                    }
                    else if (e.PropertyType == typeof(string))
                    {
                        // Create The Column
                        DataGridTemplateColumn stringColumn = new DataGridTemplateColumn();

                        Binding bind = new Binding(e.Column.Header.ToString());
                        bind.Mode = BindingMode.TwoWay;
                        bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                        // Create the TextBlock
                        FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                        textBlockFactory.SetBinding(TextBlock.TextProperty, bind);

                        DataTemplate textBlockTemplate = new DataTemplate();
                        textBlockTemplate.VisualTree = textBlockFactory;

                        // Create the TextBox
                        FrameworkElementFactory textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
                        textBoxFactory.SetBinding(TextBox.TextProperty, bind);

                        DataTemplate textBoxTemplate = new DataTemplate();
                        textBoxTemplate.VisualTree = textBoxFactory;

                        // Set the Templates to the Column
                        stringColumn.CellTemplate = textBlockTemplate;
                        stringColumn.CellEditingTemplate = textBoxTemplate;

                        e.Column = stringColumn;
                    }
                    else if (e.PropertyType == typeof(int))
                    {
                        // Create The Column
                        DataGridTemplateColumn stringColumn = new DataGridTemplateColumn();

                        Binding bind = new Binding(e.Column.Header.ToString());
                        bind.Mode = BindingMode.TwoWay;
                        bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                        bind.Converter = new TextBoxIntConverter();

                        // Create the TextBlock
                        FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                        textBlockFactory.SetBinding(TextBlock.TextProperty, bind);

                        DataTemplate textBlockTemplate = new DataTemplate();
                        textBlockTemplate.VisualTree = textBlockFactory;

                        // Create the TextBox
                        FrameworkElementFactory textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
                        textBoxFactory.SetBinding(TextBox.TextProperty, bind);

                        DataTemplate textBoxTemplate = new DataTemplate();
                        textBoxTemplate.VisualTree = textBoxFactory;

                        // Set the Templates to the Column
                        stringColumn.CellTemplate = textBlockTemplate;
                        stringColumn.CellEditingTemplate = textBoxTemplate;

                        e.Column = stringColumn;
                    }
                    else if (e.PropertyType == typeof(double))
                    {
                        // Create The Column
                        DataGridTemplateColumn stringColumn = new DataGridTemplateColumn();

                        Binding bind = new Binding(e.Column.Header.ToString());
                        bind.Mode = BindingMode.TwoWay;
                        bind.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
                        bind.Converter = new TextBoxDoubleConverter();

                        // Create the TextBlock
                        FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                        textBlockFactory.SetBinding(TextBlock.TextProperty, bind);

                        DataTemplate textBlockTemplate = new DataTemplate();
                        textBlockTemplate.VisualTree = textBlockFactory;

                        // Create the TextBox
                        FrameworkElementFactory textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
                        textBoxFactory.SetBinding(TextBox.TextProperty, bind);

                        DataTemplate textBoxTemplate = new DataTemplate();
                        textBoxTemplate.VisualTree = textBoxFactory;

                        // Set the Templates to the Column
                        stringColumn.CellTemplate = textBlockTemplate;
                        stringColumn.CellEditingTemplate = textBoxTemplate;

                        e.Column = stringColumn;
                    }
                    else if (e.PropertyType == typeof(bool))
                    {
                        // Create The Column
                        DataGridTemplateColumn stringColumn = new DataGridTemplateColumn();

                        Binding bind = new Binding(e.Column.Header.ToString());
                        bind.Mode = BindingMode.TwoWay;
                        bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                        //bind.Converter = new TextBoxDoubleConverter();

                        // Create the TextBlock
                        FrameworkElementFactory checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
                        checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, bind);
                        checkBoxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);

                        DataTemplate checkBoxTemplate = new DataTemplate();
                        checkBoxTemplate.VisualTree = checkBoxFactory;

                        // Set the Templates to the Column
                        stringColumn.CellTemplate = checkBoxTemplate;
                        stringColumn.CellEditingTemplate = checkBoxTemplate;

                        e.Column = stringColumn;
                    }

                    e.Column.Header = realHeader;
                }
            }

            int lastRowIndex = -1;
            object[] rowBeforeEdit/* = new object[] { }*/;
            object[] rowAfterEdit;
            public static bool dataGridUpdated = false;

            private void Dg_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
            {
                var dg = (sender as DataGrid);

                if (dg.Items.Count <= 0)
                    return;

                try
                {
                    if (dg.SelectedIndex < 0)
                    {
                        if (dg.Items.Count > 0)
                        {
                            dg.SelectedIndex = 0;
                            var asdk = dg.Items[0];
                        }
                    }
                    var currentItemArray = (dg.SelectedItem as DataRowView).Row.ItemArray;

                    var currentRowIndex = dg.SelectedIndex;//dg.Items.IndexOf(dg.SelectedItem);
                    if (currentRowIndex != lastRowIndex || dataGridUpdated)
                    {
                        dataGridUpdated = false;

                        lastRowIndex = currentRowIndex;
                        rowBeforeEdit = currentItemArray;

                        var currRow = (dg.SelectedItem as DataRowView).Row;
                        for (int i = 1; i < currRow.Table.Columns.Count; i++)
                        {
                            if (currentItemArray[i] is DateTime)
                            {
                                var currentItemAsDateTime = currentItemArray[i] as DateTime?;
                                string initForDefine = "";
                                switch (timeAndDateEditerTagsAndSizes[currRow.Table.Columns[i].ColumnName])
                                {
                                    case 10:
                                        initForDefine = currentItemAsDateTime.Value.ToString("dd/MM/yyyy");
                                        break;
                                    case 8:
                                        initForDefine = currentItemAsDateTime.Value.ToString("dd/MM/yy");
                                        break;
                                }

                                CSCS_GUI.DEFINES[currRow.Table.Columns[i].ColumnName.ToLower()].InitVariable(new Variable(initForDefine));
                                Gui.OnVariableChange(currRow.Table.Columns[i].ColumnName.ToLower(), new Variable(initForDefine), true);
                            }
                            else
                            {
                                CSCS_GUI.DEFINES[currRow.Table.Columns[i].ColumnName.ToLower()].InitVariable(new Variable(currentItemArray[i]));
                                Gui.OnVariableChange(currRow.Table.Columns[i].ColumnName.ToLower(), new Variable(currentItemArray[i]), true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }

                dg.BeginEdit();
            }

            private void Dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
            {
                var grid = (sender as DataGrid);

                var index = e.Row.GetIndex();
                DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
                if (row != null && Validation.GetHasError(row))
                {
                    dg.CellEditEnding -= Dg_CellEditEnding;
                    dg.CancelEdit();
                    dg.CellEditEnding += Dg_CellEditEnding;
                }

                rowAfterEdit = (e.Row.Item as DataRowView).Row.ItemArray;
            }

            private void Dg_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
            {
                if (rowBeforeEdit.SequenceEqual(rowAfterEdit))
                {
                    return;
                }

                var rowIndex = e.Row.GetIndex();

                (sender as DataGrid).RowEditEnding -= Dg_RowEditEnding;
                if (MessageBox.Show("Do you want to save the row?", "Caution", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    (sender as DataGrid).CommitEdit();

                    var rowItemArray = gridsDataTables[gridName].Rows[rowIndex].ItemArray;

                    SaveRow(rowItemArray);
                }
                else
                {
                    (sender as DataGrid).SelectedCellsChanged -= Dg_SelectedCellsChanged;
                    (sender as DataGrid).CancelEdit();
                    (sender as DataGrid).SelectedCellsChanged += Dg_SelectedCellsChanged;
                }
                (sender as DataGrid).RowEditEnding += Dg_RowEditEnding;
            }

            private void SaveRow(object[] rowItemArray)
            {
                bool redisplay = false;

                if (rowItemArray[0] is int)
                {
                    //UPDATE sql
                    //fill buffer with current row
                    new RcnSetFunction().RcnSet(Gui, tableHndlNum, (int)rowItemArray[0]);
                }
                else
                {
                    //INSERT sql
                    rowItemArray[0] = 0;
                    thisOpenv.currentRow = 0;
                    new ClrFunction().Clear(tableHndlNum, "b", Script);
                    redisplay = true;
                }

                int i = 1;

                dynamic newVariableInit = null;
                foreach (var item in tagsAndTypes)
                {
                    switch (CSCS_GUI.DEFINES[item.Key.ToLower()].Type)
                    {
                        case Variable.VarType.NONE:
                            break;
                        case Variable.VarType.UNDEFINED:
                            break;
                        case Variable.VarType.NUMBER:
                            if (rowItemArray[i] is bool)
                            {
                                rowItemArray[i] = Utils.ConvertToDouble(rowItemArray[i]);
                            }
                            newVariableInit = double.Parse(rowItemArray[i].ToString().Replace(".", ","), NumberStyles.AllowDecimalPoint);
                            break;
                        case Variable.VarType.STRING:
                            newVariableInit = rowItemArray[i].ToString();
                            break;
                        case Variable.VarType.ARRAY:
                            break;
                        case Variable.VarType.ARRAY_NUM:
                            break;
                        case Variable.VarType.ARRAY_STR:
                            break;
                        case Variable.VarType.MAP_NUM:
                            break;
                        case Variable.VarType.MAP_STR:
                            break;
                        case Variable.VarType.BYTE_ARRAY:
                            break;
                        case Variable.VarType.QUIT:
                            break;
                        case Variable.VarType.BREAK:
                            break;
                        case Variable.VarType.CONTINUE:
                            break;
                        case Variable.VarType.OBJECT:
                            break;
                        case Variable.VarType.ENUM:
                            break;
                        case Variable.VarType.VARIABLE:
                            break;
                        case Variable.VarType.DATETIME:
                            if (TimeSpan.TryParse(rowItemArray[i].ToString(), out TimeSpan newTimeSpan))
                            {
                                newVariableInit = (TimeSpan)rowItemArray[i];
                            }
                            else if (DateTime.TryParse(rowItemArray[i].ToString(), out DateTime newDateTime))
                            {
                                switch (timeAndDateEditerTagsAndSizes[item.Key])
                                {
                                    case 10:
                                        newVariableInit = ((DateTime)rowItemArray[i]).ToString("dd/MM/yyyy");
                                        break;
                                    case 8:
                                        newVariableInit = ((DateTime)rowItemArray[i]).ToString("dd/MM/yy");
                                        break;
                                }
                            }
                            break;
                        case Variable.VarType.CUSTOM:
                            break;
                        case Variable.VarType.POINTER:
                            break;
                        default:
                            break;
                    }
                    CSCS_GUI.DEFINES[item.Key.ToLower()].InitVariable(new Variable(newVariableInit));
                    Gui.OnVariableChange(item.Key.ToLower(), new Variable(newVariableInit), true);
                    i++;
                }

                new SaveFunction().Save(true, thisOpenv, tableHndlNum, false, Script);

                if (redisplay)
                {
                    //dg.Items.Clear();
                    fillDataTable(gridName);
                }
            }

        }

        class DisplayArrayClass
        {
            public DataGrid dg;
            public string lineCntrVarName;
            public string actCntrVarName;
            public string maxElemsVarName;
            public List<string> tags;
            public Dictionary<string, Type> tagsAndTypes;
            public Dictionary<string, Type> newTagsAndTypes;
            public string tableOrArray;


        }

        class DisplayTableClass
        {
            public DataGrid dg;
            public string lineCntrVarName;
            public string actCntrVarName;
            public string maxElemsVarName;
            public List<string> tags;
            public Dictionary<string, Type> tagsAndTypes;
            public Dictionary<string, Type> newTagsAndTypes;
            public string tableOrArray;

            public int tableHndlNum;

            public ParsingScript startString;
            public ParsingScript whileString;
            public string forString;
            public string tableKey;

            public KeyClass KeyClass;

            public ParsingScript Script;
        }

        static Dictionary<string, DisplayArrayClass> gridsArrayClass = new Dictionary<string, DisplayArrayClass>();
        static Dictionary<string, DisplayTableClass> gridsTableClass = new Dictionary<string, DisplayTableClass>();
        //static Dictionary<string, int> gridsLineCounters = new Dictionary<string, int>();




        //static Dictionary<string, DisplayArrayClass> grids = new Dictionary<string, DisplayArrayClass>();
        ////static Dictionary<string, int> gridsLineCounters = new Dictionary<string, int>();

        class DisplayArraySetupFunction : ParserFunction
        {
            private int _selectedIndex;
            private CSCS_GUI Gui;
            public int selectedIndex
            {
                get { return _selectedIndex; }
                set
                {

                    _selectedIndex = value;

                    if (gridsArrayClass.TryGetValue(gridName, out DisplayArrayClass dac))
                    {
                        // updates lineCntr variable
                        if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].lineCntrVarName.ToLower(), out DefineVariable defVar))
                        {
                            defVar.Value = value;
                        }
                    }

                    // gridName@move event
                    var widgetName = Gui.GetWidgetName(dg);
                    if (string.IsNullOrWhiteSpace(widgetName))
                    {
                        return;
                    }

                    string funcName;
                    if (CSCS_GUI.s_MoveHandlers.TryGetValue(widgetName, out funcName))
                    {
                        Gui.Control2Window.TryGetValue(dg, out Window win);
                        var result = Interpreter.LastInstance.Run(funcName, new Variable(widgetName), null,
                            Variable.EmptyInstance, Gui.GetScript(win));
                    }
                }
            }

            int tableHndlNum;

            string tableKey;

            int maxRows;
            int actCntr;
            string lineCntrVarName;
            string actCntrVarName;
            OpenvTable thisOpenv;

            ParsingScript Script;

            //DataTable gridSource;
            string gridName;

            Dictionary<string, string> tagsAndHeaders;
            //Dictionary<string, Type> tagsAndTypes;
            //Dictionary<string, Type> newTagsAndTypes;
            Dictionary<string, int> timeAndDateEditerTagsAndSizes = new Dictionary<string, int>();
            Dictionary<string, string> tagsAndNames = new Dictionary<string, string>();

            DataGrid dg;
            protected override Variable Evaluate(ParsingScript script)
            {
                Script = script;
                Gui = script.Context as CSCS_GUI;
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 4, m_name);

                gridName = Utils.GetSafeString(args, 0).ToLower();
                lineCntrVarName = Utils.GetSafeString(args, 1);
                actCntrVarName = Utils.GetSafeString(args, 2);
                string maxRowsVarName = Utils.GetSafeString(args, 3);

                //------------------------------------------------------------------------

                gridsArrayClass[gridName] = new DisplayArrayClass();

                gridsArrayClass[gridName].lineCntrVarName = lineCntrVarName;
                gridsArrayClass[gridName].actCntrVarName = actCntrVarName;
                gridsArrayClass[gridName].maxElemsVarName = maxRowsVarName;

                //------------------------------------------------------------------------

                if (CSCS_GUI.DEFINES.TryGetValue(maxRowsVarName.ToLower(), out DefineVariable defVar))
                {
                    if (defVar.Type == Variable.VarType.NUMBER)
                    {
                        int.TryParse(defVar.Value.ToString(), out maxRows);
                    }
                }
                else
                {
                    //throw error
                }

                if (CSCS_GUI.DEFINES.TryGetValue(actCntrVarName.ToLower(), out DefineVariable defVar2))
                {
                    if (defVar2.Type == Variable.VarType.NUMBER)
                    {
                        int.TryParse(defVar2.Value.ToString(), out actCntr);
                    }
                }
                else
                {
                    //throw error
                }

                //------------------------------------------------------------------------

                dg = Gui.GetWidget(gridName) as DataGrid;
                if (dg == null)
                {
                    return Variable.EmptyInstance;
                }

                gridsArrayClass[gridName].tagsAndTypes = new Dictionary<string, Type>();
                tagsAndHeaders = new Dictionary<string, string>();

                var columns = dg.Columns;
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = dg.Columns.ElementAt(i);

                    if (column is DataGridTemplateColumn)
                    {
                        var dgtc = column as DataGridTemplateColumn;

                        var cell = dgtc.CellTemplate.LoadContent();
                        if (cell is TimeEditer)
                        {
                            var te = cell as TimeEditer;
                            if (te.Tag != null)
                            {
                                gridsArrayClass[gridName].tagsAndTypes.Add(te.Tag.ToString(), typeof(TimeSpan));
                                tagsAndHeaders.Add(te.Tag.ToString(), dgtc.Header.ToString());
                                timeAndDateEditerTagsAndSizes[te.Tag.ToString()] = te.DisplaySize;
                                tagsAndNames[te.Tag.ToString()] = te.Name;
                            }
                        }
                        else if (cell is DateEditer)
                        {
                            var de = cell as DateEditer;
                            if (de.Tag != null)
                            {
                                gridsArrayClass[gridName].tagsAndTypes.Add(de.Tag.ToString(), typeof(DateTime));
                                tagsAndHeaders.Add(de.Tag.ToString(), dgtc.Header.ToString());
                                timeAndDateEditerTagsAndSizes[de.Tag.ToString()] = de.DisplaySize;
                                tagsAndNames[de.Tag.ToString()] = de.Name;
                            }
                        }
                        else if (cell is CheckBox)
                        {
                            var cb = cell as CheckBox;
                            if (cb.Tag != null)
                            {
                                gridsArrayClass[gridName].tagsAndTypes.Add(cb.Tag.ToString(), typeof(bool));
                                tagsAndHeaders.Add(cb.Tag.ToString(), dgtc.Header.ToString());
                                tagsAndNames[cb.Tag.ToString()] = cb.Name;
                            }
                        }
                        else if (cell is TextBox)
                        {
                            var tb = cell as TextBox;
                            if (tb.Tag != null)
                            {
                                gridsArrayClass[gridName].tagsAndTypes.Add(tb.Tag.ToString(), typeof(string));
                                tagsAndHeaders.Add(tb.Tag.ToString(), dgtc.Header.ToString());
                                tagsAndNames[tb.Tag.ToString()] = tb.Name;
                            }
                        }
                    }
                }

                gridsDataTables[gridName] = new DataTable();

                gridsArrayClass[gridName].newTagsAndTypes = new Dictionary<string, Type>();
                foreach (var item in gridsArrayClass[gridName].tagsAndTypes)
                {
                    var newColumn = new DataColumn();
                    newColumn.ColumnName = item.Key;
                    newColumn.DataType = item.Value;

                    if (CSCS_GUI.DEFINES.TryGetValue(item.Key.ToLower(), out DefineVariable defVariable))
                    {
                        switch (defVariable.DefType.ToUpper())
                        {
                            case "B":
                            case "I":
                            case "R":
                                newColumn.DataType = typeof(int);
                                //tagsAndTypes[item.Key] = typeof(int);
                                gridsArrayClass[gridName].newTagsAndTypes[item.Key] = typeof(int);
                                break;
                            case "N":
                                newColumn.DataType = typeof(double);
                                //tagsAndTypes[item.Key] = typeof(double);
                                gridsArrayClass[gridName].newTagsAndTypes[item.Key] = typeof(double);
                                break;
                            case "A":
                                newColumn.DataType = typeof(string);
                                //tagsAndTypes[item.Key] = typeof(string);
                                gridsArrayClass[gridName].newTagsAndTypes[item.Key] = typeof(string);
                                break;
                            case "L":
                                newColumn.DataType = typeof(bool);
                                //tagsAndTypes[item.Key] = typeof(bool);
                                gridsArrayClass[gridName].newTagsAndTypes[item.Key] = typeof(bool);
                                break;
                            case "D":
                                newColumn.DataType = typeof(DateTime);
                                //tagsAndTypes[item.Key] = typeof(DateTime);
                                gridsArrayClass[gridName].newTagsAndTypes[item.Key] = typeof(DateTime);
                                break;
                            case "T":
                                newColumn.DataType = typeof(TimeSpan);
                                //tagsAndTypes[item.Key] = typeof(TimeSpan);
                                gridsArrayClass[gridName].newTagsAndTypes[item.Key] = typeof(TimeSpan);
                                break;
                            default:
                                break;
                        }
                    }

                    newColumn.Caption = tagsAndHeaders[item.Key];
                    gridsDataTables[gridName].Columns.Add(newColumn);
                }

                //---------------------------------------------------------------------------

                dg.Items.Clear();
                dg.Columns.Clear();

                dg.AutoGenerateColumns = true;
                dg.AutoGeneratingColumn += DataGrid_OnAutoGeneratingColumn;

                dg.SelectionMode = DataGridSelectionMode.Single;
                //dg.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
                dg.SelectionUnit = DataGridSelectionUnit.FullRow;

                //dg.CanUserSortColumns = true;
                dg.CanUserSortColumns = true;

                //dg.ItemsSource = gridSource.DefaultView;

                //dg.SelectionChanged += Dg_SelectionChanged;
                dg.CellEditEnding += Dg_CellEditEnding;
                //dg.PreparingCellForEdit += Dg_PreparingCellForEdit;

                dg.RowEditEnding += Dg_RowEditEnding;

                dg.SelectedCellsChanged += Dg_SelectedCellsChanged;

                //dg.AddingNewItem += Dg_AddingNewItem;

                dg.AutoGeneratedColumns += Dg_AutoGeneratedColumns;

                dg.ItemsSource = gridsDataTables[gridName].AsDataView();

                //dg.IsKeyboardFocusWithinChanged += Dg_IsKeyboardFocusWithinChanged;

                //dg.SelectedCellsChanged += Dg_SelectedCellsChanged1;

                dg.DataContext = this;
                Binding bind = new Binding("selectedIndex");
                bind.Mode = BindingMode.OneWayToSource;
                bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dg.SetBinding(DataGrid.SelectedIndexProperty, bind);
                //-------------------------------------------------------------------------------

                fillDataTable(gridName);

                gridsArrayClass[gridName].dg = dg;
                gridsArrayClass[gridName].tags = tagsAndHeaders.Keys.ToList();
                gridsArrayClass[gridName].tableOrArray = "array";

                return Variable.EmptyInstance;
            }

            //private void Dg_SelectedCellsChanged1(object sender, SelectedCellsChangedEventArgs e)
            //{
            //    Debug.WriteLine("Selected Cells Changed");
            //}

            //private void Dg_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
            //{
            //   Debug.WriteLine("Dg_IsKeyboardFocusWithinChanged");
            //}

            public void fillDataTable(string gridName)
            {
                int lastArrayCount = 0;
                for (int i = 0; i < gridsArrayClass[gridName].tagsAndTypes.Count; i++)
                {
                    var thisArrCount = 0;
                    var arrName = gridsArrayClass[gridName].tagsAndTypes.Keys.ToArray()[i];
                    if (CSCS_GUI.DEFINES.TryGetValue(arrName.ToLower(), out DefineVariable defineVariable))
                    {
                        if (defineVariable.Type == Variable.VarType.ARRAY)
                        {
                            thisArrCount = defineVariable.Tuple.Count;
                        }
                    }

                    if (i > 0)
                    {
                        if (thisArrCount != lastArrayCount)
                        {
                            throw new Exception("Unequal length of arrays");
                        }
                    }

                    lastArrayCount = thisArrCount;
                }

                if (!CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].maxElemsVarName.ToLower(), out DefineVariable maxrows))
                {
                    return;
                }

                if (!CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].actCntrVarName.ToLower(), out DefineVariable actelems))
                {
                    return;
                }

                for (int i = 0; i < (int)maxrows.Value && i < lastArrayCount && i < (int)actelems.Value; i++)
                {
                    var currentRow = gridsDataTables[gridName].NewRow();

                    foreach (var column in gridsArrayClass[gridName].newTagsAndTypes)
                    {
                        if (CSCS_GUI.DEFINES.TryGetValue(column.Key.ToLower(), out DefineVariable defVar))
                        {
                            Variable current = defVar.Tuple.ElementAt(i);
                            switch (column.Value.Name)
                            {
                                case "String":
                                    currentRow[column.Key] = current.AsString();
                                    break;
                                case "Int32":
                                    currentRow[column.Key] = (int)current.AsDouble();
                                    break;
                                case "Double":
                                    currentRow[column.Key] = current.AsDouble();
                                    break;
                                case "Boolean":
                                    currentRow[column.Key] = current.AsBool();
                                    break;
                                case "DateTime":
                                    if (defVar.Size == 8)
                                    {
                                        if (DateTime.TryParseExact(current.AsString(), "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                                        {
                                            currentRow[column.Key] = dateTime;
                                        }
                                    }
                                    else if (defVar.Size == 10)
                                    {
                                        if (DateTime.TryParseExact(current.AsString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                                        {
                                            currentRow[column.Key] = dateTime;
                                        }
                                    }
                                    break;
                                case "TimeSpan":
                                    if (defVar.Size == 5)
                                    {
                                        currentRow[column.Key] = TimeSpan.ParseExact(current.AsString(), "hh\\:mm", CultureInfo.InvariantCulture);
                                    }
                                    else if (defVar.Size == 8)
                                    {
                                        currentRow[column.Key] = TimeSpan.ParseExact(current.AsString(), "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                                    }
                                    break;
                                default:
                                    throw new Exception("Type " + column.Value.Name + " implementation missing!");
                            }
                        }
                    }
                    gridsDataTables[gridName].Rows.Add(currentRow);
                }
            }

            private void DataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
            {
                var tag = e.Column.Header.ToString();
                var realHeader = tagsAndHeaders[tag];

                if (e.PropertyType == typeof(DateTime))
                {
                    // Create The Column
                    DataGridTemplateColumn dateColumn = new DataGridTemplateColumn();

                    Binding bind = new Binding(tag);
                    bind.Mode = BindingMode.TwoWay;
                    bind.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
                    if (timeAndDateEditerTagsAndSizes[tag] == 8)
                    {
                        CultureInfo ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
                        ci.DateTimeFormat.ShortDatePattern = "dd/MM/yy";
                        //ci.DateTimeFormat.LongDatePattern = "dd/MM/yyyy HH:mm:ss";
                        Thread.CurrentThread.CurrentCulture = ci;

                        bind.Converter = new DateEditerConverter();
                        bind.ConverterParameter = 8; //size
                        bind.StringFormat = "dd/MM/yy";
                    }
                    else if (timeAndDateEditerTagsAndSizes[tag] == 10)
                    {
                        CultureInfo ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
                        ci.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
                        //ci.DateTimeFormat.LongDatePattern = "dd/MM/yyyy HH:mm:ss";
                        Thread.CurrentThread.CurrentCulture = ci;

                        bind.Converter = new DateEditerConverter();
                        bind.ConverterParameter = 10; //size
                        bind.StringFormat = "dd/MM/yyyy";
                    }

                    // Create the TextBlock
                    FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                    textBlockFactory.SetBinding(TextBlock.TextProperty, bind);
                    textBlockFactory.SetValue(DateEditer.NameProperty, tagsAndNames[tag]);

                    DataTemplate textBlockTemplate = new DataTemplate();
                    textBlockTemplate.VisualTree = textBlockFactory;

                    // Create the DatePicker
                    FrameworkElementFactory datePickerFactory = new FrameworkElementFactory(typeof(DateEditer));
                    datePickerFactory.SetBinding(DateEditer.TextProperty, bind);
                    datePickerFactory.SetValue(DateEditer.DisplaySizeProperty, timeAndDateEditerTagsAndSizes[tag]);


                    DataTemplate datePickerTemplate = new DataTemplate();
                    datePickerTemplate.VisualTree = datePickerFactory;

                    // Set the Templates to the Column
                    dateColumn.CellTemplate = textBlockTemplate;
                    dateColumn.CellEditingTemplate = datePickerTemplate;

                    e.Column = dateColumn;
                }
                else if (e.PropertyType == typeof(TimeSpan))
                {
                    // Create The Column
                    DataGridTemplateColumn timeColumn = new DataGridTemplateColumn();

                    Binding bind = new Binding(tag);
                    bind.Mode = BindingMode.TwoWay;
                    bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    if (timeAndDateEditerTagsAndSizes[tag] == 5)
                    {
                        bind.Converter = new TimeEditerConverter();
                        bind.ConverterParameter = 5;
                    }
                    else if (timeAndDateEditerTagsAndSizes[tag] == 8)
                    {
                        bind.Converter = new TimeEditerConverter();
                        bind.ConverterParameter = 8;
                    }

                    // Create the TextBlock
                    FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                    textBlockFactory.SetBinding(TextBlock.TextProperty, bind);
                    //textBlockFactory.AddHandler(TextBlock.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(textBlockGotKeyboardFocus));
                    textBlockFactory.SetValue(DateEditer.NameProperty, tagsAndNames[tag]);

                    DataTemplate textBlockTemplate = new DataTemplate();
                    textBlockTemplate.VisualTree = textBlockFactory;

                    // Create the TimeEditer
                    FrameworkElementFactory timeEditerFactory = new FrameworkElementFactory(typeof(TimeEditer));
                    timeEditerFactory.SetBinding(TimeEditer.TextProperty, bind);
                    timeEditerFactory.SetValue(TimeEditer.DisplaySizeProperty, timeAndDateEditerTagsAndSizes[tag]);

                    DataTemplate timeEditerTemplate = new DataTemplate();
                    timeEditerTemplate.VisualTree = timeEditerFactory;

                    // Set the Templates to the Column
                    timeColumn.CellTemplate = textBlockTemplate;
                    timeColumn.CellEditingTemplate = timeEditerTemplate;

                    e.Column = timeColumn;
                }
                else if (e.PropertyType == typeof(string))
                {
                    // Create The Column
                    DataGridTemplateColumn stringColumn = new DataGridTemplateColumn();

                    Binding bind = new Binding(tag);
                    bind.Mode = BindingMode.TwoWay;
                    bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                    // Create the TextBlock
                    FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                    textBlockFactory.SetBinding(TextBlock.TextProperty, bind);
                    textBlockFactory.SetValue(DateEditer.NameProperty, tagsAndNames[tag]);

                    DataTemplate textBlockTemplate = new DataTemplate();
                    textBlockTemplate.VisualTree = textBlockFactory;

                    // Create the TextBox
                    FrameworkElementFactory textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
                    textBoxFactory.SetBinding(TextBox.TextProperty, bind);

                    DataTemplate textBoxTemplate = new DataTemplate();
                    textBoxTemplate.VisualTree = textBoxFactory;

                    // Set the Templates to the Column
                    stringColumn.CellTemplate = textBlockTemplate;
                    stringColumn.CellEditingTemplate = textBoxTemplate;

                    e.Column = stringColumn;
                }
                else if (e.PropertyType == typeof(int))
                {
                    // Create The Column
                    DataGridTemplateColumn intColumn = new DataGridTemplateColumn();

                    Binding bind = new Binding(tag);
                    bind.Mode = BindingMode.TwoWay;
                    bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    bind.Converter = new TextBoxIntConverter();

                    // Create the TextBlock
                    FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                    textBlockFactory.SetBinding(TextBlock.TextProperty, bind);
                    textBlockFactory.SetValue(DateEditer.NameProperty, tagsAndNames[tag]);

                    DataTemplate textBlockTemplate = new DataTemplate();
                    textBlockTemplate.VisualTree = textBlockFactory;

                    // Create the TextBox
                    FrameworkElementFactory textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
                    textBoxFactory.SetBinding(TextBox.TextProperty, bind);

                    DataTemplate textBoxTemplate = new DataTemplate();
                    textBoxTemplate.VisualTree = textBoxFactory;

                    // Set the Templates to the Column
                    intColumn.CellTemplate = textBlockTemplate;
                    intColumn.CellEditingTemplate = textBoxTemplate;

                    e.Column = intColumn;
                }
                else if (e.PropertyType == typeof(double))
                {
                    // Create The Column
                    DataGridTemplateColumn doubleColumn = new DataGridTemplateColumn();

                    Binding bind = new Binding(tag);
                    bind.Mode = BindingMode.TwoWay;
                    bind.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
                    bind.Converter = new TextBoxDoubleConverter();

                    // Create the TextBlock
                    FrameworkElementFactory textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                    textBlockFactory.SetBinding(TextBlock.TextProperty, bind);
                    textBlockFactory.SetValue(DateEditer.NameProperty, tagsAndNames[tag]);

                    DataTemplate textBlockTemplate = new DataTemplate();
                    textBlockTemplate.VisualTree = textBlockFactory;

                    // Create the TextBox
                    FrameworkElementFactory textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
                    textBoxFactory.SetBinding(TextBox.TextProperty, bind);

                    DataTemplate textBoxTemplate = new DataTemplate();
                    textBoxTemplate.VisualTree = textBoxFactory;

                    // Set the Templates to the Column
                    doubleColumn.CellTemplate = textBlockTemplate;
                    doubleColumn.CellEditingTemplate = textBoxTemplate;

                    e.Column = doubleColumn;
                }
                else if (e.PropertyType == typeof(bool))
                {
                    // Create The Column
                    DataGridTemplateColumn boolColumn = new DataGridTemplateColumn();

                    Binding bind = new Binding(tag);
                    bind.Mode = BindingMode.TwoWay;
                    bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    //bind.Converter = new TextBoxDoubleConverter();

                    FrameworkElementFactory checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
                    checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, bind);

                    ////////bind Focusable to DataGrid's IsReadOnly
                    //////checkBoxFactory.SetBinding(CheckBox.FocusableProperty, dataGridsIsReadOnlyPropertyBind);

                    //for disabling of checkBox toggling is the DataGrid is in Read Only mode
                    checkBoxFactory.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(checkBoxClick));

                    checkBoxFactory.SetValue(CheckBox.FocusableProperty, false);
                    checkBoxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    checkBoxFactory.SetValue(CheckBox.NameProperty, tagsAndNames[tag]);


                    FrameworkElementFactory checkBoxEditingFactory = new FrameworkElementFactory(typeof(CheckBox));
                    checkBoxEditingFactory.SetBinding(CheckBox.IsCheckedProperty, bind);

                    ////////bind Focusable to DataGrid's IsReadOnly
                    //////checkBoxFactory.SetBinding(CheckBox.FocusableProperty, dataGridsIsReadOnlyPropertyBind);

                    //for disabling of checkBox toggling is the DataGrid is in Read Only mode
                    //checkBoxEditingFactory.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(checkboxChecked));

                    checkBoxEditingFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    checkBoxEditingFactory.SetValue(CheckBox.NameProperty, tagsAndNames[tag]);


                    DataTemplate checkBoxTemplate = new DataTemplate();
                    checkBoxTemplate.VisualTree = checkBoxFactory;

                    DataTemplate checkBoxEditingTemplate = new DataTemplate();
                    checkBoxEditingTemplate.VisualTree = checkBoxEditingFactory;

                    // Set the Templates to the Column
                    boolColumn.CellTemplate = checkBoxTemplate;
                    boolColumn.CellEditingTemplate = checkBoxEditingTemplate;

                    e.Column = boolColumn;
                }

                e.Column.CanUserSort = false;
                //e.Column.SortMemberPath = tag;

                e.Column.Header = realHeader;
            }

            private void checkBoxClick(object sender, RoutedEventArgs e)
            {
                DependencyObject current = (e.Source as FrameworkElement);

                while (current != null)
                {
                    if (current.GetType() == typeof(DataGrid))
                    {
                        var dg = (current as DataGrid);
                        if (dg.IsReadOnly)
                        {
                            var cb = (e.Source as CheckBox);
                            cb.IsChecked = !cb.IsChecked;
                            return;
                        }
                        else
                        {
                            //edit mode
                            dg.BeginEdit();
                            //rowAfterEdit = dg. dg.SelectedIndex
                            var asdrowAfterEdit = (dg.CurrentItem as DataRowView).Row.ItemArray;
                        }
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            //private void checkboxChecked(object sender, RoutedEventArgs e)
            //{
            //    DependencyObject current = (e.Source as FrameworkElement);
            //    Type targetType = typeof(DataGrid);

            //    while (current != null)
            //    {
            //        if (current.GetType() == targetType)
            //        {
            //            if ((current as DataGrid).IsReadOnly)
            //            {
            //                var cb = (e.Source as CheckBox);
            //                cb.IsChecked = !cb.IsChecked;
            //                return;
            //            }
            //        }
            //        current = VisualTreeHelper.GetParent(current);
            //    }
            //}

            private void Dg_AutoGeneratedColumns(object sender, EventArgs e)
            {
                (sender as DataGrid).AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(AddCSCSHeaderClickHandler));
            }

            private void AddCSCSHeaderClickHandler(object sender, RoutedEventArgs e)
            {
                var dg = sender as DataGrid;
                DataGridColumnHeader dgch = e.OriginalSource as DataGridColumnHeader;
                if (dgch == null)
                    return;
                var tabIndex = dgch.TabIndex;

                var widgetName = (dg.Columns[tabIndex] as DataGridTemplateColumn).CellTemplate.LoadContent().GetValue(FrameworkElement.NameProperty);
                if (string.IsNullOrWhiteSpace(widgetName.ToString()))
                {
                    return;
                }

                string funcName = widgetName + "@Header";

                Gui.Control2Window.TryGetValue(dgch, out Window win);
                Interpreter.LastInstance.Run(funcName, new Variable(widgetName), null,
                    Variable.EmptyInstance, Gui.GetScript(win));
            }

            int lastRowIndex = -1;
            object[] rowBeforeEdit;
            object[] rowAfterEdit;

            private void Dg_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
            {
                var dg = (sender as DataGrid);

                var currentRowIndex = dg.SelectedIndex;
                if (currentRowIndex != lastRowIndex)
                {
                    lastRowIndex = currentRowIndex;
                    if (dg.CurrentItem is DataRowView)
                    {
                        var currentItemArray = (dg.CurrentItem as DataRowView).Row.ItemArray;
                        rowBeforeEdit = currentItemArray;
                    }
                    else
                    {
                        //NewItemPlaceholder
                        rowBeforeEdit = new object[dg.Columns.Count];
                    }
                }

                dg.BeginEdit();

                CSCS_GUI.DEFINES[lineCntrVarName.ToLower()].InitVariable(new Variable(currentRowIndex));
            }

            private void Dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
            {
                var dg = (sender as DataGrid);

                var index = e.Row.GetIndex();

                DataGridRow row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
                if (row != null && Validation.GetHasError(row))
                {
                    dg.CellEditEnding -= Dg_CellEditEnding;
                    dg.CancelEdit();
                    dg.CellEditEnding += Dg_CellEditEnding;
                }

                rowAfterEdit = (e.Row.Item as DataRowView).Row.ItemArray;
            }

            private void Dg_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
            {
                if (rowBeforeEdit.SequenceEqual(rowAfterEdit))
                {
                    return;
                }

                var dg = (sender as DataGrid);

                var rowIndex = e.Row.GetIndex();

                dg.RowEditEnding -= Dg_RowEditEnding;
                if (MessageBox.Show("Do you want to save the row?", "Caution", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    (sender as DataGrid).CommitEdit();

                    var rowItemArray = gridsDataTables[gridName].Rows[rowIndex].ItemArray;

                    SaveRow(rowIndex, rowItemArray);
                }
                else
                {
                    dg.SelectedCellsChanged -= Dg_SelectedCellsChanged;
                    dg.CancelEdit();
                    dg.SelectedCellsChanged += Dg_SelectedCellsChanged;
                }
                (sender as DataGrid).RowEditEnding += Dg_RowEditEnding;

                if (dg.Items.Count - 1 >= maxRows)
                {
                    dg.CanUserAddRows = false;
                }
                else
                {
                    dg.CanUserAddRows = true;
                }
            }

            private void SaveRow(int rowIndex, object[] rowNewItemArray)
            {
                for (int i = 0; i < gridsArrayClass[gridName].tagsAndTypes.Count; i++)
                {
                    var arrName = gridsArrayClass[gridName].newTagsAndTypes.Keys.ToArray()[i];
                    if (CSCS_GUI.DEFINES.TryGetValue(arrName.ToLower(), out DefineVariable defineVariable))
                    {
                        if (defineVariable.Type == Variable.VarType.ARRAY)
                        {
                            dynamic newDynamicVar;
                            switch (gridsArrayClass[gridName].newTagsAndTypes.ElementAt(i).Value.Name)
                            {
                                case "String":
                                    newDynamicVar = (string)rowNewItemArray[i];
                                    defineVariable.Tuple[rowIndex] = new Variable(newDynamicVar);
                                    break;
                                case "Int32":
                                    newDynamicVar = (int)rowNewItemArray[i];
                                    defineVariable.Tuple[rowIndex] = new Variable((double)newDynamicVar);
                                    break;
                                case "Double":
                                    newDynamicVar = (double)rowNewItemArray[i];
                                    defineVariable.Tuple[rowIndex] = new Variable(newDynamicVar);
                                    break;
                                case "Boolean":
                                    newDynamicVar = (bool)rowNewItemArray[i];
                                    defineVariable.Tuple[rowIndex] = new Variable(newDynamicVar);
                                    break;
                                case "DateTime":
                                    newDynamicVar = (DateTime)rowNewItemArray[i];
                                    defineVariable.Tuple[rowIndex] = new Variable(newDynamicVar.ToString("d"));
                                    break;
                                case "TimeSpan":
                                    newDynamicVar = "";
                                    var size = timeAndDateEditerTagsAndSizes[gridsArrayClass[gridName].newTagsAndTypes.ElementAt(i).Key];
                                    if (size == 5)
                                    {
                                        newDynamicVar = ((TimeSpan)rowNewItemArray[i]).ToString("hh\\:mm");
                                    }
                                    else if (size == 8)
                                    {
                                        newDynamicVar = ((TimeSpan)rowNewItemArray[i]).ToString("hh\\:mm\\:ss");
                                    }

                                    defineVariable.Tuple[rowIndex] = new Variable(newDynamicVar);
                                    break;
                                default:
                                    newDynamicVar = rowNewItemArray[i];
                                    defineVariable.Tuple[rowIndex] = new Variable(newDynamicVar);
                                    break;
                            }
                            //CSCS_GUI.DEFINES[newTagsAndTypes.ElementAt(i).Key.ToLower()].Tuple[rowIndex] = new Variable(newDynamicVar);
                            //CSCS_GUI.OnVariableChange(newTagsAndTypes.ElementAt(i).Key.ToLower(), new Variable(newDynamicVar), true);
                        }
                    }
                }
            }
        }


        class DisplayArrayRefreshFunction : ParserFunction
        {
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 1, m_name);

                var gridName = Utils.GetSafeString(args, 0);

                if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].lineCntrVarName.ToLower(), out DefineVariable defVar))
                {
                    gridsArrayClass[gridName].dg.SelectedIndex = (int)defVar.Value;
                    gridsArrayClass[gridName].dg.ScrollIntoView(gridsArrayClass[gridName].dg.SelectedItem);
                }

                return Variable.EmptyInstance;
            }
        }

        class DisplayArrayFunction : ParserFunction
        {
            CSCS_GUI Gui;
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);

                var gridName = Utils.GetSafeString(args, 0).ToLower();
                var option = Utils.GetSafeString(args, 1).ToLower();

                DataGrid dg = null;
                Gui = script.Context as CSCS_GUI;

                if (Gui.Controls.TryGetValue(gridName, out FrameworkElement dgFe))
                {
                    if (dgFe is DataGrid)
                    {
                        dg = dgFe as DataGrid;
                    }
                    else
                    {
                        return Variable.EmptyInstance;
                    }
                }

                switch (option)
                {
                    case "updatecurrent":

                        if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].lineCntrVarName.ToLower(), out DefineVariable lineCntrDefVar))
                        {
                            var itemArray = gridsDataTables[gridName].Rows[(int)lineCntrDefVar.Value].ItemArray;
                            for (int i = 0; i < itemArray.Length; i++)
                            {

                                if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].tags[i].ToLower(), out DefineVariable defVar))
                                {
                                    itemArray[i] = defVar.Tuple[(int)lineCntrDefVar.Value].AsString();
                                }
                            }

                            gridsDataTables[gridName].Rows[(int)lineCntrDefVar.Value].ItemArray = itemArray;
                        }

                        break;

                    case "redisplayfromtop":

                        dg.ScrollIntoView(dg.Items.GetItemAt(0));
                        dg.SelectedIndex = 0;

                        break;

                    case "redisplayfromend":

                        dg.ScrollIntoView(dg.Items.GetItemAt(dg.Items.Count - 1));
                        dg.SelectedIndex = dg.Items.Count - 1;

                        break;

                    case "redisplayactive":

                        var currentIndex = dg.SelectedIndex;

                        gridsDataTables[gridName].Rows.Clear();
                        new DisplayArraySetupFunction().fillDataTable(gridName);


                        if (currentIndex < 0 || currentIndex > dg.Items.Count - 1)
                        {
                            currentIndex = 0;
                        }
                        if (dg.Items.Count > 0)
                        {
                            dg.ScrollIntoView(dg.Items.GetItemAt(currentIndex));
                            dg.SelectedIndex = currentIndex;
                        }

                        break;

                    default:
                        break;
                }


                return Variable.EmptyInstance;
            }
        }

        class DisplayTableFunction : ParserFunction
        {
            CSCS_GUI Gui;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);

                Gui = script.Context as CSCS_GUI;

                var gridName = Utils.GetSafeString(args, 0).ToLower();
                var option = Utils.GetSafeString(args, 1).ToLower();

                DataGrid dg = null;

                if (Gui.Controls.TryGetValue(gridName, out FrameworkElement dgFe))
                {
                    if (dgFe is DataGrid)
                    {
                        dg = dgFe as DataGrid;
                    }
                    else
                    {
                        return Variable.EmptyInstance;
                    }
                }

                switch (option)
                {
                    case "updatecurrent":

                        if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].lineCntrVarName.ToLower(), out DefineVariable lineCntrDefVar))
                        {
                            var itemArray = gridsDataTables[gridName].Rows[(int)lineCntrDefVar.Value].ItemArray;
                            for (int i = 0; i < itemArray.Length; i++)
                            {

                                if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].tags[i].ToLower(), out DefineVariable defVar))
                                {
                                    itemArray[i] = defVar.Tuple[(int)lineCntrDefVar.Value].AsString();
                                }
                            }

                            gridsDataTables[gridName].Rows[(int)lineCntrDefVar.Value].ItemArray = itemArray;
                        }

                        break;

                    case "redisplayfromtop":

                        dg.ScrollIntoView(dg.Items.GetItemAt(0));
                        dg.SelectedIndex = 0;

                        break;

                    case "redisplayfromend":

                        dg.ScrollIntoView(dg.Items.GetItemAt(dg.Items.Count - 1));
                        dg.SelectedIndex = dg.Items.Count - 1;

                        break;

                    case "redisplayactive":

                        var currentIndex = dg.SelectedIndex;

                        gridsDataTables[gridName].Rows.Clear();
                        new DisplayTableSetupFunction().fillDataTable(gridName);

                        DisplayTableSetupFunction.dataGridUpdated = true;

                        if (currentIndex < 0 || currentIndex > dg.Items.Count - 1)
                        {
                            currentIndex = 0;
                        }
                        if (dg.Items.Count > 0)
                        {
                            dg.ScrollIntoView(dg.Items.GetItemAt(currentIndex));
                            dg.SelectedIndex = currentIndex;
                        }

                        break;

                    default:
                        break;
                }


                return Variable.EmptyInstance;
            }
        }

        class DataGridFunction : ParserFunction
        {
            CSCS_GUI Gui;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);

                Gui = script.Context as CSCS_GUI;

                var gridName = Utils.GetSafeString(args, 0).ToLower();
                var option = Utils.GetSafeString(args, 1).ToLower();

                if (Gui.Controls.TryGetValue(gridName, out FrameworkElement dgFe))
                {
                    if (dgFe is DataGrid)
                    {
                        DataGrid dg = dgFe as DataGrid;

                        switch (option)
                        {
                            case "addrow":

                                if (gridsArrayClass[gridName].tableOrArray == "table")
                                {
                                    //
                                }
                                else if (gridsArrayClass[gridName].tableOrArray == "array")
                                {
                                    var currentRowCount = dg.Items.Count;

                                    // check for max length
                                    if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].maxElemsVarName.ToLower(), out DefineVariable defVar))
                                    {
                                        if (currentRowCount + 1 > defVar.Value)
                                        {
                                            MessageBox.Show("Maximum elements reached.");
                                            return Variable.EmptyInstance;
                                        }
                                    }

                                    // add new array item
                                    foreach (var item in gridsArrayClass[gridName].tags)
                                    {
                                        var tagToLower = item.ToLower();

                                        if (CSCS_GUI.DEFINES.TryGetValue(tagToLower, out DefineVariable defVar2))
                                        {
                                            if (defVar2.Tuple.Count > currentRowCount)
                                            {
                                                defVar2.Tuple[currentRowCount] = new Variable();
                                            }
                                            else
                                            {
                                                defVar2.Tuple.Add(new Variable());
                                            }
                                        }
                                    }


                                    // fill linCntr with new element index
                                    if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].lineCntrVarName.ToLower(), out DefineVariable defVar3))
                                    {
                                        defVar3.InitVariable(new Variable(currentRowCount));
                                    }

                                    // increment active elements variable
                                    if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].actCntrVarName.ToLower(), out DefineVariable defVar4))
                                    {
                                        defVar4.InitVariable(new Variable(defVar4.Value + 1));
                                    }

                                    // add new grid row
                                    var newRow = gridsDataTables[gridName].NewRow();
                                    gridsDataTables[gridName].Rows.Add(newRow);

                                    dg.ScrollIntoView(dg.Items.GetItemAt(dg.Items.Count - 1));
                                    dg.SelectedIndex = dg.Items.Count - 1;
                                }

                                break;

                            case "deleterow":

                                if (gridsArrayClass[gridName].tableOrArray == "table")
                                {
                                    if (deleteFromDB(gridsDataTables[gridName].Rows[dg.SelectedIndex].ItemArray[0].ToString(), gridsOpenvs[gridName]))
                                    {
                                        gridsDataTables[gridName].Rows.RemoveAt(dg.SelectedIndex);
                                    }
                                }
                                else if (gridsArrayClass[gridName].tableOrArray == "array")
                                {
                                    if (deleteFromArrays(dg.SelectedIndex, gridsArrayClass[gridName]))
                                    {
                                        var indexToDelete = dg.SelectedIndex;
                                        gridsDataTables[gridName].Rows.RemoveAt(indexToDelete);
                                        if (dg.Items.Count <= indexToDelete)
                                        {
                                            dg.SelectedIndex = indexToDelete - 1;
                                            if (dg.SelectedItem != null)
                                                dg.ScrollIntoView(dg.SelectedItem);
                                            else
                                            {// ...deleted the last item remained
                                                //add first item do DataTable
                                                var newRow2 = gridsDataTables[gridName].NewRow();
                                                gridsDataTables[gridName].Rows.Add(newRow2);

                                                // add new array item
                                                foreach (var item in gridsArrayClass[gridName].tags)
                                                {
                                                    var tagToLower = item.ToLower();

                                                    if (CSCS_GUI.DEFINES.TryGetValue(tagToLower, out DefineVariable defVar2))
                                                    {
                                                        defVar2.Tuple[0] = new Variable() { };
                                                    }
                                                }

                                                dg.SelectedIndex = 0;
                                            }
                                        }
                                        else
                                        {
                                            dg.SelectedIndex = indexToDelete;
                                            dg.ScrollIntoView(dg.SelectedItem);
                                        }

                                        // decrement active elements variable
                                        if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].actCntrVarName.ToLower(), out DefineVariable defVar5))
                                        {
                                            if ((int)defVar5.Value > 1)
                                            {
                                                defVar5.InitVariable(new Variable(defVar5.Value - 1));
                                            }
                                        }
                                    }
                                }

                                break;

                            case "insertrow":

                                if (gridsArrayClass[gridName].tableOrArray == "table")
                                {
                                    //
                                }
                                else if (gridsArrayClass[gridName].tableOrArray == "array")
                                {
                                    // check for max length
                                    if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].maxElemsVarName.ToLower(), out DefineVariable defVar6))
                                    {
                                        if (dg.Items.Count + 1 > defVar6.Value)
                                        {
                                            MessageBox.Show("Maximum elements reached.");
                                            return Variable.EmptyInstance;
                                        }
                                    }

                                    if (gridsArrayClass[gridName].tableOrArray == "array")
                                    {
                                        if (insertIntoArrays(dg.SelectedIndex + 1, gridsArrayClass[gridName]))
                                        {
                                            gridsDataTables[gridName].Rows.InsertAt(gridsDataTables[gridName].NewRow(), dg.SelectedIndex + 1);

                                            // increment active elements variable
                                            if (CSCS_GUI.DEFINES.TryGetValue(gridsArrayClass[gridName].actCntrVarName.ToLower(), out DefineVariable defVar7))
                                            {
                                                defVar7.InitVariable(new Variable(defVar7.Value + 1));
                                            }

                                            dg.SelectedIndex++;
                                            dg.ScrollIntoView(dg.SelectedItem);
                                        }
                                    }
                                }

                                break;

                            case "aaa": // (test) counts visible rows in datagrid

                                uint VisibleRows = 0;

                                int cnt = 0;
                                foreach (var Item in dg.Items)
                                {
                                    var Row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromItem(Item);

                                    if (Row != null)
                                    {
                                        if (IsUserVisible(Row, dg))
                                        {
                                            cnt++;
                                            Debug.WriteLine("Red " + (Row.Item as DataRowView).Row.ItemArray[1].ToString());
                                        }
                                    }

                                    //if (Row != null)
                                    //{
                                    //    if (Row.TransformToVisual(dg).Transform(new Point(0, 0)).Y + Row.ActualHeight >= dg.ActualHeight)
                                    //    {
                                    //        break;
                                    //    }

                                    //    VisibleRows++;
                                    //}
                                }

                                break;
                            default:
                                break;
                        }
                    }
                }

                return Variable.EmptyInstance;
            }

            private bool insertIntoArrays(int index, DisplayArrayClass dac)
            {
                try
                {
                    foreach (var arrayName in dac.tags)
                    {
                        if (CSCS_GUI.DEFINES.TryGetValue(arrayName.ToLower(), out DefineVariable defVar))
                        {
                            defVar.Tuple.Insert(index, new Variable());
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            private bool deleteFromArrays(int index, DisplayArrayClass dac)
            {
                try
                {
                    foreach (var arrayName in dac.tags)
                    {
                        if (CSCS_GUI.DEFINES.TryGetValue(arrayName.ToLower(), out DefineVariable defVar))
                        {
                            defVar.Tuple.RemoveAt(index);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            private bool deleteFromDB(string rowId, OpenvTable thisOpenv)
            {
                try
                {
                    var query =
$@"EXECUTE sp_executesql N'
DELETE FROM {Databases[thisOpenv.databaseName.ToUpper()]}.dbo.{thisOpenv.tableName}
where ID = {rowId}
'";

                    using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                    {
                        var ret = cmd.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            private bool IsUserVisible(FrameworkElement element, FrameworkElement container)
            {
                if (!element.IsVisible)
                    return false;
                Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
                Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
                return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
            }
        }

    }

}
