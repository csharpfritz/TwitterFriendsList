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
using Microsoft.Extensions.Configuration;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.DTO.QueryDTO;

namespace TwitterFollowerLists.Web.Pages
{

	[Authorize]
	public class IndexModel : PageModel
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly IConfiguration _config;

		public IndexModel(IConfiguration config, UserManager<IdentityUser> userManager)
		{
			_userManager = userManager;
			_config = config;
		}

		public async Task OnGet([FromQuery(Name ="id")] string twitterId)
		{

			if (string.IsNullOrEmpty(twitterId)) {
				Redirect("/Error");
			}

			TwitterId = twitterId;

			var authKeys = await GetTwitterAuthKeys();

			Auth.SetUserCredentials(_config["Authentication:Twitter:ConsumerKey"], _config["Authentication:Twitter:ConsumerSecret"], authKeys.accessToken, authKeys.accessTokenSecret);

			// TODO: Ensure we do not create more than 1000 lists

			var query = string.Format("https://api.twitter.com/1.1/friends/ids.json?screen_name={0}", twitterId);

			// Ensure that we can get some information
			ExceptionHandler.SwallowWebExceptions = false;

			var usersFriends = await Tweetinvi.UserAsync.GetFriendIds(twitterId, 5000);
			var newList = TwitterList.CreateList($"private_{twitterId}", Tweetinvi.Models.PrivacyMode.Private, $"The list of accounts that {twitterId} follows as of {DateTime.Today.ToShortDateString()}");

			// Need to limit to 5000 accounts
			var result = newList.AddMultipleMembers(usersFriends.Select(u => new UserIdentifier(u)));

			var rateLimits = await RateLimitAsync.GetCurrentCredentialsRateLimits();
			Console.WriteLine($"Result:  {result}");

		}

		public string TwitterId { get; set; }

		private async Task<(string accessToken, string accessTokenSecret)> GetTwitterAuthKeys() {

			string accessToken = "";
			string accessTokenSecret = "";

			if (User.Identity.IsAuthenticated)
			{
				var user = await _userManager.GetUserAsync(User);
				accessToken = await _userManager.GetAuthenticationTokenAsync(user, "Twitter", "access_token");
				accessTokenSecret = await _userManager.GetAuthenticationTokenAsync(user, "Twitter", "access_token_secret");
			}

			return (accessToken, accessTokenSecret);

		}


	}
}
