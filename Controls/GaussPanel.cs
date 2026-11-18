using System;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

public class GaussPanel : UserControl
{
    // ===== Параметры =====
    private GroupBox grpParams;
    private ComboBox cmbMethod, cmbAlgorithm;
    private NumericUpDown numN;
    private Label lblMethod, lblAlgorithm, lblN, lblHint;
    private Button btnCalculate, btnClear, btnFillExample;
    private DataGridView dgvInput, dgvLog, dgvResults;

    public GaussPanel()
    {
        BuildParams();
        BuildTables();
        ResizeInput();
        FillControlExample();
    }

    // -----------------------------------------------------------------
    // 1. Параметры
    // -----------------------------------------------------------------
    private void BuildParams()
    {
        grpParams = new GroupBox
        {
            Text = "Параметры СЛУ (A·x = B)",
            Left = 10,
            Top = 10,
            Width = 1060,
            Height = 150,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        int x = 15, y0 = 22, y1 = 55, y2 = 88;

        // ---- Метод ----
        lblMethod = new Label { Text = "Метод:", Left = x, Top = y0 + 3, Width = 55 };
        cmbMethod = new ComboBox
        {
            Left = x + 60,
            Top = y0,
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbMethod.Items.AddRange(new object[] {
            "Гаусс (исключение по столбцам)",
            "Гаусс (оптимальное исключение)",
            "Гаусс–Жордан"
        });
        cmbMethod.SelectedIndex = 0;

        // ---- Режим ----
        lblAlgorithm = new Label { Text = "Режим:", Left = x + 400, Top = y0 + 3, Width = 55 };
        cmbAlgorithm = new ComboBox
        {
            Left = x + 460,
            Top = y0,
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbAlgorithm.Items.AddRange(new object[] {
            "Только исходная система",
            "Сравнение с B = 0.1; 0.001; 0.01"
        });
        cmbAlgorithm.SelectedIndex = 0;
        cmbAlgorithm.SelectedIndexChanged += (s, e) => UpdateAlgorithm();

        // ---- n ----
        lblN = new Label { Text = "n =", Left = x + 800, Top = y0 + 3, Width = 30 };
        numN = new NumericUpDown
        {
            Left = x + 835,
            Top = y0,
            Width = 70,
            Minimum = 2,
            Maximum = 20,
            Value = 4
        };
        numN.ValueChanged += (s, e) => ResizeInput();

        // ---- Кнопки ----
        btnFillExample = new Button { Text = "Контрольный пример", Left = x, Top = y1, Width = 180, Height = 28 };
        btnCalculate = new Button { Text = "Вычислить", Left = x + 190, Top = y1, Width = 130, Height = 28 };
        btnClear = new Button { Text = "Очистить", Left = x + 330, Top = y1, Width = 110, Height = 28 };

        btnFillExample.Click += BtnFillExample_Click;
        btnCalculate.Click += BtnCalculate_Click;
        btnClear.Click += BtnClear_Click;

        lblHint = new Label
        {
            Left = x,
            Top = y2,
            Width = 1030,
            Height = 40,
            Text = "Прямой ход: исключение неизвестных по столбцам. " +
                   "Обратный ход: нахождение xₙ, xₙ₋₁, …, x₁."
        };

        grpParams.Controls.AddRange(new Control[] {
            lblMethod, cmbMethod, lblAlgorithm, cmbAlgorithm, lblN, numN,
            btnFillExample, btnCalculate, btnClear, lblHint
        });
        Controls.Add(grpParams);
    }

    private void UpdateAlgorithm()
    {
        bool compare = cmbAlgorithm.SelectedIndex == 1;
        lblHint.Text = compare
            ? "Сравнительный режим: исходная A, но B заменяется на три вектора " +
              "(все компоненты = 0.1, затем = 0.001, затем = 0.01)."
            : "Прямой ход: исключение неизвестных по столбцам; " +
              "обратный ход: нахождение xₙ…x₁.";
    }

    // -----------------------------------------------------------------
    // 2. Таблицы: ввод, журнал, результаты
    // -----------------------------------------------------------------
    private void BuildTables()
    {
        // ---- Ввод матрицы ----
        dgvInput = new DataGridView
        {
            Left = 10,
            Top = 170,
            Width = 520,
            Height = 260,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter,
            SelectionMode = DataGridViewSelectionMode.CellSelect
        };
        Controls.Add(dgvInput);

        // ---- Журнал шагов ----
        dgvLog = new DataGridView
        {
            Left = 540,
            Top = 170,
            Width = 500,
            Height = 560,
            Anchor = AnchorStyles.Top | AnchorStyles.Left ,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True },
            Font = new Font("Consolas", 9F), // Моноширинный шрифт для ровных столбцов
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvLog.Columns.Add("Phase", "Этап");
        dgvLog.Columns.Add("Step", "Шаг");
        dgvLog.Columns.Add("Detail", "Действие");
        dgvLog.Columns.Add("Value", "Значение");
        Controls.Add(dgvLog);

        // ---- Результаты ----
        dgvResults = new DataGridView
        {
            Left = 10,
            Top = 440,
            Width = 520,
            Height = 290,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgvResults.Columns.Add("Case", "Случай");
        dgvResults.Columns.Add("Key", "Величина");
        dgvResults.Columns.Add("Val", "Значение");
        Controls.Add(dgvResults);
    }

    // -----------------------------------------------------------------
    // 3. Управление сеткой ввода
    // -----------------------------------------------------------------
    private void ResizeInput()
    {
        int n = (int)numN.Value;
        dgvInput.Rows.Clear();
        dgvInput.Columns.Clear();
        for (int j = 0; j < n; j++)
            dgvInput.Columns.Add("c" + j, "a" + (j + 1) + ",i");
        dgvInput.Columns.Add("b", "b");
        for (int i = 0; i < n; i++)
            dgvInput.Rows.Add();
    }

    private void FillControlExample()
    {
        int n = (int)numN.Value;
        if (n != 4) return; // контрольный пример — 4×4

        double[,] A = {
            {5, 7,  6,  5},
            {7, 10, 8,  7},
            {6, 8,  10, 9},
            {5, 7,  9,  10}
        };
        double[] B = { 23, 32, 33, 31 };

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
                dgvInput.Rows[i].Cells[j].Value = A[i, j].ToString(CultureInfo.InvariantCulture);
            dgvInput.Rows[i].Cells[4].Value = B[i].ToString(CultureInfo.InvariantCulture);
        }
    }

    private void BtnFillExample_Click(object sender, EventArgs e)
    {
        numN.Value = 4;
        ResizeInput();
        FillControlExample();
    }

    private void BtnClear_Click(object sender, EventArgs e)
    {
        dgvLog.Rows.Clear();
        dgvResults.Rows.Clear();
    }

    // -----------------------------------------------------------------
    // 4. Чтение исходных данных
    // -----------------------------------------------------------------
    private bool TryParseNum(string s, out double v)
    {
        s = (s ?? "").Trim().Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
    }

    private double[,] ReadMatrix(int n)
    {
        var A = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                var cell = dgvInput.Rows[i].Cells[j].Value;
                if (cell == null || !TryParseNum(cell.ToString(), out A[i, j]))
                {
                    MessageBox.Show($"Некорректное значение a[{i + 1},{j + 1}].",
                        "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
            }
        return A;
    }

    private double[] ReadVector(int n)
    {
        var b = new double[n];
        for (int i = 0; i < n; i++)
        {
            var cell = dgvInput.Rows[i].Cells[n].Value;
            if (cell == null || !TryParseNum(cell.ToString(), out b[i]))
            {
                MessageBox.Show($"Некорректное значение b[{i + 1}].",
                    "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }
        return b;
    }

    // -----------------------------------------------------------------
    // 5. Кнопка «Вычислить»
    // -----------------------------------------------------------------
    private void BtnCalculate_Click(object sender, EventArgs e)
    {
        dgvLog.Rows.Clear();
        dgvResults.Rows.Clear();

        int n = (int)numN.Value;
        double[,] A = ReadMatrix(n);
        if (A == null) return;
        double[] B = ReadVector(n);
        if (B == null) return;

        int method = cmbMethod.SelectedIndex;
        bool compare = cmbAlgorithm.SelectedIndex == 1;

        try
        {
            if (!compare)
            {
                RunCase("Исходная", A, B, method, n);
            }
            else
            {
                double[] bVals = { 0.1, 0.001, 0.01 };
                foreach (var bv in bVals)
                {
                    var B2 = new double[n];
                    for (int i = 0; i < n; i++) B2[i] = bv;
                    RunCase("B=" + bv.ToString("G4", CultureInfo.InvariantCulture),
                            A, B2, method, n);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка вычисления",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // -----------------------------------------------------------------
    // 6. Один прогон метода с журналом
    // -----------------------------------------------------------------
    private void RunCase(string label, double[,] Aorig, double[] Borig, int method, int n)
    {
        var a = (double[,])Aorig.Clone();
        var b = (double[])Borig.Clone();
        double[] x;
        double det;

        dgvLog.Rows.Add(label, "", "Начало расчёта",
            $"A: {n}×{n}, B = [{string.Join("; ", Array.ConvertAll(Borig, v => v.ToString("G6", CultureInfo.InvariantCulture)))}]");

        if (method == 0)
            SolveGauss(a, b, n, optimal: false, out x, out det, log: true);
        else if (method == 1)
            SolveGauss(a, b, n, optimal: true, out x, out det, log: true);
        else
            SolveGaussJordan(a, b, n, out x, out det, log: true);

        if (x == null)
        {
            dgvResults.Rows.Add(label, "Решение", "— (система вырождена)");
            dgvResults.Rows.Add(label, "det A", "0");
            return;
        }

        // невязка ‖A·x − B‖∞ (по исходным матрице и вектору!)
        double maxRes = 0;
        for (int i = 0; i < n; i++)
        {
            double s = 0;
            for (int j = 0; j < n; j++) s += Aorig[i, j] * x[j];
            maxRes = Math.Max(maxRes, Math.Abs(s - Borig[i]));
        }

        for (int i = 0; i < n; i++)
            dgvResults.Rows.Add(label, "x" + (i + 1), x[i].ToString("G10", CultureInfo.InvariantCulture));
        dgvResults.Rows.Add(label, "det A", det.ToString("G10", CultureInfo.InvariantCulture));
        dgvResults.Rows.Add(label, "‖r‖∞", maxRes.ToString("G6", CultureInfo.InvariantCulture));
    }

    // -----------------------------------------------------------------
    // 7. Метод Гаусса (прямой + обратный ход)
    // -----------------------------------------------------------------
    private void SolveGauss(double[,] a, double[] b, int n, bool optimal,
                            out double[] x, out double det, bool log)
    {
        x = null;
        det = 1.0;
        double sign = 1.0;

        for (int k = 0; k < n; k++)
        {
            // ---- выбор ведущего элемента ----
            int pivot;
            if (optimal)
            {
                pivot = k;
                for (int i = k + 1; i < n; i++)
                    if (Math.Abs(a[i, k]) > Math.Abs(a[pivot, k])) pivot = i;
            }
            else
            {
                pivot = -1;
                for (int i = k; i < n; i++)
                    if (Math.Abs(a[i, k]) > 1e-14) { pivot = i; break; }
            }
            if (pivot < 0 || Math.Abs(a[pivot, k]) < 1e-14)
            {
                det = 0;
                if (log) dgvLog.Rows.Add("Прямой", "k=" + (k + 1),
                    "Ведущий элемент равен 0 — система вырождена", "");
                return;
            }

            // ---- перестановка строк ----
            if (pivot != k)
            {
                for (int j = 0; j < n; j++)
                {
                    double t = a[k, j]; a[k, j] = a[pivot, j]; a[pivot, j] = t;
                }
                double tb = b[k]; b[k] = b[pivot]; b[pivot] = tb;
                sign = -sign;
                if (log) dgvLog.Rows.Add("Прямой", "k=" + (k + 1),
                    $"R{k + 1} ↔ R{pivot + 1}", "");
            }

            if (log) dgvLog.Rows.Add("Прямой", "k=" + (k + 1),
                $"Ведущий a[{k + 1},{k + 1}] = {a[k, k]:G6}", "");

            // ---- исключение ----
            for (int i = k + 1; i < n; i++)
            {
                double f = a[i, k] / a[k, k];
                if (Math.Abs(f) < 1e-14) { a[i, k] = 0; continue; }
                for (int j = k; j < n; j++) a[i, j] -= f * a[k, j];
                b[i] -= f * b[k];
                a[i, k] = 0;
                if (log) dgvLog.Rows.Add("Прямой", "k=" + (k + 1),
                    $"R{i + 1} -= {f:G6}·R{k + 1}", "");
            }

            if (log && n <= 10)
                dgvLog.Rows.Add("Матрица", "k=" + (k + 1), "После шага", FormatMatrix(a, b, n));
        }

        // ---- детерминант ----
        double d = sign;
        for (int i = 0; i < n; i++) d *= a[i, i];
        det = d;

        // ---- обратный ход ----
        x = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            double s = b[i];
            for (int j = i + 1; j < n; j++) s -= a[i, j] * x[j];
            x[i] = s / a[i, i];
            if (log) dgvLog.Rows.Add("Обратный", "i=" + (i + 1),
                $"x{i + 1} = (b{i + 1} − Σ a·x) / a{i + 1},{i + 1}",
                x[i].ToString("G10", CultureInfo.InvariantCulture));
        }
    }

    // -----------------------------------------------------------------
    // 8. Метод Гаусса–Жордана (без обратного хода)
    // -----------------------------------------------------------------
    private void SolveGaussJordan(double[,] a, double[] b, int n,
                                  out double[] x, out double det, bool log)
    {
        x = null;
        det = 1.0;
        double sign = 1.0;

        for (int k = 0; k < n; k++)
        {
            // ---- ведущий элемент (максимум по столбцу) ----
            int pivot = k;
            for (int i = k + 1; i < n; i++)
                if (Math.Abs(a[i, k]) > Math.Abs(a[pivot, k])) pivot = i;
            if (Math.Abs(a[pivot, k]) < 1e-14)
            {
                det = 0;
                if (log) dgvLog.Rows.Add("Прямой", "k=" + (k + 1),
                    "Ведущий элемент равен 0 — система вырождена", "");
                return;
            }

            if (pivot != k)
            {
                for (int j = 0; j < n; j++)
                {
                    double t = a[k, j]; a[k, j] = a[pivot, j]; a[pivot, j] = t;
                }
                double tb = b[k]; b[k] = b[pivot]; b[pivot] = tb;
                sign = -sign;
                if (log) dgvLog.Rows.Add("Прямой", "k=" + (k + 1),
                    $"R{k + 1} ↔ R{pivot + 1}", "");
            }

            double d = a[k, k];
            det *= d;
            for (int j = 0; j < n; j++) a[k, j] /= d;
            b[k] /= d;
            a[k, k] = 1;
            if (log) dgvLog.Rows.Add("Прямой", "k=" + (k + 1),
                $"R{k + 1} /= {d:G6}", "");

            // ---- исключение во всех остальных строках ----
            for (int i = 0; i < n; i++)
            {
                if (i == k) continue;
                double f = a[i, k];
                if (Math.Abs(f) < 1e-14) { a[i, k] = 0; continue; }
                for (int j = 0; j < n; j++) a[i, j] -= f * a[k, j];
                b[i] -= f * b[k];
                a[i, k] = 0;
                if (log) dgvLog.Rows.Add("Прямой", "k=" + (k + 1),
                    $"R{i + 1} -= {f:G6}·R{k + 1}", "");
            }

            if (log && n <= 10)
                dgvLog.Rows.Add("Матрица", "k=" + (k + 1), "После шага", FormatMatrix(a, b, n));
        }

        det *= sign;
        x = new double[n];
        for (int i = 0; i < n; i++) x[i] = b[i];
    }

    // -----------------------------------------------------------------
    // 9. Форматирование матрицы для журнала
    // -----------------------------------------------------------------
    private string FormatMatrix(double[,] a, double[] b, int n)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            sb.Append("| ");
            for (int j = 0; j < n; j++)
                sb.Append(a[i, j].ToString("G6", CultureInfo.InvariantCulture).PadLeft(10)).Append(' ');
            sb.Append("| ").Append(b[i].ToString("G6", CultureInfo.InvariantCulture).PadLeft(10));
            sb.AppendLine(" |");
        }
        return sb.ToString().TrimEnd();
    }
}