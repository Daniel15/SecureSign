/**
 * Copyright (c) 2017 Daniel Lo Nigro (Daniel15)
 * 
 * This source code is licensed under the MIT license found in the 
 * LICENSE file in the root directory of this source tree. 
 */

using Microsoft.AspNetCore.Mvc;

namespace SecureSign.Web.Controllers
{
	public class PingController : Controller
	{
		[Route("ping")]
		public IActionResult Index()
		{
			return Content("Hello from SecureSign");
		}
	}
}