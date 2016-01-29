using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Administration;

namespace FedRolesCP
{
    class ULSLogger : SPDiagnosticsServiceBase
    {
        #region Fields

        private static string ULSLoggerAreaName = "FedRolesCP";
        private static string ULSLoggerServiceName = "AD FS Role Claim Provider Logging";

        private static ULSLogger f_Local;

        public enum LoggingCategories
        {
            FillClaims
        }

        public static string CategoryFillClaims = Enum.GetName(typeof(LoggingCategories), LoggingCategories.FillClaims);

        #endregion

        #region Properties

        public static ULSLogger Local
        {
            get
            {
                if (null == f_Local)
                {
                    f_Local = SPFarm.Local.Services.GetValue<ULSLogger>(ULSLoggerServiceName);
                    //f_Current = new ULSLogger();
                    //ULSLogger.GetLocal<ULSLogger>();
                }
                return f_Local;
            }
        }

        #endregion

        #region Constructors

        public ULSLogger()
            : base(ULSLoggerServiceName, SPFarm.Local) { }

        public ULSLogger(string serviceName, SPFarm local)
            : base(serviceName, local) { }

        #endregion

        #region Methods

        /// <summary>
        /// Provide areas
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<SPDiagnosticsArea> ProvideAreas()
        {
            // First build categories for the area
            List<SPDiagnosticsCategory> diagnosticCategories = new List<SPDiagnosticsCategory>();
            foreach (string categoryName in Enum.GetNames(typeof(LoggingCategories)))
            {
                diagnosticCategories.Add(new SPDiagnosticsCategory(categoryName, null, TraceSeverity.Medium, EventSeverity.Information, 0, 0, false, true));
            }

            // Then build the area
            List<SPDiagnosticsArea> diagnosticAreas = new List<SPDiagnosticsArea>
            {
                new SPDiagnosticsArea(ULSLoggerAreaName, diagnosticCategories)
            };

            return diagnosticAreas;
        }

        public static void Unexpected(string category, string message)
        {
            WriteTrace(category, message, TraceSeverity.Unexpected);
        }

        public static void Verbose(string category, string message)
        {
            WriteTrace(category, message, TraceSeverity.Verbose);
        }

        public static void Medium(string category, string message)
        {
            WriteTrace(category, message, TraceSeverity.Medium);
        }

        public static void WriteTrace(string categoryName, string message, TraceSeverity severity)
        {
            var category = ULSLogger.Local.Areas[ULSLoggerAreaName].Categories[categoryName];
            ULSLogger.Local.WriteTrace(1, category, severity, message);

            //WriteTraceStandalone(severity, message);
        }

        public static void WriteTraceStandalone(TraceSeverity severity, string message)
        {
            var cat = new SPDiagnosticsCategory("Standalone logging", TraceSeverity.Verbose, EventSeverity.None);
            var cats = new List<SPDiagnosticsCategory>();
            cats.Add(cat);
            var area = new SPDiagnosticsArea("FedRolesCP", cats);

            SPDiagnosticsService.Local.WriteTrace(2, cat, severity, message);
        }
        #endregion
    }
}
