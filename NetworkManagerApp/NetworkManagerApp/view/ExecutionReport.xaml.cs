﻿using NetworkManager.Domain;
using NetworkManager.WMIExecution;
using System.Windows;

namespace NetworkManagerApp.view {
    /// <summary>
    /// Logique d'interaction pour ExecutionReport.xaml
    /// </summary>
    public partial class ExecutionReport : Window {

        public WMIExecutionResult executionResult { get; set; }

        public ExecutionReport(Computer computer, WMIExecutionResult result) {
            InitializeComponent();

            this.executionResult = result;

            // Set values
            label_ClientName.Content = $"Software Deployment report on {computer.name}";

            textBox_Duration.Text = result.duration.ToString();
            textBox_ReturnValue.Text = result.returnValue.ToString();
            checkBox_Timeout.IsChecked = result.timeout;
            textBox_Output.Text = result.output;
            textBox_Error.Text = result.err;
        }
    }
}
