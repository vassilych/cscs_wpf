using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;
using OfficeOpenXml;
using Spire.Xls;
using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfCSCS
{
    public class Excel
    {
        public static ExcelPackage document;


        public static void Init(Interpreter interpreter)
        {
            interpreter.RegisterFunction(Constants.SQL_TO_XLSX, new SqlToXlsxFunction());

            interpreter.RegisterFunction(Constants.X_FILE_NEW, new XFileNewFunction());
            interpreter.RegisterFunction(Constants.X_FILE_OPEN, new XFileOpenFunction());
            interpreter.RegisterFunction(Constants.X_FILE_SAVE, new XFileSaveFunction());
            interpreter.RegisterFunction(Constants.X_FILE_SAVE_AS, new XFileSaveAsFunction());
            interpreter.RegisterFunction(Constants.X_FILE_DELETE, new XFileDeleteFunction());
            interpreter.RegisterFunction(Constants.X_SHEET_ADD, new XSheetAddFunction());
            interpreter.RegisterFunction(Constants.X_SHEET_DELETE, new XSheetDeleteFunction());
            interpreter.RegisterFunction(Constants.X_SHEET_COUNT, new XSheetCountFunction());
            interpreter.RegisterFunction(Constants.X_SHEET_CLEAR, new XSheetClearFunction());
            interpreter.RegisterFunction(Constants.X_SHEET_RENAME, new XSheetRenameFunction());
            interpreter.RegisterFunction(Constants.X_CELL_WRITE_STRING, new XCellWriteStringFunction());
            interpreter.RegisterFunction(Constants.X_CELL_WRITE_INTEGER, new XCellWriteIntegerFunction());
            interpreter.RegisterFunction(Constants.X_CELL_WRITE_DOUBLE, new XCellWriteDoubleFunction());
            interpreter.RegisterFunction(Constants.X_CELL_WRITE_BOOLEAN, new XCellWriteBooleanFunction());
            interpreter.RegisterFunction(Constants.X_CELL_WRITE_TIME, new XCellWriteTimeFunction());
            interpreter.RegisterFunction(Constants.X_CELL_WRITE_DATE, new XCellWriteDateFunction());
            interpreter.RegisterFunction(Constants.X_CELL_WRITE_DATETIME, new XCellWriteDateTimeFunction());
            interpreter.RegisterFunction(Constants.X_CELL_WRITE_FORMULA, new XCellWriteFormulaFunction());
            interpreter.RegisterFunction(Constants.X_CELL_READ_STRING, new XCellReadStringFunction());
            interpreter.RegisterFunction(Constants.X_CELL_READ_INTEGER, new XCellReadIntegerFunction());
            interpreter.RegisterFunction(Constants.X_CELL_READ_DOUBLE, new XCellReadDoubleFunction());
            interpreter.RegisterFunction(Constants.X_CELL_READ_BOOLEAN, new XCellReadBooleanFunction());
            interpreter.RegisterFunction(Constants.X_CELL_READ_TIME, new XCellReadTimeFunction());
            interpreter.RegisterFunction(Constants.X_CELL_READ_DATE, new XCellReadDateFunction());
            interpreter.RegisterFunction(Constants.X_CELL_READ_FORMULA, new XCellReadFormulaFunction());
            interpreter.RegisterFunction(Constants.X_CELL_EMPTY, new XCellEmptyFunction());
            interpreter.RegisterFunction(Constants.X_FIND_SHEET, new XFindSheetFunction());
            interpreter.RegisterFunction(Constants.X_FIND_COLUMN, new XFindColumnFunction());
        }
    }

    #region done

    class SqlToXlsxFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var dbShortName = Utils.GetSafeString(args, 0).ToLower();
            var query = Utils.GetSafeString(args, 1);
            var xlsxFileName = Utils.GetSafeString(args, 2);

            var db = CSCS_GUI.Adictionary.SY_DATABASESList.FirstOrDefault(p => p.SYCD_USERCODE.ToLower() == dbShortName.ToLower());
            string dbFullName = "";
            if (db != null)
            {
                dbFullName = db.SYCD_DBASENAME;
            }
            query = $"use {dbFullName};\n" + query;

            var gui = CSCS_GUI.GetInstance(script);
            using (SqlCommand cmd = new SqlCommand(query, gui.SQLInstance.SqlServerConnection))
            {
                if (cmd.Connection.State != ConnectionState.Open)
                {
                    cmd.Connection.Open();
                }

                SqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    var values = new List<Dictionary<string, object>>();

                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();

                        var numOfColumns = reader.FieldCount;
                        int i = 0;
                        while (numOfColumns > i)
                        {
                            if (reader.GetName(i).ToLower() == "id")
                            {
                                i++;
                                continue;
                            }

                            if (reader.GetDataTypeName(i) == "date")
                            {
                                row.Add(reader.GetName(i), ((DateTime)reader[i]).ToString("d"));
                            }
                            else
                            {
                                row.Add(reader.GetName(i), reader[i].ToString());
                            }

                            i++;
                        }
                        values.Add(row);
                    }

                    //var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), xlsxFileName);
                    var path = xlsxFileName;
                    path = generateFilenameVersionNumberInPath(path);

                    MiniExcel.SaveAs(path, values, true, "Sheet1", ExcelType.XLSX, null /*config*/, true);

                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(path);
                    Worksheet worksheet = workbook.Worksheets[0];
                    worksheet.FreezePanes(2, 1);
                    worksheet.AllocatedRange.AutoFitColumns();
                    //worksheet.AllocatedRange.AutoFitRows();
                    workbook.SaveToFile(path, FileFormat.Version2010);
                }
                finally
                {
                    reader.Close();
                }
            }

            return Variable.EmptyInstance;
        }

        private string generateFilenameVersionNumberInPath(string path)
        {
            if (!path.ToLower().EndsWith(".xlsx"))
            {
                path += ".xlsx";
            }

            int x = 1;
            if (File.Exists(path))
            {
                path = path.Remove(path.Length - 5, 5);
                path += $"({x}).xlsx";
                x++;
                while (File.Exists(path))
                {
                    path = path.Remove(path.Length - 8, 8);
                    path += $"({x}).xlsx";
                    x++;
                }
            }

            return path;
        }
    }

    class XFileNewFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileName = Utils.GetSafeString(args, 0);

            String strFilename;
            FileInfo newFile;
            try
            {
                // convert char[] to string
                int n = Array.IndexOf<char>(fileName.ToArray(), '\0');
                if (n == -1)
                    n = fileName.Length;
                strFilename = new String(fileName.ToArray(), 0, n);

                newFile = new FileInfo(strFilename);

                // if document exist, delete it and create new
                if (newFile.Exists)
                {
                    newFile.Delete();
                    newFile = new FileInfo(strFilename);
                }

                // kreate ExcelPackage reference
                ExcelPackage document = new ExcelPackage(newFile);

                // create at least 1 worksheet (without at least 1 worksheet, document cannot be saved)
                ExcelWorksheet sheet = document.Workbook.Worksheets.Add("Sheet1");

                // save dokument
                document.Save();

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message + e.StackTrace, "FileNew");
                return new Variable(false);
            }
        }
    }

    class XFileOpenFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileName = Utils.GetSafeString(args, 0);

            String strFilename;
            FileInfo newFile;
            try
            {
                // convert char[] to string
                int n = Array.IndexOf<char>(fileName.ToArray(), '\0');
                if (n == -1)
                    n = fileName.Length;
                strFilename = new String(fileName.ToArray(), 0, n);

                newFile = new FileInfo(strFilename);

                // if document does not exist, get message
                if (!newFile.Exists)
                {
                    MessageBox.Show("Document: '" + strFilename + "' does not exist!", "FileOpen");
                    return new Variable(false);
                }

                // create ExcelPackage reference
                Excel.document = new ExcelPackage(newFile);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FileOpen");
                return new Variable(false);
            }
        }
    }

    class XFileSaveFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name);

            try
            {
                // save document
                Excel.document.Save();

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message + e.StackTrace, "FileSave");
                return new Variable(false);
            }
        }
    }

    class XFileSaveAsFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileNamePath = Utils.GetSafeString(args, 0);

            String strFilenamePath;
            try
            {
                int n = Array.IndexOf<char>(fileNamePath.ToArray(), '\0');
                if (n == -1)
                    n = fileNamePath.Length;
                strFilenamePath = new String(fileNamePath.ToArray(), 0, n);

                // save document
                Excel.document.SaveAs(new System.IO.FileInfo(strFilenamePath));

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message + e.StackTrace, "FileSaveAs");
                return new Variable(false);
            }
        }
    }

    class XFileDeleteFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileName = Utils.GetSafeString(args, 0);

            String strFilename;
            FileInfo newFile;
            try
            {
                // convert char[] to string
                int n = Array.IndexOf<char>(fileName.ToArray(), '\0');
                if (n == -1)
                    n = fileName.Length;
                strFilename = new String(fileName.ToArray(), 0, n);

                newFile = new FileInfo(strFilename);

                // if document does not exist, get message
                if (!newFile.Exists)
                {
                    MessageBox.Show("Document: '" + strFilename + "' does not exist!", "FileDelete");
                    return new Variable(false);
                }

                // delete dokument
                newFile.Delete();

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FileDelete");
                return new Variable(false);
            }
        }
    }

    class XSheetAddFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var sheetName = Utils.GetSafeString(args, 0);

            String strSheetName;
            int docSheetCount;
            try
            {
                // convert char[] to string
                int n = Array.IndexOf<char>(sheetName.ToArray(), '\0');
                if (n == -1)
                    n = sheetName.Length;
                strSheetName = new String(sheetName.ToArray(), 0, n);

                // get number of worksheets
                docSheetCount = Excel.document.Workbook.Worksheets.Count;

                // add new specific worksheet
                Excel.document.Workbook.Worksheets.Add(strSheetName);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetAdd");
                return new Variable(false);
            }
        }
    }

    class XSheetDeleteFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var sheetNum = Utils.GetSafeInt(args, 0);

            int Sheet = sheetNum;
            try
            {
                // delete specific worksheet
                Excel.document.Workbook.Worksheets.Delete(Sheet);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetDelete");
                return new Variable(false);
            }
        }
    }

    class XSheetCountFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name);

            int numSheets;
            try
            {
                // find number of worksheets in document
                numSheets = Excel.document.Workbook.Worksheets.Count;

                return new Variable(numSheets);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetCount");
                return new Variable(-1);
            }
        }
    }

    class XSheetClearFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var sheetIndex = Utils.GetSafeInt(args, 0);

            try
            {
                // convert byte[] to int
                int indexSheet = sheetIndex;

                // check if sheet index exist
                if (indexSheet > Excel.document.Workbook.Worksheets.Count)
                {
                    MessageBox.Show("Cannot clear " + indexSheet + ". worksheet because not exist in file: " + Excel.document.File.Name, "SheetClear");
                    return new Variable(false);
                }

                // get specific worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[indexSheet];

                string SheetName = sheet.Name;
                /*
                                // clear all worksheet
                                sheet.Cells[sheet.Dimension.Start.Row, sheet.Dimension.Start.Column, sheet.Dimension.End.Row, sheet.Dimension.End.Column].Value = null;

                                // if tables exist, remove
                                if (sheet.Tables.Count > 0)
                                {
                                    foreach (var table in sheet.Tables)
                                    {
                                        sheet.Names.Remove(table.Name);
                                    }
                                }
                */
                // delete sheet
                Excel.document.Workbook.Worksheets.Delete(sheet);

                // create new with equal name
                Excel.document.Workbook.Worksheets.Add(SheetName);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetClear");
                return new Variable(false);
            }
        }
    }

    class XSheetRenameFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var sheetIndex = Utils.GetSafeInt(args, 0);
            var sheetRename = Utils.GetSafeString(args, 1);

            try
            {
                int indexSheet = sheetIndex;

                string strSheetName = sheetRename;

                // get specific worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[indexSheet];

                // rename worksheet
                sheet.Name = strSheetName;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetRename");
                return new Variable(false);
            }
        }
    }

    class XCellWriteStringFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 5, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var valueLength = Utils.GetSafeInt(args, 3);
            var value = Utils.GetSafeString(args, 4);

            try
            {
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;
                int valLength = valueLength;

                // convert char[] to string
                // specific for writting empty strings
                String strValue = new String(value.ToArray(), 0, valLength);

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // write string on that worksheet in specific cell
                sheet.Cells[docRow, docColumn].Value = @strValue;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteString");
                return new Variable(false);
            }
        }
    }

    class XCellWriteIntegerFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeInt(args, 3);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;
                int intValue = value;

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // write integer on that worksheet in specific cell
                sheet.Cells[docRow, docColumn].Value = intValue;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteInteger");
                return new Variable(false);
            }
        }
    }

    class XCellWriteDoubleFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeDouble(args, 3);
            var charLength = Utils.GetSafeInt(args, 4); //optional
            var charValue = Utils.GetSafeString(args, 5); //optional

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;
                // convert byte[] to double
                double doubleValue = value;

                int charLen = charLength;
                String format = null;
                try
                {
                    format = new String(charValue.ToArray(), 0, charLen);
                }
                catch (Exception)
                {
                    format = null;
                }

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // format cell
                if (!String.IsNullOrWhiteSpace(format))
                    sheet.Cells[docRow, docColumn].Style.Numberformat.Format = format;
                else
                    sheet.Cells[docRow, docColumn].Style.Numberformat.Format = "General";

                // write double on that worksheet in specific cell
                sheet.Cells[docRow, docColumn].Value = doubleValue;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteDouble");
                return new Variable(false);
            }
        }
    }

    class XCellWriteBooleanFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeVariable(args, 3).AsBool();

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;
                //  convert byte[] to boolean
                bool boolValue = value;

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // write boolean on that worksheet in specific cell
                sheet.Cells[docRow, docColumn].Value = boolValue;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteBoolean");
                return new Variable(false);
            }
        }
    }

    class XCellWriteTimeFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var timeVariableName = Utils.GetSafeString(args, 3).ToLower();
            //var timeSec = Utils.GetSafeInt(args, 3);
            //var timeMin = Utils.GetSafeInt(args, 4);
            //var timeHour = Utils.GetSafeInt(args, 5);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                //var varDateTime = Variable.AsDateTime();

                if(!(script.Context as CSCS_GUI).DEFINES.TryGetValue(timeVariableName, out DefineVariable timeVar))
                {
                    MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {timeVariableName} not found!", "CellWriteTime");
                    return new Variable(false);
                }
                else
                {
                    var varDateTime = timeVar.DateTime;
                    int seconds = varDateTime.Second;
                    int minutes = varDateTime.Minute;
                    int hours = varDateTime.Hour;

                    // find worksheet
                    ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                    // create time object
                    TimeSpan time = new TimeSpan(hours, minutes, seconds);

                    // format cell as 'time'
                    sheet.Cells[docRow, docColumn].Style.Numberformat.Format = "hh:mm:ss";

                    // write time on that worksheet in specific cell
                    sheet.Cells[docRow, docColumn].Value = time;

                    return new Variable(true);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteTime");
                return new Variable(false);
            }
        }
    }

    class XCellWriteDateFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var dateVariableName = Utils.GetSafeString(args, 3).ToLower();

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(dateVariableName, out DefineVariable dateVar))
                {
                    MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {dateVariableName} not found!", "CellWriteDate");
                    return new Variable(false);
                }
                else
                {
                    var varDateTime = dateVar.DateTime;
                    int day = varDateTime.Day;
                    int month = varDateTime.Month;
                    int year = varDateTime.Year;

                    // create dateTime instance
                    DateTime date = new DateTime(year, month, day);

                    // find worksheet
                    ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                    // format cell as 'date'
                    sheet.Cells[docRow, docColumn].Style.Numberformat.Format = "dd-mm-yyyy";

                    // write date on that worksheet in specific cell
                    sheet.Cells[docRow, docColumn].Value = date.Date;

                    return new Variable(true);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteDate");
                return new Variable(false);
            }
        }
    }

    class XCellWriteDateTimeFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 5, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var dateVariableName = Utils.GetSafeString(args, 3).ToLower();
            var timeVariableName = Utils.GetSafeString(args, 4).ToLower();

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(dateVariableName, out DefineVariable dateVar))
                {
                    MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {dateVariableName} not found!", "CellWriteDateTime");
                    return new Variable(false);
                }
                else
                {
                    if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(timeVariableName, out DefineVariable timeVar))
                    {
                        MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {timeVariableName} not found!", "CellWriteDateTime");
                        return new Variable(false);
                    }
                    else
                    {
                        var varDateDateTime = dateVar.DateTime;
                        var varTimeDateTime = timeVar.DateTime;
                        int day = varDateDateTime.Day;
                        int month = varDateDateTime.Month;
                        int year = varDateDateTime.Year;
                        int seconds = varTimeDateTime.Second;
                        int minutes = varTimeDateTime.Minute;
                        int hours = varTimeDateTime.Hour;

                        // create dateTime instance
                        DateTime dateTime = new DateTime(year, month, day, hours, minutes, seconds);

                        // find worksheet
                        ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                        // format cell as 'date and time'
                        sheet.Cells[docRow, docColumn].Style.Numberformat.Format = "dd-mm-yyyy hh:mm:ss";

                        // write date and time on that worksheet in specific cell
                        sheet.Cells[docRow, docColumn].Value = dateTime;

                        return new Variable(true);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteDateTime");
                return new Variable(false);
            }
        }
    }

    class XCellWriteFormulaFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeString(args, 3);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;
                // convert char[] to string
                int n = Array.IndexOf<char>(value.ToArray(), '\0');
                if (n == -1)
                    n = value.Length;
                String strValue = new String(value.ToArray(), 0, n);

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // write formula on that worksheet in specific cell
                sheet.Cells[docRow, docColumn].Formula = strValue;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteFormula");
                return new Variable(false);
            }
        }
    }

    class XCellReadStringFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // read to string on that worksheet in specific cell
                string strValue = Convert.ToString(sheet.Cells[docRow, docColumn].Value);

                // get array size
                int strValueLength = strValue.ToCharArray().Length;

                return new Variable(strValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadString");

                return new Variable("");
            }
        }
    }

    class XCellReadIntegerFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // convert byte[] u int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // read to integer on that worksheet in specific cell
                int intValue = Convert.ToInt32(sheet.Cells[docRow, docColumn].Value);

                // return value
                return new Variable(intValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadInteger");
                return new Variable(0);
            }
        }
    }

    class XCellReadDoubleFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // read to double on that worksheet in specific cell
                double doubleValue = Convert.ToDouble(sheet.Cells[docRow, docColumn].Value);

                // retrun value
                return new Variable(doubleValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadDouble");
                return new Variable((double)0);
            }
        }
    }

    class XCellReadBooleanFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                //find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // read to boolean on that worksheet in specific cell
                bool boolValue = Convert.ToBoolean(sheet.Cells[docRow, docColumn].Value);

                // return value
                return new Variable(boolValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadBoolean");
                return new Variable(false);
            }
        }
    }

    class XCellReadTimeFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // get time on that worksheet in specific cell
                //DateTime timeValue = Convert.ToDateTime(sheet.Cells[docRow, docColumn].Value);
                TimeSpan timeValue = TimeSpan.Parse(sheet.Cells[docRow, docColumn].Text);

                //// get parts
                //byte hours = Convert.ToByte(timeValue.Hours);
                //byte minutes = Convert.ToByte(timeValue.Minutes);
                //byte seconds = Convert.ToByte(timeValue.Seconds);

                // return values
                //return new Variable(new DateTime(1, 1, 1, timeValue.Hours, timeValue.Minutes, timeValue.Seconds));
                return new Variable(timeValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadTime");
                return new Variable(new TimeSpan(0, 0, 0));
            }
        }
    }

    class XCellReadDateFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // get time on that worksheet in specific cell
                //DateTime dateValue = Convert.ToDateTime(sheet.Cells[docRow, docColumn].Value);
                DateTime dateValue = DateTime.Parse(sheet.Cells[docRow, docColumn].Text);

                //// get parts
                //byte days = Convert.ToByte(dateValue.Day);
                //byte months = Convert.ToByte(dateValue.Month);
                //short years = Convert.ToInt16(dateValue.Year);

                // return values
                return new Variable(dateValue.ToString("dd/MM/yyyy"));
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadDate");
                return new Variable(new DateTime().ToString("dd/MM/yyyy"));
            }
        }
    }

    class XCellReadFormulaFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;

                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];

                // read formula on that worksheet in specific cell
                string strValue = sheet.Cells[docRow, docColumn].Formula;

                //int strValueLength = strValue.Length;

                // retrun value
                return new Variable(strValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadFormula");
                return new Variable("");
            }
        }
    }

    class XCellEmptyFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var Type = Utils.GetSafeInt(args, 3);

            try
            {
                // convert byte[] to int
                int docSheet = Sheet;
                int docColumn = Column;
                int docRow = Row;
                int type = Type;


                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[docSheet];


                switch (type)
                {
                    // sets cell to default
                    case 1:
                        sheet.Cells[docRow, docColumn].Clear();
                        break;
                    // set empty value in cell
                    case 2:
                        sheet.Cells[docRow, docColumn].Value = null;
                        break;
                    // set empty style in cell
                    case 3:
                        sheet.Cells[docRow, docColumn].StyleID = 0;
                        break;
                    default:
                        MessageBox.Show("Wrong type has been set!", "EmptyCell");
                        return new Variable(false);
                }

                // set empty cell
                sheet.Cells[docRow, docColumn].Clear();

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "EmptyCell");
                return new Variable(false);
            }
        }
    }

    class XFindSheetFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var sheetName = Utils.GetSafeString(args, 0);

            String strSheetName;
            try
            {
                // convert char[] to string
                int n = Array.IndexOf<char>(sheetName.ToArray(), '\0');
                if (n == -1)
                    n = sheetName.Length;
                strSheetName = new String(sheetName.ToArray(), 0, n);

                ExcelWorksheet sheet;
                int index;

                try
                {
                    // get worksheet by name
                    sheet = Excel.document.Workbook.Worksheets[strSheetName];

                    // get index of worksheet
                    index = sheet.Index;
                }
                catch (Exception)
                {
                    // if sheet not found, zero is returned
                    index = 0;
                }

                // return value
                return new Variable(index);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FindSheet");
                return new Variable(0);
            }
        }
    }

    #endregion

    class XFindColumnFunction : ParserFunction
    {
        //protected override Variable Evaluate(ParsingScript script)
        //{
        //    List<Variable> args = script.GetFunctionArgs();
        //    Utils.CheckArgs(args.Count, 3, m_name);

        //    var Sheet = Utils.GetSafeInt(args, 0);
        //    var Row = Utils.GetSafeInt(args, 1);
        //    var ColumnName = Utils.GetSafeString(args, 2);
            
        //    string strColumnName;

        //    try
        //    {
        //        // convert byte[] to int
        //        int sheet = Sheet;
        //        int row = Row;

        //        // convert char[] to string
        //        int n = Array.IndexOf<char>(ColumnName.ToArray(), '\0');
        //        if (n == -1)
        //            n = ColumnName.Length;
        //        strColumnName = new String(ColumnName.ToArray(), 0, n).Trim();

        //        try
        //        {
        //            // find worksheet
        //            ExcelWorksheet Wsheet = Excel.document.Workbook.Worksheets[sheet];

        //            int maxColumn = Wsheet.Dimension.End.Column;

        //            int findColumn = 0;

        //            for (int i = 1; i <= maxColumn; i++)
        //            {
        //                String tempVal = Convert.ToString(Wsheet.Cells[row, i].Value);
        //                if (tempVal.Equals(strColumnName))
        //                {
        //                    findColumn = i;
        //                    break;
        //                }
        //            }

        //            par.Row = BitConverter.GetBytes(findColumn);

        //        }
        //        catch (Exception)
        //        {
        //            // retrun values
        //            return new Variable(0);
        //        }


        //        return OK;
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FindColumn");
        //        return ERR;
        //    }
        //}
    }
}
