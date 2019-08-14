using System;
using System.Linq;
using System.Web.UI.WebControls;


namespace CSVWebForms
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var path = Server.MapPath(@"~/Dealertrack-CSV-Example.csv");
            var data = CSVHelperClass.Read<DealerTrack>(new CsvSource(path)).ToList();
            
            gvDealerTrack.DataSource = data;
            gvDealerTrack.DataBind();

            var item = data.GroupBy(x => x.Vehicle).OrderByDescending(i => i.Count()).First().Key;
            txtMostCarSell.Text = item;
        }


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