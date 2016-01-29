# FedRolesCP
A SharePoint claim provider to inject Roles.

There are some situations where the roles in the identities cannot be refreshed and this claim provider aims to be an example of how those roles can be retrieved and injected back in the user tokens.

The motivation of this project can be found here: http://blogs.msdn.com/b/jesusfer/archive/2016/01/25/sharepoint-2013-role-claims-augmentation.aspx

## Included in the project:
- A claim provider that connects to Active Directoy to get the user's information and build the Role claims from the user's group memberships.
- A web part that can be used to see the claims of the currently logged in user for testing purposes.
