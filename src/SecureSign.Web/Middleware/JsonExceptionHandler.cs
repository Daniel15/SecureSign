/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SecureSign.Web.Models;

namespace SecureSign.Web.Middleware
{
	/// <summary>
	/// Returns exceptions as JSON
	/// </summary>
	public static class JsonExceptionHandler
	{
		/// <summary>
		/// Add JSON exception handling to the pipeline
		/// </summary>
		/// <param name="app">App to add exception handler to</param>
		/// <param name="isDev">If <c>true</c>, the stack trace will be included in exceptions</param>
		/// <returns></returns>
		public static IApplicationBuilder UseJsonExceptionHandler(this IApplicationBuilder app, bool isDev)
		{
			app.UseExceptionHandler(options =>
			{
				options.Run(async context => await HandleError(context, isDev));
			});
			return app;
		}

		/// <summary>
		/// Handles when an exception occurs in the app
		/// </summary>
		/// <param name="context">Current HTTP request</param>
		/// <param name="isDev">If <c>true</c>, the stack trace will be included in exceptions</param>
		/// <returns></returns>
		private static async Task HandleError(HttpContext context, bool isDev)
		{
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			context.Response.ContentType = "application/json";
			var ex = context.Features.Get<IExceptionHandlerFeature>();
			if (ex != null)
			{
				var error = new Error
				{
					Message = ex.Error.Message,
				};
				if (isDev)
				{
					error.Stack = ex.Error.StackTrace;
				}
				var response = JsonConvert.SerializeObject(error, new JsonSerializerSettings
				{
					ContractResolver = new CamelCasePropertyNamesContractResolver()
				});
				await context.Response.WriteAsync(response);
			}
		}
	}
}
