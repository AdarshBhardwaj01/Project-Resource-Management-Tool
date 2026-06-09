using System.Net.Http.Json;

using PRM.Models.DTOs.Auth;



namespace PRM.ConsoleUI.Services;



public class AuthApiClient : ApiClientBase

{

    public AuthApiClient(HttpClient httpClient, AuthSession session)

        : base(httpClient, session)

    {

    }



    public async Task<LoginResponse?> LoginAsync(LoginRequest request)

    {

        HttpClient.DefaultRequestHeaders.Authorization = null;



        var response = await HttpClient.PostAsJsonAsync("api/auth/login", request);

        await EnsureSuccessAsync(response, "Login failed.");



        var loginResponse = await ReadJsonAsync<LoginResponse>(response);



        if (loginResponse is not null)

        {

            Session.Token = loginResponse.Token;

            Session.UserId = loginResponse.UserId;

            Session.FullName = loginResponse.FullName;

            Session.Username = loginResponse.Username;

            Session.Role = loginResponse.Role.ToString();

            Session.ForcePasswordChange = loginResponse.ForcePasswordChange;

        }



        return loginResponse;

    }



    public async Task<string> ChangePasswordAsync(ChangePasswordRequest request)

    {

        ApplyAuthorizationHeader();



        var response = await HttpClient.PostAsJsonAsync("api/auth/change-password", request);

        await EnsureSuccessAsync(response, "Password change failed.");



        var result = await ReadJsonAsync<ApiMessageResponse>(response);

        Session.ForcePasswordChange = false;

        return result?.Message ?? "Password updated. Welcome!";

    }



    public void Logout()

    {

        Session.Clear();

        HttpClient.DefaultRequestHeaders.Authorization = null;

    }

}


