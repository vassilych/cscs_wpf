using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WpfCSCS
{
    public class DataGridCollectionView : ListCollectionView
    {
        const int PageSize = 20;

        #region Events
        public event Action<DataGridCollectionView, CountEventArgs> ItemsCount = delegate { };
        public event Action<DataGridCollectionView, ItemsEventArgs> ItemsRequest = delegate { };
        #endregion

        #region Fields
        private DataTable dataTable;
        Dictionary<int, DataRow> dataRows = new Dictionary<int, DataRow>();
        #endregion

        #region Constructors
        private DataGridCollectionView(IList list) : base(list)
        {
        }

        public DataGridCollectionView(DataTable dt) : this(dt.DefaultView)
        {
            dataTable = dt;
        }
        #endregion

        #region Properties
        public override int Count
        {
            get
            {
                CountEventArgs args = new CountEventArgs(this);
                ItemsCount(this, args);

                return args.Count;
            }
        }


        #endregion

        #region Public Methods
        bool m_Lock = false;
        public override object GetItemAt(int index)
        {
            if (!dataRows.ContainsKey(index))
            {
                if (m_Lock)
                    return dataTable.NewRow();

                m_Lock = true;
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        int startIndex = index;

                        ItemsEventArgs args = new ItemsEventArgs(this, startIndex, PageSize);
                        ItemsRequest(this, args);

                        if (args.Items == null)
                            return;

                        int i = 0;
                        foreach (DataRow row in args.Items.OfType<DataRow>())
                        {
                            int newIndex = startIndex + i;
                            dataRows[newIndex] = row;
                            i++;

                            Console.WriteLine(newIndex);
                        }

                        Action action = () => this.Refresh();
                        Application.Current.Dispatcher.BeginInvoke(action);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    m_Lock = false;
                });

                return dataTable.NewRow();
            }

            return dataRows[index];
        }

        #endregion

        #region Helper Methods
        #endregion

        #region Event Handling
        #endregion
    }
}
