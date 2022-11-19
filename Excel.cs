using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;
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

namespace WpfCSCS
{
    public class Excel
    {
        public static void Init(Interpreter interpreter)
        {
            interpreter.RegisterFunction(Constants.SQL_TO_XLSX, new SqlToXlsxFunction());
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
                            
                            if(reader.GetDataTypeName(i) == "date")
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

        private string generateFilenameVersionNumberInPath(string path) {
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
}
