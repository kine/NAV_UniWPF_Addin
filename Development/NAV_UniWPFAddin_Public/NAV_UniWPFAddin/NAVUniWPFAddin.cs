using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Dynamics.Framework.UI.Extensibility.WinForms;
using Microsoft.Dynamics.Framework.UI.Extensibility;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Markup;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Input;
using System.Windows.Controls;


namespace NAV_UniWPFAddin
{

    [ControlAddInExport("NAVERTICA.DynamicsNAV.UniWPFAddin")]

    [Description("Allows to add WPF controls into NAV dynamically")]


    public class NAVUniWPFAddin : StringControlAddInBase
    {
        private bool AllowCaption;
        //private UIElement uiElement;
        private String lastText;

        protected override System.Windows.Forms.Control CreateControl()
        {
            AllowCaption = true;
            ElementHost myhost = new ElementHost();
            //Control XamlControl = (Control)
            try
            {
                myhost.Child = (UIElement)XamlReader.Parse(NAV_UniWPFAddin.Properties.Resources.DefaultXAML);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                return null;
            }
            myhost.MinimumSize = new System.Drawing.Size(10, 10);
            myhost.Width = 100;
            myhost.Height = 100;
            myhost.Dock = DockStyle.Fill;
            //myhost.AutoSize = true;
            myhost.BackColor = System.Drawing.Color.LightGray;

            return myhost;

        }
        public static T Cast<T>(object o)
        {
            return (T)o;
        }
        private void SetNewWPFElement(String xaml)
        {
            try
            {
                ElementHost host = (ElementHost)this.Control;
                FrameworkElement WPFElement = (FrameworkElement)XamlReader.Parse(xaml);
                host.Child = WPFElement;
                host.Update();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }

        private void RegisterChildNames(FrameworkElement element)
        {
            try
            {
                System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)element;
                foreach (FrameworkElement child in panel.Children)
                {
                    panel.RegisterName(child.Name, child);
                    RegisterChildNames(child);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void UnregisterChildNames(FrameworkElement element)
        {
            try
            {
                System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)element;
                foreach (FrameworkElement child in panel.Children)
                {
                    panel.UnregisterName(child.Name);
                    UnregisterChildNames(child);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void AddNewWPFElement(XmlNode node)
        /*
         * <Element Parent="objname">XAML</AddElement>
         *
        */
        {
            try
            {
                ElementHost host = (ElementHost)this.Control;
                String objName = null;
                if ((node.Attributes != null) && (node.Attributes.GetNamedItem("Parent") != null))
                {
                    objName = node.Attributes.GetNamedItem("Parent").Value;
                }
                if (objName == null)
                {
                    SetNewWPFElement(node.InnerXml);
                }
                else
                {
                    FrameworkElement WPFElement = (FrameworkElement)XamlReader.Parse(node.InnerXml);
                    System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)host.Child;
                    Object myObject = panel.FindName(objName);
                    System.Windows.Controls.Panel targetPanel = (System.Windows.Controls.Panel)myObject;
                    if (targetPanel != null)
                    {
                        NameScope.SetNameScope(WPFElement, NameScope.GetNameScope(targetPanel));
                        targetPanel.Children.Add(WPFElement);
                        targetPanel.RegisterName(WPFElement.Name, WPFElement);
                        RegisterChildNames(WPFElement);
                    }
                    host.Update();
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }


        private void DeleteWPFElement(XmlNode node)
        /*
         * <DelElement Parent="parentname" ObjectName="objname"/>
         *
        */
        {
            try
            {
                ElementHost host = (ElementHost)this.Control;
                String parentName = node.Attributes.GetNamedItem("Parent").Value;
                String objName = node.Attributes.GetNamedItem("ObjectName").Value;
                System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)host.Child;
                Object parentObject = panel.FindName(parentName);
                System.Windows.Controls.Panel parentPanel= (System.Windows.Controls.Panel)parentObject;
                /*
                foreach (FrameworkElement element in parentPanel.Children)
                {
                    if (element.Name == objName)
                    {
                        parentPanel.Children.Remove(element);
                        return;
                    }

                }
                */
                FrameworkElement element = (FrameworkElement)parentPanel.FindName(objName);
                if (parentObject != null)
                {
                    parentPanel.UnregisterName(element.Name);
                    UnregisterChildNames(element);
                    parentPanel.Children.Remove(element);
                }
                
                host.Update();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }

        private void SetHostAttributes(XmlNode node)
        {
            ElementHost host = (ElementHost)this.Control;
            Type hostType = typeof(ElementHost);

            foreach (XmlAttribute attr in node.Attributes)
            {
                PropertyInfo myf = typeof(ElementHost).GetProperty(attr.Name);
                Type propertyType = myf.PropertyType;
                myf.SetValue(host, TypeDescriptor.GetConverter(propertyType).ConvertFrom(attr.Value), null);
            }
        }

        private void SetAddinAttributes(XmlNode node)
        {
            ElementHost host = (ElementHost)this.Control;
            foreach (XmlAttribute attr in node.Attributes)
            {

                switch (attr.Name)
                {
                    case "AllowCaption": this.AllowCaption = Convert.ToBoolean(attr.Value);
                        break;
                }
            }
        }

        private void CallObjectMethod(XmlNode node)
        {
            /*
             * <CallMethod Object="objname" Name="name of method"><Param1>aaa<Param1></CallMethod>
             * */
            /*
            ElementHost host = (ElementHost)this.Control;
            String objName = node.Attributes.GetNamedItem("Object").Value;
            String methodName = node.Attributes.GetNamedItem("Name").Value;
            Control[] controlList = host.Child.Controls.Find(objName, true);
            Object[] paramsList;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Object[]));
            StringReader sr = new StringReader(node.InnerXml);
            paramsList = (Object[])xmlSerializer.Deserialize(sr);

            foreach (Control control in controlList)
            {
                MethodInfo methodInfo = control.GetType().GetMethod(methodName);
                methodInfo.Invoke(control, paramsList);
            }
             * */

            ElementHost host = (ElementHost)this.Control;
            String objName = node.Attributes.GetNamedItem("Object").Value;
            String methodName = node.Attributes.GetNamedItem("Name").Value;
            Object[] param;
            System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)host.Child;
            Object myObject = panel.FindName(objName);
            if (myObject != null)
            {
                MethodInfo myf = myObject.GetType().GetMethod(methodName);
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Object[]));
                StringReader sr = new StringReader(node.InnerXml);
                param = (Object[])xmlSerializer.Deserialize(sr);
                myf.Invoke(myObject, param);
            }

        }

        private void SetObjectProperty(XmlNode node)
        {
            /*
             * <SetProperty Object="objname" Name="name of property" Type="TypeName">Value<SetProperty>"
             * */
            ElementHost host = (ElementHost)this.Control;
            String objName = node.Attributes.GetNamedItem("Object").Value;
            String propertyName = node.Attributes.GetNamedItem("Name").Value;
            String typeName = node.Attributes.GetNamedItem("Type").Value;
            //Control[] controlList = host.Controls.Find(objName, true);
            Object param;
            System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)host.Child;
            Object myObject = panel.FindName(objName);
            if (myObject != null)
            {
                PropertyInfo myf = myObject.GetType().GetProperty(propertyName);
                if (myf != null)
                {
                    Type propertyType = myf.PropertyType;
                    if (typeName == "")
                        typeName = propertyType.Name;
                    //XmlRootAttribute root=new XmlRootAttribute("Value");
                    //root.DataType = "Object";
                    Type deserializeType = System.Type.GetType(typeName, false, true);
                    XmlSerializer xmlSerializer = new XmlSerializer(deserializeType);
                    StringReader sr = new StringReader(node.InnerXml);
                    //StringReader sr = new StringReader(a);
                    param = xmlSerializer.Deserialize(sr);

                    if ((propertyType == param.GetType()) || (propertyType == typeof(Object)))
                    {
                        myf.SetValue(myObject, param, null);
                    }
                    else
                    {
                        myf.SetValue(myObject, TypeDescriptor.GetConverter(propertyType).ConvertFrom(param), null);
                    }
                }
            }

        }

        private void GetObjectProperty(XmlNode node)
        {
            /*
             * <GetProperty Object="objname" Name="name of property"/>"
             * */
            ElementHost host = (ElementHost)this.Control;
            String objName = node.Attributes.GetNamedItem("Object").Value;
            String propertyName = node.Attributes.GetNamedItem("Name").Value;
            //Control[] controlList = host.Controls.Find(objName, true);
            Object result;
            System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)host.Child;
            Object myObject = panel.FindName(objName);
            if (myObject != null)
            {
                PropertyInfo myf = myObject.GetType().GetProperty(propertyName);
                if (myf != null)
                {
                    Type propertyType = myf.PropertyType;
                    result = myf.GetValue(myObject, null);
                    String resultXml = Serialize(result);
                    resultXml = "<GetProperty><Sender>" + objName + "</Sender><Property>" + propertyName + "</Property><Value>" + resultXml + "</Value></GetProperty>";
                    this.RaiseControlAddInEvent(Convert.ToInt32(((System.Windows.Controls.Control)myObject).Tag), resultXml);
                }
            }

        }

        private void SetObjectEvent(XmlNode node)
        {
            /*
             * <SetEvent Object="objname" Name="EventName"/>"
             * */
            ElementHost host = (ElementHost)this.Control;
            String objName = node.Attributes.GetNamedItem("Object").Value;
            String eventName = node.Attributes.GetNamedItem("Name").Value;
            System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)host.Child;
            Object myObject = panel.FindName(objName);
            if (myObject != null)
            {
                EventInfo myf = myObject.GetType().GetEvent(eventName);
                //Type propertyType = myf.PropertyType;
                Type eventType = myf.EventHandlerType;
                if (eventType == typeof(RoutedEventHandler))
                {
                    myf.AddEventHandler(myObject, new RoutedEventHandler(RoutedNAVEvent));
                }
                else
                    if (eventType == typeof(System.Windows.Input.MouseEventHandler))
                    {
                        myf.AddEventHandler(myObject, new System.Windows.Input.MouseEventHandler(MouseNAVEvent));
                    }
                    else
                        if (eventType == typeof(System.Windows.Input.MouseButtonEventHandler))
                        {
                            myf.AddEventHandler(myObject, new System.Windows.Input.MouseButtonEventHandler(MouseButtonNAVEvent));
                        }
                        else
                            if (eventType == typeof(System.Windows.DragEventHandler))
                            {
                                myf.AddEventHandler(myObject, new System.Windows.DragEventHandler(DragEventNAVEvent));
                            }
                            else
                                if (eventType == typeof(EventHandler))
                                {
                                    myf.AddEventHandler(myObject, new EventHandler(EventNAVEvent));
                                }
                                else
                                    if (eventType == typeof(TextCompositionEventHandler))
                                    {
                                        myf.AddEventHandler(myObject, new TextCompositionEventHandler(TextCompositionNAVEvent));
                                    }
                                    else
                                        if (eventType == typeof(SelectionChangedEventHandler))
                                        {
                                            myf.AddEventHandler(myObject, new SelectionChangedEventHandler(SelectionChangedNAVEvent));
                                        }
                                        else
                                        {
                                throw (new Exception("Unknown event type: " + eventType.ToString()));
                            }
                //Delegate d = Delegate.CreateDelegate(eventType, this, NAVEvent);

            }
        }

