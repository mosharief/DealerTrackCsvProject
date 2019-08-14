<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="CSVWebForms.WebForm1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">

   <div><label id="lblGridHeader" title="My Dealer Tracker" style="font-size:large"></label></div>
    <div>
        <asp:GridView ID="gvDealerTrack" runat="server" AutoGenerateColumns="false" OnRowDataBound="gvDealerTrack_RowDataBound">
            <Columns>
                <asp:BoundField DataField="DealNumber" HeaderText="Deal Number" />
                <asp:BoundField DataField="CustomerName" HeaderText="Customer Name" HtmlEncode="false"/>
                <asp:BoundField DataField="DealershipName" HeaderText="Dealership Name" />
                <asp:BoundField DataField="Vehicle" HeaderText="Vehicle" />
                <asp:BoundField DataField="Price" HeaderText="Price"/>
                <asp:BoundField DataField="Date" HeaderText="Date" DataFormatString="{0:MM/dd/yyyy}" />
            </Columns>
        </asp:GridView>    
    </div>
    </form>
</body>
</html>
