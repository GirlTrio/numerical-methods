using System;
using System.Drawing;
using System.Windows.Forms;

namespace LabIntegral
{
    public class MainForm : Form
    {
        private TabControl tabs;

        private IntegralPanel integralPanel;
        private DerivativePanel derivativePanel;

        public MainForm()
        {
            Text = "Лабораторная работа — Численные методы";
            Width = 1100;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            BuildTabs();
        }

        private void BuildTabs()
        {
            tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 6)
            };

            integralPanel = new IntegralPanel { Dock = DockStyle.Fill };
            derivativePanel = new DerivativePanel { Dock = DockStyle.Fill };

            var pageIntegral = new TabPage("Интегрирование");
            pageIntegral.Controls.Add(integralPanel);

            var pageDerivative = new TabPage("Дифференцирование");
            pageDerivative.Controls.Add(derivativePanel);

            tabs.TabPages.Add(pageIntegral);
            tabs.TabPages.Add(pageDerivative);

            Controls.Add(tabs);
        }
    }

    // =========================================================
    //                  ИНТЕГРИРОВАНИЕ
    // =========================================================

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
               
                "По остаточному члену",
                "Двойной пересчёт, h / 2)",
                "Переменный шаг)"
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
                    // ===== Алгоритм 2 из PDF (обобщённый) =====
                    // Переменный шаг: h_v — шаг вычисления, h_s — шаг смещения,
                    // h_d — шаг движения. Переиспользуем ранее вычисленные точки.
                    //
                    // Работает для прямоугольников и трапеций.
                    // Для Симпсона веса чередуются (2/4), поэтому переиспользование
                    // сумм невозможно — откатываемся к обычному двойному пересчёту.
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

        /// <summary>
        /// Вычисляет сумму ординат для алгоритма переменного шага.
        /// 
        /// Для левых прямоугольников: S = Σ f(x_i), x_i = a + hs + i*hd (новые точки).
        /// Для правых прямоугольников: аналогично, но с учётом сдвига.
        /// Для трапеций: S = 0.5*(f(a) + f(b)) + Σ f(x_i), где x_i — внутренние точки.
        /// 
        /// Параметр previousSum используется для трапеций, чтобы не добавлять
        /// концевые точки a и b повторно.
        /// </summary>
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

    // =========================================================
    //         ПАРСЕР ФУНКЦИИ ДЛЯ ИНТЕГРИРОВАНИЯ
    // =========================================================

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

    // =========================================================
    //                ДИФФЕРЕНЦИРОВАНИЕ
    // =========================================================

    public class DerivativePanel : UserControl
    {
        private GroupBox grpParams;
        private ComboBox cmbFunction, cmbScheme;
        private NumericUpDown numX, numH;
        private Label lblFunction, lblScheme, lblX, lblH;
        private Button btnCalculate, btnClear;

        private DataGridView dgvResults;
        private RichTextBox rtbAnalysis;

        public DerivativePanel()
        {
            BuildParams();
            BuildResults();
            btnClear.Click += (sender, e) => { dgvResults.Rows.Clear(); rtbAnalysis.Clear(); };
            btnCalculate.Click += (sender, e) => CalculateDerivative();
        }

        private void CalculateDerivative()
        {
            try
            {
                double x = (double)numX.Value, h = (double)numH.Value;
                Func<double, double> f, df;
                switch (cmbFunction.SelectedIndex)
                {
                    case 0: f = Math.Sin; df = Math.Cos; break;
                    case 1: f = t => t * t; df = t => 2 * t; break;
                    case 2: f = Math.Exp; df = Math.Exp; break;
                    default: f = Math.Log; df = t => 1 / t; break;
                }
                int scheme = cmbScheme.SelectedIndex;
                if (cmbFunction.SelectedIndex == 3 &&
                    (x <= 0 || (scheme == 1 || scheme == 2) && x - h <= 0 || scheme == 3 && x - 2 * h <= 0))
                    throw new ArgumentException("Для ln(x) все точки схемы должны быть положительными.");
                double value;
                switch (scheme)
                {
                    case 0: value = (f(x + h) - f(x)) / h; break;
                    case 1: value = (f(x) - f(x - h)) / h; break;
                    case 2: value = (f(x + h) - f(x - h)) / (2 * h); break;
                    default: value = (f(x - 2 * h) - 8 * f(x - h) + 8 * f(x + h) - f(x + 2 * h)) / (12 * h); break;
                }
                double exact = df(x);
                if (double.IsNaN(value) || double.IsInfinity(value) || double.IsNaN(exact) || double.IsInfinity(exact))
                    throw new ArithmeticException("Значение функции или производной не конечно.");
                dgvResults.Rows.Add(cmbScheme.Text, x, h, value.ToString("G12"), Math.Abs(value - exact).ToString("G8"));
                rtbAnalysis.Text = "Точная производная: " + exact.ToString("G12") +
                    "\nАбсолютная погрешность: " + Math.Abs(value - exact).ToString("G8");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка вычисления", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void BuildParams()
        {
            grpParams = new GroupBox
            {
                Text = "Параметры вычисления производной",
                Left = 10,
                Top = 10,
                Width = 1060,
                Height = 130,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            int y1 = 25, y2 = 60, y3 = 95;
            int x = 15;

            lblFunction = new Label { Text = "Функция:", Left = x, Top = y1 + 3, Width = 65 };
            cmbFunction = new ComboBox { Left = x + 70, Top = y1, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFunction.Items.AddRange(new object[] {
                "f(x) = sin(x)",
                "f(x) = x^2",
                "f(x) = exp(x)",
                "f(x) = ln(x)"
            });
            cmbFunction.SelectedIndex = 0;

            lblScheme = new Label { Text = "Схема:", Left = x + 350, Top = y1 + 3, Width = 55 };
            cmbScheme = new ComboBox { Left = x + 410, Top = y1, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbScheme.Items.AddRange(new object[] {
                "Правая разность",
                "Левая разность",
                "Центральная разность",
                "Пятиточечная формула"
            });
            cmbScheme.SelectedIndex = 0;

            lblX = new Label { Text = "x₀ =", Left = x, Top = y2 + 3, Width = 35 };
            numX = new NumericUpDown { Left = x + 40, Top = y2, Width = 120, DecimalPlaces = 6, Minimum = -1000, Maximum = 1000, Increment = 0.1m, Value = 1m };

            lblH = new Label { Text = "h =", Left = x + 200, Top = y2 + 3, Width = 30 };
            numH = new NumericUpDown { Left = x + 235, Top = y2, Width = 120, DecimalPlaces = 8, Minimum = 0.00000001m, Maximum = 1m, Increment = 0.001m, Value = 0.001m };

            btnCalculate = new Button { Text = "Вычислить производную", Left = x, Top = y3, Width = 180, Height = 30 };
            btnClear = new Button { Text = "Очистить", Left = x + 190, Top = y3, Width = 110, Height = 30 };

            grpParams.Controls.AddRange(new Control[] {
                lblFunction, cmbFunction,
                lblScheme, cmbScheme,
                lblX, numX, lblH, numH,
                btnCalculate, btnClear
            });

            Controls.Add(grpParams);
        }

        private void BuildResults()
        {
            dgvResults = new DataGridView
            {
                Left = 10,
                Top = 150,
                Width = 700,
                Height = 300,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvResults.Columns.Add("Scheme", "Схема");
            dgvResults.Columns.Add("X", "x₀");
            dgvResults.Columns.Add("H", "h");
            dgvResults.Columns.Add("Result", "Значение");
            dgvResults.Columns.Add("Error", "Погрешность");

            rtbAnalysis = new RichTextBox
            {
                Left = 10,
                Top = 460,
                Width = 1060,
                Height = 220,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Consolas", 10F),
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            Controls.Add(dgvResults);
            Controls.Add(rtbAnalysis);
        }
    }
}