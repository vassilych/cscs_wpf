
//using LiveChartsCore;
//using LiveChartsCore.Kernel;
//using LiveChartsCore.SkiaSharpView;
//using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
//using LiveChartsCore.SkiaSharpView.Painting;
//using LiveChartsCore.SkiaSharpView.VisualElements;
//using LiveChartsCore.SkiaSharpView.WPF;
using LiveCharts.Defaults;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using SplitAndMerge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfCSCS
{
    public class Charts
    {
        public void Init(CSCS_GUI gui)
        {
            Gui = gui;
            Interpreter interpreter = gui.Interpreter;

            interpreter.RegisterFunction(Constants.CHART, new ChartFunction());
            interpreter.RegisterFunction(Constants.PIE_CHART, new PieChartFunction());
            interpreter.RegisterFunction(Constants.GAUGE_CHART, new GaugeChartFunction());
            

        }

        //static List<SolidColorPaint> colorList = new List<SolidColorPaint>()
        //{
        //    new SolidColorPaint(new SKColor(0, 0, 255)), // blue
        //    new SolidColorPaint(new SKColor(255, 0, 0)), // red
        //    new SolidColorPaint(new SKColor(0, 255, 0)) // green
        //};

        CSCS_GUI Gui { get; set; }


        class ChartFunction : ParserFunction
        {
            static Dictionary<string, string> chartsTypes = new Dictionary<string, string>();
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);

                var gui = CSCS_GUI.GetInstance(script);

                var widgetName = Utils.GetSafeString(args, 0).ToLower();
                var optionString = Utils.GetSafeString(args, 1).ToLower();
                var valueVariable = Utils.GetSafeVariable(args, 2);
                var value2Variable = Utils.GetSafeVariable(args, 3);
                if (value2Variable == null)
                    value2Variable = new Variable();
                var value3Variable = Utils.GetSafeVariable(args, 4);
                if (value3Variable == null)
                    value3Variable = new Variable();

                var widget = gui.GetWidget(widgetName);
                if (widget is CartesianChart)
                {
                    var cartesianWidget = widget as CartesianChart;

                    if (optionString == "seriestype")
                    {
                        chartsTypes[widgetName] = valueVariable.String.ToLower();
                    }
                    else if (optionString == "init")
                    {
                        cartesianWidget.Series = new ISeries[] { };
                    }
                    else if (optionString == "values")
                    {
                        if (valueVariable.Tuple.Count > 0)
                        {
                            List<double> newList = new List<double>();

                            foreach (var item in valueVariable.Tuple)
                            {
                                newList.Add(item.Value);
                            }

                            var temp = cartesianWidget.Series.ToList();
                            if (chartsTypes[widgetName] == "columnseries")
                            {
                                temp.Add(new ColumnSeries<double>() { 
                                    Values = newList, /*, Fill = colorList[temp.Count]*/
                                    YToolTipLabelFormatter = (chartPoint) => $"{newList[(int)chartPoint.Coordinate.SecondaryValue].ToString("N")}"
                                });
                                //Debug.WriteLine("temp.Count = " + temp.Count);
                            }
                            else if (chartsTypes[widgetName] == "lineseries")
                            {
                                temp.Add(new LineSeries<double>()
                                {
                                    Values = newList,
                                    //TooltipLabelFormatter = (chartPoint) => $"{newList[(int)chartPoint.Context.Series.Name.SecondaryValue]}" + $": chartPoint.Context.Series.Name}: {chartPoint.PrimaryValue:C0}"
                                    //TooltipLabelFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name} - {} - {newList[(int)chartPoint.SecondaryValue]}"
                                    YToolTipLabelFormatter = (chartPoint) => $"{newList[(int)chartPoint.Coordinate.SecondaryValue].ToString("N")}",
                                    //Fill = new SolidColorPaint(SKColors.Transparent)
                                    Fill = null,
                                    Stroke = new SolidColorPaint(new SKColor(100, 100, 100), 3),
                                    //GeometryFill = null,
                                    GeometryStroke = new SolidColorPaint(new SKColor(100, 100, 100), 3),
                                    GeometrySize = 0

                                });
                            }
                            if (!string.IsNullOrEmpty(value2Variable.String))
                            {
                                temp.Last().Name = value2Variable.String;
                            }

                            cartesianWidget.Series = temp;
                        }
                    }
                    else if (optionString == "xaxisname")
                    {
                        cartesianWidget.XAxes.First().Name = valueVariable.String;
                    }
                    else if (optionString == "yaxisname")
                    {
                        cartesianWidget.YAxes.First().Name = valueVariable.String;
                    }
                    else if (optionString == "labels")
                    {
                        if (valueVariable.String?.ToLower() == "x")
                        {
                            cartesianWidget.XAxes.First().Labels = value3Variable.Tuple.Select(p => p.String).ToList();
                            cartesianWidget.XAxes.First().TextSize = value2Variable.Value != 0 ? value2Variable.Value : 15;
                        }
                        else if (valueVariable.String?.ToLower() == "y")
                        {
                            cartesianWidget.YAxes.First().TextSize = value2Variable.Value != 0 ? value2Variable.Value : 15;
                        }

                    }
                    else if (optionString == "xlabelsrotation")
                    {
                        cartesianWidget.XAxes.First().LabelsRotation = valueVariable.Value;
                    }
                    else if (optionString == "ylabelsrotation")
                    {
                        cartesianWidget.YAxes.First().LabelsRotation = valueVariable.Value;
                    }
                    else if (optionString == "title")
                    {
                        //cartesianWidget.Title = new LabelVisual()
                        //{
                        //    Text = valueVariable.String,
                        //    TextSize = value2Variable.Value != 0 ? value2Variable.Value : 20,
                        //    Padding = new LiveChartsCore.Drawing.Padding(15),
                        //    Paint = new SolidColorPaint(SKColors.DarkSlateGray)
                        //};

                    }
                    else if (optionString == "separatorstep")
                    {
                        var firstXAxis = cartesianWidget.XAxes.FirstOrDefault();
                        if (firstXAxis != null)
                        {
                            firstXAxis.MinStep = valueVariable.Value;
                            firstXAxis.ForceStepToMin = true;
                        }
                    }
                    else if (optionString == "margins")
                    {
                        cartesianWidget.DrawMargin = new LiveChartsCore.Measure.Margin((float)valueVariable.Tuple[0].Value, (float)valueVariable.Tuple[1].Value, (float)valueVariable.Tuple[2].Value, (float)valueVariable.Tuple[3].Value);
                    }
                    else if (optionString == "tooltipdecimalplaces")
                    {
                        var aljksd = cartesianWidget.ToolTip;

                        foreach (var series in cartesianWidget.Series)
                        {
                            if (chartsTypes[widgetName] == "columnseries")
                            {
                                (series as ColumnSeries<double>).YToolTipLabelFormatter = (chartPoint) => $"{chartPoint.Coordinate.PrimaryValue.ToString($"N{valueVariable.Value}")}";
                            }
                            else if (chartsTypes[widgetName] == "lineseries")
                            {
                                (series as LineSeries<double>).YToolTipLabelFormatter = (chartPoint) => $"{chartPoint.Coordinate.PrimaryValue.ToString($"N{valueVariable.Value}")}";
                            }
                        }
                    }
                    else if (optionString == "color.series")
                    {
                        var parameter = Utils.GetSafeString(args, 2);

                        var series_ienum_property = widget.GetType().GetProperty("Series");
                        var g = args[2].Type == Variable.VarType.ARRAY ? args[2].Tuple.Select(a => a.String).ToArray() : parameter.Split('|');
                        int i = 0;

                        foreach (var item in (System.Collections.IEnumerable)series_ienum_property.GetValue(widget, null))
                        {
                            if(item is ColumnSeries<double>)
                            {
                                item.GetType().GetProperty("Fill").SetValue(item, new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColor.Parse(ToHex((Color)ColorConverter.ConvertFromString(g[i++])))), null);
                            }
                            else if(item is LineSeries<double>)
                            {
                                item.GetType().GetProperty("Stroke").SetValue(item, new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColor.Parse(ToHex((Color)ColorConverter.ConvertFromString(g[i]))), 3), null);
                                item.GetType().GetProperty("GeometryStroke").SetValue(item, new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColor.Parse(ToHex((Color)ColorConverter.ConvertFromString(g[i++]))), 3), null);
                            }
                        }
                    }
                    else if (optionString == "text.seriesnames")
                    {
                        var parameter = Utils.GetSafeString(args, 2);

                        var series_ienum_property = widget.GetType().GetProperty("Series");
                        var g = args[2].Type == Variable.VarType.ARRAY ? args[2].Tuple.Select(a => a.String).ToArray() : parameter.Split('|');
                        int i = 0;

                        foreach (var item in (System.Collections.IEnumerable)series_ienum_property.GetValue(widget, null))
                            item.GetType().GetProperty("Name").SetValue(item, g[i++], null);
                    }

                }

                return Variable.EmptyInstance;
            }

            public string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        class PieChartFunction : ParserFunction
        {
            static Dictionary<string, string> chartsTypes = new Dictionary<string, string>();
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);

                var gui = CSCS_GUI.GetInstance(script);

                var widgetName = Utils.GetSafeString(args, 0).ToLower();
                var optionString = Utils.GetSafeString(args, 1).ToLower();
                var valueVariable = Utils.GetSafeVariable(args, 2);
                var value2Variable = Utils.GetSafeVariable(args, 3);
                if (value2Variable == null)
                    value2Variable = new Variable();
                var value3Variable = Utils.GetSafeVariable(args, 4);
                if (value3Variable == null)
                    value3Variable = new Variable();

                var widget = gui.GetWidget(widgetName);
                if (widget is PieChart)
                {
                    var pieWidget = widget as PieChart;

                    if (optionString == "seriestype")
                    {
                        chartsTypes[widgetName] = valueVariable.String.ToLower();
                    }
                    else if (optionString == "init")
                    {
                        pieWidget.Series = new ISeries[] { };

                        //pieWidget.DrawMargin = new LiveChartsCore.Measure.Margin() { Left = 200, Top = 50 };
                    }
                    else if (optionString == "values")
                    {
                        if (valueVariable.Value > 0)
                        {
                            var temp = pieWidget.Series.ToList();

                            if(value3Variable.Value == 0)
                            {
                                temp.Add(new PieSeries<double>() { Values = new List<double>() { valueVariable.Value }/*, Fill = colorList[temp.Count]*/, ToolTipLabelFormatter = tooltipFormater});
                            }
                            else
                            {
                                temp.Add(new PieSeries<double>() { Values = new List<double>() { valueVariable.Value }/*, Fill = colorList[temp.Count]*/, ToolTipLabelFormatter = tooltipFormater, MaxRadialColumnWidth = value3Variable.Value });
                            }

                            ////List<double> newList = new List<double>();


                            //foreach (var item in valueVariable.Tuple)
                            //{
                            //    newList.Add(item.Value);
                            //    if (chartsTypes[widgetName] == "pie")
                            //    {
                            //        temp.Add(new PieSeries<double>() { Values = new List<double>() { item.Value }/*, Fill = colorList[temp.Count]*/ });
                            //    }
                            //}

                            if (!string.IsNullOrEmpty(value2Variable.String))
                            {
                                temp.Last().Name = value2Variable.String;
                            }

                            pieWidget.Series = temp;
                        }
                    }
                    //else if (optionString == "xaxisname")
                    //{
                    //    pieWidget.XAxes.First().Name = valueVariable.String;
                    //}
                    //else if (optionString == "yaxisname")
                    //{
                    //    pieWidget.YAxes.First().Name = valueVariable.String;
                    //}
                    //else if (optionString == "labels")
                    //{
                    //    if(valueVariable.String?.ToLower() == "x")
                    //    {
                    //        pieWidget.XAxes.First().Labels = value3Variable.Tuple.Select(p => p.String).ToList();
                    //        pieWidget.XAxes.First().TextSize = value2Variable.Value != 0 ? value2Variable.Value : 15;
                    //    }
                    //    else if (valueVariable.String?.ToLower() == "y")
                    //    {
                    //        pieWidget.YAxes.First().TextSize = value2Variable.Value != 0 ? value2Variable.Value : 15;
                    //    }

                    //}
                    //else if (optionString == "xlabelsrotation")
                    //{
                    //    pieWidget.XAxes.First().LabelsRotation = valueVariable.Value;
                    //}
                    //else if (optionString == "ylabelsrotation")
                    //{
                    //    pieWidget.YAxes.First().LabelsRotation = valueVariable.Value;
                    //}
                    else if (optionString == "title")
                    {
                        //pieWidget.Title = new LabelVisual()
                        //{
                        //    Text = valueVariable.String,
                        //    TextSize = value2Variable.Value != 0 ? value2Variable.Value : 20,
                        //    Padding = new LiveChartsCore.Drawing.Padding(15),
                        //    Paint = new SolidColorPaint(SKColors.DarkSlateGray)
                        //};
                    }
                    //else if(optionString == "separatorstep")
                    //{
                    //    var firstXAxis = pieWidget.XAxes.FirstOrDefault();
                    //    if (firstXAxis != null)
                    //    {
                    //        firstXAxis.MinStep = valueVariable.Value;
                    //        firstXAxis.ForceStepToMin = true;
                    //    }
                    //}
                    else if (optionString == "margins")
                    {
                        pieWidget.DrawMargin = new LiveChartsCore.Measure.Margin((float)valueVariable.Tuple[0].Value, (float)valueVariable.Tuple[1].Value, (float)valueVariable.Tuple[2].Value, (float)valueVariable.Tuple[3].Value);
                    }
                    else if (optionString == "tooltipdecimalplaces")
                    {
                        var aljksd = pieWidget.ToolTip;

                        foreach (var series in pieWidget.Series)
                        {
                            if (chartsTypes[widgetName] == "columnseries")
                            {
                                (series as ColumnSeries<double>).TooltipLabelFormatter = (chartPoint) => $"{chartPoint.PrimaryValue.ToString($"N{valueVariable.Value}")}";
                            }
                            else if (chartsTypes[widgetName] == "lineseries")
                            {
                                (series as LineSeries<double>).TooltipLabelFormatter = (chartPoint) => $"{chartPoint.PrimaryValue.ToString($"N{valueVariable.Value}")}";
                            }
                        }

                    }

                }

                return Variable.EmptyInstance;
            }

            private string tooltipFormater(ChartPoint<double, DoughnutGeometry, LabelGeometry> arg)
            {
                return arg.PrimaryValue.ToString("N0");
            }
        }
        
        class GaugeChartFunction : ParserFunction
        {
            static Dictionary<string, string> chartsTypes = new Dictionary<string, string>();
            protected override Variable Evaluate(ParsingScript script)
            {
                List<Variable> args = script.GetFunctionArgs();
                Utils.CheckArgs(args.Count, 2, m_name);

                var gui = CSCS_GUI.GetInstance(script);

                var widgetName = Utils.GetSafeString(args, 0).ToLower();
                var optionString = Utils.GetSafeString(args, 1).ToLower();
                var valueVariable = Utils.GetSafeVariable(args, 2);

                var widget = gui.GetWidget(widgetName);
                if (widget is PieChart)
                {
                    var pieWidget = widget as PieChart;

                    if (optionString == "value")
                    {
                        if (valueVariable.Value > 0)
                        {
                            pieWidget.Series = GaugeGenerator.BuildSolidGauge(
                                new GaugeItem(
                                    valueVariable.Value,          // the gauge value
                                    series =>    // the series style
                                    {
                                        series.MaxRadialColumnWidth = 50;
                                        series.DataLabelsSize = 50;
                                    })
                                );
                            pieWidget.Tooltip = null;
                        }
                    }
                    else if (optionString == "color")
                    {
                        var newColor = valueVariable.String;

                        var series_ienum_property = widget.GetType().GetProperty("Series");

                        foreach (var item in (System.Collections.IEnumerable)series_ienum_property.GetValue(widget, null))
                        {
                            //item ;
                            if (item is PieSeries<LiveChartsCore.Defaults.ObservableValue>)
                            {
                                if (!(item as PieSeries<LiveChartsCore.Defaults.ObservableValue>).SeriesProperties.HasFlag(SeriesProperties.GaugeFill))
                                    item.GetType().GetProperty("Fill").SetValue(item, new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColor.Parse(ToHex((Color)ColorConverter.ConvertFromString(newColor)))), null);
                            }
                        }
                    }
                }

                return Variable.EmptyInstance;
            }

            public string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }




        //class ChartFunction_livechartsWPF : ParserFunction
        //{
        //    static Dictionary<string, string> chartsTypes = new Dictionary<string, string>();
        //    protected override Variable Evaluate(ParsingScript script)
        //    {
        //        List<Variable> args = script.GetFunctionArgs();
        //        Utils.CheckArgs(args.Count, 2, m_name);

        //        var gui = CSCS_GUI.GetInstance(script);

        //        var widgetName = Utils.GetSafeString(args, 0).ToLower();
        //        var optionString = Utils.GetSafeString(args, 1).ToLower();
        //        var valueVariable = Utils.GetSafeVariable(args, 2);
        //        var value2Variable = Utils.GetSafeVariable(args, 3);
        //        if (value2Variable == null)
        //            value2Variable = new Variable();
        //        var value3Variable = Utils.GetSafeVariable(args, 4);
        //        if (value3Variable == null)
        //            value3Variable = new Variable();

        //        var widget = gui.GetWidget(widgetName);
        //        if (widget is CartesianChart)
        //        {
        //            var cartesianWidget = widget as CartesianChart;

        //            if (optionString == "seriestype")
        //            {
        //                chartsTypes[widgetName] = valueVariable.String.ToLower();
        //            }
        //            else if (optionString == "init")
        //            {
        //                cartesianWidget.Series = new SeriesCollection { };
        //            }
        //            else if (optionString == "values")
        //            {
        //                if (valueVariable.Tuple.Count > 0)
        //                {
        //                    List<double> newList = new List<double>();

        //                    foreach (var item in valueVariable.Tuple)
        //                    {
        //                        newList.Add(item.Value);
        //                    }

        //                    ChartValues<double> chartValues = new ChartValues<double>(newList);

        //                    if (chartsTypes[widgetName] == "columnseries")
        //                    {
        //                        cartesianWidget.Series.Add(new ColumnSeries
        //                        {
        //                            Title = value2Variable.String,
        //                            Values = chartValues
        //                        });
        //                    }
        //                    else if (chartsTypes[widgetName] == "lineseries")
        //                    {
        //                        cartesianWidget.Series.Add(new LineSeries
        //                        {
        //                            Title = value2Variable.String,
        //                            Values = chartValues,
        //                            //PointGeometrySize = 5,
        //                            //StrokeThickness
        //                        });
        //                    }

        //                }
        //            }
        //            else if (optionString == "xaxisname")
        //            {
        //                cartesianWidget.AxisX.First().Name = valueVariable.String;
        //            }
        //            else if (optionString == "yaxisname")
        //            {
        //                cartesianWidget.AxisY.First().Name = valueVariable.String;
        //            }
        //            else if (optionString == "labels")
        //            {
        //                if(valueVariable.String?.ToLower() == "x")
        //                {
        //                    //cartesianWidget.AxisX.First().Labels = value3Variable.Tuple.Select(p => p.String).ToList();
        //                    cartesianWidget.AxisX = new AxesCollection()
        //                    {
        //                        new Axis()
        //                        {
        //                            Labels = value3Variable.Tuple.Select(p => p.String).ToArray()
        //                        }
        //                    };

        //                    //cartesianWidget.AxisX.First().FontSize = value2Variable.Value != 0 ? value2Variable.Value : 15;
        //                }
        //                else if (valueVariable.String?.ToLower() == "y")
        //                {
        //                    cartesianWidget.AxisY.First().FontSize = value2Variable.Value != 0 ? value2Variable.Value : 15;
        //                }
                        
        //            }
        //            else if (optionString == "xlabelsrotation")
        //            {
        //                cartesianWidget.AxisX.First().LabelsRotation = valueVariable.Value;
        //            }
        //            else if (optionString == "ylabelsrotation")
        //            {
        //                cartesianWidget.AxisY.First().LabelsRotation = valueVariable.Value;
        //            }
        //            else if (optionString == "title")
        //            {
        //                //cartesianWidget.Title = new LabelVisual()
        //                //{
        //                //    Text = valueVariable.String,
        //                //    TextSize = value2Variable.Value != 0 ? value2Variable.Value : 20,
        //                //    Padding = new LiveChartsCore.Drawing.Padding(15),
        //                //    Paint = new SolidColorPaint(SKColors.DarkSlateGray)
        //                //};
                        
        //            }
        //            //else if(optionString == "separatorstep")
        //            //{
        //            //    var firstXAxis = cartesianWidget.AxisX.FirstOrDefault();
        //            //    if (firstXAxis != null)
        //            //    {
        //            //        firstXAxis.MinStep = valueVariable.Value;
        //            //        firstXAxis.ForceStepToMin = true;
        //            //    }
        //            //}
        //            else if(optionString == "margins")
        //            {
        //                //cartesianWidget.DrawMargin = new LiveChartsCore.Measure.Margin((float)valueVariable.Tuple[0].Value, (float)valueVariable.Tuple[1].Value, (float)valueVariable.Tuple[2].Value, (float)valueVariable.Tuple[3].Value);
        //                cartesianWidget.Margin = new System.Windows.Thickness((float)valueVariable.Tuple[0].Value, (float)valueVariable.Tuple[1].Value, (float)valueVariable.Tuple[2].Value, (float)valueVariable.Tuple[3].Value);
        //            }
        //            //else if(optionString == "tooltipdecimalplaces")
        //            //{
        //            //    //var aljksd = cartesianWidget.ToolTip;

        //            //    foreach (var series in cartesianWidget.Series)
        //            //    {
        //            //        if (chartsTypes[widgetName] == "columnseries")
        //            //        {
        //            //            (series as ColumnSeries).ToolTip = (chartPoint) => $"{chartPoint.PrimaryValue.ToString($"N{valueVariable.Value}")}";
        //            //        }
        //            //        else if (chartsTypes[widgetName] == "lineseries")
        //            //        {
        //            //            (series as LineSeries<double>).TooltipLabelFormatter = (chartPoint) => $"{chartPoint.PrimaryValue.ToString($"N{valueVariable.Value}")}";
        //            //        }
        //            //    }

        //            //}
        //            else if(optionString == "color.series")
        //            {
        //                var parameter = Utils.GetSafeString(args, 2);

        //                var series_ienum_property = widget.GetType().GetProperty("Series");
        //                var g = args[2].Type == Variable.VarType.ARRAY ? args[2].Tuple.Select(a => a.String).ToArray() : parameter.Split('|');
        //                int i = 0;

        //                foreach (var item in (System.Collections.IEnumerable)series_ienum_property.GetValue(widget, null))
        //                    //item.GetType().GetProperty("Fill").SetValue(item, new Brush(SkiaSharp.SKColor.Parse(ToHex((Color)ColorConverter.ConvertFromString(g[i++])))), null);
        //                    item.GetType().GetProperty("Fill").SetValue(item, new SolidColorBrush((Color)ColorConverter.ConvertFromString(g[i++])));//(ToHex((Color)ColorConverter.ConvertFromString()))); ;//, null);
        //            }
        //            else if(optionString == "text.seriesnames")
        //            {
        //                var parameter = Utils.GetSafeString(args, 2);

        //                var series_ienum_property = widget.GetType().GetProperty("Series");
        //                var g = args[2].Type == Variable.VarType.ARRAY ? args[2].Tuple.Select(a => a.String).ToArray() : parameter.Split('|');
        //                int i = 0;

        //                foreach (var item in (System.Collections.IEnumerable)series_ienum_property.GetValue(widget, null))
        //                    item.GetType().GetProperty("Name").SetValue(item, g[i++], null);
        //            }
                    
        //        }

        //        return Variable.EmptyInstance;
        //    }

        //    public string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        //}

        //class PieChartFunction_livechartsWPF : ParserFunction
        //{
        //    static Dictionary<string, string> chartsTypes = new Dictionary<string, string>();
        //    protected override Variable Evaluate(ParsingScript script)
        //    {
        //        List<Variable> args = script.GetFunctionArgs();
        //        Utils.CheckArgs(args.Count, 2, m_name);

        //        var gui = CSCS_GUI.GetInstance(script);

        //        var widgetName = Utils.GetSafeString(args, 0).ToLower();
        //        var optionString = Utils.GetSafeString(args, 1).ToLower();
        //        var valueVariable = Utils.GetSafeVariable(args, 2);
        //        var value2Variable = Utils.GetSafeVariable(args, 3);
        //        if (value2Variable == null)
        //            value2Variable = new Variable();
        //        var value3Variable = Utils.GetSafeVariable(args, 4);
        //        if (value3Variable == null)
        //            value3Variable = new Variable();

        //        var widget = gui.GetWidget(widgetName);
        //        if (widget is PieChart)
        //        {
        //            var pieWidget = widget as PieChart;

        //            if (optionString == "seriestype")
        //            {
        //                chartsTypes[widgetName] = valueVariable.String.ToLower();
        //            }
        //            else if (optionString == "init")
        //            {
        //                pieWidget.Series = new SeriesCollection { };

        //                //pieWidget.DrawMargin = new LiveChartsCore.Measure.Margin() { Left = 200, Top = 50 };
        //            }
        //            else if (optionString == "values")
        //            {
        //                if (valueVariable.Value > 0)
        //                {
        //                    pieWidget.Series.Add(new PieSeries() { Values = new ChartValues<double>() { valueVariable.Value }, Title = value2Variable.String });
        //                }
        //            }
                    
        //            else if (optionString == "title")
        //            {
        //                //pieWidget.Title = new LabelVisual()
        //                //{
        //                //    Text = valueVariable.String,
        //                //    TextSize = value2Variable.Value != 0 ? value2Variable.Value : 20,
        //                //    Padding = new LiveChartsCore.Drawing.Padding(15),
        //                //    Paint = new SolidColorPaint(SKColors.DarkSlateGray)
        //                //};
        //            }
                    
        //            else if (optionString == "margins")
        //            {
        //                pieWidget.Margin = new System.Windows.Thickness((float)valueVariable.Tuple[0].Value, (float)valueVariable.Tuple[1].Value, (float)valueVariable.Tuple[2].Value, (float)valueVariable.Tuple[3].Value);
        //            }
        //            //else if (optionString == "tooltipdecimalplaces")
        //            //{
        //            //    var aljksd = pieWidget.ToolTip;

        //            //    foreach (var series in pieWidget.Series)
        //            //    {
        //            //        if (chartsTypes[widgetName] == "columnseries")
        //            //        {
        //            //            (series as ColumnSeries<double>).TooltipLabelFormatter = (chartPoint) => $"{chartPoint.PrimaryValue.ToString($"N{valueVariable.Value}")}";
        //            //        }
        //            //        else if (chartsTypes[widgetName] == "lineseries")
        //            //        {
        //            //            (series as LineSeries<double>).TooltipLabelFormatter = (chartPoint) => $"{chartPoint.PrimaryValue.ToString($"N{valueVariable.Value}")}";
        //            //        }
        //            //    }

        //            //}

        //        }

        //        return Variable.EmptyInstance;
        //    }

        //    //private string tooltipFormater(ChartPoint<double, DoughnutGeometry, LabelGeometry> arg)
        //    //{
        //    //    return arg.PrimaryValue.ToString("N0");
        //    //}
        //}








    }
}
