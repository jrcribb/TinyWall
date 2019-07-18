﻿using System;
using System.Globalization;
using System.Management;
using TinyWall.Interface.Internal;

namespace PKSoft.Deprecated
{
    internal static class MachineFingerprint
    {
        private static string fingerPrint = string.Empty;
        internal static string Fingerprint()
        {
            if (string.IsNullOrEmpty(fingerPrint))
            {
                fingerPrint = Hasher.HashString(
                    identifier("SerialNumber", "Win32_OperatingSystem") +
                    identifier("Product", "Win32_BaseBoard")
                    );
            }
            return fingerPrint;
        }

        private static string identifier(string wmiProperty, string wmiClass)
        {
            string result = "";
            using (ManagementObjectSearcher mc = new ManagementObjectSearcher(string.Format(CultureInfo.InvariantCulture, @"select {0} from {1}", wmiProperty, wmiClass)))
            {
                ManagementObjectCollection moc = mc.Get();
                foreach (ManagementObject mo in moc)
                {
                    try
                    {
                        result = mo[wmiProperty].ToString();
                        if (!string.IsNullOrEmpty(result))
                            break;
                    }
                    catch
                    {
                    }
                }
            }
            return result;
        }
    }
}