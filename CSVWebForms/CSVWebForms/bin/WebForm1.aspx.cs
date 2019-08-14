using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using System.Threading;

namespace CSVWebForms
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var path = Server.MapPath(@"~/Dealertrack-CSV-Example.csv");
            var data = CSVHelperClass.Read<DealerTrack>(new CsvSource(path)).ToList();

            gvDealerTrack.DataSource = null;
            gvDealerTrack.DataSource = data;
            gvDealerTrack.DataBind();
        }

        //protected override void InitializeCulture()
        //{
        //    CultureInfo c1 = new CultureInfo("en-US");
        //    c1.NumberFormat.CurrencySymbol = "U+0024";
        //    Thread.CurrentThread.CurrentCulture = c1;
        //    base.InitializeCulture();
        //}

        protected void gvDealerTrack_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                for (int i = 0; i < gvDealerTrack.HeaderRow.Cells.Count; i++)
                {
                    string strHeader = gvDealerTrack.HeaderRow.Cells[i].Text;
                    if (strHeader.ToString() == "Price")
                    {
                        e.Row.Cells[i].Text = "CAD$" + e.Row.Cells[i].Text;
                    }
                }
            }
        }

    }
}