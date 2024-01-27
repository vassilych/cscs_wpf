using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Spire.Xls;
using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ExcelHorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment;
using ExcelVerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment;

namespace WpfCSCS
{
    public class Excel
    {
        public static ExcelPackage document;
        public static int errorNumber = 0;

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
            interpreter.RegisterFunction(Constants.X_FIND_ROW, new XFindRowFunction());
            interpreter.RegisterFunction(Constants.X_COPY_CELL, new XCopyCellFunction());
            interpreter.RegisterFunction(Constants.X_COPY_ROW, new XCopyRowFunction());
            interpreter.RegisterFunction(Constants.X_COPY_ROW_TO_ROW, new XCopyRowToRowFunction());
            interpreter.RegisterFunction(Constants.X_COPY_COLUMN, new XCopyColumnFunction());
            interpreter.RegisterFunction(Constants.X_FORMAT_COLUMN, new XFormatColumnFunction());
            interpreter.RegisterFunction(Constants.X_LAST_ROW, new XLastRowFunction());
            interpreter.RegisterFunction(Constants.X_LAST_COLUMN, new XLastColumnFunction());
            interpreter.RegisterFunction(Constants.X_LAST_ADDRESS, new XLastAddressFunction());
            interpreter.RegisterFunction(Constants.X_INSERT_ROWS, new XInsertRowsFunction());
            interpreter.RegisterFunction(Constants.X_DELETE_ROW, new XDeleteRowFunction());
            interpreter.RegisterFunction(Constants.X_COLUMNS_AUTO_FIT, new XColumnsAutoFitFunction());
            interpreter.RegisterFunction(Constants.X_SET_TABLE, new XSetTableFunction());
            interpreter.RegisterFunction(Constants.X_NAMED_CELL_POSITION, new XNamedCellPositionFunction());
            interpreter.RegisterFunction(Constants.X_NAMED_RANGE_ADD, new XNamedRangeAddFunction());
            interpreter.RegisterFunction(Constants.X_HEADER, new XHeaderFunction());
            interpreter.RegisterFunction(Constants.X_FOOTER, new XFooterFunction());
            interpreter.RegisterFunction(Constants.X_FONT_NAME, new XFontNameFunction());
            interpreter.RegisterFunction(Constants.X_FONT_SIZE, new XFontSizeFunction());
            interpreter.RegisterFunction(Constants.X_FONT_COLOR, new XFontColorFunction());
            interpreter.RegisterFunction(Constants.X_BACKGROUND_COLOR, new XBackgroundColorFunction());
            interpreter.RegisterFunction(Constants.X_ALIGN, new XAlignFunction());
            interpreter.RegisterFunction(Constants.X_FONT_FORMAT, new XFontFormatFunction());
            interpreter.RegisterFunction(Constants.X_BORDER, new XBorderFunction());
            interpreter.RegisterFunction(Constants.X_PIVOT_TABLE_REFRESH, new XPivotTableRefreshFunction());
            
