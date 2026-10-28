using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp2.Properties;
namespace LabIntegral
{
    public class MainForm : Form
    {
        private TabControl tabs;

        private IntegralPanel integralPanel;
        private DifferentialPanel derivativePanel;

        public MainForm()
        {
            Text = "Лабораторная работа — Численные методы";
            Width = 1100;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            Icon = Resources.appIcon2;
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
            derivativePanel = new DifferentialPanel { Dock = DockStyle.Fill };

            var pageIntegral = new TabPage("Интегрирование");
            pageIntegral.Controls.Add(integralPanel);

            var pageDerivative = new TabPage("Дифференцирование");
            pageDerivative.Controls.Add(derivativePanel);

            tabs.TabPages.Add(pageIntegral);
            tabs.TabPages.Add(pageDerivative);

            Controls.Add(tabs);
        }
    }
}