        private void MergeResource(XmlNode node)
        {
            /*
             * <MergeResource Object="objname">Value<MergeResource>"
             * */
            ElementHost host = (ElementHost)this.Control;
            String objName = node.Attributes.GetNamedItem("Object").Value;
            System.Windows.Controls.Panel panel = (System.Windows.Controls.Panel)host.Child;
            Object myObject = panel.FindName(objName);
            FrameworkElement element = (FrameworkElement)myObject;
            if (element != null)
            {
                ResourceDictionary dict = new ResourceDictionary();
                dict = (ResourceDictionary)XamlReader.Parse(node.InnerXml);
                element.Resources.MergedDictionaries.Add(dict);
                host.Update();
            }
        }

        public override bool AllowCaptionControl
        {
            get
            {
                return this.AllowCaption;
            }
        }

        public override string Value
        {

            get
            {

                return base.Value;

            }

            set
            {
                try
                {
                    if ((Convert.ToString(value) != "") && (Convert.ToString(value) != this.lastText))
                    {
                        this.lastText = Convert.ToString(value);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(value);
                        foreach (XmlNode node in doc.FirstChild.ChildNodes)
                        {
                            /*
                            if (node.Name == "Element")
                            {
                                SetNewWPFElement(node.InnerXml);
                            }
                             * */
                            switch (node.Name) {

                                case "Element": AddNewWPFElement(node); break;
                                case "DelElement": DeleteWPFElement(node); break;
                                case "Host": SetHostAttributes(node); break;
                                case "Addin": SetAddinAttributes(node); break;
                                case "CallMethod": CallObjectMethod(node); break;
                                case "SetProperty": SetObjectProperty(node); break;
                                case "SetEvent": SetObjectEvent(node); break;
                                case "GetProperty": GetObjectProperty(node); break;
                                case "MergeResource": MergeResource(node); break;
                                default: throw(new Exception("Unknow element "+node.Name));
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.InnerException != null)
                    {
                        System.Windows.MessageBox.Show(e.Message + "\n" + e.InnerException.Message);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(e.Message + "\n" + e.Message);
                    }
                }

            }

        }

        /* private SerializeInside(XmlNode node, */

        private String Serialize(object obj, XmlAttributeOverrides overrides=null)
        {
            if (obj != null)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType(),overrides);

                MemoryStream memStrm = new MemoryStream();
                XmlTextWriter xmlSink = new XmlTextWriter(memStrm, Encoding.UTF8);
                xmlSerializer.Serialize(xmlSink, obj);

                //StringWriter sw = new StringWriter();

                //xmlSerializer.Serialize(sw, obj);
                memStrm.Position = 0;
                StreamReader sr = new StreamReader(memStrm);
                String resultString = sr.ReadToEnd();
                resultString = resultString.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");
                return resultString;
            }
            else
                return "";

        }

