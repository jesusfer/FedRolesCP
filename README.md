# FedRolesCP
A SharePoint claim provider that does claim augmentation and injects role claims in user identities.

There are some situations where the roles in the identities cannot be refreshed and this claim provider aims to be an example of how those roles can be retrieved and injected back in the user tokens.

The motivation of this project can be found here: http://blogs.msdn.com/b/jesusfer/archive/2016/01/25/sharepoint-2013-role-claims-augmentation.aspx

## Included in the project:
- A claim provider that does the following:
  - Supposes that ADFS uses Active Directory to authenticate the users, so it looks for the user's group memberships in Active Directory.
  - Adds the groups the user is member of as Role claim in the user identity.
  - Does *not* compute group memberships recursively.
  - ULS logging.
- A web part that can be used to see the claims of the currently logged in user for testing purposes.
