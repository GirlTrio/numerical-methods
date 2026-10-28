using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class DifferentialPanel : UserControl
{
    // ===== Параметры =====
    private GroupBox grpParams;
    private ComboBox cmbMethod, cmbFunction, cmbAlgorithm;
    private NumericUpDown numX0, numY0, numXEnd, numN, numEps;
    private Label lblMethod, lblFunction, lblAlgorithm, lblX0, lblY0, lblXEnd, lblN, lblEps, lblHint;
    private Button btnCalculate, btnClear;
    private DataGridView dgvSteps, dgvResults;
    private PictureBox picPlot;

    private Func<double, double, double> currentFunc;
    private Func<double, double> currentExact;   // точное решение, если известно

    public DifferentialPanel()
    {
        BuildParams();
        BuildResults();
        UpdateFunction();
        UpdateAlgorithm();
    }

    // -----------------------------------------------------------------
    // 1. Параметры
    // -----------------------------------------------------------------
    private void BuildParams()
    {
        grpParams = new GroupBox
        {
            Text = "Параметры задачи Коши",
            Left = 10,
            Top = 10,
            Width = 1060,
            Height = 200,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        int x = 15, y0 = 22, y1 = 55, y2 = 88, y3 = 121;

        // ---- Метод ----
        lblMethod = new Label { Text = "Метод:", Left = x, Top = y0 + 3, Width = 55 };
        cmbMethod = new ComboBox
        {
            Left = x + 60,
            Top = y0,
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbMethod.Items.AddRange(new object[] {
            "Эйлера",
            "Рунге–Кутта (4-й порядок)"
        });
        cmbMethod.SelectedIndex = 0;

        // ---- Функция ----
        lblFunction = new Label { Text = "f(x,y):", Left = x + 340, Top = y0 + 3, Width = 60 };
        cmbFunction = new ComboBox
        {
            Left = x + 405,
            Top = y0,
            Width = 300,
            DropDownStyle = ComboBoxStyle.DropDown
        };
        cmbFunction.Items.AddRange(new object[] {
            "y * (1 - x)", "x + y", "y * sin(x)", "x * y", "y - x"
        });
        cmbFunction.Text = "y * (1 - x)";
        cmbFunction.SelectedIndex = 0;
        cmbFunction.TextChanged += (s, e) => UpdateFunction();
        cmbFunction.SelectedIndexChanged += (s, e) => UpdateFunction();

        // ---- Алгоритм ----
        lblAlgorithm = new Label { Text = "Алгоритм:", Left = x + 730, Top = y0 + 3, Width = 65 };
        cmbAlgorithm = new ComboBox
        {
            Left = x + 800,
            Top = y0,
            Width = 245,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbAlgorithm.Items.AddRange(new object[] {
            "Постоянный шаг",                          // 0
            "Двойной пересчёт (правило Рунге)"         // 1
        });
        cmbAlgorithm.SelectedIndex = 0;
        cmbAlgorithm.SelectedIndexChanged += (s, e) => UpdateAlgorithm();

        // ---- x0, y0, X, n, eps ----
        lblX0 = new Label { Text = "x₀ =", Left = x, Top = y1 + 3, Width = 35 };
        numX0 = new NumericUpDown
        {
            Left = x + 40,
            Top = y1,
            Width = 90,
            DecimalPlaces = 6,
            Minimum = -1000,
            Maximum = 1000,
            Increment = 0.1m,
            Value = 0m
        };

        lblY0 = new Label { Text = "y₀ =", Left = x + 145, Top = y1 + 3, Width = 35 };
        numY0 = new NumericUpDown
        {
            Left = x + 185,
            Top = y1,
            Width = 90,
            DecimalPlaces = 6,
            Minimum = -1000,
            Maximum = 1000,
            Increment = 0.1m,
            Value = 1m
        };

        lblXEnd = new Label { Text = "X =", Left = x + 290, Top = y1 + 3, Width = 30 };
        numXEnd = new NumericUpDown
        {
            Left = x + 325,
            Top = y1,
            Width = 90,
            DecimalPlaces = 6,
            Minimum = -1000,
            Maximum = 1000,
            Increment = 0.1m,
            Value = 1m
        };

        lblN = new Label { Text = "n =", Left = x + 430, Top = y1 + 3, Width = 30 };
        numN = new NumericUpDown
        {
            Left = x + 465,
            Top = y1,
            Width = 90,
            Minimum = 1,
            Maximum = 100000,
            Value = 10
        };

        lblEps = new Label { Text = "ε =", Left = x + 570, Top = y1 + 3, Width = 30 };
        numEps = new NumericUpDown
        {
            Left = x + 605,
            Top = y1,
            Width = 120,
            DecimalPlaces = 8,
            Minimum = 0.00000001m,
            Maximum = 1m,
            Increment = 0.0001m,
            Value = 0.0001m,
            Enabled = false
        };

        // ---- Кнопки ----
        btnCalculate = new Button { Text = "Вычислить", Left = x, Top = y2, Width = 130, Height = 28 };
        btnClear = new Button { Text = "Очистить", Left = x + 140, Top = y2, Width = 110, Height = 28 };
        btnCalculate.Click += BtnCalculate_Click;
        btnClear.Click += BtnClear_Click;

        lblHint = new Label
        {
            Left = x,
            Top = y3,
            Width = 1030,
            Height = 40,
            Text = "Метод Эйлера: y_{i+1} = y_i + h·f(x_i, y_i). " +
                   "РК4: y_{i+1} = y_i + h/6·(k1+2k2+2k3+k4)."
        };

        grpParams.Controls.AddRange(new Control[] {
            lblMethod, cmbMethod, lblFunction, cmbFunction,
            lblAlgorithm, cmbAlgorithm,
            lblX0, numX0, lblY0, numY0, lblXEnd, numXEnd, lblN, numN, lblEps, numEps,
            btnCalculate, btnClear, lblHint
        });
        Controls.Add(grpParams);
    }

    private void UpdateAlgorithm()
    {
        bool rule = cmbAlgorithm.SelectedIndex == 1;
        numEps.Enabled = rule;
        numN.Enabled = !rule;
        lblHint.Text = rule
            ? "Двойной пересчёт: n удваивается, пока |y(2n) − y(n)| > ε. " +
              "Журнал показывает каждый пересчёт."
            : "Постоянный шаг: фиксированное n, все шаги пишутся в журнал.";
    }

    // -----------------------------------------------------------------
    // 2. Таблицы: журнал шагов, результаты, график
    // -----------------------------------------------------------------
    private void BuildResults()
    {
        // ---- Журнал шагов ----
        dgvSteps = new DataGridView
        {
            Left = 10,
            Top = 220,
            Width = 500,
            Height = 200,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvSteps.Columns.Add("Step", "Шаг");
        dgvSteps.Columns.Add("x", "x");
        dgvSteps.Columns.Add("y", "y");
        dgvSteps.Columns.Add("Action", "Действие");
        dgvSteps.Columns.Add("Note", "Примечание");
        Controls.Add(dgvSteps);

        // ---- Результаты ----
        dgvResults = new DataGridView
        {
            Left = 10,
            Top = 430,
            Width = 500,
            Height = 200,
            Anchor = AnchorStyles.Top | AnchorStyles.Left ,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvResults.Columns.Add("Method", "Метод");
        dgvResults.Columns.Add("Alg", "Алгоритм");
        dgvResults.Columns.Add("N", "n");
        dgvResults.Columns.Add("H", "h");
        dgvResults.Columns.Add("Y", "y(X)");
        dgvResults.Columns.Add("Exact", "Точное");
        dgvResults.Columns.Add("Err", "Погрешность");
        dgvResults.Columns.Add("R", "R / статус");
        dgvResults.Columns.Add("Order", "p");
        dgvResults.Columns.Add("Time", "мс");
        Controls.Add(dgvResults);

        // ---- График ----
        picPlot = new PictureBox
        {
            Left = 520,
            Top = 220,
            Width = 500,
            Height = 500,
            Anchor = AnchorStyles.Top | AnchorStyles.Left ,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        Controls.Add(picPlot);
    }

    // -----------------------------------------------------------------
    // 3. Функция и точное решение
    // -----------------------------------------------------------------
    private void UpdateFunction()
    {
        string norm = cmbFunction.Text.Replace(" ", "").ToLowerInvariant();
        try
        {
            switch (norm)
            {
                case "y*(1-x)":
                    currentFunc = (x, y) => y * (1 - x);
                    currentExact = x => Math.Exp(x - x * x / 2);
                    break;
                case "x+y":
                    currentFunc = (x, y) => x + y;
                    currentExact = x => -x - 1 + 2 * Math.Exp(x);   // y0=1 при x0=0
                    break;
                case "y*sin(x)":
                    currentFunc = (x, y) => y * Math.Sin(x);
                    currentExact = x => Math.Exp(1 - Math.Cos(x));
                    break;
                case "x*y":
                    currentFunc = (x, y) => x * y;
                    currentExact = x => Math.Exp(x * x / 2);
                    break;
                case "y-x":
                    currentFunc = (x, y) => y - x;
                    currentExact = x => x + 1;
                    break;
                default:
                    currentFunc = ExpressionParser.Compile2D(norm);
                    currentExact = null;
                    break;
            }
        }
        catch { currentFunc = null; currentExact = null; }
    }

    // -----------------------------------------------------------------
    // 4. Шаги методов (чистые функции)
    // -----------------------------------------------------------------
    private double EulerStep(double xi, double yi, double h)
        => yi + h * currentFunc(xi, yi);

    private double RungeKuttaStep(double xi, double yi, double h)
    {
        double k1 = currentFunc(xi, yi);
        double k2 = currentFunc(xi + h / 2, yi + h * k1 / 2);
        double k3 = currentFunc(xi + h / 2, yi + h * k2 / 2);
        double k4 = currentFunc(xi + h, yi + h * k3);
        return yi + h * (k1 + 2 * k2 + 2 * k3 + k4) / 6.0;
    }

    // -----------------------------------------------------------------
    // 5. Расчёт с журналом
    // -----------------------------------------------------------------
    private List<PointF> SolveWithLog(int method, double x0, double y0, double X,
                                      int n, string logPrefix, bool logDetail)
    {
        var pts = new List<PointF>(n + 1);
        double h = (X - x0) / n;
        double x = x0, y = y0;
        pts.Add(new PointF((float)x, (float)y));

        if (logDetail)
            dgvSteps.Rows.Add(logPrefix, x.ToString("F4"), y.ToString("G10"),
                              "Старт", $"h = {h:G6}, n = {n}");

        for (int i = 1; i <= n; i++)
        {
            double yPrev = y;
            y = (method == 0) ? EulerStep(x, y, h) : RungeKuttaStep(x, y, h);
            x = x0 + i * h;
            pts.Add(new PointF((float)x, (float)y));

            if (logDetail)
            {
                string action = method == 0
                    ? "y += h·f(x,y)"
                    : "y += h/6·(k1+2k2+2k3+k4)";
                dgvSteps.Rows.Add(logPrefix, x.ToString("F4"), y.ToString("G10"),
                                  action, $"Δy = {y - yPrev:G6}");
            }
        }
        return pts;
    }

    // -----------------------------------------------------------------
    // 6. Кнопка «Вычислить»
    // -----------------------------------------------------------------
    private void BtnCalculate_Click(object sender, EventArgs e)
    {
        dgvSteps.Rows.Clear();
        UpdateFunction();
        if (currentFunc == null)
        {
            MessageBox.Show("Не удалось разобрать f(x,y).", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            double x0 = (double)numX0.Value, y0 = (double)numY0.Value, X = (double)numXEnd.Value;
            if (x0 >= X) throw new ArgumentException("Требуется x0 < X.");

            int method = cmbMethod.SelectedIndex;
            int algorithm = cmbAlgorithm.SelectedIndex;

            // Для сравнения всегда считаем оба метода при текущем n —
            // это даёт строку «предыдущих действий» и рисует обе кривые.
            int n = (int)numN.Value;
            if (algorithm == 1) n = 4;   // стартовое n при двойном пересчёте
            double eps = (double)numEps.Value;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            List<PointF> ptsEuler = null, ptsRK = null;
            double yEuler = double.NaN, yRK = double.NaN;
            int finalN = n;
            double r = double.NaN;
            bool converged = true;

            if (algorithm == 0)
            {
                // ---- Постоянный шаг: журнал по выбранному методу + кривые обоих ----
                ptsEuler = SolveWithLog(0, x0, y0, X, n, $"n={n}", true);
                ptsRK = SolveWithLog(1, x0, y0, X, n, $"n={n}", true);
                yEuler = ptsEuler[ptsEuler.Count - 1].Y;
                yRK = ptsRK[ptsRK.Count - 1].Y;
            }
            else
            {
                // ---- Двойной пересчёт: журнал пересчётов выбранного метода ----
                double yPrev = double.NaN;
                while (true)
                {
                    var pts = SolveWithLog(method, x0, y0, X, n, $"n={n}", true);
                    double yCur = pts[pts.Count - 1].Y;

                    if (!double.IsNaN(yPrev))
                    {
                        r = Math.Abs(yCur - yPrev);
                        dgvSteps.Rows.Add($"n={n}", X.ToString("F4"), yCur.ToString("G10"),
                                          "R = |I(2n) − I(n)|", r.ToString("G6"));
                        if (r <= eps) { finalN = n; yRK = yCur; break; }
                    }
                    yPrev = yCur;
                    if (n > 100000 / 2) { converged = false; yRK = yCur; break; }
                    n *= 2;
                }
                // кривые для графика — при финальном n
                ptsEuler = SolveWithLog(0, x0, y0, X, finalN, $"n={finalN}", false);
                ptsRK = SolveWithLog(1, x0, y0, X, finalN, $"n={finalN}", false);
                yEuler = ptsEuler[ptsEuler.Count - 1].Y;
                yRK = ptsRK[ptsRK.Count - 1].Y;
                finalN = n;
            }

            sw.Stop();

            // ---- Погрешность и порядок ----
            double exact = currentExact != null ? currentExact(X) : double.NaN;
            double errE = double.IsNaN(exact) ? double.NaN : Math.Abs(yEuler - exact);
            double errR = double.IsNaN(exact) ? double.NaN : Math.Abs(yRK - exact);

            string orderText = "—";
            if (!double.IsNaN(exact) && algorithm == 1 && finalN >= 2)
            {
                // приблизительная оценка по последнему удвоению (p = log2(e_h/e_h/2))
                // здесь просто выводим теоретический порядок метода
                orderText = method == 0 ? "1" : "4";
            }
            else if (!double.IsNaN(exact) && algorithm == 0)
                orderText = method == 0 ? "~1" : "~4";

            dgvResults.Rows.Add(
                cmbMethod.Text, cmbAlgorithm.Text, finalN,
                ((X - x0) / finalN).ToString("G6"),
                method == 0 ? yEuler.ToString("G12") : yRK.ToString("G12"),
                double.IsNaN(exact) ? "—" : exact.ToString("G12"),
                method == 0
                    ? (double.IsNaN(errE) ? "—" : errE.ToString("G6"))
                    : (double.IsNaN(errR) ? "—" : errR.ToString("G6")),
                double.IsNaN(r) ? (converged ? "Готово" : "Предел n") : r.ToString("G6"),
                orderText,
                sw.Elapsed.TotalMilliseconds.ToString("F2"));

            // ---- График ----
            picPlot.Image = DrawPlot(ptsEuler, ptsRK, x0, X,
                                     currentExact != null ? currentExact : null);

            lblHint.Text = $"h = {(X - x0) / finalN:G6}, n = {finalN}. ";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка вычисления",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnClear_Click(object sender, EventArgs e)
    {
        dgvSteps.Rows.Clear();
        dgvResults.Rows.Clear();
        picPlot.Image = null;
    }

    // -----------------------------------------------------------------
    // 7. Отрисовка графика
    // -----------------------------------------------------------------
    private Bitmap DrawPlot(List<PointF> euler, List<PointF> rk,
                            double x0, double X, Func<double, double> exact)
    {
        int W = Math.Max(picPlot.Width, 400), H = Math.Max(picPlot.Height, 200);
        var bmp = new Bitmap(W, H);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            double xmin = x0, xmax = X;
            double ymin = double.MaxValue, ymax = double.MinValue;
            foreach (var p in euler) { ymin = Math.Min(ymin, p.Y); ymax = Math.Max(ymax, p.Y); }
            foreach (var p in rk) { ymin = Math.Min(ymin, p.Y); ymax = Math.Max(ymax, p.Y); }
            if (exact != null)
            {
                for (int i = 0; i <= 200; i++)
                {
                    double xv = xmin + (xmax - xmin) * i / 200.0;
                    ymin = Math.Min(ymin, exact(xv)); ymax = Math.Max(ymax, exact(xv));
                }
            }
            if (Math.Abs(ymax - ymin) < 1e-9) { ymin -= 1; ymax += 1; }
            double padY = (ymax - ymin) * 0.1;
            ymin -= padY; ymax += padY;

            int L = 55, R = W - 20, T = 20, B = H - 30;

            Func<double, int> px = xv => L + (int)((xv - xmin) / (xmax - xmin) * (R - L));
            Func<double, int> py = yv => B - (int)((yv - ymin) / (ymax - ymin) * (B - T));

            // сетка
            using (var penGrid = new Pen(Color.LightGray))
            using (var penAxis = new Pen(Color.Black))
            using (var fSmall = new Font("Arial", 7))
            {
                for (int i = 0; i <= 10; i++)
                {
                    double xv = xmin + (xmax - xmin) * i / 10.0;
                    int xp = px(xv);
                    g.DrawLine(penGrid, xp, T, xp, B);
                    g.DrawString(xv.ToString("F2"), fSmall, Brushes.Black, xp - 12, B + 2);
                }
                for (int i = 0; i <= 5; i++)
                {
                    double yv = ymin + (ymax - ymin) * i / 5.0;
                    int yp = py(yv);
                    g.DrawLine(penGrid, L, yp, R, yp);
                    g.DrawString(yv.ToString("F2"), fSmall, Brushes.Black, 2, yp - 6);
                }
                g.DrawRectangle(penAxis, L, T, R - L, B - T);
            }

            // кривые
            using (var penE = new Pen(Color.Blue, 2))
            using (var penR = new Pen(Color.Red, 2))
            using (var penEx = new Pen(Color.Green, 1) { DashStyle = DashStyle.Dash })
            {
                if (euler.Count > 1)
                {
                    var pts = new PointF[euler.Count];
                    for (int i = 0; i < euler.Count; i++)
                        pts[i] = new PointF(px(euler[i].X), py(euler[i].Y));
                    g.DrawLines(penE, pts);
                }
                if (rk.Count > 1)
                {
                    var pts = new PointF[rk.Count];
                    for (int i = 0; i < rk.Count; i++)
                        pts[i] = new PointF(px(rk[i].X), py(rk[i].Y));
                    g.DrawLines(penR, pts);
                }
                if (exact != null)
                {
                    var pts = new List<PointF>();
                    for (int i = 0; i <= 200; i++)
                    {
                        double xv = xmin + (xmax - xmin) * i / 200.0;
                        pts.Add(new PointF(px(xv), py(exact(xv))));
                    }
                    g.DrawLines(penEx, pts.ToArray());
                }
            }

            // легенда
            using (var fLegend = new Font("Arial", 8, FontStyle.Bold))
            {
                int lx = R - 150, ly = T + 6;
                g.DrawString("— Эйлер", fLegend, Brushes.Blue, lx, ly);
                g.DrawString("— Рунге–Кутта", fLegend, Brushes.Red, lx, ly + 14);
                if (exact != null)
                    g.DrawString("--- Точное", fLegend, Brushes.Green, lx, ly + 28);
            }
        }
        return bmp;
    }
}