using System;
using System.Collections;
using System.Windows.Forms;

namespace IP_Bulk_lookup
{
    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// </summary>
    internal class ListViewColumnSorter : IComparer
    {
        /// <summary>
        /// Case insensitive comparer object
        /// </summary>
        private readonly CaseInsensitiveComparer ObjectCompare;
        /// <summary>
        /// Class constructor.  Initializes various elements
        /// </summary>
        public ListViewColumnSorter()
        {
            SortColumn = 0; // Initialize the column to '0'            
            Order = SortOrder.None;// Initialize the sort order to 'none'       
            ObjectCompare = new CaseInsensitiveComparer();// Initialize the CaseInsensitiveComparer object
        }

        /// <summary>
        /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;
            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;
            int tempCount = -1;
            int tempCount2 = -1;
            Int32.TryParse(listviewX.SubItems[SortColumn].Text, out tempCount);
            Int32.TryParse(listviewY.SubItems[SortColumn].Text, out tempCount2);
            if (tempCount > 0)
            {
                if (tempCount2 > -1)//в столбце числа а не строки, их надо в int перевести чтобы сортировка не по первой букве цифры была
                {
                    compareResult = ObjectCompare.Compare(Convert.ToInt32(listviewX.SubItems[SortColumn].Text), Convert.ToInt32(listviewY.SubItems[SortColumn].Text));
                }
                else
                {
                    compareResult = ObjectCompare.Compare(listviewX.SubItems[SortColumn].Text, listviewY.SubItems[SortColumn].Text);
                }
            }
            else
            {
                compareResult = ObjectCompare.Compare(listviewX.SubItems[SortColumn].Text, listviewY.SubItems[SortColumn].Text);
            }
            if (Order == SortOrder.Ascending) // Calculate correct return value based on object comparison
            {
                return compareResult; // Ascending sort is selected, return normal result of compare operation
            }
            else if (Order == SortOrder.Descending)
            {
                return -compareResult;// Descending sort is selected, return negative result of compare operation
            }
            else
            {
                return 0;// Return '0' to indicate they are equal
            }
        }

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn { set; get; }

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order { set; get; }
    }
}
