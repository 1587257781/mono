using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using System.IO;
using System.Collections;
using System.Xml;

namespace MonoTests.SystemWeb.Framework
{
	[Serializable]
	public class FormRequest : PostableRequest
	{
		public FormRequest (Response response, string formId)
		{
			fields = new NameValueCollection();

			HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument ();
			htmlDoc.LoadHtml (response.Body);

			StringBuilder tempxml = new StringBuilder ();
			StringWriter tsw = new StringWriter (tempxml);
			htmlDoc.OptionOutputAsXml = true;
			htmlDoc.Save (tsw);

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (tempxml.ToString ());

			const string HTML_NAMESPACE = "http://www.w3.org/1999/xhtml";

			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			nsmgr.AddNamespace ("html", HTML_NAMESPACE);

#if USE_CORRECT_FORMID
			XmlNode formNode = doc.SelectSingleNode ("//html:form[@name='" + formId + "']", nsmgr);
#else
			XmlNode formNode = doc.SelectSingleNode ("//html:form", nsmgr);
#endif
			if (formNode == null)
				throw new ArgumentException ("Form with id='" + formId +
					"' was not found in document: " + response.Body);

			string actionUrl = formNode.Attributes["action"].Value;
			if (actionUrl != null && actionUrl != string.Empty)
				base.Url = actionUrl;
			XmlNode method = formNode.Attributes["method"];
			if (method != null && "POST" == method.Value)
				base.IsPost = true;
			else
				base.IsPost = false;
#if USE_CORRECT_FORMID

			foreach (XmlNode inputNode in formNode.SelectNodes ("//html:input", nsmgr))
#else
			foreach (XmlNode inputNode in doc.SelectNodes ("//html:input", nsmgr))
#endif
			{
				string name;
				string value = "";
				name = inputNode.Attributes["name"].Value;
				if (inputNode.Attributes["value"] != null)
					value = inputNode.Attributes["value"].Value;
				fields.Add (name, value);
			}
		}

		private NameValueCollection fields;
		public NameValueCollection Fields
		{
			get { return fields; }
		}

		public override string Url
		{
			get { return base.Url; }
			set { throw new Exception ("Must not change Url of FormPostback"); }
		}

		public override bool IsPost
		{
			get { return base.IsPost; }
			set { throw new Exception ("Must not change IsPost of FormPostback"); }
		}

		public override string PostContentType
		{
			get { return "application/x-www-form-urlencoded"; }
			set { throw new Exception ("Must not change PostContentType of FormPostback"); }
		}

		public override byte[] PostData
		{
			get { return Encoding.ASCII.GetBytes (GetParameters ()); }
			set { throw new Exception ("Must not change PostData of FormPostback"); }
		}

		protected override string GetQueryString ()
		{
			if (IsPost)
				return "";
			else
				return GetParameters ();
		}

		protected string GetParameters ()
		{
			StringBuilder query = new StringBuilder ();
			bool first = true;
			foreach (string key in Fields.AllKeys) {
				if (first)
					first = false;
				else
					query.Append ("&");
				query.Append (HttpUtility.UrlEncode (key));
				query.Append ("=");
				query.Append (HttpUtility.UrlEncode (Fields[key]));
			}
			return query.ToString ();
		}
	}
}
