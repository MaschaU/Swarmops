﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Activizr.Basic.Enums;
using Activizr.Logic.Financial;
using Activizr.Logic.Pirates;
using Activizr.Logic.Security;
using Activizr.Logic.Structure;
using System.Globalization;

public partial class Pages_v5_Ledgers_Csv_ProfitLossData : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Current authentication

        string identity = HttpContext.Current.User.Identity.Name;
        string[] identityTokens = identity.Split(',');

        string userIdentityString = identityTokens[0];
        string organizationIdentityString = identityTokens[1];

        int currentUserId = Convert.ToInt32(userIdentityString);
        int currentOrganizationId = Convert.ToInt32(organizationIdentityString);

        Person currentUser = Person.FromIdentity(currentUserId);
        Authority authority = currentUser.GetAuthority();
        Organization currentOrganization = Organization.FromIdentity(currentOrganizationId);

        // Get culture

        string cultureString = "en-US";
        HttpCookie cookie = Request.Cookies["PreferredCulture"];

        if (cookie != null)
        {
            cultureString = cookie.Value;
        }

        _renderCulture = new CultureInfo(cultureString);

        // Get current year

        _year = DateTime.Today.Year;

        string yearParameter = Request.QueryString["Year"];

        if (!string.IsNullOrEmpty(yearParameter))
        {
            _year = Int32.Parse(yearParameter); // will throw if non-numeric - don't matter for app
        }

        YearlyReport report = YearlyReport.Create(currentOrganization, _year, FinancialAccountType.Result);

        Response.ClearContent();
        Response.ClearHeaders();
        Response.ContentType = "text/plain";
        Response.AppendHeader("Content-Disposition", "attachment;filename=" + Resources.Pages.Ledgers.ProfitLossStatement_DownloadFileName + _year.ToString(CultureInfo.InvariantCulture) + "-" + DateTime.Today.ToString("yyyyMMdd") + ".csv");

        if (_year == DateTime.Today.Year)
        {
            Response.Output.WriteLine("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"", Resources.Pages.Ledgers.ProfitLossStatement_AccountName, Resources.Pages.Ledgers.ProfitLossStatement_LastYear,
                Resources.Pages.Ledgers.ProfitLossStatement_Q1, Resources.Pages.Ledgers.ProfitLossStatement_Q2, Resources.Pages.Ledgers.ProfitLossStatement_Q3, Resources.Pages.Ledgers.ProfitLossStatement_Q4,
                Resources.Pages.Ledgers.ProfitLossStatement_Ytd);
        }
        else
        {
            Response.Output.WriteLine("\"{0}\",\"{1}\",\"{6}-{2}\",\"{6}-{3}\",\"{6}-{4}\",\"{6}-{5}\",\"{6}\"", Resources.Pages.Ledgers.ProfitLossStatement_AccountName, _year-1,
                Resources.Pages.Ledgers.ProfitLossStatement_Q1, Resources.Pages.Ledgers.ProfitLossStatement_Q2, Resources.Pages.Ledgers.ProfitLossStatement_Q3, Resources.Pages.Ledgers.ProfitLossStatement_Q4,
                _year);
        }

        RecurseCsvReport(report.ReportLines, string.Empty);

        Response.End();
    }

    private int _year = 2012;
    private CultureInfo _renderCulture;

    private void RecurseCsvReport (List<YearlyReportLine> reportLines, string accountPrefix)
    {
        foreach (YearlyReportLine line in reportLines)
        {
            Response.Output.WriteLine("\"{0}{1}\",{2},{3},{4},{5},{6},{7}",
                                      accountPrefix, line.AccountName, 
                                      line.AccountValues.PreviousYear / -100.0,
                                      line.AccountValues.Quarters[0] / -100.0,
                                      line.AccountValues.Quarters[1] / -100.0,
                                      line.AccountValues.Quarters[2] / -100.0,
                                      line.AccountValues.Quarters[3] / -100.0,
                                      line.AccountValues.ThisYear / -100.0);
        

            if (line.Children.Count > 0)
            {
                RecurseCsvReport(line.Children, "-" + accountPrefix);
            }
        }

    }
}