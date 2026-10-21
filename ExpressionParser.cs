using System;
using System.Collections.Generic;

internal static class ExpressionParser
{
    private enum TokenType { Number, Variable, Operator, Function }

    private sealed class Token
    {
        public TokenType Type;
        public double Number;
        public string Text;
    }

    // ==================================================================
    //  1D-компиляция (как было)
    // ==================================================================
    public static Func<double, double> Compile(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("Пустая функция.");

        var rpn = ToRpn(expression);
        return x => EvaluateRpn(rpn, x);
    }

    // ==================================================================
    //  2D-компиляция — НОВОЕ
    // ==================================================================
    public static Func<double, double, double> Compile2D(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("Пустая функция.");

        var rpn = ToRpn(expression);
        return (x, y) => EvaluateRpn2D(rpn, x, y);
    }

    // ==================================================================
    //  Разбор в ОПН — общий для 1D и 2D.
    //  Единственное отличие от исходника: имя "y" теперь тоже переменная.
    // ==================================================================
    private static List<Token> ToRpn(string expression)
    {
        var output = new List<Token>();
        var ops = new Stack<string>();
        string s = expression.Replace(" ", "").Replace(',', '.').ToLowerInvariant();
        bool expectUnary = true;

        for (int i = 0; i < s.Length;)
        {
            char c = s[i];

            // ---------- Число ----------
            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                bool hasE = false;
                i++;
                while (i < s.Length)
                {
                    char d = s[i];
                    if (char.IsDigit(d) || d == '.') { i++; continue; }
                    if (d == 'e' && !hasE)
                    {
                        hasE = true;
                        i++;
                        if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                        continue;
                    }
                    break;
                }

                string numText = s.Substring(start, i - start);
                double value;
                if (!double.TryParse(numText,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out value))
                    throw new FormatException("Некорректное число: " + numText);

                output.Add(new Token { Type = TokenType.Number, Number = value });
                expectUnary = false;
                continue;
            }

            // ---------- Имя / переменная / функция ----------
            if (char.IsLetter(c))
            {
                int start = i;
                while (i < s.Length && char.IsLetter(s[i])) i++;
                string name = s.Substring(start, i - start);

                if (name == "x" || name == "y")          // <-- добавлен y
                {
                    output.Add(new Token { Type = TokenType.Variable, Text = name });
                    expectUnary = false;
                }
                else if (name == "pi")
                {
                    output.Add(new Token { Type = TokenType.Number, Number = Math.PI });
                    expectUnary = false;
                }
                else if (name == "e")
                {
                    output.Add(new Token { Type = TokenType.Number, Number = Math.E });
                    expectUnary = false;
                }
                else if (IsFunction(name))
                {
                    ops.Push(name);
                    expectUnary = true;
                }
                else
                {
                    throw new FormatException("Неизвестное имя: " + name);
                }
                continue;
            }

            // ---------- Скобки ----------
            if (c == '(')
            {
                ops.Push("(");
                i++;
                expectUnary = true;
                continue;
            }

            if (c == ')')
            {
                while (ops.Count > 0 && ops.Peek() != "(")
                    output.Add(MakeOperatorOrFunction(ops.Pop()));

                if (ops.Count == 0)
                    throw new FormatException("Лишняя закрывающая скобка.");

                ops.Pop();
                if (ops.Count > 0 && IsFunction(ops.Peek()))
                    output.Add(new Token { Type = TokenType.Function, Text = ops.Pop() });

                i++;
                expectUnary = false;
                continue;
            }

            // ---------- Операторы ----------
            if (IsOperatorChar(c))
            {
                string op = c.ToString();
                if (c == '-' && expectUnary)
                    op = "u-";
                else if (c == '+' && expectUnary)
                {
                    i++;
                    continue;
                }

                while (op != "u-" && ops.Count > 0 && IsOperator(ops.Peek()))
                {
                    string top = ops.Peek();
                    if ((IsRightAssociative(op) && Precedence(op) < Precedence(top)) ||
                        (!IsRightAssociative(op) && Precedence(op) <= Precedence(top)))
                        output.Add(new Token { Type = TokenType.Operator, Text = ops.Pop() });
                    else
                        break;
                }

                ops.Push(op);
                i++;
                expectUnary = true;
                continue;
            }

            throw new FormatException("Недопустимый символ: " + c);
        }

        while (ops.Count > 0)
        {
            string op = ops.Pop();
            if (op == "(") throw new FormatException("Не закрыта скобка.");
            output.Add(MakeOperatorOrFunction(op));
        }

