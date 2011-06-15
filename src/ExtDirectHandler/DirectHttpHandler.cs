﻿using System;
using System.IO;
using System.Web;
using ExtDirectHandler.Configuration;
using Newtonsoft.Json;

namespace ExtDirectHandler
{
	public class DirectHttpHandler : IHttpHandler
	{
		private static Metadata _metadata;
		private static ObjectFactory _objectFactory = new ObjectFactory();
		private readonly DirectRequestsBuilder _directRequestsBuilder = new DirectRequestsBuilder();

		public bool IsReusable
		{
			get { return false; }
		}

		public static void SetMetadata(Metadata metadata)
		{
			if(_metadata != null)
			{
				throw new Exception("Already configured");
			}
			_metadata = metadata;
		}

		public static void SetObjectFactory(ObjectFactory factory)
		{
			if(_objectFactory != null)
			{
				throw new Exception("Already configured");
			}
			_objectFactory = factory;
		}

		public void ProcessRequest(HttpContext context)
		{
			switch(context.Request.HttpMethod)
			{
				case "GET":
					DoGet(context.Request, context.Response);
					break;
				case "POST":
					DoPost(context.Request, context.Response);
					break;
			}
		}

		private void DoPost(HttpRequest httpRequest, HttpResponse httpResponse)
		{
			DirectRequest[] requests = httpRequest.Form.Count > 0 ? _directRequestsBuilder.BuildFromFormData(httpRequest.Form) : _directRequestsBuilder.BuildFromRequestData(new StreamReader(httpRequest.InputStream, httpRequest.ContentEncoding));
			var responses = new DirectResponse[requests.Length];
			for(int i = 0; i < requests.Length; i++)
			{
				responses[i] = new DirectHandler(_objectFactory, _metadata).Handle(requests[i]);
			}
			httpResponse.ContentType = "application/json";
			using(var jsonWriter = new JsonTextWriter(new StreamWriter(httpResponse.OutputStream, httpResponse.ContentEncoding)))
			{
				new JsonSerializer().Serialize(jsonWriter, responses.Length == 1 ? (object)responses[0] : responses);
			}
		}

		private void DoGet(HttpRequest request, HttpResponse response)
		{
			string ns = request.QueryString["ns"];
			response.ContentType = "text/javascript";
			string url = request.Url.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.Unescaped);
			response.Write(new DirectApiBuilder(_metadata).BuildApi(ns, url));
		}
	}
}