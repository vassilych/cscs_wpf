using DevExpress.Xpf.Printing;
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
        public static void Init()
        {
            Interpreter.LastInstance.RegisterFunction(Constants.SETUP_REPORT, new ReportFunction(ReportOption.Setup));
            Interpreter.LastInstance.RegisterFunction(Constants.OUTPUT_REPORT, new ReportFunction(ReportOption.Output));
            Interpreter.LastInstance.RegisterFunction(Constants.UPDATE_REPORT, new ReportFunction(ReportOption.Update));
            Interpreter.LastInstance.RegisterFunction(Constants.PRINT_REPORT, new ReportFunction(ReportOption.Print));
        }

        ReportOption option;

        static Dictionary<int, XtraReport> Reports;

        ParsingScript script;

        static Dictionary<int, DataSet> DataSets = new Dictionary<int, DataSet>();
        static Dictionary<int, DataTable> DataTables = new Dictionary<int, DataTable>();

        static Dictionary<int, List<string>> fieldsOfReports = new Dictionary<int, List<string>>();

        static Dictionary<int, int> lastReportsNumbers = new Dictionary<int, int>();

        public ReportFunction(ReportOption _option)
        {
            option = _option;
        }
        protected override Variable Evaluate(ParsingScript _script)
        {
            script = _script;

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

            List<Variable> args = script.GetFunctionArgs();
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
                var allLabels = Reports[1].AllControls<XRLabel>();
                foreach (var label in allLabels)
                {

                    if (!string.IsNullOrEmpty(label.Tag.ToString()))
                    {
                        //has Tag
                        var fieldName = label.Tag.ToString().ToLower();

                        fieldsOfReports[1].Add(fieldName);

                        label.ExpressionBindings.Add(new ExpressionBinding("Text", $"{fieldName}"));
                    }
                }

                fieldsOfReports[1].Add("thisReportsNumber");

                foreach (var fieldName in fieldsOfReports[1])
                {
                    Type fieldType = typeof(Int32);
                    if (CSCS_GUI.DEFINES.TryGetValue(fieldName, out DefineVariable defVar))
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

                //image in main report
                var allPictures = Reports[1].AllControls<XRPictureBox>();
                foreach (var picture in allPictures)
                {

                    if (!string.IsNullOrEmpty(picture.Tag.ToString()))
                    {
                        //has Tag
                        var imageSourceVariableName = picture.Tag.ToString().ToLower();
                        if (CSCS_GUI.DEFINES.TryGetValue(imageSourceVariableName, out DefineVariable defVar))
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

                        fieldsOfReports[thisSubreportNum].Add(fieldName);

                        label.ExpressionBindings.Add(new ExpressionBinding("Text", $"[{fieldName}]"));
                    }
                }

                fieldsOfReports[thisSubreportNum].Add("thisSubreportsMainReport");
                fieldsOfReports[thisSubreportNum].Add("thisReportsNumber");

                foreach (var fieldName in fieldsOfReports[thisSubreportNum])
                {
                    Type fieldType = typeof(Int32);
                    if (CSCS_GUI.DEFINES.TryGetValue(fieldName, out DefineVariable defVar))
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

        private void OutputReport()
        {
            List<Variable> args = script.GetFunctionArgs();
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
                else if (CSCS_GUI.DEFINES.TryGetValue(dataTableFieldName, out DefineVariable defVar))
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
                        if (CSCS_GUI.DEFINES.TryGetValue(controlName, out DefineVariable defVar)) // visibility
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
                        if (CSCS_GUI.DEFINES.TryGetValue(controlName + "cl", out DefineVariable defVar1)) // font color
                        {
                            var label = control as XRLabel;
                            label.ForeColor = System.Drawing.Color.FromName(defVar1.AsString());
                        }
                        if (CSCS_GUI.DEFINES.TryGetValue(controlName + "lt", out DefineVariable defVar2)) // left margin
                        {
                            control.LeftF = (float)defVar2.AsDouble();
                        }
                        if (CSCS_GUI.DEFINES.TryGetValue(controlName + "top", out DefineVariable defVar3)) // left margin
                        {
                            control.TopF = (float)defVar3.AsDouble();
                        }
                        if (CSCS_GUI.DEFINES.TryGetValue(controlName + "fc", out DefineVariable defVar4)) // font color
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
                        if (CSCS_GUI.DEFINES.TryGetValue(controlName + "fs", out DefineVariable defVar5)) // font size
                        {
                            System.Drawing.Font oldFont = control.GetEffectiveFont();
                            System.Drawing.Font newFont = new System.Drawing.Font(oldFont.FontFamily, (float)defVar5.AsDouble(), oldFont.Style);
                            control.Font = newFont;
                        }
                        if (CSCS_GUI.DEFINES.TryGetValue(controlName + "fn", out DefineVariable defVar6)) // font name
                        {
                            System.Drawing.Font oldFont = control.GetEffectiveFont();
                            System.Drawing.Font newFont = new System.Drawing.Font(new System.Drawing.FontFamily(defVar6.AsString()), oldFont.Size, oldFont.Style);
                            control.Font = newFont;
                        }
                        if (CSCS_GUI.DEFINES.TryGetValue(control.Name.ToLower() + "wd", out DefineVariable defVar7))
                        {
                            var requestedWidth = (float)defVar7.AsDouble();
                            if (requestedWidth != 0)
                                control.WidthF = requestedWidth;
                        }
                        if (CSCS_GUI.DEFINES.TryGetValue(control.Name.ToLower() + "ht", out DefineVariable defVar8))
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
            List<Variable> args = script.GetFunctionArgs();
            Utils.CheckArgs(args.Count, 2, m_name);

            int reportHndlNum = Utils.GetSafeInt(args, 0);
            string variableListString = Utils.GetSafeString(args, 1);

            var variableArray = variableListString.Replace(" ", "").Split(',');

            int lastRowIndex = DataSets[reportHndlNum].Tables[0].Rows.Count - 1;
            DataRow lastRow = DataSets[reportHndlNum].Tables[0].Rows[lastRowIndex];

            foreach (var variable in variableArray)
            {
                if (CSCS_GUI.DEFINES.TryGetValue(variable.ToLower(), out DefineVariable defVar))
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
