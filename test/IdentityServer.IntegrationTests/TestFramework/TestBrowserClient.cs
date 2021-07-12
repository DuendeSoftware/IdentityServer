// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using CsQuery;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Duende.IdentityServer.IntegrationTests.TestFramework
{
    public class TestBrowserClient : HttpClient
    {
        class CookieHandler : DelegatingHandler
        {
            public CookieContainer CookieContainer { get; } = new CookieContainer();
            public Uri CurrentUri { get; private set; }
            public HttpResponseMessage LastResponse { get; private set; }

            public CookieHandler(HttpMessageHandler next)
                : base(next)
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CurrentUri = request.RequestUri;
                string cookieHeader = CookieContainer.GetCookieHeader(request.RequestUri);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }

                var response = await base.SendAsync(request, cancellationToken);

                if (response.Headers.Contains("Set-Cookie"))
                {
                    var responseCookieHeader = string.Join(",", response.Headers.GetValues("Set-Cookie"));
                    CookieContainer.SetCookies(request.RequestUri, responseCookieHeader);
                }

                LastResponse = response;

                return response;
            }
        }

        private CookieHandler _handler;
        
        public CookieContainer CookieContainer => _handler.CookieContainer;
        public Uri CurrentUri => _handler.CurrentUri;
        public HttpResponseMessage LastResponse => _handler.LastResponse;

        public TestBrowserClient(HttpMessageHandler handler)
            : this(new CookieHandler(handler))
        {
        }

        private TestBrowserClient(CookieHandler handler)
            : base(handler)
        {
            _handler = handler;
        }

        public Cookie GetCookie(string name)
        {
            return GetCookie(_handler.CurrentUri.ToString(), name);
        }
        public Cookie GetCookie(string uri, string name)
        {
            return _handler.CookieContainer.GetCookies(new Uri(uri)).Cast<Cookie>().Where(x => x.Name == name).FirstOrDefault();
        }

        public void RemoveCookie(string name)
        {
            RemoveCookie(CurrentUri.ToString(), name);
        }
        public void RemoveCookie(string uri, string name)
        {
            var cookie = CookieContainer.GetCookies(new Uri(uri)).Cast<Cookie>().Where(x => x.Name == name).FirstOrDefault();
            if (cookie != null)
            {
                cookie.Expired = true;
            }
        }

        public async Task FollowRedirectAsync()
        {
            LastResponse.StatusCode.Should().Be(302);
            var location = LastResponse.Headers.Location.ToString();
            await GetAsync(location);
        }

        public Task<HttpResponseMessage> PostFormAsync(HtmlForm form)
        {
            return PostAsync(form.Action, new FormUrlEncodedContent(form.Inputs));
        }

        public Task<HtmlForm> ReadFormAsync(string selector = null)
        {
            return ReadFormAsync(LastResponse, selector);
        }
        public async Task<HtmlForm> ReadFormAsync(HttpResponseMessage response, string selector = null)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var htmlForm = new HtmlForm
            {

            };

            var html = await response.Content.ReadAsStringAsync();

            var dom = new CQ(html);
            var form = dom.Find(selector ?? "form");
            form.Length.Should().Be(1);

            var postUrl = form.Attr("action");
            if (!postUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (postUrl.StartsWith("/"))
                {
                    postUrl = CurrentUri.Scheme + "://" + CurrentUri.Authority + postUrl;
                }
                else
                {
                    postUrl = CurrentUri + postUrl;
                }
            }
            htmlForm.Action = postUrl;


            var data = new Dictionary<string, string>();

            var inputs = form.Find("input");
            foreach (var input in inputs)
            {
                var name = input.GetAttribute("name");
                var value = input.GetAttribute("value");

                if (!data.ContainsKey(name))
                {
                    data.Add(name, value);
                }
                else
                {
                    data[name] = value;
                }
            }
            htmlForm.Inputs = data;

            return htmlForm;
        }


        public Task<string> ReadElementTextAsync(string selector)
        {
            return ReadElementTextAsync(LastResponse, selector);
        }
        public async Task<string> ReadElementTextAsync(HttpResponseMessage response, string selector)
        {
            var html = await response.Content.ReadAsStringAsync();

            var dom = new CQ(html);
            var element = dom.Find(selector);
            return element.Text();
        }

        public Task<string> ReadElementAttributeAsync(string selector, string attribute)
        {
            return ReadElementAttributeAsync(LastResponse, selector, attribute);
        }
        public async Task<string> ReadElementAttributeAsync(HttpResponseMessage response, string selector, string attribute)
        {
            var html = await response.Content.ReadAsStringAsync();

            var dom = new CQ(html);
            var element = dom.Find(selector);
            return element.Attr(attribute);
        }

        public Task AssertExistsAsync(string selector)
        {
            return AssertExistsAsync(LastResponse, selector);
        }

        public async Task AssertExistsAsync(HttpResponseMessage response, string selector)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var html = await response.Content.ReadAsStringAsync();

            var dom = new CQ(html);
            var element = dom.Find(selector);
            element.Length.Should().BeGreaterThan(0);
        }

        public Task AssertNotExistsAsync(string selector)
        {
            return AssertNotExistsAsync(selector);
        }
        public async Task AssertNotExistsAsync(HttpResponseMessage response, string selector)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var html = await response.Content.ReadAsStringAsync();

            var dom = new CQ(html);
            var element = dom.Find(selector);
            element.Length.Should().Be(0);
        }

        public Task AssertErrorPageAsync(string error = null)
        {
            return AssertErrorPageAsync(LastResponse, error);
        }
        public async Task AssertErrorPageAsync(HttpResponseMessage response, string error = null)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await AssertExistsAsync(response, ".error-page");

            if (!String.IsNullOrWhiteSpace(error))
            {
                var errorText = await ReadElementTextAsync(response, ".alert.alert-danger");
                errorText.Should().Contain(error);
            }
        }

        public Task AssertValidationErrorAsync(string error = null)
        {
            return AssertValidationErrorAsync(error);
        }
        public async Task AssertValidationErrorAsync(HttpResponseMessage response, string error = null)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await AssertExistsAsync(response, ".validation-summary-errors");

            if (!String.IsNullOrWhiteSpace(error))
            {
                var errorText = await ReadElementTextAsync(response, ".validation-summary-errors");
                errorText.ToLowerInvariant().Should().Contain(error.ToLowerInvariant());
            }
        }
    }

    [DebuggerDisplay("{Action}, Inputs: {Inputs.Count}")]
    public class HtmlForm
    {
        public HtmlForm(string action = null)
        {
            Action = action;
        }

        public string Action { get; set; }
        public Dictionary<string, string> Inputs { get; set; } = new Dictionary<string, string>();

        public string this[string key]
        {
            get
            {
                if (Inputs.ContainsKey(key)) return Inputs[key];
                return null;
            }
            set
            {
                Inputs[key] = value;
            }
        }
    }
}