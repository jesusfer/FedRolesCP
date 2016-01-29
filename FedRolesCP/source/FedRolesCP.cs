using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Administration.Claims;
using Microsoft.SharePoint.Utilities;

namespace FedRolesCP
{
    public class FedRolesCP : SPClaimProvider
    {
        #region Properties

        #region SPClaimProvider properties

        internal static string ProviderDisplayName
        {
            get
            {
                return "FedRolesCP";
            }
        }


        internal static string ProviderInternalName
        {
            get
            {
                return "FedRolesCP";
            }
        }
        public override string Name
        {
            get { return ProviderInternalName; }
        }

        /// <summary>
        /// This property tells SharePoint that this Claim Provider supports claim augmentantion.
        /// </summary>
        public override bool SupportsEntityInformation
        {
            get { return true; }
        }

        public override bool SupportsHierarchy
        {
            get { return false; }
        }

        public override bool SupportsResolve
        {
            get { return false; }
        }

        public override bool SupportsSearch
        {
            get { return false; }
        }

        #endregion

        #region Claim properties
        private static string RoleClaimType
        {
            get
            {
                return "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
            }
        }

        private static string RoleClaimValueType
        {
            get
            {
                return Microsoft.IdentityModel.Claims.ClaimValueTypes.String;
            }
        }

        private static string UPNClaimType
        {
            get
            {
                return "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
            }
        }

        private static string EmailClaimType
        {
            get
            {
                return "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
            }
        }

        #endregion

        #endregion

        #region Constructors

        public FedRolesCP(string displayName)
            : base(displayName)
        {
        }

        #endregion

        #region Methods

        #region Claims augmentation

        protected override void FillClaimTypes(List<string> claimTypes)
        {
            if (claimTypes == null)
                throw new ArgumentNullException("claimTypes");

            claimTypes.Add(RoleClaimType);
        }

        protected override void FillClaimValueTypes(List<string> claimValueTypes)
        {
            if (claimValueTypes == null)
                throw new ArgumentNullException("claimValueTypes");

            claimValueTypes.Add(RoleClaimValueType);
        }

        /// <summary>
        /// Use this method to augment the user identity passed as parameter with more claims.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entity">This claim identifies the user being augmented.</param>
        /// <param name="claims">Add the new claims to this list.</param>
        protected override void FillClaimsForEntity(Uri context, SPClaim entity, List<SPClaim> claims)
        {
            using (new SPMonitoredScope("FedRolesCP_FillClaimsForEntity"))
            {
                #region Parameter check
                if (entity == null)
                    throw new ArgumentNullException("entity");

                if (claims == null)
                    throw new ArgumentNullException("claims");
                #endregion

                // Try to parse the identity claim we got
                SPClaim userIdentityClaim = entity;
                try
                {
                    userIdentityClaim = SPClaimProviderManager.DecodeUserIdentifierClaim(entity);
                }
                catch (Exception ex)
                {
                    // This error doesn't have to be fatal, we can try and make do with whatever entity we got in the first place.
                    ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "Error: " + ex.Message);
                }

                // Bail out if this is not a trusted provider user
                if (!userIdentityClaim.OriginalIssuer.StartsWith("TrustedProvider"))
                {
                    ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "This is not a trusted provider: " + userIdentityClaim.OriginalIssuer);
                    return;
                }

                ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "This is the identity claim entity we got");
                ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "ClaimType: " + userIdentityClaim.ClaimType);
                ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "ValueType: " + userIdentityClaim.ValueType);
                ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "OriginalIssuer: " + userIdentityClaim.OriginalIssuer);
                ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "Value: " + userIdentityClaim.Value);

                // Look for a UPN claim as the identity
                // If UPN claim is not found, look for an email claim
                var identityClaimValue = userIdentityClaim.Value;
                if (!(userIdentityClaim.ClaimType.Equals(UPNClaimType) || userIdentityClaim.ClaimType.Equals(EmailClaimType)))
                {
                    // We don't know other identity claims for the moment. Bail out.
                    ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "I don't know this identity claim type");
                    return;
                }

                // Search for this user in the current AD, get the groups the user is member of
                var filterString = new StringBuilder("(&(objectCategory=User)");
                if (userIdentityClaim.ClaimType.Equals(UPNClaimType))
                    filterString.Append("(userPrincipalName=");
                else if (userIdentityClaim.ClaimType.Equals(EmailClaimType))
                    filterString.Append("(mail=");
                filterString.Append(identityClaimValue + "))");

                // Need to run all AD code with the app pool identity to avoid Bind issues
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    SearchResultCollection results = null;

                    var searcher = new DirectorySearcher();
                    searcher.SearchRoot = new DirectoryEntry();
                    searcher.Filter = filterString.ToString();
                    searcher.SearchScope = SearchScope.Subtree;
                    searcher.PropertiesToLoad.Add("memberOf");
                    ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "Using this filter to search AD: " + filterString.ToString());
                    try
                    {
                        results = searcher.FindAll();
                    }
                    catch (Exception exc)
                    {
                        ULSLogger.Unexpected(ULSLogger.CategoryFillClaims, "Got this error looking for the user in AD: " + exc.Message);
                        return;
                    }

                    if (results.Count != 1)
                    {
                        // Zero or more than one user returned. Bail out.
                        ULSLogger.Medium(ULSLogger.CategoryFillClaims, "Got zero or more than one results from AD");
                        results.Dispose();
                        return;
                    }

                    // Add those groups as Role
                    var userEntry = results[0];
                    foreach (string groupDN in userEntry.Properties["memberOf"])
                    {
                        var startIndex = groupDN.IndexOf("=") + 1;
                        var newClaimValue = groupDN.Substring(startIndex, groupDN.IndexOf(",") - startIndex);

                        // Create the claim with the same provider as the identity claim
                        claims.Add(new SPClaim(RoleClaimType, newClaimValue, RoleClaimValueType, userIdentityClaim.OriginalIssuer));
                        ULSLogger.Verbose(ULSLogger.CategoryFillClaims, "Added a claim with value: " + newClaimValue);
                    }

                    // Need to explicitly Dispose this collection
                    results.Dispose();
                });
            }
        }

        #endregion

        #region Not implemented SPClaimProvider methods

        protected override void FillEntityTypes(List<string> entityTypes)
        {
            throw new NotImplementedException();
        }

        protected override void FillHierarchy(Uri context, string[] entityTypes, string hierarchyNodeID, int numberOfLevels, Microsoft.SharePoint.WebControls.SPProviderHierarchyTree hierarchy)
        {
            throw new NotImplementedException();
        }

        protected override void FillResolve(Uri context, string[] entityTypes, SPClaim resolveInput, List<Microsoft.SharePoint.WebControls.PickerEntity> resolved)
        {
            throw new NotImplementedException();
        }

        protected override void FillResolve(Uri context, string[] entityTypes, string resolveInput, List<Microsoft.SharePoint.WebControls.PickerEntity> resolved)
        {
            throw new NotImplementedException();
        }

        protected override void FillSchema(Microsoft.SharePoint.WebControls.SPProviderSchema schema)
        {
            throw new NotImplementedException();
        }

        protected override void FillSearch(Uri context, string[] entityTypes, string searchPattern, string hierarchyNodeID, int maxCount, Microsoft.SharePoint.WebControls.SPProviderHierarchyTree searchTree)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
