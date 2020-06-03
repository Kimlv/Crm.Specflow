﻿using Microsoft.Dynamics365.UIAutomation.Api.UCI;
using Microsoft.Dynamics365.UIAutomation.Browser;
using Microsoft.Xrm.Sdk;
using OpenQA.Selenium;
using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using Vermaat.Crm.Specflow.EasyRepro;
using Vermaat.Crm.Specflow.FormLoadConditions;

namespace Vermaat.Crm.Specflow
{
    public static class HelperMethods
    {
        public static string GetAppSettingsValue(string key, bool emptyAllowed = false, string defaultValue = null)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (!emptyAllowed && string.IsNullOrEmpty(value))
                throw new TestExecutionException(Constants.ErrorCodes.APP_SETTINGS_REQUIRED, key);

            return value ?? defaultValue;
        }
        public static bool IsLabel(this Label label, int lcid, string name)
        {
            return name.Equals(label.GetLabelInLanguage(lcid), StringComparison.CurrentCultureIgnoreCase);
        }

        public static void ExecuteWithRetry(int retryCount, int sleepTime, Action action)
        {
            ExecuteWithRetry(retryCount, sleepTime, () =>
            {
                action();
                return true;
            });
        }

        public static T ExecuteWithRetry<T>(int retryCount, int sleepTime, Func<T> action)
        {
            try
            {
                return action();
            }
            catch
            {
                if (retryCount > 0)
                {
                    Thread.Sleep(sleepTime);
                    return ExecuteWithRetry(retryCount - 1, sleepTime, action);
                }
                else
                    throw;
            }
        }

        public static string GetLabelInLanguage(this Label label, int lcid)
        {
            string result = label.LocalizedLabels.Where(l => l.LanguageCode == lcid).FirstOrDefault()?.Label;

            if (label.UserLocalizedLabel != null && !string.IsNullOrEmpty(label.UserLocalizedLabel.Label) && string.IsNullOrEmpty(result))
                throw new TestExecutionException(Constants.ErrorCodes.LABEL_NOT_TRANSLATED, label.UserLocalizedLabel.Label, lcid);

            return result;
        }

        public static object CrmObjectToPrimitive(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (type == typeof(OptionSetValue))
            {
                return ((OptionSetValue)value).Value;
            }
            else if (type == typeof(EntityReference))
            {
                return ((EntityReference)value).Id;
            }
            else if (type == typeof(Money))
            {
                return ((Money)value).Value;
            }
            else if (type == typeof(OptionSetValueCollection))
            {
                return ((OptionSetValueCollection)value).Where(ov => ov != null).Select(ov => ov.Value);
            }
            return value;
        }

        public static CommandAction GetPreferredCommandActionFromTarget(CrmTestingContext crmContext)
        {
            if (crmContext.IsTarget(Constants.SpecFlow.TARGET_API))
            {
                return CommandAction.PreferApi;
            }
            else if (crmContext.IsTarget(Constants.SpecFlow.TARGET_Chrome) ||
                     crmContext.IsTarget(Constants.SpecFlow.TARGET_Edge) ||
                     crmContext.IsTarget(Constants.SpecFlow.TARGET_Firefox) ||
                     crmContext.IsTarget(Constants.SpecFlow.TARGET_InternetExplorer))
            {
                return CommandAction.ForceBrowser;
            }
            else
            {
                throw new TestExecutionException(Constants.ErrorCodes.UNKNOWN_TAG);
            }
        }

    }
}
