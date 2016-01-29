using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.IdentityModel.Claims;

namespace ClaimsViewerWebPart.ClaimsViewerWebPart
{
    public partial class ClaimsViewerWebPartUserControl : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            IClaimsPrincipal claimsPrincipal = Page.User as IClaimsPrincipal;
            IClaimsIdentity claimsIdentity = (IClaimsIdentity)claimsPrincipal.Identity;

            GridView1.DataSource = claimsIdentity.Claims;
            Page.DataBind();
        }
    }
}
