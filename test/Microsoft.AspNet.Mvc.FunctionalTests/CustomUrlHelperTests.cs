// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    /// <summary>
    /// The tests here verify the extensibility of <see cref="UrlHelper"/>.
    ///
    /// Following are some of the scenarios exercised here:
    /// 1. Based on configuration, generate Content urls pointing to local or a CDN server
    /// 2. Based on configuration, generate lower case urls
    /// </summary>
    public class CustomUrlHelperTests : IClassFixture<MvcTestFixture<UrlHelperWebSite.Startup>>
    {
        private const string _cdnServerBaseUrl = "http://cdn.contoso.com";

        public CustomUrlHelperTests(MvcTestFixture<UrlHelperWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task CustomUrlHelper_UseAllRouteValues()
        {
            var result = await Client.GetAsync("/api/RouteValueUsage/urlHelper/helper");
            var path = await result.Content.ReadAsStringAsync();

            Assert.NotNull(path);
            Assert.Equal("/api/routevalueusage/get/1234", path);
        }

        private static IUrlHelper CreateUrlHelper(IServiceProvider services)
        {
            var actionContextAccessor = services.GetRequiredService<IActionContextAccessor>();
            actionContextAccessor.ActionContext = new ActionContext();

            return services.GetRequiredService<IUrlHelper>();
        }

        private static IServiceCollection GetServices()
        {
            var services = new ServiceCollection();
            services.AddTransient<IUrlHelper, UrlHelper>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddTransient<IActionSelector, DefaultActionSelector>();
            services.AddSingleton<IActionDescriptorsCollectionProvider, DefaultActionDescriptorsCollectionProvider>();
            services.AddSingleton<IActionSelectorDecisionTreeProvider, ActionSelectorDecisionTreeProvider>();
            services.AddTransient<ILoggerFactory, LoggerFactory>();

            return services;
        }

        [Fact]
        public async Task CustomUrlHelper_GeneratesUrlFromController()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Home/UrlContent");
            var responseData = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(_cdnServerBaseUrl + "/bootstrap.min.css", responseData);
        }

        [Fact]
        public async Task CustomUrlHelper_GeneratesUrlFromView()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Home/Index");
            var responseData = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(_cdnServerBaseUrl + "/bootstrap.min.css", responseData);
        }

        [Theory]
        [InlineData("http://localhost/Home/LinkByUrlRouteUrl", "/api/simplepoco/10")]
        [InlineData("http://localhost/Home/LinkByUrlAction", "/home/urlcontent")]
        public async Task LowercaseUrls_LinkGeneration(string url, string expectedLink)
        {
            // Arrange & Act
            var response = await Client.GetAsync(url);
            var responseData = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedLink, responseData, ignoreCase: false);
        }
    }
}