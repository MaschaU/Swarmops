﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Swarmops.Common.Enums;
using Swarmops.Common.Interfaces;
using Swarmops.Frontend;
using Swarmops.Logic.Financial;
using Swarmops.Logic.Support;
using Swarmops.Logic.Swarm;

public partial class Pages_v5_Finance_Json_ListInvoicesInbound : DataV5Base
{
    private Dictionary<int, Int64> _attestationRights;
    private InboundInvoices _invoices;

    protected void Page_Load (object sender, EventArgs e)
    {
        // Get invoices and attestation rights

        this._attestationRights = GetAttestationRights();
        this._invoices = InboundInvoices.ForOrganization(this.CurrentOrganization, true);

        _invoices.Sort(SortInvoicesByDueDateReverse);

        // Format as JSON and return

        Response.ContentType = "application/json";
        string json = FormatAsJson();
        Response.Output.WriteLine (json);
        Response.End();
    }

    private static int SortInvoicesByDueDateReverse(InboundInvoice a, InboundInvoice b)
    {
        return DateTime.Compare(b.DueDate, a.DueDate);
    }


    private string FormatAsJson()
    {
        // Fields: item, dueDate, sender, budget, amount, progress, docs, action

        StringBuilder result = new StringBuilder (16384);

        string hasDoxString =
            "<img class='LocalIconDox action-icon' baseid='I{6}' />";

        result.Append ("{\"rows\":[");

        DateTime dueDateFormatBreakDate = DateTime.Today.AddDays(-240);

        foreach (InboundInvoice invoice in _invoices)
        {
            result.Append("{");
            result.AppendFormat(
                "\"item\":\"{0}\",\"dueDate\":\"{1}\",\"sender\":\"{2}\",\"budget\":\"{3}\",\"amount\":\"{4}\",\"progress\":\"{5}\"," +
                "\"dox\":\"" + (invoice.Documents.Count > 0 ? hasDoxString : "&nbsp;") + "\"," +
                "\"actions\":\"<span style='position:relative;left:-2px'>" +
                //"<img id=\\\"IconApproval{5}\\\" class=\\\"LocalIconApproval LocalNew LocalFundsInsufficient\\\" accountid=\\\"{6}\\\" amount=\\\"{4}\\\" baseid=\\\"{5}\\\" height=\\\"18\\\" width=\\\"24\\\" />" +
                //"<img id=\\\"IconApproved{5}\\\" class=\\\"LocalIconApproved LocalNew\\\" baseid=\\\"{5}\\\" height=\\\"18\\\" width=\\\"24\\\" />" +
                //"<img id=\\\"IconDenial{5}\\\" class=\\\"LocalIconDenial LocalNew\\\" baseid=\\\"{5}\\\" height=\\\"18\\\" width=\\\"24\\\" />" +
                //"<img id=\\\"IconDenied{5}\\\" class=\\\"LocalIconDenied LocalNew\\\" baseid=\\\"{5}\\\" height=\\\"18\\\" width=\\\"24\\\" />" +
                //"<img id=\\\"IconUndo{5}\\\" class=\\\"LocalIconUndo LocalNew\\\" baseid=\\\"{5}\\\" height=\\\"18\\\" width=\\\"24\\\" />" +
                "</span>\"",
                "#" + invoice.OrganizationSequenceId.ToString("N0"),

                JsonSanitize(invoice.DueDate.ToString(invoice.DueDate < dueDateFormatBreakDate ? "yyyy-MMM" : "MMM-dd")),
                JsonSanitize(invoice.Supplier),
                JsonSanitize(invoice.Budget.Name),
                JsonSanitize((invoice.AmountCents/100.0).ToString("N2")),
                GetProgressTicks(invoice),
                invoice.Identity); // Item #6 is only present in hasDoxString above
            result.Append("},");
        }

        if (_invoices.Count > 1)
        {
            result.Remove(result.Length - 1, 1); // remove last comma
        }

        result.Append ("]}");

        return result.ToString();
    }

    private string _emptyTick = "<img src='/Images/Icons/iconshock-empty-tick-128x96px.png' height='12' width='16'>";
    private string _greenTick = "<img src='/Images/Icons/iconshock-green-tick-128x96px.png' height='12' width='16'>";
    private string _redCross = "<img src='/Images/Icons/iconshock-red-cross-128x96px.png' height='12' width='16'>";
    private string _filler = "<img src='/Images/Icons/transparency-16px.png' height='12' width='16'>";


    private string GetProgressTicks(InboundInvoice invoice)
    {
        StringBuilder ticks = new StringBuilder(512);

        // The first tick is whether the invoice was even received yet, in anticipation of Purchase Orders
        // For now, it is always filled

        ticks.Append(_greenTick);

        // The second tick is whether the invoice has been attested

        if (invoice.Attested)
        {
            ticks.Append(_greenTick);

            // Is it also paid?

            if (invoice.PaidOut)
            {
                ticks.Append(_greenTick);

                // Is the payout closed, that is, registered closed with the bank?

                try
                {
                    if (Payout.FromDependency(invoice).Open)
                    {
                        ticks.Append(_emptyTick);
                    }
                    else
                    {
                        ticks.Append(_greenTick);
                    }
                }
                catch (ArgumentException)
                {
                    // There was no payout; the invoice was closed another way.

                    ticks.Append(_redCross);
                }
            }
            else
            {
                // attested but not paid yet

                ticks.Append(_emptyTick + _emptyTick);
            }
        }
        else // not attested
        {
            // Is the invoice closed? If so, it was denied entirely

            if (invoice.Open)
            {
                ticks.Append(_emptyTick + _emptyTick + _emptyTick);
            }
            else
            {
                // Closed, and therefore it was denied attestation
                ticks.Append(_redCross + _filler + _filler);
            }
        }

        return ticks.ToString();
    }


    private Dictionary<int, Int64> GetAttestationRights()
    {
        // Right now, this function is quite primitive. At some point in the future, it needs to take into
        // account that a budget may have several attesters. Right now, it just loops over all accounts and
        // checks the owner.

        Dictionary<int, Int64> result = new Dictionary<int, Int64>();
        FinancialAccounts accounts = FinancialAccounts.ForOrganization (CurrentOrganization);

        foreach (FinancialAccount account in accounts)
        {
            if (account.OwnerPersonId == CurrentUser.Identity)
            {
                if (account.AccountType == FinancialAccountType.Cost)
                {
                    result[account.Identity] = account.GetBudgetCentsRemaining();
                }
                else
                {
                    result[account.Identity] = 1; // any value
                }
            }
        }

        return result;
    }


}