        return output;
    }

    private static Token MakeOperatorOrFunction(string text)
    {
        return new Token
        {
            Type = IsFunction(text) ? TokenType.Function : TokenType.Operator,
            Text = text
        };
    }

    // ==================================================================
    //  1D-вычисление 
    // ==================================================================
    private static double EvaluateRpn(List<Token> rpn, double x)
    {
        var stack = new Stack<double>();

        foreach (var token in rpn)
        {
            if (token.Type == TokenType.Number)
                stack.Push(token.Number);

            else if (token.Type == TokenType.Variable)
                stack.Push(x);                        // 1D: любая переменная = x

            else if (token.Type == TokenType.Operator)
            {
                if (token.Text == "u-")
                {
                    if (stack.Count < 1) throw new FormatException("Ошибка унарного минуса.");
                    stack.Push(-stack.Pop());
                    continue;
                }
                if (stack.Count < 2) throw new FormatException("Недостаточно операндов.");
                double b = stack.Pop();
                double a = stack.Pop();
                switch (token.Text)
                {
                    case "+": stack.Push(a + b); break;
                    case "-": stack.Push(a - b); break;
                    case "*": stack.Push(a * b); break;
                    case "/": stack.Push(a / b); break;
                    case "^": stack.Push(Math.Pow(a, b)); break;
                    default: throw new FormatException("Неизвестный оператор: " + token.Text);
                }
            }
            else if (token.Type == TokenType.Function)
            {
                if (stack.Count < 1) throw new FormatException("Нет аргумента функции.");
                stack.Push(ApplyFunction(token.Text, stack.Pop()));
            }
        }

        if (stack.Count != 1)
            throw new FormatException("Некорректное выражение.");

        return stack.Pop();
    }

    // ==================================================================
    //  2D-вычисление
    // ==================================================================
    private static double EvaluateRpn2D(List<Token> rpn, double x, double y)
    {
        var stack = new Stack<double>();

        foreach (var token in rpn)
        {
            if (token.Type == TokenType.Number)
                stack.Push(token.Number);

            else if (token.Type == TokenType.Variable)
                stack.Push(token.Text == "y" ? y : x);   // различаем x и y

            else if (token.Type == TokenType.Operator)
            {
                if (token.Text == "u-")
                {
                    if (stack.Count < 1) throw new FormatException("Ошибка унарного минуса.");
                    stack.Push(-stack.Pop());
                    continue;
                }
                if (stack.Count < 2) throw new FormatException("Недостаточно операндов.");
                double b = stack.Pop();
                double a = stack.Pop();
                switch (token.Text)
                {
                    case "+": stack.Push(a + b); break;
                    case "-": stack.Push(a - b); break;
                    case "*": stack.Push(a * b); break;
                    case "/": stack.Push(a / b); break;
                    case "^": stack.Push(Math.Pow(a, b)); break;
                    default: throw new FormatException("Неизвестный оператор: " + token.Text);
                }
            }
            else if (token.Type == TokenType.Function)
            {
                if (stack.Count < 1) throw new FormatException("Нет аргумента функции.");
                stack.Push(ApplyFunction(token.Text, stack.Pop()));
            }
        }

        if (stack.Count != 1)
            throw new FormatException("Некорректное выражение.");

        return stack.Pop();
    }

    // ==================================================================
    //  Общее применение функции — вынесено, чтобы не дублировать switch
    // ==================================================================
    private static double ApplyFunction(string name, double a)
    {
        switch (name)
        {
            case "sin": return Math.Sin(a);
            case "cos": return Math.Cos(a);
            case "tan": return Math.Tan(a);
            case "exp": return Math.Exp(a);
            case "ln": return Math.Log(a);
            case "log": return Math.Log10(a);
            case "sqrt": return Math.Sqrt(a);
            case "abs": return Math.Abs(a);
            default: throw new FormatException("Неизвестная функция: " + name);
        }
    }

    // ==================================================================
    //  Служебное
    // ==================================================================
    private static bool IsFunction(string s)
    {
        return s == "sin" || s == "cos" || s == "tan" || s == "exp" ||
               s == "ln" || s == "log" || s == "sqrt" || s == "abs";
    }

    private static bool IsOperatorChar(char c)
    {
        return c == '+' || c == '-' || c == '*' || c == '/' || c == '^';
    }

    private static bool IsOperator(string s)
    {
        return s == "+" || s == "-" || s == "*" || s == "/" || s == "^" || s == "u-";
    }

    private static int Precedence(string op)
    {
        switch (op)
        {
            case "u-": return 3;
            case "^": return 3;
            case "*":
            case "/": return 2;
            case "+":
            case "-": return 1;
            default: return 0;
        }
    }

    private static bool IsRightAssociative(string op)
    {
        return op == "^" || op == "u-";
    }
}