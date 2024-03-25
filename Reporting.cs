using DevExpress.Xpf.Printing;
using DevExpress.XtraCharts;
using DevExpress.XtraExport.Helpers;
using DevExpress.XtraPrinting.Caching;
using DevExpress.XtraReports.UI;
using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCSCS
{
      
    public enum ReportOption
    {
        Setup,
        Output,
        Update,
        Print
    }
    public class ReportFunction : ParserFunction
    {
        public static void Init(Interpreter interpreter)
        {
            interpreter.RegisterFunction(Constants.SETUP_REPORT, new ReportFunction(ReportOption.Setup));
            interpreter.RegisterFunction(Constants.OUTPUT_REPORT, new ReportFunction(ReportOption.Output));
            interpreter.RegisterFunction(Constants.UPDATE_REPORT, new ReportFunction(ReportOption.Update));
            interpreter.RegisterFunction(Constants.PRINT_REPORT, new ReportFunction(ReportOption.Print));
        }

        ReportOption option;

        static Dictionary<int, XtraReport> Reports;

        ParsingScript Script;
        CSCS_GUI Gui;

        static Dictionary<int, DataSet> DataSets = new Dictionary<int, DataSet>();
        static Dictionary<int, DataTable> DataTables = new Dictionary<int, DataTable>();

        static List<string> chartTags = new List<string>();

        static Dictionary<int, List<string>> fieldsOfReports = new Dictionary<int, List<string>>();
        
        static Dictionary<int, int> lastReportsNumbers = new Dictionary<int, int>();

        public ReportFunction(ReportOption _option)
        {
            option = _option;
        }
        protected override Variable Evaluate(ParsingScript script)
        {
            Script = script;
            Gui = CSCS_GUI.GetInstance(script);

            switch (option)
            {
                case ReportOption.Setup:
                    return SetupReport();

                case ReportOption.Output:
                    OutputReport();
                    break;

                case ReportOption.Update:
                    UpdateReport();
                    break;

                case ReportOption.Print:
                    PrintReport();
                    break;
            }

            return Variable.EmptyInstance;
        }

        private Variable SetupReport()
        {
            bool isMainReport;

            List<Variable> args = Script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            string reportFilename = Utils.GetSafeString(args, 0);
            int parentReportHndlNum = Utils.GetSafeInt(args, 1);
            if (parentReportHndlNum == 0)
            {
                isMainReport = true;
            }
            else
            {
                isMainReport = false;
            }

            if (isMainReport)
            {
                Reports = new Dictionary<int, XtraReport>();

                Reports[1] = new XtraReport();
                Reports[1].LoadLayout(reportFilename);

                DataSets[1] = new DataSet();
                DataSets[1].DataSetName = "DataSet1";
                DataTables[1] = new DataTable();
                DataSets[1].Tables.Add(DataTables[1]);
                DataTables[1].TableName = "TableName1";

                Reports[1].DataSource = DataSets[1];
                Reports[1].DataMember = DataSets[1].Tables[0].TableName;

                fieldsOfReports[1] = new List<string>();
                //fieldsOfBarCodes[1] = new List<string>();

                var allLabels = Reports[1].AllControls<XRLabel>();
                foreach (var label in allLabels)
                {

                    if (!string.IsNullOrEmpty(label.Tag.ToString()))
                    {
                        //has Tag
                        var fieldName = label.Tag.ToString().ToLower();

                        if (!fieldsOfReports[1].Any(p => p == fieldName))
                            fieldsOfReports[1].Add(fieldName);

                        label.ExpressionBindings.Add(new ExpressionBinding("Text", $"{fieldName}"));
                    }
                }

                var allBarCodes = Reports[1].AllControls<XRBarCode>();
                foreach (var barcode in allBarCodes)
                {

                    if (!string.IsNullOrEmpty(barcode.Tag.ToString()))
                    {
                        //has Tag
                        var fieldName = barcode.Tag.ToString().ToLower();

                        if (!fieldsOfReports[1].Any(p => p == fieldName))
                            fieldsOfReports[1].Add(fieldName);

                        barcode.ExpressionBindings.Add(new ExpressionBinding("BinaryData", $"{fieldName}"));
                        barcode.ExpressionBindings.Add(new ExpressionBinding("Text", $"{fieldName}"));
                    }
                }

                fieldsOfReports[1].Add("thisReportsNumber");


                foreach (var fieldName in fieldsOfReports[1])
                {
                    Type fieldType = typeof(Int32);
                    if (Gui.DEFINES.TryGetValue(fieldName, out DefineVariable defVar))
                    {
                        switch (defVar.DefType)
                        {
                            case "a":
                                fieldType = typeof(string);
                                break;

                            case "n":
                            case "i":
                            case "r":
                            case "b":
                                fieldType = typeof(Double);
                                break;

                            case "d":
                                fieldType = typeof(DateTime);
                                break;

                            case "t":
                                fieldType = typeof(DateTime);
                                break;

                            default:
                                fieldType = typeof(string);
                                break;
                        }
                    }

                    DataTables[1].Columns.Add(fieldName, fieldType);
                }


                var allCharts = Reports[1].AllControls<XRChart>();
                foreach (var chart in allCharts)
                {

                    if (!string.IsNullOrEmpty(chart.Tag.ToString()))
                    {
                        var fieldName = chart.Tag.ToString().ToLower();

                        if (!fieldsOfReports[1].Any(p => p == fieldName))
                        {
                            fieldsOfReports[1].Add(fieldName);
                            chartTags.Add(fieldName);
                        }

                        chart.Series[0].ArgumentScaleType = DevExpress.XtraCharts.ScaleType.Auto;
                        chart.Series[0].ValueScaleType = DevExpress.XtraCharts.ScaleType.Numerical;

                        //DataTables[thisSubreportNum].Columns.Add(fieldName, typeof(DataSet));
                        DataTables[1].Columns.Add(fieldName, typeof(DataTable));


                        //DataTable dt;

                        //dt = new DataTable(fieldName);
                        //dt.Columns.Add(new DataColumn("Argument", typeof(string)));
                        //dt.Columns.Add(new DataColumn("Value", typeof(int)));

                        //chart.DataSource = dt;
                        
                        
                        //chart.ExpressionBindings.Add(new ExpressionBinding("DataSource", $"2+2"));
                        //chart.ExpressionBindings.Add(new ExpressionBinding("DataMember", $"{fieldName}.{fieldName}"));
                        chart.BeforePrint += Chart_BeforePrint;
                    }
                }


                //image in main report
                var allPictures = Reports[1].AllControls<XRPictureBox>();
                foreach (var picture in allPictures)
                {

                    if (!string.IsNullOrEmpty(picture.Tag.ToString()))
                    {
                        //has Tag
                        var imageSourceVariableName = picture.Tag.ToString().ToLower();
                        if (Gui.DEFINES.TryGetValue(imageSourceVariableName, out DefineVariable defVar))
                        {
                            picture.ImageUrl = defVar.AsString();
                        }

                    }
                }

                return new Variable((double)1);
            }
            else
            {
                //isSubreport
                var thisSubreportNum = Reports.Keys.Max() + 1;

                Reports[thisSubreportNum] = new XtraReport();
                Reports[thisSubreportNum].LoadLayout(reportFilename);

                Reports[thisSubreportNum].Parameters.Clear();
                Reports[thisSubreportNum].Parameters.Add(new DevExpress.XtraReports.Parameters.Parameter() { AllowNull = false, Name = $"thisReportsNumberParam_{thisSubreportNum}", Type = typeof(string), Visible = false, Value = "" });
                Reports[thisSubreportNum].FilterString = $"[thisSubreportsMainReport] = ?thisReportsNumberParam_{thisSubreportNum}";

                DataSets[thisSubreportNum] = new DataSet();
                DataSets[thisSubreportNum].DataSetName = $"DataSet{thisSubreportNum}";
                DataTables[thisSubreportNum] = new DataTable();
                DataSets[thisSubreportNum].Tables.Add(DataTables[thisSubreportNum]);
                DataTables[thisSubreportNum].TableName = $"TableName{thisSubreportNum}";

                Reports[thisSubreportNum].DataSource = DataSets[thisSubreportNum];
                Reports[thisSubreportNum].DataMember = DataSets[thisSubreportNum].Tables[0].TableName;

                fieldsOfReports[thisSubreportNum] = new List<string>();
                var allLabels = Reports[thisSubreportNum].AllControls<XRLabel>();
                foreach (var label in allLabels)
                {
                    if (!string.IsNullOrEmpty(label.Tag.ToString()))
                    {
                        //has tag
                        var fieldName = label.Tag.ToString().ToLower();

                        if (!fieldsOfReports[thisSubreportNum].Any(p => p == fieldName))
                            fieldsOfReports[thisSubreportNum].Add(fieldName);

                        label.ExpressionBindings.Add(new ExpressionBinding("Text", $"[{fieldName}]"));
                    }
                }

                var allBarCodes = Reports[thisSubreportNum].AllControls<XRBarCode>();
                foreach (var barcode in allBarCodes)
                {

                    if (!string.IsNullOrEmpty(barcode.Tag.ToString()))
                    {
                        //has Tag
                        var fieldName = barcode.Tag.ToString().ToLower();

                        if (!fieldsOfReports[thisSubreportNum].Any(p => p == fieldName))
                            fieldsOfReports[thisSubreportNum].Add(fieldName);

                        barcode.ExpressionBindings.Add(new ExpressionBinding("BinaryData", $"{fieldName}"));
                        barcode.ExpressionBindings.Add(new ExpressionBinding("Text", $"{fieldName}"));
                    }
                }

                fieldsOfReports[thisSubreportNum].Add("thisSubreportsMainReport");
                fieldsOfReports[thisSubreportNum].Add("thisReportsNumber");

                foreach (var fieldName in fieldsOfReports[thisSubreportNum])
                {
                    Type fieldType = typeof(Int32);
                    if (Gui.DEFINES.TryGetValue(fieldName, out DefineVariable defVar))
                    {
                        switch (defVar.DefType)
                        {
                            case "a":
                                fieldType = typeof(string);
                                break;

                            case "n":
                            case "i":
                            case "r":
                            case "b":
                                fieldType = typeof(Double);
                                break;

                            case "d":
                                fieldType = typeof(DateTime);
                                break;

                            case "t":
                                fieldType = typeof(DateTime);
                                break;

                            default:
                                fieldType = typeof(string);
                                break;
                        }
                    }

                    DataTables[thisSubreportNum].Columns.Add(fieldName, fieldType);
                }


                var allCharts = Reports[thisSubreportNum].AllControls<XRChart>();
                foreach (var chart in allCharts)
                {

                    if (!string.IsNullOrEmpty(chart.Tag.ToString()))
                    {
                        var fieldName = chart.Tag.ToString().ToLower();

                        if (!fieldsOfReports[thisSubreportNum].Any(p => p == fieldName))
                        {
                            fieldsOfReports[thisSubreportNum].Add(fieldName);
                            chartTags.Add(fieldName);
                        }

                        chart.Series[0].ArgumentScaleType = DevExpress.XtraCharts.ScaleType.Auto;
                        chart.Series[0].ValueScaleType = DevExpress.XtraCharts.ScaleType.Numerical;

                        //DataTables[thisSubreportNum].Columns.Add(fieldName, typeof(DataSet));
                        DataTables[thisSubreportNum].Columns.Add(fieldName, typeof(DataTable));


                        //DataTable dt;

                        //dt = new DataTable(fieldName);
                        //dt.Columns.Add(new DataColumn("Argument", typeof(string)));
                        //dt.Columns.Add(new DataColumn("Value", typeof(int)));

                        //chart.DataSource = dt;


                        //chart.ExpressionBindings.Add(new ExpressionBinding("DataSource", $"2+2"));
                        //chart.ExpressionBindings.Add(new ExpressionBinding("DataMember", $"{fieldName}.{fieldName}"));
                        chart.BeforePrint += Chart_BeforePrint;
                    }
                }


                //linking subreport(s)
                var parentsSubreports = Reports[parentReportHndlNum].AllControls<XRSubreport>();

                foreach (var subreport in parentsSubreports)
                {
                    if (reportFilename.StartsWith(subreport.Name))
                    {
                        subreport.ReportSource = Reports[thisSubreportNum];
                    }
                }

                return new Variable((double)thisSubreportNum);
            }
        }

        static List<KeyValuePair<int, int>> chartsReportsList = new List<KeyValuePair<int, int>>();
        //static List<int> chartsSeriesCountList = new List<int>();
        private void Chart_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var chart = (sender as XRChart);

            var current = chartsReportsList.FirstOrDefault();
            
            chart.DataSource = DataTables[current.Key].Rows[current.Value][chart.Tag.ToString()];

            chart.Series[0].ArgumentDataMember = "Argument";
            chart.Series[0].ValueDataMembers.AddRange(new string[] { "Value1" });

            for(int i = 1; i < chart.Series.Count; i++)
            {
                chart.Series[i].ArgumentDataMember = "Argument";
                chart.Series[i].ValueDataMembers.AddRange(new string[] { "Value" + (i + 1) });
            }

            //var currentChartSeriesCount = chartsSeriesCountList.FirstOrDefault();
            //for (int i = 1; i < currentChartSeriesCount; i++)
            //{
            //    chart.Series.Add(new DevExpress.XtraCharts.Series("ser",ViewType.Line));
            //    chart.Series[i].ArgumentDataMember = "Argument";
            //    chart.Series[i].ValueDataMembers.AddRange(new string[] { "Value" + (i + 1) });
            //}
            //chartsSeriesCountList.RemoveAt(0);

            chartsReportsList.RemoveAt(0);
        }

        private void OutputReport()
        {
            List<Variable> args = Script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 1, m_name);

            int reportHndlNum = Utils.GetSafeInt(args, 0);

            int thisSubreportsMainReport = 0;
            if (reportHndlNum > 1)
            {
                //subreport
                thisSubreportsMainReport = lastReportsNumbers[reportHndlNum - 1];
            }

            if (!lastReportsNumbers.TryGetValue(reportHndlNum, out int reportsNumber))
            {
                lastReportsNumbers[reportHndlNum] = 1;
            }
            else
            {
                lastReportsNumbers[reportHndlNum] = lastReportsNumbers[reportHndlNum] + 1;
            }

            Object[] newObjectArray = new Object[fieldsOfReports[reportHndlNum].Count];

            for (int i = 0; i < fieldsOfReports[reportHndlNum].Count; i++)
            {
                string dataTableFieldName = fieldsOfReports[reportHndlNum][i];
                if (dataTableFieldName == "thisReportsNumber")
                {
                    newObjectArray[i] = lastReportsNumbers[reportHndlNum];
                }
                else if (dataTableFieldName == "thisSubreportsMainReport")
                {
                    newObjectArray[i] = thisSubreportsMainReport;
                }
                else if (chartTags.Any(p => p == dataTableFieldName))
                {
                    //DataSet ds = new DataSet();
                    DataTable dt;

                    dt = new DataTable(dataTableFieldName);
                    dt.Columns.Add(new DataColumn("Argument", typeof(string)));
                    dt.Columns.Add(new DataColumn("Value1", typeof(double)));
                    //dt.Columns.Add(new DataColumn("Value2", typeof(int)));

                    //int numberOfSeries = 1;

                    if (Gui.DEFINES.TryGetValue(dataTableFieldName + "_val_1", out DefineVariable defVarVal1))
                    {
                        List<string> chartArgs = new List<string>();
                        if (Gui.DEFINES.TryGetValue(dataTableFieldName + "_args", out DefineVariable defVarArgs))
                        {
                            chartArgs = defVarArgs.Tuple.Select(p=>p.String).ToList();
                        }

                        if(Gui.DEFINES.TryGetValue(dataTableFieldName + "_val_2", out DefineVariable defVarVal2))
                        {
                            dt.Columns.Add(new DataColumn("Value2", typeof(double)));
                            //numberOfSeries = 2;

                            if (Gui.DEFINES.TryGetValue(dataTableFieldName + "_val_3", out DefineVariable defVarVal3))
                            {
                                dt.Columns.Add(new DataColumn("Value3", typeof(double)));
                                //numberOfSeries = 3;

                                for (int j = 0; j < defVarVal1.Tuple.Count; j++)
                                {
                                    DataRow row = dt.NewRow();
                                    row["Argument"] = chartArgs.Count > j ? chartArgs[j] : "";
                                    row["Value1"] = defVarVal1.Tuple[j].Value;
                                    row["Value2"] = defVarVal2.Tuple[j].Value;
                                    row["Value3"] = defVarVal3.Tuple[j].Value;
                                    dt.Rows.Add(row);
                                }
                            }
                            else
                            {
                                for (int j = 0; j < defVarVal1.Tuple.Count; j++)
                                {
                                    DataRow row = dt.NewRow();
                                    row["Argument"] = chartArgs.Count > j ? chartArgs[j] : "";
                                    row["Value1"] = defVarVal1.Tuple[j].Value;
                                    row["Value2"] = defVarVal2.Tuple[j].Value;
                                    dt.Rows.Add(row);
                                }
                            }
                        }
                        else
                        {
                            for (int j = 0; j < defVarVal1.Tuple.Count; j++)
                            {
                                DataRow row = dt.NewRow();
                                row["Argument"] = chartArgs.Count > j ? chartArgs[j] : "";
                                row["Value1"] = defVarVal1.Tuple[j].Value;
                                dt.Rows.Add(row);
                            }
                        }
                    }

                    //DataRow row = dt.NewRow();
                    //row["Argument"] = "jedan";
                    //row["Value"] = rnd.Next(100);
                    //dt.Rows.Add(row);

                    //DataRow row2 = dt.NewRow();
                    //row2["Argument"] = "dva";
                    //row2["Value"] = rnd.Next(100);
                    //dt.Rows.Add(row2);
                    
                    //DataRow row3 = dt.NewRow();
                    //row3["Argument"] = "tri";
                    //row3["Value"] = rnd.Next(100);
                    //dt.Rows.Add(row3);

                    //ds.Tables.Add(dt);

                    //newObjectArray[i] = ds;
                    newObjectArray[i] = dt;

                    var rowCount = DataSets[reportHndlNum].Tables[0].Rows.Count;
                    chartsReportsList.Add(new KeyValuePair<int, int>(reportHndlNum, rowCount));
                    //chartsSeriesCountList.Add(numberOfSeries);

                    //chart.DataSource = DataSets[reportHndlNum].Tables[0].Rows[DataSets[reportHndlNum].Tables[0].Rows.Count - 1][chart.Tag.ToString().ToLower()];
                }
                else if (Gui.DEFINES.TryGetValue(dataTableFieldName, out DefineVariable defVar))
                {
                    switch (defVar.DefType)
                    {
                        case "a":
                            newObjectArray[i] = defVar.AsString();
                            break;

                        case "n":
                        case "i":
                        case "r":
                        case "b":
                            newObjectArray[i] = defVar.AsDouble();
                            break;

                        case "d":
                            newObjectArray[i] = defVar.AsDateTime().ToString(defVar.GetDateFormat());
                            break;

                        case "t":
                            newObjectArray[i] = defVar.AsDateTime().ToString(defVar.GetTimeFormat());
                            break;
                        default:
                            break;
                    }
                }
            }

            DataSets[reportHndlNum].Tables[0].Rows.Add(newObjectArray);

            var allBands = Reports[reportHndlNum].AllControls<Band>();
            foreach (var band in allBands)
            {
                var allControls = band.AllControls<XRControl>();
                foreach (var control in allControls)
                {
                    if (!control.Name.StartsWith("xr"))
                    {
                        string controlName = control.Name.ToLower();
                        if (Gui.DEFINES.TryGetValue(controlName, out DefineVariable defVar)) // visibility
                        {
                            if (defVar.AsString().ToLower() == "off")
                            {
                                control.Visible = false;
                            }
                            else if (defVar.AsString().ToLower() == "on")
                            {
                                control.Visible = true;
                            }
                        }
                        if (Gui.DEFINES.TryGetValue(controlName + "cl", out DefineVariable defVar1)) // font color
                        {
                            var label = control as XRLabel;
                            label.ForeColor = System.Drawing.Color.FromName(defVar1.AsString());
                        }
                        if (Gui.DEFINES.TryGetValue(controlName + "lt", out DefineVariable defVar2)) // left margin
                        {
                            control.LeftF = (float)defVar2.AsDouble();
                        }
                        if (Gui.DEFINES.TryGetValue(controlName + "top", out DefineVariable defVar3)) // left margin
                        {
                            control.TopF = (float)defVar3.AsDouble();
                        }
                        if (Gui.DEFINES.TryGetValue(controlName + "fc", out DefineVariable defVar4)) // font color
                        {
                            try
                            {
                                var newColorString = defVar4.AsString();
                                if (!string.IsNullOrEmpty(newColorString))
                                {
                                    var label = control as XRLabel;
                                    var newColor = System.Drawing.Color.FromName(newColorString);
                                    label.ForeColor = newColor;
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                        if (Gui.DEFINES.TryGetValue(controlName + "fs", out DefineVariable defVar5)) // font size
                        {
                            System.Drawing.Font oldFont = control.GetEffectiveFont();
                            System.Drawing.Font newFont = new System.Drawing.Font(oldFont.FontFamily, (float)defVar5.AsDouble(), oldFont.Style);
                            control.Font = newFont;
                        }
                        if (Gui.DEFINES.TryGetValue(controlName + "fn", out DefineVariable defVar6)) // font name
                        {
                            System.Drawing.Font oldFont = control.GetEffectiveFont();
                            System.Drawing.Font newFont = new System.Drawing.Font(new System.Drawing.FontFamily(defVar6.AsString()), oldFont.Size, oldFont.Style);
                            control.Font = newFont;
                        }
                        if (Gui.DEFINES.TryGetValue(control.Name.ToLower() + "wd", out DefineVariable defVar7))
                        {
                            var requestedWidth = (float)defVar7.AsDouble();
                            if (requestedWidth != 0)
                                control.WidthF = requestedWidth;
                        }
                        if (Gui.DEFINES.TryGetValue(control.Name.ToLower() + "ht", out DefineVariable defVar8))
                        {
                            var requestedHeighth = (float)defVar8.AsDouble();
                            if (requestedHeighth != 0)
                                control.HeightF = requestedHeighth;
                        }
                    }
                }
            }
        }
        
        private void UpdateReport()
        {
            List<Variable> args = Script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            int reportHndlNum = Utils.GetSafeInt(args, 0);
            string variableListString = Utils.GetSafeString(args, 1);

            var variableArray = variableListString.Replace(" ", "").Split(',');

            int lastRowIndex = DataSets[reportHndlNum].Tables[0].Rows.Count - 1;
            DataRow lastRow = DataSets[reportHndlNum].Tables[0].Rows[lastRowIndex];

            foreach (var variable in variableArray)
            {
                if (Gui.DEFINES.TryGetValue(variable.ToLower(), out DefineVariable defVar))
                {
                    switch (defVar.DefType)
                    {
                        case "a":
                            lastRow.SetField<string>(lastRow.Table.Columns[variable.ToLower()], defVar.AsString());
                            break;

                        case "n":
                        case "i":
                        case "r":
                        case "b":
                            lastRow.SetField<Double>(lastRow.Table.Columns[variable.ToLower()], defVar.AsDouble());
                            break;
                        case "d":
                            lastRow.SetField<DateTime>(lastRow.Table.Columns[variable.ToLower()], defVar.AsDateTime());
                            break;

                        case "t":
                            lastRow.SetField<DateTime>(lastRow.Table.Columns[variable.ToLower()], defVar.AsDateTime());
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void PrintReport()
        {
            foreach (var report in Reports)
            {
                var allSubreports = report.Value.AllControls<XRSubreport>();
                foreach (var subreport in allSubreports)
                {
                    subreport.ParameterBindings.Clear();
                    subreport.ParameterBindings.Add(new ParameterBinding($"thisReportsNumberParam_{report.Key + 1}", report.Value.DataSource, ((DataSet)report.Value.DataSource).Tables[0].TableName + "." + "thisReportsNumber"/*parameterName.ParameterName.Replace("param", "")*/));
                    
                }
            }

            var storage = new MemoryDocumentStorage();
            var Report = Reports[1];
            var cachedReportSource = new CachedReportSource(Report, storage);

            // Invoke the Ribbon Print Preview window 
            // and load the report document into it.
            PrintHelper.ShowRibbonPrintPreview(null, cachedReportSource);

            //// Invoke the Ribbon Print Preview window modally.
            //PrintHelper.ShowRibbonPrintPreviewDialog(null, cachedReportSource);
        }
    }
}