        private void RoutedNAVEvent(object sender, RoutedEventArgs e)
        {
            //String senderXML = Serialize(sender);
            try
            {
                FrameworkElement uiElement = (FrameworkElement)sender;
                int tag = 0;
                try
                {
                    tag = Convert.ToInt32(uiElement.Tag);
                }
                catch (Exception ex) { };
                this.RaiseControlAddInEvent(tag, "<Root><Sender>" + uiElement.Name + "</Sender><Event>" + e.RoutedEvent.Name + "</Event></Root>");
            }
            catch (Exception ex)
            {
                this.RaiseControlAddInEvent(0, ex.Message);
            }
        }

        private void DragEventNAVEvent(object sender, System.Windows.DragEventArgs e)
        {
            //String senderXML = Serialize(sender);
            try
            {
                FrameworkElement uiElement = (FrameworkElement)sender;
                int tag = 0;
                try
                {
                    tag = Convert.ToInt32(uiElement.Tag);
                }
                catch (Exception ex) { }

                this.RaiseControlAddInEvent(tag, "<Root><Sender>" + uiElement.Name + "</Sender><Event>" + e.RoutedEvent.Name + "</Event><Data>" +
                    Serialize(e.Data) + "</Data>" + "</Root>");
            }
            catch (Exception ex)
            {
                this.RaiseControlAddInEvent(0, ex.Message);
            }
        }
        private void MouseNAVEvent(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //String senderXML = Serialize(sender);
            try
            {
                FrameworkElement uiElement = (FrameworkElement)sender;
                int tag = 0;
                try
                {
                    tag = Convert.ToInt32(uiElement.Tag);
                }
                catch (Exception ex) { }
                this.RaiseControlAddInEvent(tag, "<Root><Sender>" + uiElement.Name + "</Sender><Event>" + e.RoutedEvent.Name + "</Event></Root>");
            }
            catch (Exception ex)
            {
                this.RaiseControlAddInEvent(0, ex.Message);
            }
        }
        private void MouseButtonNAVEvent(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //String senderXML = Serialize(sender);
            try
            {
                FrameworkElement uiElement = (FrameworkElement)sender;
                int tag = 0;
                try
                {
                    tag = Convert.ToInt32(uiElement.Tag);
                }
                catch (Exception ex) { }
                this.RaiseControlAddInEvent(tag, "<Root><Sender>" + uiElement.Name + "</Sender><Event>" + e.RoutedEvent.Name + "</Event>" +
                    "<Data>"+Serialize(e)+"</Data>"
                    + "</Root>");
            }
            catch (Exception ex)
            {
                this.RaiseControlAddInEvent(0, ex.Message);
            }
        }

