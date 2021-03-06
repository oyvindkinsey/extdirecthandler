﻿using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using SharpTestsEx;
using System.Linq;

namespace ExtDirectHandler.Tests
{
	[TestFixture]
	public class DirectRequestsBuilderTest
	{
		private DirectRequestsBuilder _target;

		[SetUp]
		public void SetUp()
		{
			_target = new DirectRequestsBuilder();
		}

		[Test]
		public void Should_build_from_form_parameters()
		{
			var form = new NameValueCollection {
				{"extTID", "123"},
				{"extAction", "Action"},
				{"extMethod", "method"},
				{"extType", "type"},
				{"extUpload", "true"},
				{"field1", "value1"},
				{"field2", "value2"}
			};

			var file1Stream = new MemoryStream();
			var file2Stream = new MemoryStream();
			var files = new Dictionary<string, Stream> {
				{"file1", file1Stream},
				{"file2", file2Stream}
			};

			var actual = _target.Build(new StringReader(""), form, files)[0];

			actual.Tid.Should().Be.EqualTo(123);
			actual.Action.Should().Be.EqualTo("Action");
			actual.Method.Should().Be.EqualTo("method");
			actual.Type.Should().Be.EqualTo("type");
			actual.Upload.Should().Be.True();
			JToken.DeepEquals(actual.JsonData, new JArray());
			actual.FormData["field1"].Should().Be.EqualTo("value1");
			actual.FormData["field2"].Should().Be.EqualTo("value2");
			actual.FormData["file1"].Should().Be.SameInstanceAs(file1Stream);
			actual.FormData["file2"].Should().Be.SameInstanceAs(file2Stream);
		}

		[Test]
		public void Should_build_from_content()
		{
			var actual = _target.Build(new StringReader(@"
{
	tid: 123,
	type: 'rpc',
	action: 'Action',
	method: 'method',
	data: [ 'value1', 'value2' ]
}
"), new NameValueCollection(), new Dictionary<string, Stream>());
			actual.Should().Have.Count.EqualTo(1);
			actual[0].Tid.Should().Be.EqualTo(123);
			actual[0].Type.Should().Be.EqualTo("rpc");
			actual[0].Action.Should().Be.EqualTo("Action");
			actual[0].Method.Should().Be.EqualTo("method");
			actual[0].Upload.Should().Be.False();
			actual[0].JsonData.Should().Have.SameSequenceAs(new[] { new JValue("value1"), new JValue("value2") });
			actual[0].FormData.Should().Be.Empty();
		}

		[Test]
		public void Should_tolerate_null_data()
		{
			var actual = _target.Build(new StringReader(@"
{
	tid: 123,
	type: 'rpc',
	action: 'Action',
	method: 'method',
	data: null
}
"), new NameValueCollection(), new Dictionary<string, Stream>());
			actual.Should().Have.Count.EqualTo(1);
			JToken.DeepEquals(new JArray(), actual[0].JsonData).Should().Be.True();
			actual[0].FormData.Should().Be.Empty();
		}

		[Test]
		public void Should_build_multiple_requests_from_reader()
		{
			var actual = _target.Build(new StringReader(@"
[ {
	tid: 123,
	type: 'rpc',
	action: 'Action1',
	method: 'method1',
	data: []
}, {
	tid: 456,
	type: 'rpc',
	action: 'Action2',
	method: 'method2',
	data: []
} ]
"), new NameValueCollection(), new Dictionary<string, Stream>());
			actual.Should().Have.Count.EqualTo(2);
			actual.Select(x => x.Tid).Should().Have.SameSequenceAs(new[] { 123, 456 });
			actual.Select(x => x.FormData.Count).Should().Have.SameSequenceAs(new[] { 0, 0 });
		}
	}
}