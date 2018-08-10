using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace TwitterFollowerLists.Web.Pages
{

	[Authorize]
	public class IndexModel : PageModel
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly IConfiguration _config;
		private readonly IHostingEnvironment _env;
		private readonly ILogger<IndexModel> _logger;

		public IndexModel(IConfiguration config, UserManager<IdentityUser> userManager, ILogger<IndexModel> logger, IHostingEnvironment env)
		{
			_userManager = userManager;
			_config = config;
			_env = env;
			_logger = logger;
		}

		public async Task OnGet([FromQuery(Name = "id")] string twitterId, [FromQuery] bool DoIt = false)
		{

			if (string.IsNullOrEmpty(twitterId))
			{
				Redirect("/Error");
			}

			var (consumerKey, consumerSecret, accessToken, accessTokenSecret) = await GetTwitterAuthKeys();

			ExceptionHandler.SwallowWebExceptions = false;

			Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);

			LoggedInTwitterId = (await UserAsync.GetAuthenticatedUser()).ScreenName;
			TwitterId = twitterId;
			ListName = $"private_{twitterId}";

			// Ensure that we can get some information

			var usersFriends = await Tweetinvi.UserAsync.GetFriendIds(twitterId, 5000);
			ListMemberCount = usersFriends.Count() + 1;
			var membersToAdd = usersFriends.Select(f => new UserIdentifier(f));
			var list = await TwitterListAsync.GetExistingList(ListNameSlug, LoggedInTwitterId);
			if (list == null)
			{
				list = await TwitterListAsync.CreateList(
					ListName,
					PrivacyMode.Private,
					$"The list of accounts that @{twitterId} follows as of {DateTime.UtcNow.ToLongDateString()}. Should have {membersToAdd.Count()} members.");
				_logger.LogInformation($"Created list {{{ListName}}}.");
			}
			else
			{
				TwitterListUpdateParameters updateParameter = new TwitterListUpdateParameters()
				{
					// Name = ListName,
					// PrivacyMode = PrivacyMode.Private,
					Description = $"The list of accounts that @{twitterId} follows as of {DateTime.UtcNow.ToLongDateString()}. Should have {membersToAdd.Count()} members."
				};
				var existingMembers = await list.GetMembersAsync(5000);
				membersToAdd = membersToAdd.Where(m => !existingMembers.Any(existing => m.Id == existing.Id));
				if (DoIt || !_env.IsDevelopment())
				{
					var success = await list.UpdateAsync(updateParameter);
					if (success)
					{
						_logger.LogInformation($"Updated description of {{{ListName}}}.");
					}
					else
					{
						_logger.LogError($"Updating description of {{{ListName}}} failed.");
					}
				}
			}

			NoOfMembersToAdd = membersToAdd.Count();

			// Need to limit to 5000 accounts
			if (DoIt || !_env.IsDevelopment())
			{
				await list.AddMemberAsync(await UserAsync.GetUserFromScreenName(twitterId));
				var result = await list.AddMultipleMembersAsync(membersToAdd);
				ResponseString = result.ToString();
				if (result == MultiRequestsResult.Success)
				{
					_logger.LogInformation($"AddMultipleMembersToList: Adding {membersToAdd.Count()} to list {{{ListName}}} finished with result: {result}.");
				}
				else
				{
					_logger.LogError($"AddMultipleMembersToList: Adding {membersToAdd.Count()} to list {{{ListName}}} finished with result: {result}.");
				}
			}
			else
			{
				_logger.LogWarning($"DEV MODE: AddMultipleMembersToList: Didn't add {membersToAdd.Count()} to list {{{ListName}}}.");
				ResponseString = "Nothing added in Development Mode";
			}

			NoOfActualMembers = (await list.GetMembersAsync(5000)).Count();

			var rateLimits = await RateLimitAsync.GetCurrentCredentialsRateLimits();

		}

		public string TwitterId { get; set; }
		public string LoggedInTwitterId { get; set; }
		public string ListName { get; set; }
		public string ListNameSlug { get => ListName.ToLower().Replace(" ", "-"); }
		public long ListMemberCount { get; set; }
		public string ResponseString { get; set; }
		public int NoOfMembersToAdd { get; set; }
		public int NoOfActualMembers { get; set; }

		private async Task<(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)> GetTwitterAuthKeys()
		{

			string accessToken = "";
			string accessTokenSecret = "";
			string consumerKey = "";
			string consumerSecret = "";

			if (User.Identity.IsAuthenticated)
			{
				var user = await _userManager.GetUserAsync(User);
				accessToken = await _userManager.GetAuthenticationTokenAsync(user, "Twitter", "access_token");
				accessTokenSecret = await _userManager.GetAuthenticationTokenAsync(user, "Twitter", "access_token_secret");
				consumerKey = _config["Authentication:Twitter:ConsumerKey"];
				consumerSecret = _config["Authentication:Twitter:ConsumerSecret"];
			}

			return (consumerKey: consumerKey, consumerSecret: consumerSecret, accessToken: accessToken, accessTokenSecret: accessTokenSecret);

		}


	}
}
