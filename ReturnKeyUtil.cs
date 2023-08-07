using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfCSCS
{
    public class ReturnKeyUtil
    {
        public Key ReturnKey(string key)
        {
            Key keyPressed = Key.None;
            switch (key.ToLower())
            {
                case "f1":
                    keyPressed = Key.F1;
                    break;
                case "f2":
                    keyPressed = Key.F2;
                    break;
                case "f3":
                    keyPressed = Key.F3;
                    break;
                case "f4":
                    keyPressed = Key.F4;
                    break;
                case "f5":
                    keyPressed = Key.F5;
                    break;
                case "f6":
                    keyPressed = Key.F6;
                    break;
                case "f7":
                    keyPressed = Key.F7;
                    break;
                case "f8":
                    keyPressed = Key.F8;
                    break;
                case "f9":
                    keyPressed = Key.F9;
                    break;
                case "f10":
                    keyPressed = Key.F10;
                    break;
                case "f11":
                    keyPressed = Key.F11;
                    break;
                case "f12":
                    keyPressed = Key.F12;
                    break;
                case "esc":
                    keyPressed = Key.Escape;
                    break;
                case "pg_up":
                    keyPressed = Key.PageUp;
                    break;
                case "pg_dn":
                    keyPressed = Key.PageDown;
                    break;
                case "home":
                    keyPressed = Key.Home;
                    break;
                case "end":
                    keyPressed = Key.End;
                    break;
                case "enter_key":
                    keyPressed = Key.Enter;
                    break;
                //case "ctl_pg_up":
                //    keyPressed = Key.PageUp;
                //    break;
                default:
                    // code block
                    break;
            }
            return keyPressed;
        }
    }
}
