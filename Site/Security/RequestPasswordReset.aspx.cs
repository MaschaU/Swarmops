﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using Swarmops.Frontend;
using Swarmops.Logic.Communications;
using Swarmops.Logic.Communications.Payload;
using Swarmops.Logic.Support;
using Swarmops.Logic.Support.LogEntries;
using Swarmops.Logic.Swarm;

namespace Swarmops.Pages.Security
{
    public partial class RequestPasswordReset : DataV5Base // "Data" because we don't have a master page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            this.Title = Resources.Pages.Security.ResetPassword_PageTitle;

            if (!Page.IsPostBack)
            {
                Localize();
            }

            this.TextMailAddress.Focus();
        }

        private void Localize()
        {
            this.LabelContentTitle.Text = Resources.Pages.Security.ResetPassword_PageTitle;
            this.LabelMail.Text = Resources.Pages.Security.ResetPassword_Mail1;
            this.LabelSuccessMaybe.Text = Resources.Pages.Security.ResetPassword_TicketSentMaybe;
            this.ButtonRequest.Text = Resources.Pages.Security.ResetPassword_Reset;
        }

        [WebMethod]
        public static bool RequestTicket (string mailAddress)
        {
            mailAddress = mailAddress.Trim();

            if (string.IsNullOrEmpty (mailAddress))
            {
                return false; // this is the only case when we return false: a _syntactically_invalid_ address
            }

            People concernedPeople = People.FromMail (mailAddress); // Should result in exactly 1

            if (concernedPeople.Count != 1)
            {
                return true; // TODO: Prevent registration with duplicate mail addy, or this will cause problems down the road
            }

            Person concernedPerson = concernedPeople[0];

            if (!string.IsNullOrEmpty (concernedPerson.BitIdAddress))
            {
                // Cannot reset password - two factor auth is enabled. Manual intervention required.

                OutboundComm.CreateSecurityNotification (concernedPerson, null, null, string.Empty,
                    NotificationResource.Password_CannotReset2FA);
                return true; // still returning true - the fail info is in mail only
            }


            string resetTicket = SupportFunctions.GenerateSecureRandomKey (16);
            resetTicket = resetTicket.Substring (0, 21); // We're using a 21-character (84-bit) key mostly for UI consistency with the ticket sent in mail, and it's secure enough

            concernedPerson.ResetPasswordTicket = DateTime.UtcNow.AddHours (1).ToString(CultureInfo.InvariantCulture) + "," + resetTicket; // Adds expiry - one hour

            OutboundComm.CreateSecurityNotification (concernedPerson, null, null, resetTicket,
                NotificationResource.Password_ResetOnRequest);

            SwarmopsLog.CreateEntry (null,
                new PasswordResetRequestLogEntry (concernedPerson, SupportFunctions.GetRemoteIPAddressChain()));

            return true;
        }

    }
}