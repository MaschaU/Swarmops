﻿<%@ Control Language="C#" AutoEventWireup="true" Inherits="Swarmops.Frontend.Controls.Financial.CurrencyTextBox" Codebehind="CurrencyTextBox.ascx.cs" %>
<%@ Import Namespace="Swarmops.Common.Enums" %>

<!-- TODO: Add nice autocomplete stuff -->

 <% if (this.Layout == LayoutDirection.Vertical) { %><div class="stacked-input-control"><% } %>
    <asp:TextBox ID="TextInput" runat="server" CssClass="align-for-numbers" /><asp:HiddenField ID="EnteredCurrency" runat="server"/><asp:HiddenField ID="EnteredAmount" runat="server" />
 <% if (this.Layout == LayoutDirection.Vertical) { %></div><% } %>


<script type="text/javascript">

    function <%=this.ClientID%>_focus() {
        $('#<%=this.TextInput.ClientID%>').focus();
    }

    function <%=this.ClientID%>_setValue(newValue) {
        $('#<%=this.TextInput.ClientID%>').val(newValue);
    }

    function <%=this.ClientID%>_val() {
        return $('#<%=this.TextInput.ClientID%>').val();
    }

    function <%=this.ClientID%>_initialize(initValue) {
        _initVal_<%=this.TextInput.ClientID%> = initValue;
        <%=this.ClientID%>_setValue(initValue);
        // <%=this.ClientID%>_enable();
    }

    $(document).ready(function () {

        $('#<%=this.ClientID%>_TextInput').blur(function () {

            var currencyText = $('#<%=this.ClientID%>_TextInput').val();

            var jsonData = {};
            jsonData.input = currencyText;

            if (currencyText.trim().indexOf(' ') >= 0) {
                // the text contains at least one space, so try to interpret it and convert it to presentation currency

                SwarmopsJS.ajaxCall('/Automation/FinancialFunctions.aspx/InterpretCurrency',
                    jsonData,
                    function(data) {
                        if (data.Success) {
                            $('#<%=this.ClientID%>_TextInput').val(data.DisplayAmount);
                            $('#<%=this.ClientID%>_EnteredCurrency').val(data.CurrencyCode);
                            $('#<%=this.ClientID%>_EnteredAmount').val(data.EnteredAmount);
                        }
                    });
            } else {
                // the text does NOT contain at least one space, so we should format it for presentation currency
                SwarmopsJS.ajaxCall('/Automation/Formatting.aspx/FormatCurrencyString',
                    jsonData,
                    function(data) {
                        if (data.Success) {
                            $('#<%=this.ClientID%>_TextInput').val(data.NewValue);
                        }
                    });
            }
        });


    });

</script>
