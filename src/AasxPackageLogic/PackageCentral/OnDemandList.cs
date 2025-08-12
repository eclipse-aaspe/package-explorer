/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using AdminShellNS.DiaryData;
using Aas = AasCore.Aas3_1;
using Extensions;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// Base class for the side information describing the (status of the) data in
    /// the collection. Intended to be derived and stuffed with information.
    /// Note: <c>Id</c> is required for the <c>AddIfNew</c> operation.
    /// </summary>
    public class OnDemandSideInfoBase
    {
        public string Id = "";
    }

    /// <summary>
    /// Pair of side information and actual data element.
    /// </summary>
    public class OnDemandListItem<T> where T : Aas.IIdentifiable
    {
        public OnDemandSideInfoBase SideInfo;
        public T Data;
    }

    /// <summary>
    /// Test
    /// </summary>
    public class OnDemandList<T,V> : IList<T> 
        where V : OnDemandSideInfoBase 
        where T : Aas.IIdentifiable
    {
        protected List<OnDemandListItem<T>> _items = new List<OnDemandListItem<T>>();

        public T this[int index] { 
            get => _items[index].Data; 
            set {
                _items[index] = new OnDemandListItem<T>() { Data = value };
            }
        }

        /// <summary>
        /// Updates the data portion of the list index, however leaves the sideinfo unmodified!
        /// </summary>
        public void Update(int index, T value)
        {
            _items[index].Data = value;
        }

        int ICollection<T>.Count => _items.Count;

        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T item)
        {
            _items.Add(new OnDemandListItem<T>() { Data = item });
        }

        public void Add(T item, V sideInfo)
        {
            _items.Add(new OnDemandListItem<T>() { SideInfo = sideInfo, Data = item });
        }

        public bool AddIfNew(T item, V sideInfo)
        {
            var ndx = FindId(item?.Id);
            if (ndx >= 0)
                return false;
            
            _items.Add(new OnDemandListItem<T>() { SideInfo = sideInfo, Data = item });
            return true;
        }

        void ICollection<T>.Clear()
        {
            _items.Clear();
        }

        bool ICollection<T>.Contains(T item)
        {
            foreach (var x in _items)
                if ((object) x.Data == (object) item)
                    return true;
            return false;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _items.Count; i++)
                array[arrayIndex + i] = _items[i].Data.Copy();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach (var it in _items)
                yield return it.Data;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var it in _items)
                yield return it.Data;
        }

        int IList<T>.IndexOf(T item)
        {
            for (int i = 0; i < _items.Count; i++)
                if ((object)item == (object)_items[i].Data)
                    return i;
            return -1;
        }

        // TODO (MIHO, 2025-05-17): Join with above
        public int TestIndexOf(T item)
        {
            for (int i = 0; i < _items.Count; i++)
                if ((object)item == (object)_items[i].Data)
                    return i;
            return -1;
        }

        public int FindId(string id)
        {
            for (int i = 0; i < _items.Count; i++)
                if ((_items[i].Data?.Id?.HasContent() == true && _items[i].Data.Id == id)
                    || (_items[i].SideInfo?.Id?.HasContent() == true && _items[i].SideInfo.Id == id))
                    return i;
            return -1;
        }

        void IList<T>.Insert(int index, T item)
        {
            _items.Insert(index, new OnDemandListItem<T>() { Data = item });
        }

        bool ICollection<T>.Remove(T item)
        {
            for (int i = 0; i < _items.Count; i++)
                if ((object)item == (object)_items[i].Data)
                {
                    _items.RemoveAt(i);
                    return true;
                }
            return false;
        }

        void IList<T>.RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        public V GetSideInfo(int index)
        {
            if (index < 0 || index >= _items.Count)
                return null;
            return _items[index].SideInfo as V;
        }

        public V SetSideInfo(int index, V sideInfo)
        {
            if (index < 0 || index >= _items.Count)
                return null;
            _items[index].SideInfo = sideInfo;
            return sideInfo;
        }
    }

}