        private void EventNAVEvent(object sender, EventArgs e)
        {
            //String senderXML = Serialize(sender);
            try
            {
                FrameworkElement uiElement = (FrameworkElement)sender;
                int tag = 0;
                try
                {
                    tag = Convert.ToInt32(uiElement.Tag);
                }
                catch (Exception ex) { }
                this.RaiseControlAddInEvent(tag, "<Root><Sender>" + uiElement.Name + "</Sender><Event>" + e.ToString() + "</Event>" +
                    "<Data>"+Serialize(e)+"</Data>"
                    + "</Root>");
            }
            catch (Exception ex)
            {
                this.RaiseControlAddInEvent(0, ex.Message);
            }
        }

        private void TextCompositionNAVEvent(object sender, TextCompositionEventArgs e)
        {
            //String senderXML = Serialize(sender);
            try
            {
                FrameworkElement uiElement = (FrameworkElement)sender;
                int tag = 0;
                try
                {
                    tag = Convert.ToInt32(uiElement.Tag);
                }
                catch (Exception ex) { }
                this.RaiseControlAddInEvent(tag, "<Root><Sender>" + uiElement.Name + "</Sender><Event>" + e.RoutedEvent.Name + "</Event>" +
                    "<Data>"+Serialize(e)+"</Data>"
                    + "</Root>");
            }
            catch (Exception ex)
            {
                this.RaiseControlAddInEvent(0, ex.Message);
            }
        }

