using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace WpfCSCS
{
    public enum FindvOption
    {
        First, Last, Next, Previous, MatchExact, Generic /* exact or greater */
    }

    public class CacheLine
    {
        public Dictionary<string, DefineVariable> Line = new Dictionary<string, DefineVariable>(); // one line of the table, string = fieldName, DefineVariable = value
    }

    public class CachingClass
    {
        public string KeyName; // for comparison with another key while "findv next"
        public List<CacheLine> CachedLines; // index = 1, 2, 3 to 300 
                                            //public static int (in) CSCS_GUI.MaxCacheSize, from .exe.config

        public CachingClass()
        {
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

    public class Btrieve
    {
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

        public static Variable OPENV(string tableName, string databaseName, ParsingScript script)
        {
            if (CSCS_SQL.SqlServerConnection.State != System.Data.ConnectionState.Open)
            {
                CSCS_SQL.SqlServerConnection.Open();
            }

            //
            var table = CSCS_GUI.Adictionary.SY_TABLESList.FirstOrDefault(p => p.SYCT_NAME == tableName.ToUpper() && p.SYCT_USERCODE == databaseName.ToUpper());
            if (table == null)
            {
                // error "There's no table with name {tableName.ToUpper()} in database {databaseName.ToUpper()}!"

                SetFlerr(7); // testno ? treba li nam ?
                return new Variable((long)0); // ovo treba, puni hndl s nulom
            }

            var listOfFields = CSCS_GUI.Adictionary.SY_FIELDSList.Where(p => p.SYTD_SCHEMA == table.SYCT_SCHEMA).ToList();

            foreach (var field in listOfFields)
            {
                if (!CSCS_GUI.DEFINES.ContainsKey(field.SYTD_FIELD))
                {
                    DefineVariable newVar = new DefineVariable(field.SYTD_FIELD, null, field.SYTD_TYPE, field.SYTD_SIZE, field.SYTD_DEC, field.SYTD_ARRAYNUM/*, local, up*/);
                    newVar.InitVariable(Variable.EmptyInstance, script);
                }

                //DefineVariable dupVar = null;
                //if (!string.IsNullOrWhiteSpace(dup) && !CSCS_GUI.DEFINES.TryGetValue(dup, out dupVar))
                //{
                //    throw new ArgumentException("Couldn't find variable [" + dup + "]");
                //}

                //var valueStr = value == null ? "" : value.AsString();
                //init = init == null ? Variable.EmptyInstance : init;

                //DefineVariable newVar = null;
                //var parts = name.Split(new char[] { ',' });
                //foreach (var objName in parts)
                //{
                //    newVar = dupVar != null ? new DefineVariable(objName, dupVar, local) :
                //                              new DefineVariable(objName, valueStr, type, size, dec, array, local, up);
                //    newVar.InitVariable(dupVar != null ? dupVar.Init : init, script);
                //}

            }


            // 
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


            //
            var thisFnum = Btrieve.OPENVs.Keys.Count > 0 ? Btrieve.OPENVs.Keys.Max() + 1 : 1;
            Btrieve.OPENVs.Add(thisFnum, new OpenvTable()
            {
                tableName = tableName.ToLower(),
                databaseName = databaseName.ToLower(),
                FieldNames = listOfFields.Select(p => p.SYTD_FIELD).ToList(),
                Keys = listOfKeys,
                Cache = new CachingClass() // treba li to u openv-u - da
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
            }

            ParsingScript script;

            int tableHndlNum;
            string operationType;
            string tableKey;
            OpenvTable thisOpenv;
            string matchExactString;
            string forString;
            string columnsToSelect;
            string keyo;

            public static Dictionary<int, string> nextPrevCachedWhereStrings = new Dictionary<int, string>();//<tableHndlNum, nextPrevCachedWhereString>
            public static Dictionary<int, string> cachedSqlForString = new Dictionary<int, string>();//<tableHndlNum, cachedSqlForString>
            public static Dictionary<int, string> cachedColumnsToSelect = new Dictionary<int, string>();//<tableHndlNum, cachedColumnsToSelect>

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

                thisOpenv = OPENVs[tableHndlNum]; // tu more bit greska, ako programer fula broj(hndl) ?? ili ako tabla nije otvorena(hndl nije dobio broj)

                if (keyNeeded)
                {
                    if (tableKey.StartsWith("@") && int.TryParse(tableKey.TrimStart('@'), out int keyNum))
                    {
                        if (keyNum > 0)
                        {
                            //OPTIMIZIRAT
                            var kljuceviTable = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_SCHEMA == CSCS_GUI.Adictionary.SY_TABLESList.First(r => r.SYCT_NAME == thisOpenv.tableName.ToUpper()).SYCT_SCHEMA).OrderBy(s => s.SYKI_KEYNUM);

                            KeyClass = thisOpenv.Keys.First(p => p.KeyName == kljuceviTable.Where(r => r.SYKI_KEYNUM == keyNum).First().SYKI_KEYNAME);
                        }
                        else
                        {
                            KeyClass = new KeyClass() { KeyName = "ID", Ascending = true, Unique = true, KeyNum = 0, KeyColumns = new Dictionary<string, string>() { { "ID", "" } } };
                        }
                    }
                    else if (!thisOpenv.Keys.Any(p => p.KeyName == tableKey.ToUpper()) /* or ne postoji ključ s tim BROJEM*/)
                    {
                        // "Key does not exist for this table!"
                        SetFlerr(4, tableHndlNum);
                        return Variable.EmptyInstance;
                    }
                    else
                    {
                        KeyClass = thisOpenv.Keys.First(p => p.KeyName == tableKey.ToUpper());
                        //**************
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
                        return findNextOrPrevious(FindvOption.Next);
                    case "p":
                        return findNextOrPrevious(FindvOption.Previous);
                    //match exact
                    case "m":
                        return findMatchExact(FindvOption.MatchExact);
                    //generic -> matchexact or greater
                    case "g":
                        return findGeneric(FindvOption.Generic);
                    default:
                        SetFlerr(1, tableHndlNum); // krivo slovo operacije
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
                    ascDescOption = thisOpenv.lastAscDescOption; // testirat ?? "next" nakon "last"
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

                //using (SqlConnection con = new SqlConnection(CSCS_SQL.ConnectionString))
                //{
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    //con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            //MessageBox.Show("Record not found!");
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

                            while (reader.Read()) // unique???
                            {
                                currentSqlId = (int)reader["ID"];
                                int currentFieldNum = 1; // 0-ti je "ID"
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);
                                    //if(thisOpenv.CurrentKey.KeyColumns.Keys.Any(p=>p.ToUpper() == currentColumnName.ToUpper()))
                                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                    {
                                        KeyClass.KeyColumns[currentColumnName.ToUpper()] = reader[currentColumnName].ToString();
                                    }

                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    {
                                        // err: ta kolona NIJE otvorena u bufferu(DEFINE) sa openv
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
                                            CSCS_GUI.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue), script);
                                            //new MyAssignFunction().DoAssign(script, loweredCurrentColumnName, CSCS_GUI.DEFINES[loweredCurrentColumnName]);
                                            CSCS_GUI.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                        }
                                    }
                                    currentFieldNum++;
                                }

                            }

                            //OPENVs
                            thisOpenv.CurrentKey = KeyClass;// thisOpenv.Keys.First(p => p.KeyName == tableKey);
                            thisOpenv.currentRow = currentSqlId;
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                            Btrieve.OPENVs[tableHndlNum].Cache = new CachingClass() { KeyName = KeyClass.KeyName };
                            Btrieve.OPENVs[tableHndlNum].currentCacheListIndex = 1; // ??
                        }

                    }
                }
                //}

                SetFlerr(0, tableHndlNum); // 0 znači UREDU
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

                for (int j = keySegmentsOrdered.Count/* toliko uvjeta */; j > 0; j--)
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

            private string GetParametersDeclaration()
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

                //var keyUsed = CSCS_GUI.Adictionary.SY_INDEXESList.Where(p => p.SYKI_KEYNAME == thisOpenv.CurrentKey.KeyName).ToList();

                //var numOfParams = keyUsed.Count() + (keyUsed.First().SYKI_UNIQUE == "N" ? 1 : 0);

                //var keySegmentsOrdered = keyUsed.OrderBy(p => p.SYKI_SEGNUM).Select(p => p.SYKI_FIELD).ToList();

                //if (keyUsed.First().SYKI_UNIQUE == "N")
                //{
                //    keySegmentsOrdered.Add("ID");
                //}

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

                    //var numOfParams = keyUsed.Count() + (keyUsed.First().SYKI_UNIQUE == "N" ? 1 : 0);

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

                // segmenti po redu + ID ako NIJE unique
                return pvStringBuilder.ToString();
            }


            private string GetMatchExactWhereString(string[] matchExactValues = null, string forString = null)
            {

                //return $"{KeyClass.KeyColumns.First().Key} = '{matchExactValue}'";
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

                //if (!string.IsNullOrEmpty(forString))
                //{
                //    mStringBuilder.Append(" and (");
                //    mStringBuilder.Append(GetForString(forString));
                //    mStringBuilder.Append(")");
                //}

                return mStringBuilder.ToString();

            }

            private string GetGenericWhereString(string[] matchExactValues = null)
            {
                // IMPLEMENTIRAT !!!!!

                //return $"{KeyClass.KeyColumns.First().Key} = '{matchExactValue}'";
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

                for (int j = keySegmentsOrdered.Count/* toliko uvjeta */; j > 0; j--)
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
                // fali implementacija za datum s točkom (12.12.1995.)
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

            private Variable findNextOrPrevious(FindvOption option)
            {
                int currentSqlId = thisOpenv.currentRow;

                //if (thisOpenv.Cache.KeyName == thisOpenv.CurrentKey.KeyName)
                //{
                //    // ako postoji u cache-u
                //    if (option == FindvOption.Next && thisOpenv.Cache.CachedLines.Count > thisOpenv.currentCacheListIndex)
                //    {
                //        //izvuci iz cachea NEXT
                //        int wantedIndex = thisOpenv.currentCacheListIndex + 1;

                //        return new Variable((long)0); // 0 znači UREDU
                //    }
                //    else if (option == FindvOption.Previous && thisOpenv.currentCacheListIndex > 1 && thisOpenv.Cache.CachedLines.Count > 1)
                //    {
                //        //izvuci iz cachea PREVIOUS
                //        int wantedIndex = thisOpenv.currentCacheListIndex - 1;

                //        return new Variable((long)0); // 0 znači UREDU
                //    }
                //}


                //string compareSign = GetCompareSign(option);

                //string parameters = GetParameters(option/*, forString */); // dodat forString

                int numOfRowsToSelect = 1;

                //if (thisOpenv.Cache.CachedLines.Count > 0)
                //{
                //    numOfRowsToSelect = thisOpenv.Cache.CachedLines.Count * 3;
                //    if (numOfRowsToSelect > MaxCacheSize) { numOfRowsToSelect = MaxCacheSize; }
                //}

                //BRIŠI
                //numOfRowsToSelect = 1;

                //use {thisOpenv.databaseName};
                //                var query =
                //    $@"EXECUTE sp_executesql N'
                //use {Databases[thisOpenv.databaseName.ToUpper()]};
                //select top {numOfRowsToSelect}
                //* from {thisOpenv.tableName} 
                // where {whereString}  
                //order by {orderByString}
                //'";

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

                    string paramsDeclaration = GetParametersDeclaration();

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

                //using (SqlConnection con = new SqlConnection(CSCS_SQL.ConnectionString))
                //{
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    //con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            // "Record not found!"
                            SetFlerr(3, tableHndlNum); // 3 ?!?! ne
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

                            while (reader.Read()) // unique???
                            {
                                Dictionary<string, DefineVariable> cacheLine = new Dictionary<string, DefineVariable>();


                                if (firstPass)
                                    currentSqlId = (int)reader["ID"];

                                int currentFieldNum = 1; // 0-ti je "ID"
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);
                                    //if(thisOpenv.CurrentKey.KeyColumns.Keys.Any(p=>p.ToUpper() == currentColumnName.ToUpper()))

                                    //if (firstPass)
                                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                    {
                                        KeyClass.KeyColumns[currentColumnName.ToUpper()] = reader[currentColumnName].ToString();
                                    }


                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    {
                                        // err: ta kolona NIJE otvorena u bufferu(DEFINE) sa openv
                                        //SetFlerr( ?, tableHndlNum);
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
                                            CSCS_GUI.OnVariableChange(loweredCurrentColumnName, newVar, true);

                                            //var copy = CSCS_GUI.DEFINES[loweredCurrentColumnName];
                                            //cacheLine.Add(loweredCurrentColumnName, copy);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue));
                                            CSCS_GUI.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);

                                            //cacheLine.Add(loweredCurrentColumnName, CSCS_GUI.DEFINES[loweredCurrentColumnName]);

                                            //if (CSCS_GUI.DEFINES.TryGetValue(loweredCurrentColumnName, out DefineVariable copy)){
                                            //    cacheLine.Add(loweredCurrentColumnName, copy.Clone);
                                            //}

                                            //cacheLine.add[loweredCurrentColumnName] = copy;
                                        }
                                    }

                                    //if (firstPass)
                                    //    CSCS_GUI.DEFINES[loweredCurrentColumnName] = cacheLine[loweredCurrentColumnName];

                                    currentFieldNum++;
                                }

                                firstPass = false;

                                // NEBOJSA: SVI ELEMENTI SU ISTI !! ??
                                //thisOpenv.Cache.CachedLines.Add(new CacheLine() { Line = cacheLine });


                            }

                            //OPENVs
                            //thisOpenv.CurrentKey = thisOpenv.Keys.First(p => p.KeyName == tableKey.ToUpper());
                            thisOpenv.currentRow = currentSqlId;
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                        }

                    }
                }
                //}

                lastUsedPreviousOrNext[tableHndlNum] = option;

                SetFlerr(0, tableHndlNum); // 0 znači UREDU
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
                    SetFlerr(99, tableHndlNum); // nejednak broj segmenata kljuca i prilozenih vrijednosti
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

                //using (SqlConnection con = new SqlConnection(CSCS_SQL.ConnectionString))
                //{
                using (SqlCommand cmd = new SqlCommand(query, CSCS_SQL.SqlServerConnection))
                {
                    //con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            // "Record not found!"
                            SetFlerr(3, tableHndlNum); // 3 ?!?!? ne
                            return Variable.EmptyInstance;
                        }
                        else
                        {
                            if (keyo == "keyo")
                            {
                                SetFlerr(0, tableHndlNum);
                                return Variable.EmptyInstance;
                            }

                            while (reader.Read()) // unique???
                            {
                                currentSqlId = (int)reader["ID"];
                                int currentFieldNum = 1; // 0-ti je "ID"
                                while (currentFieldNum < reader.FieldCount)
                                {
                                    var currentColumnName = reader.GetName(currentFieldNum);
                                    //if(thisOpenv.CurrentKey.KeyColumns.Keys.Any(p=>p.ToUpper() == currentColumnName.ToUpper()))
                                    if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                    {
                                        KeyClass.KeyColumns[currentColumnName.ToUpper()] = reader[currentColumnName].ToString();
                                    }

                                    var loweredCurrentColumnName = currentColumnName.ToLower();
                                    if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                    {
                                        // err: ta kolona NIJE otvorena u bufferu(DEFINE) sa openv
                                        return new Variable((long)4); // SetFlerr( ?, tableHndlNum);
                                    }
                                    else
                                    {
                                        if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                        {
                                            DateTime fieldValue = (DateTime)reader[currentColumnName];
                                            var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                            var newVar = new Variable(fieldValue.ToString(dateFormat));
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(newVar);
                                            CSCS_GUI.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue));
                                            CSCS_GUI.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                        }
                                    }
                                    currentFieldNum++;
                                }

                            }

                            //OPENVs
                            //thisOpenv.CurrentKey = thisOpenv.Keys.First(p => p.KeyName == tableKey.ToUpper());
                            thisOpenv.CurrentKey = KeyClass;
                            thisOpenv.currentRow = currentSqlId;
                            Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                        }

                    }
                }
                //}

                SetFlerr(0, tableHndlNum);
                return Variable.EmptyInstance; // 0 znači UREDU
            }

            public Variable clearBuffer(OpenvTable thisOpenv)
            {
                //var listOfFields = CSCS_GUI.Adictionary.SY_FIELDSList.Where(p => p.SYTD_SCHEMA == table.SYCT_SCHEMA).ToList();

                //foreach (var field in listOfFields)
                //{
                //    if (!CSCS_GUI.DEFINES.ContainsKey(field.SYTD_FIELD))
                //    {
                //        DefineVariable newVar = new DefineVariable(field.SYTD_FIELD, null, field.SYTD_TYPE, field.SYTD_SIZE, field.SYTD_DEC, field.SYTD_ARRAYNUM/*, local, up*/);
                //        newVar.InitVariable(Variable.EmptyInstance);
                //    }
                //}

                foreach (var bufferField in thisOpenv.FieldNames)
                {
                    var field = bufferField.ToLower();
                    if (CSCS_GUI.DEFINES.ContainsKey(field))
                    {
                        //if (CSCS_GUI.DEFINES[bufferField]..GetFieldType(currentFieldNum) == typeof(DateTime))
                        //{
                        //    DateTime fieldValue = (DateTime)reader[currentColumnName];
                        //    var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                        //    CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue.ToString(dateFormat)));
                        //}
                        //else
                        //{
                        //string fieldValue = CSCS_GUI.DEFINES[bufferField].ToString().TrimEnd();
                        CSCS_GUI.DEFINES[field].InitVariable(Variable.EmptyInstance);
                        CSCS_GUI.OnVariableChange(field, Variable.EmptyInstance, true);
                        //}
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
                    SetFlerr(99, tableHndlNum); // nejednak broj segmenata kljuca i prilozenih vrijednosti
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

                string whereString = GetGenericWhereString(matchExactValues);

                string orderByString = GetOrderByString(option, thisOpenv, KeyClass);

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

                using (SqlConnection con = new SqlConnection(CSCS_SQL.ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                // "Record not found!"
                                SetFlerr(3, tableHndlNum); // 3 ?!?!? ne
                                return Variable.EmptyInstance;
                            }
                            else
                            {
                                if (keyo == "keyo")
                                {
                                    SetFlerr(0, tableHndlNum);
                                    return Variable.EmptyInstance;
                                }

                                while (reader.Read()) // unique???
                                {
                                    currentSqlId = (int)reader["ID"];
                                    int currentFieldNum = 1; // 0-ti je "ID"
                                    while (currentFieldNum < reader.FieldCount)
                                    {
                                        var currentColumnName = reader.GetName(currentFieldNum);
                                        //if(thisOpenv.CurrentKey.KeyColumns.Keys.Any(p=>p.ToUpper() == currentColumnName.ToUpper()))
                                        if (KeyClass.KeyColumns.Keys.Any(p => p.ToUpper() == currentColumnName.ToUpper()))
                                        {
                                            KeyClass.KeyColumns[currentColumnName.ToUpper()] = reader[currentColumnName].ToString();
                                        }

                                        var loweredCurrentColumnName = currentColumnName.ToLower();
                                        if (!CSCS_GUI.DEFINES.ContainsKey(loweredCurrentColumnName))
                                        {
                                            // err: ta kolona NIJE otvorena u bufferu(DEFINE) sa openv
                                            return new Variable((long)4); // SetFlerr( ?, tableHndlNum);
                                        }
                                        else
                                        {
                                            if (reader.GetFieldType(currentFieldNum) == typeof(DateTime))
                                            {
                                                DateTime fieldValue = (DateTime)reader[currentColumnName];
                                                var dateFormat = CSCS_GUI.DEFINES[loweredCurrentColumnName].GetDateFormat();
                                                var newVar = new Variable(fieldValue.ToString(dateFormat));
                                                CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(newVar);
                                                CSCS_GUI.OnVariableChange(loweredCurrentColumnName, newVar, true);
                                            }
                                            else
                                            {
                                                string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                                CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue));
                                                CSCS_GUI.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
                                            }
                                        }
                                        currentFieldNum++;
                                    }

                                }

                                //OPENVs
                                //thisOpenv.CurrentKey = thisOpenv.Keys.First(p => p.KeyName == tableKey.ToUpper());
                                thisOpenv.CurrentKey = KeyClass;
                                thisOpenv.currentRow = currentSqlId;
                                Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                            }

                        }
                    }
                }

                SetFlerr(0, tableHndlNum);
                return Variable.EmptyInstance; // 0 znači UREDU
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

                return Btrieve.OPENV(tableName, databaseName, script);
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
            int tableHndlNum;
            string operationType;

            OpenvTable thisOpenv;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);
                tableHndlNum = Utils.GetSafeInt(args, 0);
                operationType = Utils.GetSafeString(args, 1).ToLower(); // B -> buffer and recordNumber(ID)(thisOpenv.currentRow) / R -> ONLY recordNumber

                Clear();

                return Variable.EmptyInstance;
            }

            private void Clear()
            {
                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                if (operationType == "b")
                {
                    //clear buffer
                    new Btrieve.FINDVClass(tableHndlNum, "x").clearBuffer(thisOpenv);
                }

                if (operationType == "b" || operationType == "r")
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

                RcnSet();

                return Variable.EmptyInstance;
            }

            private Variable RcnSet()
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
                            return new Variable((long)3);
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
                                            CSCS_GUI.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue.ToString(dateFormat)), true);
                                        }
                                        else
                                        {
                                            string fieldValue = reader[currentColumnName].ToString().TrimEnd();
                                            CSCS_GUI.DEFINES[loweredCurrentColumnName].InitVariable(new Variable(fieldValue));
                                            CSCS_GUI.OnVariableChange(loweredCurrentColumnName, new Variable(fieldValue), true);
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
                Utils.CheckArgs(args.Count, 2, m_name);
                tableHndlNum = Utils.GetSafeInt(args, 0);
                noPrompt = Utils.GetSafeVariable(args, 1/*, new Variable(false)*/).AsBool(); // true -> noPrompt, false -> prompt("are you sure...?")

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
                    new Btrieve.FINDVClass(tableHndlNum, "x").clearBuffer(thisOpenv);

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
            int tableHndlNum;
            bool noPrompt;
            bool noClr;

            OpenvTable thisOpenv;

            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);
                tableHndlNum = Utils.GetSafeInt(args, 0);
                noPrompt = Utils.GetSafeVariable(args, 1).AsBool(); // true -> noPrompt, false -> prompt("are you sure...?")
                noClr = Utils.GetSafeVariable(args, 2, new Variable(false)).AsBool(); //

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                bool clear = false;

                if (!noPrompt)
                {
                    if (MessageBoxResult.No == MessageBox.Show("Are you sure you want to save the current record?", "Caution", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    {
                        return Variable.EmptyInstance;
                    }
                }
                if (thisOpenv.currentRow > 0)
                {
                    if (UpdateCurrentRecord())
                        clear = true;
                }
                else
                {
                    if (InsertNewRecord())
                        clear = true;
                }

                if (clear && !noClr)
                {
                    //clear buffer
                    new Btrieve.FINDVClass(tableHndlNum, "x").clearBuffer(thisOpenv);

                    //clear ID
                    thisOpenv.currentRow = 0;
                    Btrieve.OPENVs[tableHndlNum] = thisOpenv;
                }

                return Variable.EmptyInstance;
            }

            private bool InsertNewRecord()
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

            private bool UpdateCurrentRecord()
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
                    else if (!thisOpenv.Keys.Any(p => p.KeyName == tableKey.ToUpper()) /* or the key with this number does not exist */)
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
                    // if has start string
                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, startString).FINDV();
                }
                else
                {
                    // doesnt have start string
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

                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, currentStart).FINDV();
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
                        new Btrieve.FINDVClass(tableHndlNum, "n").FINDV();
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

                    new Btrieve.FINDVClass(tableHndlNum, "n").FINDV();
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
            ParsingScript from;
            string to;
            int tableHndlNum;

            string tableKey;

            ParsingScript forString;

            string cntrNameString;



            OpenvTable thisOpenv;
            KeyClass KeyClass;

            ParsingScript Script;

            double rowsAffected = 0; //

            int rowNumber = 0; //

            protected override Variable Evaluate(ParsingScript script)
            {
                Script = script;
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 6, m_name);

                from = script.GetTempScript(args[0].ToString()); // cols from DB
                to = Utils.GetSafeString(args, 1); // arrays to fill

                tableHndlNum = Utils.GetSafeInt(args, 2);

                var recaArrayName = Utils.GetSafeString(args, 3);
                var maxa = Utils.GetSafeInt(args, 4);

                forString = script.GetTempScript(args[5].ToString()); // 
                cntrNameString = Utils.GetSafeString(args, 6).ToLower(); //

                thisOpenv = Btrieve.OPENVs[tableHndlNum];

                bool limited = false;
                int selectLimit = 0;
                int selectLimitCounter = 0;
                if (maxa != 0)
                {
                    selectLimit = maxa;
                    limited = true;
                    selectLimitCounter = selectLimit;
                }

                bool forIsSet = false;
                if (!string.IsNullOrEmpty(forString.String))
                    forIsSet = true;

                while (true)
                {
                    if (forIsSet && !script.GetTempScript(forString.String).Execute(new char[] { '"' }, 0).AsBool())
                    {
                        new Btrieve.FINDVClass(tableHndlNum, "n").FINDV();
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

                    new Btrieve.FINDVClass(tableHndlNum, "n").FINDV();
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
                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, startString/*, forString.String*/).FINDV();
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


                    new Btrieve.FINDVClass(tableHndlNum, "g", tableKey, currentStart).FINDV();
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
                        new Btrieve.FINDVClass(tableHndlNum, "n").FINDV();
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

                    new Btrieve.FINDVClass(tableHndlNum, "n").FINDV();
                    if (LastFlerrsOfFnums[tableHndlNum] == 3)
                    {
                        SetFlerr(0, tableHndlNum);
                        break;
                    }
                }

                if (CSCS_GUI.DEFINES.TryGetValue(cntrNameString, out DefineVariable currentDefVar))
                {
                    currentDefVar.InitVariable(new Variable(rowsAffected));
                    CSCS_GUI.OnVariableChange(cntrNameString, new Variable(rowsAffected), true);
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

                SetFlerr(0, tableHndlNum); // 0 meanbs OK
                return Variable.EmptyInstance;
            }
        }

    }

}
