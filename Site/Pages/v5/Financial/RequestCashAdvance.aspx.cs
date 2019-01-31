﻿using System;
using System.Globalization;
using System.Web;
using Swarmops.Interface.Support;
using Swarmops.Logic.Financial;
using Swarmops.Logic.Security;

namespace Swarmops.Frontend.Pages.v5.Financial
{
    public partial class RequestCashAdvance : PageV5Base
    {
        protected void Page_Load (object sender, EventArgs e)
        {
            if (!CurrentOrganization.IsEconomyEnabled)
            {
                Response.Redirect ("/Pages/v5/Financial/EconomyNotEnabled.aspx", true);
                return;
            }

            this.BoxTitle.Text = PageTitle = Resources.Pages.Financial.RequestCashAdvance_PageTitle;
            PageIcon = "iconshock-walletmoney";
            InfoBoxLiteral = Resources.Pages.Financial.RequestCashAdvance_Info;

            PageAccessRequired = new Access (CurrentOrganization, AccessAspect.Participant, AccessType.Write);

            if (!Page.IsPostBack)
            {
                // Prime bank details

                this.TextBank.Text = CurrentUser.BankName;
                this.TextClearing.Text = CurrentUser.BankClearing;
                this.TextAccount.Text = CurrentUser.BankAccount;
                this.TextAmount.Cents = 0;
                this.TextAmount.Focus();

                Localize();
            }
        }


        private void Localize()
        {
            this.LabelAmount.Text = string.Format (Resources.Pages.Financial.RequestCashAdvance_Amount,
                CurrentOrganization.Currency.DisplayCode);
            this.LabelPurpose.Text = Resources.Pages.Financial.RequestCashAdvance_Purpose;
            this.LabelBudget.Text = Resources.Pages.Financial.RequestCashAdvance_Budget;
            this.LabelHeaderBankDetails.Text = Resources.Pages.Financial.RequestCashAdvance_HeaderBankDetails;
            this.LabelBankName.Text = Resources.Pages.Financial.RequestCashAdvance_BankName;
            this.LabelBankClearing.Text = Resources.Pages.Financial.RequestCashAdvance_BankClearing;
            this.LabelBankAccount.Text = Resources.Pages.Financial.RequestCashAdvance_BankAccount;
        }


        protected void ButtonRequest_Click (object sender, EventArgs e)
        {
            // The data has been validated client-side already. We'll throw unfriendly exceptions if invalid data is passed here.

            Int64 amountCents = this.TextAmount.Cents;

            string description = this.TextPurpose.Text;

            FinancialAccount budget = this.ComboBudgets.SelectedAccount;

            // sanity check

            if (budget.Organization.Identity != CurrentOrganization.Identity)
            {
                throw new InvalidOperationException ("Budget-organization mismatch; won't file cash advance");
            }

            // Store bank details for current user

            CurrentUser.BankName = this.TextBank.Text;
            CurrentUser.BankClearing = this.TextClearing.Text;
            CurrentUser.BankAccount = this.TextAccount.Text;

            // Create cash advance

            CashAdvance cashAdvance = CashAdvance.Create (CurrentOrganization, CurrentUser, CurrentUser, amountCents,
                budget, description);

            // Create success message

            string successMessage = string.Format (Resources.Pages.Financial.RequestCashAdvance_SuccessMessagePartOne,
                HttpUtility.HtmlEncode (CurrentUser.Name),
                HttpUtility.HtmlEncode (description), CurrentOrganization.Currency.Code,
                amountCents/100.0);

            if (budget.OwnerPersonId != CurrentUser.Identity)
            {
                successMessage += "<br/><br/>" + Resources.Pages.Financial.RequestCashAdvance_SuccessMessagePartTwo +
                                  "<br/>";
            }
            else
            {
                successMessage += "<br/><br/>" +
                                  Resources.Pages.Financial.RequestCashAdvance_SuccessMessagePartTwoOwnBudget +
                                  "<br/>";
                cashAdvance.Approve (CurrentUser);
            }

            DashboardMessage.Set (successMessage);

            // Redirect to dashboard

            Response.Redirect ("/", true);
        }


        // ReSharper disable InconsistentNaming

        public string Localized_ValidationError_BankAccount
        {
            get { return JavascriptEscape(Resources.Pages.Financial.RequestCashAdvance_ValidationError_BankAccount); }
        }

        public string Localized_ValidationError_BankClearing
        {
            get { return JavascriptEscape(Resources.Pages.Financial.RequestCashAdvance_ValidationError_BankClearing); }
        }

        public string Localized_ValidationError_BankName
        {
            get { return JavascriptEscape(Resources.Pages.Financial.RequestCashAdvance_ValidationError_BankName); }
        }

        public string Localized_ValidationError_Purpose
        {
            get { return JavascriptEscape(Resources.Pages.Financial.RequestCashAdvance_ValidationError_Purpose); }
        }

        public string Localized_ValidationError_Budget
        {
            get { return JavascriptEscape(Resources.Pages.Financial.RequestCashAdvance_ValidationError_Budget); }
        }

        public string Localized_ValidationError_Amount
        {
            get { return JavascriptEscape(String.Format(Resources.Pages.Financial.RequestCashAdvance_ValidationError_Amount, CurrentOrganization.Currency.DisplayCode)); }
        }
    }
}