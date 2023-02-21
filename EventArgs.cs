using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCSCS
{
    public class CountEventArgs : EventArgs
    {
        DataGridCollectionView m_Parent;
        int m_Count = 0;

        public CountEventArgs(DataGridCollectionView parent)
        {
            m_Parent = parent;
        }

        public int Count
        {
            get => m_Count;
            set => m_Count = value;
        }
    }


    public class ItemsEventArgs : EventArgs
    {
        DataGridCollectionView m_Parent;
        int m_StartIndex;
        int m_RequestedItemsCount;
        IList<object> m_Items;

        public ItemsEventArgs(DataGridCollectionView parent, int startIndex, int requestedCount)
        {
            m_Parent = parent;
            m_StartIndex = startIndex;
            m_RequestedItemsCount = requestedCount;
        }

        public int RequestedItemsCount => m_RequestedItemsCount;
        public int StartIndex => m_StartIndex;

        internal IList<object> Items => m_Items;

        public void SetItems(IList<object> items)
        {
            m_Items = items;
        }
    }
}