        private void SelectionChangedNAVEvent(object sender, SelectionChangedEventArgs e)
        {
            //String senderXML = Serialize(sender);
            try
            {
                FrameworkElement uiElement = (FrameworkElement)sender;
                int tag = 0;
                try
                {
                    tag = Convert.ToInt32(uiElement.Tag);
                }
                catch (Exception ex) { }
                /*
                XmlAttributeOverrides overrides = new XmlAttributeOverrides();
                XmlAttributes attr= new XmlAttributes();
                attr.XmlIgnore = true;
                overrides.Add(typeof(SelectionChangedEventArgs), "OriginalSource", attr);
                overrides.Add(typeof(SelectionChangedEventArgs), "Source", attr);
                overrides.Add(typeof(SelectionChangedEventArgs), "RoutedEvent", attr);
                 * */
                this.RaiseControlAddInEvent(tag, "<Root><Sender>" + uiElement.Name + "</Sender><Event>" + e.RoutedEvent.Name + "</Event>" 
//                    +"<Data>" + Serialize(e,overrides) + "</Data>"
                    + "</Root>");
            }
            catch (Exception ex)
            {
                this.RaiseControlAddInEvent(0, ex.Message);
            }
        }

        private void OnButtonClick(object sender, System.EventArgs e)
        {

            this.RaiseControlAddInEvent(1, "Button");

        }

    }
}
