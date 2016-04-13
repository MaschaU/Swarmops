﻿<%@ Page Title="" Language="C#" MasterPageFile="~/Master-v5.master" AutoEventWireup="true" CodeFile="BitcoinHotwallet.aspx.cs" Inherits="Swarmops.Frontend.Pages.v5.Ledgers.BitcoinHotwallet" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PlaceHolderHead" Runat="Server">
	<script type="text/javascript">

	    $(document).ready(function () {

	        $('#tableProfitLoss').treegrid(
	        {
	            onBeforeExpand: function (foo) {
	                $('span.profitlossdata-collapsed-' + foo.id).fadeOut('fast', function () {
	                    $('span.profitlossdata-expanded-' + foo.id).fadeIn('slow');
	                });
	            },

	            onBeforeCollapse: function (foo) {
	                $('span.profitlossdata-expanded-' + foo.id).fadeOut('fast', function () {
	                    $('span.profitlossdata-collapsed-' + foo.id).fadeIn('slow');
	                });
	            },

	            onLoadSuccess: function () {
	                $('div.datagrid').css('opacity', 1);
	                $('#imageLoadIndicator').hide();
	                $('span.loadingHeader').hide();

                    $('span.commonHeader').show();
	            }
	        });

	        $('div.datagrid').css('opacity', 0.4);
	    });

	    var currentYear = <%=DateTime.Today.Year %>;

	</script>
    <style>
	    .content h2 select {
		    font-size: 16px;
            font-weight: bold;
            color: #1C397E;
            letter-spacing: 1px;
	    }
    </style>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="PlaceHolderMain" Runat="Server">
    
    <h2><asp:Label ID="LabelContentHeader" runat="server" />&nbsp;<img alt="Loading" src="/Images/Abstract/ajaxloader-blackcircle.gif" ID="imageLoadIndicator" /></h2>
    <table id="tableHotwallet" title="" class="easyui-treegrid" style="width:680px;height:600px"  
        url="Json-HotwalletData.aspx"
        rownumbers="false"
        animate="true"
        fitColumns="true"
        idField="id" treeField="name">
        <thead>  
            <tr>  
                <th field="derivePath" width="178"><asp:Literal ID="LiteralHeaderDerivationPath" runat="server"/></th>  
                <th field="address" width="80" align="right"><span class="commonHeader" id="headerAddress" style="display:none"><asp:Literal ID="LiteralHeaderAddress" runat="server" /></span><span class="loadingHeader">&mdash;</span></th>  
                <th field="balanceMicrocoins" width="80" align="right"><span class="commonHeader" id="headerQ1" style="display:none"><asp:Literal ID="LiteralHeaderMicrocoins" runat="server" /></span><span class="loadingHeader">&mdash;</span></th>
                <th field="balanceFiat" width="80" align="right"><span class="commonHeader" id="headerQ2" style="display:none"><asp:Literal ID="LiteralHeaderFiatValue" runat="server" /></span><span class="loadingHeader">&mdash;</span></th>
            </tr>  
        </thead>  
    </table> 
</asp:Content>

<asp:Content ID="Content3" ContentPlaceHolderID="PlaceHolderSide" Runat="Server">
 

</asp:Content>