            interpreter.RegisterFunction(Constants.X_ERR, new XErrFunction());
        }
    }

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

    class XErrFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 0, m_name);

            return new Variable(Excel.errorNumber);
        }
    }

    class XFileNewFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileName = Utils.GetSafeString(args, 0);

            FileInfo newFile;
            try
            {
                newFile = new FileInfo(fileName);

                // if document exist, delete it and create new
                if (newFile.Exists)
                {
                    newFile.Delete();
                    newFile = new FileInfo(fileName);
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
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFileOpenFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileName = Utils.GetSafeString(args, 0);

            FileInfo newFile;
            try
            {
                newFile = new FileInfo(fileName);

                // if document does not exist, get message
                if (!newFile.Exists)
                {
                    MessageBox.Show("Document: '" + fileName + "' does not exist!", "FileOpen");
                    Excel.errorNumber = 2;
                    return new Variable(false);
                }

                // create ExcelPackage reference
                Excel.document = new ExcelPackage(newFile);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FileOpen");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFileSaveFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

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
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFileSaveAsFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileNamePath = Utils.GetSafeString(args, 0);

            try
            {
                // save document
                Excel.document.SaveAs(new System.IO.FileInfo(fileNamePath));

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message + e.StackTrace, "FileSaveAs");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFileDeleteFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var fileName = Utils.GetSafeString(args, 0);

            FileInfo newFile;
            try
            {
                newFile = new FileInfo(fileName);

                // if document does not exist, get message
                if (!newFile.Exists)
                {
                    MessageBox.Show("Document: '" + fileName + "' does not exist!", "FileDelete");
                    Excel.errorNumber = 2;
                    return new Variable(false);
                }

                // delete dokument
                newFile.Delete();

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FileDelete");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XSheetAddFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var sheetName = Utils.GetSafeString(args, 0);

            int docSheetCount;
            try
            {
                // get number of worksheets
                docSheetCount = Excel.document.Workbook.Worksheets.Count;

                // add new specific worksheet
                Excel.document.Workbook.Worksheets.Add(sheetName);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetAdd");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XSheetDeleteFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);

            try
            {
                // delete specific worksheet
                Excel.document.Workbook.Worksheets.Delete(Sheet);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetDelete");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XSheetCountFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

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
                Excel.errorNumber = 1;
                return new Variable(-1);
            }
        }
    }

    class XSheetClearFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var sheetIndex = Utils.GetSafeInt(args, 0);

            try
            {
                // check if sheet index exist
                if (sheetIndex > Excel.document.Workbook.Worksheets.Count)
                {
                    MessageBox.Show("Cannot clear " + sheetIndex + ". worksheet because not exist in file: " + Excel.document.File.Name, "SheetClear");
                    Excel.errorNumber = 3;
                    return new Variable(false);
                }

                // get specific worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[sheetIndex];

                string SheetName = sheet.Name;
                
                // delete sheet
                Excel.document.Workbook.Worksheets.Delete(sheet);

                // create new with equal name
                Excel.document.Workbook.Worksheets.Add(SheetName);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetClear");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XSheetRenameFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var sheetIndex = Utils.GetSafeInt(args, 0);
            var sheetRename = Utils.GetSafeString(args, 1);

            try
            {
                // get specific worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[sheetIndex];

                // rename worksheet
                sheet.Name = sheetRename;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SheetRename");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellWriteStringFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeString(args, 3);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // write string on that worksheet in specific cell
                sheet.Cells[Row, Column].Value = value;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteString");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellWriteIntegerFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeInt(args, 3);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // write integer on that worksheet in specific cell
                sheet.Cells[Row, Column].Value = value;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteInteger");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellWriteDoubleFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeDouble(args, 3);
            var charValue = Utils.GetSafeString(args, 4); //optional

            try
            {
                string format = null;
                if (!string.IsNullOrEmpty(charValue))
                {
                    format = charValue;
                }
               
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // format cell
                if (!String.IsNullOrWhiteSpace(format))
                    sheet.Cells[Row, Column].Style.Numberformat.Format = format;
                else
                    sheet.Cells[Row, Column].Style.Numberformat.Format = "General";

                // write double on that worksheet in specific cell
                sheet.Cells[Row, Column].Value = value;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteDouble");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellWriteBooleanFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeVariable(args, 3).AsBool();

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // write boolean on that worksheet in specific cell
                sheet.Cells[Row, Column].Value = value;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteBoolean");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellWriteTimeFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var timeVariableName = Utils.GetSafeString(args, 3).ToLower();
            
            try
            {
                if(!(script.Context as CSCS_GUI).DEFINES.TryGetValue(timeVariableName, out DefineVariable timeVar))
                {
                    MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {timeVariableName} not found!", "CellWriteTime");
                    Excel.errorNumber = 4;
                    return new Variable(false);
                }
                else
                {
                    var varDateTime = timeVar.DateTime;
                    int seconds = varDateTime.Second;
                    int minutes = varDateTime.Minute;
                    int hours = varDateTime.Hour;

                    // find worksheet
                    ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                    // create time object
                    TimeSpan time = new TimeSpan(hours, minutes, seconds);

                    // format cell as 'time'
                    sheet.Cells[Row, Column].Style.Numberformat.Format = "hh:mm:ss";

                    // write time on that worksheet in specific cell
                    sheet.Cells[Row, Column].Value = time;

                    return new Variable(true);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteTime");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellWriteDateFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var dateVariableName = Utils.GetSafeString(args, 3).ToLower();

            try
            {
                if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(dateVariableName, out DefineVariable dateVar))
                {
                    MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {dateVariableName} not found!", "CellWriteDate");
                    Excel.errorNumber = 4;
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
                    ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                    // format cell as 'date'
                    sheet.Cells[Row, Column].Style.Numberformat.Format = "dd-mm-yyyy";

                    // write date on that worksheet in specific cell
                    sheet.Cells[Row, Column].Value = date.Date;

                    return new Variable(true);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteDate");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellWriteDateTimeFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 5, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var dateVariableName = Utils.GetSafeString(args, 3).ToLower();
            var timeVariableName = Utils.GetSafeString(args, 4).ToLower();

            try
            {
                if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(dateVariableName, out DefineVariable dateVar))
                {
                    MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {dateVariableName} not found!", "CellWriteDateTime");
                    Excel.errorNumber = 4;
                    return new Variable(false);
                }
                else
                {
                    if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(timeVariableName, out DefineVariable timeVar))
                    {
                        MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {timeVariableName} not found!", "CellWriteDateTime");
                        Excel.errorNumber = 4;
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
                        ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                        // format cell as 'date and time'
                        sheet.Cells[Row, Column].Style.Numberformat.Format = "dd-mm-yyyy hh:mm:ss";

                        // write date and time on that worksheet in specific cell
                        sheet.Cells[Row, Column].Value = dateTime;

                        return new Variable(true);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteDateTime");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellWriteFormulaFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var value = Utils.GetSafeString(args, 3);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // write formula on that worksheet in specific cell
                sheet.Cells[Row, Column].Formula = value;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellWriteFormula");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellReadStringFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // read to string on that worksheet in specific cell
                string strValue = Convert.ToString(sheet.Cells[Row, Column].Value);

                return new Variable(strValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadString");
                Excel.errorNumber = 1;
                return new Variable("");
            }
        }
    }

    class XCellReadIntegerFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // read to integer on that worksheet in specific cell
                int intValue = Convert.ToInt32(sheet.Cells[Row, Column].Value);

                // return value
                return new Variable(intValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadInteger");
                Excel.errorNumber = 1;
                return new Variable(0);
            }
        }
    }

    class XCellReadDoubleFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // read to double on that worksheet in specific cell
                double doubleValue = Convert.ToDouble(sheet.Cells[Row, Column].Value);

                // retrun value
                return new Variable(doubleValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadDouble");
                Excel.errorNumber = 1;
                return new Variable((double)0);
            }
        }
    }

    class XCellReadBooleanFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                //find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // read to boolean on that worksheet in specific cell
                bool boolValue = Convert.ToBoolean(sheet.Cells[Row, Column].Value);

                // return value
                return new Variable(boolValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadBoolean");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XCellReadTimeFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // get time on that worksheet in specific cell
                TimeSpan timeValue = TimeSpan.Parse(sheet.Cells[Row, Column].Text);

                // return values
                return new Variable(timeValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadTime");
                Excel.errorNumber = 1;
                return new Variable(new TimeSpan(0, 0, 0));
            }
        }
    }

    class XCellReadDateFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // get time on that worksheet in specific cell
                DateTime dateValue = DateTime.Parse(sheet.Cells[Row, Column].Text);

                // return values
                return new Variable(dateValue.ToString("dd/MM/yyyy"));
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadDate");
                Excel.errorNumber = 1;
                return new Variable(new DateTime().ToString("dd/MM/yyyy"));
            }
        }
    }

    class XCellReadFormulaFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // read formula on that worksheet in specific cell
                string strValue = sheet.Cells[Row, Column].Formula;

                // retrun value
                return new Variable(strValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CellReadFormula");
                Excel.errorNumber = 1;
                return new Variable("");
            }
        }
    }

    class XCellEmptyFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var Type = Utils.GetSafeInt(args, 3);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                switch (Type)
                {
                    // sets cell to default
                    case 1:
                        sheet.Cells[Row, Column].Clear();
                        sheet.Cells[Row, Column].Value = null;
                        sheet.Cells[Row, Column].StyleID = 0;
                        break;
                    // set empty value in cell
                    case 2:
                        sheet.Cells[Row, Column].Value = null;
                        break;
                    // set empty style in cell
                    case 3:
                        sheet.Cells[Row, Column].StyleID = 0;
                        break;
                    default:
                        MessageBox.Show("Wrong type has been set!", "EmptyCell");
                        return new Variable(false);
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "EmptyCell");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFindSheetFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var sheetName = Utils.GetSafeString(args, 0);

            try
            {
                ExcelWorksheet sheet;
                int index;

                try
                {
                    // get worksheet by name
                    sheet = Excel.document.Workbook.Worksheets[sheetName];

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
                Excel.errorNumber = 1;
                return new Variable(0);
            }
        }
    }

    class XFindColumnFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Row = Utils.GetSafeInt(args, 1);
            var ColumnName = Utils.GetSafeString(args, 2);

            try
            {
                try
                {
                    // find worksheet
                    ExcelWorksheet Wsheet = Excel.document.Workbook.Worksheets[Sheet];

                    int maxColumn = Wsheet.Dimension.End.Column;

                    int findColumn = 0;

                    for (int i = 1; i <= maxColumn; i++)
                    {
                        String tempVal = Convert.ToString(Wsheet.Cells[Row, i].Value);
                        if (tempVal.Equals(ColumnName))
                        {
                            findColumn = i;
                            break;
                        }
                    }

                    return new Variable(findColumn);
                }
                catch (Exception)
                {
                    // retrun values
                    Excel.errorNumber = 5;
                    return new Variable(0);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FindColumn");
                Excel.errorNumber = 1;
                return new Variable(0);
            }
        }
    }
    
    class XFindRowFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var RowName = Utils.GetSafeString(args, 2);

            try
            {
                try
                {
                    // find worksheet
                    ExcelWorksheet Wsheet = Excel.document.Workbook.Worksheets[Sheet];

                    int maxRow = Wsheet.Dimension.End.Row;

                    int findRow = 0;

                    for (int i = 1; i <= maxRow; i++)
                    {
                        String tempVal = Convert.ToString(Wsheet.Cells[i, Column].Value);
                        if (tempVal.Equals(RowName))
                        {
                            findRow = i;
                            break;
                        }
                    }

                    return new Variable(findRow);
                }
                catch (Exception)
                {
                    // retrun values
                    Excel.errorNumber = 5;
                    return new Variable(0);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FindRow");
                Excel.errorNumber = 1;
                return new Variable(0);
            }
        }
    }
    
    class XCopyCellFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 6, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var SheetDest = Utils.GetSafeInt(args, 3);
            var ColumnDest = Utils.GetSafeInt(args, 4);
            var RowDest = Utils.GetSafeInt(args, 5);

            try
            {

                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // get worksheet destination by index
                ExcelWorksheet worksheetDest = Excel.document.Workbook.Worksheets[SheetDest];

                // copy cell to cell
                worksheet.Cells[Row, Column].Copy(worksheetDest.Cells[RowDest, ColumnDest]);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CopyCell");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XCopyRowFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Row = Utils.GetSafeInt(args, 1);
            var NumRowsCopy = Utils.GetSafeInt(args, 2);

            try
            {
                // get worksheet by index
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // get number of columns
                int countColumn = sheet.Dimension.End.Column;

                // copy row by row
                int i = 1;
                while (i <= NumRowsCopy)
                {
                    // copy entire row
                    for (int j = 1; j <= countColumn; j++)
                    {
                        // copy cell to cell
                        sheet.Cells[Row, j].Copy(sheet.Cells[Row + 1, j]);
                    }
                    // go to next row
                    Row++;
                    i++;
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CopyRow");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XCopyRowToRowFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Row = Utils.GetSafeInt(args, 1);
            var SheetDest = Utils.GetSafeInt(args, 2);
            var RowDest = Utils.GetSafeInt(args, 3);

            try
            {
                // get worksheet by index
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];
                ExcelWorksheet sheetDest = Excel.document.Workbook.Worksheets[SheetDest];

                // get number of columns
                int countColumn = sheet.Dimension.End.Column;

                // copy entire row to destination row 
                for (int j = 1; j <= countColumn; j++)
                {
                    // copy cell to cell
                    sheet.Cells[Row, j].Copy(sheetDest.Cells[RowDest, j]);
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CopyRowToRow");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XCopyColumnFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var NumColumnsCopy = Utils.GetSafeInt(args, 2);

            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // get number of columns
                int countRow = worksheet.Dimension.End.Row;

                // copy column by column
                int i = 1;
                while (i <= NumColumnsCopy)
                {
                    // copy entire column
                    for (int j = 1; j <= countRow; j++)
                    {
                        // copy cell to cell
                        worksheet.Cells[j, Column].Copy(worksheet.Cells[j, Column + 1]);
                    }
                    // go to next column
                    Column++;
                    i++;
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "CopyColumn");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XFormatColumnFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Format = Utils.GetSafeString(args, 2);

            try
            {
                if (string.IsNullOrEmpty(Format))
                {
                    Format = null;
                }
                
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                worksheet.Column(Column).Style.Numberformat.Format = Format;
                
                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FormatColumn");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XLastRowFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);

            try
            {
                // get worksheet by index
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                return new Variable(sheet.Dimension.End.Row);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "LastRow");
                Excel.errorNumber = 1;
                return new Variable(0);
            }
        }
    }
    
    class XLastColumnFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);

            try
            {
                // get worksheet by index
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                return new Variable(sheet.Dimension.End.Column);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "LastColumn");
                Excel.errorNumber = 1;
                return new Variable(0);
            }
        }
    }
    
    class XLastAddressFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);

            try
            {
                // find worksheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // get last address
                string strValue = sheet.Dimension.End.Address;

                // get array size
                int strValueLength = strValue.ToCharArray().Length;

                // retrun value
                return new Variable(strValue);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "LastAddress");
                Excel.errorNumber = 1;
                return new Variable("");
            }
        }
    }
    
    class XInsertRowsFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Row = Utils.GetSafeInt(args, 1);
            var NumOfRows = Utils.GetSafeInt(args, 2);

            try
            {
                // get worksheet by index
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // insert rows from row to numOfRows
                sheet.InsertRow(Row, NumOfRows);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "InsertRows");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XDeleteRowFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Row = Utils.GetSafeInt(args, 1);

            try
            {
                // get worksheet by index
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // delete row
                sheet.DeleteRow(Row, 1);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "DeleteRow");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XColumnsAutoFitFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);

            try
            {
                // get worksheet by index
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // autoFitColumns
                sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "ColumnsAutoFit");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XSetTableFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 5, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var FirstRow = Utils.GetSafeInt(args, 1);
            var FirstColumn = Utils.GetSafeInt(args, 2);
            var LastRow = Utils.GetSafeInt(args, 3);
            var LastColumn = Utils.GetSafeInt(args, 4);
            var TableName = Utils.GetSafeString(args, 5);

            try
            {
                // get sheet
                ExcelWorksheet sheet = Excel.document.Workbook.Worksheets[Sheet];

                // select the range that will be included in the table
                var range = sheet.Cells[FirstRow, FirstColumn, LastRow, LastColumn];

                // add the excel table entity
                sheet.Tables.Add(range, TableName);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "SetTable");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XNamedCellPositionFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var varNameSheet = Utils.GetSafeString(args, 0);
            var varNameColumn = Utils.GetSafeString(args, 1);
            var varNameRow = Utils.GetSafeString(args, 2);
            var CellName = Utils.GetSafeString(args, 3);

            string strCellName;

            try
            {
                int sheetIndex = -1;
                int column = -1;
                int row = -1;

                try
                {
                    // get name range
                    ExcelNamedRange range = Excel.document.Workbook.Names[CellName];
                    // get sheet
                    ExcelWorksheet sheet = range.Worksheet;
                    // get sheet index
                    sheetIndex = sheet.Index;
                    // get column
                    column = range.Start.Column;
                    // get row
                    row = range.Start.Row;
                }
                catch (Exception)
                {
                    sheetIndex = -1;
                }

                if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(varNameSheet.ToLower(), out DefineVariable sheetVar))
                {
                    MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {varNameSheet} not found!", "NamedCellPosition");
                    Excel.errorNumber = 4;
                    return new Variable(false);
                }
                else
                {
                    if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(varNameColumn.ToLower(), out DefineVariable columnVar))
                    {
                        MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {varNameSheet} not found!", "NamedCellPosition");
                        Excel.errorNumber = 4;
                        return new Variable(false);
                    }
                    else
                    {
                        if (!(script.Context as CSCS_GUI).DEFINES.TryGetValue(varNameRow.ToLower(), out DefineVariable rowVar))
                        {
                            MessageBox.Show("[TAS2XLSX] Error:" + $"Variable {varNameSheet} not found!", "NamedCellPosition");
                            Excel.errorNumber = 4;
                            return new Variable(false);
                        }
                        else
                        {
                            if (sheetIndex != -1)
                            {
                                // return values
                                (script.Context as CSCS_GUI).DEFINES[varNameSheet.ToLower()].InitVariable(new Variable(sheetIndex), script.Context as CSCS_GUI);
                                (script.Context as CSCS_GUI).DEFINES[varNameColumn.ToLower()].InitVariable(new Variable(column), script.Context as CSCS_GUI);
                                (script.Context as CSCS_GUI).DEFINES[varNameRow.ToLower()].InitVariable(new Variable(row), script.Context as CSCS_GUI);
                            }
                            else
                            {
                                // return values
                                (script.Context as CSCS_GUI).DEFINES[varNameSheet.ToLower()].InitVariable(new Variable(0), script.Context as CSCS_GUI);
                                (script.Context as CSCS_GUI).DEFINES[varNameColumn.ToLower()].InitVariable(new Variable(0), script.Context as CSCS_GUI);
                                (script.Context as CSCS_GUI).DEFINES[varNameRow.ToLower()].InitVariable(new Variable(0), script.Context as CSCS_GUI);
                            }
                        }
                    }
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "NamedCellPosition");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XNamedRangeAddFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var RangeName = Utils.GetSafeString(args, 0);
            var RangeDefinition = Utils.GetSafeString(args, 1);

            try
            {
                // get count named ranges
                int names = Excel.document.Workbook.Names.Count;

                try
                {
                    // get sheet name
                    string SheetName = RangeDefinition.Substring(0, RangeDefinition.IndexOf("!"));
                    // get cells range
                    int toLenght = RangeDefinition.Length - RangeDefinition.IndexOf("!");
                    string cells = RangeDefinition.Substring(RangeDefinition.IndexOf("!") + 1, toLenght - 1);
                    // set named range
                    Excel.document.Workbook.Names.Add(RangeName, Excel.document.Workbook.Worksheets[SheetName].Cells[cells]);
                }
                catch (Exception e)
                {
                    MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "NamedRangeAdd");
                    Excel.errorNumber = 5;
                    return new Variable(false);
                }
                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "NamedRangeAdd");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XHeaderFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var HeaderPosition = Utils.GetSafeInt(args, 1);
            var Header = Utils.GetSafeString(args, 2);

            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set header
                switch (HeaderPosition)
                {
                    case 1:
                        worksheet.HeaderFooter.FirstHeader.LeftAlignedText = Header;
                        break;
                    case 2:
                        worksheet.HeaderFooter.FirstHeader.CenteredText = Header;
                        break;
                    case 3:
                        worksheet.HeaderFooter.FirstHeader.RightAlignedText = Header;
                        break;
                    default:
                        MessageBox.Show("Wrong header position has been set!", "Header");
                        Excel.errorNumber = 6;
                        return new Variable(false);
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "Header");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFooterFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 3, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var FooterPosition = Utils.GetSafeInt(args, 1);
            var Footer = Utils.GetSafeString(args, 2);

            string strFooter;
            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set header
                switch (FooterPosition)
                {
                    case 1:
                        worksheet.HeaderFooter.FirstFooter.LeftAlignedText = Footer;
                        break;
                    case 2:
                        worksheet.HeaderFooter.FirstFooter.CenteredText = Footer;
                        break;
                    case 3:
                        worksheet.HeaderFooter.FirstFooter.RightAlignedText = Footer;
                        break;
                    default:
                        MessageBox.Show("Wrong footer position has been set!", "Footer");
                        Excel.errorNumber = 6;
                        return new Variable(false);
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "Footer");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFontNameFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var FontName = Utils.GetSafeString(args, 3);

            string strFontName;
            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set font for specific cell
                worksheet.Cells[Row, Column].Style.Font.Name = FontName;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FontName");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFontSizeFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 4, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var FontSize = Utils.GetSafeInt(args, 3);

            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set font size for specific cell
                worksheet.Cells[Row, Column].Style.Font.Size = FontSize;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FontSize");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XFontColorFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 6, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var Red = Utils.GetSafeInt(args, 3);
            var Green = Utils.GetSafeInt(args, 4);
            var Blue = Utils.GetSafeInt(args, 5);

            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set current color
                Color color = Color.FromArgb(Red, Green, Blue);

                // set font color for specific cell
                worksheet.Cells[Row, Column].Style.Font.Color.SetColor(color);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FontColor");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }

    class XBackgroundColorFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 6, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var Red = Utils.GetSafeInt(args, 3);
            var Green = Utils.GetSafeInt(args, 4);
            var Blue = Utils.GetSafeInt(args, 5);

            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set current color
                Color color = Color.FromArgb(Red, Green, Blue);

                // set pattern type
                worksheet.Cells[Row, Column].Style.Fill.PatternType = ExcelFillStyle.Solid;

                // set background color for specific cell
                worksheet.Cells[Row, Column].Style.Fill.BackgroundColor.SetColor(color);

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "BackgroundColor");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XAlignFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 5, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);
            var Horizontal = Utils.GetSafeInt(args, 3);
            var Vertical = Utils.GetSafeInt(args, 4);

            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set horizontal align for specific cell
                switch (Horizontal)
                {
                    case 1:
                        worksheet.Cells[Row, Column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        break;
                    case 2:
                        worksheet.Cells[Row, Column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        break;
                    case 3:
                        worksheet.Cells[Row, Column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        break;
                    case 4:
                        worksheet.Cells[Row, Column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Justify;
                        break;
                    default:
                        MessageBox.Show("Wrong horizontal alignment has been set!", "Align");
                        Excel.errorNumber = 6;
                        return new Variable(false);
                }

                // set vertical align for specific cell
                switch (Vertical)
                {
                    case 1:
                        worksheet.Cells[Row, Column].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        break;
                    case 2:
                        worksheet.Cells[Row, Column].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        break;
                    case 3:
                        worksheet.Cells[Row, Column].Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;
                        break;
                    default:
                        MessageBox.Show("Wrong vertical alignment has been set!", "Align");
                        Excel.errorNumber = 6;
                        return new Variable(false);
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "Align");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XFontFormatFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 9, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            bool bold = Utils.GetSafeVariable(args, 3).AsBool();
            bool italic = Utils.GetSafeVariable(args, 4).AsBool();
            bool underline = Utils.GetSafeVariable(args, 5).AsBool();
            bool strikethrough = Utils.GetSafeVariable(args, 6).AsBool();
            bool superscript = Utils.GetSafeVariable(args, 7).AsBool();
            bool subscript = Utils.GetSafeVariable(args, 8).AsBool();


            //.XLSX
            //Sheets            0..NOLIMIT
            //Column            0..NOLIMIT
            //Row               0..NOLIMIT
            //Bold              is True or False
            //Italic            is True or False
            //Underline         is True or False
            //Strikethrough     is True or False
            //Superscript       is True or False
            //Subscript         is True or False

            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set font format
                if (bold)
                    worksheet.Cells[Row, Column].Style.Font.Bold = true;
                if (italic)
                    worksheet.Cells[Row, Column].Style.Font.Italic = true;
                if (underline)
                    worksheet.Cells[Row, Column].Style.Font.UnderLine = true;
                if (strikethrough)
                    worksheet.Cells[Row, Column].Style.Font.Strike = true;
                if (superscript & subscript)
                {
                    MessageBox.Show("Parameters 'Superscript' and 'Subscript' cannot be to TRUE! Only one at time!'", "FontFormat");
                }
                if (superscript)
                    worksheet.Cells[Row, Column].Style.Font.VerticalAlign = OfficeOpenXml.Style.ExcelVerticalAlignmentFont.Superscript;
                if (subscript)
                    worksheet.Cells[Row, Column].Style.Font.VerticalAlign = OfficeOpenXml.Style.ExcelVerticalAlignmentFont.Subscript;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "FontFormat");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XBorderFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 11, m_name);

            var Sheet = Utils.GetSafeInt(args, 0);
            var Column = Utils.GetSafeInt(args, 1);
            var Row = Utils.GetSafeInt(args, 2);

            bool left = Utils.GetSafeVariable(args, 3).AsBool();
            bool right = Utils.GetSafeVariable(args, 4).AsBool();
            bool top = Utils.GetSafeVariable(args, 5).AsBool();
            bool bottom = Utils.GetSafeVariable(args, 6).AsBool();
            int weightLeft = Utils.GetSafeInt(args, 7);
            int weightRight = Utils.GetSafeInt(args, 8);
            int weightTop = Utils.GetSafeInt(args, 9);
            int weightBottom = Utils.GetSafeInt(args, 10);

            //.XLSX
            //Sheets      0..NOLIMIT
            //Column      0..NOLIMIT
            //Row         0..NOLIMIT
            //Left          is True or False
            //Right         is True or False
            //Top           is True or False
            //Bottom        is True or False
            //WeightLeft    is 1 Thin, 2 Medium, 3 Thick
            //WeightRight   is 1 Thin, 2 Medium, 3 Thick
            //WeightTop     is 1 Thin, 2 Medium, 3 Thick
            //WeightBottom  is 1 Thin, 2 Medium, 3 Thick 

            try
            {
                // get worksheet by index
                ExcelWorksheet worksheet = Excel.document.Workbook.Worksheets[Sheet];

                // set border

                // for left
                if (left)
                {
                    switch (weightLeft)
                    {
                        case 1:
                            worksheet.Cells[Row, Column].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            break;
                        case 2:
                            worksheet.Cells[Row, Column].Style.Border.Left.Style = ExcelBorderStyle.Medium;
                            break;
                        case 3:
                            worksheet.Cells[Row, Column].Style.Border.Left.Style = ExcelBorderStyle.Thick;
                            break;
                        default:
                            MessageBox.Show("Parameter 'weightLeft' must be between 1 and 3!", "Border");
                            Excel.errorNumber = 6;
                            return new Variable(false);
                    }
                }

                // for right   
                if (right)
                {
                    switch (weightRight)
                    {
                        case 1:
                            worksheet.Cells[Row, Column].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            break;
                        case 2:
                            worksheet.Cells[Row, Column].Style.Border.Right.Style = ExcelBorderStyle.Medium;
                            break;
                        case 3:
                            worksheet.Cells[Row, Column].Style.Border.Right.Style = ExcelBorderStyle.Thick;
                            break;
                        default:
                            MessageBox.Show("Parameter 'weightRight' must be between 1 and 3!", "Border");
                            Excel.errorNumber = 6;
                            return new Variable(false);
                    }
                }

                // for top
                if (top)
                {
                    switch (weightTop)
                    {
                        case 1:
                            worksheet.Cells[Row, Column].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            break;
                        case 2:
                            worksheet.Cells[Row, Column].Style.Border.Top.Style = ExcelBorderStyle.Medium;
                            break;
                        case 3:
                            worksheet.Cells[Row, Column].Style.Border.Top.Style = ExcelBorderStyle.Thick;
                            break;
                        default:
                            MessageBox.Show("Parameter 'weightTop' must be between 1 and 3!", "Border");
                            Excel.errorNumber = 6;
                            return new Variable(false);
                    }
                }

                // for bottom
                if (bottom)
                {
                    switch (weightBottom)
                    {
                        case 1:
                            worksheet.Cells[Row, Column].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            break;
                        case 2:
                            worksheet.Cells[Row, Column].Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                            break;
                        case 3:
                            worksheet.Cells[Row, Column].Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                            break;
                        default:
                            MessageBox.Show("Parameter 'weightBottom' must be between 1 and 3!", "Border");
                            Excel.errorNumber = 6;
                            return new Variable(false);
                    }
                }

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "Border");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    class XPivotTableRefreshFunction : ParserFunction
    {
        protected override Variable Evaluate(ParsingScript script)
        {
            Excel.errorNumber = 0;

            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            var SheetPivot = Utils.GetSafeInt(args, 0);
            var SheetData = Utils.GetSafeInt(args, 1);

            try
            {
                //get a reference to the Pivot and Details tables
                ExcelWorksheet piv = Excel.document.Workbook.Worksheets[SheetPivot];
                ExcelWorksheet det = Excel.document.Workbook.Worksheets[SheetData];

                //build the range from top left to the far right column and bottom row (removes blanks)               
                var dataRange = det.Cells[det.Dimension.Start.Address.ToString() + ":" + det.Dimension.End.Address.ToString()];
                piv.PivotTables[0].CacheDefinition.SourceRange = dataRange;

                return new Variable(true);
            }
            catch (Exception e)
            {
                MessageBox.Show("[TAS2XLSX] Error:" + e.Message, "PivotTableRefresh");
                Excel.errorNumber = 1;
                return new Variable(false);
            }
        }
    }
    
    
}
