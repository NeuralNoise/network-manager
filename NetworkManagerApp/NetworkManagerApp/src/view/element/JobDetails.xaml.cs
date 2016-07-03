﻿using NetworkManager.DomainContent;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NetworkManager.Scheduling;
using NetworkManager.View.Component.Job;

namespace NetworkManager.View.Component {

    /// <summary>
    /// Logique d'interaction pour JobDetails.xaml
    /// </summary>
    public partial class JobDetails : UserControl {

        public JobSchedulerWindow parent { get; set; }
        private List<Computer> preSelectedComputers { get; set; } = new List<Computer>();

        private Scheduling.Job job;

        public JobDetails() {
            InitializeComponent();

            // Fill the hours and minutes
            jobHoursPicker.Items.Clear();
            for (int i = 0; i < 24; i++)
                jobHoursPicker.Items.Add($"{i}h");

            jobMinutesPicker.Items.Clear();
            for (int i = 0; i < 60; i++)
                jobMinutesPicker.Items.Add($"{i}mn");

            // Control elements to disable
            elementToDisable = new List<FrameworkElement> {
                textBox_TaskName,
                jobDatePicker,
                jobHoursPicker,
                jobMinutesPicker,
                jobNowCheckbox,
                selectedComputersGrid,
                buttonSelectAll,
                buttonDeselectAll,
                buttonAddTask,
                selectedComputersLabel,
                tasksPanel
            };
        }

        /// <summary>
        /// Fill recusively the computer panel with the provided domain
        /// </summary>
        /// <param name="d"></param>
        private void fillComputers(Domain d) {
            foreach (Computer c in d.computers) {
                selectedComputersGrid.Items.Add(c);

                if (preSelectedComputers.Find(cSelected => cSelected.nameLong == c.nameLong) != null) {
                    selectedComputersGrid.SelectedItems.Add(c);
                }
            }

            foreach (Domain subDomain in d.domains) {
                fillComputers(subDomain);
            }
        }

        List<FrameworkElement> elementToDisable;

        /// <summary>
        /// Show a job in the panel. If the job is null, the panel will be reset
        /// </summary>
        /// <param name="job">The job to show</param>
        public void setJob(Scheduling.Job job) {
            this.job = job;

            if(job == null) {
                reset();
                return;
            }

            if(job.status == JobStatus.CREATED) {
                elementToDisable.ForEach(element => element.IsEnabled = true);
            } else {
                elementToDisable.ForEach(element => element.IsEnabled = false);
            }

            label_jobDetailsTitle.Content = "Selected Job";
            textBox_TaskName.Text = job.name;

            if(job.scheduledDateTime != DateTime.MinValue) {
                jobNowCheckbox.IsChecked = false;
                jobDatePicker.SelectedDate = job.scheduledDateTime;
                jobHoursPicker.SelectedIndex = job.scheduledDateTime.Hour;
                jobMinutesPicker.SelectedIndex = job.scheduledDateTime.Minute;
            } else {
                jobHoursPicker.SelectedIndex = -1;
                jobMinutesPicker.SelectedIndex = -1;
                jobDatePicker.SelectedDate = null;
                jobNowCheckbox.IsChecked = true;
            }

            // Selected computers
            selectedComputersGrid.SelectedItems.Clear();
            foreach (Computer c in selectedComputersGrid.Items) {
                foreach(ComputerInfo ci in job.computers) {
                    if(ci.name == c.nameLong) {
                        selectedComputersGrid.SelectedItems.Add(c);
                        break;
                    }
                }
            }

            // Tasks
            tasksPanel.Children.Clear();
            foreach (JobTask task in job.tasks) {
                dynamic panel = null;

                switch(task.type) {
                    case JobTaskType.INSTALL_SOFTWARE:
                        panel = new TaskInstall();
                        break;
                    case JobTaskType.REBOOT:
                        panel = new TaskReboot();
                        break;
                    case JobTaskType.SHUTDOWN:
                        panel = new TaskShutdown();
                        break;
                    case JobTaskType.WAKE_ON_LAN:
                        panel = new TaskWakeOnLan();
                        break;
                }

                if(panel != null) {
                    panel.initFromTask(task);
                    tasksPanel.Children.Add(panel);
                }
            }
            
            // Terminated with reports
            if (job.status == JobStatus.TERMINATED && job.reports != null && job.reports.Count > 0) {
                buttonShowReport.Visibility = Visibility.Visible;
                buttonCancel.Visibility = Visibility.Collapsed;
                parent.jobReportDetails.setJob(job);
            } 
            // Scheduled (can be cancelled)
            else if (job.status == JobStatus.CREATED || job.status == JobStatus.SCHEDULED) {
                buttonShowReport.Visibility = Visibility.Collapsed;
                buttonCancel.Visibility = Visibility.Visible;
            } 
            // In progress..
            else {
                buttonShowReport.Visibility = Visibility.Collapsed;
                buttonCancel.Visibility = Visibility.Collapsed;
            }
            buttonCreateJob.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Reset the panel
        /// </summary>
        public void reset() {
            label_jobDetailsTitle.Content = "New Job";
            elementToDisable.ForEach(element => element.IsEnabled = true);

            textBox_TaskName.Text = string.Empty;

            jobNowCheckbox.IsChecked = false;
            jobDatePicker.SelectedDate = null;
            jobHoursPicker.SelectedIndex = -1;
            jobMinutesPicker.SelectedIndex = -1;

            selectedComputersGrid.SelectedItems.Clear();

            tasksPanel.Children.Clear();

            buttonCreateJob.Visibility = Visibility.Visible;
            buttonShowReport.Visibility = Visibility.Collapsed;
            buttonCancel.Visibility = Visibility.Collapsed;
        }

        private void buttonAddTask_Click(object sender, RoutedEventArgs e) {
            TaskSelectionWindow selectTask = new TaskSelectionWindow();
            selectTask.mainWindow = this;
            selectTask.Left = parent.Left + 50;
            selectTask.Top = parent.Top + 50;
            selectTask.Show();
        }

        private void jobNow_Click(object sender, RoutedEventArgs e) {
            jobDatePicker.IsEnabled = jobNowCheckbox.IsChecked == false;
            jobHoursPicker.IsEnabled = jobNowCheckbox.IsChecked == false;
            jobMinutesPicker.IsEnabled = jobNowCheckbox.IsChecked == false;
        }

        private async void selectedComputersGrid_Loaded(object sender, RoutedEventArgs e) {
            // Fill the computers
            var domain = new Domain();
            await domain.fill(false);

            selectedComputersGrid.Items.Clear();
            fillComputers(domain);
        }

        private void selectedComputersGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
            selectedComputersLabel.Content = $"{selectedComputersGrid.SelectedItems.Count} computer{(selectedComputersGrid.SelectedItems.Count > 1 ? "s" : "")} selected";
        }

