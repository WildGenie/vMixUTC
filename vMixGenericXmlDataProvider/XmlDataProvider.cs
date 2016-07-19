﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using vMixControllerDataProvider;

namespace vMixGenericXmlDataProvider
{
    public class XmlDataProvider : DependencyObject, IvMixDataProvider, INotifyPropertyChanged
    {

        private static int _maxid = 0;
        private int _id = 0;

        private static Dictionary<string, CacheStats> _cahce = new Dictionary<string, CacheStats>();

        private OnWidgetUI _ui;
        private string _url;
        private string _xpath;
        private string _namespaces;

        public System.Windows.UIElement CustomUI
        {
            get
            {
                return _ui;
            }
        }

        public bool IsProvidingCustomProperties
        {
            get
            {
                return true;
            }
        }

        List<string> _data = new List<string>();
        bool _retrivingData = false;

        public string[] Values
        {
            get
            {

                try
                {
                    if (_cahce.ContainsKey(_url) && (DateTime.Now - _cahce[_url].LastUpdated).TotalMilliseconds < Period)
                    {
                        UpdateData(_cahce[_url].Document);
                    }
                    else
                    {
                        WebRequest req = WebRequest.Create(_url);
                        if (!_retrivingData)
                            req.BeginGetResponse(new AsyncCallback(BeginGetResponseCallback), req);
                    }
                }
                catch (Exception)
                {

                }
                return _data.ToArray();
            }
        }

        private void BeginGetResponseCallback(IAsyncResult ar)
        {

            _retrivingData = true;
            _data.Clear();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load((ar.AsyncState as WebRequest).EndGetResponse(ar).GetResponseStream());
                if (_cahce.ContainsKey(_url))
                {
                    _cahce[_url].Document = doc;
                    _cahce[_url].LastId = _id;
                    _cahce[_url].LastUpdated = DateTime.Now;
                }
                else
                    _cahce.Add(_url, new CacheStats() { Document = doc, LastId = _id, LastUpdated = DateTime.Now });

                UpdateData(doc);

            }
            catch (Exception)
            {

            }
            _retrivingData = false;

        }

        private void UpdateData(XmlDocument doc)
        {

            XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);

            foreach (var item in _namespaces.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
            {
                try
                {
                    var nsn = item.Split(' ');
                    ns.AddNamespace(nsn[0], nsn[1]);
                }
                catch (Exception ex) { }
            }

            var nodes = doc.SelectNodes(_xpath, ns);
            Data = nodes.OfType<XmlElement>().Select(x => x.InnerText).ToList();
        }

        public string Url
        {
            get { return (string)GetValue(UrlProperty); }
            set { SetValue(UrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Url.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register("Url", typeof(string), typeof(XmlDataProvider), new PropertyMetadata("", propchanged));


        public string NameSpaces
        {
            get { return (string)GetValue(NameSpacesProperty); }
            set { SetValue(NameSpacesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NameSpaces.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NameSpacesProperty =
            DependencyProperty.Register("NameSpaces", typeof(string), typeof(XmlDataProvider), new PropertyMetadata("", propchanged));



        private static void propchanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Url")
                (d as XmlDataProvider)._url = (string)e.NewValue;
            if (e.Property.Name == "XPath")
                (d as XmlDataProvider)._xpath = (string)e.NewValue;
            if (e.Property.Name == "NameSpaces")
                (d as XmlDataProvider)._namespaces = (string)e.NewValue;
        }

        public string XPath
        {
            get { return (string)GetValue(XPathProperty); }
            set { SetValue(XPathProperty, value); }
        }

        public int Period
        {
            get;

            set;
        }

        public List<string> Data
        {
            get
            {
                return _data;
            }

            set
            {
                _data = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Data"));
            }
        }

        // Using a DependencyProperty as the backing store for XPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XPathProperty =
            DependencyProperty.Register("XPath", typeof(string), typeof(XmlDataProvider), new PropertyMetadata("", propchanged));

        public event PropertyChangedEventHandler PropertyChanged;

        public List<object> GetProperties()
        {
            return new List<object>() { Url, XPath, NameSpaces };
        }

        public void SetProperties(List<object> props)
        {
            Url = (string)props[0];
            XPath = (string)props[1];
            if (props.Count > 2)
                NameSpaces = (string)props[2];
        }

        public void ShowProperties(System.Windows.Window owner)
        {
            PropertiesWindow _properties = new PropertiesWindow();
            _properties.Owner = owner;
            _properties.DataContext = this;
            var previous = GetProperties();
            var result = _properties.ShowDialog();
            if (!(result.HasValue && result.Value))
                SetProperties(previous);
        }

        public XmlDataProvider()
        {
            _id = _maxid++;
            _ui = new OnWidgetUI() { DataContext = this };
            _namespaces = "";

        }

    }
}