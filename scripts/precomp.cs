
CreateDLL blah.dll 

h = ImportDLL("C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades\System.Runtime.dll");
h = ImportDLL("C:\Users\vassi\Documents\GitHub\cscs_wpf\Modules\CSCS.Test\bin\Debug\CSCS.Tests.dll");
h = ImportDLL("CSCS.Tests.dll");
InvokeDLL(h, "blah");

dllfunction DoWork1(string data, double eps, int ct) {
    Console.WriteLine("XaxaxA " + eps + " " + ct);
    return "OOk3";
}
dllfunction DoWork2(string data, double eps, int ct) {
    Console.WriteLine("Lala " + eps + " " + ct);
    return "OOk4";
}
h1=ImportDll("DoWork1");
h2=ImportDll("DoWork2");
InvokeDLL(h1, "blah1", 0.1, 9);
InvokeDLL(h2, "blah2", 0.2, 19);

dllfunction RunCycle1(int loops) {
    double result = 0;
    for (int i = 0; i < loops; i++)
    {
      result += Cycle(i);
    }
    return result;
}
dllsub double Cycle(double x) {
    var result = Math.Sqrt(Math.PI * Math.E - Math.Sin(x) * Math.Cos(x));
    return result;
}
h=ImportDll("RunCycle1");
InvokeDLL(h, 1000000);

dllfunction ShowMessageBox(msg, string caption = "Info", string answerType = "ok", string messageType = "info") {
    var result = MessageBoxFunction.ShowMessageBox(msg, caption, answerType, messageType);
    return result;
}
dllfunction ShowQuestion(msg, string caption = "Question") {
    var result = MessageBoxFunction.ShowMessageBox(msg, caption, "yesno", "question");
    return result;
}

h=ImportDll("ShowMessageBox");
InvokeDLL(h, "ShowMessageBox", "Hi there");
InvokeDLL(h, "ShowQuestion", "Do you want to quit?");
ShowQuestion("Do you agree?");
)

inter1 = GetInterpreterHandle();
DEFINE var2 type a;
var2 = "aaaaaaaaaa";
MessageBox(var2);
x = "First interpreter";
MessageBox("interpreter1=" + inter1 + ", x=" + x); // 1, First interpreter

hndlNum = NewInterpreter();
MessageBox("hndlNum = " + hndlNum);
SetInterpreter(2);
inter2 = GetInterpreterHandle();
var2 = "bbbbb";
MessageBox(var2); // program crashes here
x = "Second interpreter";
print("interpreter2=" + inter2 + ", x=" + x); // 2, Second interpreter


SetInterpreter(1);
inter3 = GetInterpreterHandle();
MessageBox("interpreter3=" + inter3 + ", x=" + x); // 1, First interpreter
MessageBox(var2);

abstract=21;
quit; abstract=3;

using System; using System.Collections; using System.Collections.Generic; using System.Collections.Specialized; using System.Globalization; using System.Linq; using System.Linq.Expressions; using System.Reflection; using System.Text; using System.Threading; using System.Threading.Tasks;
using WpfCSCS;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
namespace SplitAndMerge {
  public partial class Precompiler {
    public static Variable callNext
(Interpreter __interpreter,
 List<string> __varStr,
 List<double> __varNum,
 List<List<string>> __varArrStr,
 List<List<double>> __varArrNum,
 List<Dictionary<string, string>> __varMapStr,
 List<Dictionary<string, double>> __varMapNum,
 List<Variable> __varVar) {

     string __argsTempStr= "";
     string __actionTempVar = "";
     ParsingScript __scriptTempVar = null;
     ParserFunction __funcTempVar = null;
     GetVarFunction __varTempGetVar = null;
     Variable __varTempVar = null;
     bool __boolTempVar = false;
            var x=10000;
      __interpreter.AddGlobalOrLocalVariable("x", new GetVarFunction(Variable.ConvertToVariable(x)));
            __actionTempVar ="";
      __argsTempStr ="handle,\"f\",\"@1\"";
      __scriptTempVar = new ParsingScript(__interpreter, __argsTempStr);
      __funcTempVar = new ParserFunction(__scriptTempVar, "findv", '(', ref __actionTempVar);
      __varTempVar = __funcTempVar.GetValue(__scriptTempVar);

              var i=0;
      for(i=0;i<x;i++) {
        __interpreter.AddGlobalOrLocalVariable("i", new GetVarFunction(Variable.ConvertToVariable(i)));
                __actionTempVar ="";
        __argsTempStr ="handle,\"n\"";
        __scriptTempVar = new ParsingScript(__interpreter, __argsTempStr);
        __funcTempVar = new ParserFunction(__scriptTempVar, "findv", '(', ref __actionTempVar);
        __varTempVar = __funcTempVar.GetValue(__scriptTempVar);

        x--;
        __interpreter.AddGlobalOrLocalVariable("x", new GetVarFunction(Variable.ConvertToVariable(x)));
      }
      return Variable.EmptyInstance;


    }
    }
}