        private void buttonSelectAll_Click(object sender, RoutedEventArgs e) {
            selectedComputersGrid.SelectedItems.Clear();
            foreach (Computer c in selectedComputersGrid.Items)
                selectedComputersGrid.SelectedItems.Add(c);
        }

        private void buttonDeselectAll_Click(object sender, RoutedEventArgs e) {
            selectedComputersGrid.SelectedItems.Clear();
        }

        internal void downTask(UIElement element) {
            int i = tasksPanel.Children.IndexOf(element);

            if (i + 1 < tasksPanel.Children.Count) {
                tasksPanel.Children.RemoveAt(i);
                tasksPanel.Children.Insert(i + 1, element);
            }
        }

        internal void upTask(UIElement element) {
            int i = tasksPanel.Children.IndexOf(element);

            if (i > 0) {
                tasksPanel.Children.RemoveAt(i);
                tasksPanel.Children.Insert(i - 1, element);
            }
        }

        internal void deleteTask(UIElement element) {
            int i = tasksPanel.Children.IndexOf(element);

            if (i >= 0) {
                tasksPanel.Children.RemoveAt(i);
            }
        }

        internal void selectComputer(Computer computer) {
            List<Computer> computers = new List<Computer>();
            computers.Add(computer);
            this.preSelectedComputers = computers;
        }

        internal void selectComputers(List<Computer> computers) {
            this.preSelectedComputers = computers;
        }

        private void buttonCreateJob_Click(object sender, RoutedEventArgs e) {

            // Get the picked date
            DateTime jobDateTime;
            if (jobNowCheckbox.IsChecked == true) {
                jobDateTime = DateTime.MinValue;
            } else {
                if (jobDatePicker.SelectedDate == null || jobHoursPicker.SelectedItem == null || jobMinutesPicker.SelectedItem == null) {
                    MessageBox.Show("Error : An execution date must be provided to the job", "Job creation error");
                    return;
                }

                jobDateTime = jobDatePicker.SelectedDate.Value;
                jobDateTime = jobDateTime.AddHours(jobHoursPicker.SelectedIndex);
                jobDateTime = jobDateTime.AddMinutes(jobMinutesPicker.SelectedIndex);

                if (jobDateTime < DateTime.Now) {
                    MessageBox.Show("Error : The specified date can't be in the past", "Job creation error");
                    return;
                }
            }

            // Get the selected computers
            if (selectedComputersGrid.SelectedItems.Count <= 0) {
                MessageBox.Show("Error : No computer selected. At least one computer should be selected", "Job creation error");
                return;
            }

            var selectedComputers = new List<ComputerInfo>();
            foreach (Computer c in selectedComputersGrid.SelectedItems) {
                selectedComputers.Add(App.computerInfoStore.getComputerInfoByName(c.nameLong));
            }

            // Get the tasks
            var tasks = new List<JobTask>();

            foreach (dynamic element in tasksPanel.Children) {
                JobTask jobTask = element.createTask();

                if (jobTask == null)
                    return; // Error during jobtask creation

                tasks.Add(jobTask);
            }

            if (tasks.Count <= 0) {
                MessageBox.Show("Error : No task defined. At least one taks should be defined", "Job creation error");
                return;
            }

            // OK, create the job
            var job = new Scheduling.Job {
                scheduledDateTime = jobDateTime,
                creationDate = DateTime.Now,
                computers = selectedComputers,
                status = JobStatus.SCHEDULED,
                tasks = tasks,
                name = textBox_TaskName.Text
            };

            // Insert into the job store
            App.jobStore.insertJob(job);

            // Create a windows task
            job.schedule();

            parent.updateScheduledJobs();

            reset();
        }

        private void buttonShowReport_Click(object sender, RoutedEventArgs e) {
            parent.jobDetails.Visibility = Visibility.Collapsed;
            parent.jobReportDetails.Visibility = Visibility.Visible;
        }

        private void button_JobReload_Click(object sender, RoutedEventArgs e) {
            if(job != null)
                setJob(App.jobStore.getJobById(job.id));
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e) {
            MessageBoxResult result = MessageBox.Show($"Are you sure you want to cancel this job ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                return;

            job = App.jobStore.getJobById(job.id);

            if(job.status == JobStatus.CREATED || job.status == JobStatus.SCHEDULED) {
                job.unSchedule();
                job.status = JobStatus.CANCELLED;
                App.jobStore.updateJob(job);
            }

            setJob(job);
            parent.updateScheduledJobs();
        }
    }
}
