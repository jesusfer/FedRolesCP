using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration.Claims;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Administration;

namespace FedRolesCP.Features.FedRolesCPFeature
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("13660b1c-85b0-494a-aa8e-6af93ddfecc3")]
    public class FedRolesCPFeatureEventReceiver : SPClaimProviderFeatureReceiver
    {
        #region Properties

        public override string ClaimProviderAssembly
        {
            get
            {
                return typeof(FedRolesCP).Assembly.FullName;
            }
        }

        public override string ClaimProviderDescription
        {
            get
            {
                return "A custom claim provider that does claim augmentation to add roles to federated identities.";
            }
        }

        public override string ClaimProviderDisplayName
        {
            get
            {
                return FedRolesCP.ProviderDisplayName;
            }
        }

        public override string ClaimProviderType
        {
            get
            {
                return typeof(FedRolesCP).FullName;
            }
        }

        #endregion

        #region Methods

        #region Event Receiver methods
        private void ExecBaseFeatureActivated(Microsoft.SharePoint.SPFeatureReceiverProperties properties)
        {
            // Wrapper function for base FeatureActivated. Used because base
            // keyword can lead to unverifiable code inside lambda expression.
            base.FeatureActivated(properties);
        }

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            RegisterULSLogger();

            // Add SPClaimProvider
            ExecBaseFeatureActivated(properties);
        }

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            UnregisterULSLogger();
            base.FeatureDeactivating(properties);
        }

        public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        {
            base.FeatureInstalled(properties);
        }

        public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        {
            base.FeatureUninstalling(properties);
        }

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}
        #endregion

        #region ULSLogger methods

        private static void RegisterULSLogger()
        {
            ULSLogger.WriteTraceStandalone(TraceSeverity.Verbose, "Registering ULSLogger");
            // Add logging

            ULSLogger service = ULSLogger.Local;
            if (service == null)
            {
                service = new ULSLogger();
                service.Update();

                if (service.Status != SPObjectStatus.Online)
                {
                    service.Provision();
                }
            }

            ULSLogger.WriteTraceStandalone(TraceSeverity.Verbose, "Done registering ULSLogger");
        }

        private static void UnregisterULSLogger()
        {
            ULSLogger.WriteTraceStandalone(TraceSeverity.Verbose, "Unregistering ULSLogger");
            // Add logging

            ULSLogger service = ULSLogger.Local;
            if (service != null)
            {
                service.Unprovision();
                service.Delete();
            }

            ULSLogger.WriteTraceStandalone(TraceSeverity.Verbose, "Done unregistering ULSLogger");
        }

        #endregion

        #endregion
    }
}
