using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class DifferentialPanel : UserControl
{
    private ComboBox cmbProblem, cmbMethod, cmbFunction, cmbAlgorithm;
    private TextBox txtSecond, txtDx, txtDy, txtDz;
    private Label lblSecond, lblDx, lblDy, lblDz;
    private Func<double, double[], double>[] customDerivatives;
    private NumericUpDown numX0, numY0, numV0, numZ0, numXEnd, numN, numEps;
    private Label lblX0, lblY0, lblV0, lblZ0, lblXEnd, lblN, lblEps, lblFunction, lblHint;
    private Button btnCalculate, btnClear;
    private DataGridView dgvSteps, dgvResults;
    private PictureBox picPlot;
    private Func<double, double, double> currentFunc;
    private Func<double, double> currentExact;
    private const int MaxN = 100000;
    private const int MaxLogRows = 1500;
    private const double MinVisibleRange = 1e-12;

    private sealed class Solution
    {
        public int N;
        public double H;
        public List<double> X = new List<double>();
        public List<double[]> Y = new List<double[]>();
        public double[] Last { get { return Y[Y.Count - 1]; } }
    }

    public DifferentialPanel()
    {
        AutoScroll = true;
        BuildControls();
        UpdateProblem();
    }

    private static NumericUpDown Number(decimal value, int left, int top, int decimals = 6)
    {
        return new NumericUpDown
        {
            Left = left,
            Top = top,
            Width = 93,
            DecimalPlaces = decimals,
            Minimum = -1000,
            Maximum = 1000,
            Increment = 0.1m,
            Value = value
        };
    }

    private static Label Caption(string text, int left, int top, int width = 50)
    {
        return new Label { Text = text, Left = left, Top = top + 4, Width = width };
    }

    private void BuildControls()
    {
        Width = 1110;
        Height = 860;
        var group = new GroupBox
        {
            Text = "Численные методы решения дифференциальных уравнений",
            Left = 10,
            Top = 10,
            Width = 1075,
            Height = 257
        };

        group.Controls.Add(Caption("Задача:", 12, 25, 55));
        cmbProblem = new ComboBox
        {
            Left = 70,
            Top = 25,
            Width = 302,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbProblem.Items.AddRange(new object[] {
            "1. Уравнение первого порядка",
            "2. Уравнение второго порядка",
            "3. Система из трёх уравнений"
        });
        cmbProblem.SelectedIndex = 0;
        cmbProblem.SelectedIndexChanged += (s, e) => UpdateProblem();
        group.Controls.Add(cmbProblem);

        group.Controls.Add(Caption("Метод:", 390, 25, 55));
        cmbMethod = new ComboBox
        {
            Left = 449,
            Top = 25,
            Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbMethod.Items.AddRange(new object[] { "Эйлера", "Рунге–Кутта (4-й порядок)" });
        cmbMethod.SelectedIndex = 0;
        group.Controls.Add(cmbMethod);

        group.Controls.Add(Caption("Алгоритм:", 681, 25, 73));
        cmbAlgorithm = new ComboBox
        {
            Left = 757,
            Top = 25,
            Width = 302,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbAlgorithm.Items.AddRange(new object[] {
            "Постоянный шаг", "Двойной пересчёт (правило Рунге)"
        });
        cmbAlgorithm.SelectedIndex = 0;
        cmbAlgorithm.SelectedIndexChanged += (s, e) => UpdateAlgorithm();
        group.Controls.Add(cmbAlgorithm);

        lblFunction = Caption("f(x,y):", 12, 61, 60);
        cmbFunction = new ComboBox
        {
            Left = 76,
            Top = 59,
            Width = 305,
            DropDownStyle = ComboBoxStyle.DropDown
        };
        cmbFunction.Items.AddRange(new object[] {
            "y * (1 - x)", "x + y", "y * sin(x)", "x * y", "y - x"
        });
        cmbFunction.SelectedIndex = 0;
        cmbFunction.TextChanged += (s, e) => UpdateFunction();
        group.Controls.Add(lblFunction);
        group.Controls.Add(cmbFunction);

        // Редактируемые уравнения для задач 2 и 3.
        lblSecond = Caption("y'' =", 12, 60, 54);
        txtSecond = new TextBox { Left = 76, Top = 59, Width = 450, Text = "-v/x-y" };
        lblDx = Caption("x' =", 12, 60, 54);
        txtDx = new TextBox { Left = 76, Top = 59, Width = 235, Text = "-2*x+5*z" };
        lblDy = Caption("y' =", 322, 60, 52);
        txtDy = new TextBox { Left = 375, Top = 59, Width = 310, Text = "sin(t-1)*x-y+3*z" };
        lblDz = Caption("z' =", 697, 60, 52);
        txtDz = new TextBox { Left = 752, Top = 59, Width = 307, Text = "-x+2*z" };
        foreach (Control c in new Control[] { lblSecond, txtSecond, lblDx, txtDx,
            lblDy, txtDy, lblDz, txtDz }) group.Controls.Add(c);

        lblX0 = Caption("x₀ =", 12, 100, 40);
        numX0 = Number(0, 54, 100);
        lblY0 = Caption("y₀ =", 157, 100, 40);
        numY0 = Number(1, 199, 100);
        lblV0 = Caption("y'₀ =", 301, 100, 48);
        numV0 = Number(-0.44m, 352, 100);
        lblZ0 = Caption("z₀ =", 457, 100, 40);
        numZ0 = Number(1, 499, 100);
        lblXEnd = Caption("X =", 602, 100, 35);
        numXEnd = Number(1, 639, 100);
        lblN = Caption("n =", 743, 100, 32);
        numN = Number(10, 775, 100, 0);
        numN.Minimum = 1;
        numN.Maximum = MaxN;
        numN.Increment = 1;
        lblEps = Caption("ε =", 882, 100, 35);
        numEps = Number(0.0001m, 919, 100, 8);
        numEps.Minimum = 0.00000001m;
        numEps.Maximum = 1;
        numEps.Increment = 0.0001m;
        foreach (Control c in new Control[] { lblX0, numX0, lblY0, numY0,
            lblV0, numV0, lblZ0, numZ0, lblXEnd, numXEnd,
            lblN, numN, lblEps, numEps }) group.Controls.Add(c);

        btnCalculate = new Button
        {
            Text = "Вычислить",
            Left = 12,
            Top = 141,
            Width = 130,
            Height = 28
        };
        btnClear = new Button
        {
            Text = "Очистить",
            Left = 151,
            Top = 141,
            Width = 110,
            Height = 28
        };
        btnCalculate.Click += Calculate;
        btnClear.Click += (s, e) => {
            dgvSteps.Rows.Clear(); dgvResults.Rows.Clear();
            if (picPlot.Image != null) { var old = picPlot.Image; picPlot.Image = null; old.Dispose(); }
        };
        group.Controls.Add(btnCalculate);
        group.Controls.Add(btnClear);
        lblHint = new Label
        {
            Left = 278,
            Top = 140,
            Width = 775,
            Height = 46,
            AutoEllipsis = true
        };
        group.Controls.Add(lblHint);
        var lblSyntax = new Label
        {
            Left = 12,
            Top = 187,
            Width = 1047,
            Height = 56,
            Text = "Можно менять все уравнения. Для задачи 2: v = y', введите правую часть y''; " +
                   "для задачи 3: x, y, z — неизвестные, t — время. " +
                   "Операции: + - * / ^, скобки, sin, cos, tan, exp, log, sqrt, abs, pow. " +
                   "Пример: -v/x-y или sin(t-1)*x-y+3*z."
        };
        group.Controls.Add(lblSyntax);
        Controls.Add(group);

        dgvSteps = new DataGridView
        {
            Left = 10,
            Top = 279,
            Width = 520,
            Height = 450,
            ReadOnly = true,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvSteps.Columns.Add("Step", "i");
        dgvSteps.Columns.Add("X", "x / t");
        dgvSteps.Columns.Add("Euler", "Эйлер: y (x,z)");
        dgvSteps.Columns.Add("RK", "РК4: y (x,z)");
        dgvSteps.Columns.Add("Difference", "|Δy|");
        Controls.Add(dgvSteps);

        dgvResults = new DataGridView
        {
            Left = 10,
            Top = 737,
            Width = 1075,
            Height = 120,
            ReadOnly = true,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        foreach (var col in new[] { "Метод", "Алгоритм", "n", "h", "Конечные значения",
                                    "Точное y(X)", "Ошибка", "Оценка Рунге", "p" })
            dgvResults.Columns.Add("C" + dgvResults.ColumnCount, col);
        Controls.Add(dgvResults);

        picPlot = new PictureBox
        {
            Left = 540,
            Top = 279,
            Width = 545,
            Height = 450,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(picPlot);
    }

    private void UpdateProblem()
    {
        int task = cmbProblem.SelectedIndex;
        bool first = task == 0, second = task == 1, system = task == 2;
        lblFunction.Visible = cmbFunction.Visible = first;
        lblSecond.Visible = txtSecond.Visible = second;
        lblDx.Visible = txtDx.Visible = system;
        lblDy.Visible = txtDy.Visible = system;
        lblDz.Visible = txtDz.Visible = system;
        lblV0.Visible = numV0.Visible = second || system;
        lblV0.Text = system ? "y₀ =" : "y'₀ =";
        lblZ0.Visible = numZ0.Visible = system;
        lblX0.Text = system ? "t₀ =" : "x₀ =";
        lblY0.Text = system ? "x₀ =" : "y₀ =";
        lblXEnd.Text = system ? "T =" : "X =";
        lblN.Text = "n =";
        if (first)
        {
            numX0.Value = 0; numY0.Value = 1; numXEnd.Value = 1; numN.Value = 10;
        }
        else if (second)
        {
            numX0.Value = 1; numY0.Value = 0.77m;
            numV0.Value = -0.44m; numXEnd.Value = 1.5m;
            numN.Value = 5; // h=0.1; для 10 частей выбрать n=10 (h=0.05)
        }
        else
        {
            numX0.Value = 0; numY0.Value = 2; numV0.Value = 1;
            numZ0.Value = 1; numXEnd.Value = 0.3m; numN.Value = 100; // h=0.003
        }
        UpdateFunction();
        UpdateAlgorithm();
    }

    private void UpdateAlgorithm()
    {
        bool adaptive = cmbAlgorithm.SelectedIndex == 1;
        numN.Enabled = !adaptive;
        numEps.Enabled = adaptive;
        int task = cmbProblem.SelectedIndex;
        string description = task == 0 ? "y' = f(x,y)"
            : task == 1 ? "y'' = " + txtSecond.Text + "; v=y'"
            : "x'=" + txtDx.Text + "; y'=" + txtDy.Text + "; z'=" + txtDz.Text;
        lblHint.Text = description + (adaptive
            ? ". n удваивается до выполнения оценки Рунге ≤ ε."
            : ". Сравниваются решения Эйлера и РК4.");
    }

    private void UpdateFunction()
    {
        if (cmbProblem.SelectedIndex != 0) return;
        string norm = cmbFunction.Text.Replace(" ", "").ToLowerInvariant();
        try
        {
            switch (norm)
            {
                case "y*(1-x)": currentFunc = (x, y) => y * (1 - x); break;
                case "x+y": currentFunc = (x, y) => x + y; break;
                case "y*sin(x)": currentFunc = (x, y) => y * Math.Sin(x); break;
                case "x*y": currentFunc = (x, y) => x * y; break;
                case "y-x": currentFunc = (x, y) => y - x; break;
                default: currentFunc = ExpressionParser.Compile2D(norm); break;
            }
        }
        catch { currentFunc = null; }
    }

    private void SetExact(double x0, double y0)
    {
        currentExact = null;
        if (cmbProblem.SelectedIndex != 0) return;
        string norm = cmbFunction.Text.Replace(" ", "").ToLowerInvariant();
        switch (norm)
        {
            case "y*(1-x)":
                currentExact = x => y0 * Math.Exp((x - x * x / 2) - (x0 - x0 * x0 / 2));
                break;
            case "x+y":
                currentExact = x => (y0 + x0 + 1) * Math.Exp(x - x0) - x - 1;
                break;
            case "y*sin(x)":
                currentExact = x => y0 * Math.Exp(Math.Cos(x0) - Math.Cos(x));
                break;
            case "x*y":
                currentExact = x => y0 * Math.Exp((x * x - x0 * x0) / 2);
                break;
            case "y-x":
                currentExact = x => x + 1 + (y0 - x0 - 1) * Math.Exp(x - x0);
                break;
        }
    }

    private double[] Derivative(int task, double t, double[] state)
    {
        if (task == 0) return new[] { currentFunc(t, state[0]) };
        if (task == 1) return new[] { state[1], customDerivatives[0](t, state) };
        return new[] {
            customDerivatives[0](t, state),
            customDerivatives[1](t, state),
            customDerivatives[2](t, state)
        };
    }

    private static double[] Add(double[] a, double[] b, double scale)
    {
        var r = new double[a.Length];
        for (int j = 0; j < a.Length; j++) r[j] = a[j] + scale * b[j];
        return r;
    }

    private double[] OneStep(int task, int method, double t, double[] state, double h)
    {
        double[] k1 = Derivative(task, t, state);
        if (method == 0) return Add(state, k1, h);
        double[] k2 = Derivative(task, t + h / 2, Add(state, k1, h / 2));
        double[] k3 = Derivative(task, t + h / 2, Add(state, k2, h / 2));
        double[] k4 = Derivative(task, t + h, Add(state, k3, h));
        var next = new double[state.Length];
        for (int j = 0; j < next.Length; j++)
            next[j] = state[j] + h * (k1[j] + 2 * k2[j] + 2 * k3[j] + k4[j]) / 6;
        return next;
    }

    private Solution Solve(int task, int method, double x0, double X,
                           double[] initial, int n)
    {
        var result = new Solution { N = n, H = (X - x0) / n };
        double[] state = (double[])initial.Clone();
        result.X.Add(x0);
        result.Y.Add((double[])state.Clone());
        for (int i = 1; i <= n; i++)
        {
            double t = x0 + (i - 1) * result.H;
            state = OneStep(task, method, t, state, result.H);
            for (int j = 0; j < state.Length; j++)
            {
                if (double.IsNaN(state[j]) || double.IsInfinity(state[j]))
                    throw new ArithmeticException("Численное решение расходится или вышло за пределы double.");
            }
            result.X.Add(x0 + i * result.H);
            result.Y.Add((double[])state.Clone());
        }
        return result;
    }

    private static double InfinityNormDiff(double[] a, double[] b)
    {
        double max = 0;
        for (int j = 0; j < a.Length; j++) max = Math.Max(max, Math.Abs(a[j] - b[j]));
        return max;
    }

    private static string FormatVector(double[] vector, int task)
    {
        if (task == 0) return vector[0].ToString("G10");
        if (task == 1) return "y=" + vector[0].ToString("G8") + "; v=" + vector[1].ToString("G8");
        return "x=" + vector[0].ToString("G7") + "; y=" + vector[1].ToString("G7")
            + "; z=" + vector[2].ToString("G7");
    }

    private void Calculate(object sender, EventArgs e)
    {
        try
        {
            dgvSteps.Rows.Clear();
            UpdateFunction();
            int task = cmbProblem.SelectedIndex;
            if (task == 0 && currentFunc == null)
                throw new ArgumentException("Не удалось разобрать f(x,y).");
            double x0 = (double)numX0.Value, X = (double)numXEnd.Value;
            if (X <= x0) throw new ArgumentException("Правая граница должна быть больше начальной.");
            // Компилируем написанные пользователем формулы перед расчётом.
            if (task == 1)
            {
                var f = Formula.Parse(txtSecond.Text, "x", "y", "v");
                customDerivatives = new Func<double, double[], double>[] {
                    (t, a) => f(new[] { t, a[0], a[1] })
                };
            }
            else if (task == 2)
            {
                var fx = Formula.Parse(txtDx.Text, "t", "x", "y", "z");
                var fy = Formula.Parse(txtDy.Text, "t", "x", "y", "z");
                var fz = Formula.Parse(txtDz.Text, "t", "x", "y", "z");
                customDerivatives = new Func<double, double[], double>[] {
                    (t, a) => fx(new[] { t, a[0], a[1], a[2] }),
                    (t, a) => fy(new[] { t, a[0], a[1], a[2] }),
                    (t, a) => fz(new[] { t, a[0], a[1], a[2] })
                };
            }
            double[] initial = task == 0 ? new[] { (double)numY0.Value }
                : task == 1 ? new[] { (double)numY0.Value, (double)numV0.Value }
                : new[] { (double)numY0.Value, (double)numV0.Value, (double)numZ0.Value };
            SetExact(x0, initial[0]);
            int method = cmbMethod.SelectedIndex;
            int n = (int)numN.Value;
            bool adaptive = cmbAlgorithm.SelectedIndex == 1;
            int p = method == 0 ? 1 : 4;
            double rungeEstimate = double.NaN;
            bool converged = true;
            if (adaptive)
            {
                n = 4;
                var previous = Solve(task, method, x0, X, initial, n);
                while (true)
                {
                    if (n > MaxN / 2) { converged = false; break; }
                    int newN = n * 2;
                    var finer = Solve(task, method, x0, X, initial, newN);
                    rungeEstimate = InfinityNormDiff(finer.Last, previous.Last)
                        / (Math.Pow(2, p) - 1);
                    n = newN;
                    previous = finer;
                    if (rungeEstimate <= (double)numEps.Value) break;
                }
            }
            var euler = Solve(task, 0, x0, X, initial, n);
            var rk4 = Solve(task, 1, x0, X, initial, n);
            var selected = method == 0 ? euler : rk4;
            int stride = Math.Max(1, (int)Math.Ceiling((double)n / MaxLogRows));
            for (int i = 0; i <= n; i += stride) AddStepRow(i, euler, rk4, task);
            if (n % stride != 0) AddStepRow(n, euler, rk4, task);
            double exact = currentExact == null ? double.NaN : currentExact(X);
            double error = double.IsNaN(exact) ? double.NaN : Math.Abs(selected.Last[0] - exact);
            dgvResults.Rows.Clear();
            dgvResults.Rows.Add(cmbMethod.Text,
                adaptive ? (converged ? "Рунге: достигнута ε" : "Предел n; ε не достигнута") : "Постоянный шаг",
                n, selected.H.ToString("G8"), FormatVector(selected.Last, task),
                double.IsNaN(exact) ? "—" : exact.ToString("G12"),
                double.IsNaN(error) ? "—" : error.ToString("G6"),
                double.IsNaN(rungeEstimate) ? "—" : rungeEstimate.ToString("G6"), p);
            var newImage = DrawPlot(task, euler, rk4);
            var previousImage = picPlot.Image;
            picPlot.Image = newImage;
            if (previousImage != null) previousImage.Dispose();
            lblHint.Text = "h=" + selected.H.ToString("G8") + "; n=" + n
                + "; |Эйлер − РК4| в конце="
                + InfinityNormDiff(euler.Last, rk4.Last).ToString("G6")
                + (adaptive && !converged ? "; точность ε не достигнута" : "");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка вычисления",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddStepRow(int i, Solution euler, Solution rk4, int task)
    {
        dgvSteps.Rows.Add(i, euler.X[i].ToString("G8"),
            FormatVector(euler.Y[i], task), FormatVector(rk4.Y[i], task),
            InfinityNormDiff(euler.Y[i], rk4.Y[i]).ToString("G6"));
    }

    private Bitmap DrawPlot(int task, Solution euler, Solution rk4)
    {
        int w = Math.Max(picPlot.Width, 400), h = Math.Max(picPlot.Height, 300);
        var bmp = new Bitmap(w, h);
        try
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                double xmin = euler.X[0], xmax = euler.X[euler.X.Count - 1];
                double ymin = double.PositiveInfinity, ymax = double.NegativeInfinity;
                int count = task == 2 ? 3 : 1; // Во втором примере строим интегральную кривую y(x)
                foreach (var s in new[] { euler, rk4 })
                    foreach (var state in s.Y)
                        for (int j = 0; j < count; j++)
                        {
                            ymin = Math.Min(ymin, state[j]);
                            ymax = Math.Max(ymax, state[j]);
                        }
                if (currentExact != null && task == 0)
                    for (int i = 0; i <= 200; i++)
                    {
                        double v = currentExact(xmin + (xmax - xmin) * i / 200);
                        if (!double.IsInfinity(v) && !double.IsNaN(v))
                        {
                            ymin = Math.Min(ymin, v); ymax = Math.Max(ymax, v);
                        }
                    }
                if (ymax - ymin < MinVisibleRange) { ymin -= 1; ymax += 1; }
                double pad = (ymax - ymin) * 0.12;
                ymin -= pad; ymax += pad;
                int left = 65, right = w - 15, top = 38, bottom = h - 38;
                Func<double, int> px = v => left + (int)((v - xmin) / (xmax - xmin) * (right - left));
                Func<double, int> py = v => bottom - (int)((v - ymin) / (ymax - ymin) * (bottom - top));
                using (var font = new Font("Arial", 8))
                using (var grid = new Pen(Color.LightGray))
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        double xv = xmin + (xmax - xmin) * i / 5;
                        int x = px(xv);
                        g.DrawLine(grid, x, top, x, bottom);
                        g.DrawString(xv.ToString("G4"), font, Brushes.Black, x - 14, bottom + 5);
                        double yv = ymin + (ymax - ymin) * i / 5;
                        int y = py(yv);
                        g.DrawLine(grid, left, y, right, y);
                        g.DrawString(yv.ToString("G4"), font, Brushes.Black, 2, y - 6);
                    }
                    g.DrawRectangle(Pens.Black, left, top, right - left, bottom - top);
                }
                Color[] eColors = { Color.RoyalBlue, Color.SeaGreen, Color.MediumVioletRed };
                Color[] rColors = { Color.Red, Color.DarkOrange, Color.DarkViolet };
                using (var font = new Font("Arial", 8))
                {
                    for (int j = 0; j < count; j++)
                    {
                        DrawSeries(g, euler, j, px, py, eColors[j], DashStyle.Dash);
                        DrawSeries(g, rk4, j, px, py, rColors[j], DashStyle.Solid);
                    }
                    g.DrawString(task == 2 ? "x,y,z: пунктир — Эйлер, сплошная — РК4"
                        : "Пунктир — Эйлер; сплошная — РК4", font, Brushes.Black, left, 4);
                    if (task == 2)
                        g.DrawString("x: синий/красный; y: зелёный/оранжевый; z: розовый/фиолетовый",
                            font, Brushes.Black, left, 19);
                }
                if (task == 0 && currentExact != null)
                {
                    var pts = new PointF[201];
                    bool allValid = true;
                    for (int i = 0; i <= 200; i++)
                    {
                        double x = xmin + (xmax - xmin) * i / 200;
                        double y = currentExact(x);
                        if (double.IsNaN(y) || double.IsInfinity(y)) { allValid = false; break; }
                        pts[i] = new PointF(px(x), py(y));
                    }
                    if (allValid)
                        using (var pen = new Pen(Color.Black, 1.5f) { DashStyle = DashStyle.Dot })
                            g.DrawLines(pen, pts);
                }
            }
            return bmp;
        }
        catch { bmp.Dispose(); throw; }
    }

    private static void DrawSeries(Graphics g, Solution s, int component,
        Func<double, int> px, Func<double, int> py, Color color, DashStyle style)
    {
        if (s.X.Count < 2) return;
        var points = new PointF[s.X.Count];
        for (int i = 0; i < points.Length; i++)
            points[i] = new PointF(px(s.X[i]), py(s.Y[i][component]));
        using (var pen = new Pen(color, 1.8f) { DashStyle = style })
            g.DrawLines(pen, points);
    }

    // Парсер математических формул без сторонних библиотек.
    // Позволяет вводить собственные функции для 2-го порядка и систем.
    private sealed class Formula
    {
        private readonly string source;
        private readonly string[] variables;
        private int position;

        private Formula(string source, string[] variables)
        {
            this.source = source ?? "";
            this.variables = variables;
        }

        public static Func<double[], double> Parse(string input, params string[] variables)
        {
            var parser = new Formula(input, variables);
            var result = parser.ParseExpression();
            parser.SkipSpaces();
            if (parser.position != parser.source.Length)
                throw new ArgumentException("Лишние символы в формуле: " + parser.source.Substring(parser.position));
            return result;
        }

        private void SkipSpaces()
        {
            while (position < source.Length && char.IsWhiteSpace(source[position])) position++;
        }

        private bool Eat(char ch)
        {
            SkipSpaces();
            if (position < source.Length && source[position] == ch) { position++; return true; }
            return false;
        }

        private Func<double[], double> ParseExpression()
        {
            var a = ParseTerm();
            while (true)
            {
                if (Eat('+')) { var left = a; var right = ParseTerm(); a = v => left(v) + right(v); }
                else if (Eat('-')) { var left = a; var right = ParseTerm(); a = v => left(v) - right(v); }
                else return a;
            }
        }

        private Func<double[], double> ParseTerm()
        {
            var a = ParseUnary();
            while (true)
            {
                if (Eat('*')) { var left = a; var right = ParseUnary(); a = v => left(v) * right(v); }
                else if (Eat('/')) { var left = a; var right = ParseUnary(); a = v => left(v) / right(v); }
                else return a;
            }
        }

        private Func<double[], double> ParseUnary()
        {
            if (Eat('+')) return ParseUnary();
            if (Eat('-')) { var operand = ParseUnary(); return v => -operand(v); }
            return ParsePower();
        }

        private Func<double[], double> ParsePower()
        {
            var a = ParseAtom();
            if (Eat('^')) { var exponent = ParseUnary(); return v => Math.Pow(a(v), exponent(v)); }
            return a;
        }

        private Func<double[], double> ParseAtom()
        {
            if (Eat('('))
            {
                var expr = ParseExpression();
                if (!Eat(')')) throw new ArgumentException("Не закрыта скобка в формуле.");
                return expr;
            }
            SkipSpaces();
            int start = position;
            if (position < source.Length && (char.IsDigit(source[position]) || source[position] == '.'))
            {
                while (position < source.Length && (char.IsDigit(source[position]) || source[position] == '.')) position++;
                double value;
                if (!double.TryParse(source.Substring(start, position - start),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out value))
                    throw new ArgumentException("Неверное число в формуле.");
                return v => value;
            }
            while (position < source.Length && (char.IsLetter(source[position]) || char.IsDigit(source[position]) || source[position] == '_')) position++;
            if (position == start) throw new ArgumentException("Неожиданный символ в формуле возле позиции " + (position + 1));
            string name = source.Substring(start, position - start).ToLowerInvariant();
            for (int i = 0; i < variables.Length; i++)
                if (name == variables[i]) { int index = i; return v => v[index]; }
            if (name == "pi") return v => Math.PI;
            if (name == "e") return v => Math.E;
            if (!Eat('(')) throw new ArgumentException("Неизвестная переменная или функция: " + name);
            var first = ParseExpression();
            Func<double[], double> second = null;
            if (Eat(',')) second = ParseExpression();
            if (!Eat(')')) throw new ArgumentException("Не закрыта функция " + name);
            if (second != null)
            {
                if (name == "pow") return v => Math.Pow(first(v), second(v));
                if (name == "min") return v => Math.Min(first(v), second(v));
                if (name == "max") return v => Math.Max(first(v), second(v));
                throw new ArgumentException("Неизвестная функция с двумя аргументами: " + name);
            }
            switch (name)
            {
                case "sin": return v => Math.Sin(first(v));
                case "cos": return v => Math.Cos(first(v));
                case "tan": return v => Math.Tan(first(v));
                case "exp": return v => Math.Exp(first(v));
                case "log": case "ln": return v => Math.Log(first(v));
                case "sqrt": return v => Math.Sqrt(first(v));
                case "abs": return v => Math.Abs(first(v));
                default: throw new ArgumentException("Неизвестная функция: " + name);
            }
        }
    }

}
