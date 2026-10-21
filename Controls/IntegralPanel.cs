using System.Windows.Forms;
using System;
using System.Collections.Generic;

public class IntegralPanel : UserControl
{
    // ===== Параметры =====
    private GroupBox grpParams;
    private ComboBox cmbTaskType, cmbFunction, cmbMethod, cmbAlgorithm;
    private NumericUpDown numA, numB, numN, numEps, numM;
    private NumericUpDown numC, numD, numM2;          // второе измерение
    private Label lblTaskType, lblA, lblB, lblN, lblEps, lblFunction,
                  lblMethod, lblAlgorithm, lblM, lblC, lblD, lblM2, lblHint;
    private Button btnCalculate, btnClear;
    private DataGridView dgvSteps, dgvResults;

    // ===== Функции =====
    private Func<double, double> currentFunc;         // 1D
    private Func<double, double, double> currentFunc2D; // 2D
    private Func<double, double> currentAntiderivative;

    private const int MaxN1D = 1000000;
    private const int MaxN2D = 2000;                  // для кратного жёстко ограничиваем

    private Dictionary<(double, double), double> cache2D;

    public IntegralPanel()
    {
        BuildParams();
        BuildResults();
        UpdateFunction();
        UpdateTaskType();
    }

    // =====================================================================
    // 1. ПАРАМЕТРЫ
    // =====================================================================
    private void BuildParams()
    {
        grpParams = new GroupBox
        {
            Text = "Параметры вычисления",
            Left = 10,
            Top = 10,
            Width = 1060,
            Height = 245,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        int y0 = 22, y1 = 55, y2 = 88, y3 = 121, y4 = 154;
        int x = 15;

        // ---- Тип задачи ----
        lblTaskType = new Label { Text = "Задача:", Left = x, Top = y0 + 3, Width = 55 };
        cmbTaskType = new ComboBox
        {
            Left = x + 60,
            Top = y0,
            Width = 240,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbTaskType.Items.AddRange(new object[] {
            "Одномерный интеграл",
            "Кратный (повторное интегрирование)"
        });
        cmbTaskType.SelectedIndex = 0;
        cmbTaskType.SelectedIndexChanged += (s, e) => { UpdateTaskType(); UpdateFunction(); };

        // ---- Функция ----
        lblFunction = new Label { Text = "Функция:", Left = x + 330, Top = y0 + 3, Width = 60 };
        cmbFunction = new ComboBox
        {
            Left = x + 395,
            Top = y0,
            Width = 300,
            DropDownStyle = ComboBoxStyle.DropDown
        };
        cmbFunction.Items.AddRange(new object[] {
            "sin(x)",
            "x^2",
            "exp(-x^2)",
            "1 / (1 + x^2)",
            "cos(x)",
            // для кратного:
            "x + y",
            "x * y",
            "sin(x) * cos(y)",
            "exp(-(x^2 + y^2))"
        });
        cmbFunction.SelectedIndex = 0;
        cmbFunction.TextChanged += (s, e) => UpdateFunction();
        cmbFunction.SelectedIndexChanged += (s, e) => UpdateFunction();

        // ---- Метод ----
        lblMethod = new Label { Text = "Метод:", Left = x, Top = y1 + 3, Width = 55 };
        cmbMethod = new ComboBox
        {
            Left = x + 60,
            Top = y1,
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbMethod.Items.AddRange(new object[] {
            "Левых прямоугольников",
            "Правых прямоугольников",
            "Трапеций",
            "Парабол (Симпсона)"
        });
        cmbMethod.SelectedIndex = 0;

        // ---- Алгоритм ----
        lblAlgorithm = new Label { Text = "Алгоритм:", Left = x + 340, Top = y1 + 3, Width = 65 };
        cmbAlgorithm = new ComboBox
        {
            Left = x + 410,
            Top = y1,
            Width = 340,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbAlgorithm.Items.AddRange(new object[] {
        "Постоянный шаг",                    // index 0
        "Двойной пересчёт (правило Рунге)",  // index 1 
        "По остаточному члену",              // index 2
        "Переменный шаг (переиспользование точек)" // index 3
        });
        cmbAlgorithm.SelectedIndex = 0;
        cmbAlgorithm.SelectedIndexChanged += (s, e) => UpdateAlgorithm();

        // ---- 1D интервал ----
        lblA = new Label { Text = "a =", Left = x, Top = y2 + 3, Width = 30 };
        numA = new NumericUpDown
        {
            Left = x + 35,
            Top = y2,
            Width = 100,
            DecimalPlaces = 6,
            Minimum = -1000,
            Maximum = 1000,
            Increment = 0.1m,
            Value = 0m
        };

        lblB = new Label { Text = "b =", Left = x + 160, Top = y2 + 3, Width = 30 };
        numB = new NumericUpDown
        {
            Left = x + 195,
            Top = y2,
            Width = 100,
            DecimalPlaces = 6,
            Minimum = -1000,
            Maximum = 1000,
            Increment = 0.1m,
            Value = (decimal)Math.PI
        };

        lblN = new Label { Text = "n =", Left = x + 320, Top = y2 + 3, Width = 30 };
        numN = new NumericUpDown
        {
            Left = x + 355,
            Top = y2,
            Width = 90,
            Minimum = 1,
            Maximum = 1000000,
            Value = 10
        };

        lblEps = new Label { Text = "ε =", Left = x + 470, Top = y2 + 3, Width = 30 };
        numEps = new NumericUpDown
        {
            Left = x + 505,
            Top = y2,
            Width = 120,
            DecimalPlaces = 8,
            Minimum = 0.00000001m,
            Maximum = 1m,
            Increment = 0.0001m,
            Value = 0.0001m,
            Enabled = false
        };

        lblM = new Label { Text = "M =", Left = x + 650, Top = y2 + 3, Width = 35 };
        numM = new NumericUpDown
        {
            Left = x + 690,
            Top = y2,
            Width = 130,
            DecimalPlaces = 6,
            Minimum = 0,
            Maximum = 1000000000,
            Value = 1,
            Enabled = false
        };

        // ---- 2D интервал [c,d] и m ----
        lblC = new Label { Text = "c =", Left = x, Top = y3 + 3, Width = 30, Visible = false };
        numC = new NumericUpDown
        {
            Left = x + 35,
            Top = y3,
            Width = 100,
            DecimalPlaces = 6,
            Minimum = -1000,
            Maximum = 1000,
            Increment = 0.1m,
            Value = 0m,
            Visible = false
        };

        lblD = new Label { Text = "d =", Left = x + 160, Top = y3 + 3, Width = 30, Visible = false };
        numD = new NumericUpDown
        {
            Left = x + 195,
            Top = y3,
            Width = 100,
            DecimalPlaces = 6,
            Minimum = -1000,
            Maximum = 1000,
            Increment = 0.1m,
            Value = (decimal)Math.PI,
            Visible = false
        };

        lblM2 = new Label { Text = "m =", Left = x + 320, Top = y3 + 3, Width = 30, Visible = false };
        numM2 = new NumericUpDown
        {
            Left = x + 355,
            Top = y3,
            Width = 90,
            Minimum = 1,
            Maximum = 100000,
            Value = 10,
            Visible = false
        };

        // ---- Кнопки ----
        btnCalculate = new Button { Text = "Вычислить", Left = x, Top = y4, Width = 130, Height = 30 };
        btnClear = new Button { Text = "Очистить", Left = x + 140, Top = y4, Width = 110, Height = 30 };
        btnCalculate.Click += BtnCalculate_Click;
        btnClear.Click += BtnClear_Click;

        lblHint = new Label { Left = 15, Top = 195, Width = 1015, Height = 40 };

        grpParams.Controls.AddRange(new Control[] {
            lblTaskType, cmbTaskType, lblFunction, cmbFunction,
            lblMethod, cmbMethod, lblAlgorithm, cmbAlgorithm,
            lblA, numA, lblB, numB, lblN, numN, lblEps, numEps, lblM, numM,
            lblC, numC, lblD, numD, lblM2, numM2,
            btnCalculate, btnClear, lblHint
        });

        Controls.Add(grpParams);
        UpdateAlgorithm();
    }

    private void BuildResults()
    {
        dgvResults = new DataGridView
        {
            Left = 10,
            Top = 265,
            Width = 10,
            Height = 200,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvResults.Columns.Add("Task", "Задача");
        dgvResults.Columns.Add("Method", "Метод");
        dgvResults.Columns.Add("Algorithm", "Алгоритм");
        dgvResults.Columns.Add("N", "n");
        dgvResults.Columns.Add("M2", "m");
        dgvResults.Columns.Add("H", "h_x");
        dgvResults.Columns.Add("H2", "h_y");
        dgvResults.Columns.Add("R", "Разность / R");
        dgvResults.Columns.Add("Status", "Статус");
        dgvResults.Columns.Add("Result", "Результат");
        dgvResults.Columns.Add("Exact", "Точное");
        dgvResults.Columns.Add("Error", "Погрешность");
        dgvResults.Columns.Add("Order", "Порядок p");
        dgvResults.Columns.Add("Time", "Время, мс");
        Controls.Add(dgvResults);

        dgvSteps = new DataGridView
        {
            Left = 10,
            Top = 470,
            Width = 10,
            Height = 200,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvSteps.Columns.Add("K", "Пересчёт");
        dgvSteps.Columns.Add("N", "n");
        dgvSteps.Columns.Add("M2", "m");
        dgvSteps.Columns.Add("H", "h_x");
        dgvSteps.Columns.Add("I", "I");
        dgvSteps.Columns.Add("R", "R");
        Controls.Add(dgvSteps);
    }

    // =====================================================================
    // 2. ОБНОВЛЕНИЕ СОСТОЯНИЙ
    // =====================================================================
    private void UpdateTaskType()
    {
        bool is2D = cmbTaskType.SelectedIndex == 1;
        lblC.Visible = numC.Visible = is2D;
        lblD.Visible = numD.Visible = is2D;
        lblM2.Visible = numM2.Visible = is2D;
        UpdateAlgorithm();
    }

    private void UpdateAlgorithm()
    {
        int algorithm = cmbAlgorithm.SelectedIndex;
        bool is2D = cmbTaskType.SelectedIndex == 1;

        numEps.Enabled = algorithm != 0;
        numN.Enabled = algorithm != 2;
        numM.Enabled = algorithm == 2;
        lblM.Visible = numM.Visible = algorithm == 2;
        cmbMethod.Enabled = true;

        if (algorithm == 2)
            lblHint.Text = is2D
                ? "M — общая граница модулей нужных частных производных по x и y. " +
                  "Для обоих направлений выбираются n и m."
                : "M — верхняя граница модуля производной порядка 1 (прямоугольники), " +
                  "2 (трапеции), 4 (Симпсон).";
        else if (algorithm == 0)
            lblHint.Text = "Фиксированное число разбиений n" + (is2D ? " и m." : ".") +
                           " Для Симпсона нечётные значения увеличиваются на 1.";
        else if (algorithm == 1)
            lblHint.Text = is2D
                ? "Двойной пересчёт по обеим осям; R — оценка разности интегралов."
                : "Удваиваем n, пока |I(2n) − I(n)| не станет меньше ε.";
        else
            lblHint.Text = is2D
                ? "Уточнение сетки по обеим осям с кешированием значений f(x,y)."
                : "Удваиваем n с переиспользованием вычисленных значений функции.";
    }

    // =====================================================================
    // 3. ФУНКЦИИ (1D и 2D)
    // =====================================================================
    private void UpdateFunction()
    {
        string expression = cmbFunction.Text.Trim();
        if (expression.StartsWith("f(x) =", StringComparison.OrdinalIgnoreCase))
            expression = expression.Substring(6).Trim();

        bool is2D = cmbTaskType.SelectedIndex == 1;

        try
        {
            if (is2D)
            {
                currentFunc2D = ExpressionParser.Compile2D(expression);
                currentFunc = null;
                currentAntiderivative = null;
                return;
            }

            currentFunc2D = null;
            currentFunc = ExpressionParser.Compile(expression);

            string normalized = expression.Replace(" ", "").ToLowerInvariant();
            switch (normalized)
            {
                case "sin(x)": currentAntiderivative = x => -Math.Cos(x); break;
                case "x^2": currentAntiderivative = x => x * x * x / 3.0; break;
                case "1/(1+x^2)": currentAntiderivative = x => Math.Atan(x); break;
                case "cos(x)": currentAntiderivative = x => Math.Sin(x); break;
                case "exp(-x^2)": currentAntiderivative = null; break; // нет в элементарных
                default: currentAntiderivative = null; break;
            }
        }
        catch
        {
            currentFunc = null;
            currentFunc2D = null;
            currentAntiderivative = null;
        }
    }

    private double ExactIntegral(double a, double b)
    {
        if (currentAntiderivative == null) return double.NaN;
        return currentAntiderivative(b) - currentAntiderivative(a);
    }

    // =====================================================================
    // 4. 1D-МЕТОДЫ (без изменений)
    // =====================================================================
    private double LeftRectangles(double a, double b, int n)
    {
        double h = (b - a) / n, sum = 0;
        for (int i = 0; i < n; i++) sum += Evaluate(a + i * h);
        return sum * h;
    }

    private double RightRectangles(double a, double b, int n)
    {
        double h = (b - a) / n, sum = 0;
        for (int i = 1; i <= n; i++) sum += Evaluate(a + i * h);
        return sum * h;
    }

    private double Trapezoids(double a, double b, int n)
    {
        double h = (b - a) / n;
        double sum = 0.5 * (Evaluate(a) + Evaluate(b));
        for (int i = 1; i < n; i++) sum += Evaluate(a + i * h);
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
        double v = currentFunc(x);
        if (double.IsNaN(v) || double.IsInfinity(v))
            throw new ArithmeticException("Функция не определена при x = " + x.ToString("G10"));
        return v;
    }

    // =====================================================================
    // 5. 2D — МЕТОД ПОВТОРНОГО ИНТЕГРИРОВАНИЯ
    // =====================================================================
    /// <summary>
    /// Внутренний интеграл по y: ∫_c^d f(x,y) dy для фиксированного x.
    /// </summary>
    private double InnerIntegral(int method, double x, double c, double d, int m)
    {
        double h = (d - c) / m;
        switch (method)
        {
            case 0: // левые
                {
                    double s = 0;
                    for (int j = 0; j < m; j++) s += Eval2D(x, c + j * h);
                    return s * h;
                }
            case 1: // правые
                {
                    double s = 0;
                    for (int j = 1; j <= m; j++) s += Eval2D(x, c + j * h);
                    return s * h;
                }
            case 2: // трапеции
                {
                    double s = 0.5 * (Eval2D(x, c) + Eval2D(x, d));
                    for (int j = 1; j < m; j++) s += Eval2D(x, c + j * h);
                    return s * h;
                }
            case 3: // Симпсон
                {
                    int mm = (m % 2 == 0) ? m : m + 1;
                    double hh = (d - c) / mm;
                    double s = Eval2D(x, c) + Eval2D(x, d);
                    for (int j = 1; j < mm; j++)
                    {
                        double y = c + j * hh;
                        s += (j % 2 == 0 ? 2.0 : 4.0) * Eval2D(x, y);
                    }
                    return s * hh / 3.0;
                }
            default: return double.NaN;
        }
    }

    /// <summary>
    /// Внешний интеграл по x: ∫_a^b InnerIntegral(x) dx.
    /// </summary>
    private double Compute2D(int method, double a, double b, int n,
                             double c, double d, int m)
    {
        double h = (b - a) / n;
        switch (method)
        {
            case 0: // левые
                {
                    double s = 0;
                    for (int i = 0; i < n; i++)
                        s += InnerIntegral(method, a + i * h, c, d, m);
                    return s * h;
                }
            case 1: // правые
                {
                    double s = 0;
                    for (int i = 1; i <= n; i++)
                        s += InnerIntegral(method, a + i * h, c, d, m);
                    return s * h;
                }
            case 2: // трапеции
                {
                    double s = 0.5 * (InnerIntegral(method, a, c, d, m)
                                    + InnerIntegral(method, b, c, d, m));
                    for (int i = 1; i < n; i++)
                        s += InnerIntegral(method, a + i * h, c, d, m);
                    return s * h;
                }
            case 3: // Симпсон
                {
                    int nn = (n % 2 == 0) ? n : n + 1;
                    double hh = (b - a) / nn;
                    double s = InnerIntegral(method, a, c, d, m)
                             + InnerIntegral(method, b, c, d, m);
                    for (int i = 1; i < nn; i++)
                    {
                        double x = a + i * hh;
                        double w = (i % 2 == 0) ? 2.0 : 4.0;
                        s += w * InnerIntegral(method, x, c, d, m);
                    }
                    return s * hh / 3.0;
                }
            default: return double.NaN;
        }
    }

    private double Eval2D(double x, double y)
    {
        double v;
        if (cache2D != null && cache2D.TryGetValue((x, y), out v))
            return v;
        v = currentFunc2D(x, y);
        if (double.IsNaN(v) || double.IsInfinity(v))
            throw new ArithmeticException(
                $"Функция не определена при x = {x:G10}, y = {y:G10}");
        if (cache2D != null) cache2D[(x, y)] = v;
        return v;
    }

    // =====================================================================
    // 6. ОСТАТОЧНЫЙ ЧЛЕН (только 1D)
    // =====================================================================
    private double ResidualBound(double length, int n, int method, double m)
    {
        if (method <= 1) return length * length * m / (2.0 * n);
        if (method == 2) return Math.Pow(length, 3) * m / (12.0 * n * n);
        return Math.Pow(length, 5) * m / (180.0 * Math.Pow(n, 4));
    }

    // =====================================================================
    // 7. ГЛАВНЫЙ ОБРАБОТЧИК
    // =====================================================================
    private void AddStep(int k, int n, int m, double a, double b,
                         double value, double r)
    {
        dgvSteps.Rows.Add(
            k, n, m,
            ((b - a) / n).ToString("G10"),
            value.ToString("G12"),
            double.IsNaN(r) ? "—" : r.ToString("G8"));
    }

    // Трапеции: при удвоении разбиений пересчитываются только новые середины.
    private double InitialTrapezoid(double a, double b, int n)
    {
        return Trapezoids(a, b, n);
    }

    private double RefineTrapezoid(double oldValue, double a, double b, int oldN)
    {
        double h = (b - a) / oldN;
        double midSum = 0;
        for (int i = 0; i < oldN; i++)
            midSum += Evaluate(a + (i + 0.5) * h);
        return oldValue / 2.0 + h * midSum / 2.0;
    }

    private double FromTrapezoid(int method, double trapezoid,
                                 double a, double b, int n, double oldTrapezoid)
    {
        double h = (b - a) / n;
        if (method == 0) return trapezoid + h * (Evaluate(a) - Evaluate(b)) / 2.0;
        if (method == 1) return trapezoid + h * (Evaluate(b) - Evaluate(a)) / 2.0;
        if (method == 2) return trapezoid;
        return (4.0 * trapezoid - oldTrapezoid) / 3.0;
    }

    private double Bound2D(double xLength, double yLength, int n, int m,
                           int method, double derivativeBound)
    {
        // Предполагается, что M ограничивает необходимые чистые частные
        // производные по обеим переменным на всём прямоугольнике.
        return yLength * ResidualBound(xLength, n, method, derivativeBound)
             + xLength * ResidualBound(yLength, m, method, derivativeBound);
    }

    private void BtnCalculate_Click(object sender, EventArgs e)
    {
        dgvSteps.Rows.Clear();
        btnCalculate.Enabled = false;
        cache2D = null;
        try
        {
            double a = (double)numA.Value, b = (double)numB.Value;
            if (a == b) throw new ArgumentException("Интервал по x нулевой длины.");

            UpdateFunction();
            bool is2D = cmbTaskType.SelectedIndex == 1;
            if (is2D && currentFunc2D == null)
                throw new FormatException("Не удалось разобрать функцию от x и y.");
            if (!is2D && currentFunc == null)
                throw new FormatException("Не удалось разобрать функцию от x.");

            int method = cmbMethod.SelectedIndex;
            int algorithm = cmbAlgorithm.SelectedIndex;
            int n = (int)numN.Value;
            if (method == 3 && n % 2 != 0) n++;
            double eps = (double)numEps.Value;
            double mBound = (double)numM.Value;
            double c = is2D ? (double)numC.Value : 0;
            double d = is2D ? (double)numD.Value : 0;
            int m = is2D ? (int)numM2.Value : 0;
            if (is2D)
            {
                if (c == d) throw new ArgumentException("Интервал по y нулевой длины.");
                if (method == 3 && m % 2 != 0) m++;
                if (n > MaxN2D || m > MaxN2D)
                    throw new ArgumentException($"Для двойного интеграла n и m не больше {MaxN2D}.");
            }
            if (algorithm == 2 && mBound <= 0)
                throw new ArgumentException("Укажите положительную оценку M для производной.");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            double r = double.NaN;
            double result = double.NaN;
            int finalN = n, finalM = m;
            bool converged = true;
            var values = new List<double>();
            var stepsN = new List<int>();
            var stepsM = new List<int>();
            int k = 0;
            Func<int, int, double> compute = (nx, my) => is2D
                ? Compute2D(method, a, b, nx, c, d, my)
                : ComputeByMethod(method, a, b, nx);

            if (algorithm == 2)
            {
                // Подбор количества разбиений по гарантированной оценке остатка.
                // Для двойного интеграла распределяем ε между двумя направлениями.
                int limit = is2D ? MaxN2D : MaxN1D;
                int unit = method == 3 ? 2 : 1;
                double lx = Math.Abs(b - a), ly = Math.Abs(d - c);
                double budgetX = is2D ? eps / 2.0 : eps;
                double budgetY = eps / 2.0;
                int nx = unit;
                while ((is2D ? ly : 1.0) * ResidualBound(lx, nx, method, mBound) > budgetX)
                {
                    if (nx > limit / 2)
                        throw new ArgumentException("Точность требует слишком много разбиений по x.");
                    nx *= 2;
                }
                n = nx;
                if (is2D)
                {
                    int my = unit;
                    while (lx * ResidualBound(ly, my, method, mBound) > budgetY)
                    {
                        if (my > limit / 2)
                            throw new ArgumentException("Точность требует слишком много разбиений по y.");
                        my *= 2;
                    }
                    m = my;
                }
                r = is2D ? Bound2D(lx, ly, n, m, method, mBound)
                         : ResidualBound(lx, n, method, mBound);
                result = compute(n, m);
                AddStep(++k, n, m, a, b, result, r);
                values.Add(result); stepsN.Add(n); stepsM.Add(m);
            }
            else if (algorithm == 0)
            {
                result = compute(n, m);
                AddStep(++k, n, m, a, b, result, r);
                values.Add(result); stepsN.Add(n); stepsM.Add(m);
            }
            else if (algorithm == 3 && !is2D)
            {
                // Переменный шаг: только новые узлы, старые значения не пересчитываются.
                double t = InitialTrapezoid(a, b, n);
                double tCoarse = method == 3 ? InitialTrapezoid(a, b, n / 2) : double.NaN;
                result = FromTrapezoid(method, t, a, b, n, tCoarse);
                AddStep(++k, n, m, a, b, result, double.NaN);
                values.Add(result); stepsN.Add(n); stepsM.Add(m);
                while (true)
                {
                    if (n > MaxN1D / 2) { converged = false; break; }
                    double nextT = RefineTrapezoid(t, a, b, n);
                    int nextN = n * 2;
                    double next = FromTrapezoid(method, nextT, a, b, nextN, t);
                    r = Math.Abs(next - result);
                    t = nextT; n = nextN; result = next;
                    AddStep(++k, n, m, a, b, result, r);
                    values.Add(result); stepsN.Add(n); stepsM.Add(m);
                    if (r <= eps) break;
                }
            }
            else
            {
                // Двойной пересчёт: в 2D одновременно уточняем обе оси.
                // В режиме 3 значения узлов (x,y) кешируются между пересчётами.
                if (is2D && algorithm == 3)
                    cache2D = new Dictionary<(double, double), double>();
                result = compute(n, m);
                AddStep(++k, n, m, a, b, result, double.NaN);
                values.Add(result); stepsN.Add(n); stepsM.Add(m);
                int limit = is2D ? MaxN2D : MaxN1D;
                while (true)
                {
                    if (n > limit / 2 || (is2D && m > limit / 2))
                    {
                        converged = false;
                        break;
                    }
                    n *= 2;
                    if (is2D) m *= 2;
                    double next = compute(n, m);
                    r = Math.Abs(next - result);
                    result = next;
                    AddStep(++k, n, m, a, b, result, r);
                    values.Add(result); stepsN.Add(n); stepsM.Add(m);
                    if (r <= eps) break;
                }
            }
            finalN = n; finalM = m;
            sw.Stop();
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new ArithmeticException("Результат не конечен.");

            double exact = is2D ? double.NaN : ExactIntegral(a, b);
            double err = double.IsNaN(exact) ? double.NaN : Math.Abs(result - exact);
            string orderText = "—";
            if (!double.IsNaN(exact) && values.Count >= 2)
            {
                int last = values.Count - 1;
                bool halfStep = stepsN[last] == 2 * stepsN[last - 1] &&
                    (!is2D || stepsM[last] == 2 * stepsM[last - 1]);
                double e0 = Math.Abs(values[last - 1] - exact);
                double e1 = Math.Abs(values[last] - exact);
                if (halfStep && e0 > 0 && e1 > 0)
                    orderText = (Math.Log(e0 / e1) / Math.Log(2.0)).ToString("F3");
            }

            dgvResults.Rows.Add(
                is2D ? "Кратный" : "1D", cmbMethod.Text, cmbAlgorithm.Text,
                finalN, is2D ? finalM.ToString() : "—",
                ((b - a) / finalN).ToString("G10"),
                is2D ? ((d - c) / finalM).ToString("G10") : "—",
                double.IsNaN(r) ? "—" : r.ToString("G8"),
                converged ? "Вычислено" : "Точность не достигнута",
                result.ToString("G12"),
                double.IsNaN(exact) ? "—" : exact.ToString("G12"),
                double.IsNaN(err) ? "—" : err.ToString("G8"),
                orderText, sw.Elapsed.TotalMilliseconds.ToString("F3"));

            if (!converged)
                MessageBox.Show("Достигнут предел разбиений. Требуемая точность не достигнута.",
                    "Предел вычислений", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка вычисления",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            cache2D = null;
            btnCalculate.Enabled = true;
        }
    }

    private void BtnClear_Click(object sender, EventArgs e)
    {
        dgvResults.Rows.Clear();
        dgvSteps.Rows.Clear();
    }
}