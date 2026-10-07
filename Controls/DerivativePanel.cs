using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
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
