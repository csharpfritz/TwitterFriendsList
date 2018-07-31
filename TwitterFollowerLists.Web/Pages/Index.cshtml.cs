using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TwitterFollowerLists.Web.Pages
{

	[Authorize]
	public class IndexModel : PageModel
	{

		private readonly HttpClient _client;

		public IndexModel(IHttpClientFactory client)
		{
			_client = client.CreateClient("twitterApi");
		}

		public async Task OnGet(string id)
		{

			if (string.IsNullOrEmpty(id)) {
				Redirect("/Error");
			}

			TwitterId = id;

			var user = User;

			//AuthenticationProperties.

			//Microsoft.AspNetCore.Http.Authentication.AuthenticationProperties


			var friends = await _client.GetStringAsync($"friends/list.json?user_id={id}&count=200&skip_status=1&include_user_entities=false");


		}

		public string TwitterId { get; set; }

	}
}
