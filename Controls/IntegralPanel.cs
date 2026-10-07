
using System.Windows.Forms;
using System;

public class IntegralPanel : UserControl
{
    private GroupBox grpParams;
    private ComboBox cmbFunction, cmbMethod, cmbAlgorithm;
    private NumericUpDown numA, numB, numN, numEps;
    private Label lblA, lblB, lblN, lblEps, lblFunction, lblMethod, lblAlgorithm;
    private Button btnCalculate, btnClear;
    private NumericUpDown numM;
    private Label lblM, lblHint;
    private DataGridView dgvSteps;
    private DataGridView dgvResults;

    private Func<double, double> currentFunc;
    private Func<double, double> currentAntiderivative;

    private const int MaxN = 1000000;

    public IntegralPanel()
    {
        BuildParams();
        BuildResults();
        UpdateFunction();
    }

    private void BuildParams()
    {
        grpParams = new GroupBox
        {
            Text = "Параметры вычисления",
            Left = 10,
            Top = 10,
            Width = 1060,
            Height = 180,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        int y1 = 25, y2 = 60, y3 = 95;
        int x = 15;

        lblFunction = new Label { Text = "Функция:", Left = x, Top = y1 + 3, Width = 65 };
        cmbFunction = new ComboBox { Left = x + 70, Top = y1, Width = 200, DropDownStyle = ComboBoxStyle.DropDown };
        cmbFunction.Items.AddRange(new object[] {
                "sin(x)",
                "x^2",
                "exp(-x^2)",
                "1 / (1 + x^2)",
                "cos(x)",
                "sqrt(0.8*x^2+1)/(x+sqrt(1.5*x^2+2))"
            });
        cmbFunction.SelectedIndex = 0;

        lblMethod = new Label { Text = "Метод:", Left = x + 300, Top = y1 + 3, Width = 55 };
        cmbMethod = new ComboBox { Left = x + 360, Top = y1, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbMethod.Items.AddRange(new object[] {
                "Левых прямоугольников",
                "Правых прямоугольников",
                "Трапеций",
                "Парабол (Симпсона)"
            });
        cmbMethod.SelectedIndex = 0;

        lblAlgorithm = new Label { Text = "Алгоритм:", Left = x + 610, Top = y1 + 3, Width = 70 };
        cmbAlgorithm = new ComboBox { Left = x + 680, Top = y1, Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbAlgorithm.Items.AddRange(new object[] {
                "Постоянный шаг",
                "Двойной пересчёт (h / 2)",
                "По остаточному члену",
                "Алгоритм 1 (двойной пересчёт, h / 2)",
                "Алгоритм 2 (переменный шаг)"
            });
        cmbAlgorithm.SelectedIndex = 0;

        lblA = new Label { Text = "a =", Left = x, Top = y2 + 3, Width = 30 };
        numA = new NumericUpDown { Left = x + 35, Top = y2, Width = 100, DecimalPlaces = 6, Minimum = -1000, Maximum = 1000, Increment = 0.1m, Value = 0m };

        lblB = new Label { Text = "b =", Left = x + 160, Top = y2 + 3, Width = 30 };
        numB = new NumericUpDown { Left = x + 195, Top = y2, Width = 100, DecimalPlaces = 6, Minimum = -1000, Maximum = 1000, Increment = 0.1m, Value = (decimal)Math.PI };

        lblN = new Label { Text = "n =", Left = x + 320, Top = y2 + 3, Width = 30 };
        numN = new NumericUpDown { Left = x + 355, Top = y2, Width = 90, Minimum = 1, Maximum = 1000000, Value = 10 };

        lblEps = new Label { Text = "ε =", Left = x + 470, Top = y2 + 3, Width = 30 };
        numEps = new NumericUpDown { Left = x + 505, Top = y2, Width = 120, DecimalPlaces = 8, Minimum = 0.00000001m, Maximum = 1m, Increment = 0.0001m, Value = 0.0001m, Enabled = false };

        btnCalculate = new Button { Text = "Вычислить", Left = x, Top = y3, Width = 130, Height = 30 };
        btnClear = new Button { Text = "Очистить", Left = x + 140, Top = y3, Width = 110, Height = 30 };

        lblM = new Label { Text = "M =", Left = 675, Top = y2 + 3, Width = 35 };
        numM = new NumericUpDown
        {
            Left = 715,
            Top = y2,
            Width = 130,
            DecimalPlaces = 6,
            Minimum = 0,
            Maximum = 1000000000,
            Value = 1,
            Enabled = false
        };

        lblHint = new Label { Left = 15, Top = 135, Width = 1015, Height = 38 };

        grpParams.Controls.AddRange(new Control[] { lblM, numM, lblHint });
        grpParams.Controls.AddRange(new Control[] {
                lblFunction, cmbFunction,
                lblMethod, cmbMethod,
                lblAlgorithm, cmbAlgorithm,
                lblA, numA, lblB, numB,
                lblN, numN, lblEps, numEps,
                btnCalculate, btnClear
            });

        btnCalculate.Click += BtnCalculate_Click;
        btnClear.Click += BtnClear_Click;
        cmbAlgorithm.SelectedIndexChanged += (s, e) => UpdateAlgorithm();
        cmbFunction.TextChanged += (s, e) => UpdateFunction();
        cmbFunction.SelectedIndexChanged += (s, e) => UpdateFunction();

        UpdateAlgorithm();

        Controls.Add(grpParams);
    }

    private void BuildResults()
    {
        dgvResults = new DataGridView
        {
            Left = 10,
            Top = 200,
            Width = 10,
            Height = 180,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvResults.Columns.Add("Method", "Метод");
        dgvResults.Columns.Add("Algorithm", "Алгоритм");
        dgvResults.Columns.Add("N", "Разбиения n");
        dgvResults.Columns.Add("H", "Шаг h");
        dgvResults.Columns.Add("R", "Разность / оценка R");
        dgvResults.Columns.Add("Status", "Статус");
        dgvResults.Columns.Add("Result", "Результат");
        dgvResults.Columns.Add("Error", "Погрешность");
        dgvResults.Columns.Add("Time", "Время, мс");

        Controls.Add(dgvResults);

        dgvSteps = new DataGridView
        {
            Left = 10,
            Top = 395,
            Width = 10,
            Height = 0,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvSteps.Columns.Add("K", "Пересчёт");
        dgvSteps.Columns.Add("N", "n");
        dgvSteps.Columns.Add("H", "h");
        dgvSteps.Columns.Add("I", "I(n)");
        dgvSteps.Columns.Add("R", "R");
        Controls.Add(dgvSteps);
    }

    private void UpdateAlgorithm()
    {
        int algorithm = cmbAlgorithm.SelectedIndex;
        numEps.Enabled = algorithm != 0;
        numN.Enabled = algorithm != 2;
        numM.Enabled = algorithm == 2;

        // Метод доступен для ВСЕХ алгоритмов — пользователь сам выбирает,
        // каким методом считать. Псевдокод из PDF был лишь примером.
        cmbMethod.Enabled = true;

        if (algorithm == 2)
            lblHint.Text = "M — верхняя граница модуля производной на всём интервале: 1-й порядок для прямоугольников, 2-й для трапеций, 4-й для Симпсона.";
        else if (algorithm == 0)
            lblHint.Text = "n — число разбиений; для Симпсона нечётное n увеличивается на 1.";
        else if (algorithm == 3)
            lblHint.Text = "Алгоритм 1 из PDF: IN = 0; R = |I2N − IN|; h = h/2, пока R > ε. Работает с любым методом.";
        else if (algorithm == 4)
            lblHint.Text = "Алгоритм 2 из PDF: переменный шаг с переиспользованием точек. Работает для прямоугольников и трапеций (для Симпсона — обычный двойной пересчёт).";
        else
            lblHint.Text = "Начинаем с заданного n, удваиваем число разбиений до |I(2n) − I(n)| < ε.";
    }

    // ============== ФУНКЦИИ ==============

    private void UpdateFunction()
    {
        string expression = cmbFunction.Text.Trim();
        if (expression.StartsWith("f(x) =", StringComparison.OrdinalIgnoreCase))
            expression = expression.Substring(6).Trim();

        try
        {
            currentFunc = ExpressionParser.Compile(expression);

            string normalized = expression.Replace(" ", "").ToLowerInvariant();
            switch (normalized)
            {
                case "sin(x)":
                    currentAntiderivative = x => -Math.Cos(x);
                    break;
                case "x^2":
                    currentAntiderivative = x => x * x * x / 3.0;
                    break;
                case "1/(1+x^2)":
                    currentAntiderivative = x => Math.Atan(x);
                    break;
                case "cos(x)":
                    currentAntiderivative = x => Math.Sin(x);
                    break;
                default:
                    currentAntiderivative = null;
                    break;
            }
        }
        catch
        {
            currentFunc = null;
            currentAntiderivative = null;
        }
    }

    private double ExactIntegral(double a, double b)
    {
        if (currentAntiderivative == null) return double.NaN;
        return currentAntiderivative(b) - currentAntiderivative(a);
    }

    // ============== ЧИСЛЕННЫЕ МЕТОДЫ ==============

    private double LeftRectangles(double a, double b, int n)
    {
        double h = (b - a) / n;
        double sum = 0;
        for (int i = 0; i < n; i++)
            sum += Evaluate(a + i * h);
        return sum * h;
    }

    private double RightRectangles(double a, double b, int n)
    {
        double h = (b - a) / n;
        double sum = 0;
        for (int i = 1; i <= n; i++)
            sum += Evaluate(a + i * h);
        return sum * h;
    }

    private double Trapezoids(double a, double b, int n)
    {
        double h = (b - a) / n;
        double sum = 0.5 * (Evaluate(a) + Evaluate(b));
        for (int i = 1; i < n; i++)
            sum += Evaluate(a + i * h);
        return sum * h;
    }

    private double Simpson(double a, double b, int n)
    {
        if (n % 2 != 0) n++;
        double h = (b - a) / n;
        double sum = Evaluate(a) + Evaluate(b);
        for (int i = 1; i < n; i++)
        {
            double x = a + i * h;
            sum += (i % 2 == 0 ? 2.0 : 4.0) * Evaluate(x);
        }
        return sum * h / 3.0;
    }

    private double ComputeByMethod(int method, double a, double b, int n)
    {
        switch (method)
        {
            case 0: return LeftRectangles(a, b, n);
            case 1: return RightRectangles(a, b, n);
            case 2: return Trapezoids(a, b, n);
            case 3: return Simpson(a, b, n);
            default: return double.NaN;
        }
    }

    private double Evaluate(double x)
    {
        double value = currentFunc(x);
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArithmeticException("Функция не определена или не конечна при x = " + x.ToString("G10"));
        return value;
    }

    /// <summary>
    /// Оценка остаточного члена по формулам (1)-(3) из PDF.
    /// ВАЖНО: в PDF для метода парабол используется знаменатель (2n)^4,
    /// где n — число ПАР отрезков. В нашем коде n — общее число отрезков,
    /// поэтому (2n)^4 = 16*n^4.
    /// </summary>
    private double ResidualBound(double length, int n, int method, double m)
    {
        if (method <= 1) return length * length * m / (2.0 * n);
        if (method == 2) return Math.Pow(length, 3) * m / (12.0 * n * n);
        return Math.Pow(length, 5) * m / (180.0 * Math.Pow(2.0 * n, 4));
    }

    private void AddStep(int k, int n, double a, double b, double value, double r)
    {
        dgvSteps.Rows.Add(k, n, ((b - a) / n).ToString("G10"),
            value.ToString("G12"), double.IsNaN(r) ? "—" : r.ToString("G8"));
    }

    private void BtnCalculate_Click(object sender, EventArgs e)
    {
        dgvSteps.Rows.Clear();
        btnCalculate.Enabled = false;
        try
        {
            double a = (double)numA.Value, b = (double)numB.Value;
            if (a == b) throw new ArgumentException("Интервал нулевой длины.");
            UpdateFunction();
            if (currentFunc == null) throw new FormatException("Не удалось разобрать функцию.");

            int method = cmbMethod.SelectedIndex;
            int algorithm = cmbAlgorithm.SelectedIndex;
            int n = (int)numN.Value;
            if (method == 3 && n % 2 != 0) n++;
            double eps = (double)numEps.Value;
            double m = (double)numM.Value;

            double r = double.NaN;
            double result = 0;
            int finalN = n;
            bool converged = true;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (algorithm == 0)
            {
                // Постоянный шаг
                result = ComputeByMethod(method, a, b, n);
                finalN = n;
                AddStep(1, n, a, b, result, r);
            }
            else if (algorithm == 1)
            {
                // Двойной пересчёт (h / 2): |I(2n) - I(n)| < ε
                double previous = ComputeByMethod(method, a, b, n);
                int k = 0;
                AddStep(++k, n, a, b, previous, double.NaN);

                while (true)
                {
                    if (n > MaxN / 2)
                    {
                        converged = false;
                        result = previous;
                        finalN = n;
                        break;
                    }
                    n *= 2;
                    result = ComputeByMethod(method, a, b, n);
                    r = Math.Abs(result - previous);
                    AddStep(++k, n, a, b, result, r);
                    if (r < eps) { finalN = n; break; }
                    previous = result;
                }
            }
            else if (algorithm == 2)
            {
                // По остаточному члену
                int unit = method == 3 ? 2 : 1;
                int lo = 1, hi = MaxN / unit;
                if (ResidualBound(Math.Abs(b - a), hi * unit, method, m) >= eps / 2)
                    throw new ArgumentException("Для заданных M и ε требуется более 1 000 000 разбиений.");

                while (lo < hi)
                {
                    int mid = lo + (hi - lo) / 2;
                    if (ResidualBound(Math.Abs(b - a), mid * unit, method, m) < eps / 2) hi = mid;
                    else lo = mid + 1;
                }
                n = lo * unit;
                r = ResidualBound(Math.Abs(b - a), n, method, m);
                result = ComputeByMethod(method, a, b, n);
                finalN = n;
                AddStep(1, n, a, b, result, r);
            }
            else if (algorithm == 3)
            {
                // ===== Алгоритм 1 из PDF (обобщённый) =====
                // IN = 0; h = (b-a)/n;
                // Цикл: I2N = метод с шагом h; R = |I2N - IN|;
                //        если k > 1 и R <= E — стоп; иначе IN = I2N; h /= 2.
                const int maxIter = 40;
                double h = (b - a) / n;
                double IN = 0;
                int k = 0;

                while (k < maxIter)
                {
                    int currentN = Math.Max(1, (int)Math.Round((b - a) / h));
                    if (currentN > MaxN)
                    {
                        converged = false;
                        break;
                    }

                    double I2N = ComputeByMethod(method, a, b, currentN);
                    double R = Math.Abs(I2N - IN);
                    AddStep(++k, currentN, a, b, I2N, k == 1 ? double.NaN : R);

                    if (k > 1 && R <= eps)
                    {
                        result = I2N;
                        r = R;
                        finalN = currentN;
                        break;
                    }

                    IN = I2N;
                    h = h / 2.0;
                }

                if (k >= maxIter)
                {
                    converged = false;
                    result = IN;
                    finalN = (int)Math.Round((b - a) / h);
                }
            }
            else if (algorithm == 4)
            {

                if (method == 3)
                {
                    // Симпсон: обычный двойной пересчёт
                    double previous = ComputeByMethod(method, a, b, n);
                    int k = 0;
                    AddStep(++k, n, a, b, previous, double.NaN);
                    while (true)
                    {
                        if (n > MaxN / 2) { converged = false; result = previous; finalN = n; break; }
                        n *= 2;
                        result = ComputeByMethod(method, a, b, n);
                        r = Math.Abs(result - previous);
                        AddStep(++k, n, a, b, result, r);
                        if (r < eps) { finalN = n; break; }
                        previous = result;
                    }
                }
                else
                {
                    // Прямоугольники и трапеции: переменный шаг с переиспользованием
                    const int maxIter = 40;

                    double hv = (b - a) / n;   // шаг вычисления
                    double S;                  // накопленная сумма ординат
                    int k = 0;

                    // 1-я итерация: стандартный проход с шагом hv
                    S = ComputeSumForVariableStep(method, a, b, hv, hv, null);
                    double IN = S * hv;
                    AddStep(++k, Math.Max(1, (int)Math.Round((b - a) / hv)), a, b, IN, double.NaN);

                    // Последующие итерации
                    while (k < maxIter)
                    {
                        double hs = hv / 2.0;   // шаг смещения
                        double hd = hv;         // шаг движения = предыдущий шаг вычисления

                        // Добавляем только новые точки: a+hs, a+hs+hd, a+hs+2*hd, ...
                        // Для трапеций нужно следить за концевыми точками a и b.
                        double newSum = ComputeSumForVariableStep(method, a, b, hs, hd, S);
                        S += newSum;
                        double I2N = S * hs;    // новый шаг = hs
                        double R = Math.Abs(I2N - IN);

                        int currentN = Math.Max(1, (int)Math.Round((b - a) / hs));
                        AddStep(++k, currentN, a, b, I2N, R);

                        if (R <= eps)
                        {
                            result = I2N;
                            r = R;
                            finalN = currentN;
                            break;
                        }

                        IN = I2N;
                        hv = hs; // новый шаг вычисления

                        if ((b - a) / hv > MaxN)
                        {
                            converged = false;
                            result = I2N;
                            finalN = currentN;
                            break;
                        }
                    }

                    if (k >= maxIter)
                    {
                        converged = false;
                        result = IN;
                    }
                }
            }

            sw.Stop();

            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new ArithmeticException("Результат вычисления не конечен.");

            double exact = ExactIntegral(a, b);
            dgvResults.Rows.Add(
                cmbMethod.Text,
                cmbAlgorithm.Text,
                finalN,
                ((b - a) / finalN).ToString("G10"),
                double.IsNaN(r) ? "—" : r.ToString("G8"),
                converged ? "Вычислено" : "Точность не достигнута",
                result.ToString("G12"),
                double.IsNaN(exact) ? "—" : Math.Abs(result - exact).ToString("G8"),
                sw.Elapsed.TotalMilliseconds.ToString("F3"));

            if (!converged)
                MessageBox.Show("Достигнут предел разбиений. Последний результат выведен, но требуемая точность не достигнута.",
                    "Предел вычислений", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка вычисления", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { btnCalculate.Enabled = true; }
    }
    private double ComputeSumForVariableStep(int method, double a, double b, double hs, double hd, double? previousSum)
    {
        double sum = 0;

        if (method == 2) // Трапеции
        {
            // Концевые точки добавляем только на первой итерации
            if (previousSum == null)
                sum += 0.5 * (Evaluate(a) + Evaluate(b));
            // Внутренние точки
            for (double x = a + hs; x < b - hd / 2.0; x += hd)
                sum += Evaluate(x);
        }
        else // Левые или правые прямоугольники
        {
            for (double x = a + hs; x < b - hd / 2.0; x += hd)
                sum += Evaluate(x);
        }

        return sum;
    }

    private void BtnClear_Click(object sender, EventArgs e)
    {
        dgvResults.Rows.Clear();
        dgvSteps.Rows.Clear();
    }
}

