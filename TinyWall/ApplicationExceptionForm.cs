﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using System.Reflection;
using PKSoft.WindowsFirewall;
using TinyWall.Interface;

namespace PKSoft
{
    internal partial class ApplicationExceptionForm : Form
    {
        private List<FirewallExceptionV3> TmpExceptionSettings = new List<FirewallExceptionV3>();

        internal List<FirewallExceptionV3> ExceptionSettings
        {
            get { return TmpExceptionSettings; }
        }

        internal ApplicationExceptionForm(FirewallExceptionV3 fwex)
        {
            try
            {
                // Prevent flickering, only if our assembly 
                // has reflection permission. 
                Type type = transparentLabel1.GetType();
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo method = type.GetMethod("SetStyle", flags);

                if (method != null)
                {
                    object[] param = { ControlStyles.SupportsTransparentBackColor, true };
                    method.Invoke(transparentLabel1, param);
                }
            }
            catch
            {
                // Don't do anything, we are running in a trusted contex.
            }

            InitializeComponent();

            this.Icon = Resources.Icons.firewall;
            this.btnOK.Image = GlobalInstances.ApplyBtnIcon;
            this.btnCancel.Image = GlobalInstances.CancelBtnIcon;

            this.TmpExceptionSettings.Add(fwex ?? new FirewallExceptionV3(GlobalSubject.Instance, new UnrestrictedPolicy()));

            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Width = this.Width;
            panel2.Location = new System.Drawing.Point(0, panel1.Height);
            panel2.Width = this.Width;

            cmbTimer.SuspendLayout();
            Dictionary<AppExceptionTimer, KeyValuePair<string, AppExceptionTimer>> timerTexts = new Dictionary<AppExceptionTimer, KeyValuePair<string, AppExceptionTimer>>();
            timerTexts.Add(AppExceptionTimer.Permanent,
                new KeyValuePair<string, AppExceptionTimer>(PKSoft.Resources.Messages.Permanent, AppExceptionTimer.Permanent)
                );
            timerTexts.Add(AppExceptionTimer.Until_Reboot,
                new KeyValuePair<string, AppExceptionTimer>(PKSoft.Resources.Messages.UntilReboot, AppExceptionTimer.Until_Reboot)
                );
            timerTexts.Add(AppExceptionTimer.For_5_Minutes,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XMinutes, 5), AppExceptionTimer.For_5_Minutes)
                );
            timerTexts.Add(AppExceptionTimer.For_30_Minutes,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XMinutes, 30), AppExceptionTimer.For_30_Minutes)
                );
            timerTexts.Add(AppExceptionTimer.For_1_Hour, 
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XHour, 1), AppExceptionTimer.For_1_Hour)
                );
            timerTexts.Add(AppExceptionTimer.For_4_Hours,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XHours, 4), AppExceptionTimer.For_4_Hours)
                );
            timerTexts.Add(AppExceptionTimer.For_9_Hours,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XHours, 9), AppExceptionTimer.For_9_Hours)
                );
            timerTexts.Add(AppExceptionTimer.For_24_Hours,
                new KeyValuePair<string, AppExceptionTimer>(string.Format(CultureInfo.CurrentCulture, PKSoft.Resources.Messages.XHours, 24), AppExceptionTimer.For_24_Hours)
                );

            foreach (AppExceptionTimer timerVal in Enum.GetValues(typeof(AppExceptionTimer)))
            {
                if (timerVal != AppExceptionTimer.Invalid)
                    cmbTimer.Items.Add(timerTexts[timerVal]);
            }
            cmbTimer.DisplayMember = "Key";
            cmbTimer.ValueMember = "Value";
            cmbTimer.ResumeLayout(true);
        }

        private void ApplicationExceptionForm_Load(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Display timer
            for (int i = 0; i < cmbTimer.Items.Count; ++i)
            {
                if (((KeyValuePair<string, AppExceptionTimer>)cmbTimer.Items[i]).Value == TmpExceptionSettings[0].Timer)
                {
                    cmbTimer.SelectedIndex = i;
                    break;
                }
            }

            // Update top colored banner
            bool hasSignature = false;
            bool validSignature = false;
            ExecutableSubject exesub = TmpExceptionSettings[0].Subject as ExecutableSubject;
            if (null != exesub)
            {
                hasSignature = exesub.IsSigned;
                validSignature = exesub.CertValid;
            }

            if (hasSignature && validSignature)
            {
                // Recognized app
                panel1.BackgroundImage = Resources.Icons.green_banner;
                transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, PKSoft.Resources.Messages.RecognizedApplication, exesub.ExecutableName);
            }
            else if (hasSignature && !validSignature)
            {
                // Recognized, but compromised app
                panel1.BackgroundImage = Resources.Icons.red_banner;
                transparentLabel1.Text = string.Format(CultureInfo.InvariantCulture, PKSoft.Resources.Messages.CompromisedApplication, exesub.ExecutableName);
            }
            else
            {
                // Unknown app
                panel1.BackgroundImage = Resources.Icons.blue_banner;
                transparentLabel1.Text = PKSoft.Resources.Messages.UnknownApplication;
            }

            Utils.CenterControlInParent(transparentLabel1);

            // Update subject fields
            switch (TmpExceptionSettings[0].Subject.SubjectType)
            {
                case SubjectType.Global:
                    txtAppPath.Text = "*";
                    txtSrvName.Text = string.Empty;
                    break;
                case SubjectType.Executable:
                    txtAppPath.Text = (TmpExceptionSettings[0].Subject as ExecutableSubject).ExecutablePath;
                    txtSrvName.Text = string.Empty;
                    break;
                case SubjectType.Service:
                    txtAppPath.Text = (TmpExceptionSettings[0].Subject as ServiceSubject).ExecutablePath;
                    txtSrvName.Text = (TmpExceptionSettings[0].Subject as ServiceSubject).ServiceName;
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Update rule/poolicy fields
            switch (TmpExceptionSettings[0].Policy.PolicyType)
            {
                case PolicyType.HardBlock:
                    radBlock.Checked = true;
                    chkRestrictToLocalNetwork.Enabled = false;
                    break;
                case PolicyType.RuleList:
                    radBlock.Enabled = false;
                    radUnrestricted.Enabled = false;
                    radTcpUdpUnrestricted.Enabled = false;
                    radTcpUdpOut.Enabled = false;
                    radOnlySpecifiedPorts.Enabled = false;
                    chkRestrictToLocalNetwork.Enabled = false;
                    break;
                case PolicyType.TcpUdpOnly:
                    TcpUdpPolicy pol = TmpExceptionSettings[0].Policy as TcpUdpPolicy;
                    chkRestrictToLocalNetwork.Checked = pol.LocalNetworkOnly;
                    if (
                        string.Equals(pol.AllowedLocalTcpListenerPorts, "*")
                        && string.Equals(pol.AllowedLocalUdpListenerPorts, "*")
                        && string.Equals(pol.AllowedRemoteTcpConnectPorts, "*")
                        && string.Equals(pol.AllowedRemoteUdpConnectPorts, "*")
                    )
                    {
                        radTcpUdpUnrestricted.Checked = true;
                    }
                    else if (
                        string.Equals(pol.AllowedRemoteTcpConnectPorts, "*")
                        && string.Equals(pol.AllowedRemoteUdpConnectPorts, "*")
                        )
                    {
                        radTcpUdpOut.Checked = true;
                    }
                    else
                    {
                        radOnlySpecifiedPorts.Checked = true;
                    }
                    // Display ports list
                    txtOutboundPortTCP.Text = string.IsNullOrEmpty(pol.AllowedRemoteTcpConnectPorts) ? string.Empty : pol.AllowedRemoteTcpConnectPorts.Replace(",", ", ");
                    txtOutboundPortUDP.Text = string.IsNullOrEmpty(pol.AllowedRemoteUdpConnectPorts) ? string.Empty : pol.AllowedRemoteUdpConnectPorts.Replace(",", ", ");
                    txtListenPortTCP.Text = string.IsNullOrEmpty(pol.AllowedLocalTcpListenerPorts) ? string.Empty : pol.AllowedLocalTcpListenerPorts.Replace(",", ", ");
                    txtListenPortUDP.Text = string.IsNullOrEmpty(pol.AllowedLocalUdpListenerPorts) ? string.Empty : pol.AllowedLocalUdpListenerPorts.Replace(",", ", ");
                    break;
                case PolicyType.Unrestricted:
                    radUnrestricted.Checked = true;
                    chkRestrictToLocalNetwork.Enabled = false;
                    break;
                default:
                    throw new NotImplementedException();
            }
            radRestriction_CheckedChanged(null, null);

            UpdateOKButtonEnabled();
        }

        private static string CleanupPortsList(string str)
        {
            string res = str;
            res = res.Replace(" ", string.Empty);
            res = res.Replace(';', ',');

            // Check validity
            Rule r = new Rule("", "", ProfileType.Private, RuleDirection.In, RuleAction.Allow, Protocol.TCP);
            r.LocalPorts = res;

            return res;
        }
        
        private void UpdateOKButtonEnabled()
        {
            switch (TmpExceptionSettings[0].Subject.SubjectType)
            {
                case SubjectType.Executable:
                case SubjectType.Service:
                    btnOK.Enabled = DatabaseClasses.SubjectIdentity.IsValidExecutablePath((TmpExceptionSettings[0].Subject as ExecutableSubject).ExecutablePath);
                    break;
                case SubjectType.Global:
                    btnOK.Enabled = true;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (radBlock.Checked)
            {
                TmpExceptionSettings[0].Policy = HardBlockPolicy.Instance;
            }
            else if (radOnlySpecifiedPorts.Checked || radTcpUdpOut.Checked || radTcpUdpUnrestricted.Checked)
            {
                TcpUdpPolicy pol = new TcpUdpPolicy();

                try
                {
                    pol.LocalNetworkOnly = chkRestrictToLocalNetwork.Checked;
                    pol.AllowedRemoteTcpConnectPorts = CleanupPortsList(txtOutboundPortTCP.Text);
                    pol.AllowedRemoteUdpConnectPorts = CleanupPortsList(txtOutboundPortUDP.Text);
                    pol.AllowedLocalTcpListenerPorts = CleanupPortsList(txtListenPortTCP.Text);
                    pol.AllowedLocalUdpListenerPorts = CleanupPortsList(txtListenPortUDP.Text);
                    TmpExceptionSettings[0].Policy = pol;
                }
                catch
                {
                    Utils.ShowMessageBox(
                        PKSoft.Resources.Messages.PortListInvalid,
                        PKSoft.Resources.Messages.TinyWall,
                        Microsoft.Samples.TaskDialogCommonButtons.Ok,
                        Microsoft.Samples.TaskDialogIcon.Warning,
                        this);

                    return;
                }
            }
            else if (radUnrestricted.Checked)
            {
                UnrestrictedPolicy pol = new UnrestrictedPolicy();
                pol.LocalNetworkOnly = chkRestrictToLocalNetwork.Checked;
                TmpExceptionSettings[0].Policy = pol;
            }

            this.TmpExceptionSettings[0].CreationDate = DateTime.Now;
            
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            List<string> procList = ProcessesForm.ChooseProcess(this, false);
            if (procList.Count == 0) return;

            ReinitFormFromSubject(new ExecutableSubject(procList[0]));
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
                return;

            ReinitFormFromSubject(new ExecutableSubject(ofd.FileName));
        }

        private void btnChooseService_Click(object sender, EventArgs e)
        {
            ServiceSubject subject = ServicesForm.ChooseService(this);
            if (subject == null) return;

            ReinitFormFromSubject(subject);
        }

        private void ReinitFormFromSubject(ExecutableSubject subject)
        {
            List<FirewallExceptionV3> exceptions = GlobalInstances.AppDatabase.GetExceptionsForApp(subject, true, out DatabaseClasses.Application app);
            if (exceptions.Count == 0)
                return;

            if (app == null)
            {
                // Single known or unknown file
                TmpExceptionSettings[0].Subject = exceptions[0].Subject;
                TmpExceptionSettings[0].Policy = exceptions[0].Policy;
            }
            else
            {
                // Multiple known files
                TmpExceptionSettings = exceptions;

                btnOK_Click(null, null);
                return;
            }

            UpdateUI();
        }

        private void txtAppPath_TextChanged(object sender, EventArgs e)
        {
            UpdateOKButtonEnabled();
        }

        private void txtSrvName_TextChanged(object sender, EventArgs e)
        {
            UpdateOKButtonEnabled();
        }

        private void cmbTimer_SelectedIndexChanged(object sender, EventArgs e)
        {
            TmpExceptionSettings[0].Timer = ((KeyValuePair<string, AppExceptionTimer>)cmbTimer.SelectedItem).Value;
        }

        private void radRestriction_CheckedChanged(object sender, EventArgs e)
        {
            if (radBlock.Checked)
            {
                panel3.Enabled = false;
                txtListenPortTCP.Text = string.Empty;
                txtListenPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Text = string.Empty;
                txtOutboundPortUDP.Text = string.Empty;
            }
            else if (radOnlySpecifiedPorts.Checked)
            {
                panel3.Enabled = true;
                txtListenPortTCP.Text = string.Empty;
                txtListenPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Text = string.Empty;
                txtOutboundPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Enabled = true;
                txtOutboundPortUDP.Enabled = true;
                label7.Enabled = true;
                label8.Enabled = true;
            }
            else if (radTcpUdpOut.Checked)
            {
                panel3.Enabled = true;
                txtListenPortTCP.Text = string.Empty;
                txtListenPortUDP.Text = string.Empty;
                txtOutboundPortTCP.Text = "*";
                txtOutboundPortUDP.Text = "*";
                txtOutboundPortTCP.Enabled = false;
                txtOutboundPortUDP.Enabled = false;
                label7.Enabled = false;
                label8.Enabled = false;
            }
            else if (radTcpUdpUnrestricted.Checked)
            {
                panel3.Enabled = false;
                txtListenPortTCP.Text = "*";
                txtListenPortUDP.Text = "*";
                txtOutboundPortTCP.Text = "*";
                txtOutboundPortUDP.Text = "*";
            }
            else if (radUnrestricted.Checked)
            {
                panel3.Enabled = false;
                txtListenPortTCP.Text = "*";
                txtListenPortUDP.Text = "*";
                txtOutboundPortTCP.Text = "*";
                txtOutboundPortUDP.Text = "*";
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
