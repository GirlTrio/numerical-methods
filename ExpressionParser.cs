using System;

internal static class ExpressionParser
{
    private enum TokenType { Number, Variable, Operator, Function }

    private sealed class Token
    {
        public TokenType Type;
        public double Number;
        public string Text;
    }

    public static Func<double, double> Compile(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("Пустая функция.");

        var rpn = ToRpn(expression);
        return x => EvaluateRpn(rpn, x);
    }

    private static System.Collections.Generic.List<Token> ToRpn(string expression)
    {
        var output = new System.Collections.Generic.List<Token>();
        var ops = new System.Collections.Generic.Stack<string>();
        string s = expression.Replace(" ", "").Replace(',', '.').ToLowerInvariant();
        bool expectUnary = true;

        for (int i = 0; i < s.Length;)
        {
            char c = s[i];

            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                bool hasE = false;
                i++;
                while (i < s.Length)
                {
                    char d = s[i];
                    if (char.IsDigit(d) || d == '.') { i++; continue; }
                    if ((d == 'e') && !hasE)
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
                if (!double.TryParse(numText, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out value))
                    throw new FormatException("Некорректное число: " + numText);

                output.Add(new Token { Type = TokenType.Number, Number = value });
                expectUnary = false;
                continue;
            }

            if (char.IsLetter(c))
            {
                int start = i;
                while (i < s.Length && char.IsLetter(s[i])) i++;
                string name = s.Substring(start, i - start);

                if (name == "x")
                {
                    output.Add(new Token { Type = TokenType.Variable, Text = "x" });
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

    private static double EvaluateRpn(System.Collections.Generic.List<Token> rpn, double x)
    {
        var stack = new System.Collections.Generic.Stack<double>();

        foreach (var token in rpn)
        {
            if (token.Type == TokenType.Number)
            {
                stack.Push(token.Number);
            }
            else if (token.Type == TokenType.Variable)
            {
                stack.Push(x);
            }
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
                double a = stack.Pop();
                switch (token.Text)
                {
                    case "sin": stack.Push(Math.Sin(a)); break;
                    case "cos": stack.Push(Math.Cos(a)); break;
                    case "tan": stack.Push(Math.Tan(a)); break;
                    case "exp": stack.Push(Math.Exp(a)); break;
                    case "ln": stack.Push(Math.Log(a)); break;
                    case "log": stack.Push(Math.Log10(a)); break;
                    case "sqrt": stack.Push(Math.Sqrt(a)); break;
                    case "abs": stack.Push(Math.Abs(a)); break;
                    default: throw new FormatException("Неизвестная функция: " + token.Text);
                }
            }
        }

        if (stack.Count != 1)
            throw new FormatException("Некорректное выражение.");

        return stack.Pop();
    }

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

