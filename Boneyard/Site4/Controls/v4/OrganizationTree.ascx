﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="OrganizationTree.ascx.cs" Inherits="Controls_v4_OrganizationTree" %>
<%@ Register TagPrefix="telerik" Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" %>

<telerik:RadTreeView ID="Tree" Skin="WebBlue" OnNodeClick="Tree_NodeClick" runat="server" />
