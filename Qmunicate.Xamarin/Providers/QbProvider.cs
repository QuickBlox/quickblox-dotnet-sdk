using System;
using Quickblox.Sdk;
using System.Threading.Tasks;
using Quickblox.Sdk.GeneralDataModel.Response;
using System.Net;
using Quickblox.Sdk.GeneralDataModel.Models;
using MugenMvvmToolkit.Interfaces;
using Xamarin.Forms;

namespace Qmunicate.Xamarin
{
	public class QbProvider
	{
		private readonly QuickbloxClient client = new QuickbloxClient(ApplicationKeys.ApplicationId,
			ApplicationKeys.AuthorizationKey,
			ApplicationKeys.AuthorizationSecret,
			ApplicationKeys.ApiBaseEndpoint,
			ApplicationKeys.ChatEndpoint,
			new QbLogger());

		public int UserId { get; set; }

		public QbProvider ()
		{

		}

		public async Task<int> LoginWithEmailAsync(string email, string password){
			var sessionResponse = await this.client.AuthenticationClient.CreateSessionWithEmailAsync (email, password); 
			if (await HandleResponse(sessionResponse, HttpStatusCode.Created)){
				UserId = sessionResponse.Result.Session.UserId;
				return sessionResponse.Result.Session.UserId;
			}
			else if ((int)sessionResponse.StatusCode == 422) {
				// Add logout
				return -1;
			}

			return 0;
		}

		public async Task<int> LoginWithFbUserAsync(String accessToken)
		{
			var sessionResponse = await this.client.AuthenticationClient.CreateSessionWithSocialNetworkKey("facebook",
				"public_profile",
				accessToken,
				null,
				new DeviceRequest() { Platform = Device.OS == TargetPlatform.iOS ? Platform.ios : Platform.android, Udid = ((IDeviceUid)App.Current.IocContainer.Get(typeof(IDeviceUid))).GetDeviceIdentifier() });
			if (sessionResponse.StatusCode == HttpStatusCode.Created) {
				this.client.Token = sessionResponse.Result.Session.Token;
				this.UserId = sessionResponse.Result.Session.UserId;
				return sessionResponse.Result.Session.UserId;
			} 
			else if ((int)sessionResponse.StatusCode == 422) {
				// Add logout
				return -1;
			}
			return 0;
		}


		private async Task<bool> HandleResponse(HttpResponse response, HttpStatusCode resultStatusCode)
		{
			switch (response.StatusCode) {
			case HttpStatusCode.NotFound:
				{ 
				}
				break;
			case HttpStatusCode.Unauthorized:
				{
					
				}
				break;
			default:
				break;
			}

			return response.StatusCode == resultStatusCode;

		}
	}
